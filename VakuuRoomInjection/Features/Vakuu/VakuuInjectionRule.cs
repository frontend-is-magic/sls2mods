using System;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MapNodeChanger.Utils.AncientOptions;
using MapNodeChanger.Utils.RoomInjection;
using VakuuEvent = MegaCrit.Sts2.Core.Models.Events.Vakuu;

namespace MapNodeChanger.Features.Vakuu;

public sealed class VakuuInjectionRule : IRoomInjectionRule
{
    private readonly VakuuInjectionConfig _config;
    private readonly AncientOptionRerollService _ancientOptionRerollService;
    private readonly Random _rng;

    public VakuuInjectionRule(VakuuInjectionConfig config, AncientOptionRerollService ancientOptionRerollService)
    {
        _config = config.Normalize();
        _ancientOptionRerollService = ancientOptionRerollService;
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

        var seedMaterial = $"{Name}|{context.RoomKey}";
        replacement = new EventRoom(ModelDb.Event<VakuuEvent>())
        {
            OnStart = eventModel =>
            {
                if (eventModel is AncientEventModel ancientEvent)
                {
                    _ancientOptionRerollService.RequestReroll(ancientEvent, seedMaterial);
                }
            }
        };
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
