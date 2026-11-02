namespace RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Constants;

/// <summary>Bits de l'ADLC Motorola 6854 ; leur décodage BBC appartient à la carte réseau.</summary>
public static class Mc6854Constants
{
    public const int FifoSize = 3, MaximumFrameBytes = 65535, DefaultCyclesPerByte = 64;
    public const byte AddressControl = 0x01, ReceiveInterrupt = 0x02, TransmitInterrupt = 0x04;
    public const byte ReceiveDma = 0x08, TransmitDma = 0x10, Discontinue = 0x20, ReceiveReset = 0x40, TransmitReset = 0x80;
    public const byte Prioritize = 0x01, TwoByte = 0x02, FrameCompleteMode = 0x08, LastData = 0x10;
    public const byte ClearReceive = 0x20, ClearTransmit = 0x40, Rts = 0x80;
    public const byte FlagDetectEnable = 0x10, Loop = 0x20, Test = 0x40, TransmitAbort = 0x20;
    public const byte DataAvailable = 0x01, StatusTwoRequest = 0x02, LoopStatus = 0x04, FlagDetected = 0x08;
    public const byte NotClearToSend = 0x10, TransmitUnderrun = 0x20, TransmitAvailable = 0x40, Interrupt = 0x80;
    public const byte AddressPresent = 0x01, FrameValid = 0x02, Idle = 0x04, AbortReceived = 0x08;
    public const byte FcsError = 0x10, NoClock = 0x20, ReceiveOverrun = 0x40, ReceiveAvailable = 0x80;
    public const byte ReceiveErrors = FrameValid | AbortReceived | FcsError | NoClock | ReceiveOverrun;
}
