using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Chips.Controllers.Sega.Input;

public sealed partial class InputHardware
{
    public void xe_1ap_reset(int index)
    {
        unchecked
        {
            new ArrayPointer<short>(input.analog, (int)(index) * 2)[(int)(0)] = unchecked((short)(128));
            new ArrayPointer<short>(input.analog, (int)(index) * 2)[(int)(1)] = unchecked((short)(128));
            new ArrayPointer<short>(input.analog, (int)((index + 1)) * 2)[(int)(0)] = unchecked((short)(128));
            index >>= 2;
            xe_1ap[(int)(index)].State = unchecked((byte)(64));
            xe_1ap[(int)(index)].Counter = unchecked((byte)(11));
            xe_1ap[(int)(index)].Latency = unchecked((byte)(0));
        }
    }
    public byte xe_1ap_read(int index)
    {
        unchecked
        {
            byte data = default;
            uint port = unchecked((uint)((index << 2)));
            switch (xe_1ap[(int)(index)].Counter) {
            case 0:
            data = unchecked((byte)((((~unchecked((int)(input.pad[(int)(port)]))) >> 10) & 15)));
            break;
            case 1:
            data = unchecked((byte)(((((~unchecked((int)(input.pad[(int)(port)]))) >> 4) & 15) & (~((unchecked((int)(input.pad[(int)(port)])) >> 6) & 12)))));
            break;
            case 2:
            data = unchecked((byte)(((unchecked((int)(new ArrayPointer<short>(input.analog, (int)(port) * 2)[(int)(0)])) >> 4) & 15)));
            break;
            case 3:
            data = unchecked((byte)(((unchecked((int)(new ArrayPointer<short>(input.analog, (int)(port) * 2)[(int)(1)])) >> 4) & 15)));
            break;
            case 4:
            data = unchecked((byte)(0));
            break;
            case 5:
            data = unchecked((byte)(((unchecked((int)(new ArrayPointer<short>(input.analog, (int)((port + unchecked((uint)(1)))) * 2)[(int)(0)])) >> 4) & 15)));
            break;
            case 6:
            data = unchecked((byte)((unchecked((int)(new ArrayPointer<short>(input.analog, (int)(port) * 2)[(int)(0)])) & 15)));
            break;
            case 7:
            data = unchecked((byte)((unchecked((int)(new ArrayPointer<short>(input.analog, (int)(port) * 2)[(int)(1)])) & 15)));
            break;
            case 8:
            data = unchecked((byte)(0));
            break;
            case 9:
            data = unchecked((byte)((unchecked((int)(new ArrayPointer<short>(input.analog, (int)((port + unchecked((uint)(1)))) * 2)[(int)(0)])) & 15)));
            break;
            case 11:
            data = unchecked((byte)((((~unchecked((int)(input.pad[(int)(port)]))) >> 6) & 15)));
            break;
            default:
            data = unchecked((byte)(15));
            break;
            }
            data |= unchecked((byte)(((unchecked((int)(xe_1ap[(int)(index)].Counter)) & 1) << 4)));
            if ((xe_1ap[(int)(index)].Latency != 0)) {
            if ((unchecked((int)(xe_1ap[(int)(index)].Latency)) > 1)) {
            data |= unchecked((byte)(32));
            }
            xe_1ap[(int)(index)].Latency--;
            } else if ((unchecked((int)(xe_1ap[(int)(index)].Counter)) <= 10)) {
            xe_1ap[(int)(index)].Counter++;
            xe_1ap[(int)(index)].Latency = unchecked((byte)(3));
            }
            return data;
        }
    }
    public void xe_1ap_write(int index, byte data, byte mask)
    {
        unchecked
        {
            data = unchecked((byte)(((unchecked((int)(xe_1ap[(int)(index)].State)) & (~unchecked((int)(mask)))) | (unchecked((int)(data)) & unchecked((int)(mask))))));
            if (((!((unchecked((int)(data)) & 64) != 0)) && ((unchecked((int)(xe_1ap[(int)(index)].State)) & 64) != 0))) {
            xe_1ap[(int)(index)].Counter = unchecked((byte)(0));
            xe_1ap[(int)(index)].Latency = unchecked((byte)(3));
            }
            xe_1ap[(int)(index)].State = data;
        }
    }
    public byte xe_1ap_1_read()
    {
        unchecked
        {
            return xe_1ap_read(0);
        }
    }
    public byte xe_1ap_2_read()
    {
        unchecked
        {
            return xe_1ap_read(1);
        }
    }
    public void xe_1ap_1_write(byte data, byte mask)
    {
        unchecked
        {
            xe_1ap_write(0, data, mask);
        }
    }
    public void xe_1ap_2_write(byte data, byte mask)
    {
        unchecked
        {
            xe_1ap_write(1, data, mask);
        }
    }
}
