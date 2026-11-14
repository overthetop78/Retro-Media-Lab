using System.Buffers.Binary;
using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Chips.Memory.Flash.Cfi;

public sealed partial class FlashCfi
{
    private sealed class T_FLASH_CFI
    {
        public uint addr;
        public uint data;
        public int mode;
        public int Type;
        public ArrayPointer<byte> cfi_data => new(cfi_query, Type * 0x41);
        public ArrayPointer<ushort> id => new(flash_id, Type * 16);
        public readonly ushort[] otp_area = new ushort[128];
        public byte readmask;
    }
    private readonly T_FLASH_CFI flash = new();
}
