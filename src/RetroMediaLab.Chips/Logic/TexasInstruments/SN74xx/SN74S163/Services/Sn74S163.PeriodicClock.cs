using C = RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74S163.Constants.Sn74S163Constants;

namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74S163.Services;

public sealed partial class Sn74S163
{
    /// <summary>Capture a represented periodic input interval, ending at its last rising edge.</summary>
    /// <remarks>The caller bounds the interval before every unrepresented observation
    /// or input change. Every receiver captures before any resolves the shared edge;
    /// final pin notifications and the falling edge use the ordinary paths.
    /// As for CaptureRisingClock, the owner guarantees CLK low and no pending capture.</remarks>
    internal void CaptureClockPeriods(int periods, Sn74S163PeriodicInputs inputs, long originClock)
    {
        var capture = inputs.Capture(_count, _inputClockCount, originClock, periods);
        var total = checked(_inputClockCount + periods);
        _count = (byte)(capture & C.CounterMask);
        _capturedCount = (byte)((capture >> C.PeriodicCountShift) & C.CounterMask);
        _clearHigh = (capture & (C.ClearHigh << C.PeriodicInputShift)) != 0;
        _loadHigh = (capture & (C.LoadHigh << C.PeriodicInputShift)) != 0;
        _enableP = (capture & (C.EnableP << C.PeriodicInputShift)) != 0;
        _enableTMask = (capture & (C.EnableT << C.PeriodicInputShift)) != 0 ? C.CarryMask : (byte)0;
        _data = (byte)(capture >> (C.PeriodicInputShift + C.DataShift));
        _inputClockCount = total;
        _clockHigh = true;
        _clockPending = true;
    }
}
