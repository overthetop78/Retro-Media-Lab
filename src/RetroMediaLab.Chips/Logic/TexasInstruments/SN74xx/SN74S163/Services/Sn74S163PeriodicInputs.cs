using C = RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74S163.Constants.Sn74S163Constants;

namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74S163.Services;

/// <summary>Immutable periodic input levels and their exact synchronous truth table.</summary>
/// <remarks>Bits 0..3 are CLR/LOAD/ENP/ENT high levels, bits 4..7 are A..D.
/// No clock, count or mutable phase is shared between counter instances.</remarks>
internal sealed class Sn74S163PeriodicInputs
{
    private readonly ushort[] _captures;
    private readonly int _period;
    private readonly int _stride;
    private readonly int _maximumPeriods;
    private readonly bool _cycleReplacesCount;
    private readonly byte _cycleFinalCount;
    private readonly int _cycleIncrements;

    internal Sn74S163PeriodicInputs(ReadOnlySpan<byte> levels, int maximumPeriods)
    {
        if (levels.IsEmpty) throw new ArgumentException(nameof(levels));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumPeriods);
        _period = levels.Length;
        _maximumPeriods = maximumPeriods;
        _stride = checked(Math.Max(maximumPeriods, _period) + 1);
        _captures = new ushort[checked(_period * (C.CounterMask + 1) * _stride)];
        for (var phase = 0; phase < _period; phase++)
        for (var count = 0; count <= C.CounterMask; count++)
        {
            var index = (phase * (C.CounterMask + 1) + count) * _stride;
            var value = (byte)count;
            for (var periods = 1; periods < _stride; periods++)
            {
                var before = value;
                var pins = levels[(phase + periods - 1) % _period];
                value = (pins & C.ClearHigh) == 0 ? (byte)0
                    : (pins & C.LoadHigh) == 0 ? (byte)(pins >> C.DataShift)
                    : (pins & (C.EnableP | C.EnableT)) == (C.EnableP | C.EnableT)
                        ? (byte)((value + 1) & C.CounterMask) : value;
                var afterInputs = levels[(phase + periods) % _period];
                _captures[index + periods] = (ushort)(before
                    | (value << C.PeriodicCountShift) | (afterInputs << C.PeriodicInputShift));
            }
        }
        foreach (var pins in levels)
        {
            if ((pins & C.ClearHigh) == 0 || (pins & C.LoadHigh) == 0) _cycleReplacesCount = true;
            if ((pins & (C.ClearHigh | C.LoadHigh | C.EnableP | C.EnableT))
                == (C.ClearHigh | C.LoadHigh | C.EnableP | C.EnableT)) _cycleIncrements++;
        }
        _cycleFinalCount = EndingCount(0, 0, _period);
    }

    internal ushort Capture(byte initialCount, long inputClock, long originClock, int periods)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(periods);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(periods, _maximumPeriods);
        var elapsed = checked(inputClock - originClock);
        if (elapsed < 0) throw new ArgumentOutOfRangeException(nameof(originClock));
        var phase = (int)(elapsed % _period);
        var index = (phase * (C.CounterMask + 1) + initialCount) * _stride;
        return _captures[index + periods];
    }

    /// <summary>Direct count after a long interval of the received periodic inputs.
    /// A complete input motif either replaces the old count (CLR/LOAD), or adds
    /// a fixed number modulo16. No mutable clock or state is shared here.</summary>
    internal byte CountAfter(byte initialCount, long inputClock, long originClock, long periods)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(initialCount, C.CounterMask);
        ArgumentOutOfRangeException.ThrowIfNegative(periods);
        var elapsed = checked(inputClock - originClock);
        if (elapsed < 0) throw new ArgumentOutOfRangeException(nameof(originClock));
        var phase = (int)(elapsed % _period);
        var head = (int)Math.Min(periods, _period - phase);
        var value = EndingCount(phase, initialCount, head);
        periods -= head;
        var cycles = periods / _period;
        if (cycles != 0)
            value = _cycleReplacesCount ? _cycleFinalCount
                : (byte)((value + (cycles & C.CounterMask) * _cycleIncrements) & C.CounterMask);
        return EndingCount(0, value, (int)(periods % _period));
    }

    private byte EndingCount(int phase, byte initial, int periods)
        => periods == 0 ? initial : (byte)((_captures[(phase * (C.CounterMask + 1) + initial) * _stride + periods]
            >> C.PeriodicCountShift) & C.CounterMask);
}
