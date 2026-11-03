using RetroMediaLab.Chips.IO.NationalSemiconductor.UART.NS16450.Services;

namespace RetroMediaLab.Chips.Tests.IO.NationalSemiconductor.UART.NS16450;

public sealed partial class Ns16450Tests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    [InlineData(5, false)]
    [InlineData(6, false)]
    [InlineData(7, false)]
    public void OnlyTheDocumentedChipSelectCombinationDrivesTheBus(int selects, bool selected)
    {
        var device = Device();
        var data = device.SetBus((selects & 1) != 0, (selects & 2) != 0, (selects & 4) != 0,
            false, false, false, false, true, 5, 0);
        Assert.Equal(selected, device.ChipSelected);
        Assert.Equal(!selected, device.DriverDisableHigh);
        Assert.Equal(selected ? (byte?)0x60 : null, data);
    }

    [Theory]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    [InlineData(true, true, true)]
    [InlineData(true, false, true)]
    public void EitherReadStrobeCanActivateASelectedRead(bool rd, bool notRd, bool reading)
    {
        var device = Device();
        var value = device.SetBus(true, true, false, false, rd, notRd, false, true, 5, 0);
        Assert.Equal(reading ? (byte?)0x60 : null, value);
        Assert.Equal(!reading, device.DriverDisableHigh);
    }

    [Fact]
    public void AdsRisingEdgeRetainsAddressAndChipSelectUntilAdsFalls()
    {
        var device = Device();
        Bus(device, 7, 0x5a, writing: true);
        Bus(device, 7);
        Bus(device, 7, ads: true);
        var latched = device.SetBus(false, false, true, true, true, true, false, true, 5, 0);
        Assert.True(device.ChipSelected);
        Assert.Equal((byte?)0x5a, latched);
        var released = device.SetBus(false, false, true, false, true, true, false, true, 5, 0);
        Assert.False(device.ChipSelected);
        Assert.Null(released);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(true, false)]
    public void ModemOutputsAndRegistersUpdateAtTheEndOfEitherWriteStrobe(bool wr, bool notWr)
    {
        var device = Device();
        Assert.Null(device.SetBus(true, true, false, false, false, true, wr, notWr, 4, 3));
        Assert.True(device.Dtr);
        Assert.Equal(0, device.Read(4));
        device.SetBus(true, true, false, false, false, true, false, true, 4, 0);
        Assert.False(device.Dtr);
        Assert.False(device.Rts);
        Assert.Equal(3, device.Read(4));
    }

    [Fact]
    public void StretchedReadFreezesIirAndDoesNotAcknowledgeNewInterrupts()
    {
        var device = Device();
        device.Write(1, 8);
        Assert.Equal((byte?)1, Bus(device, 2, reading: true));
        device.SetCts(false);
        Assert.True(device.Irq);
        Assert.Equal((byte?)1, Bus(device, 2, reading: true));
        Bus(device, 2);
        Assert.Equal((byte?)0, Bus(device, 2, reading: true));
        Bus(device, 2);
        Assert.Equal((byte?)0x11, Bus(device, 6, reading: true));
        Assert.False(device.Irq);
    }

    [Fact]
    public void StretchedRbrReadDoesNotConsumeANewArrivalDuringTheSameBusAccess()
    {
        var device = Device();
        ReceiveByte(device, 0x19);
        Assert.Equal((byte?)0x19, Bus(device, 0, reading: true));
        ReceiveByte(device, 0xa6);
        Assert.Equal((byte?)0x19, Bus(device, 0, reading: true));
        Assert.Equal(0x61, device.Read(5));
        Bus(device, 0);
        Assert.Equal((byte?)0xa6, Bus(device, 0, reading: true));
    }

    [Fact]
    public void ThrWriteClearsEmptyFlagsAtTheLeadingEdgeButCannotTransmitUntilItEnds()
    {
        var device = Device();
        device.Write(1, 2);
        Bus(device, 0, 0xa5, writing: true);
        Assert.Equal(0, device.Read(5));
        Assert.False(device.Irq);
        device.Advance(1000);
        Assert.True(device.Txd);
        Assert.Equal(0, device.Read(5));
        Bus(device, 0);
        device.Advance(15);
        Assert.True(device.Txd);
        device.Advance(1);
        Assert.False(device.Txd);
    }

    [Fact]
    public void ResetAndUndefinedSimultaneousStrobesNeverDriveTheDataBus()
    {
        var device = Device();
        Assert.Null(Bus(device, 7, 0xa5, reading: true, writing: true));
        Assert.True(device.DriverDisableHigh);
        Bus(device, 7);
        Assert.Equal(0, device.Read(7));
        device.SetReset(true);
        Assert.Null(Bus(device, 5, reading: true));
        Assert.True(device.DriverDisableHigh);
    }

    private static byte? Bus(Ns16450 device, int register, byte input = 0,
        bool reading = false, bool writing = false, bool ads = false) =>
        device.SetBus(true, true, false, ads, reading, true, writing, true, register, input);
}
