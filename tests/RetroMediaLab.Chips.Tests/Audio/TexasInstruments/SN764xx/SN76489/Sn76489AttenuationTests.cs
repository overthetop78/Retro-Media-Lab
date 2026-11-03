using RetroMediaLab.Chips.Audio.TexasInstruments.SN764xx.SN76489.Services;
using static RetroMediaLab.Chips.Tests.Audio.TexasInstruments.SN764xx.SN76489.Sn76489ToneTests;

namespace RetroMediaLab.Chips.Tests.Audio.TexasInstruments.SN764xx.SN76489;

public sealed class Sn76489AttenuationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void AllAttenuatorsHaveSixteenLevelsAndIgnoreTheUnusedDataBits(int channel)
    {
        var chip = new Sn76489();
        Mute(chip);
        if (channel < 3) Tone(chip, channel, 1023);
        else chip.Write(0xe0);
        chip.Write((byte)(0x90 | channel << 5));
        do { chip.Tick(); } while (chip.Output == 0);
        for (var attenuation = 0; attenuation < 16; attenuation++)
        {
            var expected = attenuation == 15 ? 0
                : (int)Math.Round(short.MaxValue / 4.0 * Math.Pow(10, -attenuation / 10.0));
            chip.Write((byte)(0x90 | channel << 5 | attenuation));
            Assert.Equal(expected, chip.Output);
            for (var ignoredBits = 0; ignoredBits < 8; ignoredBits++)
            {
                chip.Write((byte)(ignoredBits << 4 | attenuation));
                Assert.Equal(expected, chip.Output);
            }
        }
    }

    [Fact]
    public void FourChannelsMixLinearlyWithoutClippingOrChangingOtherAttenuators()
    {
        var chip = new Sn76489();
        Mute(chip);
        for (var channel = 0; channel < 3; channel++) Tone(chip, channel, 1023);
        chip.Write(0xe0);
        // The noise pipeline first exposes a high bit at internal tick 481, before any tone toggles low.
        Clock(chip, 16 * 481);
        var sum = 0;
        for (var channel = 0; channel < 4; channel++)
        {
            chip.Write((byte)(0x90 | channel << 5));
            sum += (int)Math.Round(short.MaxValue / 4.0);
            Assert.Equal(sum, chip.Output);
        }
        Assert.InRange(chip.Output, 0, short.MaxValue + 1);
        for (var channel = 0; channel < 4; channel++)
        {
            chip.Write((byte)(0x9f | channel << 5));
            sum -= (int)Math.Round(short.MaxValue / 4.0);
            Assert.Equal(sum, chip.Output);
        }
    }
}
