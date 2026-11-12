namespace RetroMediaLab.Chips.Logic.Common.Contracts;

/// <summary>Immutable description of an established external clock wire.
/// Edge i occurs at Origin + floor((i*N+Phase)/D). Receivers own their input cursors;
/// this value contains no mutable chip state or independently running clock.</summary>
public readonly record struct RationalClockSource(long OriginNanoseconds,
    long HalfPeriodNumerator, long HalfPeriodDenominator, long FirstEdgeIndex, bool FirstEdgeHigh,
    long PhaseNumerator = 0)
{
    public long FirstEdgeNanoseconds => EdgeNanoseconds(FirstEdgeIndex);

    public long EdgeNanoseconds(long index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(OriginNanoseconds);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfNegative(FirstEdgeIndex);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(HalfPeriodDenominator);
        ArgumentOutOfRangeException.ThrowIfLessThan(HalfPeriodNumerator, HalfPeriodDenominator);
        ArgumentOutOfRangeException.ThrowIfNegative(PhaseNumerator);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(PhaseNumerator, HalfPeriodDenominator);
        Int128 date = OriginNanoseconds + ((Int128)index * HalfPeriodNumerator + PhaseNumerator) / HalfPeriodDenominator;
        if (date > long.MaxValue) throw new ArgumentOutOfRangeException(nameof(index));
        return (long)date;
    }
}
