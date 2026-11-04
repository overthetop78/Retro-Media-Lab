using RetroMediaLab.Chips.Video.Philips.SAA50xx.SAA5050.Services;
using Xunit;

namespace RetroMediaLab.Chips.Tests.Video.Philips.SAA50xx.SAA5050;

public sealed class AttributeTests
{
    private static TeletextSaa5050 Solid()
    {
        var rom = new byte[960];
        Array.Fill(rom, (byte)31, 10, rom.Length - 10);
        var chip = new TeletextSaa5050(rom);
        chip.BeginLine(0);
        return chip;
    }

    [Fact]
    public void EveryColourAppliesAfterItsControlAndBackgroundAppliesAtItsControl()
    {
        for (var colour = 1; colour <= 7; colour++)
        {
            var chip = Solid();
            Assert.Equal(7, chip.Character(colour, 0, 0).Foreground);
            Assert.Equal(colour, chip.Character('A', 0, 0).Foreground);
            Assert.Equal(colour, chip.Character(29, 0, 0).Background);
            Assert.Equal(0, chip.Character(28, 0, 0).Background);
            Assert.Equal(colour, chip.Character(colour + 16, 0, 0).Foreground);
            Assert.Equal(colour, chip.Character(127, 0, 0).Foreground);
        }
    }

    [Fact]
    public void HoldCapturesGraphicShapeAndItsSeparationUntilRelease()
    {
        var chip = Solid();
        chip.Character(17, 0, 0);
        var original = chip.Character(127, 0, 0).Mask;
        Assert.Equal(original, chip.Character(30, 0, 0).Mask);
        Assert.Equal(original, chip.Character(26, 0, 0).Mask);
        Assert.Equal(original, chip.Character(31, 0, 0).Mask);
        Assert.Equal(0, chip.Character(29, 0, 0).Mask);
    }

    [Fact]
    public void FlashSteadyAndConcealHaveDistinctCurrentCellEffects()
    {
        var chip = Solid();
        chip.Character(17, 0, 0);
        chip.Character(127, 0, 0);
        chip.Character(30, 0, 0);
        Assert.NotEqual(0, chip.Character(8, 0, 48).Mask);
        Assert.Equal(0, chip.Character(127, 0, 48).Mask);
        Assert.NotEqual(0, chip.Character(9, 0, 48).Mask);
        Assert.Equal(0, chip.Character(24, 0, 0).Mask);
        Assert.Equal(0, chip.Character(2, 0, 0).Mask);
        Assert.NotEqual(0, chip.Character('A', 0, 0).Mask);
    }

    [Fact]
    public void FlashCycleIsThreeVisibleQuartersAndRepeats()
    {
        var chip = Solid();
        for (var field = 0; field < 128; field++)
        {
            chip.BeginLine(0);
            chip.Character(8, 0, field);
            Assert.Equal(field % 64 < 48, chip.Character('A', 0, field).Mask != 0);
        }
    }

    [Fact]
    public void DataBitSevenIsNotACharacterInput()
    {
        var a = Solid();
        var b = Solid();
        for (var data = 0; data < 128; data++)
        {
            a.BeginLine(0);
            b.BeginLine(0);
            Assert.Equal(a.Character(data, 6, 0), b.Character(data | 128, 6, 0));
        }
    }

    [Fact]
    public void LineResetRestoresWhiteBlackAndCancelsGraphicsHoldConcealAndFlash()
    {
        var chip = Solid();
        chip.Character(17, 0, 48);
        chip.Character(127, 0, 48);
        chip.Character(30, 0, 48);
        chip.Character(29, 0, 48);
        chip.Character(8, 0, 48);
        chip.Character(24, 0, 48);
        chip.BeginLine(0);
        Assert.Equal((0x3ff, 7, 0), chip.Character('A', 0, 48));
        Assert.Equal(0, chip.Character(30, 0, 48).Mask);
    }
}
