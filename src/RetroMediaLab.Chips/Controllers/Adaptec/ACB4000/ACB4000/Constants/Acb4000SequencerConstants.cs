namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants;

/// <summary>Format du microprogramme AIC-010 et registres de commande, manuel Rev B.</summary>
public static class Acb4000SequencerConstants
{
    public const int StateCount = 24;
    public const int ColumnCount = 4;
    public const int ColumnStride = 0x20;
    public const byte RamBase = 0x80;
    public const byte StateMask = 0x1f;
    public const byte StopState = 0x1f;
    public const int NextColumn = 0;
    public const int ControlColumn = 1;
    public const int CountColumn = 2;
    public const int DataColumn = 3;
    public const int BranchShift = 5;
    public const byte BranchMask = 0xe0;
    public const int BitsPerByte = 8;
    public const int LastBit = BitsPerByte - 1;
    public const int GateBit = 3;
    public const int StackLength = 8;
    public const byte Transfer = 0x01;
    public const byte Compare = 0x02;
    public const byte Output = 0x04;
    public const byte InvalidNrz = 0x08;
    public const byte Stack = 0x10;
    public const byte ResetWriteGate = 0x20;
    public const byte SetReadGate = 0x40;
    public const byte SetWriteGate = 0x80;
    public const byte AddressMark = 0x20;
    public const byte Ecc = 0x40;
    public const byte EnableBitRing = 0x80;
    public const byte TypeMask = AddressMark | Ecc | EnableBitRing;
    public const byte StatusEqual = 0x01;
    public const byte StatusLow = 0x02;
    public const byte StatusEccError = 0x04;
    public const byte StatusStopped = 0x10;
    public const byte StatusBranch = 0x20;
    public const byte StatusTransfer = 0x40;
    public const byte StatusAddressMark = 0x80;
    public const byte IndexPast = 0x01;
    public const byte SectorPast = 0x02;
    public const byte NrzPast = 0x04;
    public const byte OperationUnusedHigh = 0x08;
    public const byte Search = 0x10;
    public const byte SuppressTransfer = 0x20;
    public const byte InhibitCarry = 0x80;
    public const byte OperationWritable = Search | SuppressTransfer | InhibitCarry;
}
