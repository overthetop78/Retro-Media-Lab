using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Chips.Controllers.Sega.Input;

public sealed partial class InputHardware
{
    public void teamplayer_init(int port)
    {
        unchecked
        {
            int i = default;
            int padnum = default;
            int index = 0;
            for (i = 0; (i < 4); i++) {
            padnum = ((4 * port) + i);
            if ((unchecked((int)(input.dev[(int)(padnum)])) == 0)) {
            padnum = (padnum << 4);
            teamplayer[(int)(port)].Table[(int)((index++))] = unchecked((byte)(padnum));
            teamplayer[(int)(port)].Table[(int)((index++))] = unchecked((byte)((padnum | 4)));
            } else {
            padnum = (padnum << 4);
            teamplayer[(int)(port)].Table[(int)((index++))] = unchecked((byte)(padnum));
            teamplayer[(int)(port)].Table[(int)((index++))] = unchecked((byte)((padnum | 4)));
            teamplayer[(int)(port)].Table[(int)((index++))] = unchecked((byte)((padnum | 8)));
            }
            }
        }
    }
    public void teamplayer_reset(int port)
    {
        unchecked
        {
            teamplayer[(int)(port)].State = unchecked((byte)(96));
            teamplayer[(int)(port)].Counter = unchecked((byte)(0));
        }
    }
    public uint teamplayer_read(int port)
    {
        unchecked
        {
            uint counter = unchecked((uint)(teamplayer[(int)(port)].Counter));
            switch (counter) {
            case 0:
            {
            return unchecked((uint)((((unchecked((int)(teamplayer[(int)(port)].State)) & 32) >> 1) | 3)));
            }
            case 1:
            {
            return unchecked((uint)((((unchecked((int)(teamplayer[(int)(port)].State)) & 32) >> 1) | 15)));
            }
            case 2:
            case 3:
            {
            return unchecked((uint)(((unchecked((int)(teamplayer[(int)(port)].State)) & 32) >> 1)));
            }
            case 4:
            case 5:
            case 6:
            case 7:
            {
            uint retval = unchecked((uint)(input.dev[(int)((unchecked((uint)((port << 2))) + (counter - unchecked((uint)(4)))))]));
            return (unchecked((uint)(((unchecked((int)(teamplayer[(int)(port)].State)) & 32) >> 1))) | retval);
            }
            default:
            {
            uint retval = unchecked((uint)(15));
            uint padnum = unchecked((uint)((unchecked((int)(teamplayer[(int)(port)].Table[(int)((counter - unchecked((uint)(8))))])) >> 4)));
            retval &= unchecked((uint)((~(unchecked((int)(input.pad[(int)(padnum)])) >> (unchecked((int)(teamplayer[(int)(port)].Table[(int)((counter - unchecked((uint)(8))))])) & 15)))));
            return (unchecked((uint)(((unchecked((int)(teamplayer[(int)(port)].State)) & 32) >> 1))) | retval);
            }
            }
        }
    }
    public void teamplayer_write(int port, byte data, byte mask)
    {
        unchecked
        {
            uint state = unchecked((uint)(((unchecked((int)(teamplayer[(int)(port)].State)) & (~unchecked((int)(mask)))) | (unchecked((int)(data)) & unchecked((int)(mask))))));
            if (((state & unchecked((uint)(64))) != 0)) {
            teamplayer[(int)(port)].Counter = unchecked((byte)(0));
            } else if ((((unchecked((uint)(teamplayer[(int)(port)].State)) ^ state) & unchecked((uint)(96))) != 0)) {
            teamplayer[(int)(port)].Counter++;
            }
            teamplayer[(int)(port)].State = unchecked((byte)(state));
        }
    }
    public byte teamplayer_1_read()
    {
        unchecked
        {
            return unchecked((byte)(teamplayer_read(0)));
        }
    }
    public byte teamplayer_2_read()
    {
        unchecked
        {
            return unchecked((byte)(teamplayer_read(1)));
        }
    }
    public void teamplayer_1_write(byte data, byte mask)
    {
        unchecked
        {
            teamplayer_write(0, data, mask);
        }
    }
    public void teamplayer_2_write(byte data, byte mask)
    {
        unchecked
        {
            teamplayer_write(1, data, mask);
        }
    }
}
