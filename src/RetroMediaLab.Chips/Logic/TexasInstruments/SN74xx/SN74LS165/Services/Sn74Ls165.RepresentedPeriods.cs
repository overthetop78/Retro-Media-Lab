namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS165.Services;

public sealed partial class Sn74Ls165
{
    /// <summary>Return up to eight pre-rising-edge QH samples, first in bit7,
    /// while advancing the canonical register. The consuming circuit represents
    /// these observations instead of replaying callbacks at the interval end.
    /// All inputs must remain unchanged; CLK returns to its original phase.</summary>
    internal void RepresentSampledPeriods(int periods, out byte values, out byte known)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(periods);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(periods, 8);
        if (_clockPending) throw new InvalidOperationException(nameof(_clockPending));
        values = known = 0;
        if (periods == 0) return;
        var inputs = checked(_inputClockCount + periods);
        if (_shiftLoadHigh && !_clockInhibitHigh)
        {
            var shifts = checked(_shiftClockCount + periods);
            values = (byte)(_register & _knownBits); known = _knownBits;
            var fill = (1 << periods) - 1;
            _register = (byte)((_register << periods) | (_serialInput == true ? fill : 0));
            _knownBits = (byte)((_knownBits << periods) | (_serialInput.HasValue ? fill : 0));
            _shiftClockCount = shifts;
            _stableDeadlineValid = false;
        }
        else if ((_knownBits & 128) != 0)
        {
            known = (byte)(255 << (8 - periods));
            if ((_register & 128) != 0) values = known;
        }
        _inputClockCount = inputs;
        _publishedOutput = (byte)((_knownBits & 128) | ((_register & _knownBits & 128) >> 1));
    }
}
