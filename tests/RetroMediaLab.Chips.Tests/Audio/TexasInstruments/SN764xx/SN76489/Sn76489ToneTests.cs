using RetroMediaLab.Chips.Audio.TexasInstruments.SN764xx.SN76489.Services;

namespace RetroMediaLab.Chips.Tests.Audio.TexasInstruments.SN764xx.SN76489;

public sealed class Sn76489ToneTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void EveryTenBitDivisorHasItsFullHighAndLowHalfPeriod(int channel)
    {
        for (var divisor = 0; divisor < 1024; divisor++)
        {
            var chip = new Sn76489();
            Mute(chip);
            Tone(chip, channel, divisor);
            chip.Write((byte)(0x90 | channel << 5));
            Clock(chip, 15);
            Assert.Equal(0, chip.Output);
            Clock(chip, 1);
            var amplitude = chip.Output;
            Assert.True(amplitude > 0);
            var halfPeriod = 16 * (divisor == 0 ? 1024 : divisor);
            Clock(chip, halfPeriod - 1);
            Assert.Equal(amplitude, chip.Output);
            Clock(chip, 1);
            Assert.Equal(0, chip.Output);
            Clock(chip, halfPeriod - 1);
            Assert.Equal(0, chip.Output);
            Clock(chip, 1);
            Assert.Equal(amplitude, chip.Output);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void PeriodWritesPreserveCurrentCountdownAndTheOtherPeriodBits(int channel)
    {
        var chip = new Sn76489();
        Mute(chip);
        Tone(chip, channel, 0x123);
        chip.Write((byte)(0x90 | channel << 5));
        Clock(chip, 16);
        var amplitude = chip.Output;
        Clock(chip, 17);
        // The low nibble leaves high 0x12 in place; continuation then changes only high bits.
        chip.Write((byte)(0x80 | channel << 5 | 5));
        chip.Write(0x62); // bit 6 ignored: resulting divisor 0x225.
        Clock(chip, 0x123 * 16 - 18);
        Assert.Equal(amplitude, chip.Output);
        Clock(chip, 1);
        Assert.Equal(0, chip.Output);
        Clock(chip, 0x225 * 16 - 1);
        Assert.Equal(0, chip.Output);
        Clock(chip, 1);
        Assert.Equal(amplitude, chip.Output);
    }

    [Fact]
    public void HighPartCanBeWrittenImmediatelyAfterResetWithoutSelectingARegister()
    {
        var chip = new Sn76489();
        // Power-on selects tone 0. Writing the high six bits must not select another register.
        chip.Write(0x41);
        chip.Write(0xbf);
        chip.Write(0xdf);
        chip.Write(0xff);
        Clock(chip, 16);
        Assert.True(chip.Output > 0);
        Clock(chip, 16 * 16);
        Assert.Equal(0, chip.Output);
    }

    [Fact]
    public void MutingDoesNotStopOscillatorsOrTheirDivider()
    {
        var muted = new Sn76489();
        var audible = new Sn76489();
        Mute(muted);
        Mute(audible);
        for (var channel = 0; channel < 3; channel++)
        {
            Tone(muted, channel, 3 + channel * 7);
            Tone(audible, channel, 3 + channel * 7);
            audible.Write((byte)(0x90 | channel << 5));
        }
        Clock(muted, 12345);
        Clock(audible, 12345);
        Assert.Equal(0, muted.Output);
        for (var channel = 0; channel < 3; channel++) muted.Write((byte)(0x90 | channel << 5));
        for (var clock = 0; clock < 2000; clock++)
        {
            Assert.Equal(audible.Output, muted.Output);
            muted.Tick();
            audible.Tick();
        }
    }

    internal static void Mute(Sn76489 chip)
    {
        chip.Write(0x9f);
        chip.Write(0xbf);
        chip.Write(0xdf);
        chip.Write(0xff);
    }

    internal static void Tone(Sn76489 chip, int channel, int divisor)
    {
        chip.Write((byte)(0x80 | channel << 5 | (divisor & 15)));
        chip.Write((byte)(divisor >> 4));
    }

    internal static void Clock(Sn76489 chip, int count)
    {
        for (var clock = 0; clock < count; clock++) chip.Tick();
    }
}
