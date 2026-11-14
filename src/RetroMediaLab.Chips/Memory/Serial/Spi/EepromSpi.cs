namespace RetroMediaLab.Chips.Memory.Serial.Spi;

public sealed partial class EepromSpi
{
    private readonly Memory<byte> _memory;
    public EepromSpi(Memory<byte> memory) { if (memory.Length < 65536) throw new ArgumentException("EEPROM memory is too short.", nameof(memory)); _memory = memory; }
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
