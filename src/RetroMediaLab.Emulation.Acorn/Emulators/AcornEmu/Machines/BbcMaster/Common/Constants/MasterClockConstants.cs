namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMaster.Common.Constants;

internal static class MasterClockConstants
{
    internal const int Seconds = 0;
    internal const int Minutes = 2;
    internal const int Hours = 4;
    internal const int DayOfWeek = 6;
    internal const int Day = 7;
    internal const int Month = 8;
    internal const int Year = 9;
    internal const int StatusA = 10;
    internal const int StatusB = 11;
    internal const int StatusC = 12;
    internal const int StatusD = 13;
    internal const int ConfigurationStart = 14;
    internal const byte BinaryData = 0x04;
    internal const byte TwentyFourHour = 0x02;
    internal const byte StopUpdates = 0x80;
    internal const byte Afternoon = 0x80;
    internal const byte ValidRam = 0x80;
    internal const byte SevenBits = 0x7f;
    internal const int HoursPerHalfDay = 12;
    internal const int YearCenturyThreshold = 80;
    internal const int OldCentury = 1900;
    internal const int NewCentury = 2000;
    internal const int CenturyYears = 100;
    internal const string TimestampFormat = "yyyy-MM-dd HH:mm:ss";
    internal static IReadOnlyList<int> TimeFields { get; } = [Seconds, Minutes, Hours, DayOfWeek, Day, Month, Year];
    internal static ReadOnlySpan<byte> Configuration => [0, 0xfe, 0, 0xeb, 0, 0xc9, 0xff, 0xff, 0x12, 0, 0x17, 0xc8, 0x1e, 5, 0, 0x35, 0xa6];
}
