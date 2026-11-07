using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Dictionaries;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Constants.SystemKeyboardConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Services;

/// <summary>Clavier ASCII System 2–5 après remise en forme des sorties du codeur, et matrice hexadécimale System 1.</summary>
internal sealed class SystemKeyboard(int clock)
{
    private IReadOnlySet<EmulationKey> _keys = new HashSet<EmulationKey>();
    private EmulationKey _active = EmulationKey.Unknown;
    private byte _data = C.DataMask;
    private bool _capsLock;
    private bool _shiftLock;
    private long _repeatTicks;
    private int _selection;
    private readonly byte[] _segments = new byte[C.MatrixRows];

    internal bool Strobe { get; private set; }
    internal bool Blank => _keys.Contains(EmulationKey.RightAlt);
    internal bool Break => _keys.Contains(EmulationKey.Break);
    internal Action<bool>? StrobeChanged { get; set; }
    internal ReadOnlySpan<byte> DisplaySegments => _segments;
    internal bool TapeOutput => (_selection & C.TapeOutputBit) != 0;
    internal byte Data => (byte)(_data & C.DataMask);
    internal byte DataWithStrobe => (byte)(Data | (Strobe ? C.StrobeMask : 0));

    internal void SetInput(EmulationInputSnapshot input)
    {
        var next = input.Keys;
        if (next.Contains(EmulationKey.CapsLock) && !_keys.Contains(EmulationKey.CapsLock)) _capsLock = !_capsLock;
        if (next.Contains(EmulationKey.LeftAlt) && !_keys.Contains(EmulationKey.LeftAlt)) _shiftLock = !_shiftLock;
        _keys = new HashSet<EmulationKey>(next);
        var retained = _active != EmulationKey.Unknown && _keys.Contains(_active);
        if (!retained) _active = SystemKeyboardMap.Characters.Keys.FirstOrDefault(_keys.Contains);
        if (_active == EmulationKey.Unknown) { SetStrobe(false); return; }
        var pair = SystemKeyboardMap.Characters[_active];
        var shifted = _shiftLock || _keys.Contains(EmulationKey.LeftShift) || _keys.Contains(EmulationKey.RightShift);
        var code = shifted || _capsLock && char.IsAsciiLetter(pair.Normal) ? pair.Shift : pair.Normal;
        _data = (byte)(_keys.Contains(EmulationKey.LeftControl) ? code & C.ControlMask : code);
        if (retained) return;
        _repeatTicks = (long)clock * C.RepeatDelayMilliseconds / C.MillisecondsPerSecond;
        SetStrobe(true);
    }

    internal void Tick()
    {
        if (_active == EmulationKey.Unknown) return;
        if (_keys.Contains(EmulationKey.RightControl) && _repeatTicks > (long)clock * C.RepeatToggleMilliseconds / C.MillisecondsPerSecond)
            _repeatTicks = 0;
        if (--_repeatTicks > 0) return;
        SetStrobe(!Strobe);
        _repeatTicks = (long)clock * C.RepeatToggleMilliseconds / C.MillisecondsPerSecond;
    }

    internal void Select(byte pins) => _selection = pins & C.OutputMask;

    internal void BeginDisplayFrame() => Array.Clear(_segments);

    internal void WriteSegments(byte pins) => _segments[_selection & C.DigitMask] |= pins;

    internal byte ReadMatrix(bool tapeInput)
    {
        var row = SystemKeyboardMap.HexRows[_selection & C.DigitMask];
        var value = C.MatrixMask;
        for (var column = 0; column < C.MatrixColumns; column++)
            if (_keys.Contains(row[column])) value &= ~(1 << (column + C.MatrixFirstBit));
        return (byte)(value | _selection | (tapeInput ? C.TapeInputBit : 0));
    }

    internal void Reset()
    {
        _active = EmulationKey.Unknown;
        _keys = new HashSet<EmulationKey>();
        _repeatTicks = 0;
        _selection = 0;
        _data = C.DataMask;
        Array.Clear(_segments);
        SetStrobe(false);
    }

    private void SetStrobe(bool value)
    {
        if (Strobe == value) return;
        Strobe = value;
        StrobeChanged?.Invoke(!value);
    }
}
