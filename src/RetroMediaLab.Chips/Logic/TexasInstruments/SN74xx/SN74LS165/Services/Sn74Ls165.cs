using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS165.Interfaces;
using C = RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS165.Constants.Sn74Ls165Constants;

namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS165.Services;

/// <summary>TI DL-S 7611375: asynchronous parallel load, gated clock, QH and complemented QH.</summary>
/// <remarks>The effective positive clock is SH/LD AND (CLK OR CLK INH).
/// Its rising edge is the falling NOR output shown in the original circuit.
/// No power-on state or reset is invented; unknown bits propagate through the stages.</remarks>
public sealed class Sn74Ls165 : ISn74Ls165
{
    private long _inputClockCount;
    private long _shiftClockCount;
    private bool _clockHigh;
    private bool _clockInhibitHigh;
    private bool _shiftLoadHigh = true;
    private bool _gatedClockHigh;
    private bool _clockPending;
    private byte _parallelData;
    private bool? _serialInput;
    private byte _register;
    private byte _knownBits;
    private bool? _publishedOutput;

    public Sn74Ls165(long clockFrequency, byte? initialData = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(clockFrequency);
        ClockFrequency = clockFrequency;
        _register = initialData.GetValueOrDefault();
        _knownBits = initialData.HasValue ? byte.MaxValue : (byte)0;
        _publishedOutput = SerialOutput;
    }

    public long ClockFrequency { get; }
    public long InputClockCount => _inputClockCount;
    public long ShiftClockCount => _shiftClockCount;
    public bool ClockHigh => _clockHigh;
    public bool ClockInhibitHigh => _clockInhibitHigh;
    public bool ShiftLoadHigh => _shiftLoadHigh;
    public bool GatedClockHigh => _gatedClockHigh;
    public byte ParallelData => _parallelData;
    public bool? SerialInput => _serialInput;
    public byte? StoredData => _knownBits == byte.MaxValue ? _register : null;
    public byte KnownBits => _knownBits;
    public bool? SerialOutput => (_knownBits & C.LastStageMask) != 0
        ? (_register & C.LastStageMask) != 0 : null;
    public bool? ComplementaryOutput => !SerialOutput;
    public Action<bool?, bool?>? OutputsChanged { get; set; }

    public void SetParallelData(byte data)
    {
        _parallelData = data;
        if (!_shiftLoadHigh) LoadParallel();
    }

    public void SetSerialInput(bool? high) => _serialInput = high;

    public void SetShiftLoad(bool high)
    {
        if (_shiftLoadHigh == high) return;
        _shiftLoadHigh = high;
        ResolveGatedClock();
        if (!high) LoadParallel();
    }

    public void SetClockInhibit(bool high)
    {
        if (_clockInhibitHigh == high) return;
        _clockInhibitHigh = high;
        ResolveGatedClock();
    }

    public void SetClock(bool high)
    {
        CaptureClock(high);
        ResolveClock();
    }

    /// <summary>Capture shared CLK before any register on that wire publishes outputs.</summary>
    public void CaptureClock(bool high)
    {
        if (_clockHigh == high) return;
        if (_clockPending) throw new InvalidOperationException(nameof(_clockPending));
        if (!high) { ReceiveFallingClock(); return; }
        _clockHigh = true;
        _inputClockCount = checked(_inputClockCount + 1);
        _clockPending = true;
        ResolveGatedClock();
    }

    /// <summary>Publish the already captured edge, without advancing physical time.</summary>
    public void ResolveClock()
    {
        if (!_clockPending) return;
        _clockPending = false;
        PublishOutputs();
    }

    public void Clock()
    {
        RequireLowClock();
        CaptureClock(true);
        ResolveClock();
        ReceiveFallingClock();
    }

    // Falling CLK cannot raise SH/LD AND (CLK OR INH). It still reaches the
    // physical input and updates the internal gate level, without sampling data.
    private void ReceiveFallingClock()
    {
        _clockHigh = false;
        _gatedClockHigh = _shiftLoadHigh && _clockInhibitHigh;
    }

    /// <summary>Stable inputs between callbacks; retained outputs may feed another stage.</summary>
    public void AdvanceClocks(long clocks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(clocks);
        RequireLowClock();
        while (clocks > 0)
        {
            if (!_shiftLoadHigh || _clockInhibitHigh)
            {
                _inputClockCount = checked(_inputClockCount + clocks);
                return;
            }
            var settledValue = _serialInput == true ? byte.MaxValue : (byte)0;
            var settledKnown = _serialInput.HasValue ? byte.MaxValue : (byte)0;
            if (_register == settledValue && _knownBits == settledKnown)
            {
                _inputClockCount = checked(_inputClockCount + clocks);
                _shiftClockCount = checked(_shiftClockCount + clocks);
                return;
            }
            Clock();
            clocks--;
        }
    }

    private void ResolveGatedClock()
    {
        var high = _shiftLoadHigh && (_clockHigh || _clockInhibitHigh);
        if (_gatedClockHigh == high) return;
        _gatedClockHigh = high;
        if (!high) return;
        _shiftClockCount = checked(_shiftClockCount + 1);
        _register = (byte)((_register << 1) | (_serialInput == true ? 1 : 0));
        _knownBits = (byte)((_knownBits << 1) | (_serialInput.HasValue ? 1 : 0));
        PublishOutputs();
    }

    private void LoadParallel()
    {
        _register = _parallelData;
        _knownBits = byte.MaxValue;
        PublishOutputs();
    }

    private void PublishOutputs()
    {
        if (_clockPending) return;
        var high = SerialOutput;
        if (_publishedOutput == high) return;
        _publishedOutput = high;
        OutputsChanged?.Invoke(high, !high);
    }

    private void RequireLowClock()
    {
        if (_clockHigh) throw new InvalidOperationException(nameof(ClockHigh));
    }
}
