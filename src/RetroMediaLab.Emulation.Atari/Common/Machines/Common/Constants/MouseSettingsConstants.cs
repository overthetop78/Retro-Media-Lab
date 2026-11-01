using RetroMediaLab.Emulation.Constants;

namespace RetroMediaLab.Emulation.Atari.Common.Machines.Common.Constants;


// Unique to RetroMediaLab.Emulation.Atari.
internal static class MouseSettingsConstants
{
    internal const string SpeedOptionKey = MachineOptionConstants.PointerSpeed;
    internal const string MappingOptionPrefix = "retromedialab_atari_mouse_";
    internal const int DefaultSpeedPercent = 100;
    internal const int MinimumSpeedPercent = 25;
    internal const int MaximumSpeedPercent = 200;
    internal const int SpeedStepPercent = 25;

    internal static readonly IReadOnlyList<string> Actions = ["Left", "Right"];
}
