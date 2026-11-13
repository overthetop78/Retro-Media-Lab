using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Input;

internal sealed partial class InputHardware
{
    internal void terebi_oekaki_reset()
    {
        unchecked
        {
            new CPointer<short>(input.analog, (int)(0) * 2)[(int)(0)] = unchecked((short)(128));
            new CPointer<short>(input.analog, (int)(0) * 2)[(int)(1)] = unchecked((short)(128));
            tablet.axis = unchecked((byte)(1));
            tablet.busy = unchecked((byte)(1));
        }
    }
    internal ushort terebi_oekaki_read()
    {
        unchecked
        {
            ushort data = unchecked((ushort)(((unchecked((int)(tablet.busy)) << 15) | unchecked((int)(new CPointer<short>(input.analog, (int)(0) * 2)[(int)(tablet.axis)])))));
            if ((!((unchecked((int)(input.pad[(int)(0)])) & 16) != 0))) {
            data |= unchecked((ushort)(256));
            }
            tablet.busy = unchecked((byte)(0));
            return data;
        }
    }
    internal void terebi_oekaki_write(byte data)
    {
        unchecked
        {
            tablet.axis = unchecked((byte)(((unchecked((int)(data)) & 1) ^ 1)));
            tablet.busy = unchecked((byte)(1));
        }
    }
}
