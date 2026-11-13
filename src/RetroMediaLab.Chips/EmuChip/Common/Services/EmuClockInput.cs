using RetroMediaLab.Chips.Logic.Common.Contracts;

namespace RetroMediaLab.Chips.EmuChip.Common.Services;

/// <summary>Receiver-owned transport cursor. The physical component keeps its
/// own counters and divider state; this cache only locates the received edges.
/// Common phase lengths use prepared quotient/remainder pairs; arbitrary cuts
/// retain exact integer arithmetic, with no independent oscillator.</summary>
internal struct EmuClockInput
{
    internal long NextTime;
    internal bool NextHigh;
    private long _fraction, _whole, _remainder, _numerator, _denominator;
    private long _fastEdges, _fastSpan;
    private readonly Step[] _steps;
    private readonly record struct Step(long Whole, long Remainder);

    internal EmuClockInput(RationalClockSource source)
    {
        NextTime = source.FirstEdgeNanoseconds;
        NextHigh = source.FirstEdgeHigh;
        _numerator = source.HalfPeriodNumerator;
        _denominator = source.HalfPeriodDenominator;
        _fraction = (long)(((Int128)source.FirstEdgeIndex * _numerator + source.PhaseNumerator) % _denominator);
        _whole = _numerator / _denominator;
        _remainder = _numerator % _denominator;
        _fastEdges = (long.MaxValue - (_denominator - 1)) / _numerator;
        _fastSpan = long.MaxValue / _denominator;
        // Source metadata is immutable during a received program. All common
        // phase lengths are compiled once, not divided again at every visit.
        int maximum = (int)Math.Min(128, _fastEdges);
        _steps = new Step[maximum + 1];
        for (int count = 0; count <= maximum; count++)
        {
            long phase = (long)count * _numerator;
            long whole = phase / _denominator;
            _steps[count] = new(whole, phase - whole * _denominator);
        }
    }

    internal long TimeAfterEdges(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if ((uint)count < (uint)_steps.Length)
        {
            var step = _steps[count];
            bool carry = _fraction >= _denominator - step.Remainder;
            return checked(NextTime + step.Whole + (carry ? 1 : 0));
        }
        if (count <= _fastEdges)
            return checked(NextTime + ((long)count * _numerator + _fraction) / _denominator);
        Int128 date = NextTime + ((Int128)count * _numerator + _fraction) / _denominator;
        if (date > long.MaxValue) throw new OverflowException();
        return (long)date;
    }

    internal RationalClockSource Divide(int divisor, int firstEdgeOffset, bool firstEdgeHigh)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(divisor);
        ArgumentOutOfRangeException.ThrowIfNegative(firstEdgeOffset);
        return new(TimeAfterEdges(firstEdgeOffset), checked(_numerator * divisor), _denominator,
            0, firstEdgeHigh, (long)(((Int128)firstEdgeOffset * _numerator + _fraction) % _denominator));
    }

    internal int EdgesBefore(long boundaryExclusive)
    {
        if (boundaryExclusive <= NextTime) return 0;
        long span = boundaryExclusive - NextTime;
        if (span <= _fastSpan)
        {
            long count64 = (span * _denominator - _fraction - 1) / _numerator + 1;
            return (int)Math.Min(count64, int.MaxValue);
        }
        Int128 count = ((Int128)(boundaryExclusive - NextTime) * _denominator - _fraction - 1) / _numerator + 1;
        return (int)Int128.Min(count, int.MaxValue);
    }

    /// <summary>Consume a bounded phase portion using its prepared quotient
    /// and remainder. Arbitrary lengths and external cuts retain the general
    /// exact conversion. No physical component state is stored here.</summary>
    internal int ConsumeBefore(int maximumCount, long boundaryExclusive, out long lastTime)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumCount);
        lastTime = NextTime;
        if (boundaryExclusive <= NextTime) return 0;
        int count = maximumCount;
        if (maximumCount - 1 > _fastEdges)
        {
            count = Math.Min(maximumCount, EdgesBefore(boundaryExclusive));
            if (count == 0) return 0;
            lastTime = TimeAfterEdges(count - 1);
            Advance(count);
            return count;
        }
        long delta, remainder;
        if ((uint)(count - 1) < (uint)_steps.Length)
        {
            var step = _steps[count - 1];
            bool hasCarry = _fraction >= _denominator - step.Remainder;
            delta = step.Whole + (hasCarry ? 1 : 0);
            remainder = hasCarry ? _fraction - (_denominator - step.Remainder) : _fraction + step.Remainder;
        }
        else
        {
            long phase = (long)(count - 1) * _numerator + _fraction;
            delta = phase / _denominator; remainder = phase - delta * _denominator;
        }
        if (delta >= boundaryExclusive - NextTime)
        {
            count = Math.Min(count, EdgesBefore(boundaryExclusive));
            if (count == 0) return 0;
            // Only an actual outer cut needs the arbitrary-length conversion.
            if ((uint)(count - 1) < (uint)_steps.Length)
            {
                var step = _steps[count - 1];
                bool hasCarry = _fraction >= _denominator - step.Remainder;
                delta = step.Whole + (hasCarry ? 1 : 0);
                remainder = hasCarry ? _fraction - (_denominator - step.Remainder) : _fraction + step.Remainder;
            }
            else
            {
                long phase = (long)(count - 1) * _numerator + _fraction;
                delta = phase / _denominator; remainder = phase - delta * _denominator;
            }
        }
        bool carry = remainder >= _denominator - _remainder;
        long date = checked(NextTime + delta);
        long next = checked(date + _whole + (carry ? 1 : 0));
        _fraction = carry ? remainder - (_denominator - _remainder) : remainder + _remainder;
        NextTime = next;
        if ((count & 1) != 0) NextHigh = !NextHigh;
        lastTime = date;
        return count;
    }

    internal void Advance(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (count == 0) return;
        if (count == 1)
        {
            bool carry = _fraction >= _denominator - _remainder;
            long date = checked(NextTime + _whole + (carry ? 1 : 0));
            _fraction = carry ? _fraction - (_denominator - _remainder) : _fraction + _remainder;
            NextTime = date;
            NextHigh = !NextHigh;
            return;
        }
        if ((uint)count < (uint)_steps.Length)
        {
            var step = _steps[count];
            bool carry = _fraction >= _denominator - step.Remainder;
            long preparedNext = checked(NextTime + step.Whole + (carry ? 1 : 0));
            _fraction = carry ? _fraction - (_denominator - step.Remainder) : _fraction + step.Remainder;
            NextTime = preparedNext;
            if ((count & 1) != 0) NextHigh = !NextHigh;
            return;
        }
        if (count <= _fastEdges)
        {
            long phase64 = (long)count * _numerator + _fraction;
            long next64 = checked(NextTime + phase64 / _denominator);
            _fraction = phase64 % _denominator;
            NextTime = next64;
            if ((count & 1) != 0) NextHigh = !NextHigh;
            return;
        }
        Int128 phase = (Int128)count * _numerator + _fraction;
        Int128 next = NextTime + phase / _denominator;
        if (next > long.MaxValue) throw new OverflowException();
        _fraction = (long)(phase % _denominator);
        NextTime = (long)next;
        if ((count & 1) != 0) NextHigh = !NextHigh;
    }
}
