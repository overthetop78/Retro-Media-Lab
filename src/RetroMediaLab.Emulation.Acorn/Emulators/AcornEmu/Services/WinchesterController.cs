using RetroMediaLab.Chips.Controllers.Acorn.Winchester.Scsi.Enums;
using RetroMediaLab.Chips.Controllers.Acorn.Winchester.Scsi.Services;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.WinchesterDriveConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;

/// <summary>Câblage de l'hôte Acorn, du port cible ACB-4000 et des deux mécanismes empruntés.</summary>
internal sealed class WinchesterController
{
    private readonly ScsiHostAdapter host = new();
    private readonly Acb4000 target;
    private readonly WinchesterTrackSignals signals;
    private readonly int[] speeds = [C.NormalSpeedPercent, C.NormalSpeedPercent];
    private int clockFraction;
    private uint deferredIdleTicks;
    private bool canDeferClock;
    private bool normalSpeedIdle;

    internal WinchesterController(Func<int, WinchesterStorage?> storage)
    {
        ArgumentNullException.ThrowIfNull(storage);
        target = new Acb4000(unit => storage(unit), C.OwnerClock, C.SequencerClock);
        signals = new WinchesterTrackSignals(target, storage, C.OwnerClock);
        Synchronize();
    }

    internal bool Irq => host.Irq;
    internal Action<bool>? IrqChanged { get => host.IrqChanged; set => host.IrqChanged = value; }
    internal bool Busy => target.Bus.Busy;
    internal int Unit => target.Unit;
    internal bool Active(int unit) => target.Unit == unit && (target.ReadGate || target.WriteGate);
    internal bool Writing(int unit) => target.Unit == unit && target.WriteGate;
    internal long SeekCount(int unit) => target.SeekCount(unit);
    internal long ReadBlocks(int unit) => target.ReadBlocks(unit);
    internal long WrittenBlocks(int unit) => target.WrittenBlocks(unit);
    internal void RefreshDrives()
    {
        FlushIdleClock();
        signals.RefreshDrives();
    }

    internal void ConfigureSpeed(int unit, int speedPercent)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)unit, (uint)C.DriveCount);
        ArgumentOutOfRangeException.ThrowIfNegative(speedPercent);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(speedPercent, C.MaximumSpeedPercent);
        FlushIdleClock();
        speeds[unit] = speedPercent;
    }

    internal byte Read(int register)
    {
        FlushIdleClock();
        Synchronize();
        var data = host.Read(register);
        Synchronize();
        return data;
    }

    internal void Write(int register, byte value)
    {
        FlushIdleClock();
        host.Write(register, value);
        Synchronize();
    }

    internal void Tick()
    {
        if (normalSpeedIdle)
        {
            // La cible ne peut changer de sélection sans accès externe, lequel
            // restitue d'abord ces périodes et invalide cet état de repos.
            if (++deferredIdleTicks == C.MaximumDeferredTicks) FlushIdleClock();
            return;
        }
        var speed = speeds[target.Unit < C.DriveCount ? target.Unit : 0];
        if (speed == C.UnlimitedSpeed)
        {
            // Yield to the machine between bounded batches; this is not a wall-clock speed limit.
            for (var tick = 0; tick < C.MaximumTicksPerCall; tick++)
            {
                TickHardware();
                if (!target.Bus.Busy || target.Bus.Request) break;
            }
            return;
        }
        clockFraction += speed;
        while (clockFraction >= C.NormalSpeedPercent)
        {
            clockFraction -= C.NormalSpeedPercent;
            TickHardware();
        }
    }

    private void TickHardware()
    {
        if (canDeferClock)
        {
            if (++deferredIdleTicks == C.MaximumDeferredTicks) FlushIdleClock();
            return;
        }
        // Host outputs were resolved by the last register access or preceding tick.
        // With a free bus and no selection, mechanics cannot change the SCSI wires.
        var busCanChange = target.Bus.Busy || host.Select;
        signals.Tick();
        if (busCanChange || target.Bus.Busy) Synchronize();
        canDeferClock = !host.Select && signals.CanAdvanceIdleClock;
        normalSpeedIdle = canDeferClock
            && speeds[target.Unit < C.DriveCount ? target.Unit : 0] == C.NormalSpeedPercent;
    }

    private void FlushIdleClock()
    {
        if (deferredIdleTicks != 0)
        {
            signals.AdvanceIdleTicks(deferredIdleTicks);
            deferredIdleTicks = 0;
        }
        canDeferClock = false;
        normalSpeedIdle = false;
    }

    internal void Reset()
    {
        FlushIdleClock();
        host.SetReset(true);
        try { signals.Reset(); }
        finally
        {
            try
            {
                target.Reset();
                clockFraction = 0;
                Synchronize();
            }
            finally
            {
                host.SetReset(false);
                Synchronize();
            }
        }
    }

    private void Synchronize()
    {
        // Target REQ/BSY asynchronously clear the host ACK/SEL flip-flops. Propagate those
        // cleared outputs back before another host access; this does not advance either clock.
        target.SetInitiator(host.OutputData, host.Select, host.Acknowledge, host.ResetAsserted);
        PublishTarget();
        target.SetInitiator(host.OutputData, host.Select, host.Acknowledge, host.ResetAsserted);
        PublishTarget();
    }

    private void PublishTarget()
    {
        var bus = target.Bus;
        var state = ScsiBusSignals.None;
        if (bus.Busy) state |= ScsiBusSignals.Busy;
        if (bus.Request) state |= ScsiBusSignals.Request;
        if (bus.InputOutput) state |= ScsiBusSignals.InputOutput;
        if (bus.ControlData) state |= ScsiBusSignals.ControlData;
        if (bus.Message) state |= ScsiBusSignals.Message;
        host.SetTarget(state, bus.OutputData);
    }
}
