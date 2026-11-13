using System.Buffers.Binary;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cd;

internal sealed partial class Pcm
{
    private sealed class chan_t
    {
        public uint addr;
        public uint st;
        public Register16 ls;
        public Register16 fd;
        public byte env;
        public byte pan;
    }
    private sealed class pcm_t
    {
        public readonly chan_t[] chan = new chan_t[8];
        public readonly short[] sampleOutput = new short[2];
        public int BankOffset;
        public CPointer<byte> bank { get => new(ram, BankOffset); set => BankOffset = value - new CPointer<byte>(ram); }
        public byte enabled;
        public byte status;
        public byte index;
        public readonly byte[] ram = new byte[65536];
        public uint cycles;
        public pcm_t()
        {
            for (int i=0;i<8;i++) chan[i] = new();
        }
    }
    private readonly pcm_t pcm = new();
}
