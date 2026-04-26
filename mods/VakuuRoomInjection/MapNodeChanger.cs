using System;
using System.IO;
using System.Text.Json;
using BaseLib.Config;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Runs;
using VakuuRoomInjection.Features.Vakuu;
using Sls2Mods.Utils.AncientOptions;
using Sls2Mods.Utils.RoomInjection;

namespace VakuuRoomInjection;

[ModInitializer("ModLoaded")]
public static class VakuuRoomInjectionMod
{
    private const string ModId = "VakuuRoomInjection";
    private const int SupportedSchemaVersion = 2;

    private static RunState? _runState;
    private static readonly RoomInjectionService RoomInjectionService = new(LogInfo);
    private static readonly AncientOptionRerollService AncientOptionRerollService = new(LogInfo);

    public static void ModLoaded()
    {
        var config = LoadConfig();
        Func<VakuuInjectionConfig> getConfig = () => config;
        if (TryRegisterConfigMenu())
        {
            getConfig = () => VakuuRoomInjectionConfigMenu.ToRuntimeConfig(config);
        }
        RoomInjectionService.Register(new VakuuInjectionRule(getConfig, AncientOptionRerollService));

        RunManager.Instance.RunStarted += OnRunStarted;

        var harmony = new Harmony(ModId);
        AncientOptionRerollInstaller.Install(harmony, AncientOptionRerollService);
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
        var path = Path.Combine(configDir, "VakuuRoomInjectionConfig.json");
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

    private static bool TryRegisterConfigMenu()
    {
        try
        {
            ModConfigRegistry.Register(ModId, new VakuuRoomInjectionConfigMenu());
            LogInfo("registered in-game config menu");
            return true;
        }
        catch (Exception ex)
        {
            LogInfo($"failed to register in-game config menu, using JSON config only: {ex.Message}");
            return false;
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
