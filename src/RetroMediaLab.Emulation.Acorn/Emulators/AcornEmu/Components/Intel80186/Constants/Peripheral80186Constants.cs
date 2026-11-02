namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Intel80186.Constants;

internal static class Peripheral80186Constants
{
    internal const ushort ResetRelocation = 0x20ff;
    internal const ushort MemoryMapped = 0x1000;
    internal const ushort SlaveInterruptMode = 0x4000;
    internal const ushort EscapeTrap = 0x8000;
    internal const int RelocationRegister = 0xfe;
    internal const int PageShift = 8;
    internal const int IoPageMask = 0xff;
    internal const int MemoryPageMask = 0xfff;
    internal const int RegisterMask = 0xfe;
    internal const int ByteShift = 8;
    internal const byte EscapeVector = 7;
}
