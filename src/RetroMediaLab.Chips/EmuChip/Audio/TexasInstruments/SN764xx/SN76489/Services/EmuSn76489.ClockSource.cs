using RetroMediaLab.Chips.Logic.Common.Contracts;
using RetroMediaLab.Chips.Logic.Common.Services;

namespace RetroMediaLab.Chips.EmuChip.Audio.TexasInstruments.SN764xx.SN76489.Services;

public sealed partial class EmuSn76489
{
    private RationalClockInput _clockInput;
    private bool _hasClockSource, _advancingClockSource;

    public void ReceiveClockSource(RationalClockSource source)
    {
        if (_publishing || _advancingClockSource)
            throw new InvalidOperationException("Cannot replace a PSG clock during delivery.");
        var input = new RationalClockInput(source);
        ArgumentOutOfRangeException.ThrowIfLessThan(input.NextTime, TimeNanoseconds);
        if (input.NextHigh == _clockHigh)
            throw new ArgumentException("The first source edge must change the received clock level.", nameof(source));
        _clockInput = input; _hasClockSource = true;
        PublishReadyDeadline();
    }

    private long? NextReadyEvent
    {
        get
        {
            long? due = _readyDue;
            if (_hasClockSource && !ReadyTarget)
            {
                long completion = _clockInput.TimeAfterEdges((_clockInput.NextHigh ? 0 : 1) + 2 * (_transferClocks - 1));
                if (due is null || completion < due) due = completion;
            }
            return due;
        }
    }

    private void DetachClockSource()
    {
        if (!_hasClockSource) return;
        _hasClockSource = false;
        PublishReadyDeadline();
    }

    private void AdvancePeriodicClockTo(long timeNanoseconds, bool includeTargetEdge)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(timeNanoseconds, TimeNanoseconds);
        if (_publishing || _advancingClockSource)
        {
            if (timeNanoseconds != TimeNanoseconds)
                throw new InvalidOperationException("Cannot advance PSG clock from an output callback.");
            return;
        }
        _advancingClockSource = true;
        try
        {
            while (TimeNanoseconds < timeNanoseconds || includeTargetEdge && _clockInput.NextTime == timeNanoseconds)
            {
                int distance = ClocksUntilNextEvent;
                long clockDate = _clockInput.TimeAfterEdges((_clockInput.NextHigh ? 0 : 1) + 2 * (distance - 1));
                long next = Math.Min(clockDate, timeNanoseconds);
                if (_readyDue is { } ready && ready < next) next = ready;
                // No divider/transfer boundary occurs in this prefix. All
                // input levels and clocks are materialized before observing READY.
                int edges = _clockInput.EdgesBefore(next);
                if (edges != 0)
                {
                    int rising = (edges + (_clockInput.NextHigh ? 1 : 0)) / 2;
                    _clockInput.Advance(edges);
                    _clockHigh = !_clockInput.NextHigh;
                    AdvanceClocks(rising);
                }
                AdvanceTo(next); // Canonical timed READY resolver; guarded against source recursion.
                if (clockDate == next && (includeTargetEdge || next < timeNanoseconds))
                {
                    _clockHigh = true;
                    _clockInput.Advance(1);
                    Tick();
                }
                // An arbitrary observation may coincide with an input edge
                // before the next divider boundary. Materialize that edge too.
                if (includeTargetEdge && _clockInput.NextTime == next)
                {
                    _clockHigh = _clockInput.NextHigh;
                    _clockInput.Advance(1);
                    if (_clockHigh) Tick();
                }
                PublishReadyDeadline();
                if (next == timeNanoseconds) break;
            }
            AdvanceTo(timeNanoseconds);
        }
        finally { _advancingClockSource = false; }
    }
}
