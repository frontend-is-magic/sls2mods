using MegaCrit.Sts2.Core.Rooms;

namespace MapNodeChanger.Utils.RoomInjection;

public interface IRoomInjectionRule
{
    string Name { get; }

    bool TryCreateReplacement(RoomInjectionContext context, out AbstractRoom replacement);
}
