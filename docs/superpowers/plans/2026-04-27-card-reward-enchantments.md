# Card Reward Enchantments Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build `CardRewardEnchantments`, a Slay the Spire 2 mod that automatically adds one random allowed enchantment to eligible card rewards with a configurable default `100%` chance and a checkbox blacklist menu.

**Architecture:** Add shared `utils` helpers for config loading, BaseLib menu registration, and deterministic seed generation. Implement the new mod as a thin entry point plus focused card reward config, catalog, service, adapter, and Harmony installer classes. Resolve the exact STS2 card reward and enchantment APIs in the first task, then keep all game-specific reflection/API access isolated in `CardRewardAdapter` and `CardRewardEnchantInstaller`.

**Tech Stack:** C# 12, .NET 9, Godot.NET.Sdk 4.6.2, Harmony, BaseLib `SimpleModConfig`, STS2 `sts2.dll`, xUnit.

---

## File Structure

- Create `utils/Config/IModConfig.cs`: small interface for shared schema validation.
- Create `utils/Config/ModConfigLoader.cs`: shared JSON fallback config loader.
- Create `utils/Config/ModConfigMenuRegistrar.cs`: shared BaseLib menu registration wrapper.
- Create `utils/Randoming/DeterministicSeed.cs`: shared deterministic FNV-1a seed helper.
- Modify `utils/AncientOptions/AncientOptionRerollService.cs`: use `DeterministicSeed`.
- Create `mods/CardRewardEnchantments/CardRewardEnchantments.cs`: mod entry point and startup wiring.
- Create `mods/CardRewardEnchantments/CardRewardEnchantments.csproj`: Godot .NET mod project.
- Create `mods/CardRewardEnchantments/CardRewardEnchantments.json`: mod manifest.
- Create `mods/CardRewardEnchantments/CardRewardEnchantmentsConfig.json.example`: JSON fallback example.
- Create `mods/CardRewardEnchantments/Features/CardRewards/CardRewardEnchantConfig.cs`: runtime config.
- Create `mods/CardRewardEnchantments/Features/CardRewards/CardRewardEnchantConfigMenu.cs`: BaseLib menu model.
- Create `mods/CardRewardEnchantments/Features/CardRewards/CardRewardEnchantService.cs`: pure card reward enchantment logic.
- Create `mods/CardRewardEnchantments/Features/CardRewards/EnchantmentKeywordCatalog.cs`: dynamic keyword discovery with fallback.
- Create `mods/CardRewardEnchantments/Features/CardRewards/CardRewardAdapter.cs`: game-specific card/enchantment operations.
- Create `mods/CardRewardEnchantments/Features/CardRewards/CardRewardEnchantInstaller.cs`: Harmony patch installer.
- Create `mods/CardRewardEnchantments/build.ps1`: build and dist packaging script.
- Modify `tests/MapNodeChanger.Tests/MapNodeChanger.Tests.csproj`: reference the new mod project.
- Create `tests/MapNodeChanger.Tests/SharedUtilsTests.cs`: shared utility tests.
- Create `tests/MapNodeChanger.Tests/CardRewardEnchantConfigTests.cs`: config/menu tests.
- Create `tests/MapNodeChanger.Tests/CardRewardEnchantServiceTests.cs`: service behavior tests.
- Create `tests/MapNodeChanger.Tests/EnchantmentKeywordCatalogTests.cs`: catalog fallback tests.

## Task 1: Resolve STS2 Reward And Enchantment API

**Files:**
- Create: `docs/superpowers/research/2026-04-27-card-reward-enchantments-api.md`

- [ ] **Step 1: Inspect card reward related type names**

Run this command from the repo root:

```powershell
$asm=[Reflection.Assembly]::LoadFrom((Resolve-Path .\sts2.dll))
try { $types=$asm.GetTypes() } catch [Reflection.ReflectionTypeLoadException] { $types=$_.Exception.Types | Where-Object { $_ -ne $null } }
$types |
  Where-Object { $_.FullName -match 'Card|Reward|Enchant|Keyword|Modifier|Upgrade|Relic' } |
  Sort-Object FullName |
  Select-Object -ExpandProperty FullName
```

Expected: prints candidate type names. If output is sparse because dependencies are missing, repeat the command against `C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll` and load obvious sibling DLLs with `[Reflection.Assembly]::LoadFrom(...)` before calling `GetTypes()`.

- [ ] **Step 2: Inspect candidate methods and properties**

For each promising type from Step 1, run:

```powershell
$typeName = "MegaCrit.Sts2.Core.Nodes.Screens.CardSelection.ICardSelector"
$t = $types | Where-Object { $_.FullName -eq $typeName } | Select-Object -First 1
$t.GetMembers([Reflection.BindingFlags]'Public,NonPublic,Instance,Static,DeclaredOnly') |
  Sort-Object MemberType, Name |
  ForEach-Object { "$($_.MemberType) $($_.Name)" }
```

Expected: member names that identify the reward generation method, card collection shape, existing-enchantment detection, and enchantment application API.

- [ ] **Step 3: Write research notes**

Create `docs/superpowers/research/2026-04-27-card-reward-enchantments-api.md` with this exact structure:

```markdown
# Card Reward Enchantments API Notes

## Reward Hook Target

- Target type:
- Target method:
- Patch type: postfix
- Reward card collection expression:
- Why this target runs before the reward screen is shown:

## Card Enchantment Operations

- Card type:
- Existing enchantment check:
- Apply enchantment operation:
- Keyword/id type used by the game:

## Keyword Discovery

- Dynamic source type:
- Dynamic source member:
- Normalized keyword string format:

## Build Notes

- Additional DLLs needed for reflection:
- Any API risks:
```

Fill every bullet with the concrete names discovered in Steps 1 and 2. If a concrete API does not exist, write the reflection path that will be implemented in `CardRewardAdapter`.

- [ ] **Step 4: Commit research notes**

```powershell
git add docs/superpowers/research/2026-04-27-card-reward-enchantments-api.md
git commit -m "docs: record card reward enchantment API notes"
```

## Task 2: Add Shared Utility Tests

**Files:**
- Create: `tests/MapNodeChanger.Tests/SharedUtilsTests.cs`

- [ ] **Step 1: Write failing tests for shared utilities**

Create `tests/MapNodeChanger.Tests/SharedUtilsTests.cs`:

```csharp
using System.Text.Json.Serialization;
using BaseLib.Config;
using Sls2Mods.Utils.Config;
using Sls2Mods.Utils.Randoming;
using Xunit;

namespace MapNodeChanger.Tests;

public sealed class SharedUtilsTests
{
    [Fact]
    public void DeterministicSeedReturnsStableValue()
    {
        var first = DeterministicSeed.FromString("CardRewardEnchantments|run=123|reward=2|card=0");
        var second = DeterministicSeed.FromString("CardRewardEnchantments|run=123|reward=2|card=0");

        Assert.Equal(first, second);
        Assert.NotEqual(0u, first);
    }

    [Fact]
    public void DeterministicSeedChangesForDifferentInput()
    {
        var first = DeterministicSeed.FromString("card=0");
        var second = DeterministicSeed.FromString("card=1");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void ModConfigLoaderCreatesDefaultConfigWhenMissing()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var logs = new List<string>();

        var config = ModConfigLoader.LoadOrCreate(
            root,
            "ExampleMod",
            "ExampleConfig.json",
            supportedSchemaVersion: 3,
            createDefault: () => new ExampleConfig { Value = 7 },
            normalize: item =>
            {
                item.Value = Math.Clamp(item.Value, 0, 10);
                return item;
            },
            log: logs.Add);

        Assert.Equal(3, config.SchemaVersion);
        Assert.Equal(7, config.Value);
        Assert.True(File.Exists(Path.Combine(root, "ExampleConfig.json")));
        Assert.Contains(logs, message => message.Contains("created default config"));
    }

    [Fact]
    public void ModConfigLoaderFallsBackForUnsupportedSchema()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "ExampleConfig.json"), """{"schema_version":2,"value":99}""");

        var config = ModConfigLoader.LoadOrCreate(
            root,
            "ExampleMod",
            "ExampleConfig.json",
            supportedSchemaVersion: 3,
            createDefault: () => new ExampleConfig { Value = 5 },
            normalize: item => item,
            log: _ => { });

        Assert.Equal(3, config.SchemaVersion);
        Assert.Equal(5, config.Value);
    }

    [Fact]
    public void ModConfigLoaderFallsBackForBadJson()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "ExampleConfig.json"), "{bad json");

        var config = ModConfigLoader.LoadOrCreate(
            root,
            "ExampleMod",
            "ExampleConfig.json",
            supportedSchemaVersion: 3,
            createDefault: () => new ExampleConfig { Value = 4 },
            normalize: item => item,
            log: _ => { });

        Assert.Equal(3, config.SchemaVersion);
        Assert.Equal(4, config.Value);
    }

    private sealed class ExampleConfig : IModConfig
    {
        [JsonPropertyName("schema_version")]
        public int SchemaVersion { get; set; } = 3;

        [JsonPropertyName("value")]
        public int Value { get; set; }
    }
}
```

- [ ] **Step 2: Run tests and verify failure**

Run:

```powershell
dotnet test .\tests\MapNodeChanger.Tests\MapNodeChanger.Tests.csproj --filter SharedUtilsTests
```

Expected: build fails because `Sls2Mods.Utils.Config` and `Sls2Mods.Utils.Randoming` do not exist yet.

## Task 3: Implement Shared Utilities

**Files:**
- Create: `utils/Config/IModConfig.cs`
- Create: `utils/Config/ModConfigLoader.cs`
- Create: `utils/Config/ModConfigMenuRegistrar.cs`
- Create: `utils/Randoming/DeterministicSeed.cs`
- Modify: `utils/AncientOptions/AncientOptionRerollService.cs`

- [ ] **Step 1: Add `IModConfig`**

Create `utils/Config/IModConfig.cs`:

```csharp
namespace Sls2Mods.Utils.Config;

public interface IModConfig
{
    int SchemaVersion { get; set; }
}
```

- [ ] **Step 2: Add `ModConfigLoader`**

Create `utils/Config/ModConfigLoader.cs`:

```csharp
using System;
using System.IO;
using System.Text.Json;

namespace Sls2Mods.Utils.Config;

public static class ModConfigLoader
{
    public static string DefaultConfigRoot()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SlayTheSpire2",
            "mod_configs");
    }

    public static T LoadOrCreate<T>(
        string configRoot,
        string modId,
        string fileName,
        int supportedSchemaVersion,
        Func<T> createDefault,
        Func<T, T> normalize,
        Action<string> log)
        where T : IModConfig
    {
        var path = Path.Combine(configRoot, fileName);
        if (!File.Exists(path))
        {
            var created = createDefault();
            created.SchemaVersion = supportedSchemaVersion;
            Save(path, created);
            log($"{modId}: created default config at {path}");
            return normalize(created);
        }

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var loaded = JsonSerializer.Deserialize<T>(File.ReadAllText(path), options);
            if (loaded == null || loaded.SchemaVersion != supportedSchemaVersion)
            {
                log($"{modId}: config schema is unsupported, using defaults: {loaded?.SchemaVersion}");
                var fallback = createDefault();
                fallback.SchemaVersion = supportedSchemaVersion;
                return normalize(fallback);
            }

            return normalize(loaded);
        }
        catch (Exception ex)
        {
            log($"{modId}: failed to load config, using defaults: {ex.Message}");
            var fallback = createDefault();
            fallback.SchemaVersion = supportedSchemaVersion;
            return normalize(fallback);
        }
    }

    public static T LoadOrCreate<T>(
        string modId,
        string fileName,
        int supportedSchemaVersion,
        Func<T> createDefault,
        Func<T, T> normalize,
        Action<string> log)
        where T : IModConfig
    {
        return LoadOrCreate(DefaultConfigRoot(), modId, fileName, supportedSchemaVersion, createDefault, normalize, log);
    }

    private static void Save<T>(string path, T config)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        File.WriteAllText(path, JsonSerializer.Serialize(config, options));
    }
}
```

- [ ] **Step 3: Add `ModConfigMenuRegistrar`**

Create `utils/Config/ModConfigMenuRegistrar.cs`:

```csharp
using System;
using BaseLib.Config;

namespace Sls2Mods.Utils.Config;

public static class ModConfigMenuRegistrar
{
    public static bool TryRegister(string modId, SimpleModConfig menu, Action<string> log)
    {
        try
        {
            ModConfigRegistry.Register(modId, menu);
            log($"{modId}: registered in-game config menu");
            return true;
        }
        catch (Exception ex)
        {
            log($"{modId}: failed to register in-game config menu, using JSON config only: {ex.Message}");
            return false;
        }
    }
}
```

- [ ] **Step 4: Add `DeterministicSeed`**

Create `utils/Randoming/DeterministicSeed.cs`:

```csharp
namespace Sls2Mods.Utils.Randoming;

public static class DeterministicSeed
{
    public static uint FromString(string value)
    {
        unchecked
        {
            uint hash = 2166136261;
            foreach (var ch in value)
            {
                hash ^= ch;
                hash *= 16777619;
            }

            return hash;
        }
    }
}
```

- [ ] **Step 5: Reuse `DeterministicSeed` in Ancient reroll**

In `utils/AncientOptions/AncientOptionRerollService.cs`, add:

```csharp
using Sls2Mods.Utils.Randoming;
```

Replace:

```csharp
var seed = DeterministicSeed($"{ancientEvent.Id}|run={runSeed}|player={playerId}|{request.SeedMaterial}");
```

with:

```csharp
var seed = DeterministicSeed.FromString($"{ancientEvent.Id}|run={runSeed}|player={playerId}|{request.SeedMaterial}");
```

Remove the private `DeterministicSeed(string value)` method from the file.

- [ ] **Step 6: Run shared utility tests**

Run:

```powershell
dotnet test .\tests\MapNodeChanger.Tests\MapNodeChanger.Tests.csproj --filter SharedUtilsTests
```

Expected: tests pass.

- [ ] **Step 7: Commit shared utilities**

```powershell
git add utils/Config/IModConfig.cs utils/Config/ModConfigLoader.cs utils/Config/ModConfigMenuRegistrar.cs utils/Randoming/DeterministicSeed.cs utils/AncientOptions/AncientOptionRerollService.cs tests/MapNodeChanger.Tests/SharedUtilsTests.cs
git commit -m "feat: add shared mod utility helpers"
```

## Task 4: Scaffold CardRewardEnchantments Mod And Config

**Files:**
- Create: `mods/CardRewardEnchantments/CardRewardEnchantments.csproj`
- Create: `mods/CardRewardEnchantments/CardRewardEnchantments.json`
- Create: `mods/CardRewardEnchantments/CardRewardEnchantmentsConfig.json.example`
- Create: `mods/CardRewardEnchantments/CardRewardEnchantments.cs`
- Create: `mods/CardRewardEnchantments/Features/CardRewards/CardRewardEnchantConfig.cs`
- Create: `mods/CardRewardEnchantments/Features/CardRewards/CardRewardEnchantConfigMenu.cs`
- Modify: `tests/MapNodeChanger.Tests/MapNodeChanger.Tests.csproj`
- Create: `tests/MapNodeChanger.Tests/CardRewardEnchantConfigTests.cs`

- [ ] **Step 1: Add test project reference**

In `tests/MapNodeChanger.Tests/MapNodeChanger.Tests.csproj`, add this project reference inside the existing project reference `ItemGroup`:

```xml
<ProjectReference Include="..\..\mods\CardRewardEnchantments\CardRewardEnchantments.csproj" />
```

- [ ] **Step 2: Write failing config tests**

Create `tests/MapNodeChanger.Tests/CardRewardEnchantConfigTests.cs`:

```csharp
using CardRewardEnchantments.Features.CardRewards;
using Xunit;

namespace MapNodeChanger.Tests;

public sealed class CardRewardEnchantConfigTests
{
    [Fact]
    public void MenuDefaultsConvertToRuntimeConfig()
    {
        CardRewardEnchantConfigMenu.Enabled = true;
        CardRewardEnchantConfigMenu.EnchantChancePercent = 100.0;
        CardRewardEnchantConfigMenu.BlacklistInnate = false;
        CardRewardEnchantConfigMenu.BlacklistRetain = false;
        CardRewardEnchantConfigMenu.BlacklistEthereal = false;

        var config = CardRewardEnchantConfigMenu.ToRuntimeConfig();

        Assert.True(config.Enabled);
        Assert.Equal(1.0, config.EnchantChance);
        Assert.Empty(config.BlacklistedKeywords);
    }

    [Fact]
    public void MenuChanceClampsToProbability()
    {
        CardRewardEnchantConfigMenu.EnchantChancePercent = 150.0;
        Assert.Equal(1.0, CardRewardEnchantConfigMenu.ToRuntimeConfig().EnchantChance);

        CardRewardEnchantConfigMenu.EnchantChancePercent = -25.0;
        Assert.Equal(0.0, CardRewardEnchantConfigMenu.ToRuntimeConfig().EnchantChance);
    }

    [Fact]
    public void MenuCheckboxesConvertToBlacklist()
    {
        CardRewardEnchantConfigMenu.Enabled = true;
        CardRewardEnchantConfigMenu.EnchantChancePercent = 100.0;
        CardRewardEnchantConfigMenu.BlacklistInnate = true;
        CardRewardEnchantConfigMenu.BlacklistRetain = false;
        CardRewardEnchantConfigMenu.BlacklistEthereal = true;

        var config = CardRewardEnchantConfigMenu.ToRuntimeConfig();

        Assert.Equal(new[] { "ethereal", "innate" }, config.BlacklistedKeywords);
    }

    [Fact]
    public void RuntimeConfigNormalizeClampsAndDeduplicates()
    {
        var config = new CardRewardEnchantConfig
        {
            EnchantChance = double.NaN,
            BlacklistedKeywords = new List<string> { " Innate ", "", "innate", "retain" }
        };

        config.Normalize();

        Assert.Equal(0.0, config.EnchantChance);
        Assert.Equal(new[] { "innate", "retain" }, config.BlacklistedKeywords);
    }
}
```

- [ ] **Step 3: Run tests and verify failure**

Run:

```powershell
dotnet test .\tests\MapNodeChanger.Tests\MapNodeChanger.Tests.csproj --filter CardRewardEnchantConfigTests
```

Expected: build fails because the new mod project and config classes do not exist.

- [ ] **Step 4: Create new mod project file**

Create `mods/CardRewardEnchantments/CardRewardEnchantments.csproj`:

```xml
<Project Sdk="Godot.NET.Sdk/4.6.2">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <AssemblyName>CardRewardEnchantments</AssemblyName>
    <EnableDynamicLoading>true</EnableDynamicLoading>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <Sts2DataDir Condition="'$(Sts2DataDir)' == ''">C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64</Sts2DataDir>
    <BaseLibDir Condition="'$(BaseLibDir)' == ''">C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods\BaseLib</BaseLibDir>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include="..\..\utils\**\*.cs" LinkBase="utils" />
    <Reference Include="0Harmony">
      <HintPath>$(Sts2DataDir)\0Harmony.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="BaseLib">
      <HintPath>$(BaseLibDir)\BaseLib.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="sts2">
      <HintPath>$(Sts2DataDir)\sts2.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>
</Project>
```

- [ ] **Step 5: Create manifest**

Create `mods/CardRewardEnchantments/CardRewardEnchantments.json`:

```json
{
  "id": "CardRewardEnchantments",
  "name": "Card Reward Enchantments",
  "author": "sls2mods",
  "version": "0.1.0",
  "description": "Adds configurable random enchantments to card rewards.",
  "main": "CardRewardEnchantments.dll"
}
```

- [ ] **Step 6: Create JSON config example**

Create `mods/CardRewardEnchantments/CardRewardEnchantmentsConfig.json.example`:

```json
{
  "schema_version": 1,
  "enabled": true,
  "enchant_chance": 1.0,
  "blacklisted_keywords": [],
  "log_rolls": true
}
```

- [ ] **Step 7: Add runtime config**

Create `mods/CardRewardEnchantments/Features/CardRewards/CardRewardEnchantConfig.cs`:

```csharp
using System;
using System.Text.Json.Serialization;
using Sls2Mods.Utils.Config;

namespace CardRewardEnchantments.Features.CardRewards;

public sealed class CardRewardEnchantConfig : IModConfig
{
    [JsonPropertyName("schema_version")]
    public int SchemaVersion { get; set; } = 1;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("enchant_chance")]
    public double EnchantChance { get; set; } = 1.0;

    [JsonPropertyName("blacklisted_keywords")]
    public List<string> BlacklistedKeywords { get; set; } = new();

    [JsonPropertyName("log_rolls")]
    public bool LogRolls { get; set; } = true;

    public CardRewardEnchantConfig Normalize()
    {
        EnchantChance = Clamp01(EnchantChance);
        BlacklistedKeywords = BlacklistedKeywords
            .Select(keyword => keyword.Trim().ToLowerInvariant())
            .Where(keyword => !string.IsNullOrWhiteSpace(keyword))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(keyword => keyword, StringComparer.Ordinal)
            .ToList();
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

- [ ] **Step 8: Add config menu**

Create `mods/CardRewardEnchantments/Features/CardRewards/CardRewardEnchantConfigMenu.cs`:

```csharp
using BaseLib.Config;

namespace CardRewardEnchantments.Features.CardRewards;

public sealed class CardRewardEnchantConfigMenu : SimpleModConfig
{
    public static bool Enabled { get; set; } = true;

    [ConfigSlider(0.0, 100.0, 0.1, Format = "{0:0.0}%")]
    public static double EnchantChancePercent { get; set; } = 100.0;

    public static bool BlacklistInnate { get; set; }

    public static bool BlacklistRetain { get; set; }

    public static bool BlacklistEthereal { get; set; }

    public static CardRewardEnchantConfig ToRuntimeConfig(CardRewardEnchantConfig? fallback = null)
    {
        fallback ??= new CardRewardEnchantConfig();
        fallback.Enabled = Enabled;
        fallback.EnchantChance = PercentToProbability(EnchantChancePercent);
        fallback.BlacklistedKeywords = BuildBlacklist();
        return fallback.Normalize();
    }

    private static List<string> BuildBlacklist()
    {
        var blacklist = new List<string>();
        if (BlacklistInnate)
        {
            blacklist.Add("innate");
        }

        if (BlacklistRetain)
        {
            blacklist.Add("retain");
        }

        if (BlacklistEthereal)
        {
            blacklist.Add("ethereal");
        }

        return blacklist;
    }

    private static double PercentToProbability(double percent)
    {
        if (double.IsNaN(percent))
        {
            return 0;
        }

        return Math.Clamp(percent, 0, 100) / 100.0;
    }
}
```

- [ ] **Step 9: Run config tests**

Run:

```powershell
dotnet test .\tests\MapNodeChanger.Tests\MapNodeChanger.Tests.csproj --filter CardRewardEnchantConfigTests
```

Expected: tests pass.

## Task 5: Add Catalog, Adapter Abstractions, And Service Tests

**Files:**
- Create: `mods/CardRewardEnchantments/Features/CardRewards/EnchantmentKeywordCatalog.cs`
- Create: `mods/CardRewardEnchantments/Features/CardRewards/CardRewardAdapter.cs`
- Create: `mods/CardRewardEnchantments/Features/CardRewards/CardRewardEnchantService.cs`
- Create: `tests/MapNodeChanger.Tests/CardRewardEnchantServiceTests.cs`
- Create: `tests/MapNodeChanger.Tests/EnchantmentKeywordCatalogTests.cs`

- [ ] **Step 1: Add failing service tests**

Create `tests/MapNodeChanger.Tests/CardRewardEnchantServiceTests.cs`:

```csharp
using CardRewardEnchantments.Features.CardRewards;
using Xunit;

namespace MapNodeChanger.Tests;

public sealed class CardRewardEnchantServiceTests
{
    [Fact]
    public void ApplySkipsWhenDisabled()
    {
        var card = new TestRewardCard();
        var adapter = new TestAdapter();
        var service = CreateService(new CardRewardEnchantConfig { Enabled = false }, adapter);

        service.ApplyToRewardCards(new[] { card }, new Random(1), "reward-1");

        Assert.Empty(adapter.AppliedKeywords);
    }

    [Fact]
    public void ApplySkipsAlreadyEnchantedCards()
    {
        var card = new TestRewardCard { HasEnchantment = true };
        var adapter = new TestAdapter();
        var service = CreateService(new CardRewardEnchantConfig { EnchantChance = 1.0 }, adapter);

        service.ApplyToRewardCards(new[] { card }, new Random(1), "reward-1");

        Assert.Empty(adapter.AppliedKeywords);
    }

    [Fact]
    public void ApplyRespectsZeroChance()
    {
        var card = new TestRewardCard();
        var adapter = new TestAdapter();
        var service = CreateService(new CardRewardEnchantConfig { EnchantChance = 0.0 }, adapter);

        service.ApplyToRewardCards(new[] { card }, new Random(1), "reward-1");

        Assert.Empty(adapter.AppliedKeywords);
    }

    [Fact]
    public void ApplyUsesAllowedKeywordAtFullChance()
    {
        var card = new TestRewardCard();
        var adapter = new TestAdapter();
        var service = CreateService(new CardRewardEnchantConfig { EnchantChance = 1.0 }, adapter);

        service.ApplyToRewardCards(new[] { card }, new Random(1), "reward-1");

        Assert.Single(adapter.AppliedKeywords);
        Assert.Contains(adapter.AppliedKeywords[0], new[] { "innate", "retain", "ethereal" });
    }

    [Fact]
    public void ApplyFiltersBlacklistedKeywords()
    {
        var card = new TestRewardCard();
        var adapter = new TestAdapter();
        var config = new CardRewardEnchantConfig
        {
            EnchantChance = 1.0,
            BlacklistedKeywords = new List<string> { "innate", "retain" }
        }.Normalize();
        var service = CreateService(config, adapter);

        service.ApplyToRewardCards(new[] { card }, new Random(1), "reward-1");

        Assert.Equal("ethereal", adapter.AppliedKeywords.Single());
    }

    [Fact]
    public void ApplySkipsWhenBlacklistRemovesEveryKeyword()
    {
        var card = new TestRewardCard();
        var adapter = new TestAdapter();
        var config = new CardRewardEnchantConfig
        {
            EnchantChance = 1.0,
            BlacklistedKeywords = new List<string> { "innate", "retain", "ethereal" }
        }.Normalize();
        var service = CreateService(config, adapter);

        service.ApplyToRewardCards(new[] { card }, new Random(1), "reward-1");

        Assert.Empty(adapter.AppliedKeywords);
    }

    private static CardRewardEnchantService CreateService(CardRewardEnchantConfig config, ICardRewardEnchantAdapter adapter)
    {
        var catalog = EnchantmentKeywordCatalog.FromKeywords(new[] { "innate", "retain", "ethereal" }, _ => { });
        return new CardRewardEnchantService(() => config.Normalize(), catalog, adapter, _ => { });
    }

    private sealed class TestRewardCard
    {
        public bool HasEnchantment { get; set; }
    }

    private sealed class TestAdapter : ICardRewardEnchantAdapter
    {
        public List<string> AppliedKeywords { get; } = new();

        public bool HasEnchantment(object card)
        {
            return ((TestRewardCard)card).HasEnchantment;
        }

        public bool TryApplyEnchantment(object card, string keyword, out string failureReason)
        {
            failureReason = string.Empty;
            AppliedKeywords.Add(keyword);
            ((TestRewardCard)card).HasEnchantment = true;
            return true;
        }
    }
}
```

- [ ] **Step 2: Add failing catalog tests**

Create `tests/MapNodeChanger.Tests/EnchantmentKeywordCatalogTests.cs`:

```csharp
using CardRewardEnchantments.Features.CardRewards;
using Xunit;

namespace MapNodeChanger.Tests;

public sealed class EnchantmentKeywordCatalogTests
{
    [Fact]
    public void FromKeywordsNormalizesAndSortsKeywords()
    {
        var catalog = EnchantmentKeywordCatalog.FromKeywords(new[] { " Retain ", "", "innate", "retain" }, _ => { });

        Assert.Equal(new[] { "innate", "retain" }, catalog.Keywords);
    }

    [Fact]
    public void FromKeywordsFallsBackWhenEmpty()
    {
        var catalog = EnchantmentKeywordCatalog.FromKeywords(Array.Empty<string>(), _ => { });

        Assert.Contains("innate", catalog.Keywords);
        Assert.Contains("retain", catalog.Keywords);
        Assert.Contains("ethereal", catalog.Keywords);
    }
}
```

- [ ] **Step 3: Run tests and verify failure**

Run:

```powershell
dotnet test .\tests\MapNodeChanger.Tests\MapNodeChanger.Tests.csproj --filter "CardRewardEnchantServiceTests|EnchantmentKeywordCatalogTests"
```

Expected: build fails because service, catalog, and adapter interfaces do not exist.

- [ ] **Step 4: Add keyword catalog**

Create `mods/CardRewardEnchantments/Features/CardRewards/EnchantmentKeywordCatalog.cs`:

```csharp
using System;
using System.Reflection;

namespace CardRewardEnchantments.Features.CardRewards;

public sealed class EnchantmentKeywordCatalog
{
    private static readonly string[] FallbackKeywords = { "ethereal", "innate", "retain" };

    private EnchantmentKeywordCatalog(IReadOnlyList<string> keywords)
    {
        Keywords = keywords;
    }

    public IReadOnlyList<string> Keywords { get; }

    public static EnchantmentKeywordCatalog Create(Action<string> log)
    {
        try
        {
            var discovered = DiscoverKeywords();
            if (discovered.Count > 0)
            {
                log($"EnchantmentKeywordCatalog: using {discovered.Count} dynamically discovered keywords");
                return new EnchantmentKeywordCatalog(discovered);
            }
        }
        catch (Exception ex)
        {
            log($"EnchantmentKeywordCatalog: dynamic discovery failed, using fallback keywords: {ex.Message}");
        }

        return FromKeywords(FallbackKeywords, log);
    }

    public static EnchantmentKeywordCatalog FromKeywords(IEnumerable<string> keywords, Action<string> log)
    {
        var normalized = Normalize(keywords);
        if (normalized.Count == 0)
        {
            normalized = Normalize(FallbackKeywords);
            log($"EnchantmentKeywordCatalog: using {normalized.Count} fallback keywords");
        }

        return new EnchantmentKeywordCatalog(normalized);
    }

    private static List<string> DiscoverKeywords()
    {
        var assembly = typeof(MegaCrit.Sts2.Core.Models.AbstractModel).Assembly;
        var keywordLikeTypes = assembly.GetTypes()
            .Where(type => type.FullName != null && type.FullName.Contains("Enchant", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var keywords = new List<string>();
        foreach (var type in keywordLikeTypes)
        {
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.FieldType == typeof(string) && field.GetValue(null) is string value)
                {
                    keywords.Add(value);
                }
            }

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                if (property.PropertyType == typeof(string) && property.GetValue(null) is string value)
                {
                    keywords.Add(value);
                }
            }
        }

        return Normalize(keywords);
    }

    private static List<string> Normalize(IEnumerable<string> keywords)
    {
        return keywords
            .Select(keyword => keyword.Trim().ToLowerInvariant())
            .Where(keyword => !string.IsNullOrWhiteSpace(keyword))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(keyword => keyword, StringComparer.Ordinal)
            .ToList();
    }
}
```

- [ ] **Step 5: Add adapter interface and initial reflection game adapter**

Create `mods/CardRewardEnchantments/Features/CardRewards/CardRewardAdapter.cs`:

```csharp
using System;

namespace CardRewardEnchantments.Features.CardRewards;

public interface ICardRewardEnchantAdapter
{
    bool HasEnchantment(object card);

    bool TryApplyEnchantment(object card, string keyword, out string failureReason);
}

public sealed class CardRewardAdapter : ICardRewardEnchantAdapter
{
    private readonly Action<string> _log;

    public CardRewardAdapter(Action<string> log)
    {
        _log = log;
    }

    public bool HasEnchantment(object card)
    {
        var type = card.GetType();
        var property = type.GetProperty("Enchantments") ?? type.GetProperty("EnchantmentsData");
        if (property?.GetValue(card) is System.Collections.ICollection collection)
        {
            return collection.Count > 0;
        }

        var singleProperty = type.GetProperty("Enchantment") ?? type.GetProperty("Enchant");
        return singleProperty?.GetValue(card) != null;
    }

    public bool TryApplyEnchantment(object card, string keyword, out string failureReason)
    {
        var type = card.GetType();
        var method = type.GetMethod("AddEnchantment") ?? type.GetMethod("ApplyEnchantment");
        if (method == null)
        {
            failureReason = $"no supported enchantment method on {type.FullName}";
            _log($"CardRewardAdapter: {failureReason}");
            return false;
        }

        try
        {
            method.Invoke(card, new object[] { keyword });
            failureReason = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            failureReason = ex.InnerException?.Message ?? ex.Message;
            return false;
        }
    }
}
```

After Task 1 identifies concrete APIs, replace the reflection guesses with the concrete operations recorded in the research notes.

- [ ] **Step 6: Add service**

Create `mods/CardRewardEnchantments/Features/CardRewards/CardRewardEnchantService.cs`:

```csharp
using System;

namespace CardRewardEnchantments.Features.CardRewards;

public sealed class CardRewardEnchantService
{
    private readonly Func<CardRewardEnchantConfig> _getConfig;
    private readonly EnchantmentKeywordCatalog _catalog;
    private readonly ICardRewardEnchantAdapter _adapter;
    private readonly Action<string> _log;

    public CardRewardEnchantService(
        Func<CardRewardEnchantConfig> getConfig,
        EnchantmentKeywordCatalog catalog,
        ICardRewardEnchantAdapter adapter,
        Action<string> log)
    {
        _getConfig = getConfig;
        _catalog = catalog;
        _adapter = adapter;
        _log = log;
    }

    public void ApplyToRewardCards(IEnumerable<object> cards, Random rng, string rewardKey)
    {
        var config = _getConfig().Normalize();
        if (!config.Enabled)
        {
            return;
        }

        var candidates = _catalog.Keywords
            .Where(keyword => !config.BlacklistedKeywords.Contains(keyword, StringComparer.OrdinalIgnoreCase))
            .ToList();
        if (candidates.Count == 0)
        {
            if (config.LogRolls)
            {
                _log($"CardRewardEnchant: no allowed enchantment keywords for {rewardKey}");
            }

            return;
        }

        var index = 0;
        foreach (var card in cards)
        {
            if (_adapter.HasEnchantment(card))
            {
                index++;
                continue;
            }

            var roll = rng.NextDouble();
            if (roll >= config.EnchantChance)
            {
                index++;
                continue;
            }

            var keyword = candidates[rng.Next(candidates.Count)];
            if (!_adapter.TryApplyEnchantment(card, keyword, out var failureReason) && config.LogRolls)
            {
                _log($"CardRewardEnchant: failed to apply {keyword} to {rewardKey} cardIndex={index}: {failureReason}");
            }

            index++;
        }
    }
}
```

- [ ] **Step 7: Run config, catalog, and service tests**

Run:

```powershell
dotnet test .\tests\MapNodeChanger.Tests\MapNodeChanger.Tests.csproj --filter "CardRewardEnchantConfigTests|CardRewardEnchantServiceTests|EnchantmentKeywordCatalogTests"
```

Expected: tests pass.

- [ ] **Step 8: Commit config and pure behavior**

```powershell
git add mods/CardRewardEnchantments tests/MapNodeChanger.Tests/MapNodeChanger.Tests.csproj tests/MapNodeChanger.Tests/CardRewardEnchantConfigTests.cs tests/MapNodeChanger.Tests/CardRewardEnchantServiceTests.cs tests/MapNodeChanger.Tests/EnchantmentKeywordCatalogTests.cs
git commit -m "feat: add card reward enchantment config and service"
```

## Task 6: Implement Harmony Installer And Real Adapter API

**Files:**
- Create: `mods/CardRewardEnchantments/CardRewardEnchantments.cs`
- Create: `mods/CardRewardEnchantments/Features/CardRewards/CardRewardEnchantInstaller.cs`
- Modify: `mods/CardRewardEnchantments/Features/CardRewards/CardRewardAdapter.cs`
- Modify: `docs/superpowers/research/2026-04-27-card-reward-enchantments-api.md`

- [ ] **Step 1: Update adapter with concrete API from research notes**

Open `docs/superpowers/research/2026-04-27-card-reward-enchantments-api.md`. Replace the reflection guesses in `CardRewardAdapter.HasEnchantment()` and `CardRewardAdapter.TryApplyEnchantment()` with the exact operations recorded under `Card Enchantment Operations`.

The resulting methods must keep this shape:

```csharp
public bool HasEnchantment(object card)
{
    // Use the concrete existing-enchantment check from the research notes.
}

public bool TryApplyEnchantment(object card, string keyword, out string failureReason)
{
    try
    {
        // Use the concrete apply operation from the research notes.
        failureReason = string.Empty;
        return true;
    }
    catch (Exception ex)
    {
        failureReason = ex.InnerException?.Message ?? ex.Message;
        return false;
    }
}
```

If the concrete API requires reflection because the public types are unavailable at compile time, keep the reflection localized in this adapter and include exact member names from the research notes.

- [ ] **Step 2: Add installer using the researched hook target**

Create `mods/CardRewardEnchantments/Features/CardRewards/CardRewardEnchantInstaller.cs`. The resolver starts with a conservative reward/card name heuristic so the file builds before concrete API tuning. After Task 1 identifies the exact method, tighten `IsRewardTargetCandidate()` to match the researched type and method names.

```csharp
using System;
using HarmonyLib;
using Sls2Mods.Utils.Randoming;

namespace CardRewardEnchantments.Features.CardRewards;

public static class CardRewardEnchantInstaller
{
    private static CardRewardEnchantService? _service;
    private static CardRewardAdapter? _adapter;
    private static Action<string>? _log;

    public static void Install(Harmony harmony, CardRewardEnchantService service, CardRewardAdapter adapter, Action<string> log)
    {
        _service = service;
        _adapter = adapter;
        _log = log;

        var target = ResolveRewardTarget();
        var postfix = AccessTools.Method(typeof(CardRewardEnchantInstaller), nameof(Postfix));
        if (target == null || postfix == null)
        {
            log("CardRewardEnchant: failed to find card reward hook target or postfix");
            return;
        }

        harmony.Patch(target, postfix: new HarmonyMethod(postfix));
        log($"CardRewardEnchant: patched {target.DeclaringType?.FullName}.{target.Name}");
    }

    public static void Postfix(object __instance, object? __result)
    {
        var service = _service;
        var adapter = _adapter;
        if (service == null || adapter == null)
        {
            return;
        }

        var cards = adapter.ExtractRewardCards(__instance, __result).ToList();
        if (cards.Count == 0)
        {
            return;
        }

        var rewardKey = adapter.BuildRewardKey(__instance, __result);
        var seed = DeterministicSeed.FromString($"CardRewardEnchantments|{rewardKey}");
        service.ApplyToRewardCards(cards, new Random(unchecked((int)seed)), rewardKey);
    }

    private static System.Reflection.MethodInfo? ResolveRewardTarget()
    {
        var assembly = typeof(MegaCrit.Sts2.Core.Models.AbstractModel).Assembly;
        return assembly.GetTypes()
            .SelectMany(type => type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static))
            .FirstOrDefault(IsRewardTargetCandidate);
    }

    private static bool IsRewardTargetCandidate(System.Reflection.MethodInfo method)
    {
        var methodName = method.Name.ToLowerInvariant();
        var typeName = method.DeclaringType?.FullName?.ToLowerInvariant() ?? string.Empty;
        var combined = $"{typeName}.{methodName}";
        return combined.Contains("card")
            && combined.Contains("reward")
            && !method.ContainsGenericParameters;
    }
}
```

After the first build succeeds, use the Task 1 notes to make `IsRewardTargetCandidate()` stricter. For example, if Task 1 identifies a single target method, add exact equality checks for `method.DeclaringType?.FullName` and `method.Name`.

- [ ] **Step 3: Add reward extraction helpers to adapter**

Extend `CardRewardAdapter` with these methods, using the concrete reward card collection expression from Task 1:

```csharp
public IEnumerable<object> ExtractRewardCards(object instance, object? result)
{
    // Use the reward card collection expression recorded in the research notes.
}

public string BuildRewardKey(object instance, object? result)
{
    var instancePart = instance.GetHashCode().ToString("X");
    var resultPart = result?.GetHashCode().ToString("X") ?? "void";
    return $"{instance.GetType().FullName}|instance={instancePart}|result={resultPart}";
}
```

If the reward method result is directly enumerable, implement:

```csharp
public IEnumerable<object> ExtractRewardCards(object instance, object? result)
{
    if (result is System.Collections.IEnumerable enumerable)
    {
        foreach (var item in enumerable)
        {
            if (item != null)
            {
                yield return item;
            }
        }
    }
}
```

If the reward cards live on `__instance`, use the exact property or field from the research notes.

- [ ] **Step 4: Build the new mod**

Create `mods/CardRewardEnchantments/CardRewardEnchantments.cs`:

```csharp
using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using CardRewardEnchantments.Features.CardRewards;
using Sls2Mods.Utils.Config;

namespace CardRewardEnchantments;

[ModInitializer("ModLoaded")]
public static class CardRewardEnchantments
{
    private const string ModId = "CardRewardEnchantments";
    private const int SupportedSchemaVersion = 1;

    public static void ModLoaded()
    {
        var config = ModConfigLoader.LoadOrCreate(
            ModId,
            "CardRewardEnchantmentsConfig.json",
            SupportedSchemaVersion,
            () => new CardRewardEnchantConfig(),
            item => item.Normalize(),
            LogInfo);

        Func<CardRewardEnchantConfig> getConfig = () => config;
        if (ModConfigMenuRegistrar.TryRegister(ModId, new CardRewardEnchantConfigMenu(), LogInfo))
        {
            getConfig = () => CardRewardEnchantConfigMenu.ToRuntimeConfig(config);
        }

        var catalog = EnchantmentKeywordCatalog.Create(LogInfo);
        var adapter = new CardRewardAdapter(LogInfo);
        var service = new CardRewardEnchantService(getConfig, catalog, adapter, LogInfo);
        CardRewardEnchantInstaller.Install(new Harmony(ModId), service, adapter, LogInfo);

        LogInfo("loaded");
    }

    private static void LogInfo(string message)
    {
        Log.Warn($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {ModId}: {message}");
    }
}
```

Run:

```powershell
dotnet build .\mods\CardRewardEnchantments\CardRewardEnchantments.csproj
```

Expected: 0 errors. If compile errors refer to the researched API, update only `CardRewardAdapter` or `CardRewardEnchantInstaller`; do not move game-specific code into the service.

- [ ] **Step 5: Run all tests**

Run:

```powershell
dotnet test .\tests\MapNodeChanger.Tests\MapNodeChanger.Tests.csproj
```

Expected: all tests pass.

- [ ] **Step 6: Commit installer and adapter**

```powershell
git add mods/CardRewardEnchantments/CardRewardEnchantments.cs mods/CardRewardEnchantments/Features/CardRewards/CardRewardEnchantInstaller.cs mods/CardRewardEnchantments/Features/CardRewards/CardRewardAdapter.cs docs/superpowers/research/2026-04-27-card-reward-enchantments-api.md
git commit -m "feat: hook card reward enchantments into rewards"
```

## Task 7: Add Build Script And Packaging

**Files:**
- Create: `mods/CardRewardEnchantments/build.ps1`
- Modify: `README.md`

- [ ] **Step 1: Add build script**

Create `mods/CardRewardEnchantments/build.ps1`:

```powershell
param(
    [string]$GameDir = "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2",
    [string]$Godot = "godot"
)

$ErrorActionPreference = "Stop"

$previousLocation = Get-Location
Set-Location $PSScriptRoot
try {
$stsDll = Join-Path $GameDir "data_sts2_windows_x86_64\sts2.dll"
if (-not (Test-Path $stsDll)) {
    throw "sts2.dll not found at $stsDll"
}

Copy-Item $stsDll -Destination (Join-Path $PSScriptRoot "sts2.dll") -Force

dotnet build (Join-Path $PSScriptRoot "CardRewardEnchantments.csproj")
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed."
}

$modId = "CardRewardEnchantments"
$dll = Join-Path $PSScriptRoot ".godot\mono\temp\bin\Debug\$modId.dll"
if (-not (Test-Path $dll)) {
    throw "Built DLL not found at $dll"
}

$dist = Join-Path $PSScriptRoot "dist"
New-Item -ItemType Directory -Force -Path $dist | Out-Null
Remove-Item -Path (Join-Path $dist "$modId.pck") -Force -ErrorAction SilentlyContinue
Remove-Item -Path (Join-Path $dist "$($modId)Config.json") -Force -ErrorAction SilentlyContinue
Copy-Item $dll -Destination (Join-Path $dist "$modId.dll") -Force
Copy-Item (Join-Path $PSScriptRoot "$modId.json") -Destination (Join-Path $dist "$modId.json") -Force

Write-Host "Built mod files in $dist"
}
finally {
    Set-Location $previousLocation
}
```

- [ ] **Step 2: Document the new mod in README**

Add this bullet to the current mod list in `README.md`:

```markdown
- `mods\CardRewardEnchantments`
```

Add this short description near the existing mod descriptions:

```markdown
`CardRewardEnchantments` adds a configurable chance for generated card rewards to receive one random allowed enchantment. Its in-game menu exposes an enable toggle, enchantment chance, and checkbox-style keyword blacklist.
```

- [ ] **Step 3: Build package**

Run:

```powershell
powershell -ExecutionPolicy Bypass -File .\mods\CardRewardEnchantments\build.ps1
```

Expected: `mods\CardRewardEnchantments\dist\CardRewardEnchantments.dll` and `mods\CardRewardEnchantments\dist\CardRewardEnchantments.json` exist.

- [ ] **Step 4: Confirm dist contents**

Run:

```powershell
Get-ChildItem .\mods\CardRewardEnchantments\dist | Select-Object -ExpandProperty Name
```

Expected:

```text
CardRewardEnchantments.dll
CardRewardEnchantments.json
```

- [ ] **Step 5: Commit packaging**

```powershell
git add mods/CardRewardEnchantments/build.ps1 README.md
git commit -m "chore: package card reward enchantments mod"
```

## Task 8: Final Verification

**Files:**
- No code changes expected.

- [ ] **Step 1: Run all automated tests**

Run:

```powershell
dotnet test .\tests\MapNodeChanger.Tests\MapNodeChanger.Tests.csproj
```

Expected: all tests pass.

- [ ] **Step 2: Build existing mod**

Run:

```powershell
powershell -ExecutionPolicy Bypass -File .\mods\VakuuRoomInjection\build.ps1
```

Expected: build succeeds and existing `VakuuRoomInjection` dist files are regenerated.

- [ ] **Step 3: Build new mod**

Run:

```powershell
powershell -ExecutionPolicy Bypass -File .\mods\CardRewardEnchantments\build.ps1
```

Expected: build succeeds and new dist contains only `CardRewardEnchantments.dll` and `CardRewardEnchantments.json`.

- [ ] **Step 4: Check git status**

Run:

```powershell
git status --short
```

Expected: no uncommitted source changes. Build output changes may appear only if generated dist artifacts are tracked; commit them only if this repository already tracks comparable dist artifacts for other mods.

- [ ] **Step 5: Prepare manual smoke checklist**

Add this checklist to the final implementation summary:

```markdown
Manual smoke checklist:
- Start Slay the Spire 2 with BaseLib and CardRewardEnchantments installed.
- Confirm the mod menu shows enable, chance, and keyword blacklist controls.
- Start a run with chance set to 100%.
- Open a card reward screen and confirm eligible reward cards show one enchantment.
- Blacklist one visible keyword, open another reward screen, and confirm automatic enchantments avoid it.
- Set chance to 0%, open another reward screen, and confirm no new automatic enchantments are added.
```
