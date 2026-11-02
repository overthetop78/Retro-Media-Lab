namespace RetroMediaLab.Chips.IO.NEC.UPD700x.UPD7002.Constants;

public static class Adc7002Constants
{
    public const int MillisecondsPerSecond = 1000;
    public const int EightBitMilliseconds = 4;
    public const int TwelveBitMilliseconds = 10;
    public const int DefaultClockHz = 2000000;
    public const int RegisterMask = 3;
    public const int StatusRegister = 0;
    public const int HighRegister = 1;
    public const byte CommandMask = 0x0f;
    public const byte ChannelMask = 3;
    public const byte TwelveBitMode = 8;
    public const byte Busy = 0x80;
    public const byte Complete = 0x40;
    public const int ResultStatusMask = 0xc000;
    public const int ResultStatusShift = 10;
    public const int HighShift = 8;
    public const int LowMask = 0xf0;
    public const int ChannelCount = 4;







}
