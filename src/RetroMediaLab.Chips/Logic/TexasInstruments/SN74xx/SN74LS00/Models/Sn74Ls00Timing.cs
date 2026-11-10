using C = RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS00.Constants.Sn74Ls00Constants;

namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS00.Models;

/// <summary>TI LS00, 5 V, 25 °C, CL=15 pF, RL=2 kΩ; delays in ns.</summary>
public sealed class Sn74Ls00Timing
{
    public static Sn74Ls00Timing Typical { get; } = new(9, 10);
    public long RiseNanoseconds { get; }
    public long FallNanoseconds { get; }
    public Sn74Ls00Timing(long riseNanoseconds, long fallNanoseconds)
    {
        Validate(riseNanoseconds); Validate(fallNanoseconds);
        RiseNanoseconds = riseNanoseconds;
        FallNanoseconds = fallNanoseconds;
    }
    private static void Validate(long duration)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(duration);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(duration, C.MaximumPropagationNanoseconds);
    }
}
