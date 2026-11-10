namespace RetroMediaLab.Chips.Controllers.Acorn.Vdu80CharacterGenerator.Constants;

internal static class Vdu80CharacterConstants
{
    internal const long CharacterFrequency = 2_000_000;
    internal const long DotFrequency = 12_000_000;
    internal const long HalfDotFrequency = 24_000_000;
    internal const long NanosecondsPerSecond = 1_000_000_000;
    internal const long CharacterPeriodNanoseconds = NanosecondsPerSecond / CharacterFrequency;
    internal const long DotPeriodsPerCharacter = DotFrequency / CharacterFrequency;
    internal const long LoadPulseNanoseconds = 20; // Handbook typical, not a board measurement.
    internal const long DisplayDelayNanoseconds = 500; // Handbook: one character.
    internal const long RomAddressNanoseconds = 450; // National maximums.
    internal const long RomSelectNanoseconds = 120;
    internal const long RomDisableNanoseconds = 100;
    internal const byte RomSelects = 4; // CS1/2 low, CS3 high, figure 18.
    internal const byte GroundedInputs = 0x83; // A, B, H.
    internal const byte GlyphInputs = 0x7c; // ROM D3..D7 -> C..G.
    internal const double CursorSeriesOhms = 1000;
    internal const double CursorShuntOhms = 2700;
    internal const double CursorCapacitancePicofarads = 330;
    // Calculated reference profile, not a measured/guaranteed TTL threshold.
    internal const double CursorSwitchingFraction = 0.5;
    internal const double InitialCursorCharge = 0;
}
