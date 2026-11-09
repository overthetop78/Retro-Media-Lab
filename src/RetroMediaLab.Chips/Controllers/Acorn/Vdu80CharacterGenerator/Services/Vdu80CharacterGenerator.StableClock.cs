using C = RetroMediaLab.Chips.Controllers.Acorn.Vdu80CharacterGenerator.Constants.Vdu80CharacterConstants;

namespace RetroMediaLab.Chips.Controllers.Acorn.Vdu80CharacterGenerator.Services;

public sealed partial class Vdu80CharacterGenerator
{
    /// <summary>Advance complete low-to-low dot periods without any output transition.</summary>
    /// <remarks>The caller bounds the span before any external input/character-clock
    /// change. ROM and cursor events always bound it. A known-zero register may
    /// handle its internal load/display events with ordinary local clocks when
    /// they cannot change video; other local boundaries use the strict path.</remarks>
    public int AdvanceStableDotClocks(int maximumClocks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maximumClocks);
        if (maximumClocks == 0 || _dotClockHigh) return 0;
        maximumClocks = (int)Math.Min(maximumClocks, _serializer.StableClockPeriods);
        if (maximumClocks == 0) return 0;
        var count = maximumClocks;
        var numerator = checked(count * 2L * C.NanosecondsPerSecond + _fraction);
        var target = checked(_timeNanoseconds + numerator / C.HalfDotFrequency);
        if (target >= _nextDeadline)
        {
            if (_stableClockCandidate && _serializer.StoredData == 0)
                return AdvanceSettledZeroDotClocks(count, target);
            count = DotPeriodsBefore(count, _nextDeadline);
            if (count == 0) return 0;
            numerator = checked(count * 2L * C.NanosecondsPerSecond + _fraction);
            target = checked(_timeNanoseconds + numerator / C.HalfDotFrequency);
        }

        var accepted = (int)_serializer.AdvanceStableClocks(count);
        if (accepted == 0) return 0;
        if (accepted != count)
        {
            numerator = checked(accepted * 2L * C.NanosecondsPerSecond + _fraction);
            target = checked(_timeNanoseconds + numerator / C.HalfDotFrequency);
        }
        // Same rational accumulation as two explicit edges per period; no event
        // can occur within this span and the serializer published no transition.
        _timeNanoseconds = target;
        _fraction = numerator % C.HalfDotFrequency;
        return accepted;
    }

    private long EndTimeAfterDotPeriods(int count) => checked(_timeNanoseconds
        + (count * 2L * C.NanosecondsPerSecond + _fraction) / C.HalfDotFrequency);

    private int DotPeriodsBefore(int maximum, long deadline)
    {
        var low = 0;
        var high = maximum;
        while (low < high)
        {
            var middle = (int)(low + ((long)high - low + 1) / 2);
            if (EndTimeAfterDotPeriods(middle) < deadline) low = middle;
            else high = middle - 1;
        }
        return low;
    }

    private int AdvanceSettledZeroDotClocks(int count, long target)
    {
        // SER is physically grounded. The parallel word and every stored stage
        // are known zero: releasing NL cannot change QH, nor can a delayed DE
        // change the character latch before the next external character clock.
        // Still execute those internal events and clocks at their ordinary times.
        // ROM may alter the loaded word and CU the video XOR, so neither is crossed.
        var boundary = Math.Min(_romDeadline ?? long.MaxValue, _cursorDeadline ?? long.MaxValue);
        if (target >= boundary)
        {
            count = DotPeriodsBefore(count, boundary);
            target = EndTimeAfterDotPeriods(count);
        }
        for (var period = 0; period < count; period++)
        {
            SetDotClock(true);
            SetDotClock(false);
            // Once the internal event is handled, resume arithmetic advancement;
            // do not turn a long, now uneventful interval into a per-dot loop.
            var done = period + 1;
            if (done < count && _nextDeadline > target)
                return done + AdvanceStableDotClocks(count - done);
        }
        return count;
    }
}
