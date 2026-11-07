using S = RetroMediaLab.Emulation.Constants.EmulationMechanicalSoundConstants;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.StorageConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;

internal sealed partial class EmulatorCore
{
    /// <summary>Restaure les supports déjà présents sans rejouer une action physique de l’utilisateur.</summary>
    private void ResetRestoredMediaAudio()
    {
        _tapeMechanics?.Reset();
        _tapeSoundOperation = S.None;
        ConfigureTapeSound();
        for (var drive = 0; drive < _floppyImages.Length; drive++)
        {
            if (_floppyMechanics[drive] is not { } mechanics) continue;
            ConfigureFloppySound(drive, allowSound: false);
            try
            {
                mechanics.Reset();
                if (_floppyImages[drive] is not null) mechanics.Insert();
            }
            finally { ConfigureFloppySound(drive); }
        }
        for (var unit = 0; unit < _hardDisks.Length; unit++)
        {
            _hardDiskMechanics[unit]?.Reset();
            ConfigureHardDiskSound(unit);
        }
    }
}
