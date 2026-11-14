using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Constants;
using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Dictionaries;

namespace RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Functions;

internal static class CoreSettingsFunctions
{

    internal static MachineConfiguration Configure(MachineConfiguration configuration)
    {
        var options = new Dictionary<string, string>(configuration.Options ?? new Dictionary<string, string>(), StringComparer.Ordinal);
        foreach (var option in CoreCatalog.Get(configuration.EmulatorId).Options)
        {
            var selected = option.IsVisible && (option.MachineIds is null || option.MachineIds.Contains(configuration.Model)) ? configuration.Options?.GetValueOrDefault(option.Key) ?? option.DefaultValue : option.DefaultValue;
            options[option.Key] = AvailableValues(option, configuration.Model).Any(value => value.Value == selected) ? selected : option.DefaultValue;
        }
        if (CoreCatalog.Get(configuration.EmulatorId).MachineOptions?.GetValueOrDefault(configuration.Model) is { } forced)
            foreach (var option in forced) options[option.Key] = option.Value;
        return configuration with { Options = options };
    }

    internal static IReadOnlyList<EmulationSettingsBlock> Blocks(MachineConfiguration configuration)
    {
        var definition = CoreCatalog.Get(configuration.EmulatorId);
        var advancedAudio = definition.Options.FirstOrDefault(option => option.Key.EndsWith("_show_advanced_audio_settings", StringComparison.Ordinal));
        bool showAdvancedAudio = advancedAudio is null
            || (configuration.Options?.GetValueOrDefault(advancedAudio.Key) ?? advancedAudio.DefaultValue) == "enabled";
        var fields = definition.Options.Where(option => option.IsVisible
            && (option.MachineIds is null || option.MachineIds.Contains(configuration.Model))
            && definition.MachineOptions?.GetValueOrDefault(configuration.Model)?.ContainsKey(option.Key) != true).Select(option => new EmulationSettingsField(
            option.Key, option.Tab, CoreSettingsConstants.OptionsBlock + option.Tab,
            option.Name, EmulationSettingsEditor.Selection,
            configuration.Options?.GetValueOrDefault(option.Key) ?? option.DefaultValue,
            AvailableValues(option, configuration.Model).Select(value => new EmulationSettingsChoice(value.Value,
                value.Label.StartsWith(CoreSettingsConstants.OptionResourcePrefix, StringComparison.Ordinal) ? value.Label : string.Empty,
                value.Label.StartsWith(CoreSettingsConstants.OptionResourcePrefix, StringComparison.Ordinal) ? null : value.Label)).ToArray(),
            IsVisible: showAdvancedAudio || option.Tab != EmulationMachineTab.Audio
                || !option.Key.Contains("_channel_", StringComparison.Ordinal) || !option.Key.EndsWith("_volume", StringComparison.Ordinal),
            ExplanationResourceKey: option.Description,
            RefreshSettingsOnChange: option == advancedAudio,
            RequiresRestart: option.RequiresRestart)).ToArray();
        return fields.GroupBy(field => field.Tab).Select(group => new EmulationSettingsBlock(
            CoreSettingsConstants.OptionsBlock + group.Key, group.Key,
            CoreSettingsConstants.Sections[group.Key].Title, group.ToArray(),
            CoreSettingsConstants.Sections[group.Key].Icon, CoreSettingsConstants.OptionsColumnCount)).ToArray();
    }

    private static IEnumerable<CoreOptionValue> AvailableValues(CoreOption option, string machineId) => option.Values.Where(value => value.MachineIds is null || value.MachineIds.Contains(machineId));
}
