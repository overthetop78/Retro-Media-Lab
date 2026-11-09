namespace RetroMediaLab.Chips.Controllers.Acorn.SystemClock.Constants;

internal static class SystemClockConstants
{
    internal const long OscillatorFrequency = 24_000_000;
    internal const byte DivideByThreeSeed = 2;
    internal const byte Clock12 = 1;
    internal const byte Clock8 = 2;
    internal const byte Clock6 = 4;
    internal const byte Clock4 = 8;
    internal const byte Clock3 = 16;
    internal const byte Clock2 = 32;
    internal const byte Clock1 = 64;
}
