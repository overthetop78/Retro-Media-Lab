using System.Buffers.Binary;
using System.IO;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cd;

internal sealed partial class Cdd
{
    public const int ContextSize = 33;
    public int SaveContext(Span<byte> state)
    {
        if (state.Length < ContextSize) throw new ArgumentException("CDD context buffer is too short.", nameof(state));
        BinaryPrimitives.WriteUInt32LittleEndian(state, cdd.cycles);
        BinaryPrimitives.WriteUInt32LittleEndian(state[4..], cdd.latency);
        BinaryPrimitives.WriteInt32LittleEndian(state[8..], cdd.index);
        BinaryPrimitives.WriteInt32LittleEndian(state[12..], cdd.lba);
        BinaryPrimitives.WriteInt32LittleEndian(state[16..], cdd.scanOffset);
        BinaryPrimitives.WriteUInt16LittleEndian(state[20..], cdd.fader[0]);
        BinaryPrimitives.WriteUInt16LittleEndian(state[22..], cdd.fader[1]);
        state[24] = unchecked((byte)(cdd.status | cdd.pending << 4));
        uint offset = 0;
        track_t track = cdd.toc.tracks[cdd.index];
        if (track.type == 0)
        {
            if (_chd is not null) offset = _chdAudioPosition;
            else if (track.Vorbis is not null) offset = unchecked((uint)track.Vorbis.PcmPosition);
            else if (track.File is not null) offset = unchecked((uint)track.File.Position);
        }
        BinaryPrimitives.WriteUInt32LittleEndian(state[25..], offset);
        BinaryPrimitives.WriteInt16LittleEndian(state[29..], cdd.audio[0]);
        BinaryPrimitives.WriteInt16LittleEndian(state[31..], cdd.audio[1]);
        return ContextSize;
    }
    public int LoadContext(ReadOnlySpan<byte> state, string version)
    {
        bool previous = version.Length > 15 && version[11] == '1' && version[13] == '7' && version[15] == '5';
        int size = previous ? 25 : ContextSize;
        if (state.Length < size) throw new ArgumentException("CDD context buffer is too short.", nameof(state));
        bool completed = false;
        try
        {
            cdd.cycles = BinaryPrimitives.ReadUInt32LittleEndian(state);
            cdd.latency = BinaryPrimitives.ReadUInt32LittleEndian(state[4..]);
            uint index = BinaryPrimitives.ReadUInt32LittleEndian(state[8..]);
            int lba = BinaryPrimitives.ReadInt32LittleEndian(state[12..]);
            cdd.scanOffset = BinaryPrimitives.ReadInt32LittleEndian(state[16..]);
            cdd.fader[0] = BinaryPrimitives.ReadUInt16LittleEndian(state[20..]);
            cdd.fader[1] = BinaryPrimitives.ReadUInt16LittleEndian(state[22..]);
            cdd.status = (byte)(state[24] & 15); cdd.pending = (byte)(state[24] >> 4);
            if (index > cdd.toc.last || lba < -150 || lba > cdd.toc.end)
            {
                completed = true;
                return 0;
            }
            cdd.lba = lba;
            track_t track = cdd.toc.tracks[index];
            if (previous)
            {
                if (track.type == 0)
                {
                    if (lba < track.start) lba = track.start;
                    SeekAudio((int)index, lba);
                }
            }
            else
            {
                uint offset = BinaryPrimitives.ReadUInt32LittleEndian(state[25..]);
                cdd.audio[0] = BinaryPrimitives.ReadInt16LittleEndian(state[29..]);
                cdd.audio[1] = BinaryPrimitives.ReadInt16LittleEndian(state[31..]);
                if (track.type == 0)
                {
                    if (_chd is not null) _chdAudioPosition = offset & 0xfffffffc;
                    else if (track.Vorbis is not null) track.Vorbis.SeekPcm(offset);
                    else if (track.File is not null) SeekStream(track.File, offset);
                }
            }
            SeekSubcode(lba * 96);
            cdd.index = (int)index;
            completed = true;
            return size;
        }
        finally
        {
            if (!completed) Unload();
        }
    }
}
