namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornAtom.Common.Dictionaries;

/// <summary>Matrice Y0–Y9, bits 0–5, du clavier Atom ; Unknown désigne une intersection non câblée.</summary>
internal static class AtomKeyboardMap
{
    /// <summary>Symboles des touches Atom, distincts des noms des touches hôtes.</summary>
    internal static IReadOnlyDictionary<char, (EmulationKey Key, bool Shift)> Characters { get; } = CreateCharacters();

    internal static char KeypadCharacter(EmulationKey key) => key switch
    {
        >= EmulationKey.Numpad0 and <= EmulationKey.Numpad9 => (char)('0' + (int)key - (int)EmulationKey.Numpad0),
        EmulationKey.NumpadPlus => '+',
        EmulationKey.NumpadMinus => '-',
        EmulationKey.NumpadMultiply => '*',
        EmulationKey.NumpadDivide => '/',
        EmulationKey.NumpadPeriod => '.',
        _ => '\0'
    };

    private static IReadOnlyDictionary<char, (EmulationKey Key, bool Shift)> CreateCharacters()
    {
        var map = new Dictionary<char, (EmulationKey, bool)>();
        for (var digit = 0; digit < 10; digit++) map.Add((char)('0' + digit), (EmulationKey.D0 + digit, false));
        const string shiftedDigits = "!\"#$%&'()";
        for (var digit = 1; digit < 10; digit++) map.Add(shiftedDigits[digit - 1], (EmulationKey.D0 + digit, true));
        void Pair(EmulationKey key, char normal, char shifted)
        {
            map.Add(normal, (key, false));
            map.Add(shifted, (key, true));
        }
        Pair(EmulationKey.Minus, '-', '=');
        Pair(EmulationKey.Comma, ',', '<');
        Pair(EmulationKey.Semicolon, ';', '+');
        Pair(EmulationKey.Equals, ':', '*');
        Pair(EmulationKey.Slash, '/', '?');
        Pair(EmulationKey.Period, '.', '>');
        map.Add('@', (EmulationKey.LeftBracket, false));
        map.Add('[', (EmulationKey.Quote, false));
        map.Add(']', (EmulationKey.Backslash, false));
        map.Add('\\', (EmulationKey.RightBracket, false));
        map.Add('^', (EmulationKey.Backspace, false));
        map.Add(' ', (EmulationKey.Space, false));
        return map;
    }

    internal static IReadOnlyList<EmulationKey[]> Columns { get; } = Array.AsReadOnly<EmulationKey[]>(
    [
        [EmulationKey.Unknown, EmulationKey.D3, EmulationKey.Minus, EmulationKey.G, EmulationKey.Q, EmulationKey.Escape],
        [EmulationKey.Unknown, EmulationKey.D2, EmulationKey.Comma, EmulationKey.F, EmulationKey.P, EmulationKey.Z],
        [EmulationKey.Up, EmulationKey.D1, EmulationKey.Semicolon, EmulationKey.E, EmulationKey.O, EmulationKey.Y],
        [EmulationKey.Right, EmulationKey.D0, EmulationKey.Equals, EmulationKey.D, EmulationKey.N, EmulationKey.X],
        [EmulationKey.CapsLock, EmulationKey.Delete, EmulationKey.D9, EmulationKey.C, EmulationKey.M, EmulationKey.W],
        [EmulationKey.Backspace, EmulationKey.Tab, EmulationKey.D8, EmulationKey.B, EmulationKey.L, EmulationKey.V],
        [EmulationKey.Backslash, EmulationKey.Return, EmulationKey.D7, EmulationKey.A, EmulationKey.K, EmulationKey.U],
        [EmulationKey.RightBracket, EmulationKey.Unknown, EmulationKey.D6, EmulationKey.LeftBracket, EmulationKey.J, EmulationKey.T],
        [EmulationKey.Quote, EmulationKey.Unknown, EmulationKey.D5, EmulationKey.Slash, EmulationKey.I, EmulationKey.S],
        [EmulationKey.Space, EmulationKey.Unknown, EmulationKey.D4, EmulationKey.Period, EmulationKey.H, EmulationKey.R]
    ]);
}
