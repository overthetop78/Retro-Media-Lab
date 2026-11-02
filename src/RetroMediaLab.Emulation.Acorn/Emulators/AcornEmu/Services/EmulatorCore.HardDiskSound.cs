using S = RetroMediaLab.Emulation.Constants.EmulationMechanicalSoundConstants;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.StorageConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;

internal sealed partial class EmulatorCore
{
    private int[] _hardDiskSoundVolume = [];
    private bool[] _hardDiskSoundEnabled = [];
    private IEmulationDriveMechanics?[] _hardDiskMechanics = [];

    /// <summary>Rend les mouvements et la rotation du Winchester par la mécanique commune.</summary>
    private void MixHardDiskSound(double[] samples)
    {
        if (_machine is not { } machine) return;
        for (var unit = 0; unit < _hardDisks.Length; unit++)
        {
            if (_hardDiskMechanics[unit] is not { } mechanics) continue;
            ConfigureHardDiskSound(unit);
            mechanics.SetMotor(_hardDiskConnected[unit] && machine.HardDiskMotor(unit));
            mechanics.SetActivity(machine.HardDiskActive(unit) && !machine.HardDiskWriting(unit), machine.HardDiskWriting(unit));
            var sound = mechanics.RenderAudio(SampleRate, samples.Length);
            for (var index = 0; index < sound.Length; index++) samples[index] += sound[index];
        }
    }

    private void ConfigureHardDiskSound(int unit)
    {
        _hardDiskMechanics[unit]?.Configure(
            RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.StoragePresentationConstants.WinchesterSound,
            _hardDiskSoundVolume[unit], _hardDiskConnected[unit] && _hardDiskSoundEnabled[unit]
                && _hardDiskSettings[unit].Speed == C.DefaultSpeedPercent);
    }

}
