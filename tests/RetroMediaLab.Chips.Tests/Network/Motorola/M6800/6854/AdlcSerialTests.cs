using RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Services;
using C = RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Constants.Mc6854Constants;
using B = RetroMediaLab.Chips.Tests.Network.Motorola.M6800.Chip6854.AdlcTestBench;

namespace RetroMediaLab.Chips.Tests.Network.Motorola.M6800.Chip6854;

public sealed class AdlcSerialTests
{
    [Fact]
    public void SerialTransmitMatchesPublishedCrcX25CheckVector()
    {
        var chip = B.Device();
        byte[] payload = "123456789"u8.ToArray();
        var (frame, wire) = B.Transmit(chip, payload);
        Assert.Equal(payload, frame);
        var decoded = B.Decode(wire);
        Assert.Equal(payload.Concat(new byte[] { 0x6e, 0x90 }), decoded);
        Assert.Equal(0, chip.Read(0) & C.TransmitUnderrun);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReceiverChecksFcsAndStripsStuffedZeroes(bool bad)
    {
        var chip = B.Device();
        byte[] payload = [0xff, 0xff, 0xff, 0x7e, 0, 0xf8, 0x1f];
        var actual = B.ReceiveBits(chip, B.Encode(B.Octets(payload), bad));
        Assert.Equal(payload, actual);
        var status = chip.Read(1);
        Assert.Equal(!bad, (status & C.FrameValid) != 0);
        Assert.Equal(bad, (status & C.FcsError) != 0);
        Assert.Equal(0, status & C.ReceiveOverrun);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void PartialFinalInformationWordIsRightJustified(int count)
    {
        var chip = B.Device();
        var bits = B.Octets(1, 2);
        bits.AddRange(Enumerable.Repeat(true, count));
        var actual = B.ReceiveBits(chip, B.Encode(bits));
        Assert.Equal(new byte[] { 1, 2, (byte)((1 << count) - 1) }, actual);
        Assert.NotEqual(0, chip.Read(1) & C.FrameValid);
        Assert.Equal(0, chip.Read(1) & C.FcsError);
    }

    [Fact]
    public void FramesShorterThanTwentyFiveBitsAreIgnored()
    {
        var chip = B.Device();
        Assert.Empty(B.ReceiveBits(chip, B.Encode(B.Octets(1))));
        Assert.Equal(0, chip.Read(1) & (C.FrameValid | C.FcsError | C.ReceiveAvailable));
    }

    [Fact]
    public void IncompleteExtendedHeaderIsReportedAsInvalid()
    {
        var chip = B.Device(control3: C.ExtendedAddress);
        Assert.Equal(new byte[] { 0x80, 0x80 }, B.ReceiveBits(chip, B.Encode(B.Octets(0x80, 0x80))));
        Assert.NotEqual(0, chip.Read(1) & C.FcsError);
        Assert.Equal(0, chip.Read(1) & C.FrameValid);
    }

    [Fact]
    public void ReceiveClockRequiresRisingEdgesAndFlagOutputLastsOneBit()
    {
        var chip = B.Device(control1: C.ReceiveInterrupt, control3: C.FlagDetectEnable);
        var flag = B.Octets(0x7e);
        foreach (var bit in flag.Take(7))
            B.ClockReceive(chip, bit);
        chip.SetReceiveData(false);
        chip.SetReceiveClock(true);
        chip.SetReceiveClock(true);
        Assert.False(chip.FlagDetect);
        B.ClockReceive(chip, false);
        Assert.True(chip.FlagDetect);
        Assert.NotEqual(0, chip.Read(0) & C.FlagDetected);
        Assert.True(chip.Irq);
        chip.Write(1, C.ClearReceive);
        Assert.Equal(0, chip.Read(0) & C.FlagDetected);
        B.ClockReceive(chip, true);
        Assert.False(chip.FlagDetect);
    }

    [Fact]
    public void FlagOutputDoesNotRequireStatusInterruptEnable()
    {
        var chip = B.Device(control1: C.ReceiveInterrupt);
        foreach (var bit in B.Octets(0x7e))
            B.ClockReceive(chip, bit);
        Assert.True(chip.FlagDetect);
        Assert.Equal(0, chip.Read(0) & C.FlagDetected);
        Assert.False(chip.Irq);
    }

    [Fact]
    public void TransmitClockRequiresFallingEdges()
    {
        var chip = B.Device();
        chip.Write(3, 1);
        chip.SetTransmitClock(false);
        Assert.True(chip.TransmitData);
        chip.SetTransmitClock(true);
        Assert.True(chip.TransmitData);
        chip.SetTransmitClock(false);
        Assert.False(chip.TransmitData); // Premier bit du flag.
        chip.SetTransmitClock(false);
        Assert.False(chip.TransmitData);
        Assert.True(B.ClockTransmit(chip));
    }

    [Fact]
    public void NrziTransmitIncludesTheDocumentedOneBitDelay()
    {
        var chip = B.Device(control4: C.Nrzi);
        var (_, wire) = B.Transmit(chip, [0xff, 0x22, 0x99], nrzi: true);
        var previous = true;
        var logical = wire.Select(level =>
        {
            var bit = level == previous;
            previous = level;
            return bit;
        }).Skip(1).ToArray();
        Assert.Equal(new byte[] { 0xff, 0x22, 0x99 }, B.Decode(logical).Take(3));
    }

    [Fact]
    public void ReceiverDecodesNrziLevels()
    {
        var chip = B.Device(control4: C.Nrzi);
        var level = true;
        var levels = B.Encode(B.Octets(0xff, 0, 0x55)).Select(bit =>
        {
            if (!bit)
                level = !level;
            return level;
        }).ToArray();
        Assert.Equal(new byte[] { 0xff, 0, 0x55 }, B.ReceiveBits(chip, levels));
        Assert.NotEqual(0, chip.Read(1) & C.FrameValid);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SoftwareAbortEmitsEightOrSixteenOnes(bool extended)
    {
        var chip = B.Device();
        chip.Write(2, 1);
        for (var bit = 0; bit < 11; bit++)
            B.ClockTransmit(chip);
        chip.Write(0, C.AddressControl);
        chip.Write(3, (byte)(0x1e | C.TransmitAbort | (extended ? C.ExtendedAbort : 0)));
        chip.Write(0, 0);
        var count = extended ? 16 : 8;
        for (var bit = 0; bit < count; bit++)
            Assert.True(B.ClockTransmit(chip));
        chip.Write(1, C.FrameCompleteMode);
        Assert.NotEqual(0, chip.Read(0) & C.TransmitAvailable);
        Assert.False(chip.Transmitting);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InternalTestModeConnectsActualTransmitBitsToReceiver(bool nrzi)
    {
        var chip = B.Device(control3: C.Test, control4: nrzi ? C.Nrzi : (byte)0);
        byte[] payload = [0x15, 0x99, 0xff, 0, 0x55];
        var written = 0;
        var read = new List<byte>();
        var external = 0;
        chip.FrameTransmitted = _ => external++;
        try
        {
            for (var clock = 0; clock < 250 && read.Count < payload.Length; clock++)
            {
                if (written < payload.Length && (chip.Read(0) & C.TransmitAvailable) != 0)
                    chip.Write(written == payload.Length - 1 ? 3 : 2, payload[written++]);
                chip.Tick();
                if ((chip.Read(1) & C.ReceiveAvailable) != 0)
                    read.Add(chip.Read(2));
            }
            Assert.Equal(payload, read);
            Assert.NotEqual(0, chip.Read(1) & C.FrameValid);
            Assert.Equal(0, external);
        }
        finally { chip.FrameTransmitted = null; }
    }

    [Fact]
    public void FlagFillIsNotMarkIdleAndZeroPrefixCanBeginMarkIdle()
    {
        var chip = B.Device();
        chip.Write(1, C.FlagIdle);
        Assert.Equal(B.Octets(0x7e, 0x7e), Enumerable.Range(0, 16).Select(_ => B.ClockTransmit(chip)));
        chip.Write(0, C.AddressControl);
        chip.Write(1, C.ZeroIdle);
        chip.Write(0, 0);
        chip.Write(1, 0);
        Assert.False(B.ClockTransmit(chip));
        for (var bit = 0; bit < 16; bit++)
            Assert.True(B.ClockTransmit(chip));
    }

    [Theory]
    [InlineData(false, 3)]
    [InlineData(true, 4)]
    public void AdjacentFramesShareClosingFlagUnlessDoubleFlagSelected(bool doubleFlag, int flags)
    {
        var chip = B.Device(control4: doubleFlag ? C.DoubleFlag : (byte)0);
        (int Register, byte Value)[] bytes = [(2, 1), (3, 2), (2, 3), (3, 4)];
        var written = 0;
        var frames = 0;
        var wire = new List<bool>();
        chip.FrameTransmitted = _ => frames++;
        try
        {
            for (var bit = 0; bit < 300 && frames < 2; bit++)
            {
                if (written < bytes.Length && (chip.Read(0) & C.TransmitAvailable) != 0)
                {
                    chip.Write(bytes[written].Register, bytes[written].Value);
                    written++;
                }
                wire.Add(B.ClockTransmit(chip));
            }
            Assert.Equal(2, frames);
            Assert.Equal(flags, Enumerable.Range(0, wire.Count - 7).Count(index => wire.Skip(index).Take(8).SequenceEqual(B.Octets(0x7e))));
        }
        finally { chip.FrameTransmitted = null; }
    }
    [Fact]
    public void ReceiveShiftRegisterIncludesTheFlagDetectorDelayOnlyOnce()
    {
        var chip = B.Device();
        var wire = B.Encode(B.Octets(1, 2, 3, 4, 5, 6));
        for (var bit = 0; bit < 39; bit++)
        {
            B.ClockReceive(chip, wire[bit]);
            Assert.Equal(0, chip.Read(1) & C.ReceiveAvailable);
        }
        B.ClockReceive(chip, wire[39]);
        Assert.NotEqual(0, chip.Read(1) & C.ReceiveAvailable);
        Assert.Equal(1, chip.Read(2));
    }

    [Theory]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void VariableInformationLengthIsPresentOnTheActualSerialWire(int width)
    {
        var chip = B.Device(width);
        var (_, wire) = B.Transmit(chip, [0xff, 0x7e, 0xff, 0]);
        var payload = B.Octets(0xff, 0x7e);
        payload.AddRange(Enumerable.Repeat(true, width));
        payload.AddRange(Enumerable.Repeat(false, width));
        Assert.Equal(B.Encode(payload), wire);
    }

    [Fact]
    public void EveryOctetPatternSurvivesSerialCrcStuffingAndFifoWrap()
    {
        for (var start = 0; start < 256; start += 16)
        {
            byte[] payload = Enumerable.Range(start, 16).Select(value => (byte)value).ToArray();
            var sender = B.Device();
            var (_, wire) = B.Transmit(sender, payload);
            Assert.Equal(B.Encode(B.Octets(payload)), wire);
            var receiver = B.Device();
            Assert.Equal(payload, B.ReceiveBits(receiver, wire));
            Assert.NotEqual(0, receiver.Read(1) & C.FrameValid);
            Assert.Equal(0, receiver.Read(1) & (C.FcsError | C.ReceiveOverrun));
        }
    }

    [Theory]
    [InlineData(9)]
    [InlineData(15)]
    public void FramesFromTwentyFiveToThirtyOneBitsHaveInvalidFrameStatus(int dataBits)
    {
        var chip = B.Device();
        var actual = B.ReceiveBits(chip, B.Encode(Enumerable.Repeat(false, dataBits)));
        Assert.Equal(2, actual.Length);
        Assert.NotEqual(0, chip.Read(1) & C.FcsError);
        Assert.Equal(0, chip.Read(1) & C.FrameValid);
    }

    [Fact]
    public void LateLastDataTerminatesWordAlreadyInShiftRegister()
    {
        var chip = B.Device();
        chip.Write(1, C.FrameCompleteMode);
        byte[]? frame = null;
        chip.FrameTransmitted = value => frame = value.ToArray();
        try
        {
            chip.Write(2, 0x55);
            for (var bit = 0; bit < 10; bit++)
                B.ClockTransmit(chip);
            chip.Write(1, C.FrameCompleteMode | C.LastData);
            Assert.Null(frame);
            for (var bit = 0; bit < 100 && frame is null; bit++)
                B.ClockTransmit(chip);
            Assert.Equal(new byte[] { 0x55 }, frame);
            Assert.NotEqual(0, chip.Read(0) & C.TransmitAvailable);
            chip.Write(1, C.FrameCompleteMode | C.ClearTransmit);
            Assert.Equal(0, chip.Read(0) & C.TransmitAvailable);
        }
        finally { chip.FrameTransmitted = null; }
    }

    [Fact]
    public void ConfiguredTickRatioPreservesBitClockRatherThanCompletingOneByteEarly()
    {
        var chip = new Mc6854(64);
        chip.SetLine(true);
        chip.Write(0, 0);
        chip.Write(3, 1);
        for (var tick = 0; tick < 7; tick++)
        {
            chip.Tick();
            Assert.True(chip.TransmitData);
        }
        chip.Tick();
        Assert.False(chip.TransmitData); // Premier bit de flag, pas un octet complet.
        for (var tick = 0; tick < 7; tick++)
        {
            chip.Tick();
            Assert.False(chip.TransmitData);
        }
        chip.Tick();
        Assert.True(chip.TransmitData); // Deuxième bit du flag.
    }

    [Fact]
    public void ChipResetDoesNotRestartTheExternalClockGeneratorPhase()
    {
        var chip = new Mc6854(64);
        chip.SetLine(true);
        for (var tick = 0; tick < 3; tick++) chip.Tick();
        chip.Reset();
        chip.Write(0, 0);
        chip.Write(3, 1);
        for (var tick = 0; tick < 4; tick++)
        {
            chip.Tick();
            Assert.True(chip.TransmitData);
        }
        chip.Tick();
        Assert.False(chip.TransmitData);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StableIdleAdvancePreservesFollowingFrameAndPendingStatus(bool nrzi)
    {
        var chip = B.Device(control4: nrzi ? C.Nrzi : (byte)0);
        for (var tick = 0; tick < 10003; tick++) chip.Tick();
        Assert.NotEqual(0, chip.Read(1) & C.Idle);
        chip.Write(1, C.ClearReceive);
        Assert.Equal(new byte[] { 1, 2, 0xff }, B.ReceivePacket(chip, [1, 2, 0xff]));
        Assert.NotEqual(0, chip.Read(1) & C.FrameValid);
        Assert.Equal(0, chip.Read(1) & (C.FcsError | C.ReceiveOverrun));
    }

}
