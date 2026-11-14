using EepromChip = RetroMediaLab.Chips.Memory.Serial.Microwire.Eeprom93c;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Cartridge;

internal sealed class Eeprom93c
{
    private readonly BackupRam _backup;
    private readonly EepromChip _chip;
    public const int ContextSize = EepromChip.ContextSize;
    public Eeprom93c(BackupRam backup) { _backup = backup; _chip = new(backup.sram); }
    public bool Enabled { get => _chip.Enabled; set => _chip.Enabled = value; }
    internal byte EnableRegister { get => _chip.EnableRegister; set => _chip.EnableRegister = value; }
    public void Initialize() { _chip.Initialize(); _backup.type = BackupRam.EepromMicrowire; }
    public void Write(byte value) => _chip.Write(value);
    public byte Read() => _chip.Read();
    public int SaveContext(Span<byte> state) => _chip.SaveContext(state);
    public int LoadContext(ReadOnlySpan<byte> state) => _chip.LoadContext(state);
}
