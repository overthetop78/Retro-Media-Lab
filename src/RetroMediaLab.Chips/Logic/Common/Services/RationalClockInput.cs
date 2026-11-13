using RetroMediaLab.Chips.Logic.Common.Contracts;

namespace RetroMediaLab.Chips.Logic.Common.Services;

/// <summary>Receiver-owned transport cursor. The physical component keeps its
/// own counters and divider state; this cache only locates the received edges.</summary>
internal struct RationalClockInput
{
    internal long NextTime;
    internal bool NextHigh;
    private long _fraction, _whole, _remainder, _numerator, _denominator;
    private long _fastEdges, _fastSpan;

    internal RationalClockInput(RationalClockSource source)
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
    }

    internal long TimeAfterEdges(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
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

    /// <summary>Consume a bounded prefix with one quotient for the last edge.
    /// Its remainder also locates the following edge: no second date conversion
    /// or division is needed to commit the cursor. No component state is here.</summary>
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
        long phase = (long)(count - 1) * _numerator + _fraction;
        long delta = phase / _denominator;
        // Compare offsets, not a speculative absolute date which could overflow
        // even though the prefix strictly before the boundary is representable.
        if (delta >= boundaryExclusive - NextTime)
        {
            count = Math.Min(count, EdgesBefore(boundaryExclusive));
            if (count == 0) return 0;
            phase = (long)(count - 1) * _numerator + _fraction;
            delta = phase / _denominator;
        }
        long remainder = phase - delta * _denominator;
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
