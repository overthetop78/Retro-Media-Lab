using System.Buffers.Binary;

namespace RetroMediaLab.Chips.Audio.Sega.PicoAdpcm;

public sealed partial class PicoAdpcm
{
    public const int ContextSize = 192;

    public static bool CanLoadContext(ReadOnlySpan<byte> state)
    {
        if (state.Length < ContextSize || state[0] != 1) return false;
        int position = 65;
        int head = ReadInt(state, ref position), count = ReadInt(state, ref position), phase = ReadInt(state, ref position);
        int sampleRate = BinaryPrimitives.ReadInt32LittleEndian(state[129..]);
        uint clock = BinaryPrimitives.ReadUInt32LittleEndian(state[145..]);
        long tick = BinaryPrimitives.ReadInt64LittleEndian(state[137..]);
        return (uint)head < 64 && (uint)count <= 64 && phase is >= 1 and <= 3
            && BinaryPrimitives.ReadInt32LittleEndian(state[85..]) is >= 0 and <= 15
            && BinaryPrimitives.ReadInt32LittleEndian(state[89..]) is >= 0 and <= 256
            && BinaryPrimitives.ReadInt32LittleEndian(state[93..]) is >= 0 and <= 64
            && BinaryPrimitives.ReadInt32LittleEndian(state[97..]) is >= 0 and <= 64
            && BinaryPrimitives.ReadInt32LittleEndian(state[101..]) is 0 or 1
            && BinaryPrimitives.ReadInt32LittleEndian(state[113..]) is 0 or 1
            && BinaryPrimitives.ReadInt32LittleEndian(state[121..]) is >= 0 and <= 3
            && sampleRate is > 0 and <= 384000 && clock > 0 && tick >= 0
            && tick <= (long)uint.MaxValue * sampleRate + clock
            && BinaryPrimitives.ReadInt32LittleEndian(state[133..]) == (int)(((long)sampleRate << 16) / AdpcmClock);
    }

    public int SaveContext(Span<byte> state)
    {
        if (state.Length < ContextSize) throw new ArgumentException("ADPCM context is too short.", nameof(state));
        state[..ContextSize].Clear(); state[0] = 1; _fifo.CopyTo(state[1..]);
        int position = 65;
        ReadOnlySpan<int> fields = [_head,_count,_phase,_samplePosition,_sample,_decoderState,_nibbles,_silence,_rate,_high,_cache,_control,_irq?1:0,_lastOutput,_filterIndex,_gain,_sampleRate,_stepSamples];
        foreach (int value in fields) PutInt(state, ref position, value);
        BinaryPrimitives.WriteInt64LittleEndian(state[position..], _nextSampleTick); position += 8;
        PutInt(state, ref position, (int)_masterClock);
        foreach (int value in _x) PutInt(state, ref position, value);
        foreach (int value in _y) PutInt(state, ref position, value);
        return ContextSize;
    }

    public int LoadContext(ReadOnlySpan<byte> state)
    {
        if (!CanLoadContext(state)) throw new ArgumentException("Invalid ADPCM context.", nameof(state));
        state.Slice(1, 64).CopyTo(_fifo); int position = 65;
        _head=ReadInt(state,ref position);_count=ReadInt(state,ref position);_phase=ReadInt(state,ref position);
        _samplePosition=ReadInt(state,ref position);_sample=ReadInt(state,ref position);_decoderState=ReadInt(state,ref position);
        _nibbles=ReadInt(state,ref position);_silence=ReadInt(state,ref position);_rate=ReadInt(state,ref position);
        _high=ReadInt(state,ref position);_cache=ReadInt(state,ref position);_control=ReadInt(state,ref position);
        _irq=ReadInt(state,ref position)!=0;_lastOutput=ReadInt(state,ref position);_filterIndex=ReadInt(state,ref position);
        _gain=ReadInt(state,ref position);_sampleRate=ReadInt(state,ref position);_stepSamples=ReadInt(state,ref position);
        _nextSampleTick=BinaryPrimitives.ReadInt64LittleEndian(state[position..]);position+=8;
        _masterClock=(uint)ReadInt(state,ref position);
        for(int i=0;i<3;i++)_x[i]=ReadInt(state,ref position);
        for(int i=0;i<3;i++)_y[i]=ReadInt(state,ref position);
        ConfigureFilter();
        _output.AddDeltaFast(0,_lastOutput,_lastOutput);
        _interrupt(_irq);
        return ContextSize;
    }

    private static void PutInt(Span<byte> state,ref int position,int value)
    {BinaryPrimitives.WriteInt32LittleEndian(state[position..],value);position+=4;}
    private static int ReadInt(ReadOnlySpan<byte> state,ref int position)
    {int value=BinaryPrimitives.ReadInt32LittleEndian(state[position..]);position+=4;return value;}
}
