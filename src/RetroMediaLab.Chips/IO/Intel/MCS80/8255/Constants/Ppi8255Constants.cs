namespace RetroMediaLab.Chips.IO.Intel.MCS80.Chip8255.Constants;

public static class Ppi8255Constants
{
    public const int PortA = 0;
    public const int PortB = 1;
    public const int PortC = 2;
    public const int Control = 3;
    public const int RegisterMask = 3;
    public const byte ModeSet = 0x80;
    public const byte GroupAModeMask = 0x60;
    public const byte GroupAModeOne = 0x20;
    public const byte GroupAModeTwo = 0x40;
    public const byte GroupBModeOne = 0x04;
    public const byte InputA = 0x10;
    public const byte InputB = 0x02;
    public const byte InputUpperC = 0x08;
    public const byte InputLowerC = 0x01;
    public const byte UpperC = 0xf0;
    public const byte LowerC = 0x0f;
    public const byte ResetControl = 0x9b;
    public const int BitSelectionShift = 1;
    public const int BitSelectionMask = 7;
    public const int BitSet = 1;
    public const byte InterruptA = 0x08;
    public const byte InputStrobeA = 0x10;
    public const byte InputBufferA = 0x20;
    public const byte OutputAcknowledgeA = 0x40;
    public const byte OutputBufferA = 0x80;
    public const byte InterruptB = 0x01;
    public const byte BufferB = 0x02;
    public const byte HandshakeB = 0x04;
    public const byte ObserveA = 0x01;
    public const byte ObserveB = 0x02;
    public const byte ObserveC = 0x04;
    public const byte ObserveAll = ObserveA | ObserveB | ObserveC;
}
