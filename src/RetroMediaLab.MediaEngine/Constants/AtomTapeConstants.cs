namespace RetroMediaLab.MediaEngine.Constants;

/// <summary>En-têtes TAP/ATM Wouter Ras et blocs série du magnétophone Atom.</summary>
internal static class AtomTapeConstants
{
    internal const int FileHeaderBytes = 22;
    internal const int FileNameBytes = 16;
    internal const int LoadAddressOffset = 16;
    internal const int ExecutionAddressOffset = 18;
    internal const int LengthOffset = 20;
    internal const int MaximumBlockBytes = 256;
    internal const int SyncBytes = 4;
    internal const byte Sync = 0x2a;
    internal const byte NameTerminator = 0x0d;
    internal const byte MoreBlocks = 0x80;
    internal const byte ContainsData = 0x40;
    internal const byte Continuation = 0x20;
    internal const int EndAddressShift = 11;
    internal const int FlagHistoryShift = 2;
    internal const int FlagHistoryMask = 0x1f;
    internal const int Baud = 300;
    internal const int StopBits = 2;
    internal const int DataBits = 8;
    internal const byte NoParity = (byte)'N';
    internal const int CarrierCycles = 9600;
    internal const int HeaderDataCarrierCycles = 1200;
    internal const int GapSeconds = 2;
    internal const int InitialPhase = 180;
}
