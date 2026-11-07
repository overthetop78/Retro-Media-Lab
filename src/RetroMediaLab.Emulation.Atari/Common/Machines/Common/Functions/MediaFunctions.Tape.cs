using RetroMediaLab.Emulation;

namespace RetroMediaLab.Emulation.Atari.Common.Machines.Common.Functions;

internal static class TapeBootFunctions
{
    internal static bool IsRequested(MachineConfiguration configuration)
    {
        var configured = configuration.Options.GetValueOrDefault(EightBitSettingsConstants.TapeBootOptionKey);
        return string.Equals(configured, EightBitSettingsConstants.Enabled, StringComparison.OrdinalIgnoreCase) || configuration.Media.Any(media => media.Category == MediaCategory.Tape && media.IsInserted && media.TapeAutoBoot);
    }

}

internal static class TapeStateFunctions
{
    internal static EmulationTapeState From(bool mounted, bool motorActive) => !mounted ? EmulationTapeState.Empty : motorActive ? EmulationTapeState.Playing : EmulationTapeState.Stopped;
}
