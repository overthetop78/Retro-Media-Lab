using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants;
using S = RetroMediaLab.Emulation.Constants.EmulationMechanicalSoundConstants;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.StorageConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;

internal sealed partial class EmulatorCore
{
    private IEmulationDriveMechanics?[] _floppyMechanics = [];

    /// <summary>Rend la mécanique commune à partir du moteur et des transferts du contrôleur.</summary>
    private void MixFloppySound(double[] samples)
    {
        if (_machine is not { } machine) return;
        for (var drive = 0; drive < _floppyImages.Length; drive++)
        {
            if (_floppyMechanics[drive] is not { } mechanics) continue;
            ConfigureFloppySound(drive);
            mechanics.SetMotor(_floppyConnected[drive] && machine.FloppyMotor(drive));
            mechanics.SetActivity(machine.FloppyReading(drive), machine.FloppyWriting(drive));
            var sound = mechanics.RenderAudio(SampleRate, samples.Length);
            for (var index = 0; index < sound.Length; index++) samples[index] += sound[index];
        }
    }

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
