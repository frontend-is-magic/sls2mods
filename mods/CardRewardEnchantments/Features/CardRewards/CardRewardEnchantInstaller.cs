using System;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Rewards;
using Sls2Mods.Utils.Randoming;

namespace CardRewardEnchantments.Features.CardRewards;

public static class CardRewardEnchantInstaller
{
    private static CardRewardEnchantService? _service;
    private static CardRewardAdapter? _adapter;

    public static void Install(
        Harmony harmony,
        CardRewardEnchantService service,
        CardRewardAdapter adapter,
        Action<string> log)
    {
        _service = service;
        _adapter = adapter;

        var target = AccessTools.Method(typeof(CardReward), nameof(CardReward.Populate));
        if (target == null)
        {
            throw new MissingMethodException(typeof(CardReward).FullName, nameof(CardReward.Populate));
        }

        harmony.Patch(
            target,
            postfix: new HarmonyMethod(typeof(CardRewardEnchantInstaller), nameof(Postfix)));
        log($"CardRewardEnchant: patched {target.DeclaringType?.FullName}.{target.Name}");
    }

    public static void Postfix(CardReward __instance, Task __result)
    {
        var service = _service;
        var adapter = _adapter;
        if (service == null || adapter == null)
        {
            return;
        }

        // Current STS2 CardReward.Populate builds Cards synchronously and returns
        // Task.CompletedTask. If a future build makes it asynchronous, fail closed
        // here instead of mutating a partially populated reward.
        if (!__result.IsCompletedSuccessfully)
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
}
