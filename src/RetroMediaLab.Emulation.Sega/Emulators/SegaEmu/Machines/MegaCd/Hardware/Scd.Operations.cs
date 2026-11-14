using System.IO;
using RetroMediaLab.Chips.Common.Memory;
using RetroMediaLab.Chips.CPU.Motorola.M68000.Execution;
using RetroMediaLab.Chips.Controllers.Sega.CD;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Media;
using Pcm = RetroMediaLab.Chips.Audio.Ricoh.RF5C164.Rf5c164;
using BlipBuffer = RetroMediaLab.Chips.Audio.Common.Resampling.BandLimitedBuffer;
using System.Buffers.Binary;


namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.MegaCd.Hardware;

internal sealed partial class Scd
{
    private uint s68k_read_bus_8(uint address)
    {
        unchecked
        {
            address = (sub.pc | (address & unchecked((uint)(1))));
            return unchecked((uint)(sub_memory_map[(int)(((address >> 16) & unchecked((uint)(255))))].@base[(int)(((address & unchecked((uint)(65535))) ^ unchecked((uint)(1))))]));
        }
    }
    private uint s68k_read_bus_16(uint address)
    {
        unchecked
        {
            address = sub.pc;
            return unchecked((uint)((sub_memory_map[(int)(((address >> 16) & unchecked((uint)(255))))].@base + unchecked((int)((address & unchecked((uint)(65534)))))).Cast<ushort>()[0]));
        }
    }
    private void s68k_unused_8_w(uint address, uint data)
    {
        unchecked
        {

        }
    }
    private void s68k_unused_16_w(uint address, uint data)
    {
        unchecked
        {

        }
    }
    private void s68k_lockup_w_8(uint address, uint data)
    {
        unchecked
        {
            s68k_pulse_wait(address, unchecked((uint)(1)));
        }
    }
    private void s68k_lockup_w_16(uint address, uint data)
    {
        unchecked
        {
            s68k_pulse_wait(address, unchecked((uint)(1)));
        }
    }
    private uint s68k_lockup_r_8(uint address)
    {
        unchecked
        {
            s68k_pulse_wait(address, unchecked((uint)(0)));
            return unchecked((uint)(255));
        }
    }
    private uint s68k_lockup_r_16(uint address)
    {
        unchecked
        {
            s68k_pulse_wait(address, unchecked((uint)(0)));
            return unchecked((uint)(65535));
        }
    }
    private void prg_ram_dma_w(uint length)
    {
        unchecked
        {
            ushort data = default;
            uint words = (length >> 1);
            ushort src_index = unchecked((ushort)((unchecked((int)(cdc_dac.Word)) & 16382)));
            uint dst_index = unchecked((uint)(((unchecked((int)(scd_regs[(int)((10 >> 1))].Word)) << 3) & 524286)));
            scd_regs[(int)((10 >> 1))].Word += unchecked((ushort)((words >> 2)));
            cdc_dac.Word += unchecked((ushort)((words << 1)));
            while (((words--) != 0)) {
            data = unchecked((ushort)(((unchecked((int)(cdc_ram[(int)(src_index)])) << 8) | unchecked((int)(cdc_ram[(int)((unchecked((int)(src_index)) + 1))])))));
            (new ArrayPointer<byte>(prg_ram) + unchecked((int)(dst_index))).Cast<ushort>()[0] = data;
            src_index = unchecked((ushort)(((unchecked((int)(src_index)) + 2) & 16382)));
            dst_index = ((dst_index + unchecked((uint)(2))) & unchecked((uint)(524286)));
            }
        }
    }
    private void prg_ram_write_byte(uint address, uint data)
    {
        unchecked
        {
            address &= unchecked((uint)(524287));
            if ((address >= unchecked((uint)((unchecked((int)(scd_regs[(int)((2 >> 1))].High)) << 9))))) {
            prg_ram[(int)((address ^ unchecked((uint)(1))))] = unchecked((byte)((data & unchecked((uint)(255)))));
            return;
            }
        }
    }
    private void prg_ram_write_word(uint address, uint data)
    {
        unchecked
        {
            address &= unchecked((uint)(524286));
            if ((address >= unchecked((uint)((unchecked((int)(scd_regs[(int)((2 >> 1))].High)) << 9))))) {
            (new ArrayPointer<byte>(prg_ram) + unchecked((int)(address))).Cast<ushort>()[0] = unchecked((ushort)(data));
            return;
            }
        }
    }
    private uint prg_ram_z80_read_byte(uint address)
    {
        unchecked
        {
            int offset = unchecked((int)(((address >> 16) & unchecked((uint)(3)))));
            if ((zbank_memory_map[(int)(offset)].read != null)) {
            return zbank_memory_map[(int)(offset)].read(address);
            }
            return unchecked((uint)(memory_map[(int)(offset)].@base[(int)(((address & unchecked((uint)(65535))) ^ unchecked((uint)(1))))]));
        }
    }
    private void prg_ram_z80_write_byte(uint address, uint data)
    {
        unchecked
        {
            int offset = unchecked((int)(((address >> 16) & unchecked((uint)(3)))));
            if ((zbank_memory_map[(int)(offset)].write != null)) {
            zbank_memory_map[(int)(offset)].write(address, data);
            } else {
            memory_map[(int)(offset)].@base[(int)(((address & unchecked((uint)(65535))) ^ unchecked((uint)(1))))] = unchecked((byte)((data & unchecked((uint)(255)))));
            }
        }
    }
    private uint prg_ram_m68k_read_byte(uint address)
    {
        unchecked
        {
            int offset = unchecked((int)(((address >> 16) & unchecked((uint)(3)))));
            if ((memory_map[(int)(offset)].read8 != null)) {
            return memory_map[(int)(offset)].read8(address);
            }
            return unchecked((uint)(memory_map[(int)(offset)].@base[(int)(((address & unchecked((uint)(65535))) ^ unchecked((uint)(1))))]));
        }
    }
    private uint prg_ram_m68k_read_word(uint address)
    {
        unchecked
        {
            int offset = unchecked((int)(((address >> 16) & unchecked((uint)(3)))));
            if ((memory_map[(int)(offset)].read16 != null)) {
            return memory_map[(int)(offset)].read16(address);
            }
            return unchecked((uint)((memory_map[(int)(offset)].@base + unchecked((int)((address & unchecked((uint)(65534)))))).Cast<ushort>()[0]));
        }
    }
    private void prg_ram_m68k_write_byte(uint address, uint data)
    {
        unchecked
        {
            int offset = unchecked((int)(((address >> 16) & unchecked((uint)(3)))));
            if ((memory_map[(int)(offset)].write8 != null)) {
            memory_map[(int)(offset)].write8(address, data);
            } else {
            memory_map[(int)(offset)].@base[(int)(((address & unchecked((uint)(65535))) ^ unchecked((uint)(1))))] = unchecked((byte)((data & unchecked((uint)(255)))));
            }
        }
    }
    private void prg_ram_m68k_write_word(uint address, uint data)
    {
        unchecked
        {
            int offset = unchecked((int)(((address >> 16) & unchecked((uint)(3)))));
            if ((memory_map[(int)(offset)].write16 != null)) {
            memory_map[(int)(offset)].write16(address, data);
            } else {
            (memory_map[(int)(offset)].@base + unchecked((int)((address & unchecked((uint)(65534)))))).Cast<ushort>()[0] = unchecked((ushort)(data));
            }
        }
    }
    private uint word_ram_z80_read_byte(uint address)
    {
        unchecked
        {
            int offset = unchecked((int)(((address >> 16) & unchecked((uint)(35)))));
            if ((zbank_memory_map[(int)(offset)].read != null)) {
            return zbank_memory_map[(int)(offset)].read(address);
            }
            return unchecked((uint)(memory_map[(int)(offset)].@base[(int)(((address & unchecked((uint)(65535))) ^ unchecked((uint)(1))))]));
        }
    }
    private void word_ram_z80_write_byte(uint address, uint data)
    {
        unchecked
        {
            int offset = unchecked((int)(((address >> 16) & unchecked((uint)(35)))));
            if ((zbank_memory_map[(int)(offset)].write != null)) {
            zbank_memory_map[(int)(offset)].write(address, data);
            } else {
            memory_map[(int)(offset)].@base[(int)(((address & unchecked((uint)(65535))) ^ unchecked((uint)(1))))] = unchecked((byte)((data & unchecked((uint)(255)))));
            }
        }
    }
    private uint word_ram_m68k_read_byte(uint address)
    {
        unchecked
        {
            int offset = unchecked((int)(((address >> 16) & unchecked((uint)(35)))));
            if ((memory_map[(int)(offset)].read8 != null)) {
            return memory_map[(int)(offset)].read8(address);
            }
            return unchecked((uint)(memory_map[(int)(offset)].@base[(int)(((address & unchecked((uint)(65535))) ^ unchecked((uint)(1))))]));
        }
    }
    private uint word_ram_m68k_read_word(uint address)
    {
        unchecked
        {
            int offset = unchecked((int)(((address >> 16) & unchecked((uint)(35)))));
            if ((memory_map[(int)(offset)].read16 != null)) {
            return memory_map[(int)(offset)].read16(address);
            }
            return unchecked((uint)((memory_map[(int)(offset)].@base + unchecked((int)((address & unchecked((uint)(65534)))))).Cast<ushort>()[0]));
        }
    }
    private void word_ram_m68k_write_byte(uint address, uint data)
    {
        unchecked
        {
            int offset = unchecked((int)(((address >> 16) & unchecked((uint)(35)))));
            if ((memory_map[(int)(offset)].write8 != null)) {
            memory_map[(int)(offset)].write8(address, data);
            } else {
            memory_map[(int)(offset)].@base[(int)(((address & unchecked((uint)(65535))) ^ unchecked((uint)(1))))] = unchecked((byte)((data & unchecked((uint)(255)))));
            }
        }
    }
    private void word_ram_m68k_write_word(uint address, uint data)
    {
        unchecked
        {
            int offset = unchecked((int)(((address >> 16) & unchecked((uint)(35)))));
            if ((memory_map[(int)(offset)].write16 != null)) {
            memory_map[(int)(offset)].write16(address, data);
            } else {
            (memory_map[(int)(offset)].@base + unchecked((int)((address & unchecked((uint)(65534)))))).Cast<ushort>()[0] = unchecked((ushort)(data));
            }
        }
    }
    private uint word_ram_s68k_read_byte(uint address)
    {
        unchecked
        {
            int offset = unchecked((int)(((address >> 16) & unchecked((uint)(15)))));
            if ((sub_memory_map[(int)(offset)].read8 != null)) {
            return sub_memory_map[(int)(offset)].read8(address);
            }
            return unchecked((uint)(sub_memory_map[(int)(offset)].@base[(int)(((address & unchecked((uint)(65535))) ^ unchecked((uint)(1))))]));
        }
    }
    private uint word_ram_s68k_read_word(uint address)
    {
        unchecked
        {
            int offset = unchecked((int)(((address >> 16) & unchecked((uint)(15)))));
            if ((sub_memory_map[(int)(offset)].read16 != null)) {
            return sub_memory_map[(int)(offset)].read16(address);
            }
            return unchecked((uint)((sub_memory_map[(int)(offset)].@base + unchecked((int)((address & unchecked((uint)(65534)))))).Cast<ushort>()[0]));
        }
    }
    private void word_ram_s68k_write_byte(uint address, uint data)
    {
        unchecked
        {
            int offset = unchecked((int)(((address >> 16) & unchecked((uint)(15)))));
            if ((sub_memory_map[(int)(offset)].write8 != null)) {
            sub_memory_map[(int)(offset)].write8(address, data);
            } else {
            sub_memory_map[(int)(offset)].@base[(int)(((address & unchecked((uint)(65535))) ^ unchecked((uint)(1))))] = unchecked((byte)((data & unchecked((uint)(255)))));
            }
        }
    }
    private void word_ram_s68k_write_word(uint address, uint data)
    {
        unchecked
        {
            int offset = unchecked((int)(((address >> 16) & unchecked((uint)(15)))));
            if ((sub_memory_map[(int)(offset)].write16 != null)) {
            sub_memory_map[(int)(offset)].write16(address, data);
            } else {
            (sub_memory_map[(int)(offset)].@base + unchecked((int)((address & unchecked((uint)(65534)))))).Cast<ushort>()[0] = unchecked((ushort)(data));
            }
        }
    }
    private uint bram_read_byte(uint address)
    {
        unchecked
        {
            if (((address & unchecked((uint)(1))) != 0)) {
            return unchecked((uint)(bram[(int)(((address >> 1) & unchecked((uint)(8191))))]));
            }
            return unchecked((uint)(255));
        }
    }
    private uint bram_read_word(uint address)
    {
        unchecked
        {
            return unchecked((uint)((unchecked((int)(bram[(int)(((address >> 1) & unchecked((uint)(8191))))])) | 65280)));
        }
    }
    private void bram_write_byte(uint address, uint data)
    {
        unchecked
        {
            if (((address & unchecked((uint)(1))) != 0)) {
            bram[(int)(((address >> 1) & unchecked((uint)(8191))))] = unchecked((byte)(data));
            }
        }
    }
    private void bram_write_word(uint address, uint data)
    {
        unchecked
        {
            bram[(int)(((address >> 1) & unchecked((uint)(8191))))] = unchecked((byte)((data & unchecked((uint)(255)))));
        }
    }
    private void s68k_poll_detect(uint reg_mask)
    {
        unchecked
        {
            if (((sub.poll_detected & reg_mask) != 0)) {
            if ((unchecked((uint)(sub.cycles)) <= sub.poll_cycle)) {
            if ((sub.pc == sub.poll_pc)) {
            if (((sub.poll_detected & unchecked((uint)(1))) != 0)) {
            sub.cycles = unchecked((int)(sub.cycle_end));
            sub.stopped = reg_mask;
            } else {
            sub.poll_detected |= unchecked((uint)(1));
            sub.poll_cycle = unchecked((uint)((sub.cycles + 392)));
            }
            }
            return;
            }
            } else {
            sub.poll_detected = reg_mask;
            }
            sub.poll_cycle = unchecked((uint)((sub.cycles + 392)));
            sub.poll_pc = sub.pc;
        }
    }
    private void s68k_poll_sync(uint reg_mask)
    {
        unchecked
        {
            uint cycles = (unchecked((uint)((sub.cycles * 3420))) / cycles_per_line);
            if ((!(main.stopped != 0))) {
            if ((!(main.cycle_end != 0))) {
            m68k_run(cycles);
            }
            }
            if (((main.stopped & reg_mask) != 0)) {
            main.cycles = unchecked((int)(cycles));
            main.stopped = unchecked((uint)(0));
            }
            sub.poll_detected &= (~reg_mask);
            main.poll_detected &= (~reg_mask);
        }
    }
    private void m68k_sync()
    {
        unchecked
        {
            if ((!(main.stopped != 0))) {
            if ((!(main.cycle_end != 0))) {
            uint cycles = (unchecked((uint)((sub.cycles * 3420))) / cycles_per_line);
            m68k_run(cycles);
            }
            }
        }
    }
    private uint scd_read_byte(uint address)
    {
        unchecked
        {
            if ((!((address & unchecked((uint)(32768))) != 0))) {
            if (((address & unchecked((uint)(1))) != 0)) {
            return unchecked((uint)(pcm_read(((address >> 1) & unchecked((uint)(8191))), unchecked((uint)(sub.cycles)))));
            }
            return s68k_read_bus_8(address);
            }
            address &= unchecked((uint)(511));
            if ((address == unchecked((uint)(3)))) {
            s68k_poll_detect(unchecked((uint)((1 << 3))));
            return unchecked((uint)((unchecked((int)(scd_regs[(int)((3 >> 1))].Low)) & 31)));
            }
            if ((address == unchecked((uint)(14)))) {
            s68k_poll_detect(unchecked((uint)((1 << 14))));
            return unchecked((uint)(scd_regs[(int)((14 >> 1))].High));
            }
            if ((address == unchecked((uint)(4)))) {
            s68k_poll_detect(unchecked((uint)((1 << 4))));
            return unchecked((uint)(scd_regs[(int)((4 >> 1))].High));
            }
            if ((address == unchecked((uint)(88)))) {
            s68k_poll_detect(unchecked((uint)((1 << 8))));
            return unchecked((uint)(scd_regs[(int)((88 >> 1))].High));
            }
            if ((address == unchecked((uint)(7)))) {
            return unchecked((uint)(cdc_reg_r()));
            }
            if ((address == unchecked((uint)(0)))) {
            return unchecked((uint)(scd_regs[(int)((6 >> 1))].High));
            }
            if ((address == unchecked((uint)(1)))) {
            return unchecked((uint)(1));
            }
            if (((address >= unchecked((uint)(80))) && (address <= unchecked((uint)(87))))) {
            byte bits = unchecked((byte)(((unchecked((int)(scd_regs[(int)((78 >> 1))].Word)) >> unchecked((int)((((address & unchecked((uint)(6))) ^ unchecked((uint)(6))) << 1)))) << 2)));
            byte code = scd_regs[(int)((76 >> 1))].Low;
            ushort data = unchecked((ushort)(((unchecked((int)(code)) >> (unchecked((int)(bits)) & 4)) & 15)));
            bits = unchecked((byte)((unchecked((int)(bits)) >> 1)));
            data = unchecked((ushort)((unchecked((int)(data)) | (((unchecked((int)(code)) >> (unchecked((int)(bits)) & 4)) << 4) & 240))));
            bits = unchecked((byte)((unchecked((int)(bits)) >> 1)));
            data = unchecked((ushort)((unchecked((int)(data)) | (((unchecked((int)(code)) >> (unchecked((int)(bits)) & 4)) << 8) & 3840))));
            bits = unchecked((byte)((unchecked((int)(bits)) >> 1)));
            data = unchecked((ushort)((unchecked((int)(data)) | (((unchecked((int)(code)) >> (unchecked((int)(bits)) & 4)) << 12) & 61440))));
            return unchecked((uint)((((address & unchecked((uint)(1))) != 0) ? (unchecked((int)(data)) & 255) : (unchecked((int)(data)) >> 8))));
            }
            if (((address & unchecked((uint)(496))) == unchecked((uint)(16)))) {
            s68k_poll_detect(unchecked((uint)((1 << unchecked((int)((address & unchecked((uint)(31)))))))));
            } else if (((address & unchecked((uint)(256))) != 0)) {
            address &= unchecked((uint)(383));
            }
            if (((address & unchecked((uint)(1))) != 0)) {
            return unchecked((uint)(scd_regs[(int)((address >> 1))].Low));
            }
            return unchecked((uint)(scd_regs[(int)((address >> 1))].High));
        }
    }
    private uint scd_read_word(uint address)
    {
        unchecked
        {
            if ((!((address & unchecked((uint)(32768))) != 0))) {
            return unchecked((uint)(pcm_read(((address >> 1) & unchecked((uint)(8191))), unchecked((uint)(sub.cycles)))));
            }
            address &= unchecked((uint)(511));
            if ((address == unchecked((uint)(2)))) {
            s68k_poll_detect(unchecked((uint)((1 << 3))));
            return unchecked((uint)((unchecked((int)(scd_regs[(int)((3 >> 1))].Word)) & 65311)));
            }
            if ((address == unchecked((uint)(8)))) {
            return unchecked((uint)(cdc_host_r(unchecked((byte)(67)))));
            }
            if ((address == unchecked((uint)(0)))) {
            return unchecked((uint)(scd_regs[(int)((6 >> 1))].Word));
            }
            if ((address == unchecked((uint)(12)))) {
            return unchecked((uint)(((unchecked((int)(scd_regs[(int)((12 >> 1))].Word)) + ((sub.cycles - stopwatch) / (384 * 4))) & 4095)));
            }
            if (((address >= unchecked((uint)(80))) && (address <= unchecked((uint)(86))))) {
            byte bits = unchecked((byte)(((unchecked((int)(scd_regs[(int)((78 >> 1))].Word)) >> unchecked((int)((((address & unchecked((uint)(6))) ^ unchecked((uint)(6))) << 1)))) << 2)));
            byte code = scd_regs[(int)((76 >> 1))].Low;
            ushort data = unchecked((ushort)(((unchecked((int)(code)) >> (unchecked((int)(bits)) & 4)) & 15)));
            bits = unchecked((byte)((unchecked((int)(bits)) >> 1)));
            data = unchecked((ushort)((unchecked((int)(data)) | (((unchecked((int)(code)) >> (unchecked((int)(bits)) & 4)) << 4) & 240))));
            bits = unchecked((byte)((unchecked((int)(bits)) >> 1)));
            data = unchecked((ushort)((unchecked((int)(data)) | (((unchecked((int)(code)) >> (unchecked((int)(bits)) & 4)) << 8) & 3840))));
            bits = unchecked((byte)((unchecked((int)(bits)) >> 1)));
            data = unchecked((ushort)((unchecked((int)(data)) | (((unchecked((int)(code)) >> (unchecked((int)(bits)) & 4)) << 12) & 61440))));
            return unchecked((uint)(data));
            }
            if ((address == unchecked((uint)(6)))) {
            return unchecked((uint)(cdc_reg_r()));
            }
            if (((address & unchecked((uint)(496))) == unchecked((uint)(16)))) {
            m68k_sync();
            s68k_poll_detect(unchecked((uint)((3 << unchecked((int)((address & unchecked((uint)(30)))))))));
            } else if (((address & unchecked((uint)(256))) != 0)) {
            address &= unchecked((uint)(383));
            }
            return unchecked((uint)(scd_regs[(int)((address >> 1))].Word));
        }
    }
    private void word_ram_switch(byte mode)
    {
        unchecked
        {
            int i = default;
            ArrayPointer<ushort> ptr1 = new ArrayPointer<byte>(word_ram_2M).Cast<ushort>();
            ArrayPointer<ushort> ptr2 = word_ram[(int)(0)].Cast<ushort>();
            ArrayPointer<ushort> ptr3 = word_ram[(int)(1)].Cast<ushort>();
            if (((unchecked((int)(mode)) & 4) != 0)) {
            for (i = 0; (i < 65536); i++) {
            (ptr2++)[0] = (ptr1++)[0];
            (ptr3++)[0] = (ptr1++)[0];
            }
            for (i = (unchecked((int)(cd_boot)) + 32); (i < (unchecked((int)(cd_boot)) + 34)); i++) {
            memory_map[(int)(i)].read8 = null;
            memory_map[(int)(i)].read16 = null;
            memory_map[(int)(i)].write8 = null;
            memory_map[(int)(i)].write16 = null;
            zbank_memory_map[(int)(i)].read = null;
            zbank_memory_map[(int)(i)].write = null;
            }
            for (i = 12; (i < 14); i++) {
            sub_memory_map[(int)(i)].read8 = null;
            sub_memory_map[(int)(i)].read16 = null;
            sub_memory_map[(int)(i)].write8 = null;
            sub_memory_map[(int)(i)].write16 = null;
            }
            } else {
            for (i = 0; (i < 65536); i++) {
            (ptr1++)[0] = (ptr2++)[0];
            (ptr1++)[0] = (ptr3++)[0];
            }
            for (i = (unchecked((int)(cd_boot)) + 32); (i < (unchecked((int)(cd_boot)) + 34)); i++) {
            memory_map[(int)(i)].@base = (new ArrayPointer<byte>(word_ram_2M) + ((i & 3) << 16));
            }
            for (i = 12; (i < 14); i++) {
            sub_memory_map[(int)(i)].read8 = s68k_read_bus_8;
            sub_memory_map[(int)(i)].read16 = s68k_read_bus_16;
            sub_memory_map[(int)(i)].write8 = s68k_unused_8_w;
            sub_memory_map[(int)(i)].write16 = s68k_unused_16_w;
            }
            }
        }
    }
    private void scd_write_byte(uint address, uint data)
    {
        unchecked
        {
            if ((!((address & unchecked((uint)(32768))) != 0))) {
            if (((address & unchecked((uint)(1))) != 0)) {
            pcm_write(((address >> 1) & unchecked((uint)(8191))), unchecked((byte)(data)), unchecked((uint)(sub.cycles)));
            return;
            }
            s68k_unused_8_w(address, data);
            return;
            }
            switch ((address & unchecked((uint)(511)))) {
            case 0:
            {
            scd_regs[(int)((6 >> 1))].High = unchecked((byte)(data));
            return;
            }
            case 1:
            {
            if ((!((data & unchecked((uint)(1))) != 0))) {
            scd_reset(0);
            }
            return;
            }
            case 3:
            case 2:
            {
            s68k_poll_sync(unchecked((uint)((1 << 3))));
            if ((((data ^ unchecked((uint)(scd_regs[(int)((3 >> 1))].Low))) & unchecked((uint)(5))) != 0)) {
            int i = default;
            if (((data & unchecked((uint)(4))) != 0)) {
            if ((!((unchecked((int)(scd_regs[(int)((3 >> 1))].Low)) & 4) != 0))) {
            word_ram_switch(unchecked((byte)(4)));
            }
            if (((data & unchecked((uint)(1))) != 0)) {
            for (i = (unchecked((int)(cd_boot)) + 32); (i < (unchecked((int)(cd_boot)) + 34)); i++) {
            memory_map[(int)(i)].@base = (word_ram[(int)(1)] + ((i & 1) << 16));
            }
            for (i = (unchecked((int)(cd_boot)) + 34); (i < (unchecked((int)(cd_boot)) + 36)); i++) {
            memory_map[(int)(i)].read8 = cell_ram_1_read8;
            memory_map[(int)(i)].read16 = cell_ram_1_read16;
            memory_map[(int)(i)].write8 = cell_ram_1_write8;
            memory_map[(int)(i)].write16 = cell_ram_1_write16;
            zbank_memory_map[(int)(i)].read = cell_ram_1_read8;
            zbank_memory_map[(int)(i)].write = cell_ram_1_write8;
            }
            for (i = 8; (i < 12); i++) {
            sub_memory_map[(int)(i)].read8 = dot_ram_0_read8;
            sub_memory_map[(int)(i)].read16 = dot_ram_0_read16;
            sub_memory_map[(int)(i)].write8 = dot_ram_0_write8;
            sub_memory_map[(int)(i)].write16 = dot_ram_0_write16;
            }
            for (i = 12; (i < 14); i++) {
            sub_memory_map[(int)(i)].@base = (word_ram[(int)(0)] + ((i & 1) << 16));
            }
            dmna = unchecked((byte)(0));
            } else {
            for (i = (unchecked((int)(cd_boot)) + 32); (i < (unchecked((int)(cd_boot)) + 34)); i++) {
            memory_map[(int)(i)].@base = (word_ram[(int)(0)] + ((i & 1) << 16));
            }
            for (i = (unchecked((int)(cd_boot)) + 34); (i < (unchecked((int)(cd_boot)) + 36)); i++) {
            memory_map[(int)(i)].read8 = cell_ram_0_read8;
            memory_map[(int)(i)].read16 = cell_ram_0_read16;
            memory_map[(int)(i)].write8 = cell_ram_0_write8;
            memory_map[(int)(i)].write16 = cell_ram_0_write16;
            zbank_memory_map[(int)(i)].read = cell_ram_0_read8;
            zbank_memory_map[(int)(i)].write = cell_ram_0_write8;
            }
            for (i = 8; (i < 12); i++) {
            sub_memory_map[(int)(i)].read8 = dot_ram_1_read8;
            sub_memory_map[(int)(i)].read16 = dot_ram_1_read16;
            sub_memory_map[(int)(i)].write8 = dot_ram_1_write8;
            sub_memory_map[(int)(i)].write16 = dot_ram_1_write16;
            }
            for (i = 12; (i < 14); i++) {
            sub_memory_map[(int)(i)].@base = (word_ram[(int)(1)] + ((i & 1) << 16));
            }
            }
            scd_regs[(int)((2 >> 1))].Low = unchecked((byte)((unchecked((uint)((unchecked((int)(scd_regs[(int)((2 >> 1))].Low)) & (~31)))) | (data & unchecked((uint)(29))))));
            return;
            } else {
            if (((unchecked((int)(scd_regs[(int)((2 >> 1))].Low)) & 4) != 0)) {
            word_ram_switch(unchecked((byte)(0)));
            data |= unchecked((uint)(((~unchecked((int)(dmna))) & 1)));
            if ((!((data & unchecked((uint)(1))) != 0))) {
            for (i = (unchecked((int)(cd_boot)) + 32); (i < (unchecked((int)(cd_boot)) + 36)); i++) {
            memory_map[(int)(i)].read8 = m68k_read_bus_8;
            memory_map[(int)(i)].read16 = m68k_read_bus_16;
            memory_map[(int)(i)].write8 = m68k_unused_8_w;
            memory_map[(int)(i)].write16 = m68k_unused_16_w;
            zbank_memory_map[(int)(i)].read = zbank_unused_r;
            zbank_memory_map[(int)(i)].write = zbank_unused_w;
            }
            for (i = 8; (i < 12); i++) {
            sub_memory_map[(int)(i)].read8 = null;
            sub_memory_map[(int)(i)].read16 = null;
            sub_memory_map[(int)(i)].write8 = null;
            sub_memory_map[(int)(i)].write16 = null;
            }
            data |= unchecked((uint)(2));
            scd_regs[(int)((2 >> 1))].Low = unchecked((byte)((unchecked((uint)((unchecked((int)(scd_regs[(int)((2 >> 1))].Low)) & (~31)))) | (data & unchecked((uint)(31))))));
            return;
            }
            }
            if (((data & unchecked((uint)(1))) != 0)) {
            if (((unchecked((int)(scd_regs[(int)((88 >> 1))].High)) & 128) != 0)) {
            gfx_update(sub.cycles);
            }
            if ((cdc_dma == word_ram_2M_dma_callback)) {
            cdc_dma_update(unchecked((uint)(sub.cycles)));
            cdc_halted_dma = cdc_dma;
            cdc_dma = null;
            }
            dmna = unchecked((byte)(0));
            for (i = (unchecked((int)(cd_boot)) + 32); (i < (unchecked((int)(cd_boot)) + 36)); i++) {
            memory_map[(int)(i)].read8 = null;
            memory_map[(int)(i)].read16 = null;
            memory_map[(int)(i)].write8 = null;
            memory_map[(int)(i)].write16 = null;
            zbank_memory_map[(int)(i)].read = null;
            zbank_memory_map[(int)(i)].write = null;
            }
            for (i = 8; (i < 12); i++) {
            sub_memory_map[(int)(i)].read8 = s68k_lockup_r_8;
            sub_memory_map[(int)(i)].read16 = s68k_lockup_r_16;
            sub_memory_map[(int)(i)].write8 = s68k_lockup_w_8;
            sub_memory_map[(int)(i)].write16 = s68k_lockup_w_16;
            }
            scd_regs[(int)((2 >> 1))].Low = unchecked((byte)((unchecked((uint)((unchecked((int)(scd_regs[(int)((2 >> 1))].Low)) & (~31)))) | (data & unchecked((uint)(29))))));
            return;
            }
            }
            }
            scd_regs[(int)((2 >> 1))].Low = unchecked((byte)((unchecked((uint)((unchecked((int)(scd_regs[(int)((2 >> 1))].Low)) & (~28)))) | (data & unchecked((uint)(28))))));
            return;
            }
            case 4:
            {
            scd_regs[(int)((4 >> 1))].High = unchecked((byte)((data & unchecked((uint)(7)))));
            if ((cdc_dma != null)) {
            cdc_dma_update(unchecked((uint)(sub.cycles)));
            }
            cdc_dma_init();
            scd_regs[(int)((10 >> 1))].Word = unchecked((ushort)(0));
            return;
            }
            case 5:
            {
            scd_regs[(int)((4 >> 1))].Low = unchecked((byte)((data & unchecked((uint)(cdc_ar_mask)))));
            return;
            }
            case 7:
            {
            cdc_reg_w(unchecked((byte)(data)));
            return;
            }
            case 15:
            case 14:
            {
            s68k_poll_sync(unchecked((uint)((1 << 15))));
            scd_regs[(int)((15 >> 1))].Low = unchecked((byte)(data));
            return;
            }
            case 49:
            case 48:
            {
            if ((data != 0)) {
            timer = unchecked((int)(((data + unchecked((uint)(1))) * unchecked((uint)((384 * 4))))));
            timer += unchecked((int)((unchecked((uint)(sub.cycles)) - cd_cycles)));
            } else {
            timer = 0;
            }
            scd_regs[(int)((48 >> 1))].Low = unchecked((byte)(data));
            return;
            }
            case 51:
            {
            scd_regs[(int)((50 >> 1))].Low = unchecked((byte)(data));
            scd_regs[(int)(0)].High = unchecked((byte)((unchecked((uint)((unchecked((int)(scd_regs[(int)(0)].High)) & 127))) | ((data & unchecked((uint)(4))) << 5))));
            pending &= unchecked((byte)((unchecked((uint)(253)) | (data & unchecked((uint)(2))))));
            s68k_update_irq(((unchecked((uint)(pending)) & data) >> 1));
            return;
            }
            case 77:
            case 76:
            {
            scd_regs[(int)((76 >> 1))].Low = unchecked((byte)(data));
            break;
            }
            default:
            {
            ushort reg_16 = unchecked((ushort)((address & unchecked((uint)(510)))));
            if (((unchecked((int)(reg_16)) >= 32) && (unchecked((int)(reg_16)) <= 47))) {
            s68k_poll_sync(unchecked((uint)((1 << unchecked((int)(((address - unchecked((uint)(16))) & unchecked((uint)(31)))))))));
            } else if (((unchecked((int)(reg_16)) >= 16) && (unchecked((int)(reg_16)) <= 31))) {
            return;
            } else if (((((unchecked((int)(reg_16)) >= 8) && (unchecked((int)(reg_16)) <= 13)) || ((unchecked((int)(reg_16)) >= 52) && (unchecked((int)(reg_16)) <= 53))) || ((unchecked((int)(reg_16)) >= 90) && (unchecked((int)(reg_16)) <= 103)))) {
            scd_write_word(address, ((data << 8) | (data & unchecked((uint)(255)))));
            return;
            }
            if (((address & unchecked((uint)(1))) != 0)) {
            scd_regs[(int)((unchecked((int)(reg_16)) >> 1))].Low = unchecked((byte)(data));
            return;
            }
            scd_regs[(int)((unchecked((int)(reg_16)) >> 1))].High = unchecked((byte)(data));
            return;
            }
            }
        }
    }
    private void scd_write_word(uint address, uint data)
    {
        unchecked
        {
            if ((!((address & unchecked((uint)(32768))) != 0))) {
            pcm_write(((address >> 1) & unchecked((uint)(8191))), unchecked((byte)((data & unchecked((uint)(255))))), unchecked((uint)(sub.cycles)));
            return;
            }
            switch ((address & unchecked((uint)(510)))) {
            case 0:
            {
            scd_regs[(int)((6 >> 1))].High = unchecked((byte)((data >> 8)));
            if ((!((data & unchecked((uint)(1))) != 0))) {
            scd_reset(0);
            }
            return;
            }
            case 2:
            {
            s68k_poll_sync(unchecked((uint)((1 << 3))));
            if ((((data ^ unchecked((uint)(scd_regs[(int)((3 >> 1))].Low))) & unchecked((uint)(5))) != 0)) {
            int i = default;
            if (((data & unchecked((uint)(4))) != 0)) {
            if ((!((unchecked((int)(scd_regs[(int)((3 >> 1))].Low)) & 4) != 0))) {
            word_ram_switch(unchecked((byte)(4)));
            }
            if (((data & unchecked((uint)(1))) != 0)) {
            for (i = (unchecked((int)(cd_boot)) + 32); (i < (unchecked((int)(cd_boot)) + 34)); i++) {
            memory_map[(int)(i)].@base = (word_ram[(int)(1)] + ((i & 1) << 16));
            }
            for (i = (unchecked((int)(cd_boot)) + 34); (i < (unchecked((int)(cd_boot)) + 36)); i++) {
            memory_map[(int)(i)].read8 = cell_ram_1_read8;
            memory_map[(int)(i)].read16 = cell_ram_1_read16;
            memory_map[(int)(i)].write8 = cell_ram_1_write8;
            memory_map[(int)(i)].write16 = cell_ram_1_write16;
            zbank_memory_map[(int)(i)].read = cell_ram_1_read8;
            zbank_memory_map[(int)(i)].write = cell_ram_1_write8;
            }
            for (i = 8; (i < 12); i++) {
            sub_memory_map[(int)(i)].read8 = dot_ram_0_read8;
            sub_memory_map[(int)(i)].read16 = dot_ram_0_read16;
            sub_memory_map[(int)(i)].write8 = dot_ram_0_write8;
            sub_memory_map[(int)(i)].write16 = dot_ram_0_write16;
            }
            for (i = 12; (i < 14); i++) {
            sub_memory_map[(int)(i)].@base = (word_ram[(int)(0)] + ((i & 1) << 16));
            }
            dmna = unchecked((byte)(0));
            } else {
            for (i = (unchecked((int)(cd_boot)) + 32); (i < (unchecked((int)(cd_boot)) + 34)); i++) {
            memory_map[(int)(i)].@base = (word_ram[(int)(0)] + ((i & 1) << 16));
            }
            for (i = (unchecked((int)(cd_boot)) + 34); (i < (unchecked((int)(cd_boot)) + 36)); i++) {
            memory_map[(int)(i)].read8 = cell_ram_0_read8;
            memory_map[(int)(i)].read16 = cell_ram_0_read16;
            memory_map[(int)(i)].write8 = cell_ram_0_write8;
            memory_map[(int)(i)].write16 = cell_ram_0_write16;
            zbank_memory_map[(int)(i)].read = cell_ram_0_read8;
            zbank_memory_map[(int)(i)].write = cell_ram_0_write8;
            }
            for (i = 8; (i < 12); i++) {
            sub_memory_map[(int)(i)].read8 = dot_ram_1_read8;
            sub_memory_map[(int)(i)].read16 = dot_ram_1_read16;
            sub_memory_map[(int)(i)].write8 = dot_ram_1_write8;
            sub_memory_map[(int)(i)].write16 = dot_ram_1_write16;
            }
            for (i = 12; (i < 14); i++) {
            sub_memory_map[(int)(i)].@base = (word_ram[(int)(1)] + ((i & 1) << 16));
            }
            }
            scd_regs[(int)((3 >> 1))].Low = unchecked((byte)((unchecked((uint)((unchecked((int)(scd_regs[(int)((3 >> 1))].Low)) & (~31)))) | (data & unchecked((uint)(29))))));
            return;
            } else {
            if (((unchecked((int)(scd_regs[(int)((3 >> 1))].Low)) & 4) != 0)) {
            word_ram_switch(unchecked((byte)(0)));
            data |= unchecked((uint)(((~unchecked((int)(dmna))) & 1)));
            if ((!((data & unchecked((uint)(1))) != 0))) {
            for (i = (unchecked((int)(cd_boot)) + 32); (i < (unchecked((int)(cd_boot)) + 36)); i++) {
            memory_map[(int)(i)].read8 = m68k_read_bus_8;
            memory_map[(int)(i)].read16 = m68k_read_bus_16;
            memory_map[(int)(i)].write8 = m68k_unused_8_w;
            memory_map[(int)(i)].write16 = m68k_unused_16_w;
            zbank_memory_map[(int)(i)].read = zbank_unused_r;
            zbank_memory_map[(int)(i)].write = zbank_unused_w;
            }
            for (i = 8; (i < 12); i++) {
            sub_memory_map[(int)(i)].read8 = null;
            sub_memory_map[(int)(i)].read16 = null;
            sub_memory_map[(int)(i)].write8 = null;
            sub_memory_map[(int)(i)].write16 = null;
            }
            data |= unchecked((uint)(2));
            scd_regs[(int)((3 >> 1))].Low = unchecked((byte)((unchecked((uint)((unchecked((int)(scd_regs[(int)((3 >> 1))].Low)) & (~31)))) | (data & unchecked((uint)(31))))));
            return;
            }
            }
            if (((data & unchecked((uint)(1))) != 0)) {
            if (((unchecked((int)(scd_regs[(int)((88 >> 1))].High)) & 128) != 0)) {
            gfx_update(sub.cycles);
            }
            if ((cdc_dma == word_ram_2M_dma_callback)) {
            cdc_dma_update(unchecked((uint)(sub.cycles)));
            cdc_halted_dma = cdc_dma;
            cdc_dma = null;
            }
            dmna = unchecked((byte)(0));
            for (i = (unchecked((int)(cd_boot)) + 32); (i < (unchecked((int)(cd_boot)) + 36)); i++) {
            memory_map[(int)(i)].read8 = null;
            memory_map[(int)(i)].read16 = null;
            memory_map[(int)(i)].write8 = null;
            memory_map[(int)(i)].write16 = null;
            zbank_memory_map[(int)(i)].read = null;
            zbank_memory_map[(int)(i)].write = null;
            }
            for (i = 8; (i < 12); i++) {
            sub_memory_map[(int)(i)].read8 = s68k_lockup_r_8;
            sub_memory_map[(int)(i)].read16 = s68k_lockup_r_16;
            sub_memory_map[(int)(i)].write8 = s68k_lockup_w_8;
            sub_memory_map[(int)(i)].write16 = s68k_lockup_w_16;
            }
            scd_regs[(int)((3 >> 1))].Low = unchecked((byte)((unchecked((uint)((unchecked((int)(scd_regs[(int)((3 >> 1))].Low)) & (~31)))) | (data & unchecked((uint)(29))))));
            return;
            }
            }
            }
            scd_regs[(int)((3 >> 1))].Low = unchecked((byte)((unchecked((uint)((unchecked((int)(scd_regs[(int)((3 >> 1))].Low)) & (~28)))) | (data & unchecked((uint)(28))))));
            return;
            }
            case 4:
            {
            scd_regs[(int)((4 >> 1))].Word = unchecked((ushort)((data & unchecked((uint)((1792 | unchecked((int)(cdc_ar_mask))))))));
            if ((cdc_dma != null)) {
            cdc_dma_update(unchecked((uint)(sub.cycles)));
            }
            cdc_dma_init();
            scd_regs[(int)((10 >> 1))].Word = unchecked((ushort)(0));
            return;
            }
            case 6:
            {
            cdc_reg_w(unchecked((byte)(data)));
            return;
            }
            case 8:
            {
            cdc_host_r(unchecked((byte)(67)));
            return;
            }
            case 12:
            {
            int ticks = ((sub.cycles - stopwatch) / (384 * 4));
            stopwatch += (ticks * (384 * 4));
            scd_regs[(int)((12 >> 1))].Word = unchecked((ushort)(0));
            return;
            }
            case 14:
            {
            s68k_poll_sync(unchecked((uint)((1 << 15))));
            scd_regs[(int)((15 >> 1))].Low = unchecked((byte)((data & unchecked((uint)(255)))));
            return;
            }
            case 48:
            {
            data &= unchecked((uint)(255));
            if ((data != 0)) {
            timer = unchecked((int)(((data + unchecked((uint)(1))) * unchecked((uint)((384 * 4))))));
            timer += unchecked((int)((unchecked((uint)(sub.cycles)) - cd_cycles)));
            } else {
            timer = 0;
            }
            scd_regs[(int)((48 >> 1))].Low = unchecked((byte)(data));
            return;
            }
            case 50:
            {
            data &= unchecked((uint)(255));
            scd_regs[(int)((50 >> 1))].Low = unchecked((byte)(data));
            scd_regs[(int)(0)].High = unchecked((byte)((unchecked((uint)((unchecked((int)(scd_regs[(int)(0)].High)) & 127))) | ((data & unchecked((uint)(4))) << 5))));
            pending &= unchecked((byte)((unchecked((uint)(253)) | (data & unchecked((uint)(2))))));
            s68k_update_irq(((unchecked((uint)(pending)) & data) >> 1));
            return;
            }
            case 52:
            {
            if ((unchecked((int)(hardware_type)) == 1)) {
            data = ((data >> 8) & unchecked((uint)(127)));
            drive_fader[(int)(1)] = unchecked((ushort)(((unchecked((uint)(1024)) * data) / unchecked((uint)(127)))));
            } else if ((unchecked((int)(hardware_type)) == 2)) {
            data = (data >> 8);
            if (((data & unchecked((uint)(1))) != 0)) return;
            data = ((((((data * unchecked((uint)(2050))) & unchecked((uint)(139536))) | ((data * unchecked((uint)(32800))) & unchecked((uint)(558144)))) * unchecked((uint)(65793))) >> 16) & unchecked((uint)(127)));
            drive_fader[(int)(1)] = unchecked((ushort)(((unchecked((uint)(1024)) * (unchecked((uint)(127)) - data)) / unchecked((uint)(127)))));
            } else {
            drive_fader[(int)(1)] = unchecked((ushort)((data >> 4)));
            }
            return;
            }
            case 54:
            {
            scd_regs[(int)((55 >> 1))].Low = unchecked((byte)((data & unchecked((uint)(4)))));
            return;
            }
            case 74:
            {
            scd_regs[(int)((74 >> 1))].Word = unchecked((ushort)(0));
            cdd_process();
            break;
            }
            case 102:
            {
            scd_regs[(int)((102 >> 1))].Word = unchecked((ushort)(data));
            gfx_start(data, sub.cycles);
            return;
            }
            default:
            {
            if (((address & unchecked((uint)(496))) == unchecked((uint)(32)))) {
            s68k_poll_sync(unchecked((uint)((3 << unchecked((int)(((address - unchecked((uint)(16))) & unchecked((uint)(30)))))))));
            } else if (((address & unchecked((uint)(496))) == unchecked((uint)(16)))) {
            return;
            }
            scd_regs[(int)(((address >> 1) & unchecked((uint)(255))))].Word = unchecked((ushort)(data));
            return;
            }
            }
        }
    }
    private void scd_init()
    {
        unchecked
        {
            int i = default;
            int @base = unchecked((int)(cd_boot));
            cd_cart_init();
            for (i = (@base + 0); (i < (@base + 32)); i++) {
            switch ((i & 2)) {
            case 0:
            {
            memory_map[(int)(i)].@base = (new ArrayPointer<byte>(bootrom) + ((i & 1) << 16));
            memory_map[(int)(i)].read8 = null;
            memory_map[(int)(i)].read16 = null;
            memory_map[(int)(i)].write8 = m68k_unused_8_w;
            memory_map[(int)(i)].write16 = m68k_unused_16_w;
            zbank_memory_map[(int)(i)].read = null;
            zbank_memory_map[(int)(i)].write = zbank_unused_w;
            break;
            }
            case 2:
            {
            memory_map[(int)(i)].@base = (new ArrayPointer<byte>(prg_ram) + ((i & 1) << 16));
            if ((i > (@base + 3))) {
            memory_map[(int)(i)].read8 = prg_ram_m68k_read_byte;
            memory_map[(int)(i)].read16 = prg_ram_m68k_read_word;
            memory_map[(int)(i)].write8 = prg_ram_m68k_write_byte;
            memory_map[(int)(i)].write16 = prg_ram_m68k_write_word;
            zbank_memory_map[(int)(i)].read = prg_ram_z80_read_byte;
            zbank_memory_map[(int)(i)].write = prg_ram_z80_write_byte;
            } else {
            memory_map[(int)(i)].read8 = null;
            memory_map[(int)(i)].read16 = null;
            memory_map[(int)(i)].write8 = null;
            memory_map[(int)(i)].write16 = null;
            zbank_memory_map[(int)(i)].read = null;
            zbank_memory_map[(int)(i)].write = null;
            }
            break;
            }
            }
            }
            for (i = (@base + 32); (i < (@base + 64)); i++) {
            memory_map[(int)(i)].@base = (new ArrayPointer<byte>(word_ram_2M) + ((i & 3) << 16));
            if ((i > (@base + 35))) {
            memory_map[(int)(i)].read8 = word_ram_m68k_read_byte;
            memory_map[(int)(i)].read16 = word_ram_m68k_read_word;
            memory_map[(int)(i)].write8 = word_ram_m68k_write_byte;
            memory_map[(int)(i)].write16 = word_ram_m68k_write_word;
            zbank_memory_map[(int)(i)].read = word_ram_z80_read_byte;
            zbank_memory_map[(int)(i)].write = word_ram_z80_write_byte;
            } else {
            memory_map[(int)(i)].read8 = null;
            memory_map[(int)(i)].read16 = null;
            memory_map[(int)(i)].write8 = null;
            memory_map[(int)(i)].write16 = null;
            zbank_memory_map[(int)(i)].read = null;
            zbank_memory_map[(int)(i)].write = null;
            }
            }
            for (i = 0; (i < 256); i++) {
            switch ((i & 15)) {
            case 0:
            case 1:
            case 2:
            case 3:
            case 4:
            case 5:
            case 6:
            case 7:
            {
            sub_memory_map[(int)(i)].@base = (new ArrayPointer<byte>(prg_ram) + ((i & 7) << 16));
            sub_memory_map[(int)(i)].read8 = null;
            sub_memory_map[(int)(i)].read16 = null;
            sub_memory_map[(int)(i)].write8 = (((i & 14) != 0) ? null : prg_ram_write_byte);
            sub_memory_map[(int)(i)].write16 = (((i & 14) != 0) ? null : prg_ram_write_word);
            break;
            }
            case 8:
            case 9:
            case 10:
            case 11:
            {
            sub_memory_map[(int)(i)].@base = (new ArrayPointer<byte>(word_ram_2M) + ((i & 3) << 16));
            if ((i > 15)) {
            sub_memory_map[(int)(i)].read8 = word_ram_s68k_read_byte;
            sub_memory_map[(int)(i)].read16 = word_ram_s68k_read_word;
            sub_memory_map[(int)(i)].write8 = word_ram_s68k_write_byte;
            sub_memory_map[(int)(i)].write16 = word_ram_s68k_write_word;
            } else {
            sub_memory_map[(int)(i)].read8 = s68k_lockup_r_8;
            sub_memory_map[(int)(i)].read16 = s68k_lockup_r_16;
            sub_memory_map[(int)(i)].write8 = s68k_lockup_w_8;
            sub_memory_map[(int)(i)].write16 = s68k_lockup_w_16;
            }
            break;
            }
            case 12:
            case 13:
            {
            sub_memory_map[(int)(i)].@base = (new ArrayPointer<byte>(word_ram_2M) + ((i & 3) << 16));
            if ((i > 15)) {
            sub_memory_map[(int)(i)].read8 = word_ram_s68k_read_byte;
            sub_memory_map[(int)(i)].read16 = word_ram_s68k_read_word;
            sub_memory_map[(int)(i)].write8 = word_ram_s68k_write_byte;
            sub_memory_map[(int)(i)].write16 = word_ram_s68k_write_word;
            } else {
            sub_memory_map[(int)(i)].read8 = s68k_read_bus_8;
            sub_memory_map[(int)(i)].read16 = s68k_read_bus_16;
            sub_memory_map[(int)(i)].write8 = s68k_unused_8_w;
            sub_memory_map[(int)(i)].write16 = s68k_unused_16_w;
            }
            break;
            }
            case 14:
            {
            sub_memory_map[(int)(i)].@base = default(ArrayPointer<byte>);
            sub_memory_map[(int)(i)].read8 = bram_read_byte;
            sub_memory_map[(int)(i)].read16 = bram_read_word;
            sub_memory_map[(int)(i)].write8 = bram_write_byte;
            sub_memory_map[(int)(i)].write16 = bram_write_word;
            break;
            }
            case 15:
            {
            sub_memory_map[(int)(i)].@base = default(ArrayPointer<byte>);
            sub_memory_map[(int)(i)].read8 = scd_read_byte;
            sub_memory_map[(int)(i)].read16 = scd_read_word;
            sub_memory_map[(int)(i)].write8 = scd_write_byte;
            sub_memory_map[(int)(i)].write16 = scd_write_word;
            break;
            }
            }
            }
            cdc_init();
            gfx_init();
            cycles_per_line = unchecked((uint)((unchecked((float)(3420)) * (unchecked((float)(50000000)) / unchecked((float)(system_clock))))));
            ClearProgramRam();
            ClearWordRam();
            ClearWordRam2M();
            ClearBackupRam();
        }
    }
    private void scd_reset(int hard)
    {
        unchecked
        {
            if ((hard != 0)) {
            int i = default;
            ClearRegistersAll();
            dmna = unchecked((byte)(0));
            (memory_map[(int)(cd_boot)].@base + 112).Cast<ushort>()[0] = unchecked((ushort)(65535));
            (memory_map[(int)(cd_boot)].@base + 114).Cast<ushort>()[0] = unchecked((ushort)(65535));
            scd_regs[(int)((0 >> 1))].Word = unchecked((ushort)(2));
            scd_regs[(int)((2 >> 1))].Word = unchecked((ushort)(1));
            word_ram_switch(unchecked((byte)(0)));
            for (i = (unchecked((int)(cd_boot)) + 32); (i < (unchecked((int)(cd_boot)) + 36)); i++) {
            memory_map[(int)(i)].read8 = null;
            memory_map[(int)(i)].read16 = null;
            memory_map[(int)(i)].write8 = null;
            memory_map[(int)(i)].write16 = null;
            zbank_memory_map[(int)(i)].read = null;
            zbank_memory_map[(int)(i)].write = null;
            }
            for (i = 8; (i < 12); i++) {
            sub_memory_map[(int)(i)].read8 = s68k_lockup_r_8;
            sub_memory_map[(int)(i)].read16 = s68k_lockup_r_16;
            sub_memory_map[(int)(i)].write8 = s68k_lockup_w_8;
            sub_memory_map[(int)(i)].write16 = s68k_lockup_w_16;
            }
            memory_map[(int)((unchecked((int)(cd_boot)) + 2))].@base = new ArrayPointer<byte>(prg_ram);
            memory_map[(int)((unchecked((int)(cd_boot)) + 3))].@base = (new ArrayPointer<byte>(prg_ram) + 65536);
            memory_map[(int)((unchecked((int)(cd_boot)) + 2))].read8 = null;
            memory_map[(int)((unchecked((int)(cd_boot)) + 3))].read8 = null;
            memory_map[(int)((unchecked((int)(cd_boot)) + 2))].read16 = null;
            memory_map[(int)((unchecked((int)(cd_boot)) + 3))].read16 = null;
            memory_map[(int)((unchecked((int)(cd_boot)) + 2))].write8 = null;
            memory_map[(int)((unchecked((int)(cd_boot)) + 3))].write8 = null;
            memory_map[(int)((unchecked((int)(cd_boot)) + 2))].write16 = null;
            memory_map[(int)((unchecked((int)(cd_boot)) + 3))].write16 = null;
            zbank_memory_map[(int)((unchecked((int)(cd_boot)) + 2))].read = null;
            zbank_memory_map[(int)((unchecked((int)(cd_boot)) + 3))].read = null;
            zbank_memory_map[(int)((unchecked((int)(cd_boot)) + 2))].write = null;
            zbank_memory_map[(int)((unchecked((int)(cd_boot)) + 3))].write = null;
            sub.cycles = 0;
            s68k_pulse_reset();
            s68k_pulse_halt();
            cd_cycles = unchecked((uint)(0));
            } else {
            scd_regs[(int)((4 >> 1))].Word = unchecked((ushort)(0));
            scd_regs[(int)((12 >> 1))].Word = unchecked((ushort)(0));
            ClearRegistersSub();
            }
            scd_regs[(int)((8 >> 1))].Word = unchecked((ushort)(65535));
            scd_regs[(int)((10 >> 1))].Word = unchecked((ushort)(65535));
            scd_regs[(int)((54 >> 1))].Word = unchecked((ushort)(256));
            scd_regs[(int)((64 >> 1))].Word = unchecked((ushort)(15));
            scd_regs[(int)((66 >> 1))].Word = unchecked((ushort)(65535));
            scd_regs[(int)((68 >> 1))].Word = unchecked((ushort)(65535));
            scd_regs[(int)((70 >> 1))].Word = unchecked((ushort)(65535));
            scd_regs[(int)((72 >> 1))].Word = unchecked((ushort)(65535));
            scd_regs[(int)((74 >> 1))].Word = unchecked((ushort)(65535));
            scd_regs[(int)((6 >> 1))].Low = unchecked((byte)(1));
            timer = 0;
            stopwatch = sub.cycles;
            pending = unchecked((byte)(0));
            ClearMainPolling();
            ClearSubPolling();
            drive_cycles = ((cd_cycles - unchecked((uint)(sub.cycles))) * unchecked((uint)(3)));
            cdd_reset();
            cdc_reset();
            gfx_reset();
            pcm_reset();
        }
    }
    private void scd_update(uint cycles)
    {
        unchecked
        {
            int m68k_end_cycles = default;
            int s68k_run_cycles = default;
            int s68k_end_cycles = unchecked((int)((cd_cycles + cycles_per_line)));
            do {
            s68k_run_cycles = unchecked((int)((unchecked((uint)(s68k_end_cycles)) - cd_cycles)));
            if (((timer > 0) && (timer < s68k_run_cycles))) {
            s68k_run_cycles = timer;
            m68k_end_cycles = unchecked((int)((((cd_cycles + unchecked((uint)(s68k_run_cycles))) * unchecked((uint)(3420))) / cycles_per_line)));
            } else {
            m68k_end_cycles = unchecked((int)(cycles));
            }
            m68k_run(unchecked((uint)(m68k_end_cycles)));
            s68k_run((cd_cycles + unchecked((uint)(s68k_run_cycles))));
            cd_cycles += unchecked((uint)(s68k_run_cycles));
            drive_cycles += unchecked((uint)((s68k_run_cycles * 3)));
            if ((drive_cycles >= unchecked((uint)((500000 * 4))))) {
            drive_cycles -= unchecked((uint)((500000 * 4)));
            cdd_update();
            if (((unchecked((int)(scd_regs[(int)((55 >> 1))].Low)) & 4) != 0)) {
            pending |= unchecked((byte)((1 << 4)));
            if (((unchecked((int)(scd_regs[(int)((50 >> 1))].Low)) & 16) != 0)) {
            s68k_update_irq(unchecked((uint)(((unchecked((int)(pending)) & unchecked((int)(scd_regs[(int)((50 >> 1))].Low))) >> 1))));
            }
            }
            }
            if ((timer != 0)) {
            timer -= s68k_run_cycles;
            if ((timer <= 0)) {
            timer += ((unchecked((int)(scd_regs[(int)((48 >> 1))].Low)) + 1) * (384 * 4));
            if (((unchecked((int)(scd_regs[(int)((50 >> 1))].Low)) & 8) != 0)) {
            pending |= unchecked((byte)((1 << 3)));
            s68k_update_irq(unchecked((uint)(((unchecked((int)(pending)) & unchecked((int)(scd_regs[(int)((50 >> 1))].Low))) >> 1))));
            }
            }
            }
            } while (((unchecked((uint)(main.cycles)) < cycles) || (sub.cycles < s68k_end_cycles)));
            if ((cdc_dma != null)) {
            cdc_dma_update(cd_cycles);
            }
            if (((unchecked((int)(scd_regs[(int)((88 >> 1))].High)) & 128) != 0)) {
            gfx_update(unchecked((int)(cd_cycles)));
            }
        }
    }
    private void scd_end_frame(uint cycles)
    {
        unchecked
        {
            int ticks = unchecked((int)(((cycles - unchecked((uint)(stopwatch))) / unchecked((uint)((384 * 4))))));
            scd_regs[(int)((12 >> 1))].Word = unchecked((ushort)(((unchecked((int)(scd_regs[(int)((12 >> 1))].Word)) + ticks) & 4095)));
            stopwatch += unchecked((int)((unchecked((uint)((ticks * (384 * 4)))) - cycles)));
            sub.cycles -= unchecked((int)(cycles));
            gfx_cycles -= cycles;
            cdc_cycles[(int)(0)] -= unchecked((int)(cycles));
            cdc_cycles[(int)(1)] -= unchecked((int)(cycles));
            main.poll_cycle = unchecked((uint)(0));
            sub.poll_cycle = unchecked((uint)(0));
        }
    }
    private int scd_68k_irq_ack(int level)
    {
        unchecked
        {
            pending &= unchecked((byte)((~(1 << level))));
            if ((level == 2)) {
            scd_regs[(int)(0)].High &= unchecked((byte)((~1)));
            }
            s68k_update_irq(unchecked((uint)(((unchecked((int)(pending)) & unchecked((int)(scd_regs[(int)((50 >> 1))].Low))) >> 1))));
            return unchecked((int)(4294967295u));
        }
    }
    private void RestoreMemoryMap()
    {
        unchecked
        {
            int i = default;
            memory_map[(int)((unchecked((int)(cd_boot)) + 2))].@base = (new ArrayPointer<byte>(prg_ram) + ((unchecked((int)(scd_regs[(int)((3 >> 1))].Low)) & 192) << 11));
            memory_map[(int)((unchecked((int)(cd_boot)) + 3))].@base = (memory_map[(int)((unchecked((int)(cd_boot)) + 2))].@base + 65536);
            if (((unchecked((int)(scd_regs[(int)(0)].Low)) & 3) != 1)) {
            memory_map[(int)((unchecked((int)(cd_boot)) + 2))].read8 = (memory_map[(int)((unchecked((int)(cd_boot)) + 3))].read8 = null);
            memory_map[(int)((unchecked((int)(cd_boot)) + 2))].read16 = (memory_map[(int)((unchecked((int)(cd_boot)) + 3))].read16 = null);
            memory_map[(int)((unchecked((int)(cd_boot)) + 2))].write8 = (memory_map[(int)((unchecked((int)(cd_boot)) + 3))].write8 = null);
            memory_map[(int)((unchecked((int)(cd_boot)) + 2))].write16 = (memory_map[(int)((unchecked((int)(cd_boot)) + 3))].write16 = null);
            zbank_memory_map[(int)((unchecked((int)(cd_boot)) + 2))].read = (zbank_memory_map[(int)((unchecked((int)(cd_boot)) + 3))].read = null);
            zbank_memory_map[(int)((unchecked((int)(cd_boot)) + 2))].write = (zbank_memory_map[(int)((unchecked((int)(cd_boot)) + 3))].write = null);
            } else {
            memory_map[(int)((unchecked((int)(cd_boot)) + 2))].read8 = (memory_map[(int)((unchecked((int)(cd_boot)) + 3))].read8 = m68k_read_bus_8);
            memory_map[(int)((unchecked((int)(cd_boot)) + 2))].read16 = (memory_map[(int)((unchecked((int)(cd_boot)) + 3))].read16 = m68k_read_bus_16);
            memory_map[(int)((unchecked((int)(cd_boot)) + 2))].write8 = (memory_map[(int)((unchecked((int)(cd_boot)) + 3))].write8 = m68k_unused_8_w);
            memory_map[(int)((unchecked((int)(cd_boot)) + 2))].write16 = (memory_map[(int)((unchecked((int)(cd_boot)) + 3))].write16 = m68k_unused_16_w);
            zbank_memory_map[(int)((unchecked((int)(cd_boot)) + 2))].read = (zbank_memory_map[(int)((unchecked((int)(cd_boot)) + 3))].read = zbank_unused_r);
            zbank_memory_map[(int)((unchecked((int)(cd_boot)) + 2))].write = (zbank_memory_map[(int)((unchecked((int)(cd_boot)) + 3))].write = zbank_unused_w);
            }
            if (((unchecked((int)(scd_regs[(int)((3 >> 1))].Low)) & 4) != 0)) {
            if (((unchecked((int)(scd_regs[(int)((3 >> 1))].Low)) & 1) != 0)) {
            for (i = (unchecked((int)(cd_boot)) + 32); (i < (unchecked((int)(cd_boot)) + 34)); i++) {
            memory_map[(int)(i)].@base = (word_ram[(int)(1)] + ((i & 1) << 16));
            memory_map[(int)(i)].read8 = null;
            memory_map[(int)(i)].read16 = null;
            memory_map[(int)(i)].write8 = null;
            memory_map[(int)(i)].write16 = null;
            zbank_memory_map[(int)(i)].read = null;
            zbank_memory_map[(int)(i)].write = null;
            }
            for (i = (unchecked((int)(cd_boot)) + 34); (i < (unchecked((int)(cd_boot)) + 36)); i++) {
            memory_map[(int)(i)].read8 = cell_ram_1_read8;
            memory_map[(int)(i)].read16 = cell_ram_1_read16;
            memory_map[(int)(i)].write8 = cell_ram_1_write8;
            memory_map[(int)(i)].write16 = cell_ram_1_write16;
            zbank_memory_map[(int)(i)].read = cell_ram_1_read8;
            zbank_memory_map[(int)(i)].write = cell_ram_1_write8;
            }
            for (i = 8; (i < 12); i++) {
            sub_memory_map[(int)(i)].read8 = dot_ram_0_read8;
            sub_memory_map[(int)(i)].read16 = dot_ram_0_read16;
            sub_memory_map[(int)(i)].write8 = dot_ram_0_write8;
            sub_memory_map[(int)(i)].write16 = dot_ram_0_write16;
            }
            for (i = 12; (i < 14); i++) {
            sub_memory_map[(int)(i)].@base = (word_ram[(int)(0)] + ((i & 1) << 16));
            sub_memory_map[(int)(i)].read8 = null;
            sub_memory_map[(int)(i)].read16 = null;
            sub_memory_map[(int)(i)].write8 = null;
            sub_memory_map[(int)(i)].write16 = null;
            }
            } else {
            for (i = (unchecked((int)(cd_boot)) + 32); (i < (unchecked((int)(cd_boot)) + 34)); i++) {
            memory_map[(int)(i)].@base = (word_ram[(int)(0)] + ((i & 1) << 16));
            memory_map[(int)(i)].read8 = null;
            memory_map[(int)(i)].read16 = null;
            memory_map[(int)(i)].write8 = null;
            memory_map[(int)(i)].write16 = null;
            zbank_memory_map[(int)(i)].read = null;
            zbank_memory_map[(int)(i)].write = null;
            }
            for (i = (unchecked((int)(cd_boot)) + 34); (i < (unchecked((int)(cd_boot)) + 36)); i++) {
            memory_map[(int)(i)].read8 = cell_ram_0_read8;
            memory_map[(int)(i)].read16 = cell_ram_0_read16;
            memory_map[(int)(i)].write8 = cell_ram_0_write8;
            memory_map[(int)(i)].write16 = cell_ram_0_write16;
            zbank_memory_map[(int)(i)].read = cell_ram_0_read8;
            zbank_memory_map[(int)(i)].write = cell_ram_0_write8;
            }
            for (i = 8; (i < 12); i++) {
            sub_memory_map[(int)(i)].read8 = dot_ram_1_read8;
            sub_memory_map[(int)(i)].read16 = dot_ram_1_read16;
            sub_memory_map[(int)(i)].write8 = dot_ram_1_write8;
            sub_memory_map[(int)(i)].write16 = dot_ram_1_write16;
            }
            for (i = 12; (i < 14); i++) {
            sub_memory_map[(int)(i)].@base = (word_ram[(int)(1)] + ((i & 1) << 16));
            sub_memory_map[(int)(i)].read8 = null;
            sub_memory_map[(int)(i)].read16 = null;
            sub_memory_map[(int)(i)].write8 = null;
            sub_memory_map[(int)(i)].write16 = null;
            }
            }
            } else {
            if (((unchecked((int)(scd_regs[(int)((3 >> 1))].Low)) & 1) != 0)) {
            for (i = (unchecked((int)(cd_boot)) + 32); (i < (unchecked((int)(cd_boot)) + 36)); i++) {
            memory_map[(int)(i)].@base = (new ArrayPointer<byte>(word_ram_2M) + ((i & 3) << 16));
            memory_map[(int)(i)].read8 = null;
            memory_map[(int)(i)].read16 = null;
            memory_map[(int)(i)].write8 = null;
            memory_map[(int)(i)].write16 = null;
            zbank_memory_map[(int)(i)].read = null;
            zbank_memory_map[(int)(i)].write = null;
            }
            for (i = 8; (i < 12); i++) {
            sub_memory_map[(int)(i)].read8 = s68k_lockup_r_8;
            sub_memory_map[(int)(i)].read16 = s68k_lockup_r_16;
            sub_memory_map[(int)(i)].write8 = s68k_lockup_w_8;
            sub_memory_map[(int)(i)].write16 = s68k_lockup_w_16;
            }
            } else {
            for (i = (unchecked((int)(cd_boot)) + 32); (i < (unchecked((int)(cd_boot)) + 36)); i++) {
            memory_map[(int)(i)].@base = (new ArrayPointer<byte>(word_ram_2M) + ((i & 3) << 16));
            memory_map[(int)(i)].read8 = m68k_read_bus_8;
            memory_map[(int)(i)].read16 = m68k_read_bus_16;
            memory_map[(int)(i)].write8 = m68k_unused_8_w;
            memory_map[(int)(i)].write16 = m68k_unused_16_w;
            zbank_memory_map[(int)(i)].read = zbank_unused_r;
            zbank_memory_map[(int)(i)].write = zbank_unused_w;
            }
            for (i = 8; (i < 12); i++) {
            sub_memory_map[(int)(i)].read8 = null;
            sub_memory_map[(int)(i)].read16 = null;
            sub_memory_map[(int)(i)].write8 = null;
            sub_memory_map[(int)(i)].write16 = null;
            }
            }
            for (i = 12; (i < 14); i++) {
            sub_memory_map[(int)(i)].read8 = s68k_read_bus_8;
            sub_memory_map[(int)(i)].read16 = s68k_read_bus_16;
            sub_memory_map[(int)(i)].write8 = s68k_unused_8_w;
            sub_memory_map[(int)(i)].write16 = s68k_unused_16_w;
            }
            }
        }
    }
}
