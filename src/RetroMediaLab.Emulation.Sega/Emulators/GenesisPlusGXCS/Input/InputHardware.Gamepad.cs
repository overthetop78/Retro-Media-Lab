using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Input;

internal sealed partial class InputHardware
{
    internal void gamepad_reset(int port)
    {
        unchecked
        {
            gamepad[(int)(port)].State = unchecked((byte)(64));
            gamepad[(int)(port)].Counter = unchecked((byte)(0));
            gamepad[(int)(port)].Timeout = unchecked((byte)(0));
            gamepad[(int)(port)].Latency = unchecked((uint)(0));
            latch = unchecked((byte)(0));
            flipflop[(int)((port >> 2))].Latch = unchecked((byte)(0));
            flipflop[(int)((port >> 2))].Counter = unchecked((byte)(0));
        }
    }
    internal void gamepad_refresh(int port)
    {
        unchecked
        {
            if ((unchecked((int)((gamepad[(int)(port)].Timeout++))) > 25)) {
            gamepad[(int)(port)].Counter = unchecked((byte)(0));
            gamepad[(int)(port)].Timeout = unchecked((byte)(0));
            }
        }
    }
    internal void gamepad_end_frame(int port, uint cycles)
    {
        unchecked
        {
            if ((gamepad[(int)(port)].Latency > cycles)) {
            gamepad[(int)(port)].Latency -= cycles;
            } else {
            gamepad[(int)(port)].Latency = unchecked((uint)(0));
            }
        }
    }
    internal byte gamepad_read(int port)
    {
        unchecked
        {
            uint data = unchecked((uint)((unchecked((int)(gamepad[(int)(port)].State)) | 63)));
            uint pad = unchecked((uint)(input.pad[(int)(port)]));
            uint step = (unchecked((uint)(gamepad[(int)(port)].Counter)) | (data >> 6));
            uint cycles = (((unchecked((int)(system_hw)) & 129) == 128) ? mainCycles : z80Cycles);
            if ((cycles < gamepad[(int)(port)].Latency)) {
            step &= unchecked((uint)((~1)));
            }
            switch (step) {
            case 4:
            {
            data &= (~(((pad >> 2) & unchecked((uint)(48))) | unchecked((uint)(15))));
            break;
            }
            case 7:
            {
            data &= (~((pad & unchecked((uint)(48))) | ((pad >> 8) & unchecked((uint)(15)))));
            break;
            }
            case 6:
            {
            data &= (~((pad >> 2) & unchecked((uint)(48))));
            break;
            }
            default:
            {
            if (((step & unchecked((uint)(1))) != 0)) {
            data &= (~(pad & unchecked((uint)(63))));
            } else {
            data &= (~(((pad & unchecked((uint)(3))) | ((pad >> 2) & unchecked((uint)(48)))) | unchecked((uint)(12))));
            }
            break;
            }
            }
            return unchecked((byte)(data));
        }
    }
    internal void gamepad_write(int port, byte data, byte mask)
    {
        unchecked
        {
            if (((unchecked((int)(mask)) & 64) != 0)) {
            data &= unchecked((byte)(64));
            gamepad[(int)(port)].Latency = unchecked((uint)(0));
            if (((unchecked((int)(input.dev[(int)(port)])) == 1) && (unchecked((int)(gamepad[(int)(port)].Counter)) < 8))) {
            if (((data != 0) && (!(gamepad[(int)(port)].State != 0)))) {
            gamepad[(int)(port)].Counter += unchecked((byte)(2));
            gamepad[(int)(port)].Timeout = unchecked((byte)(0));
            }
            }
            } else {
            uint cycles = (((unchecked((int)(system_hw)) & 129) == 128) ? mainCycles : z80Cycles);
            data = unchecked((byte)(64));
            if ((!(gamepad[(int)(port)].State != 0))) {
            gamepad[(int)(port)].Latency = (cycles + unchecked((uint)(172)));
            }
            }
            gamepad[(int)(port)].State = data;
        }
    }
    internal byte gamepad_1_read()
    {
        unchecked
        {
            return gamepad_read(0);
        }
    }
    internal byte gamepad_2_read()
    {
        unchecked
        {
            return gamepad_read(4);
        }
    }
    internal void gamepad_1_write(byte data, byte mask)
    {
        unchecked
        {
            gamepad_write(0, data, mask);
        }
    }
    internal void gamepad_2_write(byte data, byte mask)
    {
        unchecked
        {
            gamepad_write(4, data, mask);
        }
    }
    internal byte wayplay_1_read()
    {
        unchecked
        {
            if (((unchecked((int)(latch)) & 4) != 0)) {
            return unchecked((byte)(124));
            }
            return gamepad_read(unchecked((int)(latch)));
        }
    }
    internal byte wayplay_2_read()
    {
        unchecked
        {
            return unchecked((byte)(127));
        }
    }
    internal void wayplay_1_write(byte data, byte mask)
    {
        unchecked
        {
            gamepad_write((unchecked((int)(latch)) & 3), data, mask);
        }
    }
    internal void wayplay_2_write(byte data, byte mask)
    {
        unchecked
        {
            data |= unchecked((byte)((~unchecked((int)(mask)))));
            if ((!((unchecked((int)(data)) & 3) != 0))) {
            latch = unchecked((byte)(((unchecked((int)(data)) >> 4) & 7)));
            }
        }
    }
    internal uint jcart_read(uint address)
    {
        unchecked
        {
            return unchecked((uint)((unchecked((int)(gamepad_read(5))) | ((unchecked((int)(gamepad_read(6))) & 63) << 8))));
        }
    }
    internal void jcart_write(uint address, uint data)
    {
        unchecked
        {
            data = ((data & unchecked((uint)(1))) << 6);
            gamepad_write(5, unchecked((byte)(data)), unchecked((byte)(64)));
            gamepad_write(6, unchecked((byte)(data)), unchecked((byte)(64)));
        }
    }
    internal byte mastertap_1_read()
    {
        unchecked
        {
            return gamepad_read(unchecked((int)(flipflop[(int)(0)].Counter)));
        }
    }
    internal byte mastertap_2_read()
    {
        unchecked
        {
            return gamepad_read((unchecked((int)(flipflop[(int)(1)].Counter)) + 4));
        }
    }
    internal void mastertap_1_write(byte data, byte mask)
    {
        unchecked
        {
            data = unchecked((byte)(((unchecked((int)(flipflop[(int)(0)].Latch)) & (~unchecked((int)(mask)))) | (unchecked((int)(data)) & unchecked((int)(mask))))));
            if ((((unchecked((int)(flipflop[(int)(0)].Latch)) & 64) != 0) && (!((unchecked((int)(data)) & 64) != 0)))) {
            flipflop[(int)(0)].Counter = unchecked((byte)(((unchecked((int)(flipflop[(int)(0)].Counter)) + 1) & 3)));
            }
            flipflop[(int)(0)].Latch = data;
        }
    }
    internal void mastertap_2_write(byte data, byte mask)
    {
        unchecked
        {
            data = unchecked((byte)(((unchecked((int)(flipflop[(int)(1)].Latch)) & (~unchecked((int)(mask)))) | (unchecked((int)(data)) & unchecked((int)(mask))))));
            if ((((unchecked((int)(flipflop[(int)(1)].Latch)) & 64) != 0) && (!((unchecked((int)(data)) & 64) != 0)))) {
            flipflop[(int)(1)].Counter = unchecked((byte)(((unchecked((int)(flipflop[(int)(1)].Counter)) + 1) & 3)));
            }
            flipflop[(int)(1)].Latch = data;
        }
    }
}
