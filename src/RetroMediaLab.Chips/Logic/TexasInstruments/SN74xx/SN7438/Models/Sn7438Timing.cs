namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN7438.Models;

/// <summary>Effective propagation in ns for the documented load. TI SDLS105 p3:
/// 5 V, 25 °C, RL=133 Ω, CL=45 pF. Another load requires its own characterized profile;
/// the measured output-rise load delay must not be applied twice.</summary>
public sealed class Sn7438Timing
{
    public static Sn7438Timing Typical { get; } = new(14, 11);
    public static Sn7438Timing CharacterizedMaximum { get; } = new(22, 18);
    public long RiseNanoseconds { get; }
    public long FallNanoseconds { get; }

    public Sn7438Timing(long riseNanoseconds, long fallNanoseconds)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(riseNanoseconds);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fallNanoseconds);
        RiseNanoseconds = riseNanoseconds;
        FallNanoseconds = fallNanoseconds;
    }
}
