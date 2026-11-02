namespace RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Constants;

/// <summary>Registres, états et cadences du contrôleur double densité WD1770.</summary>
public static class Wd1770Constants
{
    public const byte Busy = 1;
    public const byte DataRequest = 2;
    public const byte LostData = 4;
    public const byte CrcError = 8;
    public const byte NotFound = 16;
    public const byte WriteProtected = 64;
    public const byte Motor = 128;
    public const int CpuClock = 2_000_000;
    public const int DoubleDensityBytesPerSecond = 31_250;
    public const int SingleDensityBytesPerSecond = 15_625;
    public const int RevolutionsPerSecond = 5;
    public const int SectorSizeBase = 128;
    public const int MaximumSectorSizeCode = 7;
    public const int DefaultDriveCount = 2;
    public const int MaximumDriveCount = 4;
    public static IReadOnlyList<int> StepMilliseconds { get; } = Array.AsReadOnly<int>([6, 12, 20, 30]);
    public static IReadOnlyList<int> Wd1772StepMilliseconds { get; } = Array.AsReadOnly<int>([6, 12, 2, 3]);
}
