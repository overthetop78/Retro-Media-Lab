using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Input;

internal sealed partial class InputHardware
{
    internal void lightgun_reset(int port)
    {
        unchecked
        {
            new CPointer<short>(input.analog, (int)(port) * 2)[(int)(0)] = unchecked((short)((viewportWidth / 2)));
            new CPointer<short>(input.analog, (int)(port) * 2)[(int)(1)] = unchecked((short)((viewportHeight / 2)));
            lightgun.State = unchecked((byte)(64));
            lightgun.Port = unchecked((byte)(4));
        }
    }
    internal void lightgun_refresh(int port)
    {
        unchecked
        {
            if ((port == unchecked((int)(lightgun.Port)))) {
            int y = (unchecked((int)(new CPointer<short>(input.analog, (int)(port) * 2)[(int)(1)])) + input.y_offset);
            if (((y == unchecked((int)(v_counter))) && (y < viewportHeight))) {
            if (((unchecked((int)(io_reg[(int)(5)])) & 128) != 0)) {
            int x = unchecked((int)(new CPointer<short>(input.analog, (int)(port) * 2)[(int)(0)]));
            if ((unchecked((int)(input.system[(int)(1)])) == 3)) {
            if ((unchecked((int)(system_hw)) == 132)) {
            x = ((x * 304) / 320);
            } else {
            x = ((x * 289) / 320);
            }
            }
            if (((unchecked((int)(reg[(int)(11)])) & 8) != 0)) {
            m68k_update_irq(unchecked((uint)(2)));
            }
            if (((unchecked((int)(reg[(int)(0)])) & 2) != 0)) {
            hvc_latch = unchecked((uint)((65536 | (y << 8))));
            } else {
            hvc_latch = unchecked((uint)((131072 | (y << 8))));
            }
            if (((unchecked((int)(reg[(int)(12)])) & 1) != 0)) {
            hvc_latch |= unchecked((uint)(hc_320[(int)((((x / 2) + input.x_offset) % 210))]));
            } else {
            hvc_latch |= unchecked((uint)(hc_256[(int)((((x / 2) + input.x_offset) % 171))]));
            }
            }
            } else if (((hvc_latch & unchecked((uint)(131072))) != 0)) {
            hvc_latch = unchecked((uint)(0));
            }
            }
        }
    }
    internal byte phaser_read(int port)
    {
        unchecked
        {
            byte temp = unchecked((byte)((~((unchecked((int)(input.pad[(int)(port)])) >> 2) & 16))));
            if (((unchecked((int)(io_reg[(int)(15)])) & (2 << (port >> 1))) != 0)) {
            int hcounter = unchecked((int)(hctab[(int)(((z80Cycles + unchecked((uint)(530))) % unchecked((uint)(3420))))]));
            int dx = (unchecked((int)(new CPointer<short>(input.analog, (int)(port) * 2)[(int)(0)])) - (hcounter << 1));
            int dy = (unchecked((int)(new CPointer<short>(input.analog, (int)(port) * 2)[(int)(1)])) - unchecked((int)(v_counter)));
            if (((abs(dy) <= 5) && (abs(dx) <= 60))) {
            temp &= unchecked((byte)((~64)));
            if ((lightgun.State != 0)) {
            hvc_latch = unchecked((uint)((65536 | (input.x_offset + (unchecked((int)(new CPointer<short>(input.analog, (int)(port) * 2)[(int)(0)])) >> 1)))));
            lightgun.State = unchecked((byte)(0));
            } else {
            lightgun.State = unchecked((byte)(1));
            }
            }
            }
            return unchecked((byte)((unchecked((int)(temp)) & 127)));
        }
    }
    internal byte phaser_1_read()
    {
        unchecked
        {
            return phaser_read(0);
        }
    }
    internal byte phaser_2_read()
    {
        unchecked
        {
            return phaser_read(4);
        }
    }
    internal byte menacer_read()
    {
        unchecked
        {
            uint data = unchecked((uint)((unchecked((int)(input.pad[(int)(4)])) >> 4)));
            return unchecked((byte)(((((data & unchecked((uint)(9))) | ((data >> 1) & unchecked((uint)(2)))) | ((data << 1) & unchecked((uint)(4)))) | unchecked((uint)(64)))));
        }
    }
    internal byte justifier_read()
    {
        unchecked
        {
            if (((unchecked((int)(lightgun.State)) & 64) != 0)) {
            return unchecked((byte)(48));
            }
            return unchecked((byte)(((((~unchecked((int)(input.pad[(int)(lightgun.Port)]))) >> 6) & 3) | 112)));
        }
    }
    internal void justifier_write(byte data, byte mask)
    {
        unchecked
        {
            data &= mask;
            lightgun.Port = unchecked((byte)((4 + ((unchecked((int)(data)) >> 5) & 1))));
            lightgun.State = data;
        }
    }
}
