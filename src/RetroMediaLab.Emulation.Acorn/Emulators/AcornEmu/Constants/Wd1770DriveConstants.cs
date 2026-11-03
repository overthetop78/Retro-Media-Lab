namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants;

internal static class Wd1770DriveConstants
{
    internal const int OwnerClock = 2_000_000;
    internal const int DefaultDriveCount = 2;
    internal const int MaximumDriveCount = 4;
    internal const int NormalSpeedPercent = 100;
    internal const int MaximumClockMultiplier = 64;
    internal const uint NanosecondsPerSecond = 1_000_000_000;
    internal const ulong HalfNanosecondsPerSecond = 500_000_000;
    internal const uint RevolutionNanoseconds = 200_000_000;
    internal const uint IndexNanoseconds = 2_000_000;
    internal const uint MfmCellNanoseconds = 2_000;
    internal const uint FmCellNanoseconds = 4_000;
    internal const uint MfmReadPulseNanoseconds = 250;
    internal const uint FmReadPulseNanoseconds = 500;
    internal const string FmEncoding = "iso.fm";
    internal const string MfmEncoding = "iso.mfm";
    internal const string FmPhysicalLayout = "fmPhysicalLayout";
    internal const string MfmPhysicalLayout = "mfmPhysicalLayout";
    internal const int PhysicalLayoutEnabled = 1;
}
