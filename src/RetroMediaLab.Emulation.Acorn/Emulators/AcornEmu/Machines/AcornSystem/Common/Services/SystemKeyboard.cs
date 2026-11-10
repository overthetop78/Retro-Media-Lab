using RetroMediaLab.Chips.Controllers.Acorn.SystemKeyboard.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Dictionaries;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Constants.SystemKeyboardConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Services;

/// <summary>Clavier ASCII System 2–5 après remise en forme des sorties du codeur, et matrice hexadécimale System 1.</summary>
internal sealed class SystemKeyboard
{
    private IReadOnlySet<EmulationKey> _keys = new HashSet<EmulationKey>();
    private EmulationKey _active = EmulationKey.Unknown;
    private bool _capsLock;
    private bool _shiftLock;
    private readonly bool _asciiKeyboard;
    private readonly SystemKeyboardOutput? _outputCircuit;
    private int _selection;
    private readonly byte[] _matrix = Enumerable.Repeat((byte)C.MatrixMask, C.MatrixRows).ToArray();

    internal SystemKeyboard(int clock, bool asciiKeyboard = true)
    {
        _asciiKeyboard = asciiKeyboard;
        if (!asciiKeyboard) return;
        _outputCircuit = new SystemKeyboardOutput(clock);
        _outputCircuit.StrobeChanged = level => StrobeChanged?.Invoke(level);
        _outputCircuit.DataChanged = data => DataChanged?.Invoke(data);
        _outputCircuit.NextClockEventChanged = next => NextClockEventChanged?.Invoke(next);
    }

    internal bool Strobe => _outputCircuit?.Strobe == true;
    internal bool Blank => _keys.Contains(EmulationKey.RightAlt);
    internal bool Break => _keys.Contains(EmulationKey.Break);
    internal Action<bool>? StrobeChanged { get; set; }
    internal Action<byte>? DataChanged { get; set; }
    internal Action<long?>? NextClockEventChanged { get; set; }
    internal long InputClockCount => _outputCircuit?.ClockCount ?? 0;
    internal long? NextClockEvent => _outputCircuit?.NextClockEvent;
    internal bool TapeOutput => (_selection & C.TapeOutputBit) != 0;
    internal byte Data => _outputCircuit?.Data ?? C.DataMask;
    internal byte DataWithStrobe => (byte)(Data | (Strobe ? 0 : C.StrobeMask));

    internal void SetInput(EmulationInputSnapshot input)
    {
        var next = input.Keys;
        var previous = _keys;
        if (_asciiKeyboard && next.Contains(EmulationKey.CapsLock) && !_keys.Contains(EmulationKey.CapsLock)) _capsLock = !_capsLock;
        if (_asciiKeyboard && next.Contains(EmulationKey.LeftAlt) && !_keys.Contains(EmulationKey.LeftAlt)) _shiftLock = !_shiftLock;
        _keys = new HashSet<EmulationKey>(next);
        if (!_asciiKeyboard)
        {
            UpdateMatrix();
            return;
        }
        var pressed = EmulationKey.Unknown;
        byte? data = null;
        byte pressedData = 0;
        var anyKeyDown = false;
        // Host identities stay in this adapter. A fresh depression replaces the
        // encoded key even while its predecessor is held (physical rollover).
        // An old held key must not be encoded again when the newest is released.
        foreach (var key in SystemKeyboardMap.Characters.Keys)
        {
            if (!_keys.Contains(key) || !TryCharacter(input, key, out var code)) continue;
            anyKeyDown = true;
            if (pressed == EmulationKey.Unknown && !previous.Contains(key))
            {
                pressed = key;
                pressedData = code;
            }
            if (key == _active) data = code;
        }
        if (pressed != EmulationKey.Unknown) { _active = pressed; data = pressedData; }
        else if (!_keys.Contains(_active)) _active = EmulationKey.Unknown;
        _outputCircuit!.SetInputs(data, anyKeyDown, _keys.Contains(EmulationKey.RightControl),
            newKey: pressed != EmulationKey.Unknown);
    }

    private void UpdateMatrix()
    {
        for (var row = 0; row < C.MatrixRows; row++)
        {
            var value = C.MatrixMask;
            for (var column = 0; column < C.MatrixColumns; column++)
            {
                var key = SystemKeyboardMap.HexRows[row][column];
                if (_keys.Contains(key) || SystemKeyboardMap.HexAliases.TryGetValue(key, out var alias) && _keys.Contains(alias))
                    value &= ~(1 << (column + C.MatrixFirstBit));
            }
            _matrix[row] = (byte)value;
        }
    }

    /// <summary>Use the existing host-layout translation; controls and direct hardware input keep their physical mapping.</summary>
    private bool TryCharacter(EmulationInputSnapshot input, EmulationKey key, out byte data)
    {
        var pair = SystemKeyboardMap.Characters[key];
        char code;
        if (pair.Normal >= ' ' && pair.Normal != C.Delete && input.HostCharacters?.TryGetValue(key, out code) == true)
        {
            if (code is < ' ' or > '~') { data = 0; return false; }
        }
        else
        {
            var shifted = _shiftLock || _keys.Contains(EmulationKey.LeftShift) || _keys.Contains(EmulationKey.RightShift);
            code = shifted || _capsLock && char.IsAsciiLetter(pair.Normal) ? pair.Shift : pair.Normal;
        }
        data = (byte)(_keys.Contains(EmulationKey.LeftControl) ? code & C.ControlMask : code);
        return true;
    }

    internal void Tick() => AdvanceTo(checked(InputClockCount + 1));

    internal void AdvanceTo(long clockCount) => _outputCircuit?.AdvanceTo(clockCount);

    internal void Select(byte pins) => _selection = pins & C.OutputMask;

    internal byte ReadMatrix(bool tapeInput)
    {
        return (byte)(_matrix[_selection & C.DigitMask] | _selection | (tapeInput ? C.TapeInputBit : 0));
    }

    internal void Reset()
    {
        _active = EmulationKey.Unknown;
        _keys = new HashSet<EmulationKey>();
        _outputCircuit?.Reset();
        _selection = 0;
        Array.Fill(_matrix, (byte)C.MatrixMask);
    }

}
