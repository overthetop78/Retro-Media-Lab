using RetroMediaLab.Chips.IO.Motorola.M6800.Chip6850.Services;
using static RetroMediaLab.Chips.Tests.IO.Motorola.M6800.Chip6850.Acia6850TestData;

namespace RetroMediaLab.Chips.Tests.IO.Motorola.M6800.Chip6850;

public sealed class Acia6850ModemTests
{
    [Fact]
    public void ReceiveInterruptMaskDoesNotConsumeTheBufferedDataOrItsCause()
    {
        var acia = new Acia6850(1, 0);
        acia.Write(false, 0x14);
        Receive(acia, 5, 0xa5);
        Assert.False(acia.Irq);
        Assert.Equal(1, acia.Read(false) & 1);
        acia.Write(false, 0x94);
        Assert.True(acia.Irq);
        acia.Write(false, 0x14);
        Assert.False(acia.Irq);
        Assert.Equal(0xa5, acia.Read(true));
        acia.Write(false, 0x94);
        Assert.False(acia.Irq);
    }
    [Fact]
    public void DcdRequiresAReceiveClockAndStatusThenDataToAcknowledgeTheLatchedEdge()
    {
        var acia = new Acia6850(2_000_000, 19200);
        acia.Write(false, 0x95);
        acia.SetDcd(true);
        Assert.False(acia.Irq);
        acia.ClockReceive(true);
        Assert.True(acia.Irq);
        acia.Read(true); // No preceding status read: must not acknowledge DCD.
        Assert.True(acia.Irq);
        acia.SetDcd(false);
        acia.ClockReceive(true);
        Assert.Equal(0x84, acia.Read(false) & 0x84);
        acia.Read(true);
        Assert.False(acia.Irq);
        Assert.Equal(0, acia.Read(false) & 4);
        acia.SetDcd(true);
        acia.ClockReceive(true);
        acia.Read(false);
        acia.Read(true);
        Assert.False(acia.Irq);
        Assert.Equal(4, acia.Read(false) & 4); // Pin remains high but no repeated edge IRQ.
        acia.ClockReceive(true);
        Assert.False(acia.Irq);
    }

    [Fact]
    public void StatusReadBeforeALaterCarrierLossCannotAcknowledgeThatNewLoss()
    {
        var acia = new Acia6850(1, 1);
        acia.Write(false, 0x94);
        acia.Read(false);
        acia.SetDcd(true);
        acia.ClockReceive(true);
        acia.Read(true);
        Assert.True(acia.Irq);
        acia.Read(false);
        acia.Read(true);
        Assert.False(acia.Irq);
    }

    [Fact]
    public void CarrierLossAbortsAnActiveReceiveAndPreventsReceptionUntilTheLineReturns()
    {
        var acia = new Acia6850(1, 1);
        acia.Write(false, 0x94);
        acia.BeginReceive();
        acia.ReceiveBit(true);
        acia.SetDcd(true);
        acia.ClockReceive(true);
        Assert.False(acia.Receiving);
        Receive(acia, 5, 0x55);
        Assert.Equal(0, acia.Read(false) & 1);
        acia.SetDcd(false);
        acia.ClockReceive(true);
        acia.Read(false);
        acia.Read(true);
        Receive(acia, 5, 0x55);
        Assert.Equal(0x55, acia.Read(true));
    }

    [Fact]
    public void CtsMasksTdreAndTransmitIrqWithoutLosingThePendingByte()
    {
        var acia = new Acia6850(1, 1);
        acia.Write(false, 0x34);
        Assert.True(acia.Irq);
        acia.SetCts(true);
        Assert.False(acia.Irq);
        Assert.Equal(8, acia.Read(false) & 10);
        acia.Write(true, 0x55);
        Clocks(acia, 100);
        Assert.True(acia.TransmitBit);
        acia.SetCts(false);
        Assert.False(acia.Irq); // TDR is still full.
        acia.ClockTransmit();
        Assert.False(acia.TransmitBit);
        Assert.True(acia.Irq);
        Assert.Equal(0x82, acia.Read(false) & 0x82);
    }

    [Fact]
    public void ResetMaintainsExternalPinsAndMasksAllTransfersUntilReprogrammed()
    {
        var acia = new Acia6850(1, 1);
        acia.SetCts(true);
        acia.SetDcd(true);
        acia.Write(false, 0xe3);
        Assert.True(acia.RtsHigh); // First master reset forces RTS high.
        Assert.True(acia.TransmitBit);
        Assert.False(acia.Irq);
        Assert.Equal(12, acia.Read(false));
        acia.Write(true, 0x55);
        Receive(acia, 0, 0x33);
        Clocks(acia, 100);
        Assert.False(acia.Receiving);
        Assert.True(acia.TransmitBit);
        Assert.Equal(12, acia.Read(false));
        acia.Write(false, 0x03);
        Assert.False(acia.RtsHigh); // Later resets take RTS from CR5/6.
        acia.Write(false, 0x14);
        Assert.Equal(12, acia.Read(false));
        acia.SetCts(false);
        acia.SetDcd(false);
        acia.ClockReceive(true);
        Assert.Equal(2, acia.Read(false));
        acia.ClockTransmit();
        Assert.True(acia.TransmitBit); // The data write during reset was ignored.
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    public void RtsFollowsAllFourTransmitControlModes(int mode, bool high)
    {
        var acia = new Acia6850(1, 1);
        acia.Write(false, (byte)(0x14 | mode << 5));
        Assert.Equal(high, acia.RtsHigh);
        Assert.Equal(mode == 1, acia.Irq);
        Assert.Equal(mode != 3, acia.TransmitBit);
    }
}
