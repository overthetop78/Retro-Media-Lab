namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Upd765.Enums;

internal enum Upd765Command : byte
{
    ReadTrack = 0x02,
    Specify = 0x03,
    SenseDriveStatus = 0x04,
    WriteData = 0x05,
    ReadData = 0x06,
    Recalibrate = 0x07,
    SenseInterrupt = 0x08,
    WriteDeleted = 0x09,
    ReadId = 0x0a,
    ReadDeleted = 0x0c,
    FormatTrack = 0x0d,
    Seek = 0x0f,
    Version = 0x10,
    ScanEqual = 0x11,
    ScanLowEqual = 0x19,
    ScanHighEqual = 0x1d
}
