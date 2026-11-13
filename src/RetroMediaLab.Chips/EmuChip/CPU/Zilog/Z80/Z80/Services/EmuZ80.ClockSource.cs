using RetroMediaLab.Chips.Logic.Common.Contracts;
using RetroMediaLab.Chips.EmuChip.Common.Services;

namespace RetroMediaLab.Chips.EmuChip.CPU.Zilog.Z80.Z80.Services;

public sealed partial class EmuZ80
{
    private EmuClockInput _clockInput;
    private bool _hasClockSource, _drivingClockSource;
    private long _clockSourceBoundary;

    /// <summary>Receives the established waveform of the connected clock output.
    /// Its rational phase is transport metadata, never an independent CPU oscillator.</summary>
    public void ReceiveClockSource(RationalClockSource source)
    {
        if (_insideClock || _publishingDeadline || _bus.IsPublishing || _drivingClockSource)
            throw new InvalidOperationException("Cannot replace a CPU clock source during delivery.");
        var input = new EmuClockInput(source);
        _bus.ValidateClockSource(source);
        _clockInput = input;
        _hasClockSource = true;
    }

    /// <summary>Runs the canonical CPU/bus engine before a notified external
    /// interaction. A receiver may shorten this boundary from a pin callback.</summary>
    public int AdvanceClockSourceUntil(long boundaryExclusive)
    {
        if (!_hasClockSource) throw new InvalidOperationException("No received CPU clock source.");
        if (_faulted || _insideClock || _publishingDeadline || _bus.IsPublishing || _drivingClockSource)
            throw new InvalidOperationException("Cannot recursively drive the CPU clock source.");
        ArgumentOutOfRangeException.ThrowIfLessThan(boundaryExclusive, TimeNanoseconds);
        _clockSourceBoundary = boundaryExclusive;
        _bus.SetExecutionBoundary(boundaryExclusive);
        _drivingClockSource = true;
        int consumed = 0;
        try
        {
            while (consumed < int.MaxValue && _clockInput.NextTime < _clockSourceBoundary)
            {
                if (AdvanceStableInternalPrefix(_clockSourceBoundary, ref consumed)) continue;
                if (!_bus.AdvanceTo(_clockInput.NextTime)) break;
                _insideClock = true;
                try { ApplyClockEdge(_clockInput.NextHigh); }
                finally { _insideClock = false; if (_deadlineListener is not null) PublishDeadline(); }
                _clockInput.Advance(1);
                consumed++;
            }
            return consumed;
        }
        finally
        {
            _drivingClockSource = false;
            _bus.SetExecutionBoundary(long.MaxValue);
        }
    }

    private bool AdvanceStableInternalPrefix(long boundaryExclusive, ref int consumed)
    {
        if (_internalClocksRemaining < 3) return false;
        int stable = StableInternalEdges(int.MaxValue - consumed);
        if (stable < 4) return false;
        stable = Math.Min(stable, _clockInput.EdgesBefore(boundaryExclusive));
        if (stable < 4) return false;
        int rising = (stable + (_clockInput.NextHigh ? 1 : 0)) / 2;
        long last = _clockInput.TimeAfterEdges(stable - 1);
        int lastRisingOffset = (_clockInput.NextHigh ? 0 : 1) + 2 * (rising - 1);
        long lastRising = _clockInput.TimeAfterEdges(lastRisingOffset);
        _bus.AdvanceStableClockEdges(_clockInput.NextTime, last, lastRising, stable);
        _internalClocksRemaining -= rising;
        _clockInput.Advance(stable);
        consumed += stable;
        return true;
    }

    public void LimitClockSourceBefore(long boundaryExclusive)
    {
        if (_bus.IsCapturingRange)
            throw new InvalidOperationException("A bus range must exchange replies at its input fences, not shorten a scalar execution boundary.");
        if (!_drivingClockSource) return;
        ArgumentOutOfRangeException.ThrowIfLessThan(boundaryExclusive, TimeNanoseconds);
        if (boundaryExclusive >= _clockSourceBoundary) return;
        _clockSourceBoundary = boundaryExclusive;
        _bus.SetExecutionBoundary(boundaryExclusive);
    }
}
