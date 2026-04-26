namespace Sls2Mods.Utils.RoomInjection;

public readonly record struct RoomKey(
    int ActIndex,
    int ActFloor,
    string Coord,
    string MapPointType,
    string RolledRoomType);
