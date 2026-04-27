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
        try
        {
            var service = _service;
            var adapter = _adapter;
            if (service == null || adapter == null)
            {
                return;
            }

            if (!__result.IsCompletedSuccessfully)
            {
                _ = __result.ContinueWith(
                    task => ApplyAfterPopulate(__instance, task, service, adapter),
                    TaskScheduler.Default);
                return;
            }

            ApplyAfterPopulate(__instance, __result, service, adapter);
        }
        catch
        {
            return;
        }
    }

    private static void ApplyAfterPopulate(
        CardReward reward,
        Task populateTask,
        CardRewardEnchantService service,
        CardRewardAdapter adapter)
    {
        try
        {
            if (!populateTask.IsCompletedSuccessfully)
            {
                return;
            }

            Apply(reward, populateTask, service, adapter);
        }
        catch
        {
            return;
        }
    }

    private static void Apply(
        CardReward reward,
        Task populateTask,
        CardRewardEnchantService service,
        CardRewardAdapter adapter)
    {
        var cards = adapter.ExtractRewardCards(reward, populateTask).ToList();
        if (cards.Count == 0)
        {
            return;
        }

        var rewardKey = adapter.BuildRewardKey(cards);
        var seed = DeterministicSeed.FromString($"CardRewardEnchantments|{rewardKey}");
        service.ApplyToRewardCards(cards, new Random(unchecked((int)seed)), rewardKey);
    }
}
