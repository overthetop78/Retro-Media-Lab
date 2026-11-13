using RetroMediaLab.Chips.EmuChip.Common.Contracts;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Contracts;

namespace RetroMediaLab.Chips.EmuChip.CPU.Zilog.Z80.Z80.Services;

public sealed partial class EmuZ80
{
    public void ReceiveBusInputWindow(Z80BusInputWindow window) => _bus.ReceiveInputWindow(window);
    public Z80BusRangeStatistics BusRangeStatistics => _bus.RangeStatistics;

    /// <summary>Executes the received clock waveform, exchanging complete output
    /// prefixes before DATA/WAIT/INT consumption. The caller guarantees RESET,
    /// NMI and BUSRQ remain unchanged before boundaryExclusive; an external
    /// change requires ending this interval first. The receiver may update
    /// DATA/WAIT/INT, but cannot clock, restore or advance this CPU recursively.
    /// Electrical dates belong to each change, not to callback invocation time.</summary>
    public int AdvanceBusRangeUntil(long boundaryExclusive, Z80BusRangeHandler receiver)
        => ExecuteBusRangeCore(boundaryExclusive, receiver, null);

    public int ExecuteBusProgramUntil(long boundaryExclusive, EmuSignalRangeHandler receiver)
        => ExecuteBusRangeCore(boundaryExclusive, null, receiver);

    public ulong PackedPins => _bus.PackedPins;
    private long _activeCyclePortions, _activeCycleHalfEdges, _quietCycleHalfEdges;
    public long ActiveCyclePortions => _activeCyclePortions;
    public long ActiveCycleHalfEdges => _activeCycleHalfEdges;
    public long QuietCycleHalfEdges => _quietCycleHalfEdges;

    private int ExecuteBusRangeCore(long boundaryExclusive, Z80BusRangeHandler? receiver, EmuSignalRangeHandler? nativeReceiver)
    {
        if (receiver is null && nativeReceiver is null) throw new ArgumentNullException(nameof(receiver));
        if (!_hasClockSource) throw new InvalidOperationException("No received CPU clock source.");
        if (_faulted || _insideClock || _publishingDeadline || _bus.IsPublishing || _drivingClockSource)
            throw new InvalidOperationException("Cannot recursively execute a CPU bus range.");
        if (PinsChanged is not null || _deadlineListener is not null)
            throw new InvalidOperationException("Detach scalar bus observers before range execution.");
        ArgumentOutOfRangeException.ThrowIfLessThan(boundaryExclusive, TimeNanoseconds);
        if (boundaryExclusive == TimeNanoseconds) return 0;
        _bus.BeginRangeCapture(receiver, nativeReceiver);
        _drivingClockSource = true;
        int consumed = 0;
        try
        {
            while (consumed < int.MaxValue && _clockInput.NextTime < boundaryExclusive)
            {
                if (ExecuteActiveBusCycle(boundaryExclusive, ref consumed)
                    || AdvanceStableInternalPrefix(boundaryExclusive, ref consumed)) continue;
                _bus.AdvanceTo(_clockInput.NextTime);
                if (_bus.RangeChangeCount >= 96) _bus.ExchangeRangeInputs();
                _insideClock = true;
                try { ApplyClockEdge(_clockInput.NextHigh); }
                finally { _insideClock = false; }
                _clockInput.Advance(1);
                consumed++;
            }
            // Include propagations after the last clock but before the caller's
            // next external cause. Do not fabricate an extra clock edge.
            if (consumed != int.MaxValue) _bus.AdvanceTo(boundaryExclusive - 1);
            _bus.ExchangeRangeInputs();
            return consumed;
        }
        finally
        {
            _bus.EndRangeCapture();
            _drivingClockSource = false;
        }
    }
    private bool ExecuteActiveBusCycle(long boundaryExclusive, ref int consumed)
    {
        if (!_bus.IsActive || !_started || _internalClocksRemaining != 0
            || _control.ResetRequested || _control.ResetSampled || _control.ResetHolding
            || _control.BusGranted || _control.ResumePending) return false;
        int firstEdge = consumed;
        // RESET/NMI/BUSRQ are stable over the received interval by contract.
        // Hoist these invariants once per M-cycle, not once per half-edge.
        // The semantic decoder and every input capture remain the same owner.
        do
        {
            if (AdvanceQuietBusPhases(boundaryExclusive, ref consumed)) continue;
            _bus.AdvanceTo(_clockInput.NextTime);
            if (_bus.RangeChangeCount >= 96) _bus.ExchangeRangeInputs();
            bool high = _clockInput.NextHigh;
            bool completed = false, proceed = true;
            _insideClock = true;
            try
            {
                _bus.AdvanceClockEdge(high);
                if (high)
                {
                    _control.ResetSampled = false;
                    _control.ResetAtOpcodeT2 = _bus.AtOpcodeT2;
                    completed = !_bus.IsActive;
                    if (completed) proceed = CompleteReceivedBusCycle();
                    if (proceed) SampleBusCycleControl();
                }
                else CommitPendingHalt();
            }
            finally { _insideClock = false; }
            _clockInput.Advance(1);
            consumed++;
            if (completed || !proceed) break;
        }
        while (consumed < int.MaxValue && _clockInput.NextTime < boundaryExclusive);
        _activeCyclePortions++;
        _activeCycleHalfEdges += consumed - firstEdge;
        return true;
    }

    private bool AdvanceQuietBusPhases(long boundaryExclusive, ref int consumed)
    {
        if (_control.PendingHalt.HasValue) return false;
        int count = Math.Min(_bus.QuietPhaseCount, int.MaxValue - consumed);
        if (count == 0) return false;
        long last = _clockInput.TimeAfterEdges(count - 1);
        if (last >= boundaryExclusive)
        {
            count = Math.Min(count, _clockInput.EdgesBefore(boundaryExclusive));
            if (count == 0) return false;
            last = _clockInput.TimeAfterEdges(count - 1);
        }
        int rising = (count + (_clockInput.NextHigh ? 1 : 0)) / 2;
        long lastRising = rising == 0 ? 0
            : _clockInput.TimeAfterEdges((_clockInput.NextHigh ? 0 : 1) + 2 * (rising - 1));
        _bus.ExecuteQuietPhases(_clockInput.NextTime, last, lastRising, count);
        if (rising != 0)
        {
            int risingPhase = _bus.Phase - (_bus.ClockHigh ? 0 : 1);
            _control.ResetAtOpcodeT2 = _bus.Cycle == RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Enums.Z80BusCycle.OpcodeFetch
                && risingPhase == 2;
        }
        _clockInput.Advance(count);
        consumed += count;
        _quietCycleHalfEdges += count;
        return true;
    }

}
