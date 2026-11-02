namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Ata.Enums;

internal enum AtaCommand : byte
{
    Nop = 0x00,
    Recalibrate = 0x10,
    ReadSectors = 0x20,
    ReadSectorsNoRetry = 0x21,
    WriteSectors = 0x30,
    WriteSectorsNoRetry = 0x31,
    VerifySectors = 0x40,
    VerifySectorsNoRetry = 0x41,
    Seek = 0x70,
    Diagnostic = 0x90,
    InitializeParameters = 0x91,
    ReadMultiple = 0xc4,
    WriteMultiple = 0xc5,
    SetMultiple = 0xc6,
    ReadBuffer = 0xe4,
    WriteBuffer = 0xe8,
    Identify = 0xec,
    SetFeatures = 0xef
}
