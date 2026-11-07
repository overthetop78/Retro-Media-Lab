namespace RetroMediaLab.Emulation.Philips.Emulators.M2000.Functions;

internal static class StorageDescriptionFunctions
{
    internal static IReadOnlyList<EmulationMediaDevice> Describe(MachineConfiguration configuration) =>
        [new(ConfigurationFunctions.DefaultSlots(configuration.Model).Single(), EmulationMediaType.Tape, StorageSettingsConstants.TapeExtensions, RequiresMachineRecreation: true, ImageDirectory: configuration.Folders.Tapes, IsPermanent: true)];
}
