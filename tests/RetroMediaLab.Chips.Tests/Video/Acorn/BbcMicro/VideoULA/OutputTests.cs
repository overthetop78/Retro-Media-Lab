using RetroMediaLab.Chips.Video.Acorn.BbcMicro.VideoULA.Services;
using Xunit;

namespace RetroMediaLab.Chips.Tests.Video.Acorn.BbcMicro.VideoULA;

public sealed class OutputTests
{
    [Fact]
    public void TeletextMultiplexingBypassesThePaletteAndItsDisplayGate()
    {
        var chip = new BbcVideoUla();
        chip.Write(0, 2);
        for (var rgb = 0; rgb < 8; rgb++)
        {
            chip.TeletextRgb = rgb;
            Assert.Equal(rgb, chip.Rgb);
            chip.Inverted = true; Assert.Equal(rgb ^ 7, chip.Rgb);
            chip.Inverted = false;
        }
        chip.Write(0, 0); Assert.Equal(0, chip.Rgb);
        chip.DisplayEnabled = true; Assert.Equal(0, chip.Rgb);
    }

    [Fact]
    public void CursorInvertsTeletextAsWellAsBitmapAndIsGatedByDisplay()
    {
        var chip = new BbcVideoUla { DisplayEnabled = true, Cursor = true, TeletextRgb = 1 };
        chip.Write(0, 0x92); ClockTests.Clock(chip);
        Assert.Equal(6, chip.Rgb);
        chip.DisplayEnabled = false; Assert.Equal(1, chip.Rgb);
        chip.DisplayEnabled = true; Assert.Equal(6, chip.Rgb);
        chip.Inverted = true; Assert.Equal(1, chip.Rgb);
    }
}
