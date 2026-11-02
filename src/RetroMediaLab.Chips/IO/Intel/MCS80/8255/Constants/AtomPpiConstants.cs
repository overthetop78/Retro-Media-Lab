namespace RetroMediaLab.Chips.IO.Intel.MCS80.Chip8255.Constants;

public static class AtomPpiConstants
{
    public const int PortA = 0;
    public const int PortB = 1;
    public const int PortC = 2;
    public const int Control = 3;
    public const int RegisterMask = 3;
    public const byte ModeSet = 0x80;
    public const byte UnsupportedHandshakeModes = 0x64;
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
}
