using System;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Models.Events;
using Sls2Mods.Utils.AncientOptions;
using Sls2Mods.Utils.RoomInjection;
using VakuuEvent = MegaCrit.Sts2.Core.Models.Events.Vakuu;

namespace MapNodeChanger.Features.Vakuu;

public sealed class VakuuInjectionRule : IRoomInjectionRule
{
    public static readonly IReadOnlyList<AncientTarget> ConcreteAncientTargets = new[]
    {
        AncientTarget.Darv,
        AncientTarget.Neow,
        AncientTarget.Nonupeipe,
        AncientTarget.Orobas,
        AncientTarget.Pael,
        AncientTarget.Tanx,
        AncientTarget.Tezcatara,
        AncientTarget.Vakuu
    };

    private readonly Func<VakuuInjectionConfig> _getConfig;
    private readonly AncientOptionRerollService _ancientOptionRerollService;
    private readonly Random _rng;

    public VakuuInjectionRule(Func<VakuuInjectionConfig> getConfig, AncientOptionRerollService ancientOptionRerollService)
    {
        _getConfig = getConfig;
        _ancientOptionRerollService = ancientOptionRerollService;
        var config = getConfig();
        _rng = config.Seed == 0 ? new Random() : new Random(config.Seed);
    }

    public string Name => "VakuuInjection";

    public bool TryCreateReplacement(RoomInjectionContext context, out AbstractRoom replacement)
    {
        replacement = context.OriginalRoom;
        var config = _getConfig();

        if (!config.Enabled)
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

        if (context.MapPointType == MapPointType.Ancient && !config.ReplaceNaturalAncient)
        {
            LogRoll(context, 0, false, "natural ancient replacement is disabled");
            return false;
        }

        var chance = context.MapPointType == MapPointType.Unknown
            ? config.UnknownRoomChance
            : config.OtherRoomChance;
        var shouldReplace = _rng.NextDouble() < chance;
        LogRoll(context, chance, shouldReplace, "rolled");

        if (!shouldReplace)
        {
            return false;
        }

        var target = ResolveAncientTarget(config.AncientTarget, _rng);
        var seedMaterial = $"{Name}|{target}|{context.RoomKey}";
        replacement = new EventRoom(CreateAncientEvent(target))
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

    public static AncientTarget ResolveAncientTarget(AncientTarget target, Random rng)
    {
        if (target != AncientTarget.Random)
        {
            return target;
        }

        return ConcreteAncientTargets[rng.Next(ConcreteAncientTargets.Count)];
    }

    private static AncientEventModel CreateAncientEvent(AncientTarget target)
    {
        return target switch
        {
            AncientTarget.Darv => ModelDb.AncientEvent<Darv>(),
            AncientTarget.Neow => ModelDb.AncientEvent<Neow>(),
            AncientTarget.Nonupeipe => ModelDb.AncientEvent<Nonupeipe>(),
            AncientTarget.Orobas => ModelDb.AncientEvent<Orobas>(),
            AncientTarget.Pael => ModelDb.AncientEvent<Pael>(),
            AncientTarget.Tanx => ModelDb.AncientEvent<Tanx>(),
            AncientTarget.Tezcatara => ModelDb.AncientEvent<Tezcatara>(),
            AncientTarget.Vakuu => ModelDb.AncientEvent<VakuuEvent>(),
            _ => ModelDb.AncientEvent<VakuuEvent>()
        };
    }

    private void LogRoll(RoomInjectionContext context, double chance, bool replaced, string reason)
    {
        if (!_getConfig().LogRolls)
        {
            return;
        }

        Log.Warn($"VakuuInjection: {reason}; mapPoint={context.MapPointType}; room={context.RolledRoomType}; chance={chance:P1}; replaced={replaced}");
    }
}
