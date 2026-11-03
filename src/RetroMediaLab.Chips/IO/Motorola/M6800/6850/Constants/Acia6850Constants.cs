namespace RetroMediaLab.Chips.IO.Motorola.M6800.Chip6850.Constants;

public static class Acia6850Constants
{
    public const byte ReceiveDataFull = 0x01;
    public const byte TransmitDataEmpty = 0x02;
    public const byte CarrierAbsent = 0x04;
    public const byte ClearToSendBlocked = 0x08;
    public const byte FramingError = 0x10;
    public const byte Overrun = 0x20;
    public const byte ParityError = 0x40;
    public const byte InterruptRequest = 0x80;
    public const byte ReceiveErrors = FramingError | ParityError;
    public const int DividerMask = 3;
    public const int MasterResetSelection = 3;
    public const int WordFormatShift = 2;
    public const int WordFormatMask = 7;
    public const int TransmitControlShift = 5;
    public const int TransmitControlMask = 3;
    public const byte ReceiveInterruptEnable = 0x80;
    public const int DirectClockDivider = 1;
    public const int ClockDivider16 = 16;
    public const int ClockDivider64 = 64;
    public const int ResetClockDivider = 0;
    public const int SevenDataBits = 7;
    public const int EightDataBits = 8;
    public const int FirstEightBitFormat = 4;
    public const int OneStopBit = 1;
    public const int TwoStopBits = 2;
    public const int HalfBitDivisor = 2;
    public const int IdleReceiveStep = -1;
    public const int FollowProgrammedBaudRate = 0;
    public const int LeastSignificantBit = 1;
}
