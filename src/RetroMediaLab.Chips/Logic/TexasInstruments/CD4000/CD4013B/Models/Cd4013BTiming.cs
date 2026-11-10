namespace RetroMediaLab.Chips.Logic.TexasInstruments.CD4000.CD4013B.Models;

/// <summary>TI SCHS023E §6.6 at 25 °C, CL=50 pF and RL=20 kΩ.</summary>
public sealed class Cd4013BTiming
{
    public static Cd4013BTiming Typical5Volts { get; } = new(5, 150, 150, 200);
    public static Cd4013BTiming Typical10Volts { get; } = new(10, 65, 65, 85);
    public static Cd4013BTiming Typical15Volts { get; } = new(15, 45, 45, 60);
    public int ReferenceSupplyVolts { get; }
    public long ClockNanoseconds { get; }
    public long AsynchronousRiseNanoseconds { get; }
    public long AsynchronousFallNanoseconds { get; }
    public long MaximumClockFrequency { get; }

    public Cd4013BTiming(int referenceSupplyVolts, long clockNanoseconds,
        long asynchronousRiseNanoseconds, long asynchronousFallNanoseconds)
    {
        var limits = referenceSupplyVolts switch
        {
            5 => (Clock: 300L, Rise: 300L, Fall: 400L, Frequency: 3_500_000L),
            10 => (Clock: 130L, Rise: 130L, Fall: 170L, Frequency: 8_000_000L),
            15 => (Clock: 90L, Rise: 90L, Fall: 120L, Frequency: 12_000_000L),
            _ => throw new ArgumentOutOfRangeException(nameof(referenceSupplyVolts))
        };
        Validate(clockNanoseconds, limits.Clock);
        Validate(asynchronousRiseNanoseconds, limits.Rise);
        Validate(asynchronousFallNanoseconds, limits.Fall);
        ReferenceSupplyVolts = referenceSupplyVolts;
        ClockNanoseconds = clockNanoseconds;
        AsynchronousRiseNanoseconds = asynchronousRiseNanoseconds;
        AsynchronousFallNanoseconds = asynchronousFallNanoseconds;
        MaximumClockFrequency = limits.Frequency;
    }

    private static void Validate(long delay, long maximum)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(delay);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(delay, maximum);
    }
}
