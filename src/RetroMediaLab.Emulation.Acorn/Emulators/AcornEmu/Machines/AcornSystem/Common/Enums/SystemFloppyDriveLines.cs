namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Enums;

/// <summary>Physical, active-low connector outputs after the open-collector buffers.
/// A high bit means a released collector resolved high by the connector pull-up.</summary>
[Flags]
internal enum SystemFloppyDriveLines : byte
{
    Select0 = 1,
    Select1 = 2,
    Motor = 4,
    Direction = 8,
    Step = 16,
    WriteData = 32,
    WriteEnable = 64,
    FaultReset = 128
}
