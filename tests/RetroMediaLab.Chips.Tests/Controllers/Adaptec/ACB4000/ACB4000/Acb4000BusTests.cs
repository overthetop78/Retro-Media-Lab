using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;
using RetroMediaLab.Chips.Controllers.Acorn.Winchester.Scsi.Enums;
using RetroMediaLab.Chips.Controllers.Acorn.Winchester.Scsi.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed class Acb4000BusTests
{
    [Theory]
    [InlineData(2_000_000)]
    [InlineData(10_000_000)]
    public void OutputNotificationsFollowMutationsAndLeaveStableClocksSilent(int clock)
    {
        var bus = new Acb4000Bus(clock);
        var previous = Outputs();
        var notifications = 0;
        bus.OutputsChanged += () =>
        {
            var current = Outputs();
            Assert.NotEqual(previous, current);
            previous = current;
            notifications++;
        };
        Step(() => Tick(bus, 100));
        Assert.Equal(0, notifications);
        Step(() => bus.SetInitiator(1, true, false, false));
        Step(() => Tick(bus, 5));
        Assert.True(bus.Busy);
        Step(() => bus.SetInitiator(0, false, false, false));
        foreach (var phase in new[] { Acb4000BusPhase.Command, Acb4000BusPhase.DataOut,
            Acb4000BusPhase.DataIn, Acb4000BusPhase.Status, Acb4000BusPhase.MessageIn })
        {
            Step(() => Assert.True(bus.TryBeginTransfer(phase, 0xa5)));
            Step(() => Tick(bus, 5));
            Assert.True(bus.Request);
            var count = notifications;
            Step(() => Tick(bus, 100));
            Assert.Equal(count, notifications);
            Step(() => bus.SetInitiator(0x5a, false, true, false));
            Assert.False(bus.Request);
            Step(() => bus.SetInitiator(0x5a, false, false, false));
            Step(() => Assert.True(bus.TryTakeTransferredByte(out _)));
        }
        Step(() => Assert.True(bus.TryReleaseBus()));
        Step(() => bus.SetManualOutputs(true, true, true, 0x55));
        var manual = notifications;
        Step(() => bus.SetManualOutputs(true, true, true, 0x55));
        Assert.Equal(manual, notifications);
        Step(() => bus.SetManualOutputs(false, false, false, 0x55));
        Step(() => bus.SetInitiator(1, true, false, false));
        Step(() => Tick(bus, 5));
        Step(() => bus.SetInitiator(0, false, false, false));
        Step(() => Assert.True(bus.TryBeginTransfer(Acb4000BusPhase.Command)));
        Step(() => Tick(bus, 5));
        Step(() => Tick(bus, clock / 4));
        Assert.True(bus.TimedOut);
        Step(() => bus.SetManualOutputs(true, true, true, 0xa5));
        Step(() => bus.SetInitiator(0, false, false, true));
        Assert.False(bus.Busy);
        Assert.False(bus.Select);

        (bool, bool, bool, bool, bool, bool, byte) Outputs() =>
            (bus.Busy, bus.Select, bus.Request, bus.InputOutput, bus.ControlData, bus.Message, bus.OutputData);
        void Step(Action action)
        {
            action();
            Assert.Equal(Outputs(), previous);
        }
    }

    [Theory]
    [InlineData(Acb4000BusPhase.Command)]
    [InlineData(Acb4000BusPhase.DataOut)]
    public void IncomingTransferSamplesBothAssertedDataDriversAtAcknowledge(Acb4000BusPhase phase)
    {
        var bus = Selected();
        Assert.True(bus.TryBeginTransfer(phase));
        bus.SetManualOutputs(false, false, true, 0x05);
        Tick(bus, 5);
        Assert.True(bus.Request);
        bus.SetInitiator(0x30, false, true, false);
        Assert.False(bus.Request);
        bus.SetInitiator(0x30, false, false, false);
        Assert.True(bus.TryTakeTransferredByte(out var value));
        Assert.Equal(0x35, value);
    }

    [Fact]
    public void DmaControlledDataHasNoSecondAutomaticDriverWhenBoeReleases()
    {
        var bus = Selected();
        Assert.True(bus.TryBeginTransfer(Acb4000BusPhase.DataIn, 0x42));
        bus.SetManualOutputs(false, false, true, 0xa5, dmaControlsData: true);
        Assert.Equal(0xa5, bus.OutputData);
        Assert.Equal(0xa5, bus.SampleHostData());
        bus.SetManualOutputs(false, false, false, 0xa5, dmaControlsData: true);
        Assert.Equal(0, bus.OutputData);
        Assert.Equal(0, bus.SampleHostData());
        Assert.Equal(Acb4000BusPhase.DataIn, bus.Phase);
        Assert.True(bus.Busy);
    }

    [Fact]
    public void ManualSignalsDoNotCreateAnAutomaticCommandConnection()
    {
        var bus = new Acb4000Bus(10_000_000);
        bus.SetManualOutputs(true, true, true, 0xa5);
        Assert.True(bus.Select);
        Assert.True(bus.Busy);
        Assert.False(bus.ProtocolBusy);
        Assert.Equal(Acb4000BusPhase.BusFree, bus.Phase);
        Assert.Equal(0xa5, bus.OutputData);
        Assert.False(bus.TryBeginTransfer(Acb4000BusPhase.Command));
        bus.SetInitiator(1, true, false, false);
        Tick(bus, 20);
        Assert.False(bus.ProtocolBusy);
        bus.SetManualOutputs(false, false, false, 0xa5);
        Assert.True(bus.Select); // The initiator still holds SEL.
        bus.SetInitiator(1, true, false, false);
        Tick(bus, 5);
        Assert.True(bus.ProtocolBusy);
    }

    [Fact]
    public void ManualDataAndInitiatorDataResolveAsAssertedOpenCollectorBits()
    {
        var bus = new Acb4000Bus(10_000_000);
        bus.SetInitiator(0x30, false, false, false);
        bus.SetManualOutputs(false, false, true, 0x05);
        Assert.Equal(0x35, bus.SampleHostData());
        Assert.Equal(0x05, bus.OutputData);
        bus.SetManualOutputs(false, false, false, 0x05);
        Assert.Equal(0x30, bus.SampleHostData());
    }

    [Fact]
    public void TargetDataPhaseDoesNotReadBackTheInactiveInitiatorLatch()
    {
        var bus = Selected();
        Assert.True(bus.TryBeginTransfer(Acb4000BusPhase.DataIn, 0x42));
        bus.SetInitiator(0xff, false, false, false);
        bus.SetManualOutputs(false, false, true, 0x09);
        Assert.Equal(0x4b, bus.SampleHostData());
        Assert.Equal(0x4b, bus.OutputData);
    }

    [Fact]
    public void ResetReleasesManualDriversAndPreventsReassertionWhileHeld()
    {
        var bus = new Acb4000Bus(10_000_000);
        bus.SetManualOutputs(true, true, true, 0xa5);
        bus.SetInitiator(0, false, false, true);
        bus.SetManualOutputs(true, true, true, 0xa5);
        Assert.False(bus.Select);
        Assert.False(bus.Busy);
        Assert.Equal(0, bus.SampleHostData());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void OnlyStrappedTargetRespondsAndBusyWaitsForSettle(byte id)
    {
        var bus = new Acb4000Bus(10_000_000, id);
        bus.SetInitiator((byte)~(1 << id), true, false, false);
        Tick(bus, 10);
        Assert.False(bus.Busy);
        bus.SetInitiator((byte)(1 << id), true, false, false);
        Tick(bus, 4);
        Assert.False(bus.Busy);
        bus.Tick();
        Assert.True(bus.Busy);
        Assert.Equal(Acb4000BusPhase.Selected, bus.Phase);
        Assert.False(bus.Request);
        Assert.False(bus.TryBeginTransfer(Acb4000BusPhase.Command));
        bus.SetInitiator(0, false, false, false);
        Assert.True(bus.TryBeginTransfer(Acb4000BusPhase.Command));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DroppingSelectOrIdBeforeResponseAbandonsSelection(bool dropId)
    {
        var bus = new Acb4000Bus(10_000_000);
        bus.SetInitiator(1, true, false, false);
        Tick(bus, 4);
        bus.SetInitiator(dropId ? (byte)0 : (byte)1, dropId, false, false);
        Tick(bus, 20);
        Assert.False(bus.Busy);
        Assert.Equal(Acb4000BusPhase.BusFree, bus.Phase);
    }

    [Theory]
    [InlineData(Acb4000BusPhase.DataOut, false, false, false)]
    [InlineData(Acb4000BusPhase.DataIn, true, false, false)]
    [InlineData(Acb4000BusPhase.Command, false, true, false)]
    [InlineData(Acb4000BusPhase.Status, true, true, false)]
    [InlineData(Acb4000BusPhase.MessageOut, false, true, true)]
    [InlineData(Acb4000BusPhase.MessageIn, true, true, true)]
    public void PhasePinsAndTransferredDataFollowDirection(Acb4000BusPhase phase, bool io, bool cd, bool message)
    {
        var bus = Selected();
        Assert.True(bus.TryBeginTransfer(phase, 0xa5));
        Assert.Equal(io, bus.InputOutput);
        Assert.Equal(cd, bus.ControlData);
        Assert.Equal(message, bus.Message);
        Assert.False(bus.Request);
        Tick(bus, 5);
        Assert.True(bus.Request);
        bus.SetInitiator(0x3c, false, true, false);
        Assert.False(bus.Request);
        Assert.False(bus.TransferCompleted);
        Assert.Equal(0xa5, bus.OutputData);
        Assert.False(bus.TryBeginTransfer(Acb4000BusPhase.Status));
        Assert.False(bus.TryReleaseBus());
        Tick(bus, 20);
        Assert.Equal(phase, bus.Phase);
        Assert.Equal(0xa5, bus.OutputData);
        // Changing input data after ACK must not change the byte sampled on the ACK assertion.
        bus.SetInitiator(0xff, false, false, false);
        Assert.True(bus.TransferCompleted);
        Assert.False(bus.TryBeginTransfer(Acb4000BusPhase.Status));
        Assert.True(bus.TryTakeTransferredByte(out var value));
        Assert.Equal(io ? (byte)0xa5 : (byte)0x3c, value);
        Assert.False(bus.TryTakeTransferredByte(out _));
        Assert.True(bus.TryReleaseBus());
        Assert.False(bus.Busy);
        Assert.False(bus.InputOutput);
        Assert.False(bus.ControlData);
        Assert.False(bus.Message);
    }

    [Theory]
    [InlineData(1_000_000, 1, 1)]
    [InlineData(2_000_000, 1, 1)]
    [InlineData(10_000_000, 5, 1)]
    [InlineData(16_000_000, 8, 1)]
    [InlineData(100_000_000, 45, 5)]
    public void PhaseSettleAndFollowingDataDeskewRoundUpToOwnerClock(int clock, int phaseTicks, int dataTicks)
    {
        var bus = Selected(clock);
        Assert.True(bus.TryBeginTransfer(Acb4000BusPhase.DataIn, 0x59));
        Tick(bus, phaseTicks - 1);
        Assert.False(bus.Request);
        bus.Tick();
        Assert.True(bus.Request);
        Complete(bus);
        Assert.True(bus.TryBeginTransfer(Acb4000BusPhase.DataIn, 0x96));
        Tick(bus, dataTicks - 1);
        Assert.False(bus.Request);
        bus.Tick();
        Assert.True(bus.Request);
        Complete(bus);
        Assert.True(bus.TryBeginTransfer(Acb4000BusPhase.Status));
        Tick(bus, phaseTicks - 1);
        Assert.False(bus.Request);
        bus.Tick();
        Assert.True(bus.Request);
    }

    [Fact]
    public void EarlyAcknowledgeCannotCompleteAByteBeforeRequest()
    {
        var bus = Selected();
        Assert.True(bus.TryBeginTransfer(Acb4000BusPhase.Command));
        bus.SetInitiator(0x08, false, true, false);
        Tick(bus, 20);
        Assert.False(bus.Request);
        Assert.False(bus.TransferCompleted);
        bus.SetInitiator(0x0a, false, false, false);
        bus.Tick();
        Assert.True(bus.Request);
        bus.SetInitiator(0x0a, false, true, false);
        bus.SetInitiator(0, false, false, false);
        Assert.True(bus.TryTakeTransferredByte(out var data));
        Assert.Equal(0x0a, data);
    }

    [Fact]
    public void NoAcknowledgeTimesOutOnlyAfterTheFullNominalInterval()
    {
        var bus = Selected(2_000_000);
        Assert.True(bus.TryBeginTransfer(Acb4000BusPhase.Command));
        bus.Tick();
        Assert.True(bus.Request);
        Tick(bus, 499_999);
        Assert.True(bus.Request);
        Assert.False(bus.TimedOut);
        bus.Tick();
        Assert.True(bus.TimedOut);
        Assert.False(bus.Busy);
        Assert.False(bus.Request);
        Assert.False(bus.TransferCompleted);
        bus.SetInitiator(1, true, false, false);
        Assert.False(bus.TimedOut);
        bus.Tick();
        Assert.True(bus.Busy);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void ResetReleasesEveryStageAndPreventsDelayedCompletion(int stage)
    {
        var bus = new Acb4000Bus(10_000_000);
        if (stage > 0) bus.SetInitiator(1, true, false, false);
        if (stage > 1) { Tick(bus, 5); bus.SetInitiator(0, false, false, false); }
        if (stage > 2) bus.TryBeginTransfer(Acb4000BusPhase.DataIn, 0xa5);
        if (stage > 3) Tick(bus, 5);
        if (stage > 4) bus.SetInitiator(0, false, true, false);
        bus.SetInitiator(0, false, false, true);
        Assert.True(bus.ResetAsserted);
        Assert.False(bus.Busy);
        Assert.False(bus.Request);
        Assert.False(bus.TransferCompleted);
        Assert.False(bus.InputOutput);
        Assert.False(bus.ControlData);
        Assert.False(bus.Message);
        Assert.Equal(Acb4000BusPhase.BusFree, bus.Phase);
        Assert.False(bus.TryBeginTransfer(Acb4000BusPhase.Command));
        Tick(bus, 20);
        bus.SetInitiator(0, false, false, false);
        Tick(bus, 20);
        Assert.False(bus.Busy);
        Assert.False(bus.TransferCompleted);
    }

    [Fact]
    public void AcornHostLatchClosesAllFourHandshakeEdgesInBothDirections()
    {
        var host = new ScsiHostAdapter();
        var bus = new Acb4000Bus(2_000_000);
        host.Write(0, 1);
        host.Write(2, 0);
        Drive(bus, host);
        bus.Tick();
        Publish(bus, host);
        Assert.False(host.Select);
        Drive(bus, host);
        foreach (var phase in new[] { Acb4000BusPhase.Command, Acb4000BusPhase.DataOut, Acb4000BusPhase.DataIn,
            Acb4000BusPhase.Status, Acb4000BusPhase.MessageIn })
        {
            Assert.True(bus.TryBeginTransfer(phase, 0xa5));
            bus.Tick();
            Publish(bus, host);
            Assert.True((host.Read(1) & 0x20) != 0);
            if (bus.InputOutput) Assert.Equal(0xa5, host.Read(0));
            else host.Write(0, 0x3c);
            Assert.True(host.Acknowledge);
            Drive(bus, host);
            Assert.False(bus.Request);
            Assert.False(bus.TransferCompleted);
            Publish(bus, host);
            Assert.False(host.Acknowledge);
            Drive(bus, host);
            Assert.True(bus.TryTakeTransferredByte(out var data));
            Assert.Equal(bus.InputOutput ? (byte)0xa5 : (byte)0x3c, data);
        }
        Assert.True(bus.TryReleaseBus());
        Publish(bus, host);
        Assert.Equal(0, host.Read(1));
    }

    [Fact]
    public void AttentionIsAnInputAndDoesNotSilentlyChangeAnActivePhase()
    {
        var bus = Selected();
        bus.TryBeginTransfer(Acb4000BusPhase.DataIn, 0xa5);
        Tick(bus, 5);
        bus.SetInitiator(0, false, false, false, attention: true);
        Assert.True(bus.Attention);
        Assert.Equal(Acb4000BusPhase.DataIn, bus.Phase);
        Assert.True(bus.Request);
    }

    [Fact]
    public void InvalidClockTargetAndReservedPhasesAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Acb4000Bus(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Acb4000Bus(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Acb4000Bus(2_000_000, 8));
        var bus = Selected();
        foreach (var phase in new[] { Acb4000BusPhase.BusFree, Acb4000BusPhase.Selected, (Acb4000BusPhase)4, (Acb4000BusPhase)5 })
            Assert.Throws<ArgumentOutOfRangeException>(() => bus.TryBeginTransfer(phase));
    }

    private static void Publish(Acb4000Bus bus, ScsiHostAdapter host)
    {
        var signals = ScsiBusSignals.None;
        if (bus.Busy) signals |= ScsiBusSignals.Busy;
        if (bus.Request) signals |= ScsiBusSignals.Request;
        if (bus.InputOutput) signals |= ScsiBusSignals.InputOutput;
        if (bus.ControlData) signals |= ScsiBusSignals.ControlData;
        if (bus.Message) signals |= ScsiBusSignals.Message;
        host.SetTarget(signals, bus.OutputData);
    }

    private static void Drive(Acb4000Bus bus, ScsiHostAdapter host) =>
        bus.SetInitiator(host.OutputData, host.Select, host.Acknowledge, host.ResetAsserted);

    private static Acb4000Bus Selected(int clock = 10_000_000)
    {
        var bus = new Acb4000Bus(clock);
        bus.SetInitiator(1, true, false, false);
        Tick(bus, (int)((450L * clock + 999_999_999) / 1_000_000_000));
        Assert.True(bus.Busy);
        bus.SetInitiator(0, false, false, false);
        return bus;
    }

    private static void Complete(Acb4000Bus bus)
    {
        bus.SetInitiator(0, false, true, false);
        bus.SetInitiator(0, false, false, false);
        Assert.True(bus.TryTakeTransferredByte(out _));
    }

    private static void Tick(Acb4000Bus bus, int count)
    {
        for (var tick = 0; tick < count; tick++) bus.Tick();
    }
}
