using C = RetroMediaLab.Chips.Controllers.Acorn.Vdu80CharacterGenerator.Constants.Vdu80CharacterConstants;

namespace RetroMediaLab.Chips.Controllers.Acorn.Vdu80CharacterGenerator.Services;

public sealed partial class Vdu80CharacterGenerator
{
    private bool _representingCharacter;

    /// <summary>Represent one complete character interval between external inputs.
    /// Samples remain in dot order (first in bit7); capture/NL/ROM/DE retain their
    /// local chronology. Video observers and a cursor response require ordinary
    /// edges. Only the software grouping qualification is coalesced at the end.</summary>
    public bool TryAdvanceCharacterPeriod(out byte values, out byte known)
    {
        values = known = 0;
        if (_representingCharacter || VideoChanged is not null || _characterClockHigh
            || _latch.ClockHigh || _loadDeadline.HasValue || !_latchedData.HasValue
            || !_serializer.ShiftLoadHigh || _serializer.ClockInhibitHigh
            || !_rom.OutputsDriven) return false;
        var start = _timeNanoseconds;
        var target = checked(start + C.CharacterPeriodNanoseconds);
        if (_cursorDeadline <= target) return false;
        _ = checked(start + C.CharacterPeriodNanoseconds / 2 + C.RomAddressNanoseconds);
        _ = checked(_latch.InputClockCount + 1);
        _ = checked(_serializer.InputClockCount + C.DotPeriodsPerCharacter);
        _ = checked(_serializer.ShiftClockCount + C.DotPeriodsPerCharacter + (_dotClockHigh ? 1 : 0));
        var previousQualification = _stableClockCandidate;
        _representingCharacter = true;
        try
        {
            // NL is high throughout the first half: ROM/DE deadlines cannot
            // modify the shifted word or the previously captured display bit.
            // Their outputs remain local until the capture below. No external
            // video observer can observe these represented internal states.
            RepresentDotHalf(out var first, out var firstKnown);
            AdvanceLocalTime(start + C.CharacterPeriodNanoseconds / 2);
            SetCharacterClock(true);
            // The real20 ns NL pulse ends before the first half-dot (41/42 ns).
            // Release through the canonical gate: high CLK creates its real
            // additional effective shift, which is not a CLK input period.
            AdvanceLocalTime(_loadDeadline!.Value);
            RepresentDotHalf(out var second, out var secondKnown);
            AdvanceLocalTime(target);
            SetCharacterClock(false);
            // Three periods are exactly250 ns, six500 ns: the rational
            // remainder and the received dot phase are unchanged.
            values = (byte)((first & 0xe0) | ((second & 0xe0) >> 3));
            known = (byte)((firstKnown & 0xe0) | ((secondKnown & 0xe0) >> 3));
            return true;
        }
        finally
        {
            _representingCharacter = false;
            if (previousQualification != _stableClockCandidate)
                StableClockCandidateChanged?.Invoke(_stableClockCandidate);
        }
    }

    private void RepresentDotHalf(out byte values, out byte known)
    {
        _serializer.RepresentSampledPeriods((int)(C.DotPeriodsPerCharacter / 2), out var serial, out var serialKnown);
        known = _displayKnown ? _displayHigh ? serialKnown : byte.MaxValue
            : (byte)(serialKnown & ~serial);
        values = _displayKnown && _displayHigh ? (byte)(serial & known) : (byte)0;
        if (_cursor ^ _inverted) values ^= known;
        var output = _serializer.SerialOutput;
        _serialKnown = output.HasValue;
        _serialHigh = output.GetValueOrDefault();
        PublishVideo();
    }
}
