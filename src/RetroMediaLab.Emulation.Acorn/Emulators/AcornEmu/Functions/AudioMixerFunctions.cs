using S = RetroMediaLab.Emulation.Constants.EmulationMechanicalSoundConstants;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.StorageConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Functions;

/// <summary>Déplace la présentation des réglages sonores sans changer leurs clés ni leurs propriétaires.</summary>
internal static class AudioMixerFunctions
{
    internal static IEnumerable<EmulationSettingsBlock> Describe(MachineConfiguration configuration,
        IReadOnlyList<EmulationMediaDevice> devices)
    {
        foreach (var device in devices)
        {
            var slot = device.Slot;
            if (slot.Category is not (EmulationMediaCategory.TapeDrive or EmulationMediaCategory.FloppyDrive or EmulationMediaCategory.HardDisk))
                continue;
            var tape = slot.Category == EmulationMediaCategory.TapeDrive;
            var hardDisk = slot.Category == EmulationMediaCategory.HardDisk;
            var enabledKey = tape ? EmulationTapeSettingsConstants.Sound
                : hardDisk ? C.HardDiskKey(slot.Index, C.MechanicalSoundOption) : C.Key(slot.Index, C.MechanicalSoundOption);
            var volumeKey = tape ? EmulationTapeSettings.Prefix + S.VolumeOption
                : hardDisk ? C.HardDiskKey(slot.Index, C.SoundVolumeOption) : C.Key(slot.Index, C.SoundVolumeOption);
            var id = S.MixerBlockPrefix + slot;
            var volume = EmulationMechanicalSoundSettings.DescribeVolume(volumeKey, id,
                EmulationMechanicalSoundSettings.ReadVolume(configuration.Options, volumeKey)) with
            {
                Tab = EmulationMachineTab.Audio,
                NumericRange = new(0, S.MaximumVolumePercent, 1, C.PercentSuffix, IsVertical: true)
            };
            var mute = new EmulationSettingsField(enabledKey, EmulationMachineTab.Audio, id,
                S.MuteResource, EmulationSettingsEditor.Toggle,
                configuration.Options.GetValueOrDefault(enabledKey, bool.TrueString),
                EnabledValue: bool.FalseString, DisabledValue: bool.TrueString);
            yield return new(id, EmulationMachineTab.Audio, S.MixerResource, [volume, mute]) { AudioMixerSlot = slot };
        }
    }

    internal static EmulationMediaDevice WithoutSoundConfiguration(EmulationMediaDevice device)
    {
        static bool Keep(EmulationSettingsField field) => !IsSoundOption(field.Id);
        return device with
        {
            ConfigurationFields = device.ConfigurationFields?.Where(Keep).ToArray(),
            ConfigurationBlocks = device.ConfigurationBlocks?.Select(block => block with
            {
                Fields = block.Fields.Where(Keep).ToArray()
            }).Where(block => block.Fields.Count != 0).ToArray()
        };
    }

    /// <summary>Le formulaire de stockage ne possède plus les réglages Audio : ses copies ne doivent pas les réappliquer.</summary>
    internal static EmulationStorageDeviceSettings WithoutSoundSettings(EmulationStorageDeviceSettings device) => device with
    {
        Options = device.Options?.Where(pair => !IsSoundOption(pair.Key)).ToDictionary(),
        Floppy = device.Floppy is { } floppy ? floppy with
        {
            Options = floppy.Options?.Where(pair => !IsSoundOption(pair.Key)).ToDictionary()
        } : null
    };

    private static bool IsSoundOption(string id) => id == C.MechanicalSoundOption
        || id == C.SoundVolumeOption || id == EmulationTapeSettingsConstants.Sound
        || id == EmulationTapeSettings.Prefix + S.VolumeOption;
}
