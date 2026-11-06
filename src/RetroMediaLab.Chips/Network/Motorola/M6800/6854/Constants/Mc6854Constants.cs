namespace RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Constants;

/// <summary>Bits de l'ADLC Motorola 6854 ; leur décodage BBC appartient à la carte réseau.</summary>
public static class Mc6854Constants
{
    public const int StateFormatVersion = 1;
    public const int FifoSize = 3;
    public const int MaximumFrameBytes = 65535;
    public const int DefaultCyclesPerByte = 64;
    public const int BitsPerOctet = 8;
    public const int MinimumInformationBits = 5;
    public const int WordLengthMask = 3;
    public const int TransmitWordLengthShift = 1;
    public const int ReceiveWordLengthShift = 3;
    public const int RegisterMask = 3;
    public const int FcsBits = 16;
    public const int ReceiveLookaheadBits = 24;
    public const int StuffingOnes = 5;
    public const int FlagOnes = 6;
    public const int AbortOnes = 7;
    public const int IdleOnes = 15;
    public const int MinimumFrameBits = 25;
    public const int MinimumValidFrameBits = 32;
    public const int ReportableAbortBits = 26;
    public const int ShortAbortBits = 8;
    public const int LongAbortBits = 16;
    public const int RtsReleaseBits = 2;
    public const byte Flag = 0x7e;
    public const ushort CrcInitial = 0xffff;
    public const ushort CrcPolynomial = 0x8408;
    public const ushort CrcResidue = 0xf0b8;
    public const byte ExtensionAddressEnd = 0x01;
    public const byte ExtensionLogicalContinue = 0x80;

    public const byte AddressControl = 0x01;
    public const byte ReceiveInterrupt = 0x02;
    public const byte TransmitInterrupt = 0x04;
    public const byte ReceiveDma = 0x08;
    public const byte TransmitDma = 0x10;
    public const byte Discontinue = 0x20;
    public const byte ReceiveReset = 0x40;
    public const byte TransmitReset = 0x80;

    public const byte Prioritize = 0x01;
    public const byte TwoByte = 0x02;
    public const byte FlagIdle = 0x04;
    public const byte FrameCompleteMode = 0x08;
    public const byte LastData = 0x10;
    public const byte ClearReceive = 0x20;
    public const byte ClearTransmit = 0x40;
    public const byte Rts = 0x80;

    public const byte LogicalControl = 0x01;
    public const byte ExtendedControl = 0x02;
    public const byte ExtendedAddress = 0x04;
    public const byte ZeroIdle = 0x08;
    public const byte FlagDetectEnable = 0x10;
    public const byte Loop = 0x20;
    public const byte Test = 0x40;
    public const byte LoopOnline = 0x80;

    public const byte DoubleFlag = 0x01;
    public const byte TransmitAbort = 0x20;
    public const byte ExtendedAbort = 0x40;
    public const byte Nrzi = 0x80;

    public const byte DataAvailable = 0x01;
    public const byte StatusTwoRequest = 0x02;
    public const byte LoopStatus = 0x04;
    public const byte FlagDetected = 0x08;
    public const byte NotClearToSend = 0x10;
    public const byte TransmitUnderrun = 0x20;
    public const byte TransmitAvailable = 0x40;
    public const byte Interrupt = 0x80;

    public const byte AddressPresent = 0x01;
    public const byte FrameValid = 0x02;
    public const byte Idle = 0x04;
    public const byte AbortReceived = 0x08;
    public const byte FcsError = 0x10;
    public const byte DataCarrierDetect = 0x20;
    public const byte NoClock = DataCarrierDetect;
    public const byte ReceiveOverrun = 0x40;
    public const byte ReceiveAvailable = 0x80;
    public const byte ReceiveErrors = FrameValid | AbortReceived | FcsError | NoClock | ReceiveOverrun;
}
