using RetroMediaLab.Chips.IO.NationalSemiconductor.UART.NS16450.Services;

namespace RetroMediaLab.Chips.Tests.IO.NationalSemiconductor.UART.NS16450;

public sealed partial class Ns16450Tests
{
    public static IEnumerable<object[]> Formats()
    {
        foreach (var length in Enumerable.Range(0, 4))
            foreach (var parity in new[] { 0, 0x08, 0x18, 0x28, 0x38 })
                foreach (var stop in new[] { 0, 4 })
                    yield return new object[] { length | parity | stop };
    }

    [Theory]
    [MemberData(nameof(Formats))]
    public void EveryByteTransfersThroughPinsInEveryDocumentedFormat(int control)
    {
        var sender = Device((byte)control);
        var receiver = Device((byte)control);
        sender.TransmitChanged = receiver.SetRxd;
        var mask = (1 << (5 + (control & 3))) - 1;
        for (var value = 0; value < 256; value++)
        {
            sender.Write(0, (byte)value);
            ClockPair(sender, receiver, 240);
            Assert.Equal(0x61, receiver.Read(5));
            Assert.Equal(value & mask, receiver.Read(0));
            Assert.Equal(0x60, sender.Read(5));
        }
    }

    [Fact]
    public void FirstStopSetsStatusThenTheNextReceiveClockPublishesTheInterrupt()
    {
        var device = Device();
        device.Write(1, 1);
        ReceiveDataBits(device, 0x59);
        device.SetRxd(true);
        device.Advance(7);
        Assert.Equal(0x60, device.Read(5));
        device.Advance(1);
        Assert.Equal(0x61, device.Read(5));
        Assert.False(device.Irq);
        Assert.Equal(1, device.Read(2));
        device.Advance(1);
        Assert.True(device.Irq);
        Assert.Equal(4, device.Read(2));
        Assert.Equal(0x59, device.Read(0));
        Assert.False(device.Irq);
    }

    [Fact]
    public void ReadingDataBeforeTheDelayedInterruptCancelsThatCause()
    {
        var device = Device();
        device.Write(1, 1);
        ReceiveDataBits(device, 0x42);
        device.SetRxd(true);
        device.Advance(8);
        Assert.Equal(0x42, device.Read(0));
        device.Advance(32);
        Assert.False(device.Irq);
    }

    [Fact]
    public void ReadingLineStatusBeforeTheDelayedErrorDoesNotCancelDataReady()
    {
        var device = Device();
        device.Write(1, 5);
        ReceiveDataBits(device, 0x55);
        device.SetRxd(false);
        device.Advance(8);
        Assert.Equal(0x69, device.Read(5));
        device.Advance(1);
        Assert.Equal(4, device.Read(2));
        Assert.Equal(0x55, device.Read(0));
        Assert.False(device.Irq);
    }

    [Fact]
    public void ConsecutiveCharactersDoNotInsertAnIdleBitAndThreInterruptWaitsEightClocks()
    {
        var device = Device();
        device.Write(1, 2);
        device.Read(2);
        device.Write(0, 0xff);
        device.Advance(16);
        Assert.False(device.Txd);
        Assert.Equal(2, device.Read(2));
        device.Write(0, 0xff);
        device.Advance(159);
        Assert.True(device.Txd);
        Assert.Equal(0, device.Read(5));
        device.Advance(1);
        Assert.False(device.Txd); // Exactly ten bits after the first start.
        Assert.Equal(0x20, device.Read(5));
        Assert.False(device.Irq);
        device.Advance(7);
        Assert.False(device.Irq);
        device.Advance(1);
        Assert.Equal(2, device.Read(2));
    }

    [Fact]
    public void TemtUsesExternalRclkWhenTheReceiverClockIsSeparatelyWired()
    {
        var device = new Ns16450(1843200, externalReceiveClock: true);
        Program(device, 3, 1);
        device.Write(0, 0xff);
        device.Advance(176);
        Assert.Equal(0x20, device.Read(5));
        device.Advance(1000);
        Assert.Equal(0x20, device.Read(5));
        device.AdvanceReceiveClock(1);
        Assert.Equal(0x20, device.Read(5));
        device.AdvanceReceiveClock(1);
        Assert.Equal(0x60, device.Read(5));
    }

    [Fact]
    public void RewritingAFullHoldingRegisterDoesNotRestartTheInitialStartDelay()
    {
        var device = Device();
        device.Write(4, 0x10);
        device.Write(0, 0x11);
        device.Advance(8);
        device.Write(0, 0x66);
        device.Advance(8);
        Assert.Equal(0x20, device.Read(5));
        device.Advance(180);
        Assert.Equal(0x66, device.Read(0));
    }

    [Fact]
    public void DivisorChangesFromAPinCallbackApplyToTheRemainingBatchedTime()
    {
        var batched = Device();
        var stepped = Device();
        var first = new List<bool>();
        var second = new List<bool>();
        Attach(batched, first);
        Attach(stepped, second);
        batched.Write(0, 0x55);
        stepped.Write(0, 0x55);
        batched.Advance(300);
        for (var tick = 0; tick < 300; tick++) stepped.Advance(1);
        Assert.Equal(second, first);
        Assert.Equal(stepped.Txd, batched.Txd);
        Assert.Equal(0x20, batched.Read(5));

        static void Attach(Ns16450 device, List<bool> levels)
        {
            device.TransmitChanged = high =>
            {
                levels.Add(high);
                if (levels.Count == 1) Program(device, 3, 2);
            };
        }
    }

    [Fact]
    public void ResetAssertedFromAPinCallbackCancelsAllRemainingBatchedClocks()
    {
        var device = Device();
        var transitions = new List<bool>();
        device.TransmitChanged = high =>
        {
            transitions.Add(high);
            if (!high) device.SetReset(true);
        };
        device.Write(0, 0x55);
        device.Advance(10000);
        Assert.Equal(new[] { false, true }, transitions);
        Assert.Equal(0x60, device.Read(5));
        Assert.False(device.Irq);
    }

    [Fact]
    public void ReadReturnsTheOriginalByteEvenIfItsIrqCallbackReceivesAnotherByte()
    {
        var device = Device();
        device.Write(1, 1);
        ReceiveByte(device, 0x29);
        device.InterruptChanged = high =>
        {
            if (!high)
            {
                device.InterruptChanged = null;
                ReceiveByte(device, 0xb6);
            }
        };
        Assert.Equal(0x29, device.Read(0));
        Assert.Equal(0xb6, device.Read(0));
    }

    [Fact]
    public void MasterResetRetainsUnspecifiedDataDivisorAndScratchButInhibitsTransfersWhileHeld()
    {
        var device = Device();
        ReceiveByte(device, 0x69);
        device.Write(7, 0xb5);
        device.SetReset(true);
        device.Write(1, 15);
        device.Write(3, 0xff);
        device.Write(4, 15);
        device.Write(0, 0xff);
        device.SetCts(false);
        device.Advance(10000);
        Assert.Equal(0, device.Read(1));
        Assert.Equal(0, device.Read(3));
        Assert.Equal(0, device.Read(4));
        Assert.Equal(0x60, device.Read(5));
        Assert.Equal(0x10, device.Read(6));
        Assert.Equal(0x69, device.Read(0));
        Assert.Equal(0xb5, device.Read(7));
        device.SetReset(false);
        Assert.Equal(0x10, device.Read(6));
        device.Write(3, 0x80);
        Assert.Equal(1, device.Read(0));
        Assert.Equal(0, device.Read(1));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(256)]
    [InlineData(65535)]
    public void DivisorExtremesScaleSerialTimingWithoutTruncation(int divisor)
    {
        var device = Device();
        Program(device, 3, (ushort)divisor);
        device.Write(0, 0xff);
        device.Advance(16 * divisor - 1);
        Assert.True(device.Txd);
        device.Advance(1);
        Assert.False(device.Txd);
        device.Advance(162 * divisor);
        Assert.Equal(0x60, device.Read(5));
    }

    [Fact]
    public void LongIdleAdvancesPreserveFractionalPhaseWithoutOverflow()
    {
        var device = new Ns16450(int.MaxValue, int.MaxValue, 65535);
        Program(device, 3, ushort.MaxValue);
        for (var batch = 0; batch < 4; batch++) device.Advance(int.MaxValue);
        Assert.Equal(0x60, device.Read(5));
        Assert.True(device.Txd);
    }

    [Theory]
    [MemberData(nameof(Formats))]
    public void QuietClockSkippingPreservesAllPinAndInterruptTransitionsForEveryFormat(int control)
    {
        foreach (var loopback in new[] { false, true })
        {
            var batched = Device((byte)control);
            var stepped = Device((byte)control);
            var first = new List<string>();
            var second = new List<string>();
            batched.TransmitChanged = high => first.Add("TX:" + high);
            stepped.TransmitChanged = high => second.Add("TX:" + high);
            batched.InterruptChanged = high => first.Add("IRQ:" + high);
            stepped.InterruptChanged = high => second.Add("IRQ:" + high);
            batched.Write(1, 7);
            stepped.Write(1, 7);
            if (loopback)
            {
                batched.Write(4, 0x10);
                stepped.Write(4, 0x10);
            }
            foreach (var value in new byte[] { 0x55, 0x00, 0xff, 0xa5 })
            {
                batched.Write(0, value);
                stepped.Write(0, value);
                batched.Advance(240);
                for (var tick = 0; tick < 240; tick++) stepped.Advance(1);
                Assert.Equal(second, first);
                Assert.Equal(stepped.Read(5), batched.Read(5));
                Assert.Equal(stepped.Read(2), batched.Read(2));
                Assert.Equal(stepped.Read(0), batched.Read(0));
            }
        }
    }

    private static void ReceiveDataBits(Ns16450 device, byte value)
    {
        device.SetRxd(false);
        device.Advance(16);
        for (var bit = 0; bit < 8; bit++)
        {
            device.SetRxd((value & (1 << bit)) != 0);
            device.Advance(16);
        }
    }

    private static void ReceiveByte(Ns16450 device, byte value)
    {
        ReceiveDataBits(device, value);
        device.SetRxd(true);
        device.Advance(16);
    }
}
