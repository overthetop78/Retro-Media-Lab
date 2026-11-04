using RetroMediaLab.Chips.Video.Acorn.BbcMicro.VideoULA.Services;
using Xunit;

namespace RetroMediaLab.Chips.Tests.Video.Acorn.BbcMicro.VideoULA;

public sealed class RegisterTests
{
    [Fact]
    public void ControlAndPaletteDecodeOnlyA0AndPreserveAllControlBits()
    {
        var chip = new BbcVideoUla();
        for (var control = 0; control < 256; control++)
        {
            chip.Write(0xfe2e, (byte)control);
            Assert.Equal(control, chip.Control);
            Assert.Equal((control & 2) != 0, chip.TeletextSelected);
            Assert.Equal((control & 16) != 0, chip.FastCrtc);
            chip.Write(0xfe2f, 0x67);
            Assert.Equal(control, chip.Control);
        }
    }

    [Fact]
    public void EveryPaletteEntryAndBothFlashPhasesMatchTheRgbTruthTable()
    {
        for (var index = 0; index < 16; index++)
        for (var raw = 0; raw < 16; raw++)
        for (var flash = 0; flash < 2; flash++)
        {
            var chip = new BbcVideoUla { DisplayEnabled = true };
            chip.Write(0, (byte)(0x1c | flash));
            chip.Write(1, (byte)(index * 16 + raw));
            chip.Data = (byte)((index & 8) * 16 + (index & 4) * 8 + (index & 2) * 4 + (index & 1) * 2);
            ClockTests.Clock(chip);
            var expected = raw & 7;
            if (raw < 8 || flash == 0) expected = 7 - expected;
            Assert.Equal(expected, chip.Rgb);
            Assert.Equal((expected & 1) != 0, chip.Red);
            Assert.Equal((expected & 2) != 0, chip.Green);
            Assert.Equal((expected & 4) != 0, chip.Blue);
        }
    }

    [Fact]
    public void ResetRestoresTheDeterministicEmulatorState()
    {
        var chip = new BbcVideoUla { DisplayEnabled = true, Cursor = true, TeletextRgb = 5, Inverted = true, Data = 0xff };
        chip.Write(0, 0xff); chip.Write(1, 0xf8); ClockTests.Clock(chip, 7);
        chip.Reset();
        Assert.Equal(0, chip.Control); Assert.Equal(0, chip.Data); Assert.Equal(0, chip.Rgb);
        Assert.False(chip.DisplayEnabled); Assert.False(chip.Cursor); Assert.False(chip.Inverted);
        Assert.False(chip.Clock8); Assert.False(chip.Clock4); Assert.False(chip.Clock2); Assert.False(chip.Clock1);
        Assert.False(chip.CursorActive);
    }
}
