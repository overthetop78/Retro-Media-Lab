namespace RetroMediaLab.Chips.IO.NationalSemiconductor.INS815x.Chip8154.Constants;

public static class RamIoConstants
{
    public const int RamBytes = 128;
    public const int RamAddressMask = RamBytes - 1;
    public const int BitAddressMask = 7;
    public const int PortSelection = 0x08;
    public const int SetBit = 0x10;
    public const int FirstRegister = 0x20;
    public const int PortA = FirstRegister;
    public const int PortB = 0x21;
    public const int DirectionA = 0x22;
    public const int DirectionB = 0x23;
    public const int Mode = 0x24;
    public const byte BitReadHigh = 0x80;
    public const byte BasicMode = 0;
}
