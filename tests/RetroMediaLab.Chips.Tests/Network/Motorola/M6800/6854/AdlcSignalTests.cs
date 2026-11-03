using RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Services;
using C = RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Constants.Mc6854Constants;
using B = RetroMediaLab.Chips.Tests.Network.Motorola.M6800.Chip6854.AdlcTestBench;

namespace RetroMediaLab.Chips.Tests.Network.Motorola.M6800.Chip6854;

public sealed class AdlcSignalTests
{
    [Fact]
    public void CtsTransitionIsStoredUntilCorrectStatusIsReadAndCleared()
    {
        var chip = B.Device(control1: C.TransmitInterrupt | C.TransmitDma);
        Assert.False(chip.Irq);
        chip.SetClearToSend(false);
        chip.SetClearToSend(true);
        Assert.True(chip.Irq);
        chip.Read(1);
        chip.Write(1, C.ClearTransmit);
        Assert.True(chip.Irq);
        chip.Read(0);
        chip.Write(1, C.ClearTransmit);
        Assert.False(chip.Irq);
        Assert.NotEqual(0, chip.Read(0) & C.TransmitAvailable);
    }

    [Fact]
    public void DcdAndClockAreIndependentInputs()
    {
        var chip = B.Device(control1: C.ReceiveInterrupt);
        chip.SetClockPresent(false);
        Assert.Equal(0, chip.Read(1) & C.DataCarrierDetect);
        chip.SetCarrierPresent(false);
        chip.SetCarrierPresent(true);
        Assert.NotEqual(0, chip.Read(1) & C.DataCarrierDetect);
        Assert.True(chip.Irq);
        chip.Write(1, C.ClearReceive);
        Assert.False(chip.Irq);
        Assert.Equal(0, chip.Read(1) & C.DataCarrierDetect);
    }

    [Fact]
    public void DcdDiscardsUnfinishedFrameButPreservesPreviousFrameInFifo()
    {
        var chip = B.Device();
        foreach (var bit in B.Encode(B.Octets(1, 2)))
            B.ClockReceive(chip, bit);
        foreach (var bit in B.Encode(B.Octets(3, 4, 5, 6, 7, 8)).SkipLast(8))
            B.ClockReceive(chip, bit);
        chip.SetCarrierPresent(false);
        Assert.Equal(1, chip.Read(2));
        Assert.Equal(2, chip.Read(2));
        Assert.Equal(0, chip.Read(1) & C.ReceiveAvailable);
        Assert.NotEqual(0, chip.Read(1) & C.FrameValid);
    }

    [Fact]
    public void DiscontinueDropsOnlyTheCurrentFrame()
    {
        var chip = B.Device();
        var first = B.Encode(B.Octets(1, 2, 3, 4, 5, 6));
        var prefix = 50;
        foreach (var bit in first.Take(prefix))
            B.ClockReceive(chip, bit);
        chip.Write(0, C.Discontinue);
        foreach (var bit in first.Skip(prefix))
            B.ClockReceive(chip, bit);
        Assert.Equal(0, chip.Read(1) & (C.FrameValid | C.FcsError | C.ReceiveAvailable));
        Assert.Equal(new byte[] { 7, 8, 9 }, B.ReceiveBits(chip, B.Encode(B.Octets(7, 8, 9))));
        Assert.NotEqual(0, chip.Read(1) & C.FrameValid);
    }

    [Fact]
    public void LongAbortRaisesStatusWhileShortAbortIsNotAnEvent()
    {
        var shortFrame = B.Device(control1: C.ReceiveInterrupt);
        foreach (var bit in B.Octets(0x7e, 1).Concat(Enumerable.Repeat(true, 7)).Append(false))
            B.ClockReceive(shortFrame, bit);
        Assert.Equal(0, shortFrame.Read(1) & C.AbortReceived);
        Assert.False(shortFrame.Irq);
        var longFrame = B.Device(control1: C.ReceiveInterrupt);
        B.ReceiveBits(longFrame, B.Encode(B.Octets(1, 2, 3, 4, 5, 6)).SkipLast(8));
        foreach (var bit in Enumerable.Repeat(true, 7).Append(false))
            B.ClockReceive(longFrame, bit);
        Assert.NotEqual(0, longFrame.Read(1) & C.AbortReceived);
        Assert.True(longFrame.Irq);
        Assert.Equal(0, longFrame.Read(1) & C.ReceiveAvailable);
        longFrame.Write(1, C.ClearReceive);
        Assert.False(longFrame.Irq);
    }

    [Fact]
    public void OverrunIsDetectedWithoutAnArtificialFrameBuffer()
    {
        var chip = B.Device();
        foreach (var bit in B.Encode(B.Octets(1, 2, 3, 4, 5, 6, 7)))
            B.ClockReceive(chip, bit);
        Assert.NotEqual(0, chip.Read(1) & C.ReceiveOverrun);
        var data = new List<byte>();
        while ((chip.Read(1) & C.ReceiveAvailable) != 0)
            data.Add(chip.Read(2));
        Assert.Equal(3, data.Count);
        Assert.NotEqual(0, chip.Read(1) & C.FrameValid);
    }

    [Fact]
    public void TwoByteModeChangesBothFifoThresholds()
    {
        var chip = B.Device();
        chip.Write(1, C.TwoByte);
        chip.Write(2, 1);
        Assert.True(chip.TransmitServiceRequest);
        chip.Write(2, 2);
        Assert.False(chip.TransmitServiceRequest);
        foreach (var bit in B.Encode(B.Octets(0x11, 0x22, 0x33)))
            B.ClockReceive(chip, bit);
        Assert.True(chip.ReceiveServiceRequest);
        Assert.Equal(0x11, chip.Read(2));
        Assert.True(chip.ReceiveServiceRequest);
        Assert.Equal(0x22, chip.Read(2));
        Assert.False(chip.ReceiveServiceRequest);
        Assert.NotEqual(0, chip.Read(1) & C.FrameValid);
        chip.Write(1, 0);
        Assert.True(chip.ReceiveServiceRequest);
        Assert.Equal(0x33, chip.Read(2));
    }

    [Fact]
    public void DmaOutputsExistWithoutDmaModeAndDataIrqCanBeSuppressed()
    {
        var chip = B.Device(control1: C.TransmitInterrupt | C.TransmitDma | C.ReceiveInterrupt | C.ReceiveDma);
        Assert.True(chip.TransmitServiceRequest);
        Assert.False(chip.Irq);
        var wire = B.Encode(B.Octets(1, 2, 3, 4, 5, 6, 7));
        var index = 0;
        while (index < wire.Length && !chip.ReceiveServiceRequest)
            B.ClockReceive(chip, wire[index++]);
        Assert.True(chip.ReceiveServiceRequest);
        Assert.True(chip.Irq); // AP n’est pas une simple demande de données.
        Assert.Equal(1, chip.Read(2));
        while (index < wire.Length && !chip.ReceiveServiceRequest)
            B.ClockReceive(chip, wire[index++]);
        Assert.True(chip.ReceiveServiceRequest);
        Assert.False(chip.Irq);
        chip.Write(0, C.ReceiveInterrupt | C.TransmitDma);
        Assert.True(chip.Irq);
    }

    [Fact]
    public void PrioritizedDmaWaitsForAddressToBeConsumed()
    {
        var chip = B.Device();
        chip.Write(1, C.Prioritize);
        var wire = B.Encode(B.Octets(1, 2, 3, 4));
        var index = 0;
        while (index < wire.Length && (chip.Read(1) & C.AddressPresent) == 0)
            B.ClockReceive(chip, wire[index++]);
        Assert.False(chip.ReceiveServiceRequest);
        Assert.Equal(1, chip.Read(2));
        while (index < wire.Length && !chip.ReceiveServiceRequest)
            B.ClockReceive(chip, wire[index++]);
        Assert.True(chip.ReceiveServiceRequest);
    }

    [Fact]
    public void PriorityStatusReadsAreNonDestructive()
    {
        var chip = B.Device(control1: C.ReceiveInterrupt);
        B.ReceiveBits(chip, B.Encode(B.Octets(1, 2, 3), badFcs: true));
        chip.Write(1, C.Prioritize);
        var first = chip.Read(1);
        Assert.Equal(C.FcsError, first);
        for (var read = 0; read < 10; read++)
        {
            Assert.True(chip.Irq);
            Assert.Equal(first, chip.Read(1));
            chip.Read(0);
        }
    }

    [Fact]
    public void RtsReleaseWaitsForClosingFlagAndTwoIdleClocks()
    {
        var chip = B.Device();
        chip.Write(1, C.Rts);
        chip.Write(2, 1);
        chip.Write(3, 2);
        for (var bit = 0; bit < 9; bit++)
            B.ClockTransmit(chip);
        chip.Write(1, C.FrameCompleteMode);
        Assert.True(chip.Rts);
        for (var bit = 0; bit < 100 && chip.Transmitting; bit++)
            B.ClockTransmit(chip);
        Assert.False(chip.Transmitting);
        Assert.NotEqual(0, chip.Read(0) & C.TransmitAvailable);
        Assert.True(chip.Rts);
        B.ClockTransmit(chip);
        Assert.True(chip.Rts);
        B.ClockTransmit(chip);
        Assert.False(chip.Rts);
    }

    [Fact]
    public void HardwareResetIsHeldAndPreservesUnaffectedControlBits()
    {
        var chip = B.Device(5, control3: C.ExtendedControl);
        chip.Write(1, C.TwoByte | C.Rts);
        chip.SetReset(true);
        Assert.False(chip.Rts);
        chip.Write(0, C.ReceiveInterrupt | C.TransmitInterrupt);
        chip.Write(3, 0x55);
        Assert.False(chip.Transmitting);
        Assert.False(chip.Irq);
        chip.SetReset(false);
        Assert.Equal(0, chip.Read(0) & C.TransmitAvailable);
        chip.Write(0, 0);
        chip.Write(2, 1);
        chip.Write(2, 2);
        Assert.False(chip.TransmitServiceRequest); // TwoByte n’a pas été réinitialisé.
        chip.Write(0, C.TransmitReset);
        chip.Write(0, 0);
        Assert.Equal(new byte[] { 0xff, 0xfe, 0xfd, 0x1f }, B.Transmit(chip, [0xff, 0xfe, 0xfd, 0xff]).Frame);
    }

    [Fact]
    public void BusIsHighImpedanceOutsideSelectedReadAndConsumesOncePerEnable()
    {
        var chip = B.Device();
        foreach (var bit in B.Encode(B.Octets(1, 2)))
            B.ClockReceive(chip, bit);
        Assert.Null(chip.SetBus(true, false, true, 2));
        Assert.Null(chip.SetBus(false, true, true, 2));
        Assert.Equal((byte)1, chip.SetBus(true, true, true, 2));
        Assert.Equal((byte)1, chip.SetBus(true, true, true, 2));
        Assert.Null(chip.SetBus(false, true, true, 2));
        Assert.Equal((byte)2, chip.SetBus(true, true, true, 3));
        Assert.Null(chip.SetBus(false, true, false, 0, C.TransmitReset));
        Assert.Equal(0, chip.Read(1) & C.ReceiveAvailable);
    }

    [Fact]
    public void BusWritesOnlyAtTheSelectedFallingEnableEdge()
    {
        var chip = new Mc6854();
        Assert.Null(chip.SetBus(true, false, false, 0, 0));
        chip.SetBus(false, false, false, 0, 0);
        Assert.False(chip.Irq);
        chip.SetBus(true, true, false, 0, C.ReceiveInterrupt);
        Assert.False(chip.Irq);
        chip.SetBus(false, true, false, 0, C.ReceiveInterrupt);
        Assert.True(chip.Irq);
    }

    [Fact]
    public void LoopOnlineAndOfflineRequireTheirDocumentedRunsOfOnes()
    {
        var chip = B.Device(control3: C.Loop | C.LoopOnline);
        for (var bit = 0; bit < 6; bit++)
            B.ClockReceive(chip, true);
        Assert.False(chip.DataTerminalReady);
        Assert.Equal(0, chip.Read(0) & C.LoopStatus);
        B.ClockReceive(chip, true);
        Assert.True(chip.DataTerminalReady);
        Assert.NotEqual(0, chip.Read(0) & C.LoopStatus);
        chip.Write(0, C.AddressControl);
        chip.Write(1, C.Loop);
        chip.Write(0, 0);
        B.ClockReceive(chip, false);
        for (var bit = 0; bit < 7; bit++)
            B.ClockReceive(chip, true);
        Assert.True(chip.DataTerminalReady);
        B.ClockReceive(chip, true);
        Assert.False(chip.DataTerminalReady);
    }

    [Fact]
    public void LoopPollConvertsGoAheadAndReturnsToRepeatingAfterFrame()
    {
        var chip = B.Device(control3: C.Loop | C.LoopOnline);
        for (var bit = 0; bit < 7; bit++)
            B.ClockReceive(chip, true);
        chip.Write(2, 1);
        chip.Write(3, 2);
        chip.Write(0, C.AddressControl);
        chip.Write(1, C.Loop | C.LoopOnline | C.Test);
        chip.Write(0, 0);
        B.ClockReceive(chip, false);
        for (var bit = 0; bit < 7; bit++)
            B.ClockReceive(chip, true);
        Assert.Equal(0, chip.Read(0) & C.LoopStatus);
        Assert.False(B.ClockTransmit(chip));
        chip.Write(0, C.AddressControl);
        chip.Write(1, C.Loop | C.LoopOnline);
        chip.Write(0, 0);
        for (var bit = 0; bit < 100 && chip.Transmitting; bit++)
            B.ClockTransmit(chip);
        Assert.NotEqual(0, chip.Read(0) & C.LoopStatus);
        Assert.True(chip.DataTerminalReady);
    }
    [Fact]
    public void PhysicalDmaRequestsDropDuringTheCorrespondingBusTransfer()
    {
        var chip = B.Device();
        foreach (var bit in B.Encode(B.Octets(1, 2, 3)))
            B.ClockReceive(chip, bit);
        Assert.True(chip.ReceiveServiceRequest);
        Assert.Equal((byte)1, chip.SetBus(true, true, true, 2));
        Assert.False(chip.ReceiveServiceRequest);
        chip.SetBus(false, true, true, 2);
        Assert.True(chip.ReceiveServiceRequest);
        Assert.True(chip.TransmitServiceRequest);
        chip.SetBus(true, true, false, 2, 0x44);
        chip.SetBus(false, true, false, 2, 0x44);
        Assert.False(chip.TransmitServiceRequest);
        chip.SetBus(true, false, true, 0);
        Assert.True(chip.TransmitServiceRequest);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LoopRepeatingHasOneOrTwoBitDelaysAccordingToCoding(bool nrzi)
    {
        var chip = B.Device(control3: C.Loop | C.LoopOnline, control4: nrzi ? C.Nrzi : (byte)0);
        for (var bit = 0; bit < 7; bit++)
        {
            B.ClockReceive(chip, true);
            B.ClockTransmit(chip);
        }
        Assert.NotEqual(0, chip.Read(0) & C.LoopStatus);
        var previous = true;
        foreach (var input in new[] { false, true, false, false, true, true, false })
        {
            B.ClockReceive(chip, input);
            Assert.Equal(nrzi ? previous : input, B.ClockTransmit(chip));
            previous = input;
        }
    }

    [Fact]
    public void RepeatedFlagsDoNotRevokeAnAlreadyReadFlagLatch()
    {
        var chip = B.Device(control1: C.ReceiveInterrupt, control3: C.FlagDetectEnable);
        foreach (var bit in B.Octets(0x7e)) B.ClockReceive(chip, bit);
        Assert.NotEqual(0, chip.Read(0) & C.FlagDetected);
        foreach (var bit in B.Octets(0x7e)) B.ClockReceive(chip, bit);
        chip.Write(1, C.ClearReceive);
        Assert.Equal(0, chip.Read(0) & C.FlagDetected);
        Assert.False(chip.Irq);
    }

    [Fact]
    public void FifoTransferAndUnderrunOccurBeforeTheLastWordBit()
    {
        var chip = B.Device();
        chip.Write(2, 0);
        for (var bit = 0; bit < 15; bit++) B.ClockTransmit(chip); // Flag + sept bits du mot.
        Assert.Equal(0, chip.Read(0) & C.TransmitUnderrun);
        chip.SetTransmitClock(true); // Dernière moitié de l’avant-dernier bit.
        Assert.NotEqual(0, chip.Read(0) & C.TransmitUnderrun);
        chip.SetTransmitClock(false);
        Assert.True(chip.TransmitData); // Abort, pas le dernier zéro du mot incomplet.
        var queued = B.Device();
        queued.Write(1, C.TwoByte);
        queued.Write(2, 0);
        queued.Write(2, 0);
        queued.Write(3, 0);
        for (var bit = 0; bit < 15; bit++) B.ClockTransmit(queued);
        Assert.False(queued.TransmitServiceRequest);
        queued.SetTransmitClock(true);
        Assert.True(queued.TransmitServiceRequest);
        Assert.Equal(0, queued.Read(0) & C.TransmitUnderrun);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LoopWithoutPreloadedDataAbortsOrFillsFlagsAsConfigured(bool flagFill)
    {
        var chip = B.Device(control3: C.Loop | C.LoopOnline);
        if (flagFill) chip.Write(1, C.FlagIdle);
        for (var bit = 0; bit < 7; bit++) B.ClockReceive(chip, true);
        chip.Write(0, C.AddressControl);
        chip.Write(1, C.Loop | C.LoopOnline | C.Test);
        chip.Write(0, 0);
        B.ClockReceive(chip, false);
        for (var bit = 0; bit < 7; bit++) B.ClockReceive(chip, true);
        Assert.False(B.ClockTransmit(chip));
        for (var bit = 0; bit < 16; bit++) B.ClockTransmit(chip);
        Assert.Equal(!flagFill, (chip.Read(0) & C.TransmitUnderrun) != 0);
        if (flagFill)
        {
            Assert.Equal(0, chip.Read(0) & C.LoopStatus);
            chip.Write(0, C.AddressControl);
            chip.Write(1, C.Loop | C.LoopOnline);
            chip.Write(0, 0);
            for (var bit = 0; bit < 8; bit++) B.ClockTransmit(chip);
        }
        Assert.NotEqual(0, chip.Read(0) & C.LoopStatus);
    }

}
