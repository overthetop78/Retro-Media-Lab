using System.Buffers.Binary;

namespace RetroMediaLab.Chips.Controllers.Sega.CD;

public sealed partial class Cdd
{
    public const int ContextSize = 41;
    public int SaveContext(Span<byte> state)
    {
        if (state.Length < ContextSize) throw new ArgumentException("CDD context is too short.", nameof(state));
        BinaryPrimitives.WriteUInt32LittleEndian(state, cdd.cycles);
        BinaryPrimitives.WriteUInt32LittleEndian(state[4..], cdd.latency);
        BinaryPrimitives.WriteInt32LittleEndian(state[8..], cdd.index);
        BinaryPrimitives.WriteInt32LittleEndian(state[12..], cdd.lba);
        BinaryPrimitives.WriteInt32LittleEndian(state[16..], cdd.scanOffset);
        BinaryPrimitives.WriteUInt16LittleEndian(state[20..], cdd.fader[0]);
        BinaryPrimitives.WriteUInt16LittleEndian(state[22..], cdd.fader[1]);
        state[24] = (byte)(cdd.status | cdd.pending << 4);
        BinaryPrimitives.WriteInt64LittleEndian(state[25..], _audioSamplePosition);
        BinaryPrimitives.WriteInt16LittleEndian(state[33..], cdd.audio[0]);
        BinaryPrimitives.WriteInt16LittleEndian(state[35..], cdd.audio[1]);
        BinaryPrimitives.WriteInt32LittleEndian(state[37..], _subchannelLba);
        return ContextSize;
    }
    public int LoadContext(ReadOnlySpan<byte> state, string version = "")
    {
        if (state.Length < ContextSize) throw new ArgumentException("CDD context is too short.", nameof(state));
        int index = BinaryPrimitives.ReadInt32LittleEndian(state[8..]);
        int lba = BinaryPrimitives.ReadInt32LittleEndian(state[12..]);
        if (index < 0 || index > cdd.toc.last || lba < -150 || lba > cdd.toc.end) return 0;
        cdd.cycles = BinaryPrimitives.ReadUInt32LittleEndian(state);
        cdd.latency = BinaryPrimitives.ReadUInt32LittleEndian(state[4..]);
        cdd.index = index; cdd.lba = lba;
        cdd.scanOffset = BinaryPrimitives.ReadInt32LittleEndian(state[16..]);
        cdd.fader[0] = BinaryPrimitives.ReadUInt16LittleEndian(state[20..]);
        cdd.fader[1] = BinaryPrimitives.ReadUInt16LittleEndian(state[22..]);
        cdd.status = (byte)(state[24] & 15); cdd.pending = (byte)(state[24] >> 4);
        _audioSamplePosition = BinaryPrimitives.ReadInt64LittleEndian(state[25..]);
        cdd.audio[0] = BinaryPrimitives.ReadInt16LittleEndian(state[33..]);
        cdd.audio[1] = BinaryPrimitives.ReadInt16LittleEndian(state[35..]);
        _subchannelLba = BinaryPrimitives.ReadInt32LittleEndian(state[37..]);
        return ContextSize;
    }
}
