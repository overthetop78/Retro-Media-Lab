using RetroMediaLab.Chips.Audio.TexasInstruments.SN764xx.SN76489.Services;
using static RetroMediaLab.Chips.Tests.Audio.TexasInstruments.SN764xx.SN76489.Sn76489ToneTests;

namespace RetroMediaLab.Chips.Tests.Audio.TexasInstruments.SN764xx.SN76489;

public sealed class Sn76489NoiseTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void AllNoiseModesFollowThe15StageGeneratorAndTwoOutputStages(int mode)
    {
        var chip = new Sn76489();
        Mute(chip);
        Tone(chip, 2, 5);
        chip.Write((byte)(0xe0 | mode));
        chip.Write(0xf0);
        var spacing = (mode & 3) == 3 ? 160 : 512 << (mode & 3);
        var core = 1 << 14;
        var outputStages = 0;
        for (var sample = 0; sample < 128; sample++)
        {
            // Separate 15-bit recurrence and output pipeline, rather than copying the 17-bit implementation.
            var feedback = (mode & 4) == 0 ? core & 1 : (core ^ (core >> 1)) & 1;
            outputStages = (outputStages >> 1) | ((core & 1) << 1);
            core = (core >> 1) | (feedback << 14);
            Clock(chip, sample == 0 ? 16 : 1);
            Assert.Equal((outputStages & 1) != 0, chip.Output != 0);
            var held = chip.Output;
            Clock(chip, spacing - 1);
            Assert.Equal(held, chip.Output);
        }
    }

    [Theory]
    [InlineData(false, 15)]
    [InlineData(true, 32767)]
    public void ACompleteNoiseCycleHasTheMeasuredPeriodAndNoShorterRepetition(bool white, int period)
    {
        var chip = new Sn76489();
        Mute(chip);
        Tone(chip, 2, 1);
        chip.Write(white ? (byte)0xe7 : (byte)0xe3);
        chip.Write(0xf0);
        Clock(chip, 16 + 2 * 32); // Fill the two output stages.
        var first = new bool[period];
        for (var index = 0; index < period; index++)
        {
            first[index] = chip.Output != 0;
            Clock(chip, 32);
        }
        var ones = first.Count(bit => bit);
        Assert.Equal(white ? 16384 : 1, ones);
        for (var index = 0; index < period; index++)
        {
            Assert.Equal(first[index], chip.Output != 0);
            Clock(chip, 32);
        }
        foreach (var divisor in Enumerable.Range(1, period - 1).Where(divisor => period % divisor == 0))
            Assert.Contains(Enumerable.Range(divisor, period - divisor), index => first[index] != first[index % divisor]);
    }

    [Fact]
    public void NoiseControlContinuationResetsTheGeneratorButNotItsCountdown()
    {
        var chip = new Sn76489();
        Mute(chip);
        chip.Write(0xe0);
        chip.Write(0xf0);
        Clock(chip, 16 + 15 * 512);
        Assert.True(chip.Output > 0);
        Clock(chip, 19);
        chip.Write(0xe0);
        Assert.Equal(0, chip.Output);
        // The first shift still occurs at the original phase, not 512 clocks after the write.
        // Sixteen shifts are required to bring the reset bit through the A output pipeline.
        Clock(chip, 16 * 512 - 20);
        Assert.Equal(0, chip.Output);
        chip.Tick();
        Assert.True(chip.Output > 0);

        var latched = new Sn76489();
        var continued = new Sn76489();
        Mute(latched);
        Mute(continued);
        for (var mode = 0; mode < 8; mode++)
        {
            latched.Write((byte)(0xe0 | mode));
            continued.Write(0xe0);
            continued.Write((byte)(0x78 | mode)); // unused bits do not alter the three control bits.
            latched.Write(0xf0);
            continued.Write(0xf0);
            for (var clock = 0; clock < 35000; clock++)
            {
                Assert.Equal(latched.Output, continued.Output);
                latched.Tick();
                continued.Tick();
            }
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(17)]
    [InlineData(1023)]
    public void ToneThreeNoiseRateUsesTheUpdatedTonePeriodIncludingZero(int divisor)
    {
        var chip = new Sn76489();
        Mute(chip);
        Tone(chip, 2, 1);
        chip.Write(0xe3);
        chip.Write(0xf0);
        Clock(chip, 16);
        // A frequency change affects subsequent reloads, not the already-running noise countdown.
        Tone(chip, 2, divisor);
        var spacing = 32 * (divisor == 0 ? 1024 : divisor);
        Clock(chip, 32 + 14 * spacing - 1);
        Assert.Equal(0, chip.Output);
        chip.Tick();
        Assert.True(chip.Output > 0);
        Clock(chip, spacing - 1);
        Assert.True(chip.Output > 0);
        chip.Tick();
        Assert.Equal(0, chip.Output);
    }
}
