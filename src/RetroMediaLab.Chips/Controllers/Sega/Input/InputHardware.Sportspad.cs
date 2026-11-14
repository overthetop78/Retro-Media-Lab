using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Chips.Controllers.Sega.Input;

public sealed partial class InputHardware
{
    public void sportspad_reset(int index)
    {
        unchecked
        {
            new ArrayPointer<short>(input.analog, (int)(index) * 2)[(int)(0)] = unchecked((short)(128));
            new ArrayPointer<short>(input.analog, (int)(index) * 2)[(int)(1)] = unchecked((short)(128));
            sportspad[(int)((index >> 2))].State = unchecked((byte)(64));
            sportspad[(int)((index >> 2))].Counter = unchecked((byte)(0));
        }
    }
    public byte sportspad_read(int port)
    {
        unchecked
        {
            byte temp = unchecked((byte)((~(unchecked((int)(input.pad[(int)(port)])) & 48))));
            int index = (port >> 2);
            temp &= unchecked((byte)(112));
            switch ((unchecked((int)(sportspad[(int)(index)].Counter)) & 3)) {
            case 1:
            {
            temp |= unchecked((byte)(((unchecked((int)(new ArrayPointer<short>(input.analog, (int)(port) * 2)[(int)(0)])) >> 4) & 15)));
            break;
            }
            case 2:
            {
            temp |= unchecked((byte)((unchecked((int)(new ArrayPointer<short>(input.analog, (int)(port) * 2)[(int)(0)])) & 15)));
            break;
            }
            case 3:
            {
            temp |= unchecked((byte)(((unchecked((int)(new ArrayPointer<short>(input.analog, (int)(port) * 2)[(int)(1)])) >> 4) & 15)));
            break;
            }
            default:
            {
            temp |= unchecked((byte)((unchecked((int)(new ArrayPointer<short>(input.analog, (int)(port) * 2)[(int)(1)])) & 15)));
            break;
            }
            }
            return temp;
        }
    }
    public void sportspad_write(int index, byte data, byte mask)
    {
        unchecked
        {
            data = unchecked((byte)(((unchecked((int)(sportspad[(int)(index)].State)) & (~unchecked((int)(mask)))) | (unchecked((int)(data)) & unchecked((int)(mask))))));
            if ((((unchecked((int)(data)) ^ unchecked((int)(sportspad[(int)(index)].State))) & 64) != 0)) {
            sportspad[(int)(index)].Counter++;
            }
            sportspad[(int)(index)].State = data;
        }
    }
    public byte sportspad_1_read()
    {
        unchecked
        {
            return sportspad_read(0);
        }
    }
    public byte sportspad_2_read()
    {
        unchecked
        {
            return sportspad_read(4);
        }
    }
    public void sportspad_1_write(byte data, byte mask)
    {
        unchecked
        {
            sportspad_write(0, data, mask);
        }
    }
    public void sportspad_2_write(byte data, byte mask)
    {
        unchecked
        {
            sportspad_write(1, data, mask);
        }
    }
}
