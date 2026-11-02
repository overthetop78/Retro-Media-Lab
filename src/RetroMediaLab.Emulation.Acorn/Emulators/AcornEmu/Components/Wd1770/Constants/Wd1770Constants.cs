namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Wd1770.Constants;

/// <summary>Registres, états et cadences du contrôleur double densité WD1770.</summary>
internal static class Wd1770Constants
{
    internal const byte Busy = 1;
    internal const byte DataRequest = 2;
    internal const byte LostData = 4;
    internal const byte CrcError = 8;
    internal const byte NotFound = 16;
    internal const byte WriteProtected = 64;
    internal const byte Motor = 128;
    internal const int CpuClock = 2_000_000;
    internal const int DoubleDensityBytesPerSecond = 31_250;
    internal const int SingleDensityBytesPerSecond = 15_625;
    internal const int RevolutionsPerSecond = 5;
    internal const int SectorSizeBase = 128;
    internal const int MaximumSectorSizeCode = 7;
    internal const int DefaultDriveCount = 2;
    internal const int MaximumDriveCount = 4;
    internal static IReadOnlyList<int> StepMilliseconds { get; } = Array.AsReadOnly<int>([6, 12, 20, 30]);
    internal static IReadOnlyList<int> Wd1772StepMilliseconds { get; } = Array.AsReadOnly<int>([6, 12, 2, 3]);
}
