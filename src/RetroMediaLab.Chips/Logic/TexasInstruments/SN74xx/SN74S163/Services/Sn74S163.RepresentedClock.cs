using C = RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74S163.Constants.Sn74S163Constants;

namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74S163.Services;

public sealed partial class Sn74S163
{
    /// <summary>Resolve a long represented range. Only the count and elapsed
    /// source periods need materializing before the final ordinary capture,
    /// which establishes every final input and output pin.</summary>
    internal byte ResolveRepresentedClockRange(long periods, Sn74S163PeriodicInputs inputs, long originClock)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(periods);
        var end = checked(_inputClockCount + periods);
        var beforeLast = inputs.CountAfter(_count, _inputClockCount, originClock, periods - 1);
        _count = beforeLast;
        _inputClockCount = end - 1;
        return ResolveRepresentedClockPeriods(1, inputs, originClock);
    }

    /// <summary>Resolve an interval whose intervening pin changes and internal
    /// recipients are already represented by the supplied periodic input graph.
    /// No pin callback is replayed. The owner publishes external observations
    /// only after all receivers of the shared edge have reached this position.</summary>
    internal byte ResolveRepresentedClockPeriods(int periods, Sn74S163PeriodicInputs inputs, long originClock)
    {
        var capture = inputs.Capture(_count, _inputClockCount, originClock, periods);
        var total = checked(_inputClockCount + periods);
        _count = (byte)((capture >> C.PeriodicCountShift) & C.CounterMask);
        _clearHigh = (capture & (C.ClearHigh << C.PeriodicInputShift)) != 0;
        _loadHigh = (capture & (C.LoadHigh << C.PeriodicInputShift)) != 0;
        _enableP = (capture & (C.EnableP << C.PeriodicInputShift)) != 0;
        _enableTMask = (capture & (C.EnableT << C.PeriodicInputShift)) != 0 ? C.CarryMask : (byte)0;
        _data = (byte)(capture >> (C.PeriodicInputShift + C.DataShift));
        _inputClockCount = total;
        _clockPending = false;
        _clockHigh = true;
        _publishedOutputs = (byte)(_count | ((_count + 1) & _enableTMask));
        return _publishedOutputs;
    }
}
