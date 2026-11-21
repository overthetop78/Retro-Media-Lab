namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Constants;

internal static class FirmwareConstants
{
    internal const string TmssField = "configuration.firmware.segaemu.0";
    internal const string MegaCdField = "configuration.firmware.segaemu.megaCd";
    internal const string MegaCdFile = "bios_CD.bin";
    internal static IReadOnlyList<(string File, string LegacyField, byte Region)> MegaCdRegions { get; } =
        [("bios_CD_E.bin", "configuration.firmware.segaemu.1", 2),
         ("bios_CD_U.bin", "configuration.firmware.segaemu.2", 1),
         ("bios_CD_J.bin", "configuration.firmware.segaemu.3", 3)];

    internal const string SaturnBiosFileName = global::RetroMediaLab.Emulation.Sega.Common.Machines.Common.Constants.FirmwareConstants.SaturnBiosFileName;
    internal const string SaturnSh1RomFileName = "saturn_sh1.bin";
    internal const string SaturnMpegRomFileName = "saturn_mpeg.bin";
    internal const string Sf7000Ipl = "sf7000.rom";
    internal const string MasterSystemAutoFile = "bios.sms";
    internal static IReadOnlyList<string> MasterSystemRegionalFiles { get; } = ["bios_U.sms", "bios_E.sms", "bios_J.sms"];

    internal static IReadOnlyList<FirmwareSlot> All { get; } =
    [
        new("configuration.firmware.bluemsx.0", Sf7000Ipl, "Emulation.Firmware.bluemsx.0", true, new HashSet<string>(StringComparer.Ordinal) { ModelConstants.Sf7000 }),
        new("configuration.firmware.segaemu.saturn", SaturnBiosFileName, "Emulation.Firmware.yabause.0", false, new HashSet<string>(StringComparer.Ordinal) { ModelConstants.Saturn }),
        new("configuration.firmware.segaemu.saturn.sh1", SaturnSh1RomFileName, "Emulation.Firmware.SaturnSh1", false, new HashSet<string>(StringComparer.Ordinal) { ModelConstants.Saturn }),
        new("configuration.firmware.segaemu.saturn.mpeg", SaturnMpegRomFileName, "Emulation.Firmware.SaturnMpeg", false, new HashSet<string>(StringComparer.Ordinal) { ModelConstants.Saturn }),
        new("configuration.firmware.segaemu.12", "32X_G_BIOS.BIN", "Emulation.Firmware.segaemu.12", false, OptionMachineConstants.ThirtyTwoX),
        new("configuration.firmware.segaemu.13", "32X_M_BIOS.BIN", "Emulation.Firmware.segaemu.13", false, OptionMachineConstants.ThirtyTwoX),
        new("configuration.firmware.segaemu.14", "32X_S_BIOS.BIN", "Emulation.Firmware.segaemu.14", false, OptionMachineConstants.ThirtyTwoX),
        new(TmssField, "bios_MD.bin", "Emulation.Firmware.segaemu.Tmss", false, OptionMachineConstants.MegaDriveAndCd),
        new(MegaCdField, MegaCdFile, "Emulation.Firmware.segaemu.MegaCd", false,
            OptionMachineConstants.MegaCd,
            CandidateFileNames: ["bios_CD_E.bin", "bios_CD_U.bin", "bios_CD_J.bin"]),
        new("configuration.firmware.segaemu.4", "bios_E.sms", "Emulation.Firmware.segaemu.4", false, OptionMachineConstants.MasterSystem),
        new("configuration.firmware.segaemu.5", "bios_U.sms", "Emulation.Firmware.segaemu.5", false, OptionMachineConstants.MasterSystem),
        new("configuration.firmware.segaemu.6", "bios_J.sms", "Emulation.Firmware.segaemu.6", false, OptionMachineConstants.MasterSystem),
        new("configuration.firmware.segaemu.masterSystem", MasterSystemAutoFile, "Emulation.Firmware.segaemu.MasterSystem",
            false, OptionMachineConstants.MasterSystem, CandidateFileNames: MasterSystemRegionalFiles),
        new("configuration.firmware.segaemu.7", "bios.gg", "Emulation.Firmware.segaemu.7", false, new HashSet<string>(StringComparer.Ordinal) { ModelConstants.GameGear }),
        new("configuration.firmware.segaemu.8", "sk.bin", "Emulation.Firmware.segaemu.8", false, OptionMachineConstants.LockOn),
        new("configuration.firmware.segaemu.9", "sk2chip.bin", "Emulation.Firmware.segaemu.9", false, OptionMachineConstants.LockOn),
        new("configuration.firmware.segaemu.10", "ggenie.bin", "Emulation.Firmware.segaemu.10", false, OptionMachineConstants.LockOn),
        new("configuration.firmware.segaemu.11", "areplay.bin", "Emulation.Firmware.segaemu.11", false, OptionMachineConstants.LockOn),
    ];
}
