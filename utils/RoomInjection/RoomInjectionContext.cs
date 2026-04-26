using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace Sls2Mods.Utils.RoomInjection;

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
        RoomKey = BuildRoomKey();
    }

    public RunState RunState { get; }

    public MapPointType MapPointType { get; }

    public RoomType RolledRoomType { get; }

    public AbstractModel? ExplicitModel { get; }

    public AbstractRoom OriginalRoom { get; }

    public MapPoint? CurrentMapPoint { get; }

    public int CurrentActIndex { get; }

    public int ActFloor { get; }

    public RoomKey RoomKey { get; }

    private RoomKey BuildRoomKey()
    {
        var coord = CurrentMapPoint?.coord.ToString() ?? "no_coord";
        return new RoomKey(
            CurrentActIndex,
            ActFloor,
            coord,
            MapPointType.ToString(),
            RolledRoomType.ToString());
    }
}
