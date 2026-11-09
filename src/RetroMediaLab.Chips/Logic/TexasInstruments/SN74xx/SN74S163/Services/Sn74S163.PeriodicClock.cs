using C = RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74S163.Constants.Sn74S163Constants;

namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74S163.Services;

public sealed partial class Sn74S163
{
    /// <summary>Capture a represented periodic input interval, ending at its last rising edge.</summary>
    /// <remarks>The caller bounds the interval before every unrepresented observation
    /// or input change. Every receiver captures before any resolves the shared edge;
    /// final pin notifications and the falling edge use the ordinary paths.</remarks>
    internal void CaptureClockPeriods(int periods, Sn74S163PeriodicInputs inputs, long originClock)
    {
        RequireLowClock();
        if (_clockPending) throw new InvalidOperationException(nameof(_clockPending));
        inputs.Capture(_count, _inputClockCount, originClock, periods,
            out var beforeLast, out var afterLast, out var postInputs);
        var total = checked(_inputClockCount + periods);
        _count = beforeLast;
        _capturedCount = afterLast;
        _clearHigh = (postInputs & C.ClearHigh) != 0;
        _loadHigh = (postInputs & C.LoadHigh) != 0;
        _enableP = (postInputs & C.EnableP) != 0;
        _enableTMask = (postInputs & C.EnableT) != 0 ? C.CarryMask : (byte)0;
        _data = (byte)(postInputs >> C.DataShift);
        _inputClockCount = total;
        _clockHigh = true;
        _clockPending = true;
    }
}
