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
        var configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SlayTheSpire2",
            "mod_configs");
        var path = Path.Combine(configDir, "MapNodeChangerConfig.json");
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
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
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
