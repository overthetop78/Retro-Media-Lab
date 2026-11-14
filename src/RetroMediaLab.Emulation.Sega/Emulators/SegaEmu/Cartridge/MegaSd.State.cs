using System.Buffers.Binary;
using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Cartridge;

internal sealed partial class MegaSd
{
    private sealed class T_MEGASD_HW
    {
        public byte unlock;
        public byte bank0;
        public byte special;
        public byte writeEnable;
        public byte overlayEnable;
        public byte playbackLoop;
        public byte playbackLoopTrack;
        public byte playbackEndTrack;
        public ushort result;
        public ushort fadeoutStartVolume;
        public int fadeoutSamplesTotal;
        public int fadeoutSamplesCount;
        public int playbackSamplesCount;
        public int playbackLoopSector;
        public int playbackEndSector;
        public readonly byte[] buffer = new byte[2048];
    }
    private readonly T_MEGASD_HW megasd_hw = new();
}
