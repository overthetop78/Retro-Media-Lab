namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cartridge;

internal sealed partial class EepromI2c
{
    private readonly BackupRam sram;
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
        sram = backup; _cart = cart;
        memory_map = addressSpace.Main; zbank_memory_map = addressSpace.Z80;
        m68k_read_bus_8 = openBus8; m68k_unused_8_w = unused8; m68k_unused_16_w = unused16;
        jcart_read = jcartRead; jcart_write = jcartWrite;
        _database = CreateDatabase();
    }

    private bool IsCricketProduct() => _product.Contains("T-120106", StringComparison.Ordinal) || _product.Contains("T-120146", StringComparison.Ordinal);

    public void Initialize(string product, ushort checksum)
    {
        _product = product;
        eeprom_i2c.sda = eeprom_i2c.old_sda = 1;
        eeprom_i2c.scl = eeprom_i2c.old_scl = 1;
        eeprom_i2c.cycles = eeprom_i2c.rw = eeprom_i2c.buffer = 0;
        eeprom_i2c.device_address = eeprom_i2c.word_address = 0;
        eeprom_i2c.state = 0;
        eeprom_i2c.spec.address_bits = 0;
        eeprom_i2c.spec.size_mask = eeprom_i2c.spec.pagewrite_mask = 0;
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

    private void SetSpecification(int type)
    {
        var spec = Specifications[type];
        eeprom_i2c.spec.address_bits = spec.AddressBits;
        eeprom_i2c.spec.size_mask = spec.SizeMask;
        eeprom_i2c.spec.pagewrite_mask = spec.PageMask;
    }

    public int SaveContext(Span<byte> state)
    {
        if (state.Length < ContextSize) throw new ArgumentException("I2C context buffer is too short.", nameof(state));
        state[..ContextSize].Clear();
        WriteI2cState(eeprom_i2c, state);
        return ContextSize;
    }
    public int LoadContext(ReadOnlySpan<byte> state)
    {
        if (state.Length < ContextSize) throw new ArgumentException("I2C context buffer is too short.", nameof(state));
        ReadI2cState(eeprom_i2c, state);
        return ContextSize;
    }
}
