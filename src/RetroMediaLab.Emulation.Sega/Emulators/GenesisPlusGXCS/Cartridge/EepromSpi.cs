namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cartridge;

internal sealed partial class EepromSpi
{
    private readonly BackupRam sram;
    public EepromSpi(BackupRam backup) { sram = backup; }
    public void Initialize() => eeprom_spi_init();
    public void Write(byte value) => eeprom_spi_write(value);
    public uint Read(uint address) => eeprom_spi_read(address);
    public int SaveContext(Span<byte> state)
    {
        if (state.Length < ContextSize) throw new ArgumentException("EEPROM context buffer is too short.", nameof(state));
        state[..ContextSize].Clear();
        WriteT_EEPROM_SPI(spi_eeprom, state);
        return ContextSize;
    }
    public int LoadContext(ReadOnlySpan<byte> state)
    {
        if (state.Length < ContextSize) throw new ArgumentException("EEPROM context buffer is too short.", nameof(state));
        ReadT_EEPROM_SPI(spi_eeprom, state);
        return ContextSize;
    }
}
