using RetroMediaLab.Chips.Video.Acorn.BbcMicro.VideoULA.Services;
using Xunit;

namespace RetroMediaLab.Chips.Tests.Video.Acorn.BbcMicro.VideoULA;

public sealed class ShiftTests
{
    [Theory]
    [InlineData(false, 0)] [InlineData(false, 1)] [InlineData(false, 2)] [InlineData(false, 3)]
    [InlineData(true, 0)] [InlineData(true, 1)] [InlineData(true, 2)] [InlineData(true, 3)]
    public void AllBytesFollowTheFourPhysicalTapsAndShiftInOnes(bool fast, int mode)
    {
        var period = fast ? 8 : 16;
        var hold = 8 >> mode;
        for (var data = 0; data < 256; data++)
        {
            var chip = new BbcVideoUla { DisplayEnabled = true, Data = (byte)data };
            chip.Write(0, (byte)((fast ? 16 : 0) | mode * 4));
            for (var i = 0; i < 16; i++) chip.Write(1, (byte)(i * 16 + ((i ^ (i >> 1)) & 7 ^ 7)));
            for (var pixel = 0; pixel < period * 2; pixel++)
            {
                var shift = pixel % period / hold;
                int Bit(int tap) => tap < shift ? 1 : data >> (tap - shift) & 1;
                var logical = Bit(7) * 8 + Bit(5) * 4 + Bit(3) * 2 + Bit(1);
                var expected = (logical ^ (logical >> 1)) & 7;
                ClockTests.Clock(chip);
                Assert.Equal(expected, chip.Rgb);
            }
        }
    }

    [Fact]
    public void ChangingDataDuringACellOnlyAffectsTheNextLoad()
    {
        var chip = new BbcVideoUla { DisplayEnabled = true, Data = 255 };
        chip.Write(0, 0x1c);
        for (var i = 0; i < 16; i++) chip.Write(1, (byte)(i * 16 + (i ^ 7)));
        ClockTests.Clock(chip); chip.Data = 0;
        for (var i = 0; i < 7; i++) { ClockTests.Clock(chip); Assert.Equal(7, chip.Rgb); }
        ClockTests.Clock(chip); Assert.Equal(0, chip.Rgb);
    }

    [Fact]
    public void PaletteWriteInTheMiddleOfASlowCellChangesItsRemainingPixels()
    {
        var chip = new BbcVideoUla { DisplayEnabled = true, Data = 255 };
        chip.Write(0, 8); chip.Write(1, 0xf6);
        ClockTests.Clock(chip, 8); Assert.Equal(1, chip.Rgb);
        chip.Write(1, 0xf3); Assert.Equal(4, chip.Rgb);
        ClockTests.Clock(chip, 8); Assert.Equal(4, chip.Rgb);
    }
}
