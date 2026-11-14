using RetroMediaLab.Chips.Common.Memory;

using System.Buffers.Binary;


namespace RetroMediaLab.Chips.Controllers.Sega.CD;

public sealed partial class Gfx
{
    private sealed class gfx_t
    {
        public uint cycles;
        public uint cyclesPerLine;
        public uint dotMask;
        public int tracePtrOffset;
        public ArrayPointer<ushort> tracePtr { get => new ArrayPointer<byte>(_ram).Cast<ushort>() + tracePtrOffset; set => tracePtrOffset = value - new ArrayPointer<byte>(_ram).Cast<ushort>(); }
        public int mapPtrOffset;
        public ArrayPointer<ushort> mapPtr { get => new ArrayPointer<byte>(_ram).Cast<ushort>() + mapPtrOffset; set => mapPtrOffset = value - new ArrayPointer<byte>(_ram).Cast<ushort>(); }
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
