using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Input;

internal sealed partial class InputHardware
{
    internal void paddle_reset(int port)
    {
        unchecked
        {
            new CPointer<short>(input.analog, (int)(port) * 2)[(int)(0)] = unchecked((short)(128));
            paddle[(int)((port >> 2))].State = unchecked((byte)(64));
            paddle[(int)((port >> 2))].Counter = unchecked((byte)(0));
        }
    }
    internal byte paddle_read(int port)
    {
        unchecked
        {
            byte temp = unchecked((byte)((~(unchecked((int)(input.pad[(int)(port)])) & 16))));
            int index = (port >> 2);
            temp &= unchecked((byte)(112));
            if ((unchecked((int)(region_code)) < 128)) {
            if ((unchecked((int)((++paddle[(int)(index)].Counter))) > 2)) {
            paddle[(int)(index)].Counter = unchecked((byte)(0));
            paddle[(int)(index)].State ^= unchecked((byte)(64));
            }
            }
            if (((unchecked((int)(paddle[(int)(index)].State)) & 64) != 0)) {
            temp |= unchecked((byte)(((unchecked((int)(new CPointer<short>(input.analog, (int)(port) * 2)[(int)(0)])) >> 4) & 15)));
            } else {
            temp |= unchecked((byte)((unchecked((int)(new CPointer<short>(input.analog, (int)(port) * 2)[(int)(0)])) & 15)));
            temp &= unchecked((byte)((~32)));
            }
            return temp;
        }
    }
    internal void paddle_write(int index, byte data, byte mask)
    {
        unchecked
        {
            paddle[(int)(index)].State = unchecked((byte)(((unchecked((int)(paddle[(int)(index)].State)) & (~unchecked((int)(mask)))) | (unchecked((int)(data)) & unchecked((int)(mask))))));
        }
    }
    internal byte paddle_1_read()
    {
        unchecked
        {
            return paddle_read(0);
        }
    }
    internal byte paddle_2_read()
    {
        unchecked
        {
            return paddle_read(4);
        }
    }
    internal void paddle_1_write(byte data, byte mask)
    {
        unchecked
        {
            paddle_write(0, data, mask);
        }
    }
    internal void paddle_2_write(byte data, byte mask)
    {
        unchecked
        {
            paddle_write(1, data, mask);
        }
    }
}
