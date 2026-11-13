using System.Runtime.InteropServices;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Input;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cartridge;

internal interface IMdCartridgeBus
{
    byte Hardware { get; }
    int DiscLoaded { get; }
    int SampleRate { get; }
    bool CdAudioAllocated { get; }
    Memory<byte> BootRom { get; }
    uint ReadOpenBus8(uint address);
    uint ReadOpenBus16(uint address);
    void WriteUnused8(uint address, uint data);
    void WriteUnused16(uint address, uint data);
    uint ReadUnusedZ80Bank(uint address);
    void WriteUnusedZ80Bank(uint address, uint data);
    void ResetMainCpu();
    void WriteControlIoByte(uint address, uint data);
    int LoadLockOnRom(int kind, Span<byte> destination);
    void EnsureCdAudio();
    void EnsureCartridgeAudio();
    void InitializeYx5200(int sampleRate);
    void ResetYx5200();
    void WriteYx5200(uint data);
    int Yx5200ContextSize { get; }
    int SaveYx5200Context(Span<byte> state);
    int LoadYx5200Context(Span<byte> state);
    void ResetMegaSd();
    void WriteMegaSdRomMapper(uint address, uint data);
    void WriteMegaSdEnhancedMapper(uint address, uint data);
    int MegaSdContextSize { get; }
    int SaveMegaSdContext(Span<byte> state);
    int LoadMegaSdContext(Span<byte> state);
}

internal sealed class MdCartridgeConfiguration
{
    public byte lock_on, add_on;
}

internal sealed partial class MdCartridge : IDisposable
{
    private readonly IMdCartridgeBus _bus;
    private readonly InputHardware _input;
    private readonly CartridgeState cart;
    private readonly RomInformation rominfo;
    private readonly BackupRam sram;
    private readonly MdCartridgeConfiguration config;
    private readonly CartridgeMemoryBank[] memory_map;
    private readonly CartridgeZ80Bank[] zbank_memory_map;
    private readonly EepromI2c _i2c;
    private readonly EepromSpi _spi;
    private readonly FlashCfi _flash;
    private readonly GameGenie _gameGenie;
    private readonly ActionReplay _actionReplay;
    private readonly MdEntry[] rom_database;
    private readonly Func<uint, uint> m68k_read_bus_8, m68k_read_bus_16, zbank_unused_r;
    private readonly Action<uint, uint> m68k_unused_8_w, m68k_unused_16_w, zbank_unused_w;
    private Svp? _svp;
    private bool svp { get => _svp is not null; set { if (!value) _svp = null; } }
    public Svp? Svp => _svp;
    private byte system_hw => _bus.Hardware;
    private int disc_loaded => _bus.DiscLoaded;
    private int sample_rate => _bus.SampleRate;
    private bool has_cd_audio => _bus.CdAudioAllocated;
    private CPointer<byte> boot_rom => new(_bus.BootRom);
    private Span<ushort> digital_inputs => _input.DigitalInputs;
    private sealed record MdEntry(ushort chk_1, ushort chk_2, byte bank_start, byte bank_end, CartridgeHardware cart_hw);

    public MdCartridge(IMdCartridgeBus bus, CartridgeState cartridge, RomInformation information,
        BackupRam backup, CartridgeAddressSpace addresses, InputHardware input, MdCartridgeConfiguration configuration)
    {
        _bus = bus; cart = cartridge; rominfo = information; sram = backup; _input = input; config = configuration;
        memory_map = addresses.Main; zbank_memory_map = addresses.Z80;
        m68k_read_bus_8 = bus.ReadOpenBus8; m68k_read_bus_16 = bus.ReadOpenBus16;
        m68k_unused_8_w = bus.WriteUnused8; m68k_unused_16_w = bus.WriteUnused16;
        zbank_unused_r = bus.ReadUnusedZ80Bank; zbank_unused_w = bus.WriteUnusedZ80Bank;
        _spi = new(backup);
        _flash = new(backup, cartridge, addresses, m68k_unused_16_w);
        _i2c = new(backup, cartridge, addresses, m68k_read_bus_8, m68k_unused_8_w, m68k_unused_16_w, input.jcart_read, input.jcart_write);
        _gameGenie = new(cartridge, addresses, m68k_unused_8_w, m68k_unused_16_w);
        _actionReplay = new(cartridge, addresses, m68k_unused_16_w);
        rom_database = CreateDatabase();
    }

    public void Initialize() => md_cart_init();
    public void Reset(bool hard) => md_cart_reset(hard ? 1 : 0);
    private static int strstr(string text, string value) => text.Contains(value, StringComparison.Ordinal) ? 1 : 0;
    private static int memcmp(CPointer<byte> data, string text, int length)
    {
        for (int i = 0; i < length; i++)
        {
            int difference = data[i] - (i < text.Length ? (byte)text[i] : 0);
            if (difference != 0) return difference;
        }
        return 0;
    }
    private static void CopyMemory(CPointer<byte> destination, CPointer<byte> source, int length) => source.Bytes(length).CopyTo(destination.Bytes(length));
    private static void FillMemory(CPointer<byte> memory, int value, int length) => memory.Bytes(length).Fill(unchecked((byte)value));
    private void m68k_pulse_reset() => _bus.ResetMainCpu();
    private void ctrl_io_write_byte(uint address, uint data) => _bus.WriteControlIoByte(address, data);
    private void EnsureYxAudio() => _bus.EnsureCartridgeAudio();
    private void EnsureCdAudio() => _bus.EnsureCdAudio();
    private void sram_init() => sram.Initialize(cart, rominfo);
    private uint sram_read_byte(uint address) => sram.ReadByte(address);
    private uint sram_read_word(uint address) => sram.ReadWord(address);
    private void sram_write_byte(uint address, uint data) => sram.WriteByte(address, data);
    private void sram_write_word(uint address, uint data) => sram.WriteWord(address, data);
    private void eeprom_i2c_init() => _i2c.Initialize(rominfo.product, rominfo.checksum);
    private void eeprom_spi_init() => _spi.Initialize();
    private void eeprom_spi_write(byte data) => _spi.Write(data);
    private uint eeprom_spi_read(uint address) => _spi.Read(address);
    private void flash_cfi_init(int type, CPointer<ushort> otp) => _flash.Initialize(type,
        otp.IsNull ? ReadOnlySpan<ushort>.Empty : MemoryMarshal.Cast<byte, ushort>(otp.Cast<byte>().Bytes(128 * 2)));
    private uint flash_cfi_read(uint address) => _flash.Read(address);
    private void flash_cfi_write(uint address, uint data) => _flash.Write(address, data);
    private void yx5200_init(int rate) => _bus.InitializeYx5200(rate);
    private void yx5200_reset() => _bus.ResetYx5200();
    private void yx5200_write(uint data) => _bus.WriteYx5200(data);
    private void megasd_reset() => _bus.ResetMegaSd();
    private void megasd_rom_mapper_w(uint address, uint data) => _bus.WriteMegaSdRomMapper(address, data);
    private void megasd_enhanced_ssf2_mapper_w(uint address, uint data) => _bus.WriteMegaSdEnhancedMapper(address, data);
    private void ggenie_shutdown() => _gameGenie.Dispose();
    private void areplay_shutdown() => _actionReplay.Dispose();
    private void ggenie_reset(int hard) => _gameGenie.Reset(hard != 0);
    private void areplay_reset(int hard) => _actionReplay.Reset(hard != 0);
    private void ggenie_init()
    {
        int length = _bus.LoadLockOnRom(2, cart.lockrom.AsSpan(0, 0x8000));
        _gameGenie.Initialize(length > 0 ? cart.lockrom.AsSpan(0, Math.Min(length, 0x8000)) : []);
    }
    private void areplay_init()
    {
        int length = _bus.LoadLockOnRom(3, cart.lockrom);
        _actionReplay.Initialize(length > 0 ? cart.lockrom.AsSpan(0, Math.Min(length, 0x10000)) : []);
    }
    private int load_archive(int kind, CPointer<byte> destination, int maximum, CPointer<byte> extension) => _bus.LoadLockOnRom(kind, destination.Bytes(maximum));

    private static MdEntry CreateEntry(ushort first, ushort second, byte start, byte end, ReadOnlySpan<byte> registers,
        ReadOnlySpan<uint> masks, ReadOnlySpan<uint> addresses, ushort realtec, ushort bankshift,
        Func<uint, uint>? timeRead, Action<uint, uint>? timeWrite, Func<uint, uint>? registerRead, Action<uint, uint>? registerWrite)
    {
        CartridgeHardware hardware = new() { realtec = realtec, bankshift = bankshift, time_r = timeRead, time_w = timeWrite, regs_r = registerRead, regs_w = registerWrite };
        registers.CopyTo(hardware.regs); masks.CopyTo(hardware.mask); addresses.CopyTo(hardware.addr);
        return new(first, second, start, end, hardware);
    }
    private void ClearHardware()
    {
        cart.hw.regs.AsSpan().Clear(); cart.hw.mask.AsSpan().Clear(); cart.hw.addr.AsSpan().Clear();
        cart.hw.realtec = cart.hw.bankshift = 0;
        cart.hw.time_r = cart.hw.regs_r = null; cart.hw.time_w = cart.hw.regs_w = null;
    }
    private void CopyHardware(CartridgeHardware hardware)
    {
        hardware.regs.CopyTo(cart.hw.regs, 0); hardware.mask.CopyTo(cart.hw.mask, 0); hardware.addr.CopyTo(cart.hw.addr, 0);
        cart.hw.realtec = hardware.realtec; cart.hw.bankshift = hardware.bankshift;
        cart.hw.time_r = hardware.time_r; cart.hw.time_w = hardware.time_w; cart.hw.regs_r = hardware.regs_r; cart.hw.regs_w = hardware.regs_w;
    }
    private void svp_init()
    {
        _svp = new(cart.rom);
        memory_map[0x30].Data = _svp.Dram;
        memory_map[0x30].read8 = memory_map[0x30].read16 = null;
        memory_map[0x30].write8 = null; memory_map[0x30].write16 = _svp.WriteDram;
        zbank_memory_map[0x30].read = null; zbank_memory_map[0x30].write = null;
        memory_map[0x31].Data = _svp.Dram[0x10000..];
        memory_map[0x31].read8 = memory_map[0x31].read16 = null;
        memory_map[0x31].write8 = memory_map[0x31].write16 = null;
        zbank_memory_map[0x31].read = null; zbank_memory_map[0x31].write = null;
        memory_map[0x39].read8 = _svp.ReadCellByte; memory_map[0x39].read16 = _svp.ReadCell1;
        zbank_memory_map[0x39].read = _svp.ReadCellByte;
        memory_map[0x3a].read8 = _svp.ReadCellByte; memory_map[0x3a].read16 = _svp.ReadCell2;
        zbank_memory_map[0x3a].read = _svp.ReadCellByte;
    }
    private void svp_reset() => _svp!.Reset();
    public void Dispose()
    {
        try { _gameGenie.Dispose(); }
        finally
        {
            try { _actionReplay.Dispose(); }
            finally { _svp = null; }
        }
    }
}
