namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants;

/// <summary>Divisions numériques de 7Fh et phase des sorties CLKA/CLKB.</summary>
internal static class Acb4000ClockConstants
{
    internal const int EdgesPerCycle = 2;
    internal const int SystemQuarterDivider = 4;
    internal const int SystemHalfDivider = 2;
    internal const int SystemDirectDivider = 1;
    internal const int SystemQuarterSelection = 0;
    internal const int SystemHalfSelection = 1;
    internal const int SetupHalfCycles = 1;
    internal const int HoldHalfCycles = 1;
}
