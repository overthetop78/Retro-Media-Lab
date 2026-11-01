using RetroMediaLab.Emulation;

namespace RetroMediaLab.Emulation.Atari.Common.Machines.AtariST.Constants;

internal static class AtariStKeyboardConstants
{
    internal static readonly IReadOnlyList<EmulationKey> SpecialKeys = [EmulationKey.Help, EmulationKey.Undo, EmulationKey.Break];
}
