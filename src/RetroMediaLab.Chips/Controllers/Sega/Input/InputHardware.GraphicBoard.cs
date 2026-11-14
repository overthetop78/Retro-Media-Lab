using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Chips.Controllers.Sega.Input;

public sealed partial class InputHardware
{
    public void graphic_board_reset(int port)
    {
        unchecked
        {
            new ArrayPointer<short>(input.analog, (int)(0) * 2)[(int)(0)] = unchecked((short)(128));
            new ArrayPointer<short>(input.analog, (int)(0) * 2)[(int)(1)] = unchecked((short)(128));
            board.State = unchecked((byte)(127));
            board.Counter = unchecked((byte)(0));
            board.Port = unchecked((byte)(port));
        }
    }
    public byte graphic_board_read()
    {
        unchecked
        {
            byte data = default;
            if (((unchecked((int)(board.State)) & 32) != 0)) {
            return unchecked((byte)(96));
            }
            switch ((unchecked((int)(board.Counter)) & 7)) {
            case 0:
            data = unchecked((byte)((~unchecked((int)(input.pad[(int)(board.Port)])))));
            break;
            case 1:
            data = unchecked((byte)(15));
            break;
            case 2:
            data = unchecked((byte)(15));
            break;
            case 3:
            data = unchecked((byte)((unchecked((int)(new ArrayPointer<short>(input.analog, (int)(board.Port) * 2)[(int)(0)])) >> 4)));
            break;
            case 4:
            data = unchecked((byte)(new ArrayPointer<short>(input.analog, (int)(board.Port) * 2)[(int)(0)]));
            break;
            case 5:
            data = unchecked((byte)((unchecked((int)(new ArrayPointer<short>(input.analog, (int)(board.Port) * 2)[(int)(1)])) >> 4)));
            break;
            case 6:
            data = unchecked((byte)(new ArrayPointer<short>(input.analog, (int)(board.Port) * 2)[(int)(1)]));
            break;
            case 7:
            data = unchecked((byte)(15));
            break;
            }
            return unchecked((byte)(((unchecked((int)(board.State)) & (~31)) | (unchecked((int)(data)) & 15))));
        }
    }
    public void graphic_board_write(byte data, byte mask)
    {
        unchecked
        {
            data = unchecked((byte)(((unchecked((int)(board.State)) & (~unchecked((int)(mask)))) | (unchecked((int)(data)) & unchecked((int)(mask))))));
            if ((((unchecked((int)(data)) ^ unchecked((int)(board.State))) & 32) != 0)) {
            board.Counter = unchecked((byte)(0));
            } else if ((((unchecked((int)(data)) ^ unchecked((int)(board.State))) & 64) != 0)) {
            board.Counter++;
            }
            board.State = data;
        }
    }
}
