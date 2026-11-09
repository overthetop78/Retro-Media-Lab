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

    internal Sn74S163PeriodicInputs(ReadOnlySpan<byte> levels, int maximumPeriods)
    {
        if (levels.IsEmpty) throw new ArgumentException(nameof(levels));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumPeriods);
        _period = levels.Length;
        _stride = checked(maximumPeriods + 1);
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
    }

    internal ushort Capture(byte initialCount, long inputClock, long originClock, int periods)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(periods);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(periods, _stride);
        var elapsed = checked(inputClock - originClock);
        if (elapsed < 0) throw new ArgumentOutOfRangeException(nameof(originClock));
        var phase = (int)(elapsed % _period);
        var index = (phase * (C.CounterMask + 1) + initialCount) * _stride;
        return _captures[index + periods];
    }
}
