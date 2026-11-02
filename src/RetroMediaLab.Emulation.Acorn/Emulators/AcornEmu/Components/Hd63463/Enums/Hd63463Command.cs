namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Hd63463.Enums;

internal enum Hd63463Command : byte
{
    Recall = 0x08,
    Polling = 0x10,
    DisablePolling = 0x18,
    CheckEcc = 0x20,
    CheckDrive = 0x28,
    OpenBufferRead = 0x30,
    OpenBufferWrite = 0x38,
    ReadData = 0x40,
    CheckData = 0x48,
    BufferToMemory = 0x50,
    ReadId = 0x60,
    FindId = 0x61,
    ReadIdSkew = 0x68,
    ReadErroneousData = 0x70,
    WriteData = 0x87,
    CompareData = 0x88,
    MemoryToBuffer = 0x90,
    WriteFormat = 0xa3,
    WriteFormatSkew = 0xab,
    Seek = 0xc0,
    Recalibrate = 0xc8,
    Test = 0xe0,
    Specify = 0xe8,
    Abort = 0xf0
}
