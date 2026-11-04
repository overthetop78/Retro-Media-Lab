using RetroMediaLab.Chips.Video.Philips.SAA50xx.SAA5050.Services;
using Xunit;

namespace RetroMediaLab.Chips.Tests.Video.Philips.SAA50xx.SAA5050;

public sealed class GlyphTests
{
    [Fact]
    public void EveryRomCharacterAndRowIsAddressedWithoutAliasing()
    {
        var rom = new byte[TeletextSaa5050.CharacterRomSize];
        for (var character = 32; character < 128; character++)
        {
            Array.Clear(rom);
            for (var row = 0; row < 10; row++) rom[(character - 32) * 10 + row] = 31;
            var chip = new TeletextSaa5050(rom);
            for (var probe = 32; probe < 128; probe++)
                for (var raster = 0; raster < 20; raster++)
                {
                    chip.BeginLine(0);
                    Assert.Equal(character == probe ? 0x3ff : 0, chip.Character(probe, raster, 0).Mask);
                }
        }
    }

    [Fact]
    public void RomIsCopiedAndHighBitsDoNotCreateExtraColumns()
    {
        var rom = Enumerable.Repeat((byte)255, TeletextSaa5050.CharacterRomSize).ToArray();
        var chip = new TeletextSaa5050(rom);
        Array.Clear(rom);
        chip.BeginLine(0);
        Assert.Equal(0x3ff, chip.Character('A', 0, 0).Mask);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(20)]
    [InlineData(int.MaxValue)]
    public void RasterOutsideCellDoesNotReadAdjacentGlyph(int raster)
    {
        var chip = new TeletextSaa5050(Enumerable.Repeat((byte)31, 960).ToArray());
        chip.BeginLine(0);
        Assert.Equal(0, chip.Character('A', raster, 0).Mask);
    }

    [Fact]
    public void EmptyRomIsBlankAndWrongRomSizesAreRejected()
    {
        var chip = new TeletextSaa5050([]);
        chip.BeginLine(0);
        Assert.Equal(0, chip.Character('A', 4, 0).Mask);
        foreach (var size in new[] { 1, 959, 961 })
            Assert.Throws<ArgumentException>(() => new TeletextSaa5050(new byte[size]));
    }

    [Fact]
    public void BothDiagonalDirectionsRoundTowardsTheAdjacentRowOnly()
    {
        foreach (var reflected in new[] { false, true })
        {
            var rom = new byte[960];
            rom[('A' - 32) * 10 + 3] = reflected ? (byte)1 : (byte)2;
            rom[('A' - 32) * 10 + 4] = reflected ? (byte)2 : (byte)1;
            var chip = new TeletextSaa5050(rom);
            chip.BeginLine(0);
            Assert.Equal(reflected ? 3 : 12, chip.Character('A', 6, 0).Mask);
            Assert.Equal(reflected ? 7 : 14, chip.Character('A', 7, 0).Mask);
        }
    }

    [Fact]
    public void DoubleHeightRoundingUsesInternalLineParityRatherThanFieldParity()
    {
        var rom = new byte[960];
        for (var row = 0; row < 10; row++) rom[('A' - 32) * 10 + row] = (byte)(1 << (row % 5));
        var normal = new TeletextSaa5050(rom);
        var large = new TeletextSaa5050(rom);
        for (var raster = 0; raster < 20; raster++)
        {
            normal.BeginLine(0);
            large.BeginLine(0);
            large.Character(13, raster, 0);
            var expected = normal.Character('A', raster / 2, 0).Mask;
            Assert.Equal(expected, large.Character('A', raster, 0).Mask);
        }
    }

    [Fact]
    public void AllSixMosaicBlocksFollowTheDataBitsAndThreeBandBoundaries()
    {
        var chip = new TeletextSaa5050([]);
        for (var motif = 0; motif < 64; motif++)
            for (var row = 0; row < 10; row++)
            {
                var data = 32 | (motif & 31) | ((motif & 32) << 1);
                var band = row < 3 ? 0 : row < 7 ? 1 : 2;
                var left = (motif & (1 << (band * 2))) != 0;
                var right = (motif & (2 << (band * 2))) != 0;
                chip.BeginLine(0);
                chip.Character(17, row * 2, 0);
                Assert.Equal((left ? 0xfc0 : 0) | (right ? 0x03f : 0), chip.Character(data, row * 2, 0).Mask);
            }
    }
    [Fact]
    public void SeparatedMosaicBlocksHaveTwoHalfDotGapsAndOneLineGaps()
    {
        var chip = new TeletextSaa5050([]);
        for (var motif = 0; motif < 64; motif++)
            for (var row = 0; row < 10; row++)
            {
                var data = 32 | (motif & 31) | ((motif & 32) << 1);
                var band = row < 3 ? 0 : row < 7 ? 1 : 2;
                var separator = row is 2 or 6 or 9;
                chip.BeginLine(0);
                chip.Character(17, row * 2, 0);
                chip.Character(26, row * 2, 0);
                var actual = chip.Character(data, row * 2, 0).Mask;
                for (var pixel = 0; pixel < 12; pixel++)
                {
                    var block = pixel < 6 ? 0 : 1;
                    var gap = pixel % 6 < 2;
                    var expected = !separator && !gap && (motif & (1 << (band * 2 + block))) != 0;
                    Assert.Equal(expected, (actual & (1 << (11 - pixel))) != 0);
                }
            }
    }
}
