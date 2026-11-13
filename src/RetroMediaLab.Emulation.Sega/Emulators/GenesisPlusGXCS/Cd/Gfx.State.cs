using System.Buffers.Binary;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cd;

internal sealed partial class Gfx
{
    private sealed class gfx_t
    {
        public uint cycles;
        public uint cyclesPerLine;
        public uint dotMask;
        public int tracePtrOffset;
        public CPointer<ushort> tracePtr { get => new CPointer<byte>(_ram).Cast<ushort>() + tracePtrOffset; set => tracePtrOffset = value - new CPointer<byte>(_ram).Cast<ushort>(); }
        public int mapPtrOffset;
        public CPointer<ushort> mapPtr { get => new CPointer<byte>(_ram).Cast<ushort>() + mapPtrOffset; set => mapPtrOffset = value - new CPointer<byte>(_ram).Cast<ushort>(); }
        public byte stampShift;
        public byte mapShift;
        public ushort bufferOffset;
        public uint bufferStart;
        public readonly ushort[] lut_offset = new ushort[32768];
        public readonly byte[] lut_prio = new byte[262144];
        public readonly byte[] lut_pixel = new byte[512];
        public readonly byte[] lut_cell = new byte[256];
        private readonly byte[] _ram;
        public gfx_t(byte[] ram) { _ram = ram; }
    }
}
