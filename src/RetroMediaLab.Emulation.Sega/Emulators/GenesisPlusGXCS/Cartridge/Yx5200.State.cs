using System.Buffers.Binary;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cartridge;

internal sealed partial class Yx5200
{
    private sealed class T_YX5200
    {
        public byte rxCycle;
        public byte rxCounter;
        public readonly byte[] rxBuffer = new byte[10];
        public byte playbackEnabled;
        public byte playbackLoop;
        public byte playbackVolume;
        public byte audioEnabled;
        public ushort trackIndex;
        public readonly short[] audio = new short[2];
    }
    private readonly T_YX5200 yx5200 = new();
}
