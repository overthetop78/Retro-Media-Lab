namespace RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Constants;

internal static class ElectronRamConstants
{
    internal const int NibbleMask = 0x0f;
    internal const int PinGroupShift = 4;
    internal const int OddPinGroupMask = NibbleMask << PinGroupShift;
    internal const int PairMask = 0x33;
    internal const int EvenMask = 0x55;
    internal const int OddMask = 0xaa;
    internal const int GroupShift = 2;
    internal const int LaneShift = 1;
    internal const int RowLowMask = 0x1f;
    internal const int RowLowShift = 3;
    internal const int RowBitFiveMask = 0x04;
    internal const int RowBitFiveShift = 3;
    internal const int RowBitSixMask = 0x02;
    internal const int RowBitSixShift = 5;
    internal const int RowBitSevenMask = 0x0100;
    internal const int RowBitSevenShift = 1;
    internal const int ColumnLowMask = 0x3f;
    internal const int ColumnLowShift = 9;
    internal const int ColumnBitSixMask = 0x01;
    internal const int ColumnBitSixShift = 6;
    internal const int ColumnParityMask = 0x80;
    internal const int RamEndAddress = 0x8000;
    internal const int RowLatchPhase = 0;
    internal const int EvenColumnPhase = 1;
    internal const int EvenIdlePhase = 2;
    internal const int EvenDataPhase = 3;
    internal const int OddColumnPhase = 4;
    internal const int OddIdlePhase = 5;
    internal const int OddDataPhase = 6;
    internal const int RecoveryPhase = 7;
}
