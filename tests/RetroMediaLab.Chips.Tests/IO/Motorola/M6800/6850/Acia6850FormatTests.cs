using RetroMediaLab.Chips.IO.Motorola.M6800.Chip6850.Services;
using static RetroMediaLab.Chips.Tests.IO.Motorola.M6800.Chip6850.Acia6850TestData;

namespace RetroMediaLab.Chips.Tests.IO.Motorola.M6800.Chip6850;

public sealed class Acia6850FormatTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 16)]
    [InlineData(2, 64)]
    public void IdleTransmitterPreservesTheProgrammedDividerPhase(int selector, int divider)
    {
        const byte eightNoParityOneStop = 0x14;
        const byte transmitEmpty = 0x02;
        var acia = new Acia6850(2_000_000, 0);
        acia.Write(false, (byte)(eightNoParityOneStop | selector));
        acia.ClockTransmit();
        var elapsed = divider / 2;
        for (var tick = 0; tick < elapsed; tick++) acia.ClockTransmit();
        acia.Write(true, 0x5a);
        for (var tick = 1; tick < divider - elapsed; tick++)
        {
            acia.ClockTransmit();
            Assert.True(acia.TransmitBit);
            Assert.Equal(0, acia.Read(false) & transmitEmpty);
        }
        acia.ClockTransmit();
        Assert.False(acia.TransmitBit);
        Assert.Equal(transmitEmpty, acia.Read(false) & transmitEmpty);
    }

    public static IEnumerable<object[]> Formats()
    {
        for (var format = 0; format < 8; format++)
        for (var selection = 0; selection < 3; selection++)
            yield return [format, selection, selection == 0 ? 1 : selection == 1 ? 16 : 64];
    }

    [Theory]
    [MemberData(nameof(Formats))]
    public void EveryByteHasTheCorrectTransmitFrameAndReceiveValue(int format, int selection, int divider)
    {
        for (var value = 0; value <= byte.MaxValue; value++)
        {
            var transmitter = new Acia6850(2_000_000, 19200);
            var receiver = new Acia6850(2_000_000, 19200);
            var control = (byte)(format << 2 | selection);
            transmitter.Write(false, control);
            receiver.Write(false, control);
            transmitter.Write(true, (byte)value);
            Assert.Equal(0, transmitter.Read(false) & 2);
            var frame = Frame(format, (byte)value);
            transmitter.ClockTransmit();
            Assert.Equal(2, transmitter.Read(false) & 2);
            for (var bit = 0; bit < frame.Length; bit++)
            {
                Assert.Equal(frame[bit], transmitter.TransmitBit);
                for (var clock = 1; clock < divider; clock++)
                {
                    transmitter.ClockTransmit();
                    Assert.Equal(frame[bit], transmitter.TransmitBit);
                }
                transmitter.ClockTransmit();
            }
            Assert.True(transmitter.TransmitBit);
            ReceiveClocked(receiver, format, (byte)value, divider);
            Assert.Equal(1, receiver.Read(false) & 0x71);
            Assert.Equal((byte)(value & (format < 4 ? 0x7f : 0xff)), receiver.Read(true));
            Assert.Equal(0, receiver.Read(false) & 1);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(6)]
    [InlineData(7)]
    public void BadParityBelongsToItsCharacterAndIsReplacedByTheNextCharacter(int format)
    {
        var acia = new Acia6850(2_000_000, 19200);
        acia.Write(false, (byte)(0x80 | format << 2));
        var frame = Frame(format, 0xa5);
        var parityIndex = 1 + (format < 4 ? 7 : 8);
        frame[parityIndex] = !frame[parityIndex];
        acia.BeginReceive();
        foreach (var bit in frame.Skip(1)) acia.ReceiveBit(bit);
        Assert.Equal(0xc1, acia.Read(false) & 0xc1);
        var received = acia.Read(true);
        Assert.Equal((byte)(0xa5 & (format < 4 ? 0x7f : 0xff)), received);
        Assert.False(acia.Irq);
        Assert.Equal(0x40, acia.Read(false) & 0x40); // Stored character retains its associated error.
        Receive(acia, format, 0x36);
        Assert.Equal(1, acia.Read(false) & 0x71);
        Assert.Equal(0x36, acia.Read(true));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(4)]
    public void OnlyTheFirstStopBitDeterminesFramingError(int format)
    {
        foreach (var firstStop in new[] { false, true })
        foreach (var secondStop in new[] { false, true })
        {
            var acia = new Acia6850(2_000_000, 19200);
            acia.Write(false, (byte)(format << 2));
            var frame = Frame(format, 0x35);
            frame[^2] = firstStop;
            frame[^1] = secondStop;
            acia.BeginReceive();
            foreach (var bit in frame.Skip(1)) acia.ReceiveBit(bit);
            Assert.Equal(firstStop ? 1 : 0x11, acia.Read(false) & 0x11);
            Assert.Equal(0x35, acia.Read(true));
        }
    }

    [Fact]
    public void TransmitFormatChangesAreEffectiveBeforeTheNextBitNotTheNextFrame()
    {
        var acia = new Acia6850(1, 1);
        acia.Write(false, 0x14); // 8N1, /1.
        acia.Write(true, 0x80);
        acia.ClockTransmit(); // start.
        Clocks(acia, 7);
        acia.Write(false, 0x08); // 7E1: the next bit is parity, not old data bit 7.
        acia.ClockTransmit();
        Assert.False(acia.TransmitBit);
        acia.ClockTransmit();
        Assert.True(acia.TransmitBit);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(4)]
    public void TwoStopReceivePublishesAfterTheLastStopOrImmediatelyOnAFramingError(int format)
    {
        foreach (var badStop in new[] { false, true })
        {
            var acia = new Acia6850(1, 0);
            acia.Write(false, (byte)(format << 2));
            var frame = Frame(format, 0x35);
            acia.BeginReceive();
            foreach (var bit in frame.Skip(1).SkipLast(2)) acia.ReceiveBit(bit);
            Assert.Equal(0, acia.Read(false) & 1);
            acia.ReceiveBit(!badStop);
            Assert.Equal(badStop ? 0x11 : 0, acia.Read(false) & 0x11);
            if (!badStop) acia.ReceiveBit(true);
            Assert.Equal(badStop ? 0x11 : 1, acia.Read(false) & 0x11);
            Assert.Equal(0x35, acia.Read(true));
        }
    }

    [Fact]
    public void EveryFormatTransitionDuringTheDataBitsAffectsBothDirectionsImmediately()
    {
        for (var before = 0; before < 8; before++)
        for (var after = 0; after < 8; after++)
        {
            var tx = new Acia6850(1, 0);
            var rx = new Acia6850(1, 0);
            tx.Write(false, (byte)(before << 2));
            rx.Write(false, (byte)(before << 2));
            tx.Write(true, 0xa5);
            tx.ClockTransmit();
            rx.BeginReceive();
            var frame = Frame(after, 0xa5);
            for (var bit = 1; bit < frame.Length; bit++)
            {
                if (bit == 4)
                {
                    tx.Write(false, (byte)(after << 2));
                    rx.Write(false, (byte)(after << 2));
                }
                tx.ClockTransmit();
                Assert.Equal(frame[bit], tx.TransmitBit);
                rx.ReceiveBit(frame[bit]);
            }
            Assert.Equal(1, rx.Read(false) & 0x71);
            Assert.Equal(after < 4 ? 0x25 : 0xa5, rx.Read(true));
        }
    }
}
