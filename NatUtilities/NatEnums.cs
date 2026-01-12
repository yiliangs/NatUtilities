using System;

namespace NatBase
{
    /// <summary>
    /// Enums for Atypical types, order cannot be switched.
    /// </summary>
    public enum AtypicalType
    {
        Long,
        Short,
    }
    public enum ModuleAlign
    {
        Center,
        Left,
        Right,
        Even,
    }
    public enum Role
    {
        Host,
        Client,
    }
    public enum DerivType
    {
        Bend,
        Taper,
        Twist,
    }
    public enum VertexType
    {
        Unset,
        Convex,
        Concave,
        Flat,
    }
    public enum EdgeType
    {
        Unset,
        Normal,
        Broke,
        Cut,
    }
    public enum SegType
    {
        Line,
        Arc,
        Curve,
    }
    public enum EdgeTag
    {
        Unset,
        Exterior,
        Interior,
        Demising,
        Bone,
        Spine,
    }
    public enum UnitTag
    {
        Unset,
        Studio,
        OneBed,
        TwoBed,
        ThreeBed,
        FourBed,
        Core,
    }
    public enum RoomPriority
    {
        Primary,
        Secondary,
        Inadvertant,
    }
    public enum EdgePrivilege
    {
        Unset,
        Primary,
        Secondary,
        Invalid,
    }
    public enum RoomTag
    {
        Unset,
        Foyer,
        Hallway,
        MainBedroom,
        Bedroom,
        LivingRoom,
        Bathroom,
        MainBath,
        Kitchen,
        DiningRoom,
        Balcony,
        Closet,
        Laundry,
        GuestRoom,
        PowderRoom,
        Pantry,
        WineCeller,
        Exterior,
        Corridor,
    }
    public enum EncType
    {
        WhiteBox,
        Boundary,
        Corridor,
    }
    public enum MsgType
    {
        LogOpen,
        LogClose,
        LogSeal,
        Start,
        Success,
        Abort,
        Error,
        Info,
    }
    [Flags]
    public enum Axis
    {
        None = 0,
        X = 1,
        Y = 2,
        Z = 4,
        XY = X | Y,
        XZ = X | Z,
        YZ = Y | Z,
        All = X | Y | Z
    }
    public enum Bound
    {
        Xmin,
        Xmax,
        Ymin,
        Ymax,
        Zmin,
        Zmax,
    }
}
