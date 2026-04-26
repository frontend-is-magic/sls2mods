using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;

namespace MapNodeChanger.Utils.AncientOptions;

public static class AncientOptionRerollInstaller
{
    public static void Install(Harmony harmony, AncientOptionRerollService service)
    {
        Patch.Service = service;

        var target = AccessTools.Method(
            typeof(AncientEventModel),
            "GenerateInitialOptionsWrapper");
        if (target == null)
        {
            throw new MissingMethodException(typeof(AncientEventModel).FullName, "GenerateInitialOptionsWrapper");
        }

        harmony.Patch(
            target,
            prefix: new HarmonyMethod(typeof(Patch), nameof(Patch.Prefix)),
            postfix: new HarmonyMethod(typeof(Patch), nameof(Patch.Postfix)));
    }

    private static class Patch
    {
        public static AncientOptionRerollService? Service { get; set; }

        public static void Prefix(AncientEventModel __instance, ref Rng? __state)
        {
            __state = null;
            if (Service == null || !Service.TryConsume(__instance, out var rerollRng))
            {
                return;
            }

            __state = __instance.Rng;
            SetRng(__instance, rerollRng);
        }

        public static void Postfix(AncientEventModel __instance, Rng? __state)
        {
            if (__state != null)
            {
                SetRng(__instance, __state);
            }
        }

        private static void SetRng(EventModel eventModel, Rng rng)
        {
            var setter = AccessTools.PropertySetter(typeof(EventModel), nameof(EventModel.Rng));
            if (setter == null)
            {
                throw new MissingMethodException(typeof(EventModel).FullName, $"set_{nameof(EventModel.Rng)}");
            }

            setter.Invoke(eventModel, new object[] { rng });
        }
    }
}
