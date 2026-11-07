using RetroMediaLab.Emulation.Apple.Common.Machines.Common.Contracts;
using RetroMediaLab.Emulation.Apple.Common.Machines.Common.Enums;

namespace RetroMediaLab.Emulation.Apple.Common.Machines.Common.Functions;

internal static class HardwareModelFunctions
{
    internal static bool IsFirmwareCompatible(HardwareModelDefinition definition, FirmwareCategory category) => definition.Firmware.Contains(category);
    internal static bool IsMediaCompatible(HardwareModelDefinition definition, MediaCategory category, EmulationMediaSlot slot) => definition.Media.Contains(category) && category switch
        {
            MediaCategory.Floppy => slot == EmulationMediaSlot.Floppy0 || slot == EmulationMediaSlot.Floppy1,
            MediaCategory.HardDisk => slot == EmulationMediaSlot.HardDisk0,
            MediaCategory.Tape => slot == EmulationMediaSlot.Tape0,
            _ => false
        };
}
