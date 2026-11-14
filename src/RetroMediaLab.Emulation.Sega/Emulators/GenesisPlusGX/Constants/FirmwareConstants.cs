namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGX.Constants;

internal static class FirmwareConstants
{
    internal static IReadOnlyList<FirmwareSlot> All { get; } =
    [
        new("configuration.firmware.genesisplusgx.0", "bios_MD.bin", "Emulation.Firmware.genesisplusgx.0", false, new HashSet<string>(StringComparer.Ordinal) { ModelConstants.MegaDrive }),
        new("configuration.firmware.genesisplusgx.1", "bios_CD_E.bin", "Emulation.Firmware.genesisplusgx.1", false, new HashSet<string>(StringComparer.Ordinal) { ModelConstants.MegaCd, ModelConstants.MegaDrive }),
        new("configuration.firmware.genesisplusgx.2", "bios_CD_U.bin", "Emulation.Firmware.genesisplusgx.2", false, new HashSet<string>(StringComparer.Ordinal) { ModelConstants.MegaCd, ModelConstants.MegaDrive }),
        new("configuration.firmware.genesisplusgx.3", "bios_CD_J.bin", "Emulation.Firmware.genesisplusgx.3", false, new HashSet<string>(StringComparer.Ordinal) { ModelConstants.MegaCd, ModelConstants.MegaDrive }),
        new("configuration.firmware.genesisplusgx.4", "bios_E.sms", "Emulation.Firmware.genesisplusgx.4", false, new HashSet<string>(StringComparer.Ordinal) { ModelConstants.MasterSystem, ModelConstants.MarkIII }),
        new("configuration.firmware.genesisplusgx.5", "bios_U.sms", "Emulation.Firmware.genesisplusgx.5", false, new HashSet<string>(StringComparer.Ordinal) { ModelConstants.MasterSystem, ModelConstants.MarkIII }),
        new("configuration.firmware.genesisplusgx.6", "bios_J.sms", "Emulation.Firmware.genesisplusgx.6", false, new HashSet<string>(StringComparer.Ordinal) { ModelConstants.MasterSystem, ModelConstants.MarkIII }),
        new("configuration.firmware.genesisplusgx.7", "bios.gg", "Emulation.Firmware.genesisplusgx.7", false, new HashSet<string>(StringComparer.Ordinal) { ModelConstants.GameGear }),
        new("configuration.firmware.genesisplusgx.8", "sk.bin", "Emulation.Firmware.genesisplusgx.8", false, new HashSet<string>(StringComparer.Ordinal) { ModelConstants.MegaDrive }),
        new("configuration.firmware.genesisplusgx.9", "sk2chip.bin", "Emulation.Firmware.genesisplusgx.9", false, new HashSet<string>(StringComparer.Ordinal) { ModelConstants.MegaDrive }),
        new("configuration.firmware.genesisplusgx.10", "ggenie.bin", "Emulation.Firmware.genesisplusgx.10", false, new HashSet<string>(StringComparer.Ordinal) { ModelConstants.MegaDrive }),
        new("configuration.firmware.genesisplusgx.11", "areplay.bin", "Emulation.Firmware.genesisplusgx.11", false, new HashSet<string>(StringComparer.Ordinal) { ModelConstants.MegaDrive }),
    ];
}
