using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Boards;

internal abstract partial class SegaBoard
{
    protected void InitializeEightBitMapping(bool sg1000Console = false)
    {
        config.Sms.Sg1000Console = sg1000Console;
        z80_init(0, z80_irq_callback);
        sms_cart_init();
        ConfigureZ80Ports();
    }
    protected void InitializeMegaDriveMapping() { unchecked { int i = default; z80_init(0,z80_irq_callback);
            m68k_init();
            main.aerr_enabled = unchecked((uint)(config.addr_error));
            for (i = 128; (i < 224); i++) {
            memory_map[(int)(i)].@base = new ArrayPointer<byte>(work_ram);
            memory_map[(int)(i)].read8 = m68k_lockup_r_8;
            memory_map[(int)(i)].read16 = m68k_lockup_r_16;
            memory_map[(int)(i)].write8 = m68k_lockup_w_8;
            memory_map[(int)(i)].write16 = m68k_lockup_w_16;
            zbank_memory_map[(int)(i)].read = zbank_lockup_r;
            zbank_memory_map[(int)(i)].write = zbank_lockup_w;
            }
            for (i = 192; (i < 224); i += 8) {
            memory_map[(int)(i)].read8 = vdp_read_byte;
            memory_map[(int)(i)].read16 = vdp_read_word;
            memory_map[(int)(i)].write8 = vdp_write_byte;
            memory_map[(int)(i)].write16 = vdp_write_word;
            zbank_memory_map[(int)(i)].read = zbank_read_vdp;
            zbank_memory_map[(int)(i)].write = zbank_write_vdp;
            }
            for (i = 224; (i < 256); i++) {
            memory_map[(int)(i)].@base = new ArrayPointer<byte>(work_ram);
            memory_map[(int)(i)].read8 = null;
            memory_map[(int)(i)].read16 = null;
            memory_map[(int)(i)].write8 = null;
            memory_map[(int)(i)].write16 = null;
            zbank_memory_map[(int)(i)].read = zbank_unused_r;
            zbank_memory_map[(int)(i)].write = null;
            }
            ConfigureSixteenBitIo();
            InitializeSixteenBitDevices();
             } }
    protected virtual void ConfigureSixteenBitIo() => ConfigureMegaDriveIo();
    protected virtual void InitializeSixteenBitDevices() => md_cart_init();
    protected void ConfigureMegaDriveIo() { unchecked { int i = default;
            memory_map[(int)(161)].read8 = ctrl_io_read_byte;
            memory_map[(int)(161)].read16 = ctrl_io_read_word;
            memory_map[(int)(161)].write8 = ctrl_io_write_byte;
            memory_map[(int)(161)].write16 = ctrl_io_write_word;
            zbank_memory_map[(int)(161)].read = zbank_read_ctrl_io;
            zbank_memory_map[(int)(161)].write = zbank_write_ctrl_io;
            for (i = 0; (i < 64); i++) {
            z80_readmap[(int)(i)] = new ArrayPointer<byte>(zram, (int)(((i & 7) << 10)));
            }
            z80_writemem = z80_memory_w;
            z80_readmem = z80_memory_r;
            z80_writeport = z80_unused_port_w;
            z80_readport = z80_unused_port_r;
             } }
    protected void ConfigurePicoIo(Func<uint,uint> readByte, Func<uint,uint> readWord, Action<uint,uint> writeByte, Action<uint,uint> writeWord) { unchecked {
            memory_map[(int)(128)].read8 = readByte;
            memory_map[(int)(128)].read16 = readWord;
            memory_map[(int)(128)].write8 = writeByte;
            memory_map[(int)(128)].write16 = writeWord;
            memory_map[(int)(161)].read8 = m68k_read_bus_8;
            memory_map[(int)(161)].read16 = m68k_read_bus_16;
            memory_map[(int)(161)].write8 = m68k_unused_8_w;
            memory_map[(int)(161)].write16 = m68k_unused_16_w;
             } }
    protected void InitializeMegaCdDevices() { unchecked {
            s68k_init();
            sub.aerr_enabled = unchecked((uint)(config.addr_error));
            scd_init();
             } }
    protected void UseSg1000Ports() { z80_writeport = z80_sg_port_w; z80_readport = z80_sg_port_r; }
    protected void SetZ80Ports(Func<uint, byte> read, Action<uint, byte> write)
    {
        z80_readport = read;
        z80_writeport = write;
    }
    protected byte ReadSg1000Port(uint port) => z80_sg_port_r(port);
    protected void WriteSg1000Port(uint port, byte value) => z80_sg_port_w(port, value);
    protected uint MachineZ80Cycles => z80.cycles;
    private bool _flatZ80Memory;
    protected void MapFlatZ80Memory(byte[]? lowRom, int romWindow = 0x4000, int ramStart = 0)
    {
        _flatZ80Memory = true;
        for (int page = 0; page < _readMap.Length; page++)
        {
            int offset = page * 1024;
            _writeMap[page] = offset >= ramStart ? work_ram.AsMemory(offset, 1024) : default;
            z80_readmap[page] = lowRom is not null && offset < romWindow
                ? new ArrayPointer<byte>(lowRom, offset % lowRom.Length)
                : new ArrayPointer<byte>(work_ram, offset);
        }
        z80_readmem = ReadFlatZ80Memory;
        z80_writemem = WriteFlatZ80Memory;
    }
    private byte ReadFlatZ80Memory(uint address) => _readMap[(address & 0xffff) >> 10].Span[(int)(address & 1023)];
    private void WriteFlatZ80Memory(uint address, byte value)
    {
        Span<byte> page = _writeMap[(address & 0xffff) >> 10].Span;
        if (!page.IsEmpty) page[(int)(address & 1023)] = value;
    }
    protected byte[]? ReadBasicCartridge()
    {
        if (cart.romsize != 0x8000) return null;
        var rom = cart.rom.AsSpan(0, 0x8000);
        return Cartridge.Checksums.Crc32(0, rom) is 0xf691f9c7 or 0x5d9f11ca or 0xa46e3c73 or 0x78a37cbc
            ? cart.rom
            : null;
    }
    protected void UseMarkIIIPorts() { z80_writeport = z80_m3_port_w; z80_readport = z80_m3_port_r; }
    protected void UseMasterSystemPorts() { z80_writeport = z80_ms_port_w; z80_readport = z80_ms_port_r; }
    protected void UseGameGearPorts() { z80_writeport = z80_gg_port_w; z80_readport = z80_gg_port_r; }
    protected void UsePowerBasePorts() { z80_writeport = z80_md_port_w; z80_readport = z80_md_port_r; }
    private void gen_reset(int hard_reset)
    {
        unchecked
        {
            if ((hard_reset != 0)) {
            main.cycles = (((((unchecked((int)(lines_per_frame)) - 192) + 159) - (27 * unchecked((int)(vdp_pal)))) * 3420) + 1004);
            FillMemory(new ArrayPointer<byte>(work_ram), 0, 65536);
            FillMemory(new ArrayPointer<byte>(zram), 0, 8192);
            } else {
            main.cycles = unchecked((int)(unchecked((uint)((unchecked((double)((3420 * unchecked((int)(lines_per_frame))))) * (unchecked((double)(rand())) / unchecked((double)(2147483647))))))));
            fm_reset(unchecked((uint)(0)));
            }
            main.cycles = ((main.cycles / 7) * 7);
            z80.cycles = unchecked((uint)(((main.cycles / 15) * 15)));
            if (((unchecked((int)(system_hw)) & 129) == 128)) {
            if ((unchecked((int)(system_hw)) == 132)) {
            if ((hard_reset != 0)) {
            scd_reset(1);
            }
            if ((cd_boot != 0)) {
            md_cart_reset(hard_reset);
            }
            } else {
            md_cart_reset(hard_reset);
            }
            memory_map[(int)(160)].read8 = m68k_read_bus_8;
            memory_map[(int)(160)].read16 = m68k_read_bus_16;
            memory_map[(int)(160)].write8 = m68k_unused_8_w;
            memory_map[(int)(160)].write16 = m68k_unused_16_w;
            zstate = unchecked((byte)(0));
            zbank = unchecked((uint)(0));
            if (((((unchecked((int)(config.bios)) & 1) != 0) && (unchecked((int)(system_hw)) == 128)) && (hard_reset != 0))) {
            int i = default;
            FillMemory(new ArrayPointer<byte>(tmss), 0, 4);
            for (i = 192; (i < 224); i += 8) {
            memory_map[(int)(i)].read8 = m68k_lockup_r_8;
            memory_map[(int)(i)].read16 = m68k_lockup_r_16;
            memory_map[(int)(i)].write8 = m68k_lockup_w_8;
            memory_map[(int)(i)].write16 = m68k_lockup_w_16;
            zbank_memory_map[(int)(i)].read = zbank_lockup_r;
            zbank_memory_map[(int)(i)].write = zbank_lockup_w;
            }
            if (((unchecked((int)(system_bios)) & 128) != 0)) {
            cart.@base = memory_map[(int)(0)].@base;
            memory_map[(int)(0)].@base = new ArrayPointer<byte>(boot_rom);
            }
            }
            m68k_pulse_reset();
            } else {
            if (((unchecked((int)(system_hw)) == 16) || (((unchecked((int)(system_hw)) & 32) != 0) && (unchecked((int)(region_code)) == 0)))) {
            FillMemory(new ArrayPointer<byte>(work_ram), 240, 65536);
            }
            sms_cart_reset();
            m68k_pulse_halt();
            }
            z80_reset();
            if ((hard_reset != 0)) {
            if ((unchecked((int)(system_hw)) == 129)) {
            z80.hl.Word = 57345;
            z80.sp.Word = 57343;
            z80.r = 4;
            } else if (((unchecked((int)(system_hw)) & (32 | 64)) != 0)) {
            if (((!((unchecked((int)(config.bios)) & 1) != 0)) || (!((unchecked((int)(system_bios)) & (32 | 64)) != 0)))) {
            z80.sp.Word = 57328;
            }
            }
            }
        }
    }
    private void gen_tmss_w(uint offset, uint data)
    {
        unchecked
        {
            int i = default;
            tmss[(int)(offset)] = unchecked((byte)(((data >> 8) & unchecked((uint)(255)))));
            tmss[(int)((offset + unchecked((uint)(1))))] = unchecked((byte)((data & unchecked((uint)(255)))));
            if ((TmssMatches() == 0)) {
            for (i = 192; (i < 224); i += 8) {
            memory_map[(int)(i)].read8 = vdp_read_byte;
            memory_map[(int)(i)].read16 = vdp_read_word;
            memory_map[(int)(i)].write8 = vdp_write_byte;
            memory_map[(int)(i)].write16 = vdp_write_word;
            zbank_memory_map[(int)(i)].read = zbank_read_vdp;
            zbank_memory_map[(int)(i)].write = zbank_write_vdp;
            }
            } else {
            for (i = 192; (i < 224); i += 8) {
            memory_map[(int)(i)].read8 = m68k_lockup_r_8;
            memory_map[(int)(i)].read16 = m68k_lockup_r_16;
            memory_map[(int)(i)].write8 = m68k_lockup_w_8;
            memory_map[(int)(i)].write16 = m68k_lockup_w_16;
            zbank_memory_map[(int)(i)].read = zbank_lockup_r;
            zbank_memory_map[(int)(i)].write = zbank_lockup_w;
            }
            }
        }
    }
    private void gen_bankswitch_w(uint data)
    {
        unchecked
        {
            if (((unchecked((int)(system_bios)) & 128) != 0)) {
            if (((data & unchecked((uint)(1))) != 0)) {
            memory_map[(int)(0)].@base = cart.@base;
            } else {
            memory_map[(int)(0)].@base = new ArrayPointer<byte>(boot_rom);
            }
            }
        }
    }
    private uint gen_bankswitch_r()
    {
        unchecked
        {
            if (((unchecked((int)(system_bios)) & 128) != 0)) {
            return (memory_map[(int)(0)].@base.Same(cart.@base) ? (uint)1 : (uint)0);
            }
            return unchecked((uint)(255));
        }
    }
    private void gen_zbusreq_w(uint data, uint cycles)
    {
        unchecked
        {
            if ((data != 0)) {
            if ((unchecked((int)(zstate)) == 1)) {
            z80_run(cycles);
            memory_map[(int)(160)].read8 = z80_read_byte;
            memory_map[(int)(160)].read16 = z80_read_word;
            memory_map[(int)(160)].write8 = z80_write_byte;
            memory_map[(int)(160)].write16 = z80_write_word;
            }
            zstate |= unchecked((byte)(2));
            } else {
            if ((unchecked((int)(zstate)) == 3)) {
            z80.cycles = (((cycles + unchecked((uint)(14))) / unchecked((uint)(15))) * unchecked((uint)(15)));
            memory_map[(int)(160)].read8 = m68k_read_bus_8;
            memory_map[(int)(160)].read16 = m68k_read_bus_16;
            memory_map[(int)(160)].write8 = m68k_unused_8_w;
            memory_map[(int)(160)].write16 = m68k_unused_16_w;
            }
            zstate &= unchecked((byte)(1));
            }
        }
    }
    private void gen_zreset_w(uint data, uint cycles)
    {
        unchecked
        {
            if ((data != 0)) {
            if ((unchecked((int)(zstate)) == 0)) {
            z80.cycles = (((cycles + unchecked((uint)(14))) / unchecked((uint)(15))) * unchecked((uint)(15)));
            z80_reset();
            fm_reset(cycles);
            } else if ((unchecked((int)(zstate)) == 2)) {
            memory_map[(int)(160)].read8 = z80_read_byte;
            memory_map[(int)(160)].read16 = z80_read_word;
            memory_map[(int)(160)].write8 = z80_write_byte;
            memory_map[(int)(160)].write16 = z80_write_word;
            z80_reset();
            fm_reset(cycles);
            }
            zstate |= unchecked((byte)(1));
            } else {
            if ((unchecked((int)(zstate)) == 1)) {
            z80_run(cycles);
            } else if ((unchecked((int)(zstate)) == 3)) {
            memory_map[(int)(160)].read8 = m68k_read_bus_8;
            memory_map[(int)(160)].read16 = m68k_read_bus_16;
            memory_map[(int)(160)].write8 = m68k_unused_8_w;
            memory_map[(int)(160)].write16 = m68k_unused_16_w;
            }
            fm_reset(cycles);
            zstate &= unchecked((byte)(2));
            }
        }
    }
    private void gen_zbank_w(uint data)
    {
        unchecked
        {
            zbank = (((zbank >> 1) | ((data & unchecked((uint)(1))) << 23)) & unchecked((uint)(16744448)));
        }
    }
    private int z80_irq_callback(int param)
    {
        unchecked
        {
            return (-1);
        }
    }
}
