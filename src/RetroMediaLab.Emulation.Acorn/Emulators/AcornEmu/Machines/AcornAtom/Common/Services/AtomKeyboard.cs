using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornAtom.Common.Dictionaries;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornAtom.Common.Constants.AtomConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornAtom.Common.Services;

/// <summary>Matrice Atom lue par le PPI, sans conversion en caractères par le cœur.</summary>
internal sealed class AtomKeyboard
{
    private IReadOnlySet<EmulationKey> _keys = new HashSet<EmulationKey>();
    private int _column;

    internal bool Break => _keys.Contains(EmulationKey.Break);
    internal bool Repeat => _keys.Contains(EmulationKey.RightControl);
    internal void Select(byte pins) => _column = pins & C.KeyboardColumnMask;
    internal void SetInput(EmulationInputSnapshot input) => _keys = new HashSet<EmulationKey>(input.Keys);

    internal byte Read()
    {
        var pins = byte.MaxValue;
        if (_column < AtomKeyboardMap.Columns.Count)
        {
            var column = AtomKeyboardMap.Columns[_column];
            for (var row = 0; row < column.Length; row++)
                if (Pressed(column[row])) pins &= (byte)~(1 << row);
        }
        if (_keys.Contains(EmulationKey.LeftControl)) pins &= unchecked((byte)~C.KeyboardControl);
        if (_keys.Contains(EmulationKey.LeftShift) || _keys.Contains(EmulationKey.RightShift)
            || _keys.Contains(EmulationKey.Down) || _keys.Contains(EmulationKey.Left))
            pins &= unchecked((byte)~C.KeyboardShift);
        return pins;
    }

    internal void Reset()
    {
        _keys = new HashSet<EmulationKey>();
        _column = 0;
    }

    private bool Pressed(EmulationKey key) => key != EmulationKey.Unknown && (_keys.Contains(key)
        || key == EmulationKey.Up && _keys.Contains(EmulationKey.Down)
        || key == EmulationKey.Right && _keys.Contains(EmulationKey.Left));
}
