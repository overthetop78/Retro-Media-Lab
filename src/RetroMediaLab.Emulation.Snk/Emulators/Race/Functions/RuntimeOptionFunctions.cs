using RetroMediaLab.Emulation.Snk.Emulators.Race.Constants;
using RetroMediaLab.Emulation.Snk.Emulators.Race.Dictionaries;

namespace RetroMediaLab.Emulation.Snk.Emulators.Race.Functions;

internal static class RuntimeOptionFunctions
{
    internal static IReadOnlyDictionary<string, string> Prepare(MachineConfiguration configuration) => OptionCatalog.Options.ToDictionary(option => option.Key, option => configuration.Options.GetValueOrDefault(option.Key, option.DefaultValue), StringComparer.Ordinal);
    internal static IReadOnlyList<CoreOption> Describe(MachineConfiguration configuration)
    {
        var options = Prepare(configuration);
        return OptionCatalog.Options.Select(option => option with { CurrentValue = options[option.Key], IsEnabled = option.Key != RuntimeOptionConstants.FrameskipThreshold || options[RuntimeOptionConstants.Frameskip] == RuntimeOptionConstants.ManualFrameskip }).ToArray();
    }
}
