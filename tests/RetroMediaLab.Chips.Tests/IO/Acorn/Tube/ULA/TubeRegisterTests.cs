using RetroMediaLab.Chips.IO.Acorn.Tube.ULA.Services;

namespace RetroMediaLab.Chips.Tests.IO.Acorn.Tube.ULA;

public sealed class TubeRegisterTests
{
    [Theory]
    [InlineData(true, 0, 1)] [InlineData(false, 0, 24)]
    [InlineData(true, 1, 1)] [InlineData(false, 1, 1)]
    [InlineData(true, 3, 1)] [InlineData(false, 3, 1)]
    public void RegistersPreserveDirectionCapacityOrderAndLastData(bool hostSends, int channel, int capacity)
    {
        var tube = new TubeUla();
        var status = channel * 2;
        var data = status + 1;
        byte SenderStatus() => hostSends ? tube.HostRead(status) : tube.ParasiteRead(status);
        byte ReceiverStatus() => hostSends ? tube.ParasiteRead(status) : tube.HostRead(status);
        byte Receive() => hostSends ? tube.ParasiteRead(data) : tube.HostRead(data);
        void Send(byte value) { if (hostSends) tube.HostWrite(data, value); else tube.ParasiteWrite(data, value); }
        Assert.Equal(0x40, SenderStatus() & 0x40);
        Assert.Equal(0, ReceiverStatus() & 0x80);
        for (var index = 0; index < capacity; index++) Send((byte)(index + 1));
        Assert.Equal(0, SenderStatus() & 0x40);
        Assert.Equal(0x80, ReceiverStatus() & 0x80);
        Send(0xff); // No capacity remains; valid bytes must not disappear.
        for (var index = 0; index < capacity; index++) Assert.Equal(index + 1, Receive());
        Assert.Equal(capacity, Receive());
        Assert.Equal(0x40, SenderStatus() & 0x40);
        Assert.Equal(0, ReceiverStatus() & 0x80);
    }

    [Fact]
    public void ControlBitsSetAndClearIndependentlyAndUnusedStatusBitsReadHigh()
    {
        var tube = new TubeUla();
        for (var flag = 1; flag <= 0x20; flag <<= 1)
        {
            tube.HostWrite(0, (byte)(0x80 | flag));
            Assert.Equal(flag, tube.HostRead(0) & 0x3f);
            Assert.Equal(flag, tube.ParasiteRead(0) & 0x3f);
            tube.ParasiteWrite(0, 0xff);
            Assert.Equal(flag, tube.HostRead(0) & 0x3f);
            tube.HostWrite(0, (byte)flag);
            Assert.Equal(0, tube.HostRead(0) & 0x3f);
        }
        for (var register = 2; register < 8; register += 2)
        {
            Assert.Equal(0x3f, tube.HostRead(register) & 0x3f);
            Assert.Equal(0x3f, tube.ParasiteRead(register) & 0x3f);
        }
    }

    [Fact]
    public void SoftwareRegisterResetStaysAssertedUntilClearedAndPreservesOtherControlBits()
    {
        var tube = new TubeUla();
        tube.HostWrite(0, 0x8f);
        tube.HostWrite(1, 0x11);
        tube.ParasiteWrite(7, 0x22);
        Assert.True(tube.HostIrq);
        Assert.True(tube.ParasiteIrq);
        tube.HostWrite(0, 0xc0);
        Assert.Equal(0x0f, tube.HostRead(0) & 0x3f);
        Assert.False(tube.HostIrq);
        Assert.False(tube.ParasiteIrq);
        Assert.False(tube.ParasiteNmi);
        tube.HostWrite(1, 0x33);
        tube.ParasiteWrite(7, 0x44);
        tube.HostRead(5); // Initial R3 byte is not consumed while T is held.
        Assert.Equal(0xff, tube.HostRead(4));
        Assert.False(tube.DmaRequest);
        tube.HostWrite(0, 0x40);
        tube.HostWrite(1, 0x55);
        Assert.True(tube.ParasiteIrq);
        Assert.Equal(0x55, tube.ParasiteRead(1));
        Assert.False(tube.ParasiteIrq);
    }

    [Fact]
    public void PhysicalResetClearsControlsHoldsBothInterfacesAndPublishesOnlyEdges()
    {
        var tube = new TubeUla();
        var changes = new List<bool>();
        var resets = 0;
        tube.ParasiteResetChanged = changes.Add;
        tube.ResetParasite = () => resets++;
        tube.HostWrite(0, 0xa0);
        tube.HostWrite(0, 0xa0);
        Assert.True(tube.ResetHeld);
        tube.SetHostReset(true);
        tube.HostWrite(0, 0x8f);
        tube.HostWrite(1, 0x66);
        Assert.Equal(0, tube.HostRead(0) & 0x3f);
        tube.SetHostReset(true);
        tube.SetHostReset(false);
        Assert.False(tube.ResetHeld);
        Assert.Equal(new[] { true, false }, changes);
        Assert.Equal(1, resets);
        Assert.Equal(0, tube.ParasiteRead(0) & 0x80);
        Assert.Equal(0xff, tube.HostRead(4));
        Assert.Equal(0x3f, tube.ParasiteRead(4));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void RegisterThreeKeepsAvailabilityAndNotFullUntilTheWholeTransferCompletes(bool hostSends)
    {
        var tube = new TubeUla();
        tube.HostRead(5);
        tube.HostWrite(0, 0x90);
        void Send(byte value) { if (hostSends) tube.HostWrite(5, value); else tube.ParasiteWrite(5, value); }
        byte Receive() => hostSends ? tube.ParasiteRead(5) : tube.HostRead(5);
        byte SenderStatus() => hostSends ? tube.HostRead(4) : tube.ParasiteRead(4);
        byte ReceiverStatus() => hostSends ? tube.ParasiteRead(4) : tube.HostRead(4);
        if (hostSends) { tube.ParasiteWrite(5, 0x99); tube.ParasiteWrite(5, 0x98); }
        Send(0x11);
        Assert.Equal(0x40, SenderStatus() & 0x40);
        Assert.Equal(0, ReceiverStatus() & 0x80);
        Send(0x22);
        Assert.Equal(0, SenderStatus() & 0x40);
        Assert.Equal(0x80, ReceiverStatus() & 0x80);
        Assert.Equal(0x11, Receive());
        Assert.Equal(0, SenderStatus() & 0x40);
        Assert.Equal(0x80, ReceiverStatus() & 0x80);
        Assert.Equal(0x22, Receive());
        Assert.Equal(0x40, SenderStatus() & 0x40);
        Assert.Equal(0, ReceiverStatus() & 0x80);
    }

    [Fact]
    public void SimultaneousNmiCausesRemainAssertedUntilBothAreRemoved()
    {
        var tube = new TubeUla();
        var nmi = new List<bool>();
        var dma = new List<bool>();
        tube.ParasiteNmiChanged = nmi.Add;
        tube.DmaRequestChanged = dma.Add;
        tube.HostWrite(0, 0x88);
        Assert.False(tube.ParasiteNmi);
        tube.HostRead(5);
        Assert.True(tube.ParasiteNmi);
        tube.HostWrite(5, 0x55);
        tube.ParasiteRead(5); // Receive empty, but parasite output still empty.
        Assert.True(tube.ParasiteNmi);
        tube.ParasiteWrite(5, 0x66);
        Assert.False(tube.ParasiteNmi);
        Assert.Equal(new[] { true, false }, nmi);
        Assert.Equal(nmi, dma);
    }

    [Fact]
    public void CombinedParasiteIrqSourcesAreIndependentAndStatusReadsHaveNoEffects()
    {
        var tube = new TubeUla();
        var levels = new List<bool>();
        tube.ParasiteIrqChanged = levels.Add;
        tube.HostWrite(0, 0x86);
        tube.HostWrite(1, 0x11);
        tube.HostWrite(7, 0x22);
        for (var repeat = 0; repeat < 100; repeat++)
        {
            for (var register = 0; register < 8; register += 2)
            {
                tube.HostRead(register);
                tube.ParasiteRead(register);
            }
        }
        tube.ParasiteRead(1);
        Assert.True(tube.ParasiteIrq);
        tube.ParasiteRead(7);
        Assert.False(tube.ParasiteIrq);
        Assert.Equal(new[] { true, false }, levels);
    }
}
