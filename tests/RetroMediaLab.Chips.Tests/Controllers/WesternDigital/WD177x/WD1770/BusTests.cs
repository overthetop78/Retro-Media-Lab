using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Constants;
using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Enums;
using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.WesternDigital.WD177x.WD1770;

public sealed class BusTests
{
    [Theory]
    [InlineData(Wd1770Model.Wd1770)]
    [InlineData(Wd1770Model.Wd1772)]
    public void UnselectedBusHasNoEffectsAndDoesNotDriveDal(Wd1770Model model)
    {
        var chip = new Wd1770(model: model);
        chip.SetBus(false, false, Wd1770Constants.TrackRegister, 0x91);
        chip.SetBus(false, true, Wd1770Constants.TrackRegister);
        Assert.Null(chip.BusData);
        Assert.Equal(0, chip.Read(Wd1770Constants.TrackRegister));
    }

    [Theory]
    [InlineData(Wd1770Model.Wd1770, Wd1770Constants.TrackRegister)]
    [InlineData(Wd1770Model.Wd1770, Wd1770Constants.SectorRegister)]
    [InlineData(Wd1770Model.Wd1770, Wd1770Constants.DataRegister)]
    [InlineData(Wd1770Model.Wd1772, Wd1770Constants.TrackRegister)]
    [InlineData(Wd1770Model.Wd1772, Wd1770Constants.SectorRegister)]
    [InlineData(Wd1770Model.Wd1772, Wd1770Constants.DataRegister)]
    public void WriteSamplesLastDataAtTheEndOfTheSelectedCycle(Wd1770Model model, int register)
    {
        var chip = new Wd1770(model: model);
        var original = chip.Read(register);
        chip.SetBus(true, false, register, 0x21);
        chip.SetBus(true, false, register, 0xb6);
        Assert.Null(chip.BusData);
        Assert.Equal(original, chip.Read(register));
        chip.SetBus(false, false, register);
        Assert.Equal(0xb6, chip.Read(register));
        chip.SetBus(true, true, register);
        Assert.Equal((byte)0xb6, chip.BusData);
        chip.SetBus(false, true, register);
        Assert.Null(chip.BusData);
    }

    [Theory]
    [InlineData(Wd1770Model.Wd1770)]
    [InlineData(Wd1770Model.Wd1772)]
    public void SwitchingToReadCompletesThePreviousWrite(Wd1770Model model)
    {
        var chip = new Wd1770(model: model);
        chip.SetBus(true, false, Wd1770Constants.TrackRegister, 0x73);
        chip.SetBus(true, true, Wd1770Constants.TrackRegister);
        Assert.Equal((byte)0x73, chip.BusData);
    }

    [Theory]
    [InlineData(Wd1770Model.Wd1770)]
    [InlineData(Wd1770Model.Wd1772)]
    public void ResetDiscardsAnUnfinishedBusWrite(Wd1770Model model)
    {
        var chip = new Wd1770(model: model);
        chip.SetBus(true, false, Wd1770Constants.TrackRegister, 0x73);
        chip.Reset();
        chip.SetBus(false, false, Wd1770Constants.TrackRegister);
        Assert.Equal(0, chip.Read(Wd1770Constants.TrackRegister));
        Assert.Null(chip.BusData);
    }

    [Theory]
    [InlineData(Wd1770Model.Wd1770)]
    [InlineData(Wd1770Model.Wd1772)]
    public void BusyProtectsTrackAndSectorThroughBothAccessPaths(Wd1770Model model)
    {
        var chip = new Wd1770(model: model);
        chip.Write(Wd1770Constants.TrackRegister, 7);
        chip.Write(Wd1770Constants.SectorRegister, 9);
        chip.Write(Wd1770Constants.StatusCommandRegister, 0x88);
        Assert.True(chip.Active);
        chip.SetBus(true, false, Wd1770Constants.TrackRegister, 0x73);
        chip.SetBus(false, false, Wd1770Constants.TrackRegister);
        chip.Write(Wd1770Constants.SectorRegister, 0x52);
        Assert.Equal(7, chip.Read(Wd1770Constants.TrackRegister));
        Assert.Equal(9, chip.Read(Wd1770Constants.SectorRegister));
    }
}
