namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMaster.Common.Constants;

/// <summary>Configuration initiale du montage Compact ; ces octets ne sont pas des valeurs de reset du PCD8572.</summary>
internal static class CompactNonvolatileMemoryConstants
{
    internal static ReadOnlySpan<byte> InitialConfiguration =>
        [0, 0xfe, 0, 0xeb, 0, 0xed, 0xff, 0xff, 0, 0, 0xf7, 0xe3, 0x20, 8, 0x0a, 0x2c, 0x80];
}
