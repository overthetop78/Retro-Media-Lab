using RetroMediaLab.Emulation.Commodore.Emulators.Common.Constants;
using RetroMediaLab.Emulation.Commodore.Emulators.PUAE.Constants;

namespace RetroMediaLab.Emulation.Commodore.Emulators.PUAE.Factories;

internal sealed class PuaeMachineFactory : UaeMachineFactory
{
    public override EmulatorCatalogEntry CatalogEntry { get; } = new(Emulator.PUAE, new(EmulatorConstants.Id, EmulatorConstants.DisplayName, EmulatorDescriptionConstants.AmigaDescriptionResourceKey, SupportedMachines));
    internal override CoreDefinition CoreDefinition { get; } = new(
        EmulatorConstants.DisplayName, EmulatorConstants.LibraryFile, EmulatorConstants.DownloadUrl,
        new(Emulator.PUAE, EmulatorConstants.ValidatedReleaseId, EmulatorConstants.ValidatedReleaseId, new(EmulatorConstants.DownloadUrl), EmulatorConstants.ReleasePublishedAt, null) { DisplayName = EmulatorConstants.ValidatedReleaseDisplayName, IsRequired = true, IsZip = true });
}
