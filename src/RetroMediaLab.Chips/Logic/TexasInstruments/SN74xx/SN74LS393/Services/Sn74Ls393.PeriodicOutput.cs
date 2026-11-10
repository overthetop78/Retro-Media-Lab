using C = RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS393.Constants.Sn74Ls393Constants;
using System.Numerics;

namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS393.Services;

public sealed partial class Sn74Ls393
{
    private bool _updatingQdPatterns;
    private byte _qdPendingPatterns;

    /// <summary>Represent the physical QD waveform at the receiver. Zero period
    /// revokes it before an input/source change; the wire owner resumes scalar
    /// QD notifications until a new, phase-consistent description is available.</summary>
    public void SetQdPeriodicClockReceiver(int channel, Action<long, long, long>? receiver)
    {
        var state = Get(channel);
        InvalidateQdPattern(state);
        state.QdReceiver = receiver;
        var mask = (byte)(1 << channel);
        if (receiver is null) _qdPendingPatterns &= (byte)~mask;
        else _qdPendingPatterns |= mask;
        PublishDeadline();
    }

    private void InvalidateQdPattern(Channel state)
    {
        if (state.QdReceiver is not null) _qdPendingPatterns |= (byte)(ReferenceEquals(state, _channels[0]) ? 1 : 2);
        if (!state.QdPatternActive) return;
        state.QdPatternActive = false;
        var previous = _updatingQdPatterns; _updatingQdPatterns = true;
        try { state.QdReceiver?.Invoke(0, 0, 0); }
        finally { _updatingQdPatterns = previous; }
    }

    private void UpdateQdPatterns()
    {
        if (_updatingQdPatterns || _qdPendingPatterns == 0) return;
        var pending = _qdPendingPatterns;
        while (pending != 0)
        {
            var channel = BitOperations.TrailingZeroCount((uint)pending);
            var mask = (byte)(1 << channel); pending &= (byte)~mask;
            var state = _channels[channel];
            if (state.QdPatternActive || state.QdReceiver is null || state.ClearHigh
                || state.Period < _maximumRippleNanoseconds || state.NextFall == 0
                || state.Period > long.MaxValue / (C.OutputMask + 1))
            { _qdPendingPatterns &= (byte)~mask; continue; }
            if (state.Pending != 0) continue;
            var period = state.Period * (C.OutputMask + 1);
            // Every QD transition is reached through falling QA/QB/QC. The
            // configured later stages have the same rise/fall propagation.
            var delay = _toggleDelays[C.Stages] + _toggleDelays[C.RippleOffsetBase + C.Stages - 1];
            var nextFall = FutureOutputTime(state, C.OutputMask + 1 - state.Outputs, delay);
            if (nextFall == 0) continue;
            var remaining = nextFall - _timeNanoseconds;
            if (remaining <= 0 || remaining > period || ((state.Outputs & 8) != 0) != (remaining <= period / 2)) continue;
            state.QdPatternActive = true;
            _qdPendingPatterns &= (byte)~mask;
            _updatingQdPatterns = true;
            try { state.QdReceiver(nextFall, period, period / 2); }
            finally { _updatingQdPatterns = false; }
        }
    }
}
