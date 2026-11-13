using System.Buffers.Binary;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cd;

internal sealed partial class Cdd
{
    private sealed partial class track_t
    {
        public int offset;
        public int start;
        public int end;
        public int type;
        public int loopEnabled;
        public int loopOffset;
    }
    private sealed partial class toc_t
    {
        public int end;
        public int last;
        public readonly track_t[] tracks = new track_t[100];
        public toc_t()
        {
            for (int i=0;i<100;i++) tracks[i] = new();
        }
    }
    private sealed partial class cdd_t
    {
        public uint cycles;
        public uint latency;
        public int loaded;
        public int index;
        public int lba;
        public int scanOffset;
        public readonly ushort[] fader = new ushort[2];
        public byte status;
        public byte pending;
        public ushort sectorSize;
        public readonly toc_t toc = new();
        public readonly short[] audio = new short[2];
    }
    private readonly cdd_t cdd = new();
}
