using System.Buffers.Binary;
using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Cartridge;

internal sealed partial class MegaSd
{
    private const int NativeContextSize = 2080;
    private static void WriteT_MEGASD_HW(T_MEGASD_HW value, Span<byte> state)
    {
        state[0] = unchecked((byte)value.unlock);
        state[1] = unchecked((byte)value.bank0);
        state[2] = unchecked((byte)value.special);
        state[3] = unchecked((byte)value.writeEnable);
        state[4] = unchecked((byte)value.overlayEnable);
        state[5] = unchecked((byte)value.playbackLoop);
        state[6] = unchecked((byte)value.playbackLoopTrack);
        state[7] = unchecked((byte)value.playbackEndTrack);
        BinaryPrimitives.WriteUInt16LittleEndian(state.Slice(8), value.result);
        BinaryPrimitives.WriteUInt16LittleEndian(state.Slice(10), value.fadeoutStartVolume);
        BinaryPrimitives.WriteInt32LittleEndian(state.Slice(12), value.fadeoutSamplesTotal);
        BinaryPrimitives.WriteInt32LittleEndian(state.Slice(16), value.fadeoutSamplesCount);
        BinaryPrimitives.WriteInt32LittleEndian(state.Slice(20), value.playbackSamplesCount);
        BinaryPrimitives.WriteInt32LittleEndian(state.Slice(24), value.playbackLoopSector);
        BinaryPrimitives.WriteInt32LittleEndian(state.Slice(28), value.playbackEndSector);
        for (int i=0;i<2048;i++) {
        state[32+i*1] = unchecked((byte)value.buffer[i]);
        }
    }
    private static void ReadT_MEGASD_HW(T_MEGASD_HW value, ReadOnlySpan<byte> state)
    {
        value.unlock = unchecked((byte)state[0]);
        value.bank0 = unchecked((byte)state[1]);
        value.special = unchecked((byte)state[2]);
        value.writeEnable = unchecked((byte)state[3]);
        value.overlayEnable = unchecked((byte)state[4]);
        value.playbackLoop = unchecked((byte)state[5]);
        value.playbackLoopTrack = unchecked((byte)state[6]);
        value.playbackEndTrack = unchecked((byte)state[7]);
        value.result = BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(8));
        value.fadeoutStartVolume = BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(10));
        value.fadeoutSamplesTotal = BinaryPrimitives.ReadInt32LittleEndian(state.Slice(12));
        value.fadeoutSamplesCount = BinaryPrimitives.ReadInt32LittleEndian(state.Slice(16));
        value.playbackSamplesCount = BinaryPrimitives.ReadInt32LittleEndian(state.Slice(20));
        value.playbackLoopSector = BinaryPrimitives.ReadInt32LittleEndian(state.Slice(24));
        value.playbackEndSector = BinaryPrimitives.ReadInt32LittleEndian(state.Slice(28));
        for (int i=0;i<2048;i++) {
        value.buffer[i] = unchecked((byte)state[32+i*1]);
        }
    }
}
