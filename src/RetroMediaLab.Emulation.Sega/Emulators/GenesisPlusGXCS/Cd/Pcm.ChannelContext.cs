using System.Buffers.Binary;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cd;

internal sealed partial class Pcm
{
    private const int ChannelContextSize = 16;
    private static void Writechan_t(chan_t value, Span<byte> state)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(0), value.addr);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(4), value.st);
        BinaryPrimitives.WriteUInt16LittleEndian(state.Slice(8), value.ls);
        BinaryPrimitives.WriteUInt16LittleEndian(state.Slice(10), value.fd);
        state[12] = unchecked((byte)value.env);
        state[13] = unchecked((byte)value.pan);
    }
    private static void Readchan_t(chan_t value, ReadOnlySpan<byte> state)
    {
        value.addr = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(0));
        value.st = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(4));
        value.ls = BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(8));
        value.fd = BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(10));
        value.env = unchecked((byte)state[12]);
        value.pan = unchecked((byte)state[13]);
    }
}
