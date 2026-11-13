using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cartridge;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cpu;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cd;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS;

internal sealed partial class SystemCore
{
    private void z80_unused_w(uint address, byte data)
    {
        unchecked
        {

        }
    }
    private byte z80_unused_r(uint address)
    {
        unchecked
        {
            return unchecked((byte)(255));
        }
    }
    private void z80_lockup_w(uint address, byte data)
    {
        unchecked
        {
            if ((!(config.force_dtack != 0))) {
            z80.cycles = 4294967295u;
            zstate = unchecked((byte)(0));
            }
        }
    }
    private byte z80_lockup_r(uint address)
    {
        unchecked
        {
            if ((!(config.force_dtack != 0))) {
            z80.cycles = 4294967295u;
            zstate = unchecked((byte)(0));
            }
            return unchecked((byte)(255));
        }
    }
    private void z80_request_68k_bus_access()
    {
        unchecked
        {
            if (((z80.cycles < dma_endCycles) && (unchecked((int)(dma_type)) < 2))) {
            z80.cycles = dma_endCycles;
            if ((dma_length != 0)) {
            zstate |= unchecked((byte)(4));
            }
            }
            z80.cycles += unchecked((uint)((3 * 15)));
        }
    }
    private byte z80_memory_r(uint address)
    {
        unchecked
        {
            switch (((address >> 13) & unchecked((uint)(7)))) {
            case 0:
            case 1:
            {
            return zram[(int)((address & unchecked((uint)(8191))))];
            }
            case 2:
            {
            return unchecked((byte)(fm_read(z80.cycles, (address & unchecked((uint)(3))))));
            }
            case 3:
            {
            if (((address >> 8) == unchecked((uint)(127)))) {
            z80_request_68k_bus_access();
            return unchecked((byte)(zbank_memory_map[(int)(192)].read(address)));
            }
            return z80_unused_r(address);
            }
            default:
            {
            z80_request_68k_bus_access();
            address = (zbank | (address & unchecked((uint)(32767))));
            if ((zbank_memory_map[(int)((address >> 16))].read != null)) {
            return unchecked((byte)(zbank_memory_map[(int)((address >> 16))].read(address)));
            }
            return memory_map[(int)((address >> 16))].@base[(int)(((address & unchecked((uint)(65535))) ^ unchecked((uint)(1))))];
            }
            }
        }
    }
    private void z80_memory_w(uint address, byte data)
    {
        unchecked
        {
            switch (((address >> 13) & unchecked((uint)(7)))) {
            case 0:
            case 1:
            {
            zram[(int)((address & unchecked((uint)(8191))))] = data;
            return;
            }
            case 2:
            {
            fm_write(z80.cycles, (address & unchecked((uint)(3))), unchecked((uint)(data)));
            return;
            }
            case 3:
            {
            switch ((address >> 8)) {
            case 96:
            {
            gen_zbank_w(unchecked((uint)((unchecked((int)(data)) & 1))));
            return;
            }
            case 127:
            {
            z80_request_68k_bus_access();
            zbank_memory_map[(int)(192)].write(address, unchecked((uint)(data)));
            return;
            }
            default:
            {
            z80_unused_w(address, data);
            return;
            }
            }
            }
            default:
            {
            z80_request_68k_bus_access();
            address = (zbank | (address & unchecked((uint)(32767))));
            if ((zbank_memory_map[(int)((address >> 16))].write != null)) {
            zbank_memory_map[(int)((address >> 16))].write(address, unchecked((uint)(data)));
            return;
            }
            memory_map[(int)((address >> 16))].@base[(int)(((address & unchecked((uint)(65535))) ^ unchecked((uint)(1))))] = unchecked((byte)((unchecked((int)(data)) & 255)));
            return;
            }
            }
        }
    }
    private byte z80_unused_port_r(uint port)
    {
        unchecked
        {
            if ((unchecked((int)(system_hw)) == 32)) {
            uint address = unchecked((uint)(((z80.pc.Word - 1) & 65535)));
            return z80_readmap[(int)((address >> 10))][(int)((address & unchecked((uint)(1023))))];
            }
            return unchecked((byte)(255));
        }
    }
    private void z80_unused_port_w(uint port, byte data)
    {
        unchecked
        {

        }
    }
    private void z80_md_port_w(uint port, byte data)
    {
        unchecked
        {
            switch ((port & unchecked((uint)(193)))) {
            case 1:
            {
            io_z80_write(unchecked((uint)(1)), unchecked((uint)(data)), (z80.cycles + unchecked((uint)(560))));
            return;
            }
            case 64:
            case 65:
            {
            psg_write(z80.cycles, unchecked((uint)(data)));
            return;
            }
            case 128:
            {
            vdp_z80_data_w(unchecked((uint)(data)));
            return;
            }
            case 129:
            {
            vdp_z80_ctrl_w(unchecked((uint)(data)));
            return;
            }
            default:
            {
            port &= unchecked((uint)(255));
            if (((port >= unchecked((uint)(240))) && ((unchecked((int)(ym2413)) & 1) != 0))) {
            fm_write(z80.cycles, port, unchecked((uint)(data)));
            return;
            }
            z80_unused_port_w(port, data);
            return;
            }
            }
        }
    }
    private byte z80_md_port_r(uint port)
    {
        unchecked
        {
            switch ((port & unchecked((uint)(193)))) {
            case 64:
            {
            return unchecked((byte)(((vdp_hvc_r((z80.cycles - unchecked((uint)(15)))) >> 8) & unchecked((uint)(255)))));
            }
            case 65:
            {
            return unchecked((byte)((vdp_hvc_r((z80.cycles - unchecked((uint)(15)))) & unchecked((uint)(255)))));
            }
            case 128:
            {
            return unchecked((byte)(vdp_z80_data_r()));
            }
            case 129:
            {
            return unchecked((byte)(vdp_z80_ctrl_r(z80.cycles)));
            }
            default:
            {
            port &= unchecked((uint)(255));
            if (((((port == unchecked((uint)(192))) || (port == unchecked((uint)(193)))) || (port == unchecked((uint)(220)))) || (port == unchecked((uint)(221))))) {
            return unchecked((byte)(io_z80_read((port & unchecked((uint)(1))))));
            }
            if (((port >= unchecked((uint)(240))) && ((unchecked((int)(ym2413)) & 1) != 0))) {
            return unchecked((byte)(fm_read(z80.cycles, port)));
            }
            return z80_unused_port_r(port);
            }
            }
        }
    }
    private void z80_gg_port_w(uint port, byte data)
    {
        unchecked
        {
            switch ((port & unchecked((uint)(193)))) {
            case 0:
            case 1:
            {
            port &= unchecked((uint)(255));
            if ((port < unchecked((uint)(7)))) {
            if ((unchecked((int)(system_hw)) == 64)) {
            io_gg_write(port, unchecked((uint)(data)));
            return;
            }
            } else if (((port == unchecked((uint)(62))) || (port == unchecked((uint)(63))))) {
            io_z80_write((port & unchecked((uint)(1))), unchecked((uint)(data)), (z80.cycles + unchecked((uint)(530))));
            return;
            }
            z80_unused_port_w(port, data);
            return;
            }
            case 64:
            case 65:
            {
            psg_write(z80.cycles, unchecked((uint)(data)));
            return;
            }
            case 128:
            {
            vdp_z80_data_w(unchecked((uint)(data)));
            return;
            }
            case 129:
            {
            vdp_sms_ctrl_w(unchecked((uint)(data)));
            return;
            }
            default:
            {
            z80_unused_port_w((port & unchecked((uint)(255))), data);
            return;
            }
            }
        }
    }
    private byte z80_gg_port_r(uint port)
    {
        unchecked
        {
            switch ((port & unchecked((uint)(193)))) {
            case 0:
            case 1:
            {
            port &= unchecked((uint)(255));
            if ((port < unchecked((uint)(7)))) {
            if ((unchecked((int)(system_hw)) == 64)) {
            return unchecked((byte)(io_gg_read(port)));
            }
            }
            return z80_unused_port_r(port);
            }
            case 64:
            {
            return unchecked((byte)(((vdp_hvc_r(z80.cycles) >> 8) & unchecked((uint)(255)))));
            }
            case 65:
            {
            return unchecked((byte)((vdp_hvc_r(z80.cycles) & unchecked((uint)(255)))));
            }
            case 128:
            {
            return unchecked((byte)(vdp_z80_data_r()));
            }
            case 129:
            {
            return unchecked((byte)(vdp_z80_ctrl_r(z80.cycles)));
            }
            default:
            {
            port &= unchecked((uint)(255));
            if (((((port == unchecked((uint)(192))) || (port == unchecked((uint)(193)))) || (port == unchecked((uint)(220)))) || (port == unchecked((uint)(221))))) {
            return unchecked((byte)(io_z80_read((port & unchecked((uint)(1))))));
            }
            return z80_unused_port_r(port);
            }
            }
        }
    }
    private void z80_ms_port_w(uint port, byte data)
    {
        unchecked
        {
            switch ((port & unchecked((uint)(193)))) {
            case 0:
            case 1:
            {
            if (((unchecked((int)(region_code)) != 0) || ((port & unchecked((uint)(254))) == unchecked((uint)(62))))) {
            io_z80_write((port & unchecked((uint)(1))), unchecked((uint)(data)), (z80.cycles + unchecked((uint)(530))));
            return;
            }
            z80_unused_port_w((port & unchecked((uint)(255))), data);
            return;
            }
            case 64:
            case 65:
            {
            psg_write(z80.cycles, unchecked((uint)(data)));
            return;
            }
            case 128:
            {
            vdp_z80_data_w(unchecked((uint)(data)));
            return;
            }
            case 129:
            {
            vdp_sms_ctrl_w(unchecked((uint)(data)));
            return;
            }
            default:
            {
            if (((unchecked((int)(ym2413)) & 1) != 0)) {
            if ((unchecked((int)(region_code)) == 0)) {
            port &= unchecked((uint)(255));
            if (((port == unchecked((uint)(240))) || (port == unchecked((uint)(241))))) {
            fm_write(z80.cycles, port, unchecked((uint)(data)));
            return;
            }
            if ((port == unchecked((uint)(242)))) {
            psg_config(z80.cycles, psg_preamp, unchecked((uint)(((((unchecked((int)(data)) + 1) & 2) != 0) ? 0 : 255))));
            fm_write(z80.cycles, unchecked((uint)(2)), unchecked((uint)(data)));
            io_reg[(int)(6)] = data;
            return;
            }
            } else if ((!((port & unchecked((uint)(4))) != 0))) {
            fm_write(z80.cycles, port, unchecked((uint)(data)));
            return;
            }
            }
            z80_unused_port_w((port & unchecked((uint)(255))), data);
            return;
            }
            }
        }
    }
    private byte z80_ms_port_r(uint port)
    {
        unchecked
        {
            switch ((port & unchecked((uint)(193)))) {
            case 0:
            case 1:
            {
            return z80_unused_port_r((port & unchecked((uint)(255))));
            }
            case 64:
            {
            return unchecked((byte)(((vdp_hvc_r(z80.cycles) >> 8) & unchecked((uint)(255)))));
            }
            case 65:
            {
            return unchecked((byte)((vdp_hvc_r(z80.cycles) & unchecked((uint)(255)))));
            }
            case 128:
            {
            return unchecked((byte)(vdp_z80_data_r()));
            }
            case 129:
            {
            return unchecked((byte)(vdp_z80_ctrl_r(z80.cycles)));
            }
            default:
            {
            if ((unchecked((int)(region_code)) == 0)) {
            port &= unchecked((uint)(255));
            if ((port == unchecked((uint)(242)))) {
            return unchecked((byte)((unchecked((int)(io_reg[(int)(6)])) & 3)));
            }
            if (((((port == unchecked((uint)(192))) || (port == unchecked((uint)(193)))) || (port == unchecked((uint)(220)))) || (port == unchecked((uint)(221))))) {
            if ((!((unchecked((int)(io_reg[(int)(14)])) & 4) != 0))) {
            return unchecked((byte)(io_z80_read((port & unchecked((uint)(1))))));
            }
            }
            return z80_unused_port_r(port);
            } else {
            byte data = unchecked((byte)(255));
            if (((!((port & unchecked((uint)(4))) != 0)) && ((unchecked((int)(ym2413)) & 1) != 0))) {
            data = unchecked((byte)(fm_read(z80.cycles, port)));
            }
            if ((!((unchecked((int)(io_reg[(int)(14)])) & 4) != 0))) {
            data &= unchecked((byte)(io_z80_read((port & unchecked((uint)(1))))));
            }
            return data;
            }
            }
            }
        }
    }
    private void z80_m3_port_w(uint port, byte data)
    {
        unchecked
        {
            switch ((port & unchecked((uint)(193)))) {
            case 0:
            case 1:
            {
            z80_unused_port_w((port & unchecked((uint)(255))), data);
            return;
            }
            case 64:
            case 65:
            {
            psg_write(z80.cycles, unchecked((uint)(data)));
            return;
            }
            case 128:
            {
            vdp_z80_data_w(unchecked((uint)(data)));
            return;
            }
            case 129:
            {
            vdp_sms_ctrl_w(unchecked((uint)(data)));
            return;
            }
            default:
            {
            if (((!((port & unchecked((uint)(4))) != 0)) && ((unchecked((int)(ym2413)) & 1) != 0))) {
            fm_write(z80.cycles, port, unchecked((uint)(data)));
            if (((port & unchecked((uint)(2))) != 0)) {
            psg_config(z80.cycles, psg_preamp, unchecked((uint)((((unchecked((int)(data)) & 1) != 0) ? 0 : 255))));
            }
            return;
            }
            z80_unused_port_w((port & unchecked((uint)(255))), data);
            return;
            }
            }
        }
    }
    private byte z80_m3_port_r(uint port)
    {
        unchecked
        {
            switch ((port & unchecked((uint)(193)))) {
            case 0:
            case 1:
            {
            return z80_unused_port_r((port & unchecked((uint)(255))));
            }
            case 64:
            {
            return unchecked((byte)(((vdp_hvc_r(z80.cycles) >> 8) & unchecked((uint)(255)))));
            }
            case 65:
            {
            return unchecked((byte)((vdp_hvc_r(z80.cycles) & unchecked((uint)(255)))));
            }
            case 128:
            {
            return unchecked((byte)(vdp_z80_data_r()));
            }
            case 129:
            {
            return unchecked((byte)(vdp_z80_ctrl_r(z80.cycles)));
            }
            default:
            {
            if (((!((port & unchecked((uint)(4))) != 0)) && ((unchecked((int)(ym2413)) & 1) != 0))) {
            return unchecked((byte)(fm_read(z80.cycles, port)));
            }
            return unchecked((byte)(io_z80_read((port & unchecked((uint)(1))))));
            }
            }
        }
    }
    private void z80_sg_port_w(uint port, byte data)
    {
        unchecked
        {
            switch ((port & unchecked((uint)(193)))) {
            case 64:
            case 65:
            {
            psg_write(z80.cycles, unchecked((uint)(data)));
            z80.cycles += unchecked((uint)((32 * 15)));
            return;
            }
            case 128:
            {
            vdp_z80_data_w(unchecked((uint)(data)));
            return;
            }
            case 129:
            {
            vdp_tms_ctrl_w(unchecked((uint)(data)));
            return;
            }
            default:
            {
            z80_unused_port_w((port & unchecked((uint)(255))), data);
            return;
            }
            }
        }
    }
    private byte z80_sg_port_r(uint port)
    {
        unchecked
        {
            switch ((port & unchecked((uint)(193)))) {
            case 128:
            {
            return unchecked((byte)(vdp_z80_data_r()));
            }
            case 129:
            {
            return unchecked((byte)(vdp_z80_ctrl_r(z80.cycles)));
            }
            case 192:
            case 193:
            {
            return unchecked((byte)(io_z80_read((port & unchecked((uint)(1))))));
            }
            default:
            {
            return z80_unused_port_r((port & unchecked((uint)(255))));
            }
            }
        }
    }
}
