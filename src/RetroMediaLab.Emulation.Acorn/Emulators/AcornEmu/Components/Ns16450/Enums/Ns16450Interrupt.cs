namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Ns16450.Enums;

internal enum Ns16450Interrupt : byte
{
    ModemStatus = 0x00,
    None = 0x01,
    TransmitEmpty = 0x02,
    ReceivedData = 0x04,
    LineStatus = 0x06
}
