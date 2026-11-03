using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Constants;
using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Enums;
using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.WesternDigital.WD177x.WD1770;

public sealed class InterruptTests
{
    [Theory]
    [InlineData(Wd1770Model.Wd1770)]
    [InlineData(Wd1770Model.Wd1772)]
    public void ImmediateInterruptRequiresD0BeforeItCanBeAcknowledged(Wd1770Model model)
    {
        var chip = new Wd1770(model: model);
        chip.Write(Wd1770Constants.StatusCommandRegister, 0xd8);
        Assert.True(chip.Irq);
        chip.Read(Wd1770Constants.StatusCommandRegister);
        Assert.True(chip.Irq);
        chip.SetBus(true, true, Wd1770Constants.StatusCommandRegister);
        chip.SetBus(false, true, Wd1770Constants.StatusCommandRegister);
        Assert.True(chip.Irq);
        chip.Write(Wd1770Constants.TrackRegister, 5);
        Assert.True(chip.Irq);
        chip.Write(Wd1770Constants.StatusCommandRegister, 0x58);
        Assert.True(chip.Irq);
        chip.Write(Wd1770Constants.StatusCommandRegister, 0xd0);
        Assert.False(chip.Irq);
        Assert.False(chip.Active);
    }

    [Theory]
    [InlineData(Wd1770Model.Wd1770)]
    [InlineData(Wd1770Model.Wd1772)]
    public void AnInterruptedTypeTwoCommandKeepsItsStatusInterpretation(Wd1770Model model)
    {
        var chip = new Wd1770(model: model);
        chip.SetTrackZero(true);
        chip.Write(Wd1770Constants.StatusCommandRegister, 0x88);
        chip.Write(Wd1770Constants.StatusCommandRegister, 0xd0);
        Assert.Equal(0, chip.Read(Wd1770Constants.StatusCommandRegister) & Wd1770Constants.TrackZeroStatus);
        chip.Write(Wd1770Constants.StatusCommandRegister, 0xd0);
        Assert.NotEqual(0, chip.Read(Wd1770Constants.StatusCommandRegister) & Wd1770Constants.TrackZeroStatus);
    }

    [Theory]
    [InlineData(Wd1770Model.Wd1770)]
    [InlineData(Wd1770Model.Wd1772)]
    public void StatusBusReadAcknowledgesOnlyWhenTheCycleEnds(Wd1770Model model)
    {
        var chip = new Wd1770(model: model);
        chip.Write(Wd1770Constants.StatusCommandRegister, 0xd4);
        chip.SetIndex(true);
        Assert.True(chip.Irq);
        chip.SetBus(true, true, Wd1770Constants.StatusCommandRegister);
        Assert.True(chip.Irq);
        chip.SetBus(true, true, Wd1770Constants.StatusCommandRegister);
        Assert.True(chip.Irq);
        chip.SetBus(false, true, Wd1770Constants.StatusCommandRegister);
        Assert.False(chip.Irq);
    }

    [Theory]
    [InlineData(Wd1770Model.Wd1770, 0xd1)]
    [InlineData(Wd1770Model.Wd1770, 0xd2)]
    [InlineData(Wd1770Model.Wd1770, 0xd3)]
    [InlineData(Wd1770Model.Wd1772, 0xd1)]
    [InlineData(Wd1770Model.Wd1772, 0xd2)]
    [InlineData(Wd1770Model.Wd1772, 0xd3)]
    public void ReservedReadyConditionsDoNotGenerateAnInterrupt(Wd1770Model model, byte command)
    {
        var chip = new Wd1770(model: model);
        chip.Write(Wd1770Constants.StatusCommandRegister, command);
        chip.Advance(Wd1770Constants.SiliconClock);
        Assert.False(chip.Irq);
    }
}
