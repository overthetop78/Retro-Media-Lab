using RetroMediaLab.Emulation.Philips.Emulators.SameCdi.Constants;
namespace RetroMediaLab.Emulation.Philips.Emulators.SameCdi.Functions;

internal static class StorageDescriptionFunctions
{
    internal static IReadOnlyList<EmulationMediaDevice> Describe(MachineConfiguration configuration) =>
        [new(ConfigurationFunctions.DefaultSlots(configuration.Model).Single(), EmulationMediaType.CompactDisc,
            RetroMediaLab.Emulation.Philips.Emulators.SameCdi.Constants.StorageConstants.AcceptedExtensions, RequiresMachineRecreation: true, ImageDirectory: configuration.Folders.CompactDiscs, IsPermanent: true)];
}
