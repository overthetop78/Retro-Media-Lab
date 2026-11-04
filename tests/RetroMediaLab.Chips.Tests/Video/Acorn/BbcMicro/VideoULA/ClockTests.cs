using RetroMediaLab.Chips.Video.Acorn.BbcMicro.VideoULA.Services;
using Xunit;

namespace RetroMediaLab.Chips.Tests.Video.Acorn.BbcMicro.VideoULA;

public sealed class ClockTests
{
    internal static void Clock(BbcVideoUla chip, int count = 1)
    {
        for (var i = 0; i < count; i++) { chip.SetMasterClock(true); chip.SetMasterClock(false); }
    }

    [Fact]
    public void DividerLevelsShareOnePhaseAndOnlyRisingEdgesAdvanceIt()
    {
        var chip = new BbcVideoUla();
        for (var cycle = 1; cycle <= 256; cycle++)
        {
            chip.SetMasterClock(true);
            Assert.Equal((cycle & 1) != 0, chip.Clock8);
            Assert.Equal((cycle & 2) != 0, chip.Clock4);
            Assert.Equal((cycle & 4) != 0, chip.Clock2);
            Assert.Equal((cycle & 8) != 0, chip.Clock1);
            chip.SetMasterClock(true);
            Assert.Equal((cycle & 8) != 0, chip.Clock1);
            chip.SetMasterClock(false);
            chip.SetMasterClock(false);
            Assert.Equal((cycle & 4) != 0, chip.Clock2);
        }
    }

    [Fact]
    public void ChangingCrtcSelectionDoesNotRestartTheOtherDividers()
    {
        var chip = new BbcVideoUla();
        Clock(chip, 4);
        Assert.True(chip.Clock2); Assert.False(chip.Clock1); Assert.False(chip.CrtcClock);
        chip.Write(0, 16);
        Assert.True(chip.CrtcClock); Assert.True(chip.Clock2); Assert.False(chip.Clock1);
        chip.Write(0, 0);
        Assert.False(chip.CrtcClock);
        Clock(chip, 4);
        Assert.True(chip.CrtcClock);
    }
}
