namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornAtom.Common.Dictionaries;

/// <summary>Matrice Y0–Y9, bits 0–5, du clavier Atom ; Unknown désigne une intersection non câblée.</summary>
internal static class AtomKeyboardMap
{
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
