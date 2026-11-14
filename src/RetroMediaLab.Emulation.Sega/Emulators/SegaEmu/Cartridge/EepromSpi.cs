using EepromChip = RetroMediaLab.Chips.Memory.Serial.Spi.EepromSpi;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Cartridge;

internal sealed class EepromSpi
{
    private readonly BackupRam _backup;
    private readonly EepromChip _chip;
    public const int ContextSize = EepromChip.ContextSize;
    public EepromSpi(BackupRam backup) { _backup = backup; _chip = new(backup.sram); }
    public void Initialize() { _chip.Initialize(); _backup.type = BackupRam.EepromSpi; _backup.on = 1; }
    public void Write(byte value) => _chip.Write(value);
    public uint Read(uint address) => _chip.Read(address);
    public int SaveContext(Span<byte> state) => _chip.SaveContext(state);
    public int LoadContext(ReadOnlySpan<byte> state) => _chip.LoadContext(state);
}
