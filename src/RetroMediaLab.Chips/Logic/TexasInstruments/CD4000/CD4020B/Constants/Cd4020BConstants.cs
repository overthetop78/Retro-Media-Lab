namespace RetroMediaLab.Chips.Logic.TexasInstruments.CD4000.CD4020B.Constants;

internal static class Cd4020BConstants
{
    internal const int Stages = 14;
    internal const ushort CounterMask = 0x3fff;
    internal const ushort ExposedMask = 0x3ff9; // Q1 and Q4–Q14; Q2/Q3 remain internal.
    internal const long NanosecondsPerSecond = 1_000_000_000;
}
