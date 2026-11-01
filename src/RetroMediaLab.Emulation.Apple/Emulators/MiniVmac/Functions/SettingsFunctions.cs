using RetroMediaLab.Emulation.Apple.Emulators.MiniVmac.Dictionaries;
using static RetroMediaLab.Emulation.Apple.Common.Machines.Common.Functions.SettingsDescriptionFunctions;
using MiniVmacConstants = RetroMediaLab.Emulation.Apple.Emulators.MiniVmac.Constants.EmulatorConstants;
using MiniVmacOptions = RetroMediaLab.Emulation.Apple.Emulators.MiniVmac.Constants.OptionConstants;

namespace RetroMediaLab.Emulation.Apple.Emulators.MiniVmac.Functions;

internal static class SettingsDescriptionFunctions
{
    internal static IReadOnlyList<EmulationSettingsBlock> Create(MachineConfiguration configuration)
    {
        var blocks = new List<EmulationSettingsBlock>();
        if (!RetroMediaLab.Emulation.Apple.Common.Machines.Common.Functions.ConfigurationFunctions.SupportsRuntime(configuration.Model))
            return [];
        blocks.Add(Block(EmulationMachineTab.Rom, SettingsConstants.ResourceFirmware,
            new EmulationSettingsField(SettingsConstants.Firmware, EmulationMachineTab.Rom,
                EmulationMachineTab.Rom.ToString(), SettingsConstants.ResourceFirmware, EmulationSettingsEditor.Path,
                configuration.Firmwares.FirstOrDefault(firmware => firmware.Category == FirmwareCategory.SystemRom)?.Path,
                DefaultFolderCategory: EmulationDefaultFolderCategory.Firmware, RequiresRestart: false)));
        blocks.Add(Block(EmulationMachineTab.Ram, SettingsConstants.ResourceRuntimeMemory,
            Information(SettingsConstants.ResourceRuntimeMemory, EmulationMachineTab.Ram, SettingsConstants.ResourceRuntimeMemory, $"{MiniVmacConstants.MainMemoryBytes / SettingsConstants.Kibibyte} KiB")));
        blocks.Add(Block(EmulationMachineTab.Video, SettingsConstants.ResourceNativeVideo, Native(configuration, MiniVmacOptionCatalog.All.Single(option => option.Key == MiniVmacOptions.Statusbar), EmulationMachineTab.Video)));
        return blocks;
    }
}
