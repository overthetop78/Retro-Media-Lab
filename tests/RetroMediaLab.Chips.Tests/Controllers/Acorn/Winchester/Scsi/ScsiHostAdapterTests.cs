using RetroMediaLab.Chips.Controllers.Acorn.Winchester.Scsi.Enums;
using RetroMediaLab.Chips.Controllers.Acorn.Winchester.Scsi.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Acorn.Winchester.Scsi;

public sealed class ScsiHostAdapterTests
{
    public static IEnumerable<object[]> SignalStates()
    {
        byte[] masks = [0x01, 0x02, 0x20, 0x40, 0x80];
        for (var bits = 0; bits < 32; bits++)
        {
            byte state = 0;
            for (var index = 0; index < masks.Length; index++)
                if ((bits & (1 << index)) != 0) state |= masks[index];
            yield return [state, false];
            yield return [state, true];
        }
    }

    [Theory]
    [MemberData(nameof(SignalStates))]
    public void StatusMuxAndInterruptFollowEveryInputCombination(byte state, bool enabled)
    {
        var host = new ScsiHostAdapter();
        host.SetTarget((ScsiBusSignals)state, 0x69);
        host.Write(0, 0xa5);
        host.Write(3, enabled ? (byte)1 : (byte)0);
        var irq = enabled && (state & 0x20) != 0;
        Assert.Equal(irq, host.Irq);
        Assert.Equal((byte)(state | (irq ? 0x10 : 0)), host.Read(1));
        Assert.Equal((state & 0x40) == 0 ? 0xa5 : 0x69, host.Read(0));
        Assert.Equal((state & 0x40) == 0, host.DrivesData);
        Assert.Equal((state & 0x20) != 0, host.Acknowledge);
        Assert.False(host.Select);
    }

    [Fact]
    public void OnlyD0EnablesIrqAndAnAlreadyAssertedRequestSetsTheLatch()
    {
        for (var value = 0; value <= byte.MaxValue; value++)
        {
            var host = new ScsiHostAdapter();
            host.SetTarget(ScsiBusSignals.Request, 0);
            host.Write(3, (byte)value);
            Assert.Equal((value & 1) != 0, host.IrqEnabled);
            Assert.Equal((value & 1) != 0, host.Irq);
        }
    }

    [Fact]
    public void IrqWaitsForRequestAndRemainsLatchedUntilDisabled()
    {
        var host = new ScsiHostAdapter();
        Assert.Equal(0, host.Read(1));
        host.Write(3, 1);
        Assert.False(host.Irq);
        host.SetTarget(ScsiBusSignals.Request, 0);
        Assert.True(host.Irq);
        host.SetTarget(ScsiBusSignals.None, 0);
        Assert.True(host.Irq);
        Assert.Equal(0x10, host.Read(1));
        host.Read(0); host.Write(0, 0x55); host.Write(2, 0);
        Assert.True(host.Irq);
        host.Write(3, 0);
        Assert.False(host.Irq);
        Assert.Equal(0, host.Read(1));
    }

    [Fact]
    public void SelectIsASeparateFlipFlopClearedAsynchronouslyByBusy()
    {
        var host = new ScsiHostAdapter();
        host.Write(0, 2);
        host.Write(1, 255);
        host.Write(3, 1);
        Assert.False(host.Select);
        host.Write(2, 0);
        Assert.True(host.Select);
        host.SetTarget(ScsiBusSignals.Busy, 0);
        Assert.False(host.Select);
        host.Write(2, 255);
        Assert.False(host.Select);
        host.SetTarget(ScsiBusSignals.None, 0);
        Assert.False(host.Select);
        host.Write(2, 123);
        Assert.True(host.Select);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DataAccessLatchesAckUntilTheTargetDropsRequest(bool writing)
    {
        var host = new ScsiHostAdapter();
        host.SetTarget(ScsiBusSignals.Busy | ScsiBusSignals.Request | ScsiBusSignals.InputOutput, 0x5a);
        host.Read(1);
        Assert.False(host.Acknowledge);
        if (writing) host.Write(0, 0xa5); else Assert.Equal(0x5a, host.Read(0));
        Assert.True(host.Acknowledge);
        host.SetTarget(ScsiBusSignals.Busy | ScsiBusSignals.Request, 0);
        Assert.True(host.Acknowledge);
        host.SetTarget(ScsiBusSignals.Busy, 0);
        Assert.False(host.Acknowledge);
        if (writing) host.Write(0, 0xa5); else host.Read(0);
        Assert.False(host.Acknowledge);
    }

    [Fact]
    public void ResetAffectsOnlyTheFlipFlopsWiredToNrst()
    {
        var host = new ScsiHostAdapter();
        host.Write(0, 0xa5);
        host.Write(2, 0);
        host.SetTarget(ScsiBusSignals.Request, 0);
        host.Read(0);
        host.Write(3, 1);
        Assert.True(host.Acknowledge);
        Assert.True(host.Irq);
        Assert.True(host.Select);
        host.SetReset(true);
        Assert.False(host.Select);
        Assert.False(host.Irq);
        Assert.False(host.IrqEnabled);
        Assert.True(host.Acknowledge); // ACK's /CLR is REQ, not NRST.
        Assert.Equal(0xa5, host.OutputData);
        host.Write(3, 1); host.Write(2, 0);
        Assert.False(host.IrqEnabled);
        Assert.False(host.Select);
        host.SetTarget(ScsiBusSignals.None, 0); // The target releases its bus in response to RESET.
        Assert.False(host.Acknowledge);
        host.SetReset(false);
        Assert.Equal(0xa5, host.Read(0));
        Assert.Equal(0, host.Read(1));
    }

    [Fact]
    public void DataLatchReadbackDoesNotSelectOrResetTheTarget()
    {
        var host = new ScsiHostAdapter();
        for (var value = 0; value <= byte.MaxValue; value++)
        {
            host.Write(0, (byte)value);
            Assert.Equal(value, host.Read(0));
            Assert.False(host.Select);
            Assert.False(host.Acknowledge);
            Assert.Equal(0, host.Read(1));
            host.SetTarget(ScsiBusSignals.InputOutput, (byte)~value);
            Assert.Equal((byte)~value, host.Read(0));
            host.SetTarget(ScsiBusSignals.None, 0);
        }
    }

    [Fact]
    public void UndecodedAccessesDoNotInventControlSignals()
    {
        var host = new ScsiHostAdapter();
        host.SetTarget((ScsiBusSignals)0xff, 0x5a);
        Assert.Equal(0xe3, host.Read(1)); // Reserved bits and IRQ are not target pins.
        Assert.Equal(255, host.Read(2));
        Assert.Equal(255, host.Read(3));
        host.Write(1, 255);
        Assert.False(host.Select);
        Assert.False(host.IrqEnabled);
        Assert.False(host.Irq);
        Assert.False(host.Acknowledge);
    }
}
