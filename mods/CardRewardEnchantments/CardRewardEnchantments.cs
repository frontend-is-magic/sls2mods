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

        CardRewardEnchantConfigMenu.InitializeFrom(config);
        Func<CardRewardEnchantConfig> getConfig = () => config;
        if (ModConfigMenuRegistrar.TryRegister(ModId, new CardRewardEnchantConfigMenu(), LogInfo))
        {
            getConfig = () => CardRewardEnchantConfigMenu.ToRuntimeConfig(config);
        }

        var catalog = EnchantmentKeywordCatalog.Create(LogInfo);
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
