using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Constants;
using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Enums;
using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.WesternDigital.WD177x.WD1770;

public sealed class TimingTests
{
    [Theory]
    [InlineData(Wd1770Model.Wd1770, 0x8c, 30)]
    [InlineData(Wd1770Model.Wd1770, 0xac, 30)]
    [InlineData(Wd1770Model.Wd1770, 0xcc, 30)]
    [InlineData(Wd1770Model.Wd1770, 0xec, 30)]
    [InlineData(Wd1770Model.Wd1770, 0xfc, 30)]
    [InlineData(Wd1770Model.Wd1772, 0x8c, 15)]
    [InlineData(Wd1770Model.Wd1772, 0xac, 15)]
    [InlineData(Wd1770Model.Wd1772, 0xcc, 15)]
    [InlineData(Wd1770Model.Wd1772, 0xec, 15)]
    [InlineData(Wd1770Model.Wd1772, 0xfc, 15)]
    public void SettleFlagDelaysTypeTwoAndThreeCommandsByTheVariantDuration(
        Wd1770Model model, byte command, int milliseconds)
    {
        var chip = new Wd1770(model: model);
        chip.Write(Wd1770Constants.StatusCommandRegister, command);
        var ticks = Wd1770Constants.SiliconClock / Wd1770Constants.MillisecondsPerSecond * milliseconds;
        chip.Advance(ticks - 1);
        Assert.True(chip.Active);
        Assert.False(chip.Drq);
        chip.Advance(1);
        // Une entrée sans index ne fabrique plus de fin de recherche ni de disque absent.
        Assert.True(chip.Active);
        if ((command & 0xf0) == 0xf0)
        {
            Assert.True(chip.Drq);
            return;
        }
        if ((command & 0xf0) == 0xe0)
        {
            chip.SetIndex(true); Assert.True(chip.Active);
            chip.SetIndex(false); chip.SetIndex(true); Assert.False(chip.Active);
            return;
        }
        for (var index = 0; index < 4; index++) { chip.SetIndex(false); chip.SetIndex(true); }
        Assert.True(chip.Active);
        chip.SetIndex(false); chip.SetIndex(true);
        Assert.False(chip.Active);
        Assert.NotEqual(0, chip.Read(Wd1770Constants.StatusCommandRegister) & Wd1770Constants.NotFound);
    }
}
