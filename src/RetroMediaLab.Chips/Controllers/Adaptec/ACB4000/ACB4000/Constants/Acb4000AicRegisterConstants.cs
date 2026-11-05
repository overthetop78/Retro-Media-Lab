namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants;

/// <summary>Décodage interne AIC-010, manuel constructeur Rev B pages 11–25.</summary>
public static class Acb4000AicRegisterConstants
{
    public const byte EccControl = 0x71;
    public const byte MiddleSyndrome = 0x72;
    public const byte HighSyndrome = 0x73;
    public const byte FirstPolynomial = 0x74;
    public const byte LastPolynomial = 0x77;
    public const byte Branch = 0x78;
    public const byte StartStatus = 0x79;
    public const byte Operation = 0x7a;
    public const byte WriteAddressMark = 0x7b;
    public const byte DetectAddressMark = 0x7c;
    public const byte GpioControl = 0x7d;
    public const byte Gpio = 0x7e;
    public const byte ClockStack = 0x7f;
    public const byte External6E = 0x6e;
    public const byte External6F = 0x6f;
    public const byte GpioMask = 0x0f;
    public const byte BranchInputMask = 0x10;
    public const byte SequencerOutputMask = 0x20;
    public const int DecodeShift = 4;
    public const byte CompareStartMask = 0x07;
    public const byte ClocksHighImpedance = 0x08;
    public const byte FourTransferClocks = 0x10;
    public const int NonTransferClockShift = 6;
    public const byte NonTransferClockMask = 0x03;
    public const byte UnstableNonTransferClock = 0x03;
    public const int Write6EPin = 0;
    public const int Read6EPin = 1;
    public const int Write6FPin = 2;
    public const int Read6FPin = 3;
    public const int TwoCyclesPerByte = 2;
    public const int FourCyclesPerByte = 4;
}
