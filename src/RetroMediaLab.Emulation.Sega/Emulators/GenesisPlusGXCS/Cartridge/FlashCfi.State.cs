using System.Buffers.Binary;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cartridge;

internal sealed partial class FlashCfi
{
    private sealed class T_FLASH_CFI
    {
        public uint addr;
        public uint data;
        public int mode;
        public int Type;
        public CPointer<byte> cfi_data => new(cfi_query, Type * 0x41);
        public CPointer<ushort> id => new(flash_id, Type * 16);
        public readonly ushort[] otp_area = new ushort[128];
        public byte readmask;
    }
    private readonly T_FLASH_CFI flash = new();
}
