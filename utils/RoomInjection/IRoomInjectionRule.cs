using MegaCrit.Sts2.Core.Rooms;

namespace Sls2Mods.Utils.RoomInjection;

public interface IRoomInjectionRule
{
    string Name { get; }

    bool TryCreateReplacement(RoomInjectionContext context, out AbstractRoom replacement);
}
