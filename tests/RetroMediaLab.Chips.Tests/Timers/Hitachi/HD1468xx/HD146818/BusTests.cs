using RetroMediaLab.Chips.Timers.Hitachi.HD1468xx.HD146818.Services;
using RetroMediaLab.Chips.Timers.Hitachi.HD1468xx.HD146818.Enums;
using R = RetroMediaLab.Chips.Timers.Hitachi.HD1468xx.HD146818.Constants.Hd146818Constants;

namespace RetroMediaLab.Chips.Tests.Timers.Hitachi.HD1468xx.HD146818;

public sealed class BusTests
{
    private static void Address(Hd146818 chip, byte address, bool intel)
    {
        chip.SetBus(address, true, true, intel, true);
        chip.SetBus(address, true, false, intel, true);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AddressIsLatchedOnFallingAsAndWriteOnTrailingDataStrobe(bool intel)
    {
        var chip = new Hd146818();
        Address(chip, 32, intel);
        chip.SetBus(0x17, true, false, true, false);
        chip.SetBus(0x39, true, false, true, false);
        Assert.Equal(0, chip.ReadRegister(32));
        chip.SetBus(0x5a, true, false, intel, intel);
        Assert.Equal(0x5a, chip.ReadRegister(32));
        Address(chip, 32, intel);
        chip.SetBus(0, true, false, !intel, true);
        Assert.True(chip.DrivingDataBus);
        Assert.Equal(0x5a, chip.DataBus);
        chip.SetBus(0, false, false, !intel, true);
        Assert.False(chip.DrivingDataBus);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StretchingAReadDoesNotReacquireDataOrAcknowledgeANewCause(bool intel)
    {
        var chip = new Hd146818();
        chip.WriteRegister(R.StatusA, 0x26);
        chip.WriteRegister(R.StatusB, R.StopUpdates);
        chip.AdvanceClocks(32);
        Address(chip, R.StatusC, intel);
        chip.SetBus(0, true, false, !intel, true);
        Assert.Equal(R.PeriodicFlag, chip.DataBus);
        chip.AdvanceClocks(32);
        chip.SetBus(0, true, false, !intel, true);
        chip.SetBus(0, true, false, intel, true);
        Assert.Equal(R.PeriodicFlag, chip.ReadRegister(R.StatusC));
    }

    [Fact]
    public void StandbyReleasesBusButDoesNotStopTheDivider()
    {
        var chip = new Hd146818(variant: Hd146818Variant.Hd146818A);
        chip.SetMotorolaMode(false);
        chip.WriteRegister(R.StatusA, 0x26);
        chip.WriteRegister(32, 0x55);
        Address(chip, 32, true);
        chip.SetBus(0, true, false, false, true);
        Assert.True(chip.DrivingDataBus);
        chip.SetStandby(true);
        Assert.True(chip.DrivingDataBus);
        chip.SetBus(0, true, false, true, true);
        Address(chip, 32, true);
        chip.SetBus(0, true, false, false, true);
        chip.SetBus(0, true, false, true, true);
        Address(chip, 32, true);
        chip.SetBus(0x17, true, false, true, false);
        chip.SetBus(0x17, true, false, true, true);
        Assert.False(chip.DrivingDataBus);
        Assert.Equal(0x55, chip.ReadRegister(32));
        chip.AdvanceClocks(32);
        Assert.Equal(R.PeriodicFlag, chip.ReadRegister(R.StatusC));
        chip.SetStandby(false);
        Address(chip, 32, true);
        chip.SetBus(0, true, false, false, true);
        Assert.False(chip.DrivingDataBus);
        chip.SetBus(0, true, false, true, true);
        Address(chip, 32, true);
        chip.SetBus(0, true, false, false, true);
        Assert.True(chip.DrivingDataBus);
        Assert.Equal(0x55, chip.DataBus);
    }

    [Fact]
    public void ARevisionUsesTheMotPinInsteadOfDetectingDsAtAddressLatch()
    {
        var chip = new Hd146818(variant: Hd146818Variant.Hd146818A);
        chip.SetMotorolaMode(true);
        chip.WriteRegister(32, 0x5a);
        Address(chip, 32, true);
        Assert.True(chip.DrivingDataBus);
        Assert.Equal(0x5a, chip.DataBus);
        chip.SetBus(0, true, false, false, true);
        Assert.False(chip.DrivingDataBus);
    }

    [Fact]
    public void OriginalPartDoesNotAcquireTheARevisionStandbyPin()
    {
        var chip = new Hd146818();
        chip.SetStandby(true);
        Address(chip, 32, false);
        chip.SetBus(0, true, false, true, true);
        Assert.True(chip.DrivingDataBus);
    }
}
