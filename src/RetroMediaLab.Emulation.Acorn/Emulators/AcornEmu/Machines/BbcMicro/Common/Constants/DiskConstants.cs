namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants;

/// <summary>Décodage BBC des ROM latérales et des cartes disquette 8271 et Acorn WD1770.</summary>
internal static class DiskConstants
{
    internal const int RomBankSize = 16384;
    internal const int RomBankCount = 16;
    internal const int BasicBank = 15;
    internal const int DfsBank = 14;
    internal const int NetworkBank = 13;
    internal const ushort RomSelectStart = 0xfe30;
    internal const ushort RomSelectEnd = 0xfe40;
    internal const ushort ControllerStart = 0xfe80;
    internal const ushort ControllerEnd = 0xfea0;
    internal const byte IntelSideSelect = 0x20;
    internal const int DriveCount = 2;
    internal const int IntelOwnerClock = 2_000_000;
    internal const int IntelChipClock = 2_000_000;
}
