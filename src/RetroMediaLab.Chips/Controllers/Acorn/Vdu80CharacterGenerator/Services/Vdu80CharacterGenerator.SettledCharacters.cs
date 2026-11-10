using C = RetroMediaLab.Chips.Controllers.Acorn.Vdu80CharacterGenerator.Constants.Vdu80CharacterConstants;

namespace RetroMediaLab.Chips.Controllers.Acorn.Vdu80CharacterGenerator.Services;

public sealed partial class Vdu80CharacterGenerator
{
    /// <summary>Complete character periods with unchanged data and video.
    /// Caller bounds the interval before any external character/raster/DE/CU
    /// input change. The six dot periods and intervening20 ns NL pulses remain
    /// represented by their effects on the physical components' clock totals.</summary>
    public bool TryAdvanceSettledCharacterPeriods(int periods, out bool white)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(periods);
        white = false;
        var data = (byte)(_characterData | (_delayedDisplay ? 0x80 : 0));
        if (periods == 0 || _characterClockHigh || _latch.ClockHigh || _loadDeadline.HasValue
            || !_stableClockCandidate || !_serialKnown || _serialHigh
            || _latchedData != data || _latch.DataInput != data) return false;
        var target = checked(_timeNanoseconds + periods * C.CharacterPeriodNanoseconds);
        if (target >= _nextDeadline) return false;
        // Prevalidate the other package before the serializer updates its own
        // counts. The established latch value cannot publish a new output.
        _ = checked(_latch.InputClockCount + periods);
        if (!_serializer.TryAdvanceConstantLoadedPeriods(periods * C.DotPeriodsPerCharacter, periods)) return false;
        _latch.AdvanceClocks(periods);
        _timeNanoseconds = target;
        // A character contains12 rational half-dots, exactly500 ns. Fraction,
        // CLK levels, NL high, ROM and output caches retain their final values.
        white = _videoWhite == true;
        return true;
    }
}
