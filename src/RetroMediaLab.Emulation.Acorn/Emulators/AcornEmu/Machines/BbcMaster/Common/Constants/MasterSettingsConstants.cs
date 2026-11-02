namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMaster.Common.Constants;

internal static class MasterSettingsConstants
{
    internal const string Prefix = "acornemu.master.";
    internal const string ChipPrefix = Prefix + "rom.";
    internal const string Coprocessor = Prefix + "coprocessor";
    internal const string CoprocessorRom = Prefix + "coprocessor.rom";
    internal const string CoprocessorGroup = "Emulation.Acorn.Master.Coprocessor";
    internal const string CoprocessorRomLabel = "Emulation.Acorn.Master.CoprocessorRom";
    internal const string TurboLabel = "Emulation.Acorn.Master.Turbo";
    internal const string Master512Label = "Emulation.Acorn.Master.Master512";
    internal const string MemoryGroup = "Emulation.Acorn.Master.Memory";
    internal const string MemoryDescription = "Emulation.Acorn.Master.MemoryDescription";
    internal const string RomGroup = "Emulation.Acorn.Master.RomSlots";
    internal const string CompactBankPrefix = "Emulation.Acorn.Master.CompactRom";
    internal const string CharacterLabel = "Emulation.Acorn.Firmware.CharacterGenerator";
    internal const string NetworkRomLabel = "Emulation.Acorn.Network.Rom";
    internal const string CmosFile = "master-cmos.bin";
    internal const int MemoryKib = 128;
    internal const int TurboMemoryKib = 64;
    internal const int Master512MemoryKib = 512;
    internal const int NetworkBank = 8;
    internal const int AdfsBank = 13;
    internal const int TerminalBank = 15;
    internal const string BasicLabel = "Emulation.Firmware.Rom.Basic";
    internal const string FirmwareBlock = "Rom";
    internal const string FirmwareLabel = "Emulation.Firmware";
    internal const string Memory128 = "128";
    internal const string Memory128Display = "128 KiB";
    internal const string FirmwareSizeError = "Emulation.Acorn.Master.InvalidSystemRom";
    internal static IReadOnlyList<int> ExtensionBanks { get; } = [0, 1, 2, 3, 8];
    internal static IReadOnlyList<int> InternalBanks { get; } = [9, 10, 11, 12, 13, 14, 15];
}
