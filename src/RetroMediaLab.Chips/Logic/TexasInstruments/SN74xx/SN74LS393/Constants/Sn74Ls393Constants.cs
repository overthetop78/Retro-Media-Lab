namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS393.Constants;

internal static class Sn74Ls393Constants
{
    internal const int Channels = 2;
    internal const int Stages = 4;
    internal const int RippleOffsetBase = 5;
    internal const byte OutputMask = 15;
    internal const byte QbMask = 2;
    internal const byte QbQcMask = 6;
    internal const byte QcMask = 4;
    internal const byte QcQdMask = 12;
    internal const long NanosecondsPerSecond = 1_000_000_000;
    internal const long MaximumClockFrequency = 25_000_000;
    internal const long MaximumQaDelay = 20;
    internal const long MaximumQdDelay = 60;
    internal const long MaximumClearDelay = 39;
}
