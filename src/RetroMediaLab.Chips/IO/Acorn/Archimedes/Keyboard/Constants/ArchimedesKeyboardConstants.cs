namespace RetroMediaLab.Chips.IO.Acorn.Archimedes.Keyboard.Constants;

public static class ArchimedesKeyboardConstants
{
    public const byte HardReset = 0xff;
    public const byte ResetAckOne = 0xfe;
    public const byte ResetAckTwo = 0xfd;
    public const byte ByteAck = 0x3f;
    public const byte ScanAckBase = 0x30;
    public const byte ScanAckMask = 0xfc;
    public const byte ScanEnable = 1;
    public const byte MouseEnable = 2;
    public const byte RequestId = 0x20;
    public const byte RequestReset = 0x21;
    public const byte RequestMouse = 0x22;
    public const byte RequestDataBase = 0x40;
    public const byte RequestDataMask = 0xf0;
    public const byte DataResponseBase = 0xe0;
    public const byte BritishKeyboardId = 0x81;
    public const byte LedMask = 7;
    public const byte KeyDownBase = 0xc0;
    public const byte KeyUpBase = 0xd0;
    public const int RowShift = 4;
    public const int ColumnMask = 15;
    public const int MatrixKeys = 128;
    public const int MouseMinimum = -64;
    public const int MouseMaximum = 63;
    public const int MouseMask = 0x7f;
    public const int MouseLeftKey = 0x70;
    public const int MouseMenuKey = 0x71;
    public const int MouseRightKey = 0x72;
    public const int InputClockHz = 2000000;
    public const int PollTicks = InputClockHz / 100;
    public const int ReplyTicks = InputClockHz / 1000;
}
