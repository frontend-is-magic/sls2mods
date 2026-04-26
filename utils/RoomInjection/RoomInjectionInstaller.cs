using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace Sls2Mods.Utils.RoomInjection;

public static class RoomInjectionInstaller
{
    private static RoomInjectionService? _service;
    private static Func<RunState?>? _getRunState;

    public static void Install(Harmony harmony, RoomInjectionService service, Func<RunState?> getRunState)
    {
        _service = service;
        _getRunState = getRunState;

        var target = AccessTools.Method(
            typeof(RunManager),
            "CreateRoom",
            new[] { typeof(RoomType), typeof(MapPointType), typeof(AbstractModel) });
        var postfix = AccessTools.Method(typeof(RoomInjectionInstaller), nameof(Postfix));

        if (target == null || postfix == null)
        {
            Log.Warn("RoomInjection: failed to find RunManager.CreateRoom or postfix");
            return;
        }

        harmony.Patch(target, postfix: new HarmonyMethod(postfix));
        Log.Warn("RoomInjection: patched RunManager.CreateRoom");
    }

    public static void Postfix(
        RoomType roomType,
        MapPointType mapPointType,
        AbstractModel? model,
        ref AbstractRoom __result)
    {
        var service = _service;
        var runState = _getRunState?.Invoke();
        if (service == null || runState == null)
        {
            return;
        }

        var context = new RoomInjectionContext(runState, mapPointType, roomType, model, __result);
        __result = service.Apply(context);
    }
}
