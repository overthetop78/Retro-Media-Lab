using RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Services;
using C = RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Constants.Mc6854Constants;

namespace RetroMediaLab.Chips.Tests.Network.Motorola.M6800.Chip6854;
public sealed class AdlcTests
{
    [Fact]
    public void ReceiverResetRearmsTheDisconnectedClockStatus()
    {
        var device = new Mc6854();
        device.Write(0, C.ReceiveInterrupt);
        Assert.True(device.Irq);
        device.Read(0);
        device.Read(1);
        device.Write(1, C.ClearReceive);
        Assert.False(device.Irq);
        device.Write(0, C.ReceiveReset);
        device.Write(0, C.ReceiveInterrupt);
        Assert.True(device.Irq);
    }

    [Fact]
    public void AcknowledgedIdleRemainsReadableWithoutRepeatedInterrupts()
    {
        var device = Device(C.ReceiveInterrupt);
        for (var tick = 0; tick < 16; tick++)
            device.Tick();
        Assert.NotEqual(0, device.Read(1) & C.Idle);
        device.Read(0);
        device.Write(1, C.ClearReceive);
        for (var tick = 0; tick < 32; tick++)
            device.Tick();
        Assert.NotEqual(0, device.Read(1) & C.Idle);
        Assert.False(device.Irq);
        Assert.True(device.ReceiveFrame([0x42]));
        ByteTime(device);
        Assert.True(device.Irq);
    }

    [Fact]
    public void ReceiveClearPreservesAnEventThatHasNotBeenRead()
    {
        var device = Device(C.ReceiveInterrupt);
        Assert.True(device.ReceiveFrame([0x42]));
        ByteTime(device);
        device.Write(1, C.ClearReceive);
        Assert.NotEqual(0, device.Read(1) & C.FrameValid);
        device.Write(1, C.ClearReceive);
        Assert.Equal(0, device.Read(1) & C.FrameValid);
        Assert.Equal(0x42, device.Read(2));
    }

    private static Mc6854 Device(byte control = 0)
    {
        var device = new Mc6854(4);
        device.SetLine(true);
        device.Write(0, C.AddressControl);
        device.Write(3, 0x1e);
        device.Write(0, control);
        device.Read(0);
        device.Read(1);
        device.Write(1, C.ClearReceive | C.ClearTransmit);
        return device;
    }

    private static void ByteTime(Mc6854 device)
    {
        for (var index = 0; index < 4; index++)
            device.Tick();
    }

    [Fact]
    public void ResetAndDisconnectedClockDoNotPretendTheNetworkIsReady()
    {
        var device = new Mc6854();
        Assert.Equal(C.NoClock, device.Read(1));
        Assert.False(device.Irq);
        Assert.False(device.ReceiveFrame([1, 2]));
        device.SetLine(true);
        Assert.False(device.ReceiveFrame([1, 2]));
        device.Write(0, 0);
        Assert.True((device.Read(0) & C.TransmitAvailable) != 0);
        device.Reset();
        Assert.Equal(0, device.Read(0) & C.TransmitAvailable);
        Assert.False(device.Irq);
    }

    [Fact]
    public void ReceivedBytesFollowTheClockAndRetainAddressAndEndMarkers()
    {
        var device = Device(C.ReceiveInterrupt);
        byte[] frame = [254, 0, 1, 0, 0x80, 0x99, 0xa5];
        Assert.True(device.ReceiveFrame(frame));
        frame[0] = 12;
        for (var tick = 0; tick < 3; tick++)
            device.Tick();
        Assert.Equal(0, device.Read(0) & C.DataAvailable);
        device.Tick();
        Assert.True(device.Irq);
        Assert.Equal(C.AddressPresent | C.ReceiveAvailable, device.Read(1));
        Assert.Equal(254, device.Read(2));
        byte[] remaining = [0, 1, 0, 0x80, 0x99, 0xa5];
        for (var index = 0; index < remaining.Length; index++)
        {
            ByteTime(device);
            if (index == remaining.Length - 1)
                Assert.NotEqual(0, device.Read(1) & C.FrameValid);
            Assert.Equal(remaining[index], device.Read(3));
        }

        Assert.False(device.ReceiveFrame([1]));
        device.Write(1, C.ClearReceive);
        Assert.False(device.Irq);
        Assert.True(device.ReceiveFrame([2]));
    }

    [Fact]
    public void TransmitUnderrunAndResetDiscardAnIncompleteFrame()
    {
        var device = Device(C.TransmitInterrupt);
        var frames = new List<byte[]>();
        device.FrameTransmitted = frame => frames.Add(frame.ToArray());
        device.Write(2, 0x55);
        ByteTime(device);
        ByteTime(device);
        Assert.NotEqual(0, device.Read(0) & C.TransmitUnderrun);
        Assert.Empty(frames);
        device.Write(0, C.TransmitReset);
        device.Write(0, 0);
        Assert.Equal(0, device.Read(0) & C.TransmitUnderrun);
        device.Write(3, 0x66);
        ByteTime(device);
        Assert.Equal(new byte[] { 0x66 }, Assert.Single(frames));
    }

    [Fact]
    public void AddressControlSeparatesWordLengthAndTransmitData()
    {
        var device = Device();
        var frames = new List<byte[]>();
        device.FrameTransmitted = frame => frames.Add(frame.ToArray());
        device.Write(0, C.AddressControl);
        device.Write(3, 0);
        device.Write(0, 0);
        device.Write(3, 0xff);
        ByteTime(device);
        Assert.Equal(new byte[] { 31 }, Assert.Single(frames));
    }

    [Fact]
    public void PrioritizedReceiveStatusHidesDataUntilAddressIsAcknowledged()
    {
        var device = Device(C.ReceiveInterrupt);
        device.Write(1, C.Prioritize | C.ClearReceive);
        Assert.True(device.ReceiveFrame([1, 2]));
        ByteTime(device);
        Assert.Equal(C.AddressPresent, device.Read(1));
        Assert.Equal(0, device.Read(0) & C.DataAvailable);
        device.Write(1, C.Prioritize | C.ClearReceive);
        Assert.NotEqual(0, device.Read(0) & C.DataAvailable);
        Assert.Equal(1, device.Read(2));
        ByteTime(device);
        Assert.Equal(2, device.Read(2));
        Assert.NotEqual(0, device.Read(1) & C.FrameValid);
        device.Write(1, C.Prioritize | C.ClearReceive);
        Assert.Equal(0, device.Read(1) & C.FrameValid);
    }
}
