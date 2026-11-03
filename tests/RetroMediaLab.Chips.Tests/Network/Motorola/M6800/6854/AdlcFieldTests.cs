using C = RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Constants.Mc6854Constants;
using B = RetroMediaLab.Chips.Tests.Network.Motorola.M6800.Chip6854.AdlcTestBench;

namespace RetroMediaLab.Chips.Tests.Network.Motorola.M6800.Chip6854;

public sealed class AdlcFieldTests
{
    [Theory]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void OnlyInformationWordsUseConfiguredWidth(int width)
    {
        var chip = B.Device(width);
        byte[] source = [0xff, 0xfe, 0xff];
        byte[] expected = [0xff, 0xfe, (byte)((1 << width) - 1)];
        Assert.Equal(expected, B.Transmit(chip, source).Frame);
        Assert.Equal(expected, B.ReceivePacket(chip, source));
        Assert.NotEqual(0, chip.Read(1) & C.FrameValid);
        Assert.Equal(0, chip.Read(1) & C.FcsError);
    }

    [Fact]
    public void AddressControlAndLogicalExtensionsRemainOctets()
    {
        var chip = B.Device(5, control3: C.ExtendedAddress | C.ExtendedControl | C.LogicalControl);
        byte[] source = [0x82, 0x84, 0x85, 0xfe, 0xfd, 0x80, 0x90, 0, 0xff, 0xfe];
        byte[] expected = [0x82, 0x84, 0x85, 0xfe, 0xfd, 0x80, 0x90, 0, 0x1f, 0x1e];
        Assert.Equal(expected, B.Transmit(chip, source).Frame);
        Assert.Equal(expected, B.ReceivePacket(chip, source));
        Assert.NotEqual(0, chip.Read(1) & C.FrameValid);
    }

    [Fact]
    public void NullAddressTerminatesAnExtendedAddress()
    {
        var chip = B.Device(5, control3: C.ExtendedAddress);
        byte[] source = [0, 0xff, 0xff];
        byte[] expected = [0, 0xff, 0x1f];
        Assert.Equal(expected, B.Transmit(chip, source).Frame);
        Assert.Equal(expected, B.ReceivePacket(chip, source));
        Assert.NotEqual(0, chip.Read(1) & C.FrameValid);
    }

    [Fact]
    public void CtsInhibitsRequestsButDoesNotStopLoadedTransmission()
    {
        var chip = B.Device();
        byte[]? result = null;
        chip.FrameTransmitted = frame => result = frame.ToArray();
        try
        {
            chip.Write(2, 0xa5);
            chip.Write(2, 0x80);
            chip.Write(3, 0x99);
            chip.SetClearToSend(false);
            Assert.Equal(0, chip.Read(0) & C.TransmitAvailable);
            for (var bit = 0; bit < 100 && result is null; bit++)
                B.ClockTransmit(chip);
            Assert.Equal(new byte[] { 0xa5, 0x80, 0x99 }, result);
            Assert.Equal(0, chip.Read(0) & C.TransmitUnderrun);
        }
        finally { chip.FrameTransmitted = null; }
    }

    [Fact]
    public void RingFifosPreserveDataAcrossHeadWrapping()
    {
        var chip = B.Device();
        byte[] source = Enumerable.Range(0, 35).Select(value => (byte)value).ToArray();
        Assert.Equal(source, B.Transmit(chip, source).Frame);
        Assert.Equal(source, B.ReceivePacket(chip, source));
        Assert.Equal(0, chip.Read(1) & C.ReceiveOverrun);
        Assert.NotEqual(0, chip.Read(1) & C.FrameValid);
    }

    [Fact]
    public void ANewFrameRestartsAtAddress()
    {
        var chip = B.Device(5);
        byte[] expected = [0xff, 0xfe, 0x1f];
        for (var frame = 0; frame < 2; frame++)
        {
            Assert.Equal(expected, B.Transmit(chip, [0xff, 0xfe, 0xff]).Frame);
            Assert.Equal(expected, B.ReceivePacket(chip, [0xff, 0xfe, 0xff]));
            chip.Read(1);
            chip.Write(1, C.ClearReceive);
        }
    }
}
