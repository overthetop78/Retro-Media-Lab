using C = RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74S163.Constants.Sn74S163Constants;

namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74S163.Services;

/// <summary>Immutable periodic input levels and their exact synchronous truth table.</summary>
/// <remarks>Bits 0..3 are CLR/LOAD/ENP/ENT high levels, bits 4..7 are A..D.
/// No clock, count or mutable phase is shared between counter instances.</remarks>
internal sealed class Sn74S163PeriodicInputs
{
    private readonly byte[] _pins;
    private readonly byte[] _counts;
    private readonly int _stride;

    internal Sn74S163PeriodicInputs(ReadOnlySpan<byte> levels, int maximumPeriods)
    {
        if (levels.IsEmpty) throw new ArgumentException(nameof(levels));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumPeriods);
        _pins = levels.ToArray();
        _stride = checked(maximumPeriods + 1);
        _counts = new byte[checked(_pins.Length * (C.CounterMask + 1) * _stride)];
        for (var phase = 0; phase < _pins.Length; phase++)
        for (var count = 0; count <= C.CounterMask; count++)
        {
            var index = (phase * (C.CounterMask + 1) + count) * _stride;
            var value = (byte)count;
            _counts[index] = value;
            for (var periods = 1; periods < _stride; periods++)
            {
                var pins = _pins[(phase + periods - 1) % _pins.Length];
                value = (pins & C.ClearHigh) == 0 ? (byte)0
                    : (pins & C.LoadHigh) == 0 ? (byte)(pins >> C.DataShift)
                    : (pins & (C.EnableP | C.EnableT)) == (C.EnableP | C.EnableT)
                        ? (byte)((value + 1) & C.CounterMask) : value;
                _counts[index + periods] = value;
            }
        }
    }

    internal void Capture(byte initialCount, long inputClock, long originClock, int periods,
        out byte beforeLast, out byte afterLast, out byte postInputs)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(periods);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(periods, _stride);
        var elapsed = checked(inputClock - originClock);
        if (elapsed < 0) throw new ArgumentOutOfRangeException(nameof(originClock));
        var phase = (int)(elapsed % _pins.Length);
        var index = (phase * (C.CounterMask + 1) + initialCount) * _stride;
        beforeLast = _counts[index + periods - 1];
        afterLast = _counts[index + periods];
        postInputs = _pins[(phase + periods) % _pins.Length];
    }
}
