using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Constants.SystemKeyboardConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Dictionaries;

internal static class SystemKeyboardMap
{
    /// <summary>Sources hôtes supplémentaires des contacts existants : pavé numérique et commandes M/G du moniteur.</summary>
    internal static IReadOnlyDictionary<EmulationKey, EmulationKey> HexAliases { get; } =
        new System.Collections.ObjectModel.ReadOnlyDictionary<EmulationKey, EmulationKey>(new Dictionary<EmulationKey, EmulationKey>
        {
            [EmulationKey.D0] = EmulationKey.Numpad0, [EmulationKey.D1] = EmulationKey.Numpad1,
            [EmulationKey.D2] = EmulationKey.Numpad2, [EmulationKey.D3] = EmulationKey.Numpad3,
            [EmulationKey.D4] = EmulationKey.Numpad4, [EmulationKey.D5] = EmulationKey.Numpad5,
            [EmulationKey.D6] = EmulationKey.Numpad6, [EmulationKey.D7] = EmulationKey.Numpad7,
            [EmulationKey.D8] = EmulationKey.Numpad8, [EmulationKey.D9] = EmulationKey.Numpad9,
            [EmulationKey.Minus] = EmulationKey.M, [EmulationKey.X] = EmulationKey.G
        });

    internal static IReadOnlyDictionary<EmulationKey, (char Normal, char Shift)> Characters { get; } =
        new System.Collections.ObjectModel.ReadOnlyDictionary<EmulationKey, (char, char)>(new Dictionary<EmulationKey, (char, char)>
        {
            [EmulationKey.A] = ('a', 'A'), [EmulationKey.B] = ('b', 'B'),
            [EmulationKey.C] = ('c', 'C'), [EmulationKey.D] = ('d', 'D'),
            [EmulationKey.E] = ('e', 'E'), [EmulationKey.F] = ('f', 'F'),
            [EmulationKey.G] = ('g', 'G'), [EmulationKey.H] = ('h', 'H'),
            [EmulationKey.I] = ('i', 'I'), [EmulationKey.J] = ('j', 'J'),
            [EmulationKey.K] = ('k', 'K'), [EmulationKey.L] = ('l', 'L'),
            [EmulationKey.M] = ('m', 'M'), [EmulationKey.N] = ('n', 'N'),
            [EmulationKey.O] = ('o', 'O'), [EmulationKey.P] = ('p', 'P'),
            [EmulationKey.Q] = ('q', 'Q'), [EmulationKey.R] = ('r', 'R'),
            [EmulationKey.S] = ('s', 'S'), [EmulationKey.T] = ('t', 'T'),
            [EmulationKey.U] = ('u', 'U'), [EmulationKey.V] = ('v', 'V'),
            [EmulationKey.W] = ('w', 'W'), [EmulationKey.X] = ('x', 'X'),
            [EmulationKey.Y] = ('y', 'Y'), [EmulationKey.Z] = ('z', 'Z'),
            [EmulationKey.D0] = ('0', '0'), [EmulationKey.D1] = ('1', '!'),
            [EmulationKey.D2] = ('2', '"'), [EmulationKey.D3] = ('3', '#'),
            [EmulationKey.D4] = ('4', '$'), [EmulationKey.D5] = ('5', '%'),
            [EmulationKey.D6] = ('6', '&'), [EmulationKey.D7] = ('7', '\''),
            [EmulationKey.D8] = ('8', '('), [EmulationKey.D9] = ('9', ')'),
            [EmulationKey.Comma] = (',', '<'), [EmulationKey.Period] = ('.', '>'),
            [EmulationKey.Minus] = ('-', '='), [EmulationKey.Equals] = (';', '+'),
            [EmulationKey.Semicolon] = (':', '*'), [EmulationKey.Slash] = ('/', '?'),
            [EmulationKey.Quote] = ('@', '\''), [EmulationKey.Backslash] = ('^', '~'),
            [EmulationKey.Oem102] = ('\\', '|'),
            [EmulationKey.LeftBracket] = ('[', '['), [EmulationKey.RightBracket] = (']', ']'),
            [EmulationKey.Space] = (' ', ' '),
            [EmulationKey.Return] = ((char)C.Return, (char)C.Return),
            [EmulationKey.Backspace] = ((char)C.Backspace, (char)C.Backspace),
            [EmulationKey.Delete] = ((char)C.Delete, (char)C.Delete),
            [EmulationKey.Tab] = ((char)C.Tab, (char)C.Tab),
            [EmulationKey.Insert] = ((char)C.LineFeed, (char)C.LineFeed),
            [EmulationKey.Escape] = ((char)C.Escape, (char)C.Escape)
        });

    internal static IReadOnlyList<EmulationKey[]> HexRows { get; } = Array.AsReadOnly<EmulationKey[]>(
    [
        [EmulationKey.D8, EmulationKey.Minus, EmulationKey.D0],
        [EmulationKey.D9, EmulationKey.X, EmulationKey.D1],
        [EmulationKey.A, EmulationKey.P, EmulationKey.D2],
        [EmulationKey.B, EmulationKey.S, EmulationKey.D3],
        [EmulationKey.C, EmulationKey.L, EmulationKey.D4],
        [EmulationKey.D, EmulationKey.R, EmulationKey.D5],
        [EmulationKey.E, EmulationKey.Up, EmulationKey.D6],
        [EmulationKey.F, EmulationKey.Down, EmulationKey.D7]
    ]);
}
