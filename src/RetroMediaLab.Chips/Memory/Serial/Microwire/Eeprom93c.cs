namespace RetroMediaLab.Chips.Memory.Serial.Microwire;

public sealed partial class Eeprom93c
{
    private readonly Memory<byte> _memory;
    public Eeprom93c(Memory<byte> memory) { if (memory.Length < 128) throw new ArgumentException("EEPROM memory is too short.", nameof(memory)); _memory = memory; }
    public bool Enabled { get => eeprom_93c.enabled != 0; set => eeprom_93c.enabled = value ? (byte)1 : (byte)0; }
    public byte EnableRegister { get => eeprom_93c.enabled; set => eeprom_93c.enabled = value; }
    public void Initialize() => eeprom_93c_init();
    public void Write(byte value) => eeprom_93c_write(value);
    public byte Read() => eeprom_93c_read();
    private void EraseAll() => _memory.Span.Slice(0, 128).Fill(0xff);
    public int SaveContext(Span<byte> state)
    {
        if (state.Length < ContextSize) throw new ArgumentException("EEPROM context buffer is too short.", nameof(state));
        state[..ContextSize].Clear();
        WriteT_EEPROM_93C(eeprom_93c, state);
        return ContextSize;
    }
    public int LoadContext(ReadOnlySpan<byte> state)
    {
        if (state.Length < ContextSize) throw new ArgumentException("EEPROM context buffer is too short.", nameof(state));
        ReadT_EEPROM_93C(eeprom_93c, state);
        return ContextSize;
    }
}
