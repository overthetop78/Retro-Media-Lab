using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;

namespace RetroMediaLab.Tests.Emulation;

public sealed partial class AcornEmuWinchesterStorageTests
{
    [Theory]
    [InlineData(1, 100)]
    [InlineData(2_000_003, 100)]
    [InlineData(100_001, 200)]
    [InlineData(100_001, 0)]
    public void ControllerAbsoluteClockMatchesUnitTicksAcrossIdleCommandsResetAndSpeed(long idleTicks, int speed)
    {
        var stepped = Run(false);
        var advanced = Run(true);
        Assert.Equal(stepped.Data, advanced.Data);
        Assert.Equal(stepped.Time, advanced.Time);
        Assert.Equal(stepped.Interrupts, advanced.Interrupts);

        (byte[] Data, long Time, (long, bool)[] Interrupts) Run(bool grouped)
        {
            using var image = new Image(256, 33);
            var storage = SignalStorage(image);
            var controller = new WinchesterController(unit => unit == 0 ? storage : null);
            var interrupts = new List<(long, bool)>();
            long time = 0;
            controller.IrqChanged = asserted => interrupts.Add((time, asserted));
            try
            {
                Idle(idleTicks);
                Assert.Null(controller.NextClockEvent);
                var first = HostCommand(controller.Read, controller.Write, Clock, () => controller.Busy, [8, 0, 0, 1, 1, 0]);
                Assert.Equal(0, first.Status);
                Assert.Equal(image.Data.AsSpan(256, 256).ToArray(), first.Data);
                controller.ConfigureSpeed(0, speed);
                Idle(100_003);
                controller.Reset();
                Idle(31);
                var second = HostCommand(controller.Read, controller.Write, Clock, () => controller.Busy, [8, 0, 0, 2, 1, 0]);
                Assert.Equal(0, second.Status);
                Assert.Equal(time, controller.ClockTicks);
                Assert.Throws<ArgumentOutOfRangeException>(() => controller.AdvanceTo(time - 1));
                return (first.Data.Concat(second.Data).ToArray(), time, interrupts.ToArray());
            }
            finally { controller.Reset(); }

            void Clock()
            {
                time++;
                if (grouped) controller.AdvanceTo(time);
                else controller.Tick();
            }
            void Idle(long count)
            {
                if (grouped) { time += count; controller.AdvanceTo(time); }
                else for (long index = 0; index < count; index++) Clock();
            }
        }
    }

    [Fact]
    public void MachineRelaySleepsUntilABusAccessAndWakesAtTheNextPhysicalCycle()
    {
        long time = 0;
        long wake = long.MaxValue;
        var bus = new RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services.Winchester(true, () => time)
        {
            NextClockEventChanged = next => wake = next ?? long.MaxValue
        };
        try
        {
            bus.Reset();
            Assert.Equal(1, wake);
            time = 1;
            wake = bus.AdvanceTo(time);
            Assert.Equal(long.MaxValue, wake);
            time += 2_000_003;
            Assert.Equal(long.MaxValue, wake);
            bus.Read(0);
            Assert.Equal(time + 1, wake);
            time++;
            wake = bus.AdvanceTo(time);
            Assert.Equal(long.MaxValue, wake);
        }
        finally { bus.Reset(); }
    }

    [Theory]
    [InlineData(1, 2_000_000, 0)]
    [InlineData(23333, 2_000_000, 0)]
    [InlineData(100001, 2_000_003, 0)]
    [InlineData(100001, 2_000_003, 1)]
    public void DeferredIdleRotationPreservesReadBytesAndExactTransactionTicks(uint idleTicks, int clock, int unit)
    {
        var stepped = ReadAfterIdle(false, idleTicks, clock, unit);
        var deferred = ReadAfterIdle(true, idleTicks, clock, unit);
        Assert.Equal(stepped.Bytes, deferred.Bytes);
        Assert.Equal(stepped.Ticks, deferred.Ticks);
        Assert.Equal(256, deferred.Bytes.Length);
    }

    [Fact]
    public void IdleClockCannotBypassACommandOrTheSettlingAfterSeek()
    {
        using var image = new Image(256, 33);
        var storage = SignalStorage(image);
        var target = new Acb4000(unit => unit == 0 ? storage : null, 2_000_000);
        var relay = new WinchesterTrackSignals(target, unit => unit == 0 ? storage : null);
        try
        {
            relay.Tick();
            SendSignalCommand(target, relay, [0x0b, 0, 0, 66, 0, 0]);
            Assert.False(relay.CanAdvanceIdleClock);
            Assert.Throws<InvalidOperationException>(() => relay.AdvanceIdleTicks(1));
            Assert.Equal(0, ReceiveSignalStatus(target, relay));
            Assert.False(relay.CanAdvanceIdleClock);
            Assert.Throws<InvalidOperationException>(() => relay.AdvanceIdleTicks(1));
            for (var tick = 0; tick < 100_000 && !relay.CanAdvanceIdleClock; tick++) relay.Tick();
            Assert.True(relay.CanAdvanceIdleClock);
            var steps = target.SeekCount(0);
            relay.AdvanceIdleTicks(2_000_000);
            Assert.Equal(steps, target.SeekCount(0));
        }
        finally { try { relay.Reset(); } finally { target.Reset(); } }
    }

    private static (byte[] Bytes, long Ticks) ReadAfterIdle(bool deferred, uint idleTicks, int clock, int unit)
    {
        using var image = new Image(256, 33);
        var storage = SignalStorage(image);
        var target = new Acb4000(index => index == unit ? storage : null, clock);
        var relay = new WinchesterTrackSignals(target, index => index == unit ? storage : null, clock);
        long ticks = 0;
        try
        {
            relay.Tick();
            if (deferred) relay.AdvanceIdleTicks(idleTicks - 1);
            else for (uint tick = 1; tick < idleTicks; tick++) relay.Tick();
            target.SetInitiator(1, true, false, false);
            for (var tick = 0; tick < 10 && !target.Bus.Busy; tick++) Clock();
            Assert.True(target.Bus.Busy);
            target.SetInitiator(0, false, false, false);
            byte[] command = [8, (byte)(unit << 5), 0, 1, 1, 0];
            foreach (var value in command) { Wait(); Transfer(value); }
            var data = new List<byte>();
            byte? status = null;
            while (target.Bus.Busy)
            {
                Wait();
                if (target.Bus.Phase == Acb4000BusPhase.DataIn) data.Add(target.Bus.OutputData);
                if (target.Bus.Phase == Acb4000BusPhase.Status) status = target.Bus.OutputData;
                Transfer(0);
            }
            Assert.Equal((byte)0, status);
            Assert.Equal(1, target.ReadBlocks(unit));
            Assert.Equal(image.Data.AsSpan(256, 256).ToArray(), data.ToArray());
            return (data.ToArray(), ticks);

            void Clock() { relay.Tick(); ticks++; }
            void Wait()
            {
                for (var tick = 0; tick < 2_000_000 && !target.Bus.Request; tick++) Clock();
                Assert.True(target.Bus.Request);
            }
            void Transfer(byte value)
            {
                target.SetInitiator(value, false, true, false);
                target.SetInitiator(value, false, false, false);
                Clock();
            }
        }
        finally { try { relay.Reset(); } finally { target.Reset(); } }
    }
}
