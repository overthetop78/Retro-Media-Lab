namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.Archimedes.Common.Constants;

internal static class ArchimedesKeyboardConstants
{
    internal const byte HardReset = 0xff;
    internal const byte ResetAckOne = 0xfe;
    internal const byte ResetAckTwo = 0xfd;
    internal const byte ByteAck = 0x3f;
    internal const byte ScanAckBase = 0x30;
    internal const byte ScanAckMask = 0xfc;
    internal const byte ScanEnable = 1;
    internal const byte MouseEnable = 2;
    internal const byte RequestId = 0x20;
    internal const byte RequestReset = 0x21;
    internal const byte RequestMouse = 0x22;
    internal const byte RequestDataBase = 0x40;
    internal const byte RequestDataMask = 0xf0;
    internal const byte DataResponseBase = 0xe0;
    internal const byte BritishKeyboardId = 0x81;
    internal const byte LedMask = 7;
    internal const byte KeyDownBase = 0xc0;
    internal const byte KeyUpBase = 0xd0;
    internal const int RowShift = 4;
    internal const int ColumnMask = 15;
    internal const int MatrixKeys = 128;
    internal const int MouseMinimum = -64;
    internal const int MouseMaximum = 63;
    internal const int MouseMask = 0x7f;
    internal const int MouseLeftKey = 0x70;
    internal const int MouseMenuKey = 0x71;
    internal const int MouseRightKey = 0x72;
    internal const int PollTicks = ArchimedesBoardConstants.IocClockHz / 100;
    internal const int ReplyTicks = ArchimedesBoardConstants.IocClockHz / 1000;
}
