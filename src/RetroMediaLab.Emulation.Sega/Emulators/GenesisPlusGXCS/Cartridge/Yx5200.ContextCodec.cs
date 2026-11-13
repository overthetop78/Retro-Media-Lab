using System.Buffers.Binary;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cartridge;

internal sealed partial class Yx5200
{
    private const int NativeContextSize = 22;
    private static void WriteT_YX5200(T_YX5200 value, Span<byte> state)
    {
        state[0] = unchecked((byte)value.rxCycle);
        state[1] = unchecked((byte)value.rxCounter);
        for (int i=0;i<10;i++) {
        state[2+i*1] = unchecked((byte)value.rxBuffer[i]);
        }
        state[12] = unchecked((byte)value.playbackEnabled);
        state[13] = unchecked((byte)value.playbackLoop);
        state[14] = unchecked((byte)value.playbackVolume);
        state[15] = unchecked((byte)value.audioEnabled);
        BinaryPrimitives.WriteUInt16LittleEndian(state.Slice(16), value.trackIndex);
        for (int i=0;i<2;i++) {
        BinaryPrimitives.WriteInt16LittleEndian(state.Slice(18+i*2), value.audio[i]);
        }
    }
    private static void ReadT_YX5200(T_YX5200 value, ReadOnlySpan<byte> state)
    {
        value.rxCycle = unchecked((byte)state[0]);
        value.rxCounter = unchecked((byte)state[1]);
        for (int i=0;i<10;i++) {
        value.rxBuffer[i] = unchecked((byte)state[2+i*1]);
        }
        value.playbackEnabled = unchecked((byte)state[12]);
        value.playbackLoop = unchecked((byte)state[13]);
        value.playbackVolume = unchecked((byte)state[14]);
        value.audioEnabled = unchecked((byte)state[15]);
        value.trackIndex = BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(16));
        for (int i=0;i<2;i++) {
        value.audio[i] = BinaryPrimitives.ReadInt16LittleEndian(state.Slice(18+i*2));
        }
    }
}
