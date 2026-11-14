using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Chips.Controllers.Sega.Input;

public sealed partial class InputHardware
{
    public void terebi_oekaki_reset()
    {
        unchecked
        {
            new ArrayPointer<short>(input.analog, (int)(0) * 2)[(int)(0)] = unchecked((short)(128));
            new ArrayPointer<short>(input.analog, (int)(0) * 2)[(int)(1)] = unchecked((short)(128));
            tablet.axis = unchecked((byte)(1));
            tablet.busy = unchecked((byte)(1));
        }
    }
    public ushort terebi_oekaki_read()
    {
        unchecked
        {
            ushort data = unchecked((ushort)(((unchecked((int)(tablet.busy)) << 15) | unchecked((int)(new ArrayPointer<short>(input.analog, (int)(0) * 2)[(int)(tablet.axis)])))));
            if ((!((unchecked((int)(input.pad[(int)(0)])) & 16) != 0))) {
            data |= unchecked((ushort)(256));
            }
            tablet.busy = unchecked((byte)(0));
            return data;
        }
    }
    public void terebi_oekaki_write(byte data)
    {
        unchecked
        {
            tablet.axis = unchecked((byte)(((unchecked((int)(data)) & 1) ^ 1)));
            tablet.busy = unchecked((byte)(1));
        }
    }
}
