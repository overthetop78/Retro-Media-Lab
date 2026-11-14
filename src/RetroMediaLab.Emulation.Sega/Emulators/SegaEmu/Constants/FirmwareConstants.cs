using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Common;
using RetroMediaLab.Chips.Common.Memory;
namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Constants;

internal static class FirmwareConstants
{
    internal static IReadOnlyList<FirmwareSlot> All { get; } =
    [
        new("configuration.firmware.segaemu.0", "bios_MD.bin", "Emulation.Firmware.segaemu.0", false, new HashSet<string>(StringComparer.Ordinal) { ModelConstants.MegaDrive }),
        new("configuration.firmware.segaemu.1", "bios_CD_E.bin", "Emulation.Firmware.segaemu.1", false, new HashSet<string>(StringComparer.Ordinal) { ModelConstants.MegaCd, ModelConstants.MegaDrive }),
        new("configuration.firmware.segaemu.2", "bios_CD_U.bin", "Emulation.Firmware.segaemu.2", false, new HashSet<string>(StringComparer.Ordinal) { ModelConstants.MegaCd, ModelConstants.MegaDrive }),
        new("configuration.firmware.segaemu.3", "bios_CD_J.bin", "Emulation.Firmware.segaemu.3", false, new HashSet<string>(StringComparer.Ordinal) { ModelConstants.MegaCd, ModelConstants.MegaDrive }),
        new("configuration.firmware.segaemu.4", "bios_E.sms", "Emulation.Firmware.segaemu.4", false, new HashSet<string>(StringComparer.Ordinal) { ModelConstants.MasterSystem, ModelConstants.MarkIII }),
        new("configuration.firmware.segaemu.5", "bios_U.sms", "Emulation.Firmware.segaemu.5", false, new HashSet<string>(StringComparer.Ordinal) { ModelConstants.MasterSystem, ModelConstants.MarkIII }),
        new("configuration.firmware.segaemu.6", "bios_J.sms", "Emulation.Firmware.segaemu.6", false, new HashSet<string>(StringComparer.Ordinal) { ModelConstants.MasterSystem, ModelConstants.MarkIII }),
        new("configuration.firmware.segaemu.7", "bios.gg", "Emulation.Firmware.segaemu.7", false, new HashSet<string>(StringComparer.Ordinal) { ModelConstants.GameGear }),
        new("configuration.firmware.segaemu.8", "sk.bin", "Emulation.Firmware.segaemu.8", false, OptionMachineConstants.MegaDrive),
        new("configuration.firmware.segaemu.9", "sk2chip.bin", "Emulation.Firmware.segaemu.9", false, OptionMachineConstants.MegaDrive),
        new("configuration.firmware.segaemu.10", "ggenie.bin", "Emulation.Firmware.segaemu.10", false, OptionMachineConstants.MegaDrive),
        new("configuration.firmware.segaemu.11", "areplay.bin", "Emulation.Firmware.segaemu.11", false, OptionMachineConstants.MegaDrive),
    ];
}
