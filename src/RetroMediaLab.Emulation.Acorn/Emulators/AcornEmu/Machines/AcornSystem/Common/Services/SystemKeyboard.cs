using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Dictionaries;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Constants.SystemKeyboardConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Services;

/// <summary>Clavier ASCII System 2–5 après remise en forme des sorties du codeur, et matrice hexadécimale System 1.</summary>
internal sealed class SystemKeyboard(int clock, bool asciiKeyboard = true)
{
    private IReadOnlySet<EmulationKey> _keys = new HashSet<EmulationKey>();
    private EmulationKey _active = EmulationKey.Unknown;
    private byte _data = C.DataMask;
    private bool _capsLock;
    private bool _shiftLock;
    private readonly long _repeatDelay = Math.Max(1, (long)clock * C.RepeatDelayMilliseconds / C.MillisecondsPerSecond);
    private readonly long _repeatInterval = Math.Max(1, (long)clock * C.RepeatToggleMilliseconds / C.MillisecondsPerSecond);
    private long _clockCount;
    private long? _nextRepeat;
    private int _selection;
    private readonly byte[] _matrix = Enumerable.Repeat((byte)C.MatrixMask, C.MatrixRows).ToArray();

    internal bool Strobe { get; private set; }
    internal bool Blank => _keys.Contains(EmulationKey.RightAlt);
    internal bool Break => _keys.Contains(EmulationKey.Break);
    internal Action<bool>? StrobeChanged { get; set; }
    internal Action<byte>? DataChanged { get; set; }
    internal Action<long?>? NextClockEventChanged { get; set; }
    internal long InputClockCount => _clockCount;
    internal long? NextClockEvent => _nextRepeat;
    internal bool TapeOutput => (_selection & C.TapeOutputBit) != 0;
    internal byte Data => (byte)(_data & C.DataMask);
    internal byte DataWithStrobe => (byte)(Data | (Strobe ? 0 : C.StrobeMask));

    internal void SetInput(EmulationInputSnapshot input)
    {
        var next = input.Keys;
        if (asciiKeyboard && next.Contains(EmulationKey.CapsLock) && !_keys.Contains(EmulationKey.CapsLock)) _capsLock = !_capsLock;
        if (asciiKeyboard && next.Contains(EmulationKey.LeftAlt) && !_keys.Contains(EmulationKey.LeftAlt)) _shiftLock = !_shiftLock;
        _keys = new HashSet<EmulationKey>(next);
        if (!asciiKeyboard)
        {
            UpdateMatrix();
            return;
        }
        var retained = _active != EmulationKey.Unknown && _keys.Contains(_active);
        if (!retained) _active = SystemKeyboardMap.Characters.Keys.FirstOrDefault(key => _keys.Contains(key) && TryCharacter(input, key, out _));
        if (_active == EmulationKey.Unknown || !TryCharacter(input, _active, out var code))
        {
            _active = EmulationKey.Unknown;
            ScheduleRepeat(null);
            SetStrobe(false);
            return;
        }
        SetData(code);
        var repeatHeld = _keys.Contains(EmulationKey.RightControl);
        if (retained)
        {
            if (repeatHeld && _nextRepeat is { } due && due - _clockCount > _repeatInterval)
                ScheduleRepeat(checked(_clockCount + 1));
            return;
        }
        var delay = repeatHeld && _repeatDelay > _repeatInterval ? 1 : _repeatDelay;
        ScheduleRepeat(checked(_clockCount + delay));
        SetStrobe(true);
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

    internal void Tick() => AdvanceTo(checked(_clockCount + 1));

    /// <summary>Référence absolue du codeur ; visite chaque front de répétition, sans appels entre échéances.</summary>
    internal void AdvanceTo(long clockCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(clockCount, _clockCount);
        while (_nextRepeat is { } due && due <= clockCount)
        {
            _clockCount = due;
            ScheduleRepeat(checked(due + _repeatInterval));
            SetStrobe(!Strobe);
        }
        _clockCount = clockCount;
    }

    private void ScheduleRepeat(long? clockCount)
    {
        if (_nextRepeat == clockCount) return;
        _nextRepeat = clockCount;
        NextClockEventChanged?.Invoke(clockCount);
    }

    internal void Select(byte pins) => _selection = pins & C.OutputMask;

    internal byte ReadMatrix(bool tapeInput)
    {
        return (byte)(_matrix[_selection & C.DigitMask] | _selection | (tapeInput ? C.TapeInputBit : 0));
    }

    internal void Reset()
    {
        _active = EmulationKey.Unknown;
        _keys = new HashSet<EmulationKey>();
        ScheduleRepeat(null);
        _selection = 0;
        SetData(C.DataMask);
        Array.Fill(_matrix, (byte)C.MatrixMask);
        SetStrobe(false);
    }

    private void SetData(byte value)
    {
        if (_data == value) return;
        _data = value;
        DataChanged?.Invoke(Data);
    }

    private void SetStrobe(bool value)
    {
        if (Strobe == value) return;
        Strobe = value;
        StrobeChanged?.Invoke(!value);
    }
}
