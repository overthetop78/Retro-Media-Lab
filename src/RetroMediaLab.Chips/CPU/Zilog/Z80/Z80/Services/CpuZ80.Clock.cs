using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Enums;

namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Services;

public sealed partial class CpuZ80
{
    /// <summary>Reçoit un train fini de fronts alternés de la source extérieure,
    /// en commençant par le niveau opposé au CLK reçu. La source borne ce train
    /// avant toute entrée extérieure qui n'est pas livrée par les callbacks du bus.
    /// Chaque transaction et chaque notification conserve sa date matérielle.</summary>
    /// <remarks>Ne crée ni oscillateur, ni horloge autonome, ni état de source.
    /// Les fronts isolés continuent d'emprunter exactement le même séquenceur.</remarks>
    public void ReceiveClockEdges(long firstEdgeNanoseconds, long halfPeriodNanoseconds, int edgeCount)
    {
        if (_faulted) throw new InvalidOperationException("The Z80 decoder is stopped.");
        if (_insideClock || _publishingDeadline)
            throw new InvalidOperationException("Cannot recursively drive the CPU clock.");
        _bus.ValidateClockTrain(firstEdgeNanoseconds, halfPeriodNanoseconds, edgeCount);
        long next = firstEdgeNanoseconds;
        int remaining = edgeCount;
        while (remaining != 0)
        {
            int stable = _internalClocksRemaining >= 3 ? StableInternalEdges(remaining) : 0;
            if (stable >= 4)
            {
                int rising = (stable + (_bus.ClockHigh ? 0 : 1)) / 2;
                _internalClocksRemaining -= rising;
                _bus.AdvanceStableClockEdges(next, halfPeriodNanoseconds, stable);
                remaining -= stable;
                if (remaining != 0) next = checked(next + stable * halfPeriodNanoseconds);
                continue;
            }
            // Les propagations entre fronts publient leurs échéances normalement,
            // comme AdvanceTo suivi de SetClock ; ne pas les différer en fin de train.
            _bus.AdvanceTo(next);
            _insideClock = true;
            try { ApplyClockEdge(!_bus.ClockHigh); }
            finally { _insideClock = false; if (_deadlineListener is not null) PublishDeadline(); }
            if (--remaining != 0) next = checked(next + halfPeriodNanoseconds);
        }
    }

    private int StableInternalEdges(int available)
    {
        if (_internalClocksRemaining < 3 || _bus.IsActive || _bus.NextEventTimestamp != long.MaxValue
            || _control.ResetRequested || _control.ResetSampled || _control.ResetHolding
            || _control.BusGranted || _control.ResumePending || _control.PendingHalt.HasValue)
            return 0;
        int boundary = _flow == CpuZ80Flow.InternalOperation && _internalClocksRemaining >= 4
            && _operation is CpuZ80Operation.AddPair or CpuZ80Operation.AddCarryPair or CpuZ80Operation.SubtractCarryPair
            ? 4 : 1;
        // Garder le front qui échantillonne le dernier T, et la frontière M elle-même.
        int rising = _internalClocksRemaining - boundary - 1;
        return rising <= 0 ? 0 : Math.Min(available, 2 * rising + (_bus.ClockHigh ? 1 : 0));
    }
}
