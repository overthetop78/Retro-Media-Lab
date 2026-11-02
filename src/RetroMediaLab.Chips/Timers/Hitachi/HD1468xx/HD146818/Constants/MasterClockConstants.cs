namespace RetroMediaLab.Chips.Timers.Hitachi.HD1468xx.HD146818.Constants;

public static class MasterClockConstants
{
    public const int Seconds = 0;
    public const int Minutes = 2;
    public const int Hours = 4;
    public const int DayOfWeek = 6;
    public const int Day = 7;
    public const int Month = 8;
    public const int Year = 9;
    public const int StatusA = 10;
    public const int StatusB = 11;
    public const int StatusC = 12;
    public const int StatusD = 13;
    public const int ConfigurationStart = 14;
    public const byte BinaryData = 0x04;
    public const byte TwentyFourHour = 0x02;
    public const byte StopUpdates = 0x80;
    public const byte Afternoon = 0x80;
    public const byte ValidRam = 0x80;
    public const byte SevenBits = 0x7f;
    public const int HoursPerHalfDay = 12;
    public const int YearCenturyThreshold = 80;
    public const int OldCentury = 1900;
    public const int NewCentury = 2000;
    public const int CenturyYears = 100;
    public const string TimestampFormat = "yyyy-MM-dd HH:mm:ss";
    public static IReadOnlyList<int> TimeFields { get; } = [Seconds, Minutes, Hours, DayOfWeek, Day, Month, Year];
}
