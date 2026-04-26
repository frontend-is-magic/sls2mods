using System;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MapNodeChanger.Utils.RoomInjection;
using VakuuEvent = MegaCrit.Sts2.Core.Models.Events.Vakuu;

namespace MapNodeChanger.Features.Vakuu;

public sealed class VakuuInjectionRule : IRoomInjectionRule
{
    private readonly VakuuInjectionConfig _config;
    private readonly Random _rng;

    public VakuuInjectionRule(VakuuInjectionConfig config)
    {
        _config = config.Normalize();
        _rng = config.Seed == 0 ? new Random() : new Random(config.Seed);
    }

    public string Name => "VakuuInjection";

    public bool TryCreateReplacement(RoomInjectionContext context, out AbstractRoom replacement)
    {
        replacement = context.OriginalRoom;

        if (!_config.Enabled)
        {
            return false;
        }

        if (context.ExplicitModel != null)
        {
            LogRoll(context, 0, false, "explicit model is preserved");
            return false;
        }

        if (context.MapPointType == MapPointType.Boss || context.RolledRoomType == RoomType.Boss)
        {
            LogRoll(context, 0, false, "boss rooms are excluded");
            return false;
        }

        if (context.MapPointType == MapPointType.Ancient && !_config.ReplaceNaturalAncient)
        {
            LogRoll(context, 0, false, "natural ancient replacement is disabled");
            return false;
        }

        var chance = context.MapPointType == MapPointType.Unknown
            ? _config.UnknownRoomChance
            : _config.OtherRoomChance;
        var shouldReplace = _rng.NextDouble() < chance;
        LogRoll(context, chance, shouldReplace, "rolled");

        if (!shouldReplace)
        {
            return false;
        }

        replacement = new EventRoom(ModelDb.Event<VakuuEvent>());
        return true;
    }

    private void LogRoll(RoomInjectionContext context, double chance, bool replaced, string reason)
    {
        if (!_config.LogRolls)
        {
            return;
        }

        Log.Warn($"VakuuInjection: {reason}; mapPoint={context.MapPointType}; room={context.RolledRoomType}; chance={chance:P1}; replaced={replaced}");
    }
}
