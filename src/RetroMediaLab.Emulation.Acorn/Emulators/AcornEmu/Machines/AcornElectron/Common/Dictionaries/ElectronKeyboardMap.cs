namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornElectron.Common.Dictionaries;

/// <summary>Lignes actives hautes, sélectionnées par les quatorze bits d’adresse à zéro.</summary>
internal static class ElectronKeyboardMap
{
    internal static IReadOnlyList<EmulationKey[]> Lines { get; } = Array.AsReadOnly<EmulationKey[]>(
    [
        [EmulationKey.Right, EmulationKey.End, EmulationKey.Unknown, EmulationKey.Space],
        [EmulationKey.Left, EmulationKey.Down, EmulationKey.Return, EmulationKey.Backspace],
        [EmulationKey.Minus, EmulationKey.Up, EmulationKey.Quote, EmulationKey.Unknown],
        [EmulationKey.D0, EmulationKey.P, EmulationKey.Semicolon, EmulationKey.Slash],
        [EmulationKey.D9, EmulationKey.O, EmulationKey.L, EmulationKey.Period],
        [EmulationKey.D8, EmulationKey.I, EmulationKey.K, EmulationKey.Comma],
        [EmulationKey.D7, EmulationKey.U, EmulationKey.J, EmulationKey.M],
        [EmulationKey.D6, EmulationKey.Y, EmulationKey.H, EmulationKey.N],
        [EmulationKey.D5, EmulationKey.T, EmulationKey.G, EmulationKey.B],
        [EmulationKey.D4, EmulationKey.R, EmulationKey.F, EmulationKey.V],
        [EmulationKey.D3, EmulationKey.E, EmulationKey.D, EmulationKey.C],
        [EmulationKey.D2, EmulationKey.W, EmulationKey.S, EmulationKey.X],
        [EmulationKey.D1, EmulationKey.Q, EmulationKey.A, EmulationKey.Z],
        [EmulationKey.Escape, EmulationKey.Tab, EmulationKey.LeftControl, EmulationKey.LeftShift]
    ]);
}
