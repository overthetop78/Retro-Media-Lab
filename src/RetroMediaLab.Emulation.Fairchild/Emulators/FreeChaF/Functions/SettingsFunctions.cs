using RetroMediaLab.Emulation.Fairchild.Emulators.Common.Interop.Constants;
using RetroMediaLab.Emulation.Fairchild.Emulators.FreeChaF.Dictionaries;
using CommonSettings = RetroMediaLab.Emulation.Fairchild.Common.Machines.Common.Functions.SettingsDescriptionFunctions;

namespace RetroMediaLab.Emulation.Fairchild.Emulators.FreeChaF.Functions;

internal static class SettingsDescriptionFunctions
{
    internal static IReadOnlyList<EmulationMediaDevice> Storage(MachineConfiguration configuration) =>
        [new(EmulationMediaSlot.Cartridge0, EmulationMediaType.Cartridge, MediaRuntimeConstants.CartridgeExtensions, RequiresMachineRecreation: true, ImageDirectory: configuration.Folders.Cartridges, IsPermanent: true)];

    internal static IReadOnlyList<CoreOption> GetOptions() => FreeChaFOptionCatalog.All;

    internal static IReadOnlyList<EmulationSettingsBlock> Create(MachineConfiguration configuration) =>
        [new(SettingsConstants.ResourceNativeVideo, EmulationMachineTab.Video, SettingsConstants.ResourceNativeVideo,
            GetOptions().Select(option => CommonSettings.Native(configuration, option, EmulationMachineTab.Video, SettingsConstants.ResourceNativeVideo)).ToArray())];
}
