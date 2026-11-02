namespace RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Constants;

public static class ElectronUlaConstants
{
    public const int CpuClock = 2000000;
    public const int RegisterMask = 0x0f;
    public const int PageMask = 0x0f;
    public const int InternalPageMask = 0x0c;
    public const int InternalPageSelect = 0x08;
    public const byte UnusedUlaRegister = 0xfe;
    public const int InterruptRegister = 0;
    public const int ScreenStartLowRegister = 2;
    public const int ScreenStartHighRegister = 3;
    public const int TapeDataRegister = 4;
    public const int PageInterruptRegister = 5;
    public const int DividerRegister = 6;
    public const int ControlRegister = 7;
    public const int PaletteStartRegister = 8;
    public const byte InterruptSummary = 0x01;
    public const byte PowerOnInterrupt = 0x02;
    public const byte DisplayEndInterrupt = 0x04;
    public const byte ClockInterrupt = 0x08;
    public const byte ReceiveFullInterrupt = 0x10;
    public const byte TransmitEmptyInterrupt = 0x20;
    public const byte HighToneInterrupt = 0x40;
    public const byte StatusHigh = 0x80;
    public const byte EnabledInterruptMask = 0x7c;
    public const byte ClearDisplayEnd = 0x10;
    public const byte ClearClock = 0x20;
    public const byte ClearHighTone = 0x40;
    public const int CommsShift = 1;
    public const int CommsMask = 3;
    public const int TapeInputMode = 0;
    public const int SoundMode = 1;
    public const int TapeOutputMode = 2;
    public const int UnusedCommsMode = 3;
    public const int TapeMotorMask = 0x40;
    public const int CapsLockMask = 0x80;
    public const int TapeBaud = 1200;
    public const int TapeFrameBits = 10;
    public const int TapeDataBits = 8;
    public const int TapeReceivePhaseDelay = 4;
    public const int TapeCarrierBits = 10;
    public const int TapeStopBitPosition = 9;
    public const int TapeTransmitReadyBits = 1;
    public const int TapeFirstDataPosition = 1;
    public const int InitialSoundDivider = 0;
    public const int SoundHalfPeriodDivider = 32;
    public const int SpeakerAmplitude = 8192;

}
