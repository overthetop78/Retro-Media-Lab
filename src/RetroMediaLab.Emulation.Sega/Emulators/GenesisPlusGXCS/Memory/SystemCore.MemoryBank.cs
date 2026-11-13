using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cartridge;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cpu;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cd;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS;

internal sealed partial class SystemCore
{
    private uint zbank_unused_r(uint address)
    {
        unchecked
        {
            return unchecked((uint)(255));
        }
    }
    private void zbank_unused_w(uint address, uint data)
    {
        unchecked
        {

        }
    }
    private uint zbank_lockup_r(uint address)
    {
        unchecked
        {
            if ((!(config.force_dtack != 0))) {
            z80.cycles = 4294967295u;
            zstate = unchecked((byte)(0));
            }
            return unchecked((uint)(255));
        }
    }
    private void zbank_lockup_w(uint address, uint data)
    {
        unchecked
        {
            if ((!(config.force_dtack != 0))) {
            z80.cycles = 4294967295u;
            zstate = unchecked((byte)(0));
            }
        }
    }
    private uint zbank_read_ctrl_io(uint address)
    {
        unchecked
        {
            switch (((address >> 8) & unchecked((uint)(255)))) {
            case 0:
            {
            if ((!((address & unchecked((uint)(224))) != 0))) {
            return io_68k_read(((address >> 1) & unchecked((uint)(15))));
            }
            return zbank_unused_r(address);
            }
            case 17:
            {
            if (((address & unchecked((uint)(1))) != 0)) {
            return zbank_unused_r(address);
            }
            return unchecked((uint)(255));
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
            return zbank_unused_r(address);
            }
            case 65:
            {
            if (((address & unchecked((uint)(1))) != 0)) {
            return (gen_bankswitch_r() | unchecked((uint)(254)));
            }
            return zbank_unused_r(address);
            }
            case 16:
            case 18:
            case 32:
            case 64:
            case 68:
            case 80:
            {
            return zbank_unused_r(address);
            }
            default:
            {
            return zbank_lockup_r(address);
            }
            }
        }
    }
    private void zbank_write_ctrl_io(uint address, uint data)
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
            zbank_unused_w(address, data);
            return;
            }
            case 17:
            {
            if ((!((address & unchecked((uint)(1))) != 0))) {
            gen_zbusreq_w((data & unchecked((uint)(1))), z80.cycles);
            return;
            }
            zbank_unused_w(address, data);
            return;
            }
            case 18:
            {
            if ((!((address & unchecked((uint)(1))) != 0))) {
            gen_zreset_w((data & unchecked((uint)(1))), z80.cycles);
            return;
            }
            zbank_unused_w(address, data);
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
            zbank_unused_w(address, data);
            return;
            }
            case 16:
            case 32:
            case 64:
            case 68:
            case 80:
            {
            zbank_unused_w(address, data);
            return;
            }
            default:
            {
            zbank_lockup_w(address, data);
            return;
            }
            }
        }
    }
    private uint zbank_read_vdp(uint address)
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
            return (((vdp_68k_ctrl_r(z80.cycles) >> 8) & unchecked((uint)(3))) | unchecked((uint)(252)));
            }
            case 5:
            {
            return (vdp_68k_ctrl_r(z80.cycles) & unchecked((uint)(255)));
            }
            case 8:
            case 12:
            {
            return (vdp_hvc_r(z80.cycles) >> 8);
            }
            case 9:
            case 13:
            {
            return (vdp_hvc_r(z80.cycles) & unchecked((uint)(255)));
            }
            case 24:
            case 25:
            case 28:
            case 29:
            {
            return zbank_unused_r(address);
            }
            default:
            {
            return zbank_lockup_r(address);
            }
            }
        }
    }
    private void zbank_write_vdp(uint address, uint data)
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
            psg_write(z80.cycles, data);
            return;
            }
            zbank_unused_w(address, data);
            return;
            }
            case 24:
            {
            zbank_unused_w(address, data);
            return;
            }
            case 28:
            {
            vdp_test_w(((data << 8) | data));
            return;
            }
            default:
            {
            zbank_lockup_w(address, data);
            return;
            }
            }
        }
    }
}
