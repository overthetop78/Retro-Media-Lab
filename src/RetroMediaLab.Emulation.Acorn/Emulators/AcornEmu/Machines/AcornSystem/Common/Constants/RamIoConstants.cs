namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Constants;

internal static class RamIoConstants
{
    internal const int RamBytes = 128;
    internal const int RamAddressMask = RamBytes - 1;
    internal const int BitAddressMask = 7;
    internal const int PortSelection = 0x08;
    internal const int SetBit = 0x10;
    internal const int FirstRegister = 0x20;
    internal const int PortA = FirstRegister;
    internal const int PortB = 0x21;
    internal const int DirectionA = 0x22;
    internal const int DirectionB = 0x23;
    internal const int Mode = 0x24;
    internal const byte BitReadHigh = 0x80;
    internal const byte BasicMode = 0;
}
