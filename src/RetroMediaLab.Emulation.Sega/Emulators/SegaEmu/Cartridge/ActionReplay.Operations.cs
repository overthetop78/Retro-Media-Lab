using System.Buffers.Binary;
using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Cartridge;

internal sealed partial class ActionReplay
{
    private void areplay_shutdown()
    {
        unchecked
        {
            areplay_set_status(0);
            action_replay.enabled = unchecked((byte)(0));
        }
    }
    private void areplay_reset(int hard)
    {
        unchecked
        {
            if ((action_replay.enabled != 0)) {
            if (((hard != 0) || (unchecked((int)(action_replay.status)) == 2))) {
            Clear_regs();
            Clear_old();
            Clear_data();
            Clear_addr();
            memory_map[(int)(0)].@base = new ArrayPointer<byte>(cart.lockrom);
            if ((hard != 0)) {
            FillMemory(new ArrayPointer<byte>(action_replay.ram), 255, 65536);
            }
            }
            }
        }
    }
    private int areplay_get_status()
    {
        unchecked
        {
            if ((action_replay.enabled != 0)) {
            return unchecked((int)(action_replay.status));
            }
            return (-1);
        }
    }
    private void areplay_set_status(int status)
    {
        unchecked
        {
            if ((action_replay.enabled != 0)) {
            if (((unchecked((int)(action_replay.enabled)) == 2) && (status == 2))) {
            status = 0;
            }
            switch (status) {
            case 0:
            case 2:
            {
            if ((unchecked((int)(action_replay.status)) == 1)) {
            (new ArrayPointer<byte>(cart.rom) + unchecked((int)(action_replay.addr[(int)(0)]))).Cast<ushort>()[0] = action_replay.old[(int)(0)];
            (new ArrayPointer<byte>(cart.rom) + unchecked((int)(action_replay.addr[(int)(1)]))).Cast<ushort>()[0] = action_replay.old[(int)(1)];
            (new ArrayPointer<byte>(cart.rom) + unchecked((int)(action_replay.addr[(int)(2)]))).Cast<ushort>()[0] = action_replay.old[(int)(2)];
            (new ArrayPointer<byte>(cart.rom) + unchecked((int)(action_replay.addr[(int)(3)]))).Cast<ushort>()[0] = action_replay.old[(int)(3)];
            }
            break;
            }
            case 1:
            {
            if ((unchecked((int)(action_replay.status)) != 1)) {
            action_replay.data[(int)(0)] = action_replay.regs[(int)(0)];
            action_replay.data[(int)(1)] = action_replay.regs[(int)(4)];
            action_replay.data[(int)(2)] = action_replay.regs[(int)(7)];
            action_replay.data[(int)(3)] = action_replay.regs[(int)(10)];
            action_replay.addr[(int)(0)] = unchecked((uint)(((unchecked((int)(action_replay.regs[(int)(1)])) | ((unchecked((int)(action_replay.regs[(int)(2)])) & 16128) << 8)) << 1)));
            action_replay.addr[(int)(1)] = unchecked((uint)(((unchecked((int)(action_replay.regs[(int)(5)])) | ((unchecked((int)(action_replay.regs[(int)(6)])) & 16128) << 8)) << 1)));
            action_replay.addr[(int)(2)] = unchecked((uint)(((unchecked((int)(action_replay.regs[(int)(8)])) | ((unchecked((int)(action_replay.regs[(int)(9)])) & 16128) << 8)) << 1)));
            action_replay.addr[(int)(3)] = unchecked((uint)(((unchecked((int)(action_replay.regs[(int)(11)])) | ((unchecked((int)(action_replay.regs[(int)(12)])) & 16128) << 8)) << 1)));
            action_replay.old[(int)(0)] = (new ArrayPointer<byte>(cart.rom) + unchecked((int)(action_replay.addr[(int)(0)]))).Cast<ushort>()[0];
            action_replay.old[(int)(1)] = (new ArrayPointer<byte>(cart.rom) + unchecked((int)(action_replay.addr[(int)(1)]))).Cast<ushort>()[0];
            action_replay.old[(int)(2)] = (new ArrayPointer<byte>(cart.rom) + unchecked((int)(action_replay.addr[(int)(2)]))).Cast<ushort>()[0];
            action_replay.old[(int)(3)] = (new ArrayPointer<byte>(cart.rom) + unchecked((int)(action_replay.addr[(int)(3)]))).Cast<ushort>()[0];
            (new ArrayPointer<byte>(cart.rom) + unchecked((int)(action_replay.addr[(int)(0)]))).Cast<ushort>()[0] = action_replay.data[(int)(0)];
            (new ArrayPointer<byte>(cart.rom) + unchecked((int)(action_replay.addr[(int)(1)]))).Cast<ushort>()[0] = action_replay.data[(int)(1)];
            (new ArrayPointer<byte>(cart.rom) + unchecked((int)(action_replay.addr[(int)(2)]))).Cast<ushort>()[0] = action_replay.data[(int)(2)];
            (new ArrayPointer<byte>(cart.rom) + unchecked((int)(action_replay.addr[(int)(3)]))).Cast<ushort>()[0] = action_replay.data[(int)(3)];
            }
            break;
            }
            default:
            {
            return;
            }
            }
            action_replay.status = unchecked((byte)(status));
            }
        }
    }
    private void ar_write_regs(uint address, uint data)
    {
        unchecked
        {
            int offset = unchecked((int)(((address & unchecked((uint)(65535))) >> 1)));
            if ((offset > 12)) {
            m68k_unused_16_w(address, data);
            return;
            }
            action_replay.regs[(int)(offset)] = unchecked((ushort)(data));
            if ((unchecked((int)(action_replay.regs[(int)(3)])) == 65535)) {
            if ((unchecked((int)(action_replay.status)) == 1)) {
            areplay_set_status(0);
            areplay_set_status(1);
            }
            memory_map[(int)(0)].@base = new ArrayPointer<byte>(cart.rom);
            }
        }
    }
    private void ar2_write_reg(uint address, uint data)
    {
        unchecked
        {
            if ((((address & unchecked((uint)(255))) == unchecked((uint)(120))) && (data == unchecked((uint)(65535))))) {
            memory_map[(int)(0)].@base = new ArrayPointer<byte>(cart.rom);
            }
        }
    }
    private void ar_write_ram_8(uint address, uint data)
    {
        unchecked
        {
            (new ArrayPointer<byte>(action_replay.ram) + unchecked((int)((address & unchecked((uint)(65534)))))).Cast<ushort>()[0] = unchecked((ushort)((data | (data << 8))));
        }
    }
}
