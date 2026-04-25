# Vakuu Room Injection Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace non-boss rooms at entry time with the existing Ancient event `Vakuu`, using 66% odds for unknown map points and 6.6% odds for all other non-boss map points.

**Architecture:** Replace the current map-node mutation code with a reusable room injection utility layer. A generic Harmony patch on `RunManager.CreateRoom(...)` builds a `RoomInjectionContext`, asks registered rules for a replacement room, and returns the replacement before the game enters the room. The first concrete rule is `VakuuInjectionRule`, which injects `new EventRoom(ModelDb.Event<Vakuu>())`.

**Tech Stack:** C# 12, .NET 9, Godot.NET.Sdk 4.6.2, STS2 `v0.103.2`, Harmony `0Harmony.dll`, STS2 `sts2.dll`.

---

## File Structure

- Modify `MapNodeChanger.cs`
  - Keep only mod bootstrap responsibilities: load config, remember current `RunState`, register injection rules, install room injection patch.
  - Remove all map traversal and `MapPoint.PointType` mutation logic.

- Modify `MapNodeChanger.csproj`
  - Add a non-private reference to `0Harmony.dll`.

- Modify `MapNodeChangerConfig.json.example`
  - Replace old node mutation rules with Vakuu injection settings.

- Create `Utils/RoomInjection/IRoomInjectionRule.cs`
  - Defines the generic extension point for room replacement rules.

- Create `Utils/RoomInjection/RoomInjectionContext.cs`
  - Immutable context passed to rules: run state, map point type, rolled room type, explicit model, original room, current map point, act index, floor.

- Create `Utils/RoomInjection/RoomInjectionService.cs`
  - Stores rules, handles per-room roll caching, and applies the first replacement rule that matches.

- Create `Utils/RoomInjection/RoomInjectionInstaller.cs`
  - Installs the Harmony postfix patch for `RunManager.CreateRoom(...)`.

- Create `Features/Vakuu/VakuuInjectionConfig.cs`
  - JSON-backed config model for the feature.

- Create `Features/Vakuu/VakuuInjectionRule.cs`
  - Implements the requested odds and creates the Vakuu event room.

- Modify `README.md`
  - Replace the old "map node changer" behavior description with room-entry Vakuu injection behavior.

---

### Task 1: Add Harmony Reference

**Files:**
- Modify: `MapNodeChanger.csproj`

- [ ] **Step 1: Add `0Harmony.dll` reference**

Edit the `ItemGroup` in `MapNodeChanger.csproj` to include Harmony beside the existing `sts2` reference:

```xml
  <ItemGroup>
    <Reference Include="0Harmony">
      <HintPath>$(Sts2DataDir)\0Harmony.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="sts2">
      <HintPath>$(Sts2DataDir)\sts2.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>
```

- [ ] **Step 2: Build to verify the reference path**

Run:

```powershell
$env:Path = [Environment]::GetEnvironmentVariable('Path','Machine') + ';' + [Environment]::GetEnvironmentVariable('Path','User')
dotnet build MapNodeChanger.csproj
```

Expected: build succeeds or fails only on existing source code, not on unresolved `0Harmony`.

- [ ] **Step 3: Commit**

```powershell
git add MapNodeChanger.csproj
git commit -m "chore: reference Harmony for room injection"
```

---

### Task 2: Create Generic Room Injection Types

**Files:**
- Create: `Utils/RoomInjection/IRoomInjectionRule.cs`
- Create: `Utils/RoomInjection/RoomInjectionContext.cs`

- [ ] **Step 1: Create `IRoomInjectionRule.cs`**

Create `Utils/RoomInjection/IRoomInjectionRule.cs`:

```csharp
using MegaCrit.Sts2.Core.Rooms;

namespace MapNodeChanger.Utils.RoomInjection;

public interface IRoomInjectionRule
{
    string Name { get; }

    bool TryCreateReplacement(RoomInjectionContext context, out AbstractRoom replacement);
}
```

- [ ] **Step 2: Create `RoomInjectionContext.cs`**

Create `Utils/RoomInjection/RoomInjectionContext.cs`:

```csharp
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace MapNodeChanger.Utils.RoomInjection;

public sealed class RoomInjectionContext
{
    public RoomInjectionContext(
        RunState runState,
        MapPointType mapPointType,
        RoomType rolledRoomType,
        AbstractModel? explicitModel,
        AbstractRoom originalRoom)
    {
        RunState = runState;
        MapPointType = mapPointType;
        RolledRoomType = rolledRoomType;
        ExplicitModel = explicitModel;
        OriginalRoom = originalRoom;
        CurrentMapPoint = runState.CurrentMapPoint;
        CurrentActIndex = runState.CurrentActIndex;
        ActFloor = runState.ActFloor;
    }

    public RunState RunState { get; }

    public MapPointType MapPointType { get; }

    public RoomType RolledRoomType { get; }

    public AbstractModel? ExplicitModel { get; }

    public AbstractRoom OriginalRoom { get; }

    public MapPoint? CurrentMapPoint { get; }

    public int CurrentActIndex { get; }

    public int ActFloor { get; }
}
```

- [ ] **Step 3: Build to verify type names**

Run:

```powershell
dotnet build MapNodeChanger.csproj
```

Expected: build passes with the new files included automatically by the SDK project.

- [ ] **Step 4: Commit**

```powershell
git add Utils/RoomInjection/IRoomInjectionRule.cs Utils/RoomInjection/RoomInjectionContext.cs
git commit -m "feat: add room injection abstractions"
```

---

### Task 3: Add Room Injection Service

**Files:**
- Create: `Utils/RoomInjection/RoomKey.cs`
- Create: `Utils/RoomInjection/RoomInjectionService.cs`

- [ ] **Step 1: Create `RoomKey.cs`**

Create `Utils/RoomInjection/RoomKey.cs`:

```csharp
namespace MapNodeChanger.Utils.RoomInjection;

public readonly record struct RoomKey(
    int ActIndex,
    int ActFloor,
    string Coord,
    string MapPointType,
    string RolledRoomType);
```

- [ ] **Step 2: Create `RoomInjectionService.cs`**

Create `Utils/RoomInjection/RoomInjectionService.cs`:

```csharp
using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Rooms;

namespace MapNodeChanger.Utils.RoomInjection;

public sealed class RoomInjectionService
{
    private readonly List<IRoomInjectionRule> _rules = new();
    private readonly Dictionary<RoomKey, AbstractRoom?> _cache = new();
    private readonly Action<string> _log;

    public RoomInjectionService(Action<string>? log = null)
    {
        _log = log ?? (message => Log.Warn(message));
    }

    public void Register(IRoomInjectionRule rule)
    {
        _rules.Add(rule);
    }

    public void ClearForNewRun()
    {
        _cache.Clear();
    }

    public AbstractRoom Apply(RoomInjectionContext context)
    {
        var key = BuildKey(context);
        if (_cache.TryGetValue(key, out var cachedRoom))
        {
            if (cachedRoom != null)
            {
                _log($"RoomInjection: using cached replacement for {key}");
                return cachedRoom;
            }

            _log($"RoomInjection: using cached original room for {key}");
            return context.OriginalRoom;
        }

        foreach (var rule in _rules)
        {
            if (rule.TryCreateReplacement(context, out var replacement))
            {
                _cache[key] = replacement;
                _log($"RoomInjection: {rule.Name} replaced {context.RolledRoomType} at {key}");
                return replacement;
            }
        }

        _cache[key] = null;
        return context.OriginalRoom;
    }

    private static RoomKey BuildKey(RoomInjectionContext context)
    {
        var coord = context.CurrentMapPoint?.coord.ToString() ?? "no_coord";
        return new RoomKey(
            context.CurrentActIndex,
            context.ActFloor,
            coord,
            context.MapPointType.ToString(),
            context.RolledRoomType.ToString());
    }
}
```

- [ ] **Step 3: Build**

Run:

```powershell
dotnet build MapNodeChanger.csproj
```

Expected: build passes.

- [ ] **Step 4: Commit**

```powershell
git add Utils/RoomInjection/RoomKey.cs Utils/RoomInjection/RoomInjectionService.cs
git commit -m "feat: add room injection service"
```

---

### Task 4: Install Generic `CreateRoom` Patch

**Files:**
- Create: `Utils/RoomInjection/RoomInjectionInstaller.cs`

- [ ] **Step 1: Create the installer**

Create `Utils/RoomInjection/RoomInjectionInstaller.cs`:

```csharp
using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace MapNodeChanger.Utils.RoomInjection;

public static class RoomInjectionInstaller
{
    private static RoomInjectionService? _service;
    private static Func<RunState?>? _getRunState;

    public static void Install(Harmony harmony, RoomInjectionService service, Func<RunState?> getRunState)
    {
        _service = service;
        _getRunState = getRunState;

        var target = AccessTools.Method(
            typeof(RunManager),
            "CreateRoom",
            new[] { typeof(RoomType), typeof(MapPointType), typeof(AbstractModel) });
        var postfix = AccessTools.Method(typeof(RoomInjectionInstaller), nameof(Postfix));

        if (target == null || postfix == null)
        {
            Log.Warn("RoomInjection: failed to find RunManager.CreateRoom or postfix");
            return;
        }

        harmony.Patch(target, postfix: new HarmonyMethod(postfix));
        Log.Warn("RoomInjection: patched RunManager.CreateRoom");
    }

    public static void Postfix(
        RoomType roomType,
        MapPointType mapPointType,
        AbstractModel? model,
        ref AbstractRoom __result)
    {
        var service = _service;
        var runState = _getRunState?.Invoke();
        if (service == null || runState == null)
        {
            return;
        }

        var context = new RoomInjectionContext(runState, mapPointType, roomType, model, __result);
        __result = service.Apply(context);
    }
}
```

- [ ] **Step 2: Build**

Run:

```powershell
dotnet build MapNodeChanger.csproj
```

Expected: build passes.

- [ ] **Step 3: Commit**

```powershell
git add Utils/RoomInjection/RoomInjectionInstaller.cs
git commit -m "feat: install room creation injection hook"
```

---

### Task 5: Add Vakuu Config and Rule

**Files:**
- Create: `Features/Vakuu/VakuuInjectionConfig.cs`
- Create: `Features/Vakuu/VakuuInjectionRule.cs`

- [ ] **Step 1: Create config model**

Create `Features/Vakuu/VakuuInjectionConfig.cs`:

```csharp
using System.Text.Json.Serialization;

namespace MapNodeChanger.Features.Vakuu;

public sealed class VakuuInjectionConfig
{
    [JsonPropertyName("schema_version")]
    public int SchemaVersion { get; set; } = 2;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("seed")]
    public int Seed { get; set; }

    [JsonPropertyName("unknown_room_chance")]
    public double UnknownRoomChance { get; set; } = 0.66;

    [JsonPropertyName("other_room_chance")]
    public double OtherRoomChance { get; set; } = 0.066;

    [JsonPropertyName("replace_natural_ancient")]
    public bool ReplaceNaturalAncient { get; set; } = true;

    [JsonPropertyName("log_rolls")]
    public bool LogRolls { get; set; } = true;

    public VakuuInjectionConfig Normalize()
    {
        UnknownRoomChance = Clamp01(UnknownRoomChance);
        OtherRoomChance = Clamp01(OtherRoomChance);
        return this;
    }

    private static double Clamp01(double value)
    {
        if (double.IsNaN(value))
        {
            return 0;
        }

        return Math.Clamp(value, 0, 1);
    }
}
```

- [ ] **Step 2: Create Vakuu rule**

Create `Features/Vakuu/VakuuInjectionRule.cs`:

```csharp
using System;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Rooms;
using MapNodeChanger.Utils.RoomInjection;

namespace MapNodeChanger.Features.Vakuu;

public sealed class VakuuInjectionRule : IRoomInjectionRule
{
    private readonly VakuuInjectionConfig _config;
    private readonly Random _rng;

    public VakuuInjectionRule(VakuuInjectionConfig config)
    {
        _config = config.Normalize();
        _rng = config.Seed == 0 ? new Random() : new Random(config.Seed);
    }

    public string Name => "VakuuInjection";

    public bool TryCreateReplacement(RoomInjectionContext context, out AbstractRoom replacement)
    {
        replacement = context.OriginalRoom;

        if (!_config.Enabled)
        {
            return false;
        }

        if (context.ExplicitModel != null)
        {
            Log(context, 0, false, "explicit model is preserved");
            return false;
        }

        if (context.MapPointType == MapPointType.Boss || context.RolledRoomType == RoomType.Boss)
        {
            Log(context, 0, false, "boss rooms are excluded");
            return false;
        }

        if (context.MapPointType == MapPointType.Ancient && !_config.ReplaceNaturalAncient)
        {
            Log(context, 0, false, "natural ancient replacement is disabled");
            return false;
        }

        var chance = context.MapPointType == MapPointType.Unknown
            ? _config.UnknownRoomChance
            : _config.OtherRoomChance;
        var shouldReplace = _rng.NextDouble() < chance;
        Log(context, chance, shouldReplace, "rolled");

        if (!shouldReplace)
        {
            return false;
        }

        replacement = new EventRoom(ModelDb.Event<Vakuu>());
        return true;
    }

    private void Log(RoomInjectionContext context, double chance, bool replaced, string reason)
    {
        if (!_config.LogRolls)
        {
            return;
        }

        Log.Warn($"VakuuInjection: {reason}; mapPoint={context.MapPointType}; room={context.RolledRoomType}; chance={chance:P1}; replaced={replaced}");
    }
}
```

- [ ] **Step 3: Build**

Run:

```powershell
dotnet build MapNodeChanger.csproj
```

Expected: build passes.

- [ ] **Step 4: Commit**

```powershell
git add Features/Vakuu/VakuuInjectionConfig.cs Features/Vakuu/VakuuInjectionRule.cs
git commit -m "feat: add Vakuu room injection rule"
```

---

### Task 6: Replace Main Mod Bootstrap

**Files:**
- Modify: `MapNodeChanger.cs`

- [ ] **Step 1: Replace `MapNodeChanger.cs` contents**

Replace the file with:

```csharp
using System;
using System.IO;
using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Runs;
using MapNodeChanger.Features.Vakuu;
using MapNodeChanger.Utils.RoomInjection;

namespace MapNodeChanger;

[ModInitializer("ModLoaded")]
public static class MapNodeChanger
{
    private const string ModId = "MapNodeChanger";
    private const int SupportedSchemaVersion = 2;

    private static RunState? _runState;
    private static readonly RoomInjectionService RoomInjectionService = new(LogInfo);

    public static void ModLoaded()
    {
        var config = LoadConfig();
        RoomInjectionService.Register(new VakuuInjectionRule(config));

        RunManager.Instance.RunStarted += OnRunStarted;

        var harmony = new Harmony(ModId);
        RoomInjectionInstaller.Install(harmony, RoomInjectionService, () => _runState);

        LogInfo("loaded");
    }

    private static void OnRunStarted(RunState runState)
    {
        _runState = runState;
        RoomInjectionService.ClearForNewRun();
        LogInfo("run started");
    }

    private static VakuuInjectionConfig LoadConfig()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "MapNodeChangerConfig.json");
        if (!File.Exists(path))
        {
            var created = new VakuuInjectionConfig();
            SaveConfig(path, created);
            LogInfo($"created default config at {path}");
            return created;
        }

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var loaded = JsonSerializer.Deserialize<VakuuInjectionConfig>(File.ReadAllText(path), options);
            if (loaded == null || loaded.SchemaVersion != SupportedSchemaVersion)
            {
                LogInfo($"config schema is unsupported, using defaults: {loaded?.SchemaVersion}");
                return new VakuuInjectionConfig();
            }

            return loaded.Normalize();
        }
        catch (Exception ex)
        {
            LogInfo($"failed to load config, using defaults: {ex.Message}");
            return new VakuuInjectionConfig();
        }
    }

    private static void SaveConfig(string path, VakuuInjectionConfig config)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        File.WriteAllText(path, JsonSerializer.Serialize(config, options));
    }

    private static void LogInfo(string message)
    {
        Log.Warn($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {ModId}: {message}");
    }
}
```

- [ ] **Step 2: Build**

Run:

```powershell
dotnet build MapNodeChanger.csproj
```

Expected: build passes. This also verifies the previous `RunManager.Instance.State` mistake is gone.

- [ ] **Step 3: Commit**

```powershell
git add MapNodeChanger.cs
git commit -m "refactor: bootstrap reusable room injection"
```

---

### Task 7: Update Config Example

**Files:**
- Modify: `MapNodeChangerConfig.json.example`

- [ ] **Step 1: Replace config example**

Replace the file contents with:

```json
{
  "schema_version": 2,
  "enabled": true,
  "seed": 0,
  "unknown_room_chance": 0.66,
  "other_room_chance": 0.066,
  "replace_natural_ancient": true,
  "log_rolls": true
}
```

- [ ] **Step 2: Validate JSON**

Run:

```powershell
Get-Content MapNodeChangerConfig.json.example -Raw | ConvertFrom-Json | Out-Null
```

Expected: no output and exit code 0.

- [ ] **Step 3: Commit**

```powershell
git add MapNodeChangerConfig.json.example
git commit -m "docs: update Vakuu injection config example"
```

---

### Task 8: Update README

**Files:**
- Modify: `README.md`

- [ ] **Step 1: Replace behavior description**

Replace the top behavior sections with this English text to avoid the current mojibake problem:

```markdown
# MapNodeChanger

MapNodeChanger is a Slay the Spire 2 mod that injects the existing Ancient event `Vakuu` when entering rooms.

It does not modify the map graph, map node type, node icon, or path layout. Instead, it hooks room creation and may replace the room that is about to be entered.

## Default Behavior

- Boss rooms are never replaced.
- Unknown map points have a 66% chance to become a Vakuu event room.
- Every other non-boss map point has a 6.6% chance to become a Vakuu event room.
- Natural Ancient rooms are also eligible for the 6.6% roll and can become Vakuu.

## Configuration

Copy `MapNodeChangerConfig.json.example` to `MapNodeChangerConfig.json` next to the built mod DLL if you want to customize behavior.

```json
{
  "schema_version": 2,
  "enabled": true,
  "seed": 0,
  "unknown_room_chance": 0.66,
  "other_room_chance": 0.066,
  "replace_natural_ancient": true,
  "log_rolls": true
}
```

## Build

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

The packaged mod files are written to `dist\`.
```

- [ ] **Step 2: Build after docs change**

Run:

```powershell
dotnet build MapNodeChanger.csproj
```

Expected: build still passes.

- [ ] **Step 3: Commit**

```powershell
git add README.md
git commit -m "docs: describe Vakuu room injection behavior"
```

---

### Task 9: Verify Full Packaging

**Files:**
- No source changes expected.

- [ ] **Step 1: Run normal build**

Run:

```powershell
dotnet build MapNodeChanger.csproj
```

Expected:

```text
0 warning(s)
0 error(s)
```

- [ ] **Step 2: Run packaging script**

Run:

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

Expected:

```text
Built mod files in C:\Users\asus\Documents\New project\dist
```

- [ ] **Step 3: Confirm dist outputs**

Run:

```powershell
Get-ChildItem dist | Select-Object Name,Length
```

Expected files:

```text
MapNodeChanger.dll
MapNodeChanger.json
MapNodeChanger.pck
MapNodeChangerConfig.json
```

- [ ] **Step 4: Commit no-op verification marker only if files changed**

Run:

```powershell
git status -sb
```

Expected: only ignored build artifacts are present. If tracked files changed unexpectedly, inspect with `git diff` before committing.

---

### Task 10: Manual In-Game Smoke Test

**Files:**
- No source changes expected unless a bug is found.

- [ ] **Step 1: Install built mod locally**

Copy the built mod files into a game mod folder:

```powershell
$gameModDir = "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods\MapNodeChanger"
New-Item -ItemType Directory -Force -Path $gameModDir | Out-Null
Copy-Item dist\MapNodeChanger.dll $gameModDir -Force
Copy-Item dist\MapNodeChanger.json $gameModDir -Force
Copy-Item dist\MapNodeChanger.pck $gameModDir -Force
Copy-Item dist\MapNodeChangerConfig.json $gameModDir -Force
```

- [ ] **Step 2: Force high odds for a quick test**

Edit the installed `MapNodeChangerConfig.json` in the game mod folder:

```json
{
  "schema_version": 2,
  "enabled": true,
  "seed": 1,
  "unknown_room_chance": 1.0,
  "other_room_chance": 1.0,
  "replace_natural_ancient": true,
  "log_rolls": true
}
```

- [ ] **Step 3: Launch the game and enter a non-boss room**

Expected: the room entered is the existing Ancient event `Vakuu`.

- [ ] **Step 4: Enter a boss room**

Expected: the boss room is not replaced.

- [ ] **Step 5: Restore default odds**

Replace the installed config with:

```json
{
  "schema_version": 2,
  "enabled": true,
  "seed": 0,
  "unknown_room_chance": 0.66,
  "other_room_chance": 0.066,
  "replace_natural_ancient": true,
  "log_rolls": true
}
```

---

## Self-Review

- Spec coverage:
  - Non-boss 6.6% replacement: Task 5 implements this in `VakuuInjectionRule`.
  - Unknown 66% replacement: Task 5 implements this in `VakuuInjectionRule`.
  - Boss exclusion: Task 5 checks both `MapPointType.Boss` and `RoomType.Boss`.
  - No map modification: Task 6 removes map traversal and node mutation from `MapNodeChanger.cs`.
  - Room-entry style replacement: Task 4 hooks `RunManager.CreateRoom`, which is after room type roll and before room entry.
  - Reusable utils: Tasks 2-4 create the generic `Utils/RoomInjection` layer.

- Placeholder scan:
  - No open-ended implementation placeholders are left in the task steps.

- Type consistency:
  - `IRoomInjectionRule.TryCreateReplacement` uses `RoomInjectionContext` and `AbstractRoom`.
  - `RoomInjectionService.Apply` returns `AbstractRoom`.
  - `RoomInjectionInstaller.Postfix` updates `ref AbstractRoom __result`.
  - `VakuuInjectionRule` creates `new EventRoom(ModelDb.Event<Vakuu>())`.
