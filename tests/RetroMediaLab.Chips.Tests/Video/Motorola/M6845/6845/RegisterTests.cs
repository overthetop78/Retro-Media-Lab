using RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Enums;
using RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Services;

namespace RetroMediaLab.Chips.Tests.Video.Motorola.M6845.Chip6845;

public sealed class RegisterTests
{
    [Theory]
    [InlineData(Crtc6845Model.Mc6845)]
    [InlineData(Crtc6845Model.Hd6845S)]
    public void EveryRegisterMasksEveryInputValueAndProtectsReadOnlyRegisters(Crtc6845Model model)
    {
        // Motorola table 2 / Hitachi table 1, independent of production constants.
        byte[] masks = [255, 255, 255, 255, 127, 31, 127, 127, 243, 31, 127, 31, 63, 255, 63, 255];
        if (model == Crtc6845Model.Mc6845) { masks[3] = 15; masks[8] = 3; }
        var crtc = new Crtc6845(model);
        for (byte register = 0; register < 32; register++)
        {
            crtc.Select(register);
            for (var value = 0; value <= byte.MaxValue; value++)
            {
                crtc.Write((byte)value);
                var expected = register < 16 ? value & masks[register] : 0;
                if (register < 18) Assert.Equal(expected, crtc[register]);
                var firstReadable = model == Crtc6845Model.Hd6845S ? 12 : 14;
                Assert.Equal(register >= firstReadable && register < 18 ? expected : 0, crtc.Read());
            }
        }
    }

    [Theory]
    [InlineData(Crtc6845Model.Mc6845)]
    [InlineData(Crtc6845Model.Hd6845S)]
    public void AddressLatchUsesFiveBitsWithoutAliasingInvalidRegisters(Crtc6845Model model)
    {
        var crtc = new Crtc6845(model);
        crtc.Select(0x8e); crtc.Write(0xff);
        crtc.Select(14); Assert.Equal(0x3f, crtc.Read());
        crtc.Select(0xff); crtc.Write(0);
        crtc.Select(14); Assert.Equal(0x3f, crtc.Read());
        crtc.Select(18); Assert.Equal(0, crtc.Read());
    }

    [Fact]
    public void ExistingAcornPathUsesHitachiExtensionsAndRejectsUnknownVariant()
    {
        Assert.Equal(Crtc6845Model.Hd6845S, new Crtc6845().Model);
        Assert.Throws<ArgumentOutOfRangeException>(() => new Crtc6845((Crtc6845Model)int.MaxValue));
    }
}
