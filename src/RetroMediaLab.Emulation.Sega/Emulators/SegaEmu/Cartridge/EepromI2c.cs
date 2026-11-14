using EepromChip = RetroMediaLab.Chips.Memory.Serial.I2c.EepromI2c;
namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Cartridge;

internal sealed partial class EepromI2c
{
    private readonly BackupRam sram;
    private readonly EepromChip _chip;
    private readonly CartridgeState _cart;
    private readonly CartridgeMemoryBank[] memory_map;
    private readonly CartridgeZ80Bank[] zbank_memory_map;
    private readonly Func<uint, uint> m68k_read_bus_8, jcart_read;
    private readonly Action<uint, uint> m68k_unused_8_w, m68k_unused_16_w, jcart_write;
    private readonly DatabaseEntry[] _database;
    private string _product = "";
    private ref ushort bankshift => ref _cart.hw.bankshift;
    private ref byte cart_special => ref _cart.special;
    private sealed record DatabaseEntry(string Id, uint StackPointer, ushort Checksum, Action InitializeMapper, int Type);

    public EepromI2c(BackupRam backup, CartridgeState cart, CartridgeAddressSpace addressSpace,
        Func<uint, uint> openBus8, Action<uint, uint> unused8, Action<uint, uint> unused16,
        Func<uint, uint> jcartRead, Action<uint, uint> jcartWrite)
    {
        sram = backup; _chip = new(backup.sram); _cart = cart;
        memory_map = addressSpace.Main; zbank_memory_map = addressSpace.Z80;
        m68k_read_bus_8 = openBus8; m68k_unused_8_w = unused8; m68k_unused_16_w = unused16;
        jcart_read = jcartRead; jcart_write = jcartWrite;
        _database = CreateDatabase();
    }

    private bool IsCricketProduct() => _product.Contains("T-120106", StringComparison.Ordinal) || _product.Contains("T-120146", StringComparison.Ordinal);

    public void Initialize(string product, ushort checksum)
    {
        _product = product;
        _chip.Reset();
        eeprom_i2c.sda = eeprom_i2c.scl = 1;
        eeprom_i2c.scl_in_bit = eeprom_i2c.sda_in_bit = eeprom_i2c.sda_out_bit = 0;
        for (int i = _database.Length - 1; i >= 0; i--)
        {
            DatabaseEntry entry = _database[i];
            if (!_product.Contains(entry.Id, StringComparison.Ordinal)) continue;
            if (entry.Id[0] == ' ' && unchecked(sram.end - sram.start) > 2) break;
            if ((entry.Checksum != 0 && entry.Checksum != checksum) || (entry.StackPointer != 0 && entry.StackPointer != _cart.ReadLong(0))) continue;
            if (entry.Type > -1)
            {
                SetSpecification(entry.Type);
                sram.on = 1;
                sram.type = BackupRam.EepromI2c;
            }
            entry.InitializeMapper();
            break;
        }
        if (sram.type == 0 && sram.detected != 0 && (_cart.ReadByte(0x1b2) == 0xe8 || unchecked(sram.end - sram.start) < 2))
        {
            sram.type = BackupRam.EepromI2c;
            SetSpecification(0);
            mapper_i2c_sega_init();
        }
    }

    private void SetSpecification(int type) => _chip.Configure(type);
    private byte eeprom_i2c_out() => _chip.DataOutput ? (byte)1 : (byte)0;
    private void eeprom_i2c_update() => _chip.SetLines(eeprom_i2c.scl != 0, eeprom_i2c.sda != 0);
    private sealed class PinMap { public byte sda = 1, scl = 1, scl_in_bit, sda_in_bit, sda_out_bit; }
    private readonly PinMap eeprom_i2c = new();
    public const int ContextSize = EepromChip.ContextSize + 5;
    public int SaveContext(Span<byte> state)
    {
        int size = _chip.SaveContext(state);
        state[size] = eeprom_i2c.sda; state[size+1] = eeprom_i2c.scl;
        state[size+2] = eeprom_i2c.scl_in_bit; state[size+3] = eeprom_i2c.sda_in_bit; state[size+4] = eeprom_i2c.sda_out_bit;
        return ContextSize;
    }
    public int LoadContext(ReadOnlySpan<byte> state)
    {
        int size = _chip.LoadContext(state);
        eeprom_i2c.sda = state[size]; eeprom_i2c.scl = state[size+1];
        eeprom_i2c.scl_in_bit = state[size+2]; eeprom_i2c.sda_in_bit = state[size+3]; eeprom_i2c.sda_out_bit = state[size+4];
        return ContextSize;
    }
}