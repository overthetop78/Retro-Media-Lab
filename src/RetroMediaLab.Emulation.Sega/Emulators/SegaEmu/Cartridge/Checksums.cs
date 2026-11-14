namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Cartridge;

internal static class Checksums
{
    private static readonly uint[] Crc32Table = CreateTable();
    private static uint[] CreateTable()
    {
        uint[] table = new uint[256];
        for (uint i = 0; i < table.Length; i++)
        {
            uint value = i;
            for (int bit = 0; bit < 8; bit++) value = (value >> 1) ^ ((value & 1) != 0 ? 0xedb88320u : 0u);
            table[i] = value;
        }
        return table;
    }
    public static uint Crc32(uint previous, ReadOnlySpan<byte> data)
    {
        uint value = previous ^ 0xffffffff;
        foreach (byte b in data) value = Crc32Table[(value ^ b) & 255] ^ (value >> 8);
        return value ^ 0xffffffff;
    }
}
