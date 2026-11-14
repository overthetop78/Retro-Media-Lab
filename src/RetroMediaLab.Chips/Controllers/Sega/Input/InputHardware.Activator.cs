using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Chips.Controllers.Sega.Input;

public sealed partial class InputHardware
{
    public void activator_reset(int index)
    {
        unchecked
        {
            activator[(int)(index)].State = unchecked((byte)(64));
            activator[(int)(index)].Counter = unchecked((byte)(0));
        }
    }
    public byte activator_read(int index)
    {
        unchecked
        {
            ushort data = unchecked((ushort)((~unchecked((int)(input.pad[(int)((index << 2))])))));
            byte temp = unchecked((byte)(((unchecked((int)(activator[(int)(index)].State)) & 1) << 1)));
            switch (activator[(int)(index)].Counter) {
            case 0:
            temp |= unchecked((byte)(4));
            break;
            case 1:
            temp |= unchecked((byte)(((unchecked((int)(data)) << 2) & 60)));
            break;
            case 2:
            temp |= unchecked((byte)(((unchecked((int)(data)) >> 2) & 60)));
            break;
            case 3:
            temp |= unchecked((byte)(((unchecked((int)(data)) >> 6) & 60)));
            break;
            case 4:
            temp |= unchecked((byte)(((unchecked((int)(data)) >> 10) & 60)));
            break;
            }
            return temp;
        }
    }
    public void activator_write(int index, byte data, byte mask)
    {
        unchecked
        {
            data = unchecked((byte)(((unchecked((int)(activator[(int)(index)].State)) & (~unchecked((int)(mask)))) | (unchecked((int)(data)) & unchecked((int)(mask))))));
            if ((((unchecked((int)(activator[(int)(index)].State)) ^ unchecked((int)(data))) & 64) != 0)) {
            activator[(int)(index)].Counter = unchecked((byte)(0));
            } else {
            if ((((unchecked((int)(activator[(int)(index)].State)) ^ unchecked((int)(data))) & 1) != 0)) {
            if ((unchecked((int)(activator[(int)(index)].Counter)) < 4)) {
            activator[(int)(index)].Counter++;
            }
            }
            }
            activator[(int)(index)].State = data;
        }
    }
    public byte activator_1_read()
    {
        unchecked
        {
            return activator_read(0);
        }
    }
    public byte activator_2_read()
    {
        unchecked
        {
            return activator_read(1);
        }
    }
    public void activator_1_write(byte data, byte mask)
    {
        unchecked
        {
            activator_write(0, data, mask);
        }
    }
    public void activator_2_write(byte data, byte mask)
    {
        unchecked
        {
            activator_write(1, data, mask);
        }
    }
}
