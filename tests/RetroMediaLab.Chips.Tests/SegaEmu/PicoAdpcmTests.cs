using RetroMediaLab.Chips.Audio.Common.Resampling;
using Decoder = RetroMediaLab.Chips.Audio.Sega.PicoAdpcm.PicoAdpcm;

namespace RetroMediaLab.Chips.Tests.SegaEmu;

public sealed class PicoAdpcmTests
{
    private static BandLimitedBuffer Output()
    {
        BandLimitedBuffer output = new(4096); output.SetRates(53693175, 44100); return output;
    }
    private static void Stream(Decoder chip)
    {
        chip.WriteData(0x9f03,0); // Variable block: four nibbles at divisor 32.
        chip.WriteData(0x1234,0);
        chip.WriteData(0,0);
    }
    [Fact]
    public void SlaveStreamDecodesKnownNibblesAndSignalsFifoRefill()
    {
        var output=Output();bool irq=false;Decoder chip=new(output,value=>irq=value);
        chip.Configure(53693175,44100);chip.Reset();chip.WriteControl(0x4000,0);Stream(chip);
        chip.EndFrame(896040);output.EndFrame(896040);
        short[] samples=new short[output.SamplesAvailable*2];output.ReadSamples(samples,samples.Length/2);
        Assert.Equal(6,chip.DecodedSample);
        Assert.Contains(samples,value=>value!=0);Assert.True(irq);
        Assert.Equal(0,chip.FifoCount);Assert.Equal(63,chip.FreeBytes);
        Assert.Equal(0x8000,chip.Status&0x8000);
        chip.AcknowledgeInterrupt();Assert.False(irq);
    }
    [Fact]
    public void MidPacketSnapshotResumesTheDecoderAndItsClock()
    {
        Decoder a=new(Output(),_=>{}),b=new(Output(),_=>{});
        a.Configure(53693175,44100);b.Configure(53693175,44100);a.Reset();b.Reset();
        Stream(a);a.Update(10000);
        byte[] state=new byte[Decoder.ContextSize];a.SaveContext(state);b.LoadContext(state);
        a.Update(500000);b.Update(500000);
        byte[] expected=new byte[Decoder.ContextSize],actual=new byte[Decoder.ContextSize];
        a.SaveContext(expected);b.SaveContext(actual);
        Assert.Equal(expected,actual);Assert.Equal(6,b.DecodedSample);
    }
    [Fact]
    public void FifoCapacityAndMalformedContextAreBounded()
    {
        Decoder chip=new(Output(),_=>{});chip.Configure(53693175,44100);chip.Reset();
        for(int i=0;i<40;i++)chip.WriteData(0x1234,0);
        Assert.Equal(64,chip.FifoCount);Assert.Equal(0,chip.FreeBytes);
        byte[] state=new byte[Decoder.ContextSize];chip.SaveContext(state);
        state[65]=255;Assert.False(Decoder.CanLoadContext(state));
        Assert.Throws<ArgumentException>(()=>chip.LoadContext(state));
        Assert.Equal(64,chip.FifoCount);
        chip.SaveContext(state);
        System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(state.AsSpan(137),-1);
        Assert.False(Decoder.CanLoadContext(state));
        chip.SaveContext(state);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(state.AsSpan(145),0);
        Assert.False(Decoder.CanLoadContext(state));
    }
}
