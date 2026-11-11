using System.Numerics;
using C = RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Constants.Mc6847Constants;

namespace RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Services;

public sealed partial class Mc6847
{
    /// <inheritdoc />
    public int TryAdvanceStableHalfClocks(int halfClocks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(halfClocks);
        var half = _timing.HalfClock;
        var count = Math.Min(halfClocks, C.HalfClocksPerLine - half - 1);
        if (half >= C.ActiveHalfClockStart && half < C.ActiveHalfClockEnd)
            count = Math.Min(count, C.ActiveHalfClockEnd - half - 1);
        else
        {
            // Frontières conservatives des sorties numériques et du composite nominal.
            var boundary = half < C.EqualisingPulseHalfClocks ? C.EqualisingPulseHalfClocks
                : half < C.HorizontalSyncEnd ? C.HorizontalSyncEnd
                : half < C.BurstHalfClockStart ? C.BurstHalfClockStart
                : half < C.RowPresetStart ? C.RowPresetStart
                : half < C.RowPresetEnd ? C.RowPresetEnd
                : half < C.BurstHalfClockEnd ? C.BurstHalfClockEnd
                : half < C.BorderHalfClockStart ? C.BorderHalfClockStart
                : half < C.ActiveHalfClockStart ? C.ActiveHalfClockStart
                : half < C.ActiveHalfClockEnd ? C.ActiveHalfClockEnd
                : half < C.HalfClocksPerLine - C.HorizontalSyncEnd
                    ? C.HalfClocksPerLine - C.HorizontalSyncEnd : C.HalfClocksPerLine;
            count = Math.Min(count, boundary - half - 1);
        }
        if (_timing.ActiveLine && _nextFetchHalfClock > half)
            count = Math.Min(count, _nextFetchHalfClock - half - 1);
        if (Active && _displayValid && _cellDot < _pixels.PixelCount)
        {
            var remaining = _pixels.PixelCount - _cellDot - 1;
            if (remaining == 0) return 0;
            // Comparer les nibbles au niveau courant, sans nouvelle lecture mémoire.
            var next = _pixels.Pixels >> ((_cellDot + 1) * C.PackedColourBits);
            var repeated = (ulong)Colour * 0x1111111111111111UL;
            var run = BitOperations.TrailingZeroCount(next ^ repeated) / C.PackedColourBits;
            count = Math.Min(count, Math.Min(remaining, run));
        }
        if (count <= 0) return 0;
        _timing.AdvanceStableHalfClocks(count);
        if (_displayValid) _cellDot += count;
        return count;
    }
}
