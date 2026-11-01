using RetroMediaLab.Emulation.Mattel.Emulators.FreeIntv.Constants;
using Storage = RetroMediaLab.Emulation.Mattel.Emulators.FreeIntv.Constants.StorageConstants;

namespace RetroMediaLab.Emulation.Mattel.Emulators.FreeIntv.Functions;

internal static class StorageDescriptionFunctions
{
    internal static IReadOnlyList<EmulationMediaDevice> Describe(MachineConfiguration configuration) =>
        [new(ConfigurationFunctions.DefaultSlots(configuration.Model).Single(), EmulationMediaType.Cartridge, Storage.CartridgeExtensions, RequiresMachineRecreation: true, ImageDirectory: configuration.Folders.Cartridges, IsPermanent: true)];
}
