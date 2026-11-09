using C = RetroMediaLab.Chips.Controllers.Acorn.Vdu80CharacterGenerator.Constants.Vdu80CharacterConstants;

namespace RetroMediaLab.Chips.Controllers.Acorn.Vdu80CharacterGenerator.Services;

public sealed partial class Vdu80CharacterGenerator
{
    /// <summary>Advance complete low-to-low dot periods without any output transition.</summary>
    /// <remarks>The caller bounds the span before any external input/character-clock
    /// change. This component additionally bounds it strictly before its own pending
    /// ROM, load, display and cursor events. A rejected boundary uses SetDotClock.</remarks>
    public int AdvanceStableDotClocks(int maximumClocks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maximumClocks);
        if (maximumClocks == 0 || _dotClockHigh) return 0;
        maximumClocks = (int)Math.Min(maximumClocks, _serializer.StableClockPeriods);
        if (maximumClocks == 0) return 0;
        if (EndTimeAfterDotPeriods(1) >= _nextDeadline) return 0;

        var count = maximumClocks;
        if (EndTimeAfterDotPeriods(count) >= _nextDeadline)
        {
            var low = 1;
            var high = count;
            while (low < high)
            {
                var middle = (int)(low + ((long)high - low + 1) / 2);
                if (EndTimeAfterDotPeriods(middle) < _nextDeadline) low = middle;
                else high = middle - 1;
            }
            count = low;
        }

        count = (int)_serializer.AdvanceStableClocks(count);
        if (count == 0) return 0;
        // Same rational accumulation as two explicit edges per period; no event
        // can occur within this span and the serializer published no transition.
        var numerator = checked(count * 2L * C.NanosecondsPerSecond + _fraction);
        _timeNanoseconds = checked(_timeNanoseconds + numerator / C.HalfDotFrequency);
        _fraction = numerator % C.HalfDotFrequency;
        return count;
    }

    private long EndTimeAfterDotPeriods(int count) => checked(_timeNanoseconds
        + (count * 2L * C.NanosecondsPerSecond + _fraction) / C.HalfDotFrequency);
}
