using RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Enums;
using RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Services;
using Xunit;

namespace RetroMediaLab.Chips.Tests.Video.Motorola.M6847.Chip6847;

public sealed class DataPathTests
{
    [Theory]
    [InlineData(0, 16, 3)]
    [InlineData(1, 16, 3)]
    [InlineData(2, 32, 3)]
    [InlineData(3, 16, 2)]
    [InlineData(4, 32, 2)]
    [InlineData(5, 16, 1)]
    [InlineData(6, 32, 1)]
    [InlineData(7, 32, 1)]
    public void GraphicsUsesEveryDataBitAndIgnoresAlphaPins(int mode, int bytes, int rows)
    {
        var chip = new Mc6847DataPath(new byte[280]);
        var pixelCount = 256 / bytes;
        for (var data = 0; data < 256; data++)
        for (var css = 0; css < 2; css++)
        {
            var pins = (Mc6847Mode)(128 | mode * 16 | css * 8);
            chip.Load((byte)data, 0, pins);
            Assert.Equal(bytes, chip.BytesPerLine);
            Assert.Equal(rows, chip.RowsPerMemoryLine);
            Assert.Equal(pixelCount, chip.PixelCount);
            var packed = chip.Pixels;
            for (var dot = 0; dot < pixelCount; dot++)
            {
                // Oracle des motifs : suite de quatre paires ou huit bits MSB-first.
                var resolution = mode is 1 or 3 or 5 or 7;
                var source = dot * (resolution ? 8 : 4) / pixelCount;
                var sample = resolution ? (data >> (7 - source)) & 1 : (data >> (6 - source * 2)) & 3;
                var expected = resolution
                    ? sample == 0 ? Mc6847Colour.Black : css == 0 ? Mc6847Colour.Green : Mc6847Colour.Buff
                    : (Mc6847Colour)(1 + css * 4 + sample);
                Assert.Equal(expected, chip.GetPixel(dot));
            }
            for (var alphaPins = 0; alphaPins < 8; alphaPins++)
            {
                chip.Load((byte)data, 11, pins | (Mc6847Mode)alphaPins);
                Assert.Equal(packed, chip.Pixels);
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SemigraphicsChecksEveryPatternColourAndRow(bool six)
    {
        var chip = new Mc6847DataPath(new byte[280]);
        for (var data = 0; data < 256; data++)
        for (var css = 0; css < 2; css++)
        for (var row = 0; row < 12; row++)
        {
            var mode = Mc6847Mode.Semigraphics | (six ? Mc6847Mode.External : 0) | (css == 0 ? 0 : Mc6847Mode.ColourSet);
            chip.Load((byte)data, row, mode);
            Assert.Equal(8, chip.PixelCount);
            Assert.Equal(32, chip.BytesPerLine);
            Assert.Equal(12, chip.RowsPerMemoryLine);
            var packed = chip.Pixels;
            for (var dot = 0; dot < 8; dot++)
            {
                var band = six ? row / 4 : row / 6;
                var bit = (six ? 5 : 3) - band * 2 - dot / 4;
                var lit = (data & (1 << bit)) != 0;
                var colour = six ? (data >> 6) + css * 4 : (data >> 4) & 7;
                Assert.Equal(lit ? (Mc6847Colour)(1 + colour) : Mc6847Colour.Black, chip.GetPixel(dot));
            }
            chip.Load((byte)data, row, mode | Mc6847Mode.Inverse | Mc6847Mode.GraphicsMode0 | Mc6847Mode.GraphicsMode1 | Mc6847Mode.GraphicsMode2);
            Assert.Equal(packed, chip.Pixels);
        }
    }

    [Fact]
    public void ExternalAlphaTreatsIncomingDataAsEightDotsAndLatchesInverseAndCss()
    {
        var chip = new Mc6847DataPath(new byte[280]);
        for (var data = 0; data < 256; data++)
        for (var row = 0; row < 12; row++)
        for (var selection = 0; selection < 4; selection++)
        {
            var inverse = (selection & 1) != 0;
            var orange = (selection & 2) != 0;
            var pins = Mc6847Mode.External | (inverse ? Mc6847Mode.Inverse : 0) | (orange ? Mc6847Mode.ColourSet : 0);
            chip.Load((byte)data, row, pins);
            var packed = chip.Pixels;
            for (var dot = 0; dot < 8; dot++)
            {
                var lit = ((data & (128 >> dot)) != 0) ^ inverse;
                var expected = orange ? lit ? Mc6847Colour.BrightOrange : Mc6847Colour.DarkOrange
                    : lit ? Mc6847Colour.Green : Mc6847Colour.DarkGreen;
                Assert.Equal(expected, chip.GetPixel(dot));
            }
            chip.Load((byte)data, row, pins | (Mc6847Mode)112);
            Assert.Equal(packed, chip.Pixels);
        }
    }

    [Fact]
    public void InternalAlphaUsesTheFiveBySevenMaskAndIgnoresHighCharacterBits()
    {
        var rom = Enumerable.Repeat((byte)255, 280).ToArray();
        var chip = new Mc6847DataPath(rom);
        Array.Clear(rom); // La copie des caractères appartient au composant.
        for (var code = 0; code < 256; code++)
        for (var row = 0; row < 12; row++)
        for (var inverse = 0; inverse < 2; inverse++)
        {
            chip.Load((byte)code, row, inverse == 0 ? Mc6847Mode.None : Mc6847Mode.Inverse);
            for (var dot = 0; dot < 8; dot++)
            {
                var lit = (row is >= 3 and < 10 && dot is >= 2 and < 7) ^ (inverse != 0);
                Assert.Equal(lit ? Mc6847Colour.Green : Mc6847Colour.DarkGreen, chip.GetPixel(dot));
            }
        }
    }

    [Fact]
    public void LoadingANewCellReplacesAllStoredDotsWithoutKeepingPreviousPixels()
    {
        var chip = new Mc6847DataPath(new byte[280]);
        chip.Load(255, 0, Mc6847Mode.Graphics);
        Assert.Equal(16, chip.PixelCount);
        chip.Load(0, 0, Mc6847Mode.External);
        Assert.Equal(8, chip.PixelCount);
        Assert.Equal(0UL, chip.Pixels >> 32);
        Assert.Throws<ArgumentOutOfRangeException>(() => chip.Load(0, 12, Mc6847Mode.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => chip.Load(0, -1, Mc6847Mode.None));
        Assert.Throws<ArgumentException>(() => new Mc6847DataPath(new byte[279]));
    }
}
