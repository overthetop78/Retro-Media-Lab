namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS74A.Constants;

internal static class Sn74Ls74Constants
{
    internal const int Channels = 2;
    internal const byte QMask = 1;
    internal const byte NotQMask = 2;
    internal const byte KnownMask = 4;
    internal const byte UnknownOutputs = 0;
    internal const byte LowOutputs = KnownMask | NotQMask;
    internal const byte HighOutputs = KnownMask | QMask;
}
