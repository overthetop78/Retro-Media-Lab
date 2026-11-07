using S = RetroMediaLab.Emulation.Constants.EmulationMechanicalSoundConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;

internal sealed partial class EmulatorCore
{
    private void ConfigureTapeSound()
    {
        _tapeMechanics?.Configure(null, _tapeSettings.MechanicalVolumePercent,
            _tapeSettings.MechanicalSound && _tapeSettings.Mode == EmulationTapePlaybackMode.RealTime);
        _tapeSoundOperation = S.None;
        OnTapeActivityChanged();
    }

    private void OnTapeActivityChanged()
    {
        var operation = (TapeActive ? TapeOperation?.ToString() : null) ?? S.None;
        if (operation != _tapeSoundOperation)
        {
            var previous = _tapeSoundOperation;
            _tapeSoundOperation = operation;
            _tapeMechanics?.SetTapeOperation(operation == S.None && previous != S.None ? S.Stop : operation);
        }
        ActivityChanged?.Invoke();
    }

    /// <summary>Transmet une commande utilisateur ; la lecture du bruitage appartient à la sortie hôte.</summary>
    private void SetTapeSoundEvent(string operation) => _tapeMechanics?.SetTapeOperation(operation);

    private void ResetMechanicalAudio()
    {
        _tapeMechanics?.Reset();
        _tapeSoundOperation = S.None;
        foreach (var mechanics in _floppyMechanics) mechanics?.Reset();
        foreach (var mechanics in _hardDiskMechanics) mechanics?.Reset();
    }
}
