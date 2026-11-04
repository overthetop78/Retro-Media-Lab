using RetroMediaLab.Chips.Video.Acorn.BbcMicro.VideoULA.Services;
using Xunit;

namespace RetroMediaLab.Chips.Tests.Video.Acorn.BbcMicro.VideoULA;

public sealed class CursorTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void AllEightPatternsKeepTheIndependentOneOneTwoCharacterSegments(bool fast)
    {
        for (var mask = 0; mask < 8; mask++)
        {
            var chip = new BbcVideoUla { DisplayEnabled = true, Cursor = true };
            chip.Write(0, (byte)((mask << 5) | (fast ? 16 : 0)));
            var period = fast ? 8 : 16;
            for (var cell = 0; cell < 5; cell++)
            {
                for (var dot = 0; dot < period; dot++)
                {
                    ClockTests.Clock(chip);
                    var expected = cell switch { 0 => (mask & 4) != 0, 1 => (mask & 2) != 0, 2 or 3 => (mask & 1) != 0, _ => false };
                    Assert.Equal(expected, chip.CursorActive);
                    Assert.Equal(expected ? 7 : 0, chip.Rgb);
                }
                chip.Cursor = false;
            }
        }
    }

    [Fact]
    public void CursorContinuesToShiftWhenDisplayIsBlanked()
    {
        var chip = new BbcVideoUla { DisplayEnabled = true, Cursor = true };
        chip.Write(0, 0xf0);
        ClockTests.Clock(chip, 8);
        chip.Cursor = false; chip.DisplayEnabled = false;
        ClockTests.Clock(chip, 24);
        chip.DisplayEnabled = true;
        ClockTests.Clock(chip);
        Assert.False(chip.CursorActive); Assert.Equal(0, chip.Rgb);
    }
}
