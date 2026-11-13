using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Input;

internal sealed partial class InputHardware
{
    internal byte smash_read(int port)
    {
        unchecked
        {
            uint temp = unchecked((uint)(input.pad[(int)(port)]));
            byte data = default;
            if (((temp & unchecked((uint)(2))) != 0)) {
            data = unchecked((byte)(9));
            } else if (((temp & unchecked((uint)(64))) != 0)) {
            data = unchecked((byte)(13));
            } else if (((temp & unchecked((uint)(128))) != 0)) {
            data = unchecked((byte)(2));
            } else if (((temp & unchecked((uint)(4))) != 0)) {
            data = unchecked((byte)(6));
            } else if (((temp & unchecked((uint)(256))) != 0)) {
            data = unchecked((byte)(3));
            } else if (((temp & unchecked((uint)(16))) != 0)) {
            data = unchecked((byte)(7));
            } else if (((temp & unchecked((uint)(32))) != 0)) {
            data = unchecked((byte)(10));
            } else if (((temp & unchecked((uint)(8))) != 0)) {
            data = unchecked((byte)(14));
            } else if (((temp & unchecked((uint)(1))) != 0)) {
            data = unchecked((byte)(11));
            } else {
            data = unchecked((byte)(15));
            }
            return unchecked((byte)((unchecked((int)(data)) | 64)));
        }
    }
    internal byte smash_1_read()
    {
        unchecked
        {
            return smash_read(0);
        }
    }
    internal byte smash_2_read()
    {
        unchecked
        {
            return smash_read(4);
        }
    }
}
