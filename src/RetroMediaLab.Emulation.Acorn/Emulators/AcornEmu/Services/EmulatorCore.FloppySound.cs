using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants;
using S = RetroMediaLab.Emulation.Constants.EmulationMechanicalSoundConstants;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.StorageConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;

internal sealed partial class EmulatorCore
{
    private IEmulationDriveMechanics?[] _floppyMechanics = [];

    private void ConfigureFloppySound(int drive, bool allowSound = true)
    {
        if (_floppyMechanics[drive] is not { } mechanics) return;
        var settings = _floppySettings[drive];
        var enabled = allowSound && _floppyConnected[drive] && settings is { Speed: C.DefaultSpeed }
            && bool.Parse(settings.Options?.GetValueOrDefault(C.MechanicalSoundOption, bool.TrueString) ?? bool.TrueString);
        mechanics.Configure(Functions.ArchimedesFunctions.IsModel(_definition.Model) ? S.Floppy35 : StoragePresentationConstants.FloppySound,
            EmulationMechanicalSoundSettings.ReadVolume(settings?.Options), enabled);
    }

}
