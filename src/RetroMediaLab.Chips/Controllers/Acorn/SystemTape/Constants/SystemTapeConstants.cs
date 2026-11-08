namespace RetroMediaLab.Chips.Controllers.Acorn.SystemTape.Constants;

internal static class SystemTapeConstants
{
    // IC5/IC3 reset IC1 when Q5, Q4 and Q2 are all high: 16 + 8 + 2.
    internal const int Divider = 26;
    internal const int CounterMask = 127;
    internal const int CarrierBit = 64;
    internal const int MarkBit = 8;
    internal const int SpaceBit = 16;
    internal const int Q3Period = 8;
    internal const int Q3EdgesPerDivision = 3;
    internal const byte CarrierInputPin = 0x20;
    internal const byte DataInputPin = 0x80;
}
