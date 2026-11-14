using RetroMediaLab.Chips.Audio.Common.Resampling;
using RetroMediaLab.Chips.Audio.TexasInstruments.SN76489.Instruction;
using RetroMediaLab.Chips.Audio.Yamaha.YM2612.Instruction;

namespace RetroMediaLab.Chips.Tests.SegaEmu;

public sealed class AudioMixingTests
{
    [Fact]
    public void PsgMixingCanMuteOneChannelWithoutChangingItsRegisters()
    {
        short[] audible = PsgTone(100), muted = PsgTone(0);
        Assert.Contains(audible, sample => sample != 0);
        Assert.All(muted, sample => Assert.Equal(0, sample));
    }

    private static short[] PsgTone(int volume)
    {
        BandLimitedBuffer output = new(1024);
        output.SetRates(53693175, 44100);
        Sn76489 psg = new(output, Sn76489Type.Integrated);
        psg.Reset(); psg.ChannelVolumes[0] = volume;
        psg.Configure(0,100,0xff);
        psg.Write(0,0x84); psg.Write(0,0x10); psg.Write(0,0x90);
        psg.EndFrame(896040); output.EndFrame(896040);
        short[] samples = new short[output.SamplesAvailable * 2];
        output.ReadSamples(samples,samples.Length / 2);
        return samples;
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    [InlineData(3)] [InlineData(4)] [InlineData(5)]
    public void Ym2612MixingControlsEachProgrammedFmChannel(int channel)
    {
        Assert.Contains(FmTone(channel,100), sample => sample != 0);
        Assert.All(FmTone(channel,0), sample => Assert.Equal(0,sample));
    }

    private static int[] FmTone(int channel, int volume)
    {
        Ym2612 fm = new(); fm.Configure(Ym2612.Integrated); fm.Reset();
        fm.ChannelVolumes.Fill(0); fm.ChannelVolumes[channel] = volume;
        uint bank = channel < 3 ? 0u : 2u, local = (uint)channel % 3;
        void Write(uint address, uint value) { fm.Write(bank,address); fm.Write(bank + 1,value); }
        for (uint slot = 0; slot < 4; slot++)
        {
            uint offset = slot * 4 + local;
            Write(0x30 + offset,1); Write(0x40 + offset,0); Write(0x50 + offset,31);
            Write(0x60 + offset,0); Write(0x70 + offset,0); Write(0x80 + offset,0x0f);
        }
        Write(0xb0 + local,7); Write(0xb4 + local,0xc0);
        Write(0xa4 + local,0x22); Write(0xa0 + local,0x69);
        fm.Write(0,0x28); fm.Write(1,0xf0u | (uint)(channel < 3 ? channel : channel + 1));
        int[] stereo = new int[2048]; fm.Update(stereo,stereo.Length / 2);
        return stereo;
    }
}
