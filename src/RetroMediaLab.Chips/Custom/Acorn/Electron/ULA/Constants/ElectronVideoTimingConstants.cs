namespace RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Constants;

/// <summary>Nominal 16 MHz boundaries, with the first active pixel as phase origin.</summary>
internal static class ElectronVideoTimingConstants
{
    internal const int MasterClock = 16_000_000;
    internal const int MasterClocksPerCpuClock = MasterClock / ElectronUlaConstants.CpuClock;
    internal const int CpuClockLowPoints = MasterClocksPerCpuClock / 2;
    internal const int CpuClockTransitionMask = CpuClockLowPoints - 1;
    internal const int LowBandwidthFetchOffset = MasterClocksPerCpuClock;
    internal const int ClocksPerLine = 1_024;
    internal const int ShortFieldLines = 312;
    internal const int LongFieldLines = 313;
    internal const int ShortFieldClocks = ShortFieldLines * ClocksPerLine;
    internal const int LongFieldClocks = LongFieldLines * ClocksPerLine;
    internal const int PictureClocks = ShortFieldClocks + LongFieldClocks;
    internal const int RtcPeriod = PictureClocks / 2;
    internal const int RtcFirstEdge = 101_360;
    internal const int GraphicsDisplayEnd = 261_888;
    internal const int TextDisplayEnd = 255_744;
    internal const int HorizontalSyncStart = 766;
    internal const int HorizontalSyncEnd = 830;
    internal const int VerticalSyncFirstEdge = 287_728;
    internal const int VerticalSyncLength = 2_560;
}
