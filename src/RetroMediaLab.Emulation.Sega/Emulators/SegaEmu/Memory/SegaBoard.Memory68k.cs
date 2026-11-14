using System.IO;
using RetroMediaLab.Chips.Common.Memory;
using RetroMediaLab.Chips.CPU.Motorola.M68000.Execution;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Instruction;
using RetroMediaLab.Chips.Controllers.Audio.YX5200;
using RetroMediaLab.Chips.Controllers.Sega.Input;
using RetroMediaLab.Chips.Video.Sega.VDP;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Cartridge;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Common.Audio;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.MegaCd.Hardware;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Media;
using BlipBuffer = RetroMediaLab.Chips.Audio.Common.Resampling.BandLimitedBuffer;
using Cdd = RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Media.CdDrive;
using Pcm = RetroMediaLab.Chips.Audio.Ricoh.RF5C164.Rf5c164;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Common;

internal abstract partial class SegaBoard
{
    private uint m68k_read_bus_8(uint address)
    {
        unchecked
        {
            address = (main.pc | (address & unchecked((uint)(1))));
            return unchecked((uint)(memory_map[(int)(((address >> 16) & unchecked((uint)(255))))].@base[(int)(((address & unchecked((uint)(65535))) ^ unchecked((uint)(1))))]));
        }
    }
    private uint m68k_read_bus_16(uint address)
    {
        unchecked
        {
            address = main.pc;
            return unchecked((uint)((memory_map[(int)(((address >> 16) & unchecked((uint)(255))))].@base + unchecked((int)((address & unchecked((uint)(65534)))))).Cast<ushort>()[0]));
        }
    }
    private void m68k_unused_8_w(uint address, uint data)
    {
        unchecked
        {

        }
    }
    private void m68k_unused_16_w(uint address, uint data)
    {
        unchecked
        {

        }
    }
    private void m68k_lockup_w_8(uint address, uint data)
    {
        unchecked
        {
            if ((!(config.force_dtack != 0))) {
            m68k_pulse_halt();
            main.cycles = unchecked((int)(main.cycle_end));
            }
        }
    }
    private void m68k_lockup_w_16(uint address, uint data)
    {
        unchecked
        {
            if ((!(config.force_dtack != 0))) {
            m68k_pulse_halt();
            main.cycles = unchecked((int)(main.cycle_end));
            }
        }
    }
    private uint m68k_lockup_r_8(uint address)
    {
        unchecked
        {
            if ((!(config.force_dtack != 0))) {
            m68k_pulse_halt();
            main.cycles = unchecked((int)(main.cycle_end));
            }
            address = (main.pc | (address & unchecked((uint)(1))));
            return unchecked((uint)(memory_map[(int)(((address >> 16) & unchecked((uint)(255))))].@base[(int)(((address & unchecked((uint)(65535))) ^ unchecked((uint)(1))))]));
        }
    }
    private uint m68k_lockup_r_16(uint address)
    {
        unchecked
        {
            if ((!(config.force_dtack != 0))) {
            m68k_pulse_halt();
            main.cycles = unchecked((int)(main.cycle_end));
            }
            address = main.pc;
            return unchecked((uint)((memory_map[(int)(((address >> 16) & unchecked((uint)(255))))].@base + unchecked((int)((address & unchecked((uint)(65534)))))).Cast<ushort>()[0]));
        }
    }
    private uint z80_read_byte(uint address)
    {
        unchecked
        {
            main.cycles += (1 * 7);
            switch (((address >> 13) & unchecked((uint)(3)))) {
            case 2:
            {
            return fm_read(unchecked((uint)(main.cycles)), (address & unchecked((uint)(3))));
            }
            case 3:
            {
            if (((address & unchecked((uint)(65280))) == unchecked((uint)(32512)))) {
            return m68k_lockup_r_8(address);
            }
            return (m68k_read_bus_8(address) | unchecked((uint)(255)));
            }
            default:
            {
            return unchecked((uint)(zram[(int)((address & unchecked((uint)(8191))))]));
            }
            }
        }
    }
    private uint z80_read_word(uint address)
    {
        unchecked
        {
            uint data = z80_read_byte(address);
            return (data | (data << 8));
        }
    }
    private void z80_write_byte(uint address, uint data)
    {
        unchecked
        {
            main.cycles += (1 * 7);
            switch (((address >> 13) & unchecked((uint)(3)))) {
            case 2:
            {
            fm_write(unchecked((uint)(main.cycles)), (address & unchecked((uint)(3))), data);
            return;
            }
            case 3:
            {
            switch (((address >> 8) & unchecked((uint)(127)))) {
            case 96:
            {
            gen_zbank_w((data & unchecked((uint)(1))));
            return;
            }
            case 127:
            {
            m68k_lockup_w_8(address, data);
            return;
            }
            default:
            {
            m68k_unused_8_w(address, data);
            return;
            }
            }
            }
            default:
            {
            zram[(int)((address & unchecked((uint)(8191))))] = unchecked((byte)(data));
            return;
            }
            }
        }
    }
    private void z80_write_word(uint address, uint data)
    {
        unchecked
        {
            z80_write_byte(address, (data >> 8));
        }
    }
    private void m68k_poll_detect(uint reg_mask)
    {
        unchecked
        {
            if (((main.poll_detected & reg_mask) != 0)) {
            if ((unchecked((uint)(main.cycles)) <= main.poll_cycle)) {
            if ((main.pc == main.poll_pc)) {
            if (((main.poll_detected & unchecked((uint)(1))) != 0)) {
            main.cycles = unchecked((int)(main.cycle_end));
            main.stopped = reg_mask;
            } else {
            main.poll_detected |= unchecked((uint)(1));
            main.poll_cycle = unchecked((uint)((main.cycles + 840)));
            }
            }
            return;
            }
            } else {
            main.poll_detected = reg_mask;
            }
            main.poll_cycle = unchecked((uint)((main.cycles + 840)));
            main.poll_pc = main.pc;
        }
    }
    private void m68k_poll_sync(uint reg_mask)
    {
        unchecked
        {
            uint cycles = ((unchecked((uint)(main.cycles)) * cd_cycles_per_line) / unchecked((uint)(3420)));
            if ((!(sub.stopped != 0))) {
            if ((!(sub.cycle_end != 0))) {
            s68k_run(cycles);
            }
            }
            if (((sub.stopped & reg_mask) != 0)) {
            sub.cycles = unchecked((int)((cycles + ((m68k_cycles() * cd_cycles_per_line) / unchecked((uint)(3420))))));
            sub.stopped = unchecked((uint)(0));
            }
            sub.poll_detected &= (~reg_mask);
            main.poll_detected &= (~reg_mask);
        }
    }
    private void s68k_sync()
    {
        unchecked
        {
            if ((!(sub.stopped != 0))) {
            if ((!(sub.cycle_end != 0))) {
            uint cycles = ((unchecked((uint)(main.cycles)) * cd_cycles_per_line) / unchecked((uint)(3420)));
            s68k_run(cycles);
            }
            }
        }
    }
    private uint ctrl_io_read_byte(uint address)
    {
        unchecked
        {
            switch (((address >> 8) & unchecked((uint)(255)))) {
            case 0:
            {
            if ((!((address & unchecked((uint)(224))) != 0))) {
            return io_68k_read(((address >> 1) & unchecked((uint)(15))));
            }
            return m68k_read_bus_8(address);
            }
            case 17:
            {
            if ((!((address & unchecked((uint)(1))) != 0))) {
            address = main.pc;
            if ((unchecked((int)(zstate)) == 3)) {
            return unchecked((uint)((unchecked((int)(memory_map[(int)(((address >> 16) & unchecked((uint)(255))))].@base[(int)(((address & unchecked((uint)(65535))) ^ unchecked((uint)(1))))])) & 254)));
            }
            return unchecked((uint)((unchecked((int)(memory_map[(int)(((address >> 16) & unchecked((uint)(255))))].@base[(int)(((address & unchecked((uint)(65535))) ^ unchecked((uint)(1))))])) | 1)));
            }
            return m68k_read_bus_8(address);
            }
            case 32:
            {
            if ((unchecked((int)(system_hw)) == 132)) {
            byte index = unchecked((byte)((address & unchecked((uint)(63)))));
            if ((unchecked((int)(index)) == 3)) {
            m68k_poll_detect(unchecked((uint)((1 << 3))));
            return unchecked((uint)((unchecked((int)(scd_regs[(int)((3 >> 1))].Low)) & 199)));
            }
            if ((unchecked((int)(index)) == 4)) {
            s68k_sync();
            return unchecked((uint)((unchecked((int)(scd_regs[(int)((4 >> 1))].High)) & 199)));
            }
            if ((unchecked((int)(index)) == 5)) {
            return unchecked((uint)(0));
            }
            if ((unchecked((int)(index)) == 15)) {
            s68k_sync();
            m68k_poll_detect(unchecked((uint)((1 << 15))));
            return unchecked((uint)(scd_regs[(int)((15 >> 1))].Low));
            }
            if ((unchecked((int)(index)) == 0)) {
            s68k_sync();
            return unchecked((uint)(scd_regs[(int)((0 >> 1))].High));
            }
            if ((unchecked((int)(index)) < 48)) {
            if ((unchecked((int)(index)) >= 32)) {
            m68k_poll_detect(unchecked((uint)((1 << (unchecked((int)(index)) - 16)))));
            }
            if (((address & unchecked((uint)(1))) != 0)) {
            return unchecked((uint)(scd_regs[(int)((unchecked((int)(index)) >> 1))].Low));
            }
            return unchecked((uint)(scd_regs[(int)((unchecked((int)(index)) >> 1))].High));
            }
            }
            return m68k_read_bus_8(address);
            }
            case 48:
            {
            if ((cart.hw.time_r != null)) {
            uint data = cart.hw.time_r(address);
            if (((address & unchecked((uint)(1))) != 0)) {
            return (data & unchecked((uint)(255)));
            }
            return (data >> 8);
            }
            return m68k_read_bus_8(address);
            }
            case 65:
            {
            if ((((unchecked((int)(config.bios)) & 1) != 0) && ((address & unchecked((uint)(1))) != 0))) {
            uint data = (gen_bankswitch_r() & unchecked((uint)(1)));
            address = main.pc;
            data |= unchecked((uint)((unchecked((int)(memory_map[(int)(((address >> 16) & unchecked((uint)(255))))].@base[(int)(((address & unchecked((uint)(65535))) ^ unchecked((uint)(1))))])) & 254)));
            return data;
            }
            return m68k_read_bus_8(address);
            }
            case 80:
            {
            if (((address & unchecked((uint)(252))) == unchecked((uint)(0)))) {
            uint data = ReadSvpExchange();
            return (((address & unchecked((uint)(1))) != 0) ? (data & unchecked((uint)(255))) : (data >> 8));
            }
            if (((address & unchecked((uint)(254))) == unchecked((uint)(4)))) {
            uint data = ReadSvpStatus();
            return (((address & unchecked((uint)(1))) != 0) ? (data & unchecked((uint)(255))) : (data >> 8));
            }
            return m68k_read_bus_8(address);
            }
            case 16:
            case 18:
            case 19:
            case 64:
            case 68:
            {
            return m68k_read_bus_8(address);
            }
            default:
            {
            return m68k_lockup_r_8(address);
            }
            }
        }
    }
    private uint ctrl_io_read_word(uint address)
    {
        unchecked
        {
            switch (((address >> 8) & unchecked((uint)(255)))) {
            case 0:
            {
            if ((!((address & unchecked((uint)(224))) != 0))) {
            uint data = io_68k_read(((address >> 1) & unchecked((uint)(15))));
            return ((data << 8) | data);
            }
            return m68k_read_bus_16(address);
            }
            case 17:
            {
            address = main.pc;
            if ((unchecked((int)(zstate)) == 3)) {
            return unchecked((uint)((unchecked((int)((memory_map[(int)(((address >> 16) & unchecked((uint)(255))))].@base + unchecked((int)((address & unchecked((uint)(65534)))))).Cast<ushort>()[0])) & 65279)));
            }
            return unchecked((uint)((unchecked((int)((memory_map[(int)(((address >> 16) & unchecked((uint)(255))))].@base + unchecked((int)((address & unchecked((uint)(65534)))))).Cast<ushort>()[0])) | 256)));
            }
            case 32:
            {
            if ((unchecked((int)(system_hw)) == 132)) {
            byte index = unchecked((byte)((address & unchecked((uint)(63)))));
            if ((unchecked((int)(index)) == 2)) {
            m68k_poll_detect(unchecked((uint)((1 << 3))));
            return unchecked((uint)((unchecked((int)(scd_regs[(int)((3 >> 1))].Word)) & 65479)));
            }
            if ((unchecked((int)(index)) == 8)) {
            if ((!((unchecked((int)(scd_regs[(int)((4 >> 1))].High)) & 64) != 0))) {
            s68k_sync();
            }
            return unchecked((uint)(cdc_host_r(unchecked((byte)(66)))));
            }
            if ((unchecked((int)(index)) == 6)) {
            return unchecked((uint)((memory_map[(int)(cd_boot)].@base + 114).Cast<ushort>()[0]));
            }
            if ((unchecked((int)(index)) == 12)) {
            uint cycles = ((unchecked((uint)(main.cycles)) * cd_cycles_per_line) / unchecked((uint)(3420)));
            return ((unchecked((uint)(scd_regs[(int)((12 >> 1))].Word)) + ((cycles - unchecked((uint)(cd_stopwatch))) / unchecked((uint)((384 * 4))))) & unchecked((uint)(4095)));
            }
            if ((unchecked((int)(index)) == 4)) {
            return unchecked((uint)((unchecked((int)(scd_regs[(int)((unchecked((int)(index)) >> 1))].High)) << 8)));
            }
            if ((unchecked((int)(index)) < 48)) {
            if ((unchecked((int)(index)) >= 32)) {
            s68k_sync();
            m68k_poll_detect(unchecked((uint)((3 << (unchecked((int)(index)) - 16)))));
            }
            return unchecked((uint)(scd_regs[(int)((unchecked((int)(index)) >> 1))].Word));
            }
            }
            return m68k_read_bus_16(address);
            }
            case 48:
            {
            if ((cart.hw.time_r != null)) {
            return cart.hw.time_r(address);
            }
            return m68k_read_bus_16(address);
            }
            case 80:
            {
            if (((address & unchecked((uint)(252))) == unchecked((uint)(0)))) {
            return ReadSvpExchange();
            }
            if (((address & unchecked((uint)(254))) == unchecked((uint)(4)))) {
            uint data = ReadSvpStatus();
            return data;
            }
            return m68k_read_bus_16(address);
            }
            case 16:
            case 18:
            case 19:
            case 64:
            case 65:
            case 68:
            {
            return m68k_read_bus_16(address);
            }
            default:
            {
            return m68k_lockup_r_16(address);
            }
            }
        }
    }
    private void ctrl_io_write_byte(uint address, uint data)
    {
        unchecked
        {
            switch (((address >> 8) & unchecked((uint)(255)))) {
            case 0:
            {
            if (((address & unchecked((uint)(225))) == unchecked((uint)(1)))) {
            io_68k_write(((address >> 1) & unchecked((uint)(15))), data);
            return;
            }
            m68k_unused_8_w(address, data);
            return;
            }
            case 17:
            {
            if ((!((address & unchecked((uint)(1))) != 0))) {
            gen_zbusreq_w((data & unchecked((uint)(1))), unchecked((uint)(main.cycles)));
            return;
            }
            m68k_unused_8_w(address, data);
            return;
            }
            case 18:
            {
            if ((!((address & unchecked((uint)(1))) != 0))) {
            gen_zreset_w((data & unchecked((uint)(1))), unchecked((uint)(main.cycles)));
            return;
            }
            m68k_unused_8_w(address, data);
            return;
            }
            case 32:
            {
            if ((unchecked((int)(system_hw)) == 132)) {
            switch ((address & unchecked((uint)(63)))) {
            case 0:
            {
            if (((data & unchecked((uint)(1))) != 0)) {
            if (((unchecked((int)(scd_regs[(int)((50 >> 1))].Low)) & 4) != 0)) {
            m68k_poll_sync(4294967288u);
            scd_regs[(int)(0)].High |= unchecked((byte)(1));
            cd_pending |= unchecked((byte)((1 << 2)));
            s68k_update_irq(unchecked((uint)(((unchecked((int)(cd_pending)) & unchecked((int)(scd_regs[(int)((50 >> 1))].Low))) >> 1))));
            }
            }
            return;
            }
            case 1:
            {
            uint halted = sub.stopped;
            if (((data & unchecked((uint)(1))) != 0)) {
            if ((!((unchecked((int)(scd_regs[(int)(0)].Low)) & 1) != 0))) {
            s68k_pulse_reset();
            }
            if (((data & unchecked((uint)(2))) != 0)) {
            s68k_pulse_halt();
            } else {
            s68k_clear_halt();
            }
            scd_regs[(int)(0)].Low = unchecked((byte)((data & unchecked((uint)(3)))));
            } else {
            s68k_pulse_halt();
            scd_regs[(int)(0)].Low = unchecked((byte)(2));
            }
            if (((sub.stopped & unchecked((uint)(1))) != 0)) {
            scd_regs[(int)(0)].Low &= unchecked((byte)((~2)));
            }
            if ((sub.stopped != halted)) {
            uint @base = unchecked((uint)((unchecked((int)(cd_boot)) + 2)));
            if (((unchecked((int)(scd_regs[(int)(0)].Low)) & 2) != 0)) {
            memory_map[(int)(@base)].read8 = (memory_map[(int)((@base + unchecked((uint)(1))))].read8 = null);
            memory_map[(int)(@base)].read16 = (memory_map[(int)((@base + unchecked((uint)(1))))].read16 = null);
            memory_map[(int)(@base)].write8 = (memory_map[(int)((@base + unchecked((uint)(1))))].write8 = null);
            memory_map[(int)(@base)].write16 = (memory_map[(int)((@base + unchecked((uint)(1))))].write16 = null);
            zbank_memory_map[(int)(@base)].read = (zbank_memory_map[(int)((@base + unchecked((uint)(1))))].read = null);
            zbank_memory_map[(int)(@base)].write = (zbank_memory_map[(int)((@base + unchecked((uint)(1))))].write = null);
            if ((cdc_dma == prg_ram_dma_w)) {
            cdc_dma_update(((unchecked((uint)(main.cycles)) * cd_cycles_per_line) / unchecked((uint)(3420))));
            cdc_halted_dma = cdc_dma;
            cdc_dma = null;
            }
            } else {
            memory_map[(int)(@base)].read8 = (memory_map[(int)((@base + unchecked((uint)(1))))].read8 = m68k_read_bus_8);
            memory_map[(int)(@base)].read16 = (memory_map[(int)((@base + unchecked((uint)(1))))].read16 = m68k_read_bus_16);
            memory_map[(int)(@base)].write8 = (memory_map[(int)((@base + unchecked((uint)(1))))].write8 = m68k_unused_8_w);
            memory_map[(int)(@base)].write16 = (memory_map[(int)((@base + unchecked((uint)(1))))].write16 = m68k_unused_16_w);
            zbank_memory_map[(int)(@base)].read = (zbank_memory_map[(int)((@base + unchecked((uint)(1))))].read = zbank_unused_r);
            zbank_memory_map[(int)(@base)].write = (zbank_memory_map[(int)((@base + unchecked((uint)(1))))].write = zbank_unused_w);
            if ((cdc_halted_dma == prg_ram_dma_w)) {
            uint cycles = ((unchecked((uint)(main.cycles)) * cd_cycles_per_line) / unchecked((uint)(3420)));
            cdc_dma = prg_ram_dma_w;
            cdc_halted_dma = null;
            if ((unchecked((uint)(cdc_cycles[(int)(0)])) < cycles)) {
            cdc_cycles[(int)(0)] = unchecked((int)(cycles));
            }
            }
            }
            }
            return;
            }
            case 2:
            {
            scd_regs[(int)((2 >> 1))].High = unchecked((byte)(data));
            return;
            }
            case 3:
            {
            m68k_poll_sync(unchecked((uint)((1 << 3))));
            memory_map[(int)((unchecked((int)(cd_boot)) + 2))].@base = (cd_program_ram + unchecked((int)(((data & unchecked((uint)(192))) << 11))));
            memory_map[(int)((unchecked((int)(cd_boot)) + 3))].@base = (memory_map[(int)((unchecked((int)(cd_boot)) + 2))].@base + 65536);
            if (((unchecked((int)(scd_regs[(int)((3 >> 1))].Low)) & 4) != 0)) {
            if (((data & unchecked((uint)(2))) != 0)) {
            cd_dmna = unchecked((byte)(1));
            } else {
            data |= unchecked((uint)(2));
            scd_regs[(int)((3 >> 1))].Low = unchecked((byte)((unchecked((uint)((unchecked((int)(scd_regs[(int)((3 >> 1))].Low)) & (~194)))) | (data & unchecked((uint)(194))))));
            return;
            }
            } else {
            if (((data & unchecked((uint)(2))) != 0)) {
            int i = default;
            cd_dmna = unchecked((byte)(1));
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
            scd_regs[(int)((3 >> 1))].Low = unchecked((byte)((unchecked((uint)((unchecked((int)(scd_regs[(int)((3 >> 1))].Low)) & (~195)))) | (data & unchecked((uint)(194))))));
            if (((sub.stopped & unchecked((uint)(4))) != 0)) {
            sub.cycles = unchecked((int)(((unchecked((uint)(main.cycles)) * cd_cycles_per_line) / unchecked((uint)(3420)))));
            s68k_clear_wait();
            }
            if (((unchecked((int)(scd_regs[(int)((88 >> 1))].High)) & 128) != 0)) {
            uint cycles = ((unchecked((uint)(main.cycles)) * cd_cycles_per_line) / unchecked((uint)(3420)));
            if ((gfx_cycles < cycles)) {
            gfx_cycles = cycles;
            }
            }
            if ((cdc_halted_dma == word_ram_2M_dma_w)) {
            uint cycles = ((unchecked((uint)(main.cycles)) * cd_cycles_per_line) / unchecked((uint)(3420)));
            cdc_dma = word_ram_2M_dma_w;
            cdc_halted_dma = null;
            if ((unchecked((uint)(cdc_cycles[(int)(0)])) < cycles)) {
            cdc_cycles[(int)(0)] = unchecked((int)(cycles));
            }
            }
            return;
            }
            }
            scd_regs[(int)((3 >> 1))].Low = unchecked((byte)((unchecked((uint)((unchecked((int)(scd_regs[(int)((2 >> 1))].Low)) & (~192)))) | (data & unchecked((uint)(192))))));
            return;
            }
            case 14:
            case 15:
            {
            m68k_poll_sync(unchecked((uint)((1 << 14))));
            scd_regs[(int)((14 >> 1))].High = unchecked((byte)(data));
            return;
            }
            default:
            {
            if (((address & unchecked((uint)(48))) == unchecked((uint)(16)))) {
            m68k_poll_sync(unchecked((uint)((1 << unchecked((int)((address & unchecked((uint)(31)))))))));
            if (((address & unchecked((uint)(1))) != 0)) {
            scd_regs[(int)(((address >> 1) & unchecked((uint)(255))))].Low = unchecked((byte)(data));
            return;
            }
            scd_regs[(int)(((address >> 1) & unchecked((uint)(255))))].High = unchecked((byte)(data));
            return;
            }
            m68k_unused_8_w(address, data);
            return;
            }
            }
            }
            m68k_unused_8_w(address, data);
            return;
            }
            case 48:
            {
            cart.hw.time_w(address, data);
            return;
            }
            case 65:
            {
            if ((((unchecked((int)(config.bios)) & 1) != 0) && ((address & unchecked((uint)(1))) != 0))) {
            gen_bankswitch_w((data & unchecked((uint)(1))));
            return;
            }
            m68k_unused_8_w(address, data);
            return;
            }
            case 16:
            case 19:
            case 64:
            case 68:
            case 80:
            {
            m68k_unused_8_w(address, data);
            return;
            }
            default:
            {
            m68k_lockup_w_8(address, data);
            return;
            }
            }
        }
    }
    private void ctrl_io_write_word(uint address, uint data)
    {
        unchecked
        {
            switch (((address >> 8) & unchecked((uint)(255)))) {
            case 0:
            {
            if ((!((address & unchecked((uint)(224))) != 0))) {
            io_68k_write(((address >> 1) & unchecked((uint)(15))), (data & unchecked((uint)(255))));
            return;
            }
            m68k_unused_16_w(address, data);
            return;
            }
            case 17:
            {
            gen_zbusreq_w(((data >> 8) & unchecked((uint)(1))), unchecked((uint)(main.cycles)));
            return;
            }
            case 18:
            {
            gen_zreset_w(((data >> 8) & unchecked((uint)(1))), unchecked((uint)(main.cycles)));
            return;
            }
            case 32:
            {
            if ((unchecked((int)(system_hw)) == 132)) {
            switch ((address & unchecked((uint)(62)))) {
            case 0:
            {
            uint halted = sub.stopped;
            if (((data & unchecked((uint)(1))) != 0)) {
            if ((!((unchecked((int)(scd_regs[(int)(0)].Low)) & 1) != 0))) {
            s68k_pulse_reset();
            }
            if (((data & unchecked((uint)(2))) != 0)) {
            s68k_pulse_halt();
            } else {
            s68k_clear_halt();
            }
            scd_regs[(int)(0)].Low = unchecked((byte)((data & unchecked((uint)(3)))));
            } else {
            s68k_pulse_halt();
            scd_regs[(int)(0)].Low = unchecked((byte)(2));
            }
            if (((sub.stopped & unchecked((uint)(1))) != 0)) {
            scd_regs[(int)(0)].Low &= unchecked((byte)((~2)));
            }
            if ((sub.stopped != halted)) {
            uint @base = unchecked((uint)((unchecked((int)(cd_boot)) + 2)));
            if (((unchecked((int)(scd_regs[(int)(0)].Low)) & 2) != 0)) {
            memory_map[(int)(@base)].read8 = (memory_map[(int)((@base + unchecked((uint)(1))))].read8 = null);
            memory_map[(int)(@base)].read16 = (memory_map[(int)((@base + unchecked((uint)(1))))].read16 = null);
            memory_map[(int)(@base)].write8 = (memory_map[(int)((@base + unchecked((uint)(1))))].write8 = null);
            memory_map[(int)(@base)].write16 = (memory_map[(int)((@base + unchecked((uint)(1))))].write16 = null);
            zbank_memory_map[(int)(@base)].read = (zbank_memory_map[(int)((@base + unchecked((uint)(1))))].read = null);
            zbank_memory_map[(int)(@base)].write = (zbank_memory_map[(int)((@base + unchecked((uint)(1))))].write = null);
            if ((cdc_dma == prg_ram_dma_w)) {
            cdc_dma_update(((unchecked((uint)(main.cycles)) * cd_cycles_per_line) / unchecked((uint)(3420))));
            cdc_halted_dma = cdc_dma;
            cdc_dma = null;
            }
            } else {
            memory_map[(int)(@base)].read8 = (memory_map[(int)((@base + unchecked((uint)(1))))].read8 = m68k_read_bus_8);
            memory_map[(int)(@base)].read16 = (memory_map[(int)((@base + unchecked((uint)(1))))].read16 = m68k_read_bus_16);
            memory_map[(int)(@base)].write8 = (memory_map[(int)((@base + unchecked((uint)(1))))].write8 = m68k_unused_8_w);
            memory_map[(int)(@base)].write16 = (memory_map[(int)((@base + unchecked((uint)(1))))].write16 = m68k_unused_16_w);
            zbank_memory_map[(int)(@base)].read = (zbank_memory_map[(int)((@base + unchecked((uint)(1))))].read = zbank_unused_r);
            zbank_memory_map[(int)(@base)].write = (zbank_memory_map[(int)((@base + unchecked((uint)(1))))].write = zbank_unused_w);
            if ((cdc_halted_dma == prg_ram_dma_w)) {
            uint cycles = ((unchecked((uint)(main.cycles)) * cd_cycles_per_line) / unchecked((uint)(3420)));
            cdc_dma = prg_ram_dma_w;
            cdc_halted_dma = null;
            if ((unchecked((uint)(cdc_cycles[(int)(0)])) < cycles)) {
            cdc_cycles[(int)(0)] = unchecked((int)(cycles));
            }
            }
            }
            }
            if (((data & unchecked((uint)(256))) != 0)) {
            if (((unchecked((int)(scd_regs[(int)((50 >> 1))].Low)) & 4) != 0)) {
            scd_regs[(int)(0)].High |= unchecked((byte)(1));
            cd_pending |= unchecked((byte)((1 << 2)));
            s68k_update_irq(unchecked((uint)(((unchecked((int)(cd_pending)) & unchecked((int)(scd_regs[(int)((50 >> 1))].Low))) >> 1))));
            }
            }
            return;
            }
            case 2:
            {
            m68k_poll_sync(unchecked((uint)((1 << 3))));
            memory_map[(int)((unchecked((int)(cd_boot)) + 2))].@base = (cd_program_ram + unchecked((int)(((data & unchecked((uint)(192))) << 11))));
            memory_map[(int)((unchecked((int)(cd_boot)) + 3))].@base = (memory_map[(int)((unchecked((int)(cd_boot)) + 2))].@base + 65536);
            if (((unchecked((int)(scd_regs[(int)((3 >> 1))].Low)) & 4) != 0)) {
            if (((data & unchecked((uint)(2))) != 0)) {
            cd_dmna = unchecked((byte)(1));
            } else {
            data |= unchecked((uint)(2));
            scd_regs[(int)((2 >> 1))].Word = unchecked((ushort)((unchecked((uint)((unchecked((int)(scd_regs[(int)((2 >> 1))].Word)) & (~65474)))) | (data & unchecked((uint)(65474))))));
            return;
            }
            } else {
            if (((data & unchecked((uint)(2))) != 0)) {
            int i = default;
            cd_dmna = unchecked((byte)(1));
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
            scd_regs[(int)((2 >> 1))].Word = unchecked((ushort)((unchecked((uint)((unchecked((int)(scd_regs[(int)((2 >> 1))].Word)) & (~65475)))) | (data & unchecked((uint)(65474))))));
            if (((sub.stopped & unchecked((uint)(4))) != 0)) {
            sub.cycles = unchecked((int)(((unchecked((uint)(main.cycles)) * cd_cycles_per_line) / unchecked((uint)(3420)))));
            s68k_clear_wait();
            }
            if (((unchecked((int)(scd_regs[(int)((88 >> 1))].High)) & 128) != 0)) {
            uint cycles = ((unchecked((uint)(main.cycles)) * cd_cycles_per_line) / unchecked((uint)(3420)));
            if ((gfx_cycles < cycles)) {
            gfx_cycles = cycles;
            }
            }
            if ((cdc_halted_dma == word_ram_2M_dma_w)) {
            uint cycles = ((unchecked((uint)(main.cycles)) * cd_cycles_per_line) / unchecked((uint)(3420)));
            cdc_dma = word_ram_2M_dma_w;
            cdc_halted_dma = null;
            if ((unchecked((uint)(cdc_cycles[(int)(0)])) < cycles)) {
            cdc_cycles[(int)(0)] = unchecked((int)(cycles));
            }
            }
            return;
            }
            }
            scd_regs[(int)((2 >> 1))].Word = unchecked((ushort)((unchecked((uint)((unchecked((int)(scd_regs[(int)((2 >> 1))].Word)) & (~65472)))) | (data & unchecked((uint)(65472))))));
            return;
            }
            case 6:
            {
            (memory_map[(int)(cd_boot)].@base + 114).Cast<ushort>()[0] = unchecked((ushort)(data));
            return;
            }
            case 8:
            {
            cdc_host_r(unchecked((byte)(66)));
            return;
            }
            case 14:
            {
            m68k_poll_sync(unchecked((uint)((1 << 14))));
            scd_regs[(int)((14 >> 1))].High = unchecked((byte)((data & unchecked((uint)(255)))));
            return;
            }
            default:
            {
            if (((address & unchecked((uint)(48))) == unchecked((uint)(16)))) {
            m68k_poll_sync(unchecked((uint)((3 << unchecked((int)((address & unchecked((uint)(30)))))))));
            scd_regs[(int)(((address >> 1) & unchecked((uint)(255))))].Word = unchecked((ushort)(data));
            return;
            }
            m68k_unused_16_w(address, data);
            return;
            }
            }
            }
            m68k_unused_16_w(address, data);
            return;
            }
            case 48:
            {
            cart.hw.time_w(address, data);
            return;
            }
            case 64:
            {
            if (((unchecked((int)(config.bios)) & 1) != 0)) {
            gen_tmss_w((address & unchecked((uint)(3))), data);
            return;
            }
            m68k_unused_16_w(address, data);
            return;
            }
            case 80:
            {
            if ((!((address & unchecked((uint)(253))) != 0))) {
            WriteSvpExchange(data);
            return;
            }
            m68k_unused_16_w(address, data);
            return;
            }
            case 16:
            case 19:
            case 65:
            case 68:
            {
            m68k_unused_16_w(address, data);
            return;
            }
            default:
            {
            m68k_lockup_w_16(address, data);
            return;
            }
            }
        }
    }
    private uint vdp_read_byte(uint address)
    {
        unchecked
        {
            switch ((address & unchecked((uint)(253)))) {
            case 0:
            {
            return (vdp_68k_data_r() >> 8);
            }
            case 1:
            {
            return (vdp_68k_data_r() & unchecked((uint)(255)));
            }
            case 4:
            {
            uint data = ((vdp_68k_ctrl_r(unchecked((uint)(main.cycles))) >> 8) & unchecked((uint)(3)));
            address = main.pc;
            data |= unchecked((uint)((unchecked((int)(memory_map[(int)(((address >> 16) & unchecked((uint)(255))))].@base[(int)(((address & unchecked((uint)(65535))) ^ unchecked((uint)(1))))])) & 252)));
            return data;
            }
            case 5:
            {
            return (vdp_68k_ctrl_r(unchecked((uint)(main.cycles))) & unchecked((uint)(255)));
            }
            case 8:
            case 12:
            {
            return (vdp_hvc_r(unchecked((uint)(main.cycles))) >> 8);
            }
            case 9:
            case 13:
            {
            return (vdp_hvc_r(unchecked((uint)(main.cycles))) & unchecked((uint)(255)));
            }
            case 24:
            case 25:
            case 28:
            case 29:
            {
            return m68k_read_bus_8(address);
            }
            default:
            {
            return m68k_lockup_r_8(address);
            }
            }
        }
    }
    private uint vdp_read_word(uint address)
    {
        unchecked
        {
            switch ((address & unchecked((uint)(252)))) {
            case 0:
            {
            return vdp_68k_data_r();
            }
            case 4:
            {
            uint data = (vdp_68k_ctrl_r(unchecked((uint)(main.cycles))) & unchecked((uint)(1023)));
            address = main.pc;
            data |= unchecked((uint)((unchecked((int)((memory_map[(int)(((address >> 16) & unchecked((uint)(255))))].@base + unchecked((int)((address & unchecked((uint)(65535)))))).Cast<ushort>()[0])) & 64512)));
            return data;
            }
            case 8:
            case 12:
            {
            return vdp_hvc_r(unchecked((uint)(main.cycles)));
            }
            case 24:
            case 28:
            {
            return m68k_read_bus_16(address);
            }
            default:
            {
            return m68k_lockup_r_16(address);
            }
            }
        }
    }
    private void vdp_write_byte(uint address, uint data)
    {
        unchecked
        {
            switch ((address & unchecked((uint)(252)))) {
            case 0:
            {
            vdp_68k_data_w(((data << 8) | data));
            return;
            }
            case 4:
            {
            vdp_68k_ctrl_w(((data << 8) | data));
            return;
            }
            case 16:
            case 20:
            {
            if (((address & unchecked((uint)(1))) != 0)) {
            psg_write(unchecked((uint)(main.cycles)), data);
            return;
            }
            m68k_unused_8_w(address, data);
            return;
            }
            case 24:
            {
            m68k_unused_8_w(address, data);
            return;
            }
            case 28:
            {
            vdp_test_w(((data << 8) | data));
            return;
            }
            default:
            {
            m68k_lockup_w_8(address, data);
            return;
            }
            }
        }
    }
    private void vdp_write_word(uint address, uint data)
    {
        unchecked
        {
            switch ((address & unchecked((uint)(252)))) {
            case 0:
            {
            vdp_68k_data_w(data);
            return;
            }
            case 4:
            {
            vdp_68k_ctrl_w(data);
            return;
            }
            case 16:
            case 20:
            {
            psg_write(unchecked((uint)(main.cycles)), (data & unchecked((uint)(255))));
            return;
            }
            case 24:
            {
            m68k_unused_16_w(address, data);
            return;
            }
            case 28:
            {
            vdp_test_w(data);
            return;
            }
            default:
            {
            m68k_lockup_w_16(address, data);
            return;
            }
            }
        }
    }
    private uint pico_read_byte(uint address)
    {
        unchecked
        {
            switch ((address & unchecked((uint)(255)))) {
            case 1:
            {
            return unchecked((uint)((unchecked((int)(region_code)) >> 1)));
            }
            case 3:
            {
            return unchecked((uint)((~unchecked((int)(input_pad[(int)(0)])))));
            }
            case 5:
            {
            return unchecked((uint)((unchecked((int)(input_analog[(int)(0)])) >> 8)));
            }
            case 7:
            {
            return unchecked((uint)((unchecked((int)(input_analog[(int)(0)])) & 255)));
            }
            case 9:
            {
            return unchecked((uint)((unchecked((int)(input_analog[(int)(1)])) >> 8)));
            }
            case 11:
            {
            return unchecked((uint)((unchecked((int)(input_analog[(int)(1)])) & 255)));
            }
            case 13:
            {
            return unchecked((uint)(((1 << unchecked((int)(pico_current))) - 1)));
            }
            case 16:
            case 17:
            {
            return unchecked((uint)(255));
            }
            case 18:
            {
            return unchecked((uint)(128));
            }
            default:
            {
            return m68k_read_bus_8(address);
            }
            }
        }
    }
    private uint pico_read_word(uint address)
    {
        unchecked
        {
            return (pico_read_byte((address | unchecked((uint)(1)))) | (pico_read_byte(address) << 8));
        }
    }
}
