using RetroMediaLab.Emulation;

namespace RetroMediaLab.Emulation.Atari.Common.Machines.Atari8Bit.Constants;

internal static class Atari8BitKeyboardConstants
{
    internal static readonly IReadOnlyList<EmulationKey> SpecialKeys =
    [
        EmulationKey.AtariOption,
        EmulationKey.AtariSelect,
        EmulationKey.AtariStart,
        EmulationKey.Help,
        EmulationKey.Break
    ];

}
