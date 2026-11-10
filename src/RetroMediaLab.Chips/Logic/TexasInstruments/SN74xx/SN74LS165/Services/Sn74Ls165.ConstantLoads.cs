namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS165.Services;

public sealed partial class Sn74Ls165
{
    /// <summary>Complete CLK periods plus SH/LD pulses between rising CLK edges.
    /// Each load pulse starts/ends while CLK has its initial level. The caller
    /// represents that waveform; no data or output transition can be omitted.</summary>
    internal bool TryAdvanceConstantLoadedPeriods(long clocks, long loadPulses)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(clocks);
        ArgumentOutOfRangeException.ThrowIfNegative(loadPulses);
        if (_clockPending || !_shiftLoadHigh || _clockInhibitHigh
            || _knownBits != byte.MaxValue || _parallelKnownBits != byte.MaxValue
            || _register != _parallelData
            || !(_serialInput == false && _register == 0 || _serialInput == true && _register == byte.MaxValue)) return false;
        var inputs = checked(_inputClockCount + clocks);
        // Releasing NL while CLK is high raises the effective gated clock even
        // though it is not an additional CLK input edge (TI gated-clock circuit).
        var shifts = checked(_shiftClockCount + checked(clocks + (_clockHigh ? loadPulses : 0)));
        _inputClockCount = inputs; _shiftClockCount = shifts;
        return true;
    }
}
