using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace MapNodeChanger.Utils.RoomInjection;

public sealed class RoomInjectionContext
{
    public RoomInjectionContext(
        RunState runState,
        MapPointType mapPointType,
        RoomType rolledRoomType,
        AbstractModel? explicitModel,
        AbstractRoom originalRoom)
    {
        RunState = runState;
        MapPointType = mapPointType;
        RolledRoomType = rolledRoomType;
        ExplicitModel = explicitModel;
        OriginalRoom = originalRoom;
        CurrentMapPoint = runState.CurrentMapPoint;
        CurrentActIndex = runState.CurrentActIndex;
        ActFloor = runState.ActFloor;
    }

    public RunState RunState { get; }

    public MapPointType MapPointType { get; }

    public RoomType RolledRoomType { get; }

    public AbstractModel? ExplicitModel { get; }

    public AbstractRoom OriginalRoom { get; }

    public MapPoint? CurrentMapPoint { get; }

    public int CurrentActIndex { get; }

    public int ActFloor { get; }
}
