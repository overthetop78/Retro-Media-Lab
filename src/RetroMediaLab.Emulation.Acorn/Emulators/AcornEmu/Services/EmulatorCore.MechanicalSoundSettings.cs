using System.Globalization;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Functions;
using CommonHost = RetroMediaLab.Emulation.Acorn.Common.Functions.CoreHostFunctions;
using S = RetroMediaLab.Emulation.Constants.EmulationMechanicalSoundConstants;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.StorageConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;

internal sealed partial class EmulatorCore
{
    private IEnumerable<CoreOption> MechanicalSoundOptions()
    {
        foreach (var option in SoundOptions(EmulationTapeSettingsConstants.Sound, EmulationCassetteSettings.Prefix + S.VolumeOption,
            _cassetteSettings.MechanicalSound.ToString(), _cassetteSettings.MechanicalVolumePercent)) yield return option;
        for (var index = 0; index < _floppySettings.Length; index++)
        {
            if (!_floppyConnected[index]) continue;
            foreach (var option in SoundOptions(C.Key(index, C.MechanicalSoundOption), C.Key(index, C.SoundVolumeOption),
                _floppySettings[index]?.Options?.GetValueOrDefault(C.MechanicalSoundOption, bool.TrueString) ?? bool.TrueString,
                EmulationMechanicalSoundSettings.ReadVolume(_floppySettings[index]?.Options))) yield return option;
        }
        for (var index = 0; index < _hardDiskSettings.Length; index++)
        {
            if (!_hardDiskConnected[index]) continue;
            foreach (var option in SoundOptions(C.HardDiskKey(index, C.MechanicalSoundOption), C.HardDiskKey(index, C.SoundVolumeOption),
                _hardDiskSoundEnabled[index].ToString(), _hardDiskSoundVolume[index])) yield return option;
        }
    }

    private static IEnumerable<CoreOption> SoundOptions(string enabledKey, string volumeKey, string enabled, int volume)
    {
        yield return new(enabledKey, C.SoundResource, null, null, bool.TrueString, enabled, [],
            IsVisible: false, ResourceKey: C.SoundResource, Tab: EmulationMachineTab.Storage);
        yield return new(volumeKey, SettingsConstants.ResourceAudioVolume, null, null,
            S.DefaultVolumePercent.ToString(CultureInfo.InvariantCulture), volume.ToString(CultureInfo.InvariantCulture), [],
            IsVisible: false, ResourceKey: SettingsConstants.ResourceAudioVolume, Tab: EmulationMachineTab.Storage);
    }

    /// <summary>Applique les seuls réglages sonores du lecteur sans toucher au matériel de la machine.</summary>
    private bool ApplyMechanicalSoundOption(string key, string value)
    {
        if (!StorageFunctions.TryGetMechanicalSoundOption(key, out var hardDisk, out var index, out var option)) return false;
        var values = new Dictionary<string, string>(StringComparer.Ordinal) { [option] = value };
        if (option == C.SoundVolumeOption) _ = EmulationMechanicalSoundSettings.ReadVolume(values);
        else if (option == C.MechanicalSoundOption) _ = bool.Parse(value);
        if (hardDisk)
        {
            if (option == C.SoundVolumeOption) _hardDiskSoundVolume[index] = int.Parse(value, CultureInfo.InvariantCulture);
            else if (option == C.MechanicalSoundOption) _hardDiskSoundEnabled[index] = bool.Parse(value);
            ConfigureHardDiskSound(index);
        }
        else if (_floppySettings[index] is { } settings)
        {
            var options = new Dictionary<string, string>(settings.Options ?? new Dictionary<string, string>(), StringComparer.Ordinal) { [option] = value };
            _floppySettings[index] = settings with { Options = options };
            ConfigureFloppySound(index);
        }
        _runtimeOptions[key] = value;
        return true;
    }
}
