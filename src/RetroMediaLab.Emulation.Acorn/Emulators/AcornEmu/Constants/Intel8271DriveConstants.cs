namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants;

/// <summary>Unités du câblage numérique des lecteurs 8271, sans description de conteneur ou de machine.</summary>
internal static class Intel8271DriveConstants
{
    internal const uint StandardSingleShotWindowNanoseconds = 2850;
    internal const uint NanosecondsPerSecond = 1_000_000_000;
    internal const uint RevolutionNanoseconds = 200_000_000;
    internal const uint IndexNanoseconds = 2_000_000;
    internal const string EncodingId = "iso.fm";
    internal const string PhysicalLayoutAttribute = "fmPhysicalLayout";
    internal const int PhysicalLayoutEnabled = 1;
}
