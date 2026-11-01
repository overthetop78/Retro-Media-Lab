using AudioFunctions = RetroMediaLab.Emulation.Sharp.Common.Functions.AudioFunctions;
using RetroMediaLab.Emulation;
using System.Globalization;

namespace RetroMediaLab.Emulation.Sharp.Common.Machines.Common.Functions;
internal static class CoreOptionValidationFunctions
{
    internal static IReadOnlyList<CoreOption> ForCore(Emulator core) => core == Emulator.None ? [] : new Engine().Adapter(core).GetSettingsOptions();

    internal static IReadOnlyList<CoreOption> ForConfiguration(MachineConfiguration configuration) => configuration.Core == Emulator.None ? [] : new Engine().Adapter(configuration).GetSettingsOptions(configuration);

    internal static void Validate(MachineConfiguration configuration)
    {
        var catalog = ForCore(configuration.Core);
        foreach (var option in configuration.Options)
        {
            var valid = option.Key switch
            {
                SettingsConstants.AudioOutput => !string.IsNullOrWhiteSpace(option.Value),
                SettingsConstants.AudioLatency => !AudioFunctions.Processing.HasInvalidConfiguredLatency(option.Value),
                VideoAudioSettingsConstants.AudioVolumeOption => !AudioFunctions.Processing.HasInvalidConfiguredVolume(option.Value, VideoAudioSettingsConstants.MinimumVolumePercent, VideoAudioSettingsConstants.MaximumVolumePercent),
                _ => catalog.Any(definition => definition.Key == option.Key && definition.Values.Any(choice => choice.Value == option.Value))
            };
            if (!valid)
                throw new ArgumentException(nameof(configuration.Options));
        }
    }
}