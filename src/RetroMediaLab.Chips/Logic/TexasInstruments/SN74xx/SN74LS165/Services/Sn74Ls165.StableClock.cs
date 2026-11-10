using C = RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS165.Constants.Sn74Ls165Constants;

namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS165.Services;

public sealed partial class Sn74Ls165
{
    // Effective shifts strictly before the next output transition. This is a
    // local event distance, invalidated only by the inputs/state determining it.
    private int _stableShifts;
    private bool _stableDeadlineValid;
    private static readonly byte[] StablePrefixDecoder = CreateStablePrefixDecoder();

    public long StableClockPeriods
        => _clockHigh ? 0 : StableClockPeriodsAtCurrentPhase;

    internal long StableClockPeriodsAtCurrentPhase
    {
        get
        {
            if (!_shiftLoadHigh || _clockInhibitHigh) return long.MaxValue;
            if (!_stableDeadlineValid) UpdateStableShiftDeadline();
            return _stableShifts == int.MaxValue ? long.MaxValue : _stableShifts;
        }
    }

    /// <summary>Advance low-to-low periods strictly before the next QH transition.</summary>
    /// <remarks>Inputs must remain stable during the operation. At most eight stages
    /// determine the next transition; its retained distance permits arithmetic advances.
    /// Internal shifts and both clock totals are retained, including unknown bits.</remarks>
    public long AdvanceStableClocks(long maximumClocks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maximumClocks);
        RequireLowClock();
        return AdvanceStableClockPeriods(maximumClocks);
    }

    /// <summary>Complete stable periods preserving the received CLK phase.</summary>
    /// <remarks>High-to-high periods contain a falling then rising edge. The final
    /// gate level equals the initial one; every real rising edge still shifts.</remarks>
    internal long AdvanceStableClockPeriods(long maximumClocks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maximumClocks);
        if (_clockPending) throw new InvalidOperationException(nameof(_clockPending));
        if (maximumClocks == 0) return 0;
        if (!_shiftLoadHigh || _clockInhibitHigh)
        {
            _inputClockCount = checked(_inputClockCount + maximumClocks);
            return maximumClocks;
        }

        if (!_stableDeadlineValid) UpdateStableShiftDeadline();
        if (_stableShifts == int.MaxValue && _knownBits == byte.MaxValue)
        {
            // All eight stages already equal the received serial level. Every
            // real shift leaves the known word unchanged; only its clock totals
            // advance. Input/load changes invalidate the local qualification.
            var inputs = checked(_inputClockCount + maximumClocks);
            var shifts = checked(_shiftClockCount + maximumClocks);
            _inputClockCount = inputs;
            _shiftClockCount = shifts;
            return maximumClocks;
        }
        var periods = _stableShifts == int.MaxValue ? maximumClocks : Math.Min(maximumClocks, _stableShifts);
        if (periods == 0) return 0;
        var inputClocks = checked(_inputClockCount + periods);
        var shiftClocks = checked(_shiftClockCount + periods);
        if (periods >= C.RegisterBits)
        {
            _register = _serialInput == true ? byte.MaxValue : (byte)0;
            _knownBits = _serialInput.HasValue ? byte.MaxValue : (byte)0;
        }
        else
        {
            var shifts = (int)periods;
            var fill = (1 << shifts) - 1;
            _register = (byte)((_register << shifts) | (_serialInput == true ? fill : 0));
            _knownBits = (byte)((_knownBits << shifts) | (_serialInput.HasValue ? fill : 0));
        }
        if (_stableShifts != int.MaxValue) _stableShifts -= (int)periods;
        _inputClockCount = inputClocks;
        _shiftClockCount = shiftClocks;
        return periods;
    }

    private void UpdateStableShiftDeadline()
    {
        _stableDeadlineValid = true;
        var known = (_knownBits & C.LastStageMask) != 0;
        var high = (_register & C.LastStageMask) != 0;
        // Stages differing from QH: known stages differ from an unknown output;
        // otherwise either unknown stages or the opposite level cause a transition.
        var different = !known ? _knownBits
            : high ? ~_knownBits | ~_register : ~_knownBits | _register;
        var prefix = StablePrefixDecoder[different & (C.LastStageMask - 1)];
        if (prefix != C.RegisterBits - 1) { _stableShifts = prefix; return; }
        _stableShifts = known == _serialInput.HasValue
            && (!known || high == _serialInput.GetValueOrDefault()) ? int.MaxValue : prefix;
    }

    private static byte[] CreateStablePrefixDecoder()
    {
        var decoder = new byte[C.LastStageMask];
        decoder[0] = C.RegisterBits - 1;
        for (var bit = 0; bit < C.RegisterBits - 1; bit++)
            for (var mask = 1 << bit; mask < 1 << (bit + 1); mask++)
                decoder[mask] = (byte)(C.RegisterBits - 2 - bit);
        return decoder;
    }
}
