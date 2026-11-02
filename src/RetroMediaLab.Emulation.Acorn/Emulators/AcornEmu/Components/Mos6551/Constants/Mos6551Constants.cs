namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Mos6551.Constants;

internal static class Mos6551Constants
{
    internal const int RegisterMask = 3;
    internal const byte ParityError = 0x01;
    internal const byte FramingError = 0x02;
    internal const byte Overrun = 0x04;
    internal const byte ReceiveFull = 0x08;
    internal const byte TransmitEmpty = 0x10;
    internal const byte CarrierAbsent = 0x20;
    internal const byte DataSetAbsent = 0x40;
    internal const byte Interrupt = 0x80;
    internal const byte ReceiveStatusMask = ParityError | FramingError | Overrun | ReceiveFull;
    internal const byte Enable = 0x01;
    internal const byte DisableReceiveInterrupt = 0x02;
    internal const byte TransmitControlMask = 0x0c;
    internal const byte TransmitInterrupt = 0x04;
    internal const byte TransmitBreak = 0x0c;
    internal const byte Echo = 0x10;
    internal const byte ParityEnable = 0x20;
    internal const byte ParityMask = 0xe0;
    internal const byte OddParity = 0x20;
    internal const byte EvenParity = 0x60;
    internal const byte MarkParity = 0xa0;
    internal const byte SpaceParity = 0xe0;
    internal const byte ProgramResetPreserve = ParityMask;
    internal const byte BaudMask = 0x0f;
    internal const byte InternalReceiveClock = 0x10;
    internal const int WordLengthShift = 5;
    internal const int WordLengthMask = 3;
    internal const byte ExtraStop = 0x80;
    internal const int MaximumDataBits = 8;
    internal const int MinimumDataBits = 5;
    internal const int Oversampling = 16;
    internal const int HalfBit = Oversampling / 2;
    internal const int CrystalClockHz = 1843200;
    internal const int IdleReceive = -1;
    internal static ReadOnlySpan<int> BaudDivisors => [1, 2304, 1536, 1048, 856, 768, 384, 192, 96, 64, 48, 32, 24, 16, 12, 6];
}
