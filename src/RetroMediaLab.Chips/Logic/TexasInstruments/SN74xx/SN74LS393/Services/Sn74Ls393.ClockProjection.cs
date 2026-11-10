namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS393.Services;

public sealed partial class Sn74Ls393
{
    /// <summary>Read the QA clock count at an established external time without
    /// resolving unrelated ripple stages. Valid only before the next connected
    /// effect; the caller must process that effect normally at its announced date.
    /// Returns false when the received waveform/state cannot prove the result.</summary>
    public bool TryGetQaRisingClockCountAt(int channel, long nanoseconds, out long count)
    {
        var state = Get(channel);
        ArgumentOutOfRangeException.ThrowIfLessThan(nanoseconds, _timeNanoseconds);
        count = state.QaRises;
        if (nanoseconds == _timeNanoseconds) return true;
        if (_resolving || _advancingPeriodic || state.Period == 0 || state.NextFall == 0
            || state.ClearHigh || state.Clearing != 0 || state.Period < _maximumRippleNanoseconds) return false;
        var deadline = _nextOutputEventChanged is null ? ConnectedDeadline() : _publishedOutputDeadline;
        if (deadline != 0 && deadline <= nanoseconds) return false;
        var high = (state.Outputs & 1) != 0;
        if ((state.Pending & 1) != 0)
        {
            if (state.Due[0] >= state.NextFall) return false;
            if (!high && state.Due[0] <= nanoseconds) count = checked(count + 1);
            high = !high;
        }
        var riseDelay = _toggleDelays[0];
        if (state.NextFall > nanoseconds || nanoseconds - state.NextFall < riseDelay) return true;
        var toggles = (nanoseconds - state.NextFall - riseDelay) / state.Period + 1;
        count = checked(count + toggles / 2 + (((toggles & 1) != 0 && !high) ? 1 : 0));
        return true;
    }
}
