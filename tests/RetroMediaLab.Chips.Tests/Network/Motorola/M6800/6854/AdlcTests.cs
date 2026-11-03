using RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Services;
using C = RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Constants.Mc6854Constants;
using B = RetroMediaLab.Chips.Tests.Network.Motorola.M6800.Chip6854.AdlcTestBench;

namespace RetroMediaLab.Chips.Tests.Network.Motorola.M6800.Chip6854;

public sealed class AdlcTests
{
    [Fact]
    public void ReceiverResetRearmsDisconnectedCarrierInterrupt()
    {
        var chip = new Mc6854();
        chip.Write(0, C.ReceiveInterrupt);
        Assert.True(chip.Irq);
        chip.Read(1);
        chip.Write(1, C.ClearReceive);
        Assert.False(chip.Irq);
        chip.Write(0, C.ReceiveReset);
        chip.Write(0, C.ReceiveInterrupt);
        Assert.True(chip.Irq);
    }

    [Fact]
    public void AcknowledgedIdleStaysVisibleWithoutRepeatedInterrupts()
    {
        var chip = B.Device(control1: C.ReceiveInterrupt);
        for (var bit = 0; bit < 15; bit++)
            B.ClockReceive(chip, true);
        Assert.NotEqual(0, chip.Read(1) & C.Idle);
        chip.Write(1, C.ClearReceive);
        Assert.False(chip.Irq);
        for (var bit = 0; bit < 50; bit++)
            B.ClockReceive(chip, true);
        Assert.NotEqual(0, chip.Read(1) & C.Idle);
        Assert.False(chip.Irq);
        Assert.Equal(new byte[] { 1, 2, 3 }, B.ReceivePacket(chip, [1, 2, 3]));
        Assert.True(chip.Irq);
    }

    [Fact]
    public void ClearReceiveDoesNotAcknowledgeAnUnreadEvent()
    {
        var chip = B.Device(control1: C.ReceiveInterrupt);
        chip.SetCarrierPresent(false);
        chip.Write(1, C.ClearReceive);
        Assert.True(chip.Irq);
        chip.Read(0); // SR1 n’est pas la lecture de SR2 requise pour DCD.
        chip.Write(1, C.ClearReceive);
        Assert.True(chip.Irq);
        chip.Read(1);
        chip.Write(1, C.ClearReceive);
        Assert.False(chip.Irq);
    }

    [Fact]
    public void ReceiveFrameIsCopiedAndRequiresCarrierAndReleasedReset()
    {
        var chip = new Mc6854(8);
        Assert.Equal(C.DataCarrierDetect, chip.Read(1));
        Assert.False(chip.Irq);
        Assert.False(chip.ReceiveFrame([1, 2]));
        chip.SetLine(true);
        Assert.False(chip.ReceiveFrame([1, 2]));
        chip.Write(0, C.AddressControl);
        chip.Write(3, 0x1e);
        chip.Write(0, 0);
        byte[] data = [0xfe, 0, 1, 0x80, 0x99, 0xa5];
        Assert.True(chip.ReceiveFrame(data));
        data[0] = 12;
        var actual = new List<byte>();
        for (var tick = 0; tick < 200 && actual.Count < data.Length; tick++)
        {
            chip.Tick();
            if ((chip.Read(1) & C.ReceiveAvailable) != 0)
                actual.Add(chip.Read(2));
        }
        Assert.Equal(new byte[] { 0xfe, 0, 1, 0x80, 0x99, 0xa5 }, actual);
        Assert.NotEqual(0, chip.Read(1) & C.FrameValid);
        Assert.False(chip.ReceiveFrame([1, 2]));
        chip.Write(1, C.ClearReceive);
        Assert.True(chip.ReceiveFrame([1, 2]));
    }

    [Fact]
    public void UnderrunAbortsAndResetClearsOnlyTransmitter()
    {
        var chip = B.Device();
        var frames = 0;
        chip.FrameTransmitted = _ => frames++;
        try
        {
            chip.Write(2, 0x55);
            for (var bit = 0; bit < 40; bit++)
                B.ClockTransmit(chip);
            Assert.NotEqual(0, chip.Read(0) & C.TransmitUnderrun);
            Assert.Equal(0, frames);
            chip.Write(0, C.TransmitReset);
            chip.Write(0, 0);
            Assert.Equal(0, chip.Read(0) & C.TransmitUnderrun);
        }
        finally { chip.FrameTransmitted = null; }
        Assert.Equal(new byte[] { 0x66, 0x77 }, B.Transmit(chip, [0x66, 0x77]).Frame);
    }

    [Fact]
    public void AddressHasEightBitsEvenWhenInformationHasFive()
    {
        var chip = B.Device(5);
        Assert.Equal(new byte[] { 0xff, 0xff, 0x1f }, B.Transmit(chip, [0xff, 0xff, 0xff]).Frame);
    }

    [Fact]
    public void PriorityAddressRequiresADataReadAndCannotBeClearedAsStatus()
    {
        var chip = B.Device(control1: C.ReceiveInterrupt);
        chip.Write(1, C.Prioritize);
        var wire = B.Encode(B.Octets(1, 2, 3, 4, 5, 6));
        var position = 0;
        while (position < wire.Length && (chip.Read(1) & C.AddressPresent) == 0)
            B.ClockReceive(chip, wire[position++]);
        Assert.Equal(C.AddressPresent, chip.Read(1));
        Assert.Equal(0, chip.Read(0) & C.DataAvailable);
        chip.Write(1, C.Prioritize | C.ClearReceive);
        Assert.True(chip.Irq);
        Assert.Equal(C.AddressPresent, chip.Read(1));
        Assert.Equal(1, chip.Read(2));
        Assert.Equal(0, chip.Read(1) & C.AddressPresent);
    }
}
