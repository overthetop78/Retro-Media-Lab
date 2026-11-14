namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Constants;

internal static class MachineOptionConstants
{
    internal static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> All { get; } =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);
}
