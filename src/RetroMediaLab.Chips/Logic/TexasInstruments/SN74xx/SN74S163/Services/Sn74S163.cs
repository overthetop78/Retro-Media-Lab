using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74S163.Interfaces;
using C = RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74S163.Constants.Sn74S163Constants;

namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74S163.Services;

/// <summary>TI SN74S163: four synchronous flip-flops, synchronous CLR/LOAD and combinational RCO.</summary>
/// <remarks>The initial count is a model seed, not a physical power-up reset.
/// The board supplies CLK and electrical input levels; no host resource or other chip is queried.</remarks>
public sealed class Sn74S163 : ISn74S163
{
    private bool _clearHigh = true;
    private bool _loadHigh = true;
    private bool _enableP;
    private byte _enableTMask;
    private byte _data;
    private byte _publishedOutputs;
    private bool _clockPending;
    private byte _capturedCount;
    private long _inputClockCount;
    private bool _clockHigh;
    private byte _count;
    private Action<byte, bool>? _outputsChanged;

    public long ClockFrequency { get; }
    public long InputClockCount => _inputClockCount;
    public bool ClockHigh => _clockHigh;
    public byte Count => _count;
    public bool Carry => ((_count + 1) & _enableTMask) != 0;
    public Action<byte, bool>? OutputsChanged { get => _outputsChanged; set => _outputsChanged = value; }

    public Sn74S163(long clockFrequency, byte initialCount = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(clockFrequency);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(initialCount, C.CounterMask);
        ClockFrequency = clockFrequency;
        _count = _publishedOutputs = initialCount;
    }

    public void SetClear(bool high) => _clearHigh = high;
    public void SetLoad(bool high) => _loadHigh = high;
    public void SetData(byte data) => _data = (byte)(data & C.CounterMask);

    public void SetEnable(bool p, bool t)
    {
        _enableP = p;
        _enableTMask = t ? C.CarryMask : (byte)0;
        // ENT qualifies RCO combinationally, even if ENP inhibits counting.
        if (!_clockPending) ResolveClock();
    }

    public void SetClock(bool high)
    {
        CaptureClock(high);
        ResolveClock();
    }

    // Shared CLK: every receiver captures its old input levels before connected
    // outputs resolve. Both operations belong to the same physical edge.
    internal void CaptureClock(bool high)
    {
        if (_clockHigh == high) return;
        if (_clockPending) throw new InvalidOperationException(nameof(_clockPending));
        if (high) CaptureRisingClock();
        else ReceiveFallingClock();
    }

    // The owner of a complete period has already established CLK low and no
    // pending edge. Shared sources capture every receiver before resolving any.
    internal void CaptureRisingClock()
    {
        _clockHigh = true;
        _inputClockCount = checked(_inputClockCount + 1);
        _capturedCount = !_clearHigh ? (byte)0 : !_loadHigh ? _data
            : _enableP && _enableTMask != 0 ? (byte)((_count + 1) & C.CounterMask) : _count;
        _clockPending = true;
    }

    // Used only after the rising edge has been resolved. This real falling edge
    // updates the pin; positive-edge flip-flops do not sample their data again.
    internal void ReceiveFallingClock() => _clockHigh = false;

    internal void ResolveClock()
    {
        if (_clockPending)
        {
            _count = _capturedCount;
            _clockPending = false;
        }
        // RCO is ENT AND all four count bits: count+1 projects that condition
        // into bit 4. Compare the resolved pins before decoding the callback.
        var outputs = (byte)(_count | ((_count + 1) & _enableTMask));
        if (_publishedOutputs == outputs) return;
        _publishedOutputs = outputs;
        _outputsChanged?.Invoke(_count, (outputs & C.CarryMask) != 0);
    }

    /// <summary>One complete CLK period starting and ending low.</summary>
    public void Clock()
    {
        RequireLowClock();
        ClockPeriod();
    }

    /// <summary>Complete periods with stable inputs. Every connected output transition remains observable.</summary>
    public void AdvanceClocks(long clocks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(clocks);
        RequireLowClock();
        while (clocks > 0)
        {
            var next = NextCount();
            if (_outputsChanged is null || next == _count)
            {
                _inputClockCount = checked(_inputClockCount + clocks);
                _count = !_clearHigh ? (byte)0 : !_loadHigh ? _data
                    : _enableP && _enableTMask != 0 ? (byte)((_count + (clocks & C.CounterMask)) & C.CounterMask) : _count;
                ResolveClock();
                return;
            }
            ClockPeriod();
            clocks--;
        }
    }

    private void ClockPeriod()
    {
        CaptureRisingClock();
        ResolveClock();
        ReceiveFallingClock();
    }

    private byte NextCount() => !_clearHigh ? (byte)0 : !_loadHigh ? _data
        : _enableP && _enableTMask != 0 ? (byte)((_count + 1) & C.CounterMask) : _count;

    private void RequireLowClock()
    {
        if (_clockHigh) throw new InvalidOperationException(nameof(ClockHigh));
    }

}
