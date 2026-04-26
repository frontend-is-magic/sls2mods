using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Rooms;

namespace Sls2Mods.Utils.RoomInjection;

public sealed class RoomInjectionService
{
    private readonly List<IRoomInjectionRule> _rules = new();
    private readonly Dictionary<RoomKey, AbstractRoom?> _cache = new();
    private readonly Action<string> _log;

    public RoomInjectionService(Action<string>? log = null)
    {
        _log = log ?? (message => Log.Warn(message));
    }

    public void Register(IRoomInjectionRule rule)
    {
        _rules.Add(rule);
    }

    public void ClearForNewRun()
    {
        _cache.Clear();
    }

    public AbstractRoom Apply(RoomInjectionContext context)
    {
        var key = BuildKey(context);
        if (_cache.TryGetValue(key, out var cachedRoom))
        {
            if (cachedRoom != null)
            {
                _log($"RoomInjection: using cached replacement for {key}");
                return cachedRoom;
            }

            _log($"RoomInjection: using cached original room for {key}");
            return context.OriginalRoom;
        }

        foreach (var rule in _rules)
        {
            if (rule.TryCreateReplacement(context, out var replacement))
            {
                _cache[key] = replacement;
                _log($"RoomInjection: {rule.Name} replaced {context.RolledRoomType} at {key}");
                return replacement;
            }
        }

        _cache[key] = null;
        return context.OriginalRoom;
    }

    private static RoomKey BuildKey(RoomInjectionContext context)
    {
        return context.RoomKey;
    }
}
