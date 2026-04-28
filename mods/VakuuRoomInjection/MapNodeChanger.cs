using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Runs;
using VakuuRoomInjection.Features.Vakuu;
using Sls2Mods.Utils.AncientOptions;
using Sls2Mods.Utils.Config;
using Sls2Mods.Utils.RoomInjection;

namespace VakuuRoomInjection;

[ModInitializer("ModLoaded")]
public static class VakuuRoomInjectionMod
{
    private const string ModId = "VakuuRoomInjection";
    private const string ConfigFileName = "VakuuRoomInjectionConfig.json";
    private const int SupportedSchemaVersion = 2;

    private static RunState? _runState;
    private static readonly RoomInjectionService RoomInjectionService = new(LogInfo);
    private static readonly AncientOptionRerollService AncientOptionRerollService = new(LogInfo);

    public static void ModLoaded()
    {
        var config = ModConfigLoader.LoadOrCreate(
            ModId,
            ConfigFileName,
            SupportedSchemaVersion,
            () => new VakuuInjectionConfig(),
            item => item.Normalize(),
            LogInfo);
        VakuuRoomInjectionConfigMenu.InitializeFrom(config);
        Func<VakuuInjectionConfig> getConfig = () => config;
        var menu = new VakuuRoomInjectionConfigMenu(
            config,
            runtimeConfig => ModConfigLoader.Save(ModConfigLoader.DefaultConfigPath(ConfigFileName), runtimeConfig));
        if (ModConfigMenuRegistrar.TryRegister(ModId, menu, LogInfo))
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

    private static void LogInfo(string message)
    {
        Log.Warn($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {ModId}: {message}");
    }
}
