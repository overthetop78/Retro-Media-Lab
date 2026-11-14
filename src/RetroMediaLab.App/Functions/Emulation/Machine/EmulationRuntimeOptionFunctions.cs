using RetroMediaLab.Emulation;

namespace RetroMediaLab.App.Functions.Emulation.Machine;

internal static class EmulationRuntimeOptionFunctions
{
    internal static IReadOnlyDictionary<string, string> HotChanges(IReadOnlyDictionary<string, string> configured, IReadOnlyList<EmulationOption> available)
    {
        var hotOptions = available.Where(option => !option.RequiresRestart).ToDictionary(option => option.Key, StringComparer.Ordinal);
        return configured.Where(option => hotOptions.TryGetValue(option.Key, out var current)
                && !string.Equals(current.CurrentValue, option.Value, StringComparison.Ordinal))
            .ToDictionary(option => option.Key, option => option.Value, StringComparer.Ordinal);
    }
}
