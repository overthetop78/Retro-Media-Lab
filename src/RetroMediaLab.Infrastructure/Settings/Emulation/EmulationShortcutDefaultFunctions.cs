namespace RetroMediaLab.Infrastructure.Settings.Emulation;

public static class EmulationShortcutDefaultFunctions
{
    public static Dictionary<string, string> Create() => new(
        EmulationShortcutDefaults.Values.Concat(EmulationShortcutDefaults.OsdValues), StringComparer.Ordinal);
}
