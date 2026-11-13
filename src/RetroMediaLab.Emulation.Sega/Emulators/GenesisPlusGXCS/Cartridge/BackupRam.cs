namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cartridge;

internal sealed partial class BackupRam
{
    public const byte DefaultSram = 0, EepromI2c = 1, EepromSpi = 2, EepromMicrowire = 3, FlashMemory = 4;
    internal byte detected, on, type;
    internal uint start, end, crc;
    internal readonly byte[] sram = new byte[0x10000];
    public Span<byte> Data => sram;
    public bool Enabled { get => on != 0; set => on = value ? (byte)1 : (byte)0; }
    public byte Type => type;
    public uint Start => start;
    public uint End => end;
    public uint Checksum { get => crc; set => crc = value; }
}
