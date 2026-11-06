using S = RetroMediaLab.Emulation.Constants.EmulationMechanicalSoundConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;

internal sealed partial class EmulatorCore
{
    private void ConfigureCassetteSound()
    {
        _cassetteMechanics?.Configure(null, _cassetteSettings.MechanicalVolumePercent,
            _cassetteSettings.MechanicalSound && _cassetteSettings.Mode == EmulationCassettePlaybackMode.RealTime);
        _cassetteSoundOperation = S.None;
        OnCassetteActivityChanged();
    }

    private void OnCassetteActivityChanged()
    {
        var operation = (CassetteActive ? CassetteOperation?.ToString() : null) ?? S.None;
        if (operation != _cassetteSoundOperation)
        {
            var previous = _cassetteSoundOperation;
            _cassetteSoundOperation = operation;
            _cassetteMechanics?.SetTapeOperation(operation == S.None && previous != S.None ? S.Stop : operation);
        }
        ActivityChanged?.Invoke();
    }

    /// <summary>Transmet une commande utilisateur ; la lecture du bruitage appartient à la sortie hôte.</summary>
    private void SetCassetteSoundEvent(string operation) => _cassetteMechanics?.SetTapeOperation(operation);

    private void ResetMechanicalAudio()
    {
        _cassetteMechanics?.Reset();
        _cassetteSoundOperation = S.None;
        foreach (var mechanics in _floppyMechanics) mechanics?.Reset();
        foreach (var mechanics in _hardDiskMechanics) mechanics?.Reset();
    }
}
