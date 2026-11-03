using RetroMediaLab.Chips.IO.Motorola.M6800.Chip6850.Services;
using static RetroMediaLab.Chips.Tests.IO.Motorola.M6800.Chip6850.Acia6850TestData;

namespace RetroMediaLab.Chips.Tests.IO.Motorola.M6800.Chip6850;

public sealed class Acia6850TransmitTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 16)]
    [InlineData(2, 64)]
    public void DoubleBufferingStartsTheSecondFrameAtTheEndOfTheFirstStop(int selection, int divider)
    {
        var acia = new Acia6850(1, 0);
        acia.Write(false, (byte)(0x34 | selection));
        acia.Write(true, 0x55);
        acia.ClockTransmit();
        Assert.True(acia.Irq);
        Assert.False(acia.TransmitBit);
        acia.Write(true, 0xaa);
        Assert.False(acia.Irq);
        var frame = Frame(5, 0x55);
        for (var bit = 1; bit < frame.Length; bit++)
        {
            Clocks(acia, divider);
            Assert.Equal(frame[bit], acia.TransmitBit);
            Assert.False(acia.Irq);
        }
        Clocks(acia, divider);
        Assert.False(acia.TransmitBit); // Immediate second start, no extra mark bit.
        Assert.True(acia.Irq);
        foreach (var bit in Frame(5, 0xaa).Skip(1))
        {
            Clocks(acia, divider);
            Assert.Equal(bit, acia.TransmitBit);
        }
    }

    [Fact]
    public void CtsRaisedDuringAFrameDoesNotTruncateItButBlocksTheNextStart()
    {
        var acia = new Acia6850(1, 0);
        acia.Write(false, 0x34);
        acia.Write(true, 0xa5);
        acia.ClockTransmit();
        acia.Write(true, 0x5a);
        acia.SetCts(true);
        foreach (var bit in Frame(5, 0xa5).Skip(1))
        {
            acia.ClockTransmit();
            Assert.Equal(bit, acia.TransmitBit);
            Assert.False(acia.Irq);
        }
        Clocks(acia, 50);
        Assert.True(acia.TransmitBit);
        acia.SetCts(false);
        acia.ClockTransmit();
        Assert.False(acia.TransmitBit);
        Assert.True(acia.Irq);
    }

    [Fact]
    public void BreakHoldsSpaceAndKeepsTheNextBytePendingUntilReleased()
    {
        var acia = new Acia6850(1, 0);
        acia.Write(false, 0x74);
        acia.Write(true, 0x55);
        for (var tick = 0; tick < 100; tick++)
        {
            acia.ClockTransmit();
            Assert.False(acia.TransmitBit);
            Assert.Equal(0, acia.Read(false) & 2);
            Assert.False(acia.Irq);
        }
        acia.Write(false, 0x34);
        Assert.True(acia.TransmitBit);
        acia.ClockTransmit();
        Assert.False(acia.TransmitBit);
        Assert.True(acia.Irq);
    }

    [Fact]
    public void RtsReleasedDoesNotItselfInhibitTheTransmitter()
    {
        var acia = new Acia6850(1, 0);
        acia.Write(false, 0x54);
        Assert.True(acia.RtsHigh);
        acia.Write(true, 0x55);
        acia.ClockTransmit();
        Assert.False(acia.TransmitBit);
        Assert.Equal(2, acia.Read(false) & 2);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 16)]
    [InlineData(2, 64)]
    public void TickKeepsTheFractionalExternalClockWithoutRoundingTheBitRate(int selection, int divider)
    {
        const int cpuClock = 2_000_000;
        const int serialClock = 19200;
        var acia = new Acia6850(cpuClock, serialClock);
        acia.Write(false, (byte)(0x14 | selection));
        acia.Write(true, 0x55);
        var frame = Frame(5, 0x55);
        var previousTime = 0L;
        var previousLevel = true;
        for (var bit = 0; bit < frame.Length; bit++)
        {
            var eventTime = (long)Math.Ceiling((1L + bit * divider) * (double)cpuClock / serialClock);
            for (var tick = previousTime + 1; tick < eventTime; tick++)
            {
                acia.Tick();
                Assert.Equal(previousLevel, acia.TransmitBit);
            }
            acia.Tick();
            Assert.Equal(frame[bit], acia.TransmitBit);
            previousTime = eventTime;
            previousLevel = frame[bit];
        }
    }

    [Fact]
    public void BaudOverrideAndReceiveClockAreIndependentOfTransmitClockProgress()
    {
        var acia = new Acia6850(2_000_000, 19200);
        acia.BaudRateOverride = 300;
        acia.Write(false, 0x95);
        Assert.Equal(300, acia.BaudRate);
        acia.Write(true, 0x55);
        ReceiveClocked(acia, 5, 0xaa, 16);
        Assert.Equal(0xaa, acia.Read(true));
        Assert.Equal(0, acia.Read(false) & 2); // RxC did not consume pending TX data.
        acia.ClockTransmit();
        Assert.False(acia.TransmitBit);
        acia.BaudRateOverride = 0;
        Assert.Equal(1200, acia.BaudRate);
        acia.Write(false, 0x96);
        Assert.Equal(300, acia.BaudRate);
        acia.Write(false, 3);
        Assert.Equal(0, acia.BaudRate);
    }
}
