using RetroMediaLab.Emulation;

namespace RetroMediaLab.Emulation.Atari.Common.Machines.AtariST.Dictionaries;

internal static class AtariStKeyboardDictionary
{
    internal static readonly IReadOnlyDictionary<EmulationKey, EmulationKey> DefaultHostKeys =
        new Dictionary<EmulationKey, EmulationKey>
        {
            [EmulationKey.Help] = EmulationKey.Insert,
            [EmulationKey.Undo] = EmulationKey.Home,
            [EmulationKey.Break] = EmulationKey.End
        };
}
