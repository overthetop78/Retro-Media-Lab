using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants;

/// <summary>Lecteurs ST-506/412 du Winchester Acorn ; manuel OEM Seagate avril 1982, sections 4.2–4.3.</summary>
internal static class WinchesterDriveConstants
{
    internal const ushort RegisterStart = 0xfc40;
    internal const ushort RegisterEnd = 0xfc44;
    internal const EmulationDiskTrackEncoding TrackEncoding = EmulationDiskTrackEncoding.AdaptecMfm;
    internal const uint CellNanoseconds = NanosecondsPerSecond / ReadClock;
    internal const int OwnerClock = 2_000_000;
    internal const int SequencerClock = Acb4000SearchTimingConstants.RevisionBSequencerClock;
    internal const int DriveCount = 2;
    internal const int NormalSpeedPercent = 100;
    internal const int MaximumSpeedPercent = 800;
    internal const int UnlimitedSpeed = 0;
    internal const int MaximumTicksPerCall = 1024;
    internal const uint MaximumDeferredTicks = OwnerClock;
    internal const uint ReadClock = 10_000_000;
    internal const uint RevolutionsPerSecond = 60;
    internal const uint NanosecondsPerSecond = 1_000_000_000;
    internal const uint RevolutionNanoseconds = (NanosecondsPerSecond + RevolutionsPerSecond - 1) / RevolutionsPerSecond;
    internal const uint IndexPulseNanoseconds = 200_000;
    internal const uint HalfCellNanoseconds = 50;
    internal const uint SeekSettleNanoseconds = 15_000_000;
}
