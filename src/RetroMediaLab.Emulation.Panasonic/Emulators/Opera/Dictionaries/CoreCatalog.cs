using EmulatorCatalogFunctions = RetroMediaLab.Emulation.Panasonic.Common.Functions.EmulatorCatalogFunctions;
using RetroMediaLab.Emulation;
using RetroMediaLab.Emulation.Panasonic.Emulators.Opera.Constants;

namespace RetroMediaLab.Emulation.Panasonic.Emulators.Opera.Dictionaries;
internal static class CoreCatalog
{
    internal static readonly EmulatorCatalogEntry Entry = new(
        Emulator.Opera,
        EmulatorConstants.Id,
        EmulatorConstants.LibraryName,
        EmulatorConstants.DllName,
        EmulatorConstants.DllName + EmulatorCatalogConstants.ArchiveExtension,
        new Uri(EmulatorConstants.ArchiveUri),
        new Uri(EmulatorConstants.SourceUri),
        EmulatorConstants.InspectedRevision,
        Enum.GetValues<MachineModel>().ToHashSet(),
        EmulatorConstants.DescriptionResourceKey
    );
    internal static EmulatorCatalogEntry Get(Emulator emulator) => emulator == Emulator.Opera ? Entry : throw new ArgumentOutOfRangeException(nameof(emulator));
    internal static CoreInstallationPaths GetInstallationPaths(Emulator emulator, string installationRoot, string version) =>
        EmulatorCatalogFunctions.GetInstallationPaths(Get(emulator), installationRoot, version);

    internal static string GetActiveManifestPath(Emulator emulator, string installationRoot) =>
        EmulatorCatalogFunctions.GetActiveManifestPath(Get(emulator), installationRoot);
}