namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Constants;

internal static class FirmwareConstants
{
    internal static IReadOnlyList<FirmwareSlot> All { get; } =
    [
        new("configuration.firmware.genesisplusgxcs.0", "bios_MD.bin", "Emulation.Firmware.genesisplusgxcs.0", false, new HashSet<string>(StringComparer.Ordinal) { ModelConstants.MegaDrive }),
        new("configuration.firmware.genesisplusgxcs.1", "bios_CD_E.bin", "Emulation.Firmware.genesisplusgxcs.1", false, new HashSet<string>(StringComparer.Ordinal) { ModelConstants.MegaCd, ModelConstants.MegaDrive }),
        new("configuration.firmware.genesisplusgxcs.2", "bios_CD_U.bin", "Emulation.Firmware.genesisplusgxcs.2", false, new HashSet<string>(StringComparer.Ordinal) { ModelConstants.MegaCd, ModelConstants.MegaDrive }),
        new("configuration.firmware.genesisplusgxcs.3", "bios_CD_J.bin", "Emulation.Firmware.genesisplusgxcs.3", false, new HashSet<string>(StringComparer.Ordinal) { ModelConstants.MegaCd, ModelConstants.MegaDrive }),
        new("configuration.firmware.genesisplusgxcs.4", "bios_E.sms", "Emulation.Firmware.genesisplusgxcs.4", false, new HashSet<string>(StringComparer.Ordinal) { ModelConstants.MasterSystem, ModelConstants.MarkIII }),
        new("configuration.firmware.genesisplusgxcs.5", "bios_U.sms", "Emulation.Firmware.genesisplusgxcs.5", false, new HashSet<string>(StringComparer.Ordinal) { ModelConstants.MasterSystem, ModelConstants.MarkIII }),
        new("configuration.firmware.genesisplusgxcs.6", "bios_J.sms", "Emulation.Firmware.genesisplusgxcs.6", false, new HashSet<string>(StringComparer.Ordinal) { ModelConstants.MasterSystem, ModelConstants.MarkIII }),
        new("configuration.firmware.genesisplusgxcs.7", "bios.gg", "Emulation.Firmware.genesisplusgxcs.7", false, new HashSet<string>(StringComparer.Ordinal) { ModelConstants.GameGear }),
        new("configuration.firmware.genesisplusgxcs.8", "sk.bin", "Emulation.Firmware.genesisplusgxcs.8", false, new HashSet<string>(StringComparer.Ordinal) { ModelConstants.MegaDrive }),
        new("configuration.firmware.genesisplusgxcs.9", "sk2chip.bin", "Emulation.Firmware.genesisplusgxcs.9", false, new HashSet<string>(StringComparer.Ordinal) { ModelConstants.MegaDrive }),
        new("configuration.firmware.genesisplusgxcs.10", "ggenie.bin", "Emulation.Firmware.genesisplusgxcs.10", false, new HashSet<string>(StringComparer.Ordinal) { ModelConstants.MegaDrive }),
        new("configuration.firmware.genesisplusgxcs.11", "areplay.bin", "Emulation.Firmware.genesisplusgxcs.11", false, new HashSet<string>(StringComparer.Ordinal) { ModelConstants.MegaDrive }),
    ];
}
