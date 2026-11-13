using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cartridge;

internal sealed partial class BackupRam
{
    private BackupRam backup => this;
    private CartridgeState cart = null!;
    private RomInformation rominfo = null!;
    private static int strstr(string text, string value) => text.Contains(value, StringComparison.Ordinal) ? 1 : 0;
    private void FillRam(int value) => sram.AsSpan().Fill(unchecked((byte)value));
    private static uint crc32(uint seed, CPointer<byte> data, uint length) => Checksums.Crc32(seed, data.Bytes(checked((int)length)));
    public void Initialize(CartridgeState cartridge, RomInformation information)
    {
        cart = cartridge; rominfo = information;
        sram_init();
    }
    public uint ReadByte(uint address) => sram_read_byte(address);
    public uint ReadWord(uint address) => sram_read_word(address);
    public void WriteByte(uint address, uint value) => sram_write_byte(address, value);
    public void WriteWord(uint address, uint value) => sram_write_word(address, value);
}
