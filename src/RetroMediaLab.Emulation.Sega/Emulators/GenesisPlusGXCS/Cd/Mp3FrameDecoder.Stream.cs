using System.Buffers.Binary;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cd;

internal sealed partial class Mp3FrameDecoder
{
    public int MainDataBytes(Memory<byte> frame, int frameSize)
    {
        CPointer<byte> header = new(frame);
        CPointer<bs_t> bits = frame_bits;
        bs_init(bits, header + 4, frameSize - 4);
        if ((frame.Span[1] & 1) == 0) get_bits(bits, 16);
        if (L3_read_side_info(bits, new(scratch.gr_info), header) < 0) return -1;
        return (bits[0].limit - bits[0].pos) / 8;
    }
    public int ReadVbrTag(Memory<byte> frame, int frameSize, out uint frames, out int delay, out int padding)
    {
        frames = 0; delay = padding = 0;
        CPointer<byte> header = new(frame);
        CPointer<bs_t> bits = frame_bits;
        bs_init(bits, header + 4, frameSize - 4);
        if ((frame.Span[1] & 1) == 0) get_bits(bits, 16);
        if (L3_read_side_info(bits, new(scratch.gr_info), header) < 0) return 0;
        int offset = 4 + bits[0].pos / 8;
        if (offset > frameSize - 8) return 0;
        ReadOnlySpan<byte> data = frame.Span[..frameSize];
        if (!data.Slice(offset, 4).SequenceEqual("Xing"u8) && !data.Slice(offset, 4).SequenceEqual("Info"u8)) return 0;
        int flags = data[offset + 7];
        if ((flags & 1) == 0) return -1;
        offset += 8;
        if (offset > frameSize - 4) return 0;
        frames = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]); offset += 4;
        if ((flags & 2) != 0) offset += 4;
        if ((flags & 4) != 0) offset += 100;
        if ((flags & 8) != 0) offset += 4;
        if (offset >= frameSize) return 0;
        if (data[offset] != 0)
        {
            offset += 21;
            if (offset + 14 >= frameSize) return 0;
            delay = ((data[offset] << 4) | (data[offset + 1] >> 4)) + 529;
            padding = (((data[offset + 1] & 15) << 8) | data[offset + 2]) - 529;
        }
        return 1;
    }
}
