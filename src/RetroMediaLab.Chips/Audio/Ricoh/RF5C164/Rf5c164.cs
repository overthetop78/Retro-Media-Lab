using RetroMediaLab.Chips.Common.Memory;

using System.Buffers.Binary;
using RetroMediaLab.Chips.Audio.Common.Resampling;

namespace RetroMediaLab.Chips.Audio.Ricoh.RF5C164;

public sealed partial class Rf5c164
{
    private const uint ClockRatio = 384 * 4;
    public const int ContextSize = ChannelContextSize * 8 + 4 + 4 + 0x10000;
    private readonly BandLimitedBuffer _output;
    private int pcm_volume = 100;
    public int Volume { get => pcm_volume; set => pcm_volume = value; }
    public Memory<byte> WaveRam => pcm.ram;
    public Rf5c164(BandLimitedBuffer output) { _output = output; }
    public void Initialize(double clock, int sampleRate) => _output.SetRates(clock / ClockRatio, sampleRate);
    public void Reset()
    {
        foreach (chan_t channel in pcm.chan)
        {
            channel.addr = channel.st = 0;
            channel.ls = channel.fd = default;
            channel.env = 0;
            channel.pan = 0xff;
        }
        pcm.sampleOutput.AsSpan().Clear();
        pcm.ram.AsSpan().Clear();
        pcm.enabled = pcm.status = pcm.index = 0;
        pcm.BankOffset = 0;
        pcm.cycles = 0;
        _output.Clear();
    }
    public void Update(uint samples) => pcm_update(samples);
    public void Write(uint address, byte data, uint cycles) => pcm_write(address, data, cycles);
    public byte Read(uint address, uint cycles) => pcm_read(address, cycles);
    private void AddDeltaFast(uint time, int left, int right) => _output.AddDeltaFast(time, left, right);
    private void EndFrame(uint clocks) => _output.EndFrame(clocks);
    private uint ClocksNeeded(uint samples) => unchecked((uint)_output.ClocksNeeded((int)samples));

    public void DmaWrite(uint length, ref Register16 sourceAddress, ref Register16 destinationAddress, ReadOnlySpan<byte> cdcRam)
    {
        ushort source = (ushort)(sourceAddress.Word & 0x3fff);
        ushort destination = (ushort)((destinationAddress.Word << 2) & 0xfff);
        destinationAddress.Word = unchecked((ushort)(destinationAddress.Word + (length >> 2)));
        sourceAddress.Word = unchecked((ushort)(sourceAddress.Word + length));
        while (length-- != 0)
        {
            pcm.ram[pcm.BankOffset + destination] = cdcRam[source];
            source = (ushort)((source + 1) & 0x3fff);
            destination = (ushort)((destination + 1) & 0xfff);
        }
    }

    public int SaveContext(Span<byte> state)
    {
        if (state.Length < ContextSize) throw new ArgumentException("PCM context buffer is too short.", nameof(state));
        int offset = 0;
        foreach (chan_t channel in pcm.chan)
        {
            state.Slice(offset, ChannelContextSize).Clear();
            Writechan_t(channel, state[offset..]);
            offset += ChannelContextSize;
        }
        BinaryPrimitives.WriteInt16LittleEndian(state[offset..], pcm.sampleOutput[0]); offset += 2;
        BinaryPrimitives.WriteInt16LittleEndian(state[offset..], pcm.sampleOutput[1]); offset += 2;
        state[offset++] = (byte)(pcm.BankOffset >> 12);
        state[offset++] = pcm.enabled;
        state[offset++] = pcm.status;
        state[offset++] = pcm.index;
        pcm.ram.CopyTo(state[offset..]);
        return offset + pcm.ram.Length;
    }
    public int LoadContext(ReadOnlySpan<byte> state)
    {
        if (state.Length < ContextSize) throw new ArgumentException("PCM context buffer is too short.", nameof(state));
        int offset = 0;
        foreach (chan_t channel in pcm.chan)
        {
            Readchan_t(channel, state[offset..]);
            offset += ChannelContextSize;
        }
        pcm.sampleOutput[0] = BinaryPrimitives.ReadInt16LittleEndian(state[offset..]); offset += 2;
        pcm.sampleOutput[1] = BinaryPrimitives.ReadInt16LittleEndian(state[offset..]); offset += 2;
        pcm.BankOffset = (state[offset++] & 15) << 12;
        pcm.enabled = state[offset++];
        pcm.status = state[offset++];
        pcm.index = (byte)(state[offset++] & 7);
        state.Slice(offset, pcm.ram.Length).CopyTo(pcm.ram);
        return offset + pcm.ram.Length;
    }
}
