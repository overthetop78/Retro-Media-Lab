using RetroMediaLab.Chips.IO.Motorola.M6800.Chip6850.Services;
using static RetroMediaLab.Chips.Tests.IO.Motorola.M6800.Chip6850.Acia6850TestData;

namespace RetroMediaLab.Chips.Tests.IO.Motorola.M6800.Chip6850;

public sealed class Acia6850ReceiveTests
{
    [Theory]
    [InlineData(1, 16)]
    [InlineData(2, 64)]
    public void EveryShortFalseStartIsRejectedBeforeHalfABit(int selection, int divider)
    {
        for (var length = 1; length < divider / 2; length++)
        {
            var acia = new Acia6850(1, 0);
            acia.Write(false, (byte)(0x94 | selection));
            for (var tick = 0; tick < length; tick++) acia.ClockReceive(false);
            Assert.False(acia.Receiving);
            acia.ClockReceive(true);
            Assert.False(acia.Receiving);
            ReceiveClocked(acia, 5, 0xa5, divider);
            Assert.Equal(0xa5, acia.Read(true));
        }
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 16)]
    [InlineData(2, 64)]
    public void AllBytesCanArriveBackToBackWithoutAnExtraIdleBit(int selection, int divider)
    {
        var acia = new Acia6850(1, 0);
        acia.Write(false, (byte)(0x94 | selection));
        for (var value = 0; value <= byte.MaxValue; value++)
        {
            ReceiveClocked(acia, 5, (byte)value, divider);
            Assert.True(acia.Irq);
            Assert.Equal(value, acia.Read(true));
            Assert.False(acia.Irq);
        }
    }

    [Fact]
    public void AnOverrunPreservesTheUnreadCharactersErrorsAndNeedsASecondReadToClearTheLoss()
    {
        var acia = new Acia6850(1, 0);
        acia.Write(false, 0x98); // 8E1 receive IRQ.
        var damaged = Frame(6, 0x55);
        damaged[^2] = !damaged[^2];
        damaged[^1] = false;
        acia.BeginReceive();
        foreach (var bit in damaged.Skip(1)) acia.ReceiveBit(bit);
        Assert.Equal(0x51, acia.Read(false) & 0x71);
        Receive(acia, 6, 0xaa);
        Receive(acia, 6, 0xbb);
        Assert.Equal(0x51, acia.Read(false) & 0x71);
        Assert.Equal(0x55, acia.Read(true));
        Assert.True(acia.Irq);
        Assert.Equal(0x71, acia.Read(false) & 0x71);
        Assert.Equal(0x55, acia.Read(true));
        Assert.False(acia.Irq);
        Receive(acia, 6, 0x3c);
        Assert.Equal(1, acia.Read(false) & 0x71);
        Assert.Equal(0x3c, acia.Read(true));
    }

    [Fact]
    public void AReadDuringThePayloadPreventsOverrunButAReadAfterItsLastBitExposesIt()
    {
        foreach (var afterLastBit in new[] { false, true })
        {
            var acia = new Acia6850(1, 0);
            acia.Write(false, 0x94);
            Receive(acia, 5, 0x11);
            acia.BeginReceive();
            var frame = Frame(5, 0x22);
            var countBeforeRead = afterLastBit ? 8 : 7;
            for (var index = 1; index <= countBeforeRead; index++) acia.ReceiveBit(frame[index]);
            Assert.Equal(0x11, acia.Read(true));
            for (var index = countBeforeRead + 1; index < frame.Length; index++) acia.ReceiveBit(frame[index]);
            Assert.Equal(afterLastBit ? 0x21 : 1, acia.Read(false) & 0x21);
            Assert.Equal(afterLastBit ? 0x11 : 0x22, acia.Read(true));
            Assert.False(acia.Irq);
        }
    }

    [Fact]
    public void AFramingErrorDoesNotTreatAContinuousSpaceAsEndlessNewCharacters()
    {
        var acia = new Acia6850(1, 0);
        acia.Write(false, 0x94);
        for (var tick = 0; tick < 10; tick++) acia.ClockReceive(false);
        Assert.Equal(0x11, acia.Read(false) & 0x11);
        Assert.Equal(0, acia.Read(true));
        for (var tick = 0; tick < 1000; tick++) acia.ClockReceive(false);
        Assert.False(acia.Irq);
        Assert.Equal(0, acia.Read(false) & 0x21);
        acia.ClockReceive(true);
        ReceiveClocked(acia, 5, 0x5a, 1);
        Assert.Equal(1, acia.Read(false) & 0x71);
        Assert.Equal(0x5a, acia.Read(true));
    }

    [Fact]
    public void CancellingAReceiveDoesNotDeleteTheAlreadyBufferedCharacter()
    {
        var acia = new Acia6850(1, 0);
        acia.Write(false, 0x94);
        Receive(acia, 5, 0x42);
        acia.BeginReceive();
        acia.ReceiveBit(true);
        acia.CancelReceive();
        Assert.False(acia.Receiving);
        Assert.True(acia.Irq);
        Assert.Equal(0x42, acia.Read(true));
    }
}
