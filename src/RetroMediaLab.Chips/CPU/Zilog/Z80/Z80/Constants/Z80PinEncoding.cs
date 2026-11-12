namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Constants;

/// <summary>Représentation logicielle des sorties, distincte des numéros du boîtier.</summary>
internal static class Z80PinEncoding
{
    internal const int DataShift = 16, ControlShift = 22, DrivenShift = 32;
    internal const ulong AddressMask = 0xffff, DataMask = 0xffUL << DataShift;
    internal const ulong AddressDriven = 1UL << 32, DataDriven = 1UL << 33;
    internal const ulong MemoryHigh = 1UL << 24, IoHigh = 1UL << 25, ReadHigh = 1UL << 26, WriteHigh = 1UL << 27;
    internal const ulong MemoryDriven = 1UL << 34, IoDriven = 1UL << 35, ReadDriven = 1UL << 36, WriteDriven = 1UL << 37;
    internal const ulong M1High = 1UL << 28, RefreshHigh = 1UL << 29, HaltHigh = 1UL << 30, BusAckHigh = 1UL << 31;
    internal const ulong MemoryRead = AddressDriven | MemoryDriven | ReadDriven;
    internal const ulong IoRead = AddressDriven | IoDriven | ReadDriven;
    internal const ulong MemoryWrite = AddressDriven | DataDriven | MemoryDriven | WriteDriven;
    internal const ulong IoWrite = AddressDriven | DataDriven | IoDriven | WriteDriven;
    internal const ulong InitialLevels = (0xffUL << 24) | (0x3cUL << DrivenShift);
    internal static readonly ulong[] Masks = [AddressMask | AddressDriven, DataMask | DataDriven,
        MemoryHigh | MemoryDriven, IoHigh | IoDriven, ReadHigh | ReadDriven, WriteHigh | WriteDriven,
        M1High, RefreshHigh, HaltHigh, BusAckHigh];
    internal static readonly ulong[] DrivenMasks = [AddressDriven, DataDriven, MemoryDriven,
        IoDriven, ReadDriven, WriteDriven, 0, 0, 0, 0];

}
