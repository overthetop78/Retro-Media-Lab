namespace RetroMediaLab.Chips.Custom.Acorn.Archimedes.Hardware.Constants;

public static class ArchimedesHardwareConstants
{
    public const uint LogicalLimit = 0x02000000;
    public const uint PhysicalBase = 0x02000000;
    public const uint PhysicalLimit = 0x03000000;
    public const uint HighRomBase = 0x03800000;
    public const uint RomOffsetMask = 0x001fffff;
    public const uint BootLowAddressMask = 0x03000000;
    public const int WordBytes = 4;
    public const int Megabyte = Kilobyte * Kilobyte;
    public const uint OpenBusWord = uint.MaxValue;
    public const byte OpenBusByte = byte.MaxValue;
    public const int DefaultVideoClockHz = 24000000;
    public const int Kilobyte = 1024;

}
