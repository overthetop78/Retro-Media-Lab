using System.Buffers.Binary;
using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Cartridge;

internal sealed partial class SmsCartridge
{
    private sealed class rominfo_t
    {
        public uint crc;
        public byte g_3d;
        public byte fm;
        public byte peripheral;
        public byte mapper;
        public byte system;
        public byte region;
    }
    private sealed class romhw_t
    {
        public readonly byte[] fcr = new byte[4];
        public byte mapper;
        public ushort pages;
    }
    private sealed class SmsSlot
    {
        private Memory<byte> _rom;
        public ArrayPointer<byte> rom { get => new(_rom); set => _rom = value.Memory; }
        private Memory<byte> _fcr;
        public ArrayPointer<byte> fcr { get => new(_fcr); set => _fcr = value.Memory; }
        public byte mapper;
        public ushort pages;
    }
    private readonly romhw_t cart_rom = new(), bios_rom = new();
    private readonly SmsSlot slot = new();
}
