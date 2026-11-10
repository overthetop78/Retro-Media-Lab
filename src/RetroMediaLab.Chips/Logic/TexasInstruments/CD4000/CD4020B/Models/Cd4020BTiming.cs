namespace RetroMediaLab.Chips.Logic.TexasInstruments.CD4000.CD4020B.Models;

/// <summary>TI SCHS030D, 25 °C, CL=50 pF, RL=200 kΩ digital propagation profile.</summary>
/// <remarks>Reference supply identifies a characterized table, not an analogue supply model.</remarks>
public sealed class Cd4020BTiming
{
    public static Cd4020BTiming Typical5Volts { get; } = new(5, 180, 100, 140);
    public static Cd4020BTiming Typical10Volts { get; } = new(10, 80, 40, 60);
    public static Cd4020BTiming Typical15Volts { get; } = new(15, 65, 30, 50);

    public int ReferenceSupplyVolts { get; }
    public long FirstStageNanoseconds { get; }
    public long SubsequentStageNanoseconds { get; }
    public long ResetNanoseconds { get; }
    public long MaximumClockFrequency { get; }
    public long FullRippleNanoseconds { get; }

    public Cd4020BTiming(int referenceSupplyVolts, long firstStageNanoseconds,
        long subsequentStageNanoseconds, long resetNanoseconds)
    {
        var limits = referenceSupplyVolts switch
        {
            5 => (First: 360L, Stage: 330L, Reset: 280L, Frequency: 3_500_000L),
            10 => (First: 160L, Stage: 80L, Reset: 120L, Frequency: 8_000_000L),
            15 => (First: 130L, Stage: 60L, Reset: 100L, Frequency: 12_000_000L),
            _ => throw new ArgumentOutOfRangeException(nameof(referenceSupplyVolts))
        };
        Validate(firstStageNanoseconds, limits.First);
        Validate(subsequentStageNanoseconds, limits.Stage);
        Validate(resetNanoseconds, limits.Reset);
        ReferenceSupplyVolts = referenceSupplyVolts;
        FirstStageNanoseconds = firstStageNanoseconds;
        SubsequentStageNanoseconds = subsequentStageNanoseconds;
        ResetNanoseconds = resetNanoseconds;
        MaximumClockFrequency = limits.Frequency;
        FullRippleNanoseconds = checked(firstStageNanoseconds + 13 * subsequentStageNanoseconds);
    }

    private static void Validate(long delay, long maximum)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(delay);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(delay, maximum);
    }
}
