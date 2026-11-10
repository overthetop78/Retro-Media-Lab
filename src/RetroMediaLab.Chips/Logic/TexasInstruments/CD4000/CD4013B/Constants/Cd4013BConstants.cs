namespace RetroMediaLab.Chips.Logic.TexasInstruments.CD4000.CD4013B.Constants;

internal static class Cd4013BConstants
{
    internal const int Channels = 2;
    internal const byte Q = 1;
    internal const byte NotQ = 2;
    internal const byte QKnown = 4;
    internal const byte NotQKnown = 8;
    internal const byte Unknown = 0;
    internal const byte Low = QKnown | NotQKnown | NotQ;
    internal const byte High = QKnown | NotQKnown | Q;
    internal const byte BothHigh = QKnown | NotQKnown | Q | NotQ;
    internal const long NanosecondsPerSecond = 1_000_000_000;
}
