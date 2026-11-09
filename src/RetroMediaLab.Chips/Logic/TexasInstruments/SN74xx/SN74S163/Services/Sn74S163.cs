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
    private bool _enableT;
    private byte _data;
    private byte _publishedCount;
    private bool _publishedCarry;

    public long ClockFrequency { get; }
    public long InputClockCount { get; private set; }
    public bool ClockHigh { get; private set; }
    public byte Count { get; private set; }
    public bool Carry => _enableT && Count == C.CounterMask;
    public Action<byte, bool>? OutputsChanged { get; set; }

    public Sn74S163(long clockFrequency, byte initialCount = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(clockFrequency);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(initialCount, C.CounterMask);
        ClockFrequency = clockFrequency;
        Count = _publishedCount = initialCount;
    }

    public void SetClear(bool high) => _clearHigh = high;
    public void SetLoad(bool high) => _loadHigh = high;
    public void SetData(byte data) => _data = (byte)(data & C.CounterMask);

    public void SetEnable(bool p, bool t)
    {
        _enableP = p;
        _enableT = t;
        // ENT qualifies RCO combinationally, even if ENP inhibits counting.
        PublishOutputs();
    }

    public void SetClock(bool high)
    {
        if (ClockHigh == high) return;
        ClockHigh = high;
        if (!high) return;
        InputClockCount = checked(InputClockCount + 1);
        Count = NextCount();
        PublishOutputs();
    }

    /// <summary>One complete CLK period starting and ending low.</summary>
    public void Clock()
    {
        RequireLowClock();
        SetClock(true);
        SetClock(false);
    }

    /// <summary>Complete periods with stable inputs. Every connected output transition remains observable.</summary>
    public void AdvanceClocks(long clocks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(clocks);
        RequireLowClock();
        while (clocks > 0)
        {
            var next = NextCount();
            if (OutputsChanged is null || next == Count)
            {
                InputClockCount = checked(InputClockCount + clocks);
                Count = !_clearHigh ? (byte)0 : !_loadHigh ? _data
                    : _enableP && _enableT ? (byte)((Count + (clocks & C.CounterMask)) & C.CounterMask) : Count;
                PublishOutputs();
                return;
            }
            Clock();
            clocks--;
        }
    }

    private byte NextCount() => !_clearHigh ? (byte)0 : !_loadHigh ? _data
        : _enableP && _enableT ? (byte)((Count + 1) & C.CounterMask) : Count;

    private void RequireLowClock()
    {
        if (ClockHigh) throw new InvalidOperationException(nameof(ClockHigh));
    }

    private void PublishOutputs()
    {
        var carry = Carry;
        if (_publishedCount == Count && _publishedCarry == carry) return;
        _publishedCount = Count;
        _publishedCarry = carry;
        OutputsChanged?.Invoke(Count, carry);
    }
}
