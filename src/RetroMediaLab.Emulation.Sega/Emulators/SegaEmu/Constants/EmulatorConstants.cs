namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Constants;

internal static class EmulatorConstants
{
    internal const string Id = "segaemu", Name = "SegaEmu", Version = "1";
    internal const string InternalGameEnabled = "segaemu.internalGame.enabled";
    internal const string InternalGameFirmware = "segaemu.internalGame.rom";
    internal const string CartridgeLayout = "segaemu.cartridge.layout";
    internal const string InternalGameLabel = "Emulation.SegaEmu.InternalGame";
    internal const string InternalGameHelp = "Emulation.SegaEmu.InternalGame.Help";
    internal const string InternalRomLabel = "Emulation.SegaEmu.InternalRom";
    internal const string LayoutLabel = "Emulation.SegaEmu.CartridgeLayout";
    internal const string StandardLayoutLabel = "Emulation.SegaEmu.CartridgeLayout.Standard";
    internal const string TwoRomLayoutLabel = "Emulation.SegaEmu.CartridgeLayout.Two8K";
    internal const string CastleLayoutLabel = "Emulation.SegaEmu.CartridgeLayout.TheCastle";
    internal const string OthelloLayoutLabel = "Emulation.SegaEmu.CartridgeLayout.Othello";
    internal const string PauseLabel = "Emulation.SegaEmu.ConsolePause";
    internal static readonly IReadOnlySet<string> Extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".sg", ".mv", ".bin", ".rom" };
}
