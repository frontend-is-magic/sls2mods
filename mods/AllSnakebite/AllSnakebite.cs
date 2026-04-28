using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.ValueProps;
using Sls2Mods.Utils.CardText;
using Sls2Mods.Utils.Config;

namespace AllSnakebite;

[ModInitializer("ModLoaded")]
public static class AllSnakebite
{
    private const string ModId = "AllSnakebite";
    private const string ConfigFileName = "AllSnakebiteConfig.json";
    private const int SupportedSchemaVersion = AllSnakebiteConfig.CurrentSchemaVersion;
    private const string SnakebiteSuffix = "\u86c7\u54ac";
    private const string SnakebiteDescriptionHint = "\u86c7\u54ac\uff1a\u653b\u51fb\u4f24\u5bb3\u6539\u4e3a\u7ed9\u4e88\u7b49\u91cf\u4e2d\u6bd2\u3002";
    private static Func<AllSnakebiteConfig> GetRuntimeConfig = () => new AllSnakebiteConfig();

    public static void ModLoaded()
    {
        var config = ModConfigLoader.LoadOrCreate(
            ModId,
            ConfigFileName,
            SupportedSchemaVersion,
            () => new AllSnakebiteConfig(),
            item => item.Normalize(),
            LogInfo);

        AllSnakebiteConfigMenu.InitializeFrom(config);
        GetRuntimeConfig = () => config;
        var menu = new AllSnakebiteConfigMenu(
            config,
            runtimeConfig => ModConfigLoader.Save(ModConfigLoader.DefaultConfigPath(ConfigFileName), runtimeConfig));
        if (ModConfigMenuRegistrar.TryRegister(ModId, menu, LogInfo))
        {
            GetRuntimeConfig = () => AllSnakebiteConfigMenu.ToRuntimeConfig(config);
        }

        var harmony = new Harmony(ModId);
        PatchCardKeywords(harmony);
        PatchCardTitle(harmony);
        PatchCardDescriptions(harmony);
        PatchCardDamage(harmony);
        LogInfo("loaded");
    }

    private static void PatchCardKeywords(Harmony harmony)
    {
        var target = AccessTools.PropertyGetter(typeof(CardModel), nameof(CardModel.Keywords));
        if (target == null)
        {
            throw new MissingMethodException(typeof(CardModel).FullName, nameof(CardModel.Keywords));
        }

        harmony.Patch(
            target,
            postfix: new HarmonyMethod(typeof(AllSnakebite), nameof(CardKeywordsPostfix)));
        LogInfo($"patched {target.DeclaringType?.FullName}.{target.Name}");
    }

    private static void PatchCardTitle(Harmony harmony)
    {
        var target = AccessTools.PropertyGetter(typeof(CardModel), nameof(CardModel.Title));
        if (target == null)
        {
            throw new MissingMethodException(typeof(CardModel).FullName, nameof(CardModel.Title));
        }

        harmony.Patch(
            target,
            postfix: new HarmonyMethod(typeof(AllSnakebite), nameof(CardTitlePostfix)));
        LogInfo($"patched {target.DeclaringType?.FullName}.{target.Name}");
    }

    private static void PatchCardDescriptions(Harmony harmony)
    {
        var pileDescriptionTarget = AccessTools.Method(
            typeof(CardModel),
            nameof(CardModel.GetDescriptionForPile),
            new[] { typeof(PileType), typeof(Creature) });
        if (pileDescriptionTarget == null)
        {
            throw new MissingMethodException(typeof(CardModel).FullName, nameof(CardModel.GetDescriptionForPile));
        }

        var upgradeDescriptionTarget = AccessTools.Method(
            typeof(CardModel),
            nameof(CardModel.GetDescriptionForUpgradePreview),
            Type.EmptyTypes);
        if (upgradeDescriptionTarget == null)
        {
            throw new MissingMethodException(typeof(CardModel).FullName, nameof(CardModel.GetDescriptionForUpgradePreview));
        }

        var postfix = new HarmonyMethod(typeof(AllSnakebite), nameof(CardDescriptionPostfix));
        harmony.Patch(pileDescriptionTarget, postfix: postfix);
        harmony.Patch(upgradeDescriptionTarget, postfix: postfix);
        LogInfo($"patched {pileDescriptionTarget.DeclaringType?.FullName}.{pileDescriptionTarget.Name}");
        LogInfo($"patched {upgradeDescriptionTarget.DeclaringType?.FullName}.{upgradeDescriptionTarget.Name}");
    }

    private static void PatchCardDamage(Harmony harmony)
    {
        var target = AccessTools.Method(
            typeof(CreatureCmd),
            nameof(CreatureCmd.Damage),
            new[]
            {
                typeof(PlayerChoiceContext),
                typeof(IEnumerable<Creature>),
                typeof(decimal),
                typeof(ValueProp),
                typeof(Creature),
                typeof(CardModel)
            });
        if (target == null)
        {
            throw new MissingMethodException(typeof(CreatureCmd).FullName, nameof(CreatureCmd.Damage));
        }

        harmony.Patch(
            target,
            prefix: new HarmonyMethod(typeof(AllSnakebite), nameof(CardDamagePrefix)));
        LogInfo($"patched {target.DeclaringType?.FullName}.{target.Name}");
    }

    public static void CardKeywordsPostfix(ref IReadOnlySet<CardKeyword> __result)
    {
        if (!GetRuntimeConfig().Enabled)
        {
            return;
        }

        if (__result.Contains(CardKeyword.Retain))
        {
            return;
        }

        __result = __result.Concat(new[] { CardKeyword.Retain }).ToHashSet();
    }

    public static void CardTitlePostfix(ref string __result)
    {
        if (!GetRuntimeConfig().Enabled)
        {
            return;
        }

        __result = CardTextRewrite.AppendSuffixOnce(__result, SnakebiteSuffix);
    }

    public static void CardDescriptionPostfix(CardModel __instance, ref string __result)
    {
        if (!GetRuntimeConfig().Enabled || __instance.Type != CardType.Attack)
        {
            return;
        }

        __result = CardTextRewrite.RewriteDamageAsPoison(__result, SnakebiteDescriptionHint);
    }

    public static bool CardDamagePrefix(
        PlayerChoiceContext choiceContext,
        IEnumerable<Creature> targets,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        ref Task<IEnumerable<DamageResult>> __result)
    {
        if (!GetRuntimeConfig().Enabled || cardSource == null || cardSource.Type != CardType.Attack || amount <= 0m)
        {
            return true;
        }

        __result = ApplyPoisonInsteadOfDamage(targets, amount, dealer, cardSource);
        return false;
    }

    private static async Task<IEnumerable<DamageResult>> ApplyPoisonInsteadOfDamage(
        IEnumerable<Creature> targets,
        decimal amount,
        Creature? dealer,
        CardModel cardSource)
    {
        var targetList = targets.ToList();
        foreach (var target in targetList)
        {
            await PowerCmd.Apply<PoisonPower>(target, amount, dealer, cardSource);
        }

        return Array.Empty<DamageResult>();
    }

    private static void LogInfo(string message)
    {
        Log.Warn($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {ModId}: {message}");
    }
}
