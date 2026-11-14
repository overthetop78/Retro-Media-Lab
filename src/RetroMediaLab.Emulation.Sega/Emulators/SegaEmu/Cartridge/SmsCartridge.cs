using RetroMediaLab.Chips.Controllers.Sega.Input;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Common.Audio;
using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Cartridge;

internal interface ISmsCartridgeBus
{
    byte Hardware { get; set; }
    byte BootRomFlags { get; set; }
    byte Region { get; set; }
    byte LastZ80Fetch { get; }
    ReadOnlyMemory<byte> IoRegisters { get; }
    int LoadBios(int hardware);
}

internal sealed class SmsCartridgeConfiguration
{
    private readonly SoundConfiguration _sound;
    public byte system, bios;
    public int ym2413 { get => _sound.Ym2413Mode; set => _sound.Ym2413Mode = value; }
    public SmsCartridgeConfiguration(SoundConfiguration sound) { _sound = sound; }
}

internal sealed partial class SmsCartridge
{
    private readonly ISmsCartridgeBus _bus;
    private readonly InputHardware _input;
    private readonly Eeprom93c _eeprom;
    private readonly CartridgeState cart;
    private readonly RomInformation _information;
    private readonly BackupRam sram;
    private readonly SmsCartridgeConfiguration config;
    private readonly BytePageMap z80_readmap, z80_writemap;
    private readonly byte[] work_ram;
    private Func<uint, byte> z80_readmem = null!;
    private Action<uint, byte> z80_writemem = null!;
    private byte system_hw { get => _bus.Hardware; set => _bus.Hardware = value; }
    private byte system_bios { get => _bus.BootRomFlags; set => _bus.BootRomFlags = value; }
    private byte region_code { get => _bus.Region; set => _bus.Region = value; }
    private byte z80_last_fetch => _bus.LastZ80Fetch;
    private ReadOnlySpan<byte> io_reg => _bus.IoRegisters.Span;
    private Span<int> old_system => _input.OriginalPortSystems;
    private Span<byte> port_systems => _input.PortSystems;
    private int gun_offset_x { get => _input.GunOffsetX; set => _input.GunOffsetX = value; }
    private int gun_offset_y { get => _input.GunOffsetY; set => _input.GunOffsetY = value; }
    private byte eeprom_enabled { get => _eeprom.EnableRegister; set => _eeprom.EnableRegister = value; }

    public SmsCartridge(ISmsCartridgeBus bus, CartridgeState cartridge, RomInformation information,
        BackupRam backup, Eeprom93c eeprom, InputHardware input, SmsCartridgeConfiguration configuration,
        Memory<byte>[] readMap, Memory<byte>[] writeMap, byte[] workRam)
    {
        _bus = bus; cart = cartridge; _information = information; sram = backup; _eeprom = eeprom;
        _input = input; config = configuration; work_ram = workRam;
        z80_readmap = new(readMap); z80_writemap = new(writeMap);
    }

    public void Initialize() => sms_cart_init();
    public void Reset() => sms_cart_reset();
    public void Switch(byte mode) => sms_cart_switch(mode);
    public int RamSize => sms_cart_ram_size();
    public int DetectRegion() => sms_cart_region_detect();
    public int ContextSize => 4 + RamSize;
    public byte ReadMemory(uint address) => z80_readmem(address);
    public void WriteMemory(uint address, byte data) => z80_writemem(address, data);
    private int load_bios(int hardware) => _bus.LoadBios(hardware);
    private void sram_init() => sram.Initialize(cart, _information);
    private void eeprom_93c_init() => _eeprom.Initialize();
    private void eeprom_93c_write(byte data) => _eeprom.Write(data);
    private byte eeprom_93c_read() => _eeprom.Read();
    private void terebi_oekaki_write(byte data) => _input.terebi_oekaki_write(data);
    private ushort terebi_oekaki_read() => _input.terebi_oekaki_read();
    private bool HasJapaneseHeader() => _information.country.Length == 0 ||
        _information.country.StartsWith("SMS Japan", StringComparison.Ordinal) ||
        _information.country.StartsWith("GG Japan", StringComparison.Ordinal);
    private static uint crc32(uint seed, ArrayPointer<byte> data, uint length) => Checksums.Crc32(seed, data.Bytes(checked((int)length)));
    private static void FillMemory(ArrayPointer<byte> data, int value, int length) => data.Bytes(length).Fill(unchecked((byte)value));
    private static void CopyMemory(ArrayPointer<byte> destination, ArrayPointer<byte> source, uint length) => source.Bytes(checked((int)length)).CopyTo(destination.Bytes(checked((int)length)));
    private static int SaveChunk(ArrayPointer<byte> destination, ArrayPointer<byte> source, int length)
    {
        source.Bytes(length).CopyTo(destination.Bytes(length)); return length;
    }
    private static int LoadChunk(ArrayPointer<byte> source, ArrayPointer<byte> destination, int length) => SaveChunk(destination, source, length);

    public int SaveContext(Span<byte> state)
    {
        if (state.Length < ContextSize) throw new ArgumentException("SMS cartridge context buffer is too short.", nameof(state));
        return sms_cart_context_save(new ArrayPointer<byte>(state));
    }
    public int LoadContext(Span<byte> state)
    {
        if (state.Length < ContextSize) throw new ArgumentException("SMS cartridge context buffer is too short.", nameof(state));
        return sms_cart_context_load(new ArrayPointer<byte>(state));
    }
}
