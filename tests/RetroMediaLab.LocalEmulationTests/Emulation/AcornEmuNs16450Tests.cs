using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Ns16450.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuNs16450Tests
{
    [Fact]
    public void RegisterAliasesMasksAndReadOnlyPortsMatchThe16450WithoutAFifo()
    {
        var device = Device();
        Assert.Equal(0x60, device.Read(5));
        Assert.Equal(1, device.Read(2));
        device.Write(3, 0x80);
        device.Write(0, 0x34);
        device.Write(1, 0x12);
        Assert.Equal(0x34, device.Read(0));
        Assert.Equal(0x12, device.Read(1));
        device.Write(3, 3);
        device.Write(1, 0xff);
        Assert.Equal(15, device.Read(1));
        device.Write(7, 0xc9);
        Assert.Equal(0xc9, device.Read(7));
        device.Write(2, 0xc7); // No 16550 FIFO control register at this address.
        Assert.Equal(2, device.Read(2));
        Assert.Equal(1, device.Read(2));
        device.Write(5, 0xff);
        device.Write(6, 0xff);
        Assert.Equal(0x60, device.Read(5));
        Assert.Equal(0, device.Read(6));
        device.Write(4, 0xff);
        Assert.Equal(0x1f, device.Read(4));
    }

    [Theory]
    [InlineData(0x00, 0x15, 5)]
    [InlineData(0x01, 0x35, 6)]
    [InlineData(0x02, 0x65, 7)]
    [InlineData(0x03, 0xa5, 8)]
    [InlineData(0x04, 0x15, 5)]
    [InlineData(0x07, 0xa5, 8)]
    [InlineData(0x0b, 0xa5, 8)]
    [InlineData(0x1b, 0xa5, 8)]
    [InlineData(0x2b, 0xa5, 8)]
    [InlineData(0x3b, 0xa5, 8)]
    [InlineData(0x1f, 0xa5, 8)]
    public void ElectricalPinsTransferAllWordLengthsParityModesAndStopFormats(int control, int value, int bits)
    {
        var sender = Device((byte)control);
        var receiver = Device((byte)control);
        sender.TransmitChanged = receiver.SetRxd;
        sender.Write(0, (byte)value);
        ClockPair(sender, receiver, 220);
        Assert.Equal(0x61, receiver.Read(5));
        Assert.Equal(value & ((1 << bits) - 1), receiver.Read(0));
        Assert.Equal(0x60, receiver.Read(5));
        Assert.True(sender.Txd);
    }

    [Fact]
    public void HoldingEmptyInterruptAndTransmitterEmptyAreDifferentEvents()
    {
        var device = Device();
        device.Write(1, 2);
        Assert.True(device.Irq); // OUT2 does not gate the interrupt pin inside the UART.
        Assert.Equal(2, device.Read(2));
        Assert.False(device.Irq);
        device.Write(0, 0x53);
        Assert.Equal(0, device.Read(5));
        device.Advance(1);
        Assert.Equal(0x20, device.Read(5));
        Assert.True(device.Irq);
        Assert.Equal(2, device.Read(2));
        Assert.False(device.Irq);
        Assert.Equal(1, device.Read(2));
        device.Advance(159);
        Assert.Equal(0x20, device.Read(5));
        device.Advance(1);
        Assert.Equal(0x60, device.Read(5));
        Assert.False(device.Irq);
    }

    [Fact]
    public void HoldingRegisterQueuesASecondCharacterWhileTheShiftRegisterIsBusy()
    {
        var sender = Device();
        var receiver = Device();
        sender.TransmitChanged = receiver.SetRxd;
        sender.Write(0, 0x35);
        ClockPair(sender, receiver, 1);
        sender.Write(0, 0xb2);
        Assert.Equal(0, sender.Read(5));
        ClockPair(sender, receiver, 151);
        Assert.Equal(0x35, receiver.Read(0));
        ClockPair(sender, receiver, 160);
        Assert.Equal(0xb2, receiver.Read(0));
        ClockPair(sender, receiver, 9);
        Assert.Equal(0x60, sender.Read(5));
    }

    [Fact]
    public void ReceiverChecksOnlyTheFirstStopEvenWhenConfiguredForTwoStops()
    {
        var sender = Device(3);
        var receiver = Device(7);
        sender.TransmitChanged = receiver.SetRxd;
        sender.Write(0, 0x81);
        ClockPair(sender, receiver, 1);
        sender.Write(0, 0x24);
        ClockPair(sender, receiver, 151);
        Assert.Equal(0x61, receiver.Read(5));
        Assert.Equal(0x81, receiver.Read(0));
        ClockPair(sender, receiver, 160);
        Assert.Equal(0x61, receiver.Read(5));
        Assert.Equal(0x24, receiver.Read(0));
    }

    [Theory]
    [InlineData(4, 121)]
    [InlineData(7, 177)]
    [InlineData(0x1f, 193)]
    public void TransmitterWaitsForTheEntireConfiguredStopInterval(int control, int clocks)
    {
        var device = Device((byte)control);
        device.Write(0, 0x55);
        device.Advance(clocks - 1);
        Assert.Equal(0x20, device.Read(5));
        device.Advance(1);
        Assert.Equal(0x60, device.Read(5));
    }

    [Fact]
    public void InterruptPriorityAndAcknowledgmentsKeepOtherPendingSourcesIntact()
    {
        var sender = Device(0x0b); // Odd parity.
        var receiver = Device(0x1b); // Even parity.
        sender.TransmitChanged = receiver.SetRxd;
        receiver.Write(1, 15);
        receiver.SetCts(false);
        sender.Write(0, 0x35);
        ClockPair(sender, receiver, 200);
        Assert.Equal(6, receiver.Read(2));
        Assert.Equal(0x65, receiver.Read(5));
        Assert.Equal(4, receiver.Read(2));
        Assert.Equal(0x35, receiver.Read(0));
        Assert.Equal(2, receiver.Read(2));
        Assert.True(receiver.Irq); // Modem delta remains after acknowledging THRE.
        Assert.Equal(0, receiver.Read(2));
        Assert.Equal(0x11, receiver.Read(6));
        Assert.False(receiver.Irq);
        Assert.Equal(1, receiver.Read(2));
    }

    [Fact]
    public void OverrunReplacesTheUnreadByteAndReadingDataDoesNotClearLineErrors()
    {
        var sender = Device();
        var receiver = Device();
        receiver.Write(1, 5);
        sender.TransmitChanged = receiver.SetRxd;
        sender.Write(0, 0x19);
        ClockPair(sender, receiver, 170);
        sender.Write(0, 0x72);
        ClockPair(sender, receiver, 170);
        Assert.Equal(6, receiver.Read(2));
        Assert.Equal(0x72, receiver.Read(0));
        Assert.Equal(6, receiver.Read(2));
        Assert.Equal(0x62, receiver.Read(5));
        Assert.False(receiver.Irq);
    }

    [Fact]
    public void FalseStartIsRejectedBeforeAnyCharacterIsDelivered()
    {
        var receiver = Device();
        receiver.SetRxd(false);
        receiver.Advance(7);
        receiver.SetRxd(true);
        receiver.Advance(100);
        Assert.Equal(0x60, receiver.Read(5));
        Assert.Equal(1, receiver.Read(2));
    }

    [Fact]
    public void FramingErrorCanBeTheStartOfTheFollowingCharacter()
    {
        var receiver = Device();
        receiver.SetRxd(false);
        receiver.Advance(16);
        for (var bit = 0; bit < 8; bit++)
        {
            receiver.SetRxd((0x55 & (1 << bit)) != 0);
            receiver.Advance(16);
        }
        receiver.SetRxd(false); // Missing stop is the next character's start bit.
        receiver.Advance(16);
        Assert.Equal(0x69, receiver.Read(5));
        Assert.Equal(0x55, receiver.Read(0));
        for (var bit = 0; bit < 8; bit++)
        {
            receiver.SetRxd((0xaa & (1 << bit)) != 0);
            receiver.Advance(16);
        }
        receiver.SetRxd(true);
        receiver.Advance(16);
        Assert.Equal(0x61, receiver.Read(5));
        Assert.Equal(0xaa, receiver.Read(0));
    }

    [Fact]
    public void BreakDeliversOneZeroAndRequiresAHalfBitOfMarkingBeforeRestart()
    {
        var receiver = Device();
        receiver.Write(1, 5);
        receiver.SetRxd(false);
        receiver.Advance(161);
        Assert.Equal(0x79, receiver.Read(5));
        Assert.Equal(0, receiver.Read(0));
        receiver.Advance(1600);
        Assert.Equal(0x60, receiver.Read(5));
        receiver.SetRxd(true);
        receiver.Advance(7);
        receiver.SetRxd(false);
        receiver.Advance(200);
        Assert.Equal(0x60, receiver.Read(5));
        receiver.SetRxd(true);
        receiver.Advance(8);
        receiver.SetRxd(false);
        receiver.Advance(161);
        Assert.Equal(0x79, receiver.Read(5));
        Assert.Equal(0, receiver.Read(0));
    }

    [Fact]
    public void LoopbackUsesInternalSerialAndModemSignalsWhileExternalOutputsStayInactive()
    {
        var device = Device();
        var externalTransitions = new List<bool>();
        device.TransmitChanged = externalTransitions.Add;
        device.Write(4, 0x1f);
        Assert.Equal(0xfb, device.Read(6));
        Assert.True(device.Dtr);
        Assert.True(device.Rts);
        Assert.True(device.Out1);
        Assert.True(device.Out2);
        device.SetRxd(false);
        device.Write(0, 0xa5);
        device.Advance(180);
        Assert.Equal(0x61, device.Read(5));
        Assert.Equal(0xa5, device.Read(0));
        Assert.True(device.Txd);
        Assert.Empty(externalTransitions);
        device.Write(4, 0x10);
        Assert.Equal(0x0f, device.Read(6));
        device.Write(3, 0x43);
        device.Advance(161);
        Assert.Equal(0x79, device.Read(5));
        Assert.True(device.Txd);
        Assert.Empty(externalTransitions);
    }

    [Fact]
    public void PhysicalModemInputsAreComplementedAndRingDeltaUsesOnlyTheTrailingEdge()
    {
        var device = Device();
        device.Write(1, 8);
        device.SetCts(false);
        Assert.True(device.Irq);
        Assert.Equal(0x11, device.Read(6));
        Assert.False(device.Irq);
        device.SetDsr(false);
        device.SetDcd(false);
        Assert.Equal(0xba, device.Read(6));
        device.SetRing(false);
        Assert.False(device.Irq);
        Assert.Equal(0xf0, device.Read(6));
        device.SetRing(true);
        Assert.True(device.Irq);
        Assert.Equal(0xb4, device.Read(6));
        Assert.False(device.Irq);
    }

    [Fact]
    public void ModemOutputsAreActiveLowAndDoNotGateTheTransmitter()
    {
        var sender = Device();
        var receiver = Device();
        sender.TransmitChanged = receiver.SetRxd;
        sender.Write(4, 15);
        Assert.False(sender.Dtr);
        Assert.False(sender.Rts);
        Assert.False(sender.Out1);
        Assert.False(sender.Out2);
        sender.SetCts(true); // Deasserted CTS does not implement automatic flow control.
        sender.Write(0, 0xac);
        ClockPair(sender, receiver, 180);
        Assert.Equal(0xac, receiver.Read(0));
    }

    [Fact]
    public void RationalSourceClockHasTheSameResultAcrossBoardClockBatchSizes()
    {
        var batched = new Ns16450(8000000, 24000000, 13);
        var stepped = new Ns16450(8000000, 24000000, 13);
        Program(batched, 3, 7);
        Program(stepped, 3, 7);
        var first = new List<bool>();
        var second = new List<bool>();
        batched.TransmitChanged = first.Add;
        stepped.TransmitChanged = second.Add;
        batched.Write(0, 0x53);
        stepped.Write(0, 0x53);
        batched.Advance(5000);
        for (var clock = 0; clock < 5000; clock++) stepped.Advance(1);
        Assert.Equal(first, second);
        Assert.Equal(0x60, batched.Read(5));
        Assert.Equal(0x60, stepped.Read(5));
    }

    [Fact]
    public void WritingEitherDivisorLatchReloadsTheBaudCounter()
    {
        var device = new Ns16450(8000000, 24000000, 13);
        Program(device, 3, 7);
        device.Write(0, 0x53);
        device.Advance(30);
        Assert.True(device.Txd);
        device.Write(3, 0x83);
        device.Write(0, 7);
        device.Write(3, 3);
        device.Advance(30);
        Assert.True(device.Txd);
        device.Advance(1);
        Assert.False(device.Txd);
    }

    [Fact]
    public void ExternalReceiveClockOperatesIndependentlyOfTheTransmitDivisor()
    {
        var receiver = new Ns16450(1843200, externalReceiveClock: true);
        receiver.Write(3, 3);
        receiver.SetRxd(false);
        receiver.Advance(10000);
        Assert.Equal(0x60, receiver.Read(5));
        receiver.AdvanceReceiveClock(16);
        for (var bit = 0; bit < 8; bit++)
        {
            receiver.SetRxd((0x69 & (1 << bit)) != 0);
            receiver.AdvanceReceiveClock(16);
        }
        receiver.SetRxd(true);
        receiver.AdvanceReceiveClock(16);
        Assert.Equal(0x61, receiver.Read(5));
        Assert.Equal(0x69, receiver.Read(0));
    }

    [Fact]
    public void ResetClearsPendingTransfersAndInterruptsWhileKeepingTheExternalInputLevels()
    {
        var device = Device();
        device.Write(1, 15);
        device.Write(4, 15);
        device.SetCts(false);
        device.Write(0, 0x53);
        device.Advance(1);
        device.Write(0, 0x68);
        device.Reset();
        Assert.False(device.Irq);
        Assert.True(device.Txd);
        Assert.True(device.Dtr);
        Assert.True(device.Rts);
        Assert.Equal(0, device.Read(1));
        Assert.Equal(1, device.Read(2));
        Assert.Equal(0, device.Read(3));
        Assert.Equal(0, device.Read(4));
        Assert.Equal(0x60, device.Read(5));
        Assert.Equal(0x10, device.Read(6));
        device.Advance(1000);
        Assert.True(device.Txd);
    }

    private static Ns16450 Device(byte control = 3)
    {
        var device = new Ns16450(1843200);
        Program(device, control, 1);
        return device;
    }

    private static void Program(Ns16450 device, byte control, ushort divisor)
    {
        device.Write(3, (byte)(control | 0x80));
        device.Write(0, (byte)divisor);
        device.Write(1, (byte)(divisor >> 8));
        device.Write(3, control);
    }

    private static void ClockPair(Ns16450 sender, Ns16450 receiver, int clocks)
    {
        for (var clock = 0; clock < clocks; clock++)
        {
            sender.Advance(1);
            receiver.Advance(1);
        }
    }
}
