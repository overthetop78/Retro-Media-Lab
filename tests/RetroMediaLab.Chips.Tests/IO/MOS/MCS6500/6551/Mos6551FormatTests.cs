using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6551.Services;
using static RetroMediaLab.Chips.Tests.IO.MOS.MCS6500.Chip6551.Mos6551TestSignals;

namespace RetroMediaLab.Chips.Tests.IO.MOS.MCS6500.Chip6551;

public sealed class Mos6551FormatTests
{
    public static IEnumerable<object[]> Formats()
    {
        foreach (var bits in new[] { 5, 6, 7, 8 })
        foreach (var parity in new[] { 0, 1, 3, 5, 7 })
        foreach (var extraStop in new[] { false, true })
            yield return [bits, parity, extraStop];
    }

    [Theory]
    [MemberData(nameof(Formats))]
    public void EveryByteUsesTheSelectedDataParityAndStopTiming(int bits, int parity, bool extraStop)
    {
        var control = (byte)(((8 - bits) << 5) | (extraStop ? 128 : 0));
        var stop = !extraStop || bits == 8 && parity != 0 ? 16 : bits == 5 && parity == 0 ? 24 : 32;
        for (var data = 0; data <= byte.MaxValue; data++)
        {
            var sender = Device(control, (byte)((parity << 5) | 9));
            var receiver = Device(control, (byte)((parity << 5) | 9));
            var expected = Frame(bits, parity, (byte)data);
            sender.Write(0, (byte)data);
            Tx(sender);
            Assert.False(sender.Txd);
            Rx(receiver, false, 8); // Start qualified at its midpoint.
            for (var bit = 1; bit < expected.Length; bit++)
            {
                Tx(sender, 16);
                Assert.Equal(expected[bit], sender.Txd);
                if (bit < expected.Length - 1) Rx(receiver, expected[bit]);
            }
            var receiveStop = stop == 24 ? 28 : stop + 1;
            Rx(receiver, true, receiveStop - 1);
            Assert.Equal(0, receiver.Read(1) & 8);
            Rx(receiver, true, 1);
            Assert.Equal(0x88, receiver.Read(1) & 0x8f);
            Assert.Equal(data & ((1 << bits) - 1), receiver.Read(0));
            sender.Write(0, 0);
            Tx(sender, stop - 1);
            Assert.True(sender.Txd);
            Tx(sender);
            Assert.False(sender.Txd);
        }
    }

    [Theory]
    [InlineData(0x20, true)]
    [InlineData(0x60, false)]
    [InlineData(0xa0, true)]
    [InlineData(0xe0, false)]
    public void OddEvenAndFixedParityHaveIndependentReceiveChecking(int command, bool valid)
    {
        foreach (var bit in new[] { false, true })
        {
            var receiver = Device(0, (byte)(command | 9));
            Send(receiver, 0x55, bit);
            var shouldFail = command < 0xa0 && bit != valid;
            Assert.Equal(shouldFail ? 1 : 0, receiver.Read(1) & 1);
        }
    }

    [Fact]
    public void EveryFormatTransitionAfterThreeDataBitsAppliesToTheActiveReceiverAndTransmitter()
    {
        var formats = Formats().ToArray();
        foreach (var before in formats)
        foreach (var after in formats)
        {
            static byte Control(object[] format) => (byte)(((8 - (int)format[0]) << 5) | ((bool)format[2] ? 128 : 0));
            static byte Command(object[] format) => (byte)(((int)format[1] << 5) | 9);
            var sender = Device(Control(before), Command(before));
            var receiver = Device(Control(before), Command(before));
            sender.Write(0, 0xa5);
            Tx(sender);
            Rx(receiver, false, 8);
            var bits = (int)after[0];
            var parity = (int)after[1];
            var frame = Frame(bits, parity, 0xa5);
            for (var bit = 1; bit < frame.Length - 1; bit++)
            {
                if (bit == 4)
                {
                    sender.Write(3, Control(after)); sender.Write(2, Command(after));
                    receiver.Write(3, Control(after)); receiver.Write(2, Command(after));
                }
                Tx(sender, 16);
                Assert.Equal(frame[bit], sender.Txd);
                Rx(receiver, frame[bit]);
            }
            Tx(sender, 16);
            Assert.True(sender.Txd);
            var stop = !(bool)after[2] || bits == 8 && parity != 0 ? 17 : bits == 5 && parity == 0 ? 28 : 33;
            Rx(receiver, true, stop);
            Assert.Equal(8, receiver.Read(1) & 0x0f);
            Assert.Equal(0xa5 & ((1 << bits) - 1), receiver.Read(0));
        }
    }

    [Fact]
    public void FormatChangesBeforeTheLastDataBitAffectTheActiveTransmitter()
    {
        var chip = Device();
        chip.Write(0, 0x80);
        Tx(chip);
        Tx(chip, 7 * 16);
        chip.Write(3, 0x20); // Seven data bits.
        chip.Write(2, 0x69); // Even parity: the next bit is zero instead of data bit 7.
        Tx(chip, 16);
        Assert.False(chip.Txd);
        Tx(chip, 16);
        Assert.True(chip.Txd);
    }
}
