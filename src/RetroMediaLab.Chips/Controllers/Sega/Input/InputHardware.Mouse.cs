using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Chips.Controllers.Sega.Input;

public sealed partial class InputHardware
{
    public void mouse_reset(int port)
    {
        unchecked
        {
            new ArrayPointer<short>(input.analog, (int)(port) * 2)[(int)(0)] = unchecked((short)(0));
            new ArrayPointer<short>(input.analog, (int)(port) * 2)[(int)(1)] = unchecked((short)(0));
            mouse.State = unchecked((byte)(96));
            mouse.Counter = unchecked((byte)(0));
            mouse.Wait = unchecked((byte)(0));
            mouse.Port = unchecked((byte)(port));
        }
    }
    public byte mouse_read()
    {
        unchecked
        {
            uint temp = unchecked((uint)(0));
            int x = unchecked((int)(new ArrayPointer<short>(input.analog, (int)(mouse.Port) * 2)[(int)(0)]));
            int y = unchecked((int)(new ArrayPointer<short>(input.analog, (int)(mouse.Port) * 2)[(int)(1)]));
            switch (mouse.Counter) {
            case 0:
            temp = unchecked((uint)(0));
            break;
            case 1:
            temp = unchecked((uint)(11));
            break;
            case 2:
            temp = unchecked((uint)(15));
            break;
            case 3:
            temp = unchecked((uint)(15));
            break;
            case 4:
            temp |= ((x < 0) ? (uint)1 : (uint)0);
            temp |= unchecked((uint)((((y < 0) ? (int)1 : (int)0) << 1)));
            break;
            case 5:
            temp = unchecked((uint)(((unchecked((int)(input.pad[(int)(mouse.Port)])) >> 4) & 15)));
            break;
            case 6:
            temp = unchecked((uint)(((x >> 4) & 15)));
            break;
            case 7:
            temp = unchecked((uint)((x & 15)));
            break;
            case 8:
            temp = unchecked((uint)(((y >> 4) & 15)));
            break;
            case 9:
            temp = unchecked((uint)((y & 15)));
            break;
            }
            if ((mouse.Wait != 0)) {
            mouse.Wait--;
            temp |= unchecked((uint)((((~unchecked((int)(mouse.State))) & 32) >> 1)));
            ;
            } else {
            temp |= unchecked((uint)(((unchecked((int)(mouse.State)) & 32) >> 1)));
            }
            return unchecked((byte)(temp));
        }
    }
    public void mouse_write(byte data, byte mask)
    {
        unchecked
        {
            data = unchecked((byte)(((unchecked((int)(mouse.State)) & (~unchecked((int)(mask)))) | (unchecked((int)(data)) & unchecked((int)(mask))))));
            if ((((unchecked((int)(mouse.State)) ^ unchecked((int)(data))) & 32) != 0)) {
            if (((unchecked((int)(mouse.Counter)) > 0) && (unchecked((int)(mouse.Counter)) < 9))) {
            mouse.Counter++;
            }
            mouse.Wait = unchecked((byte)(2));
            }
            if ((((unchecked((int)(mouse.State)) ^ unchecked((int)(data))) & 64) != 0)) {
            mouse.Counter = unchecked((byte)(((unchecked((int)(mouse.State)) >> 6) & 1)));
            }
            mouse.State = data;
        }
    }
}
