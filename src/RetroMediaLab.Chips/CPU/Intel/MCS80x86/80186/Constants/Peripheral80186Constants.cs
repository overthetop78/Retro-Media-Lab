namespace RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Constants;

public static class Peripheral80186Constants
{
    public const ushort ResetRelocation = 0x20ff;
    public const ushort MemoryMapped = 0x1000;
    public const ushort SlaveInterruptMode = 0x4000;
    public const ushort EscapeTrap = 0x8000;
    public const int RelocationRegister = 0xfe;
    public const int PageShift = 8;
    public const int IoPageMask = 0xff;
    public const int MemoryPageMask = 0xfff;
    public const int RegisterMask = 0xfe;
    public const int ByteShift = 8;
    public const byte EscapeVector = 7;
}
