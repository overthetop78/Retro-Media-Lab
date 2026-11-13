using System.Buffers.Binary;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cartridge;

internal sealed partial class GameGenie
{
    private void ggenie_shutdown()
    {
        unchecked
        {
            if ((ggenie.enabled != 0)) {
            ggenie_switch(0);
            ggenie.enabled = unchecked((byte)(0));
            }
        }
    }
    private void ggenie_reset(int hard)
    {
        unchecked
        {
            if ((ggenie.enabled != 0)) {
            ggenie_switch(0);
            if ((hard != 0)) {
            Clear_regs();
            Clear_old();
            Clear_data();
            Clear_addr();
            }
            memory_map[(int)(0)].@base = new CPointer<byte>(cart.lockrom);
            memory_map[(int)(0)].write8 = ggenie_write_byte;
            memory_map[(int)(0)].write16 = ggenie_write_word;
            memory_map[(int)(0)].read16 = null;
            }
        }
    }
    private void ggenie_switch(int enable)
    {
        unchecked
        {
            int i = default;
            if ((enable != 0)) {
            for (i = 0; (i < 6); i++) {
            if (((unchecked((int)(ggenie.regs[(int)(0)])) & (1 << i)) != 0)) {
            ggenie.old[(int)(i)] = (new CPointer<byte>(cart.rom) + unchecked((int)(ggenie.addr[(int)(i)]))).Cast<ushort>()[0];
            (new CPointer<byte>(cart.rom) + unchecked((int)(ggenie.addr[(int)(i)]))).Cast<ushort>()[0] = ggenie.data[(int)(i)];
            }
            }
            } else {
            for (i = 5; (i >= 0); i--) {
            if (((unchecked((int)(ggenie.regs[(int)(0)])) & (1 << i)) != 0)) {
            (new CPointer<byte>(cart.rom) + unchecked((int)(ggenie.addr[(int)(i)]))).Cast<ushort>()[0] = ggenie.old[(int)(i)];
            }
            }
            }
        }
    }
    private uint ggenie_read_byte(uint address)
    {
        unchecked
        {
            uint data = unchecked((uint)(ggenie.regs[(int)(((address >> 1) & unchecked((uint)(31))))]));
            return (((address & unchecked((uint)(1))) != 0) ? (data & unchecked((uint)(255))) : ((data >> 8) & unchecked((uint)(255))));
        }
    }
    private uint ggenie_read_word(uint address)
    {
        unchecked
        {
            return unchecked((uint)(ggenie.regs[(int)(((address >> 1) & unchecked((uint)(31))))]));
        }
    }
    private void ggenie_write_byte(uint address, uint data)
    {
        unchecked
        {
            byte offset = unchecked((byte)(((address >> 1) & unchecked((uint)(31)))));
            if (((address & unchecked((uint)(1))) != 0)) {
            data = (unchecked((uint)((unchecked((int)(ggenie.regs[(int)(offset)])) & 65280))) | (data & unchecked((uint)(255))));
            } else {
            data = (unchecked((uint)((unchecked((int)(ggenie.regs[(int)(offset)])) & 255))) | ((data & unchecked((uint)(255))) << 8));
            }
            ggenie_write_regs(unchecked((uint)(offset)), data);
        }
    }
    private void ggenie_write_word(uint address, uint data)
    {
        unchecked
        {
            byte offset = unchecked((byte)(((address >> 1) & unchecked((uint)(31)))));
            ggenie_write_regs(unchecked((uint)(offset)), data);
        }
    }
    private void ggenie_write_regs(uint offset, uint data)
    {
        unchecked
        {
            ggenie.regs[(int)(offset)] = unchecked((ushort)(data));
            if ((offset == unchecked((uint)(0)))) {
            if (((data & unchecked((uint)(1024))) != 0)) {
            memory_map[(int)(0)].@base = new CPointer<byte>(cart.rom);
            memory_map[(int)(0)].read8 = null;
            memory_map[(int)(0)].read16 = null;
            } else {
            memory_map[(int)(0)].@base = new CPointer<byte>(cart.lockrom);
            memory_map[(int)(0)].read8 = null;
            memory_map[(int)(0)].read16 = null;
            if (((data & unchecked((uint)(512))) != 0)) {
            memory_map[(int)(0)].read8 = ggenie_read_byte;
            memory_map[(int)(0)].read16 = ggenie_read_word;
            }
            }
            if (((data & unchecked((uint)(256))) != 0)) {
            ggenie.addr[(int)(0)] = unchecked((uint)((((unchecked((int)(ggenie.regs[(int)(2)])) & 63) << 16) | unchecked((int)(ggenie.regs[(int)(3)])))));
            ggenie.addr[(int)(1)] = unchecked((uint)((((unchecked((int)(ggenie.regs[(int)(5)])) & 63) << 16) | unchecked((int)(ggenie.regs[(int)(6)])))));
            ggenie.addr[(int)(2)] = unchecked((uint)((((unchecked((int)(ggenie.regs[(int)(8)])) & 63) << 16) | unchecked((int)(ggenie.regs[(int)(9)])))));
            ggenie.addr[(int)(3)] = unchecked((uint)((((unchecked((int)(ggenie.regs[(int)(11)])) & 63) << 16) | unchecked((int)(ggenie.regs[(int)(12)])))));
            ggenie.addr[(int)(4)] = unchecked((uint)((((unchecked((int)(ggenie.regs[(int)(14)])) & 63) << 16) | unchecked((int)(ggenie.regs[(int)(15)])))));
            ggenie.addr[(int)(5)] = unchecked((uint)((((unchecked((int)(ggenie.regs[(int)(17)])) & 63) << 16) | unchecked((int)(ggenie.regs[(int)(18)])))));
            ggenie.data[(int)(0)] = ggenie.regs[(int)(4)];
            ggenie.data[(int)(1)] = ggenie.regs[(int)(7)];
            ggenie.data[(int)(2)] = ggenie.regs[(int)(10)];
            ggenie.data[(int)(3)] = ggenie.regs[(int)(13)];
            ggenie.data[(int)(4)] = ggenie.regs[(int)(16)];
            ggenie.data[(int)(5)] = ggenie.regs[(int)(19)];
            memory_map[(int)(0)].write8 = m68k_unused_8_w;
            memory_map[(int)(0)].write16 = m68k_unused_16_w;
            ggenie_switch(1);
            } else {
            memory_map[(int)(0)].write8 = ggenie_write_byte;
            memory_map[(int)(0)].write16 = ggenie_write_word;
            }
            } else if ((offset == unchecked((uint)(1)))) {
            ggenie.regs[(int)(1)] |= unchecked((ushort)(1));
            }
        }
    }
}
