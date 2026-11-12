using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Contracts;

namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Services;

public sealed partial class CpuZ80
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
    {
        ArgumentNullException.ThrowIfNull(receiver);
        if (!_hasClockSource) throw new InvalidOperationException("No received CPU clock source.");
        if (_faulted || _insideClock || _publishingDeadline || _bus.IsPublishing || _drivingClockSource)
            throw new InvalidOperationException("Cannot recursively execute a CPU bus range.");
        if (PinsChanged is not null || _deadlineListener is not null)
            throw new InvalidOperationException("Detach scalar bus observers before range execution.");
        ArgumentOutOfRangeException.ThrowIfLessThan(boundaryExclusive, TimeNanoseconds);
        if (boundaryExclusive == TimeNanoseconds) return 0;
        _bus.BeginRangeCapture(receiver);
        _drivingClockSource = true;
        int consumed = 0;
        try
        {
            while (consumed < int.MaxValue && _clockInput.NextTime < boundaryExclusive)
            {
                if (AdvanceStableInternalPrefix(boundaryExclusive, ref consumed)) continue;
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
}
