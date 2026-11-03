using RetroMediaLab.Chips.IO.NationalSemiconductor.UART.NS16450.Services;

namespace RetroMediaLab.Chips.Tests.IO.NationalSemiconductor.UART.NS16450;

public sealed partial class Ns16450Tests
{
    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(2, 2, 2)]
    [InlineData(3, 2, 4)]
    [InlineData(7, 10, 4)]
    [InlineData(65535, 131066, 4)]
    public void BaudOutDutyCycleMatchesTheManufacturerWaveform(int divisor, int highTicks, int lowTicks)
    {
        var device = new Ns16450(2, 1);
        Program(device, 3, (ushort)divisor);
        var edges = new List<bool>();
        device.BaudOutChanged = edges.Add;
        Assert.True(device.BaudOutHigh);
        device.Advance(highTicks - 1);
        Assert.Empty(edges);
        Assert.True(device.BaudOutHigh);
        device.Advance(1);
        Assert.Equal(new[] { false }, edges);
        Assert.False(device.BaudOutHigh);
        device.Advance(lowTicks - 1);
        Assert.Single(edges);
        device.Advance(1);
        Assert.Equal(new[] { false, true }, edges);
        Assert.True(device.BaudOutHigh);
        device.Advance(highTicks + lowTicks);
        Assert.Equal(new[] { false, true, false, true }, edges);
    }

    [Fact]
    public void BaudOutLevelIsCorrectEvenWithoutAnObserver()
    {
        var device = new Ns16450(2, 1);
        Program(device, 3, 3);
        device.Advance(1);
        Assert.True(device.BaudOutHigh);
        device.Advance(1);
        Assert.False(device.BaudOutHigh);
        device.Advance(3);
        Assert.False(device.BaudOutHigh);
        device.Advance(1);
        Assert.True(device.BaudOutHigh);
    }

    [Fact]
    public void MasterResetDoesNotResetTheBaudDividerOrStopItsOutput()
    {
        var device = new Ns16450(2, 1);
        Program(device, 3, 7);
        var edges = new List<bool>();
        device.BaudOutChanged = edges.Add;
        device.Advance(11);
        Assert.False(device.BaudOutHigh);
        device.SetReset(true);
        Assert.False(device.BaudOutHigh);
        device.Advance(3);
        Assert.True(device.BaudOutHigh);
        device.Advance(10);
        Assert.False(device.BaudOutHigh);
        Assert.Equal(new[] { false, true, false }, edges);
        Assert.Equal(0x60, device.Read(5));
        device.SetReset(false);
        device.Advance(4);
        Assert.True(device.BaudOutHigh);
    }

    [Fact]
    public void BaudOutCanClockAnotherUartsReceiverThroughItsElectricalInput()
    {
        var sender = Device();
        var receiver = new Ns16450(1843200, externalReceiveClock: true);
        receiver.Write(3, 3);
        sender.TransmitChanged = receiver.SetRxd;
        sender.BaudOutChanged = receiver.SetReceiveClock;
        sender.Write(0, 0xa5);
        sender.Advance(220);
        Assert.Equal(0x61, receiver.Read(5));
        Assert.Equal(0xa5, receiver.Read(0));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(13)]
    public void ObservingBaudOutDoesNotChangeSerialResultsOrFractionalPhase(int divisor)
    {
        var fast = new Ns16450(8000000, 24000000, 13);
        var observed = new Ns16450(8000000, 24000000, 13);
        Program(fast, 0x1f, (ushort)divisor);
        Program(observed, 0x1f, (ushort)divisor);
        fast.Write(4, 0x10);
        observed.Write(4, 0x10);
        var first = new List<bool>();
        var second = new List<bool>();
        fast.InterruptChanged = first.Add;
        observed.InterruptChanged = second.Add;
        observed.BaudOutChanged = _ => { };
        fast.Write(1, 7);
        observed.Write(1, 7);
        fast.Write(0, 0x5a);
        observed.Write(0, 0x5a);
        foreach (var batch in new[] { 1, 7, 513, 3, 10000, 1000 })
        {
            fast.Advance(batch);
            observed.Advance(batch);
            Assert.Equal(fast.BaudOutHigh, observed.BaudOutHigh);
            Assert.Equal(fast.Txd, observed.Txd);
        }
        Assert.Equal(first, second);
        Assert.Equal(fast.Read(5), observed.Read(5));
        Assert.Equal(fast.Read(0), observed.Read(0));
    }

    [Fact]
    public void BaudCallbackMayReloadTheDivisorWithoutUsingStaleRemainingClockPeriods()
    {
        var device = new Ns16450(2, 1);
        Program(device, 3, 3);
        var edges = new List<bool>();
        device.BaudOutChanged = high =>
        {
            edges.Add(high);
            if (edges.Count == 1) Program(device, 3, 7);
        };
        device.Advance(12);
        Assert.Equal(new[] { false, true, false }, edges);
        Assert.False(device.BaudOutHigh);
    }
}
