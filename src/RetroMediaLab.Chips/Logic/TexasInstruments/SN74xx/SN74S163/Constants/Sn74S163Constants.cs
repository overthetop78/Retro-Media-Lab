namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74S163.Constants;

public static class Sn74S163Constants
{
    public const byte CounterMask = 0x0f;
    public const byte CarryMask = 0x10;
    internal const byte ClearHigh = 1;
    internal const byte LoadHigh = 2;
    internal const byte EnableP = 4;
    internal const byte EnableT = 8;
    internal const int DataShift = 4;
    internal const int PeriodicCountShift = 4;
    internal const int PeriodicInputShift = 8;
}
