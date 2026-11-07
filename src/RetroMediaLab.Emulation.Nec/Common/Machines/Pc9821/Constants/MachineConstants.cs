using RetroMediaLab.Emulation.Nec.Common.Machines.Common.Contracts;
using RetroMediaLab.Emulation.Nec.Common.Machines.Common.Enums;

namespace RetroMediaLab.Emulation.Nec.Common.Machines.Pc9821.Constants;

internal static class MachineConstants
{
    internal const string Id = "Pc9821";
    internal const string DisplayName = "PC-9821";
    internal const string CpuName = "80386 / 80486";
    internal const string CpuClock = "16 MHz / 33 MHz";
    internal static Model Definition { get; } = new(Id, DisplayName, Id,
        (int)RamCapacity._640KB, HasKeyboard: true,
        BuiltInFloppyDriveCount: (int)DriveCount.Two,
        MaximumFloppyDriveCount: (int)DriveCount.Two,
        HasBuiltInTapeDrive: false, SupportsTapeDrive: false,
        HasBuiltInCartridgeSlot: false, SupportsCartridgeSlot: false,
        ControllerPortCount: (int)ControllerPortCount.Two,
        MouseButtonCount: (int)MouseButtonCount.Two,
        CpuName: CpuName, CpuClock: CpuClock,
        HasBuiltInCdDrive: false, SupportsCdDrive: false, SupportsHardDrives: true);
}
