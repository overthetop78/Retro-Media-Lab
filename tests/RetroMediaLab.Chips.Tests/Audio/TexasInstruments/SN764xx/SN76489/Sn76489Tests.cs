using RetroMediaLab.Chips.Audio.TexasInstruments.SN764xx.SN76489.Services;

namespace RetroMediaLab.Chips.Tests.Audio.TexasInstruments.SN764xx.SN76489;
public sealed class Sn76489Tests
{
    [Fact]
    public void ChipUsesThreeIndependentTonesAndTwoDecibelAttenuation()
    {
        for (var channel = 0; channel < 3; channel++)
        {
            var chip = new Sn76489();
            Mute(chip.Write);
            chip.Write((byte)(0x80 | channel << 5 | 10));
            chip.Write(0);
            chip.Write((byte)(0x90 | channel << 5));
            for (var tick = 0; tick < 16; tick++)
                chip.Tick();
            var full = chip.Output;
            Assert.True(full > 0);
            chip.Write((byte)(0x91 | channel << 5));
            Assert.InRange((double)chip.Output / full, 0.794, 0.795);
            chip.Write(15); // Un octet sans bit de sélection modifie le volume déjà sélectionné.
            Assert.Equal(0, chip.Output);
        }
    }

    [Fact]
    public void NoiseIsDeterministicAndWhiteNoiseDiffersFromPeriodicNoise()
    {
        int[] Sequence(byte mode)
        {
            var chip = new Sn76489();
            Mute(chip.Write);
            chip.Write(mode);
            chip.Write(0xf0);
            var values = new int[96];
            for (var index = 0; index < values.Length; index++)
            {
                for (var tick = 0; tick < (index == 0 ? 16 : 512); tick++)
                    chip.Tick();
                values[index] = chip.Output;
            }

            return values;
        }

        var periodic = Sequence(0xe0);
        var white = Sequence(0xe4);
        Assert.Equal(periodic, Sequence(0xe0));
        Assert.Equal(white, Sequence(0xe4));
        Assert.Contains(periodic, value => value != 0);
        Assert.Contains(white, value => value != 0);
        Assert.False(periodic.SequenceEqual(white));
        for (var index = 16; index < periodic.Length; index++)
            Assert.Equal(periodic[index - 15], periodic[index]);
    }

    /// <summary>Atténuation 15 : silence matériel de chaque voie, indépendamment du volume de l'application.</summary>
    private static void Mute(Action<byte> write)
    {
        write(0x9f);
        write(0xbf);
        write(0xdf);
        write(0xff);
    }
}
