namespace RetroMediaLab.Chips.Memory.Serial.I2c;

/// <summary>Serial EEPROM protocol, with board-level address and pin decoding left to the caller.</summary>
public sealed partial class EepromI2c
{
    private readonly Memory<byte> _memory;
    public EepromI2c(Memory<byte> memory) => _memory = memory;
    public void Configure(int type)
    {
        if ((uint)type >= Specifications.Length) throw new ArgumentOutOfRangeException(nameof(type));
        var specification = Specifications[type];
        if (_memory.Length <= specification.SizeMask) throw new ArgumentException("EEPROM memory is too short for this type.");
        eeprom_i2c.spec.address_bits = specification.AddressBits;
        eeprom_i2c.spec.size_mask = specification.SizeMask;
        eeprom_i2c.spec.pagewrite_mask = specification.PageMask;
        Reset();
    }
    public void Reset()
    {
        eeprom_i2c.sda = eeprom_i2c.old_sda = eeprom_i2c.scl = eeprom_i2c.old_scl = 1;
        eeprom_i2c.cycles = eeprom_i2c.rw = eeprom_i2c.buffer = 0;
        eeprom_i2c.device_address = eeprom_i2c.word_address = 0;
        eeprom_i2c.state = 0;
    }
    public void SetLines(bool clock, bool data)
    {
        eeprom_i2c.scl = clock ? (byte)1 : (byte)0;
        eeprom_i2c.sda = data ? (byte)1 : (byte)0;
        eeprom_i2c_update();
    }
    public bool DataOutput => eeprom_i2c_out() != 0;
    public int SaveContext(Span<byte> state)
    {
        if (state.Length < ContextSize) throw new ArgumentException("I2C context is too short.", nameof(state));
        state[..ContextSize].Clear(); WriteI2cState(eeprom_i2c, state); return ContextSize;
    }
    public int LoadContext(ReadOnlySpan<byte> state)
    {
        if (state.Length < ContextSize) throw new ArgumentException("I2C context is too short.", nameof(state));
        ReadI2cState(eeprom_i2c, state); return ContextSize;
    }
}
