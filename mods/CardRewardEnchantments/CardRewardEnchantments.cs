using System;
using CardRewardEnchantments.Features.CardRewards;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using Sls2Mods.Utils.Config;

namespace CardRewardEnchantments;

[ModInitializer("ModLoaded")]
public static class CardRewardEnchantments
{
    private const string ModId = "CardRewardEnchantments";
    private const string ConfigFileName = "CardRewardEnchantmentsConfig.json";
    private const int SupportedSchemaVersion = 1;

    public static void ModLoaded()
    {
        var config = ModConfigLoader.LoadOrCreate(
            ModId,
            ConfigFileName,
            SupportedSchemaVersion,
            () => new CardRewardEnchantConfig(),
            item => item.Normalize(),
            LogInfo);

        var catalog = EnchantmentKeywordCatalog.Create(LogInfo);
        CardRewardEnchantConfigMenu.InitializeFrom(config, catalog.Keywords);
        Func<CardRewardEnchantConfig> getConfig = () => config;
        var menu = new CardRewardEnchantConfigMenu(
            catalog.Keywords,
            config,
            runtimeConfig => ModConfigLoader.Save(ModConfigLoader.DefaultConfigPath(ConfigFileName), runtimeConfig));
        if (ModConfigMenuRegistrar.TryRegister(ModId, menu, LogInfo))
        {
            getConfig = () => CardRewardEnchantConfigMenu.ToRuntimeConfig(config);
        }

        var adapter = new CardRewardAdapter();
        var service = new CardRewardEnchantService(getConfig, catalog, adapter, LogInfo);
        CardRewardEnchantInstaller.Install(new Harmony(ModId), service, adapter, LogInfo);

        LogInfo("loaded");
    }

    private static void LogInfo(string message)
    {
        Log.Warn($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {ModId}: {message}");
    }
}
