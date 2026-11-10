using C = RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS393.Constants.Sn74Ls393Constants;

namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS393.Models;

/// <summary>Stage delays in nanoseconds, not accumulated CLK-to-output durations.</summary>
public sealed class Sn74Ls393Timing
{
    /// <summary>TI typical QA/QD/CLR, with calculated equal intermediate stages.
    /// QB=22 ns and QC=31 ns are estimates, not measured TI characteristics.</summary>
    public static Sn74Ls393Timing CalculatedTypical { get; } = new(12, 13, 9, 9, 9, 24);

    public long QaRiseNanoseconds { get; }
    public long QaFallNanoseconds { get; }
    public long QbStageNanoseconds { get; }
    public long QcStageNanoseconds { get; }
    public long QdStageNanoseconds { get; }
    public long ClearNanoseconds { get; }
    public long MaximumRippleNanoseconds { get; }

    public Sn74Ls393Timing(long qaRiseNanoseconds, long qaFallNanoseconds,
        long qbStageNanoseconds, long qcStageNanoseconds, long qdStageNanoseconds, long clearNanoseconds)
    {
        Validate(qaRiseNanoseconds, C.MaximumQaDelay);
        Validate(qaFallNanoseconds, C.MaximumQaDelay);
        Validate(qbStageNanoseconds, C.MaximumQdDelay);
        Validate(qcStageNanoseconds, C.MaximumQdDelay);
        Validate(qdStageNanoseconds, C.MaximumQdDelay);
        Validate(clearNanoseconds, C.MaximumClearDelay);
        var ripple = checked(qaFallNanoseconds + qbStageNanoseconds + qcStageNanoseconds + qdStageNanoseconds);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(ripple, C.MaximumQdDelay);
        QaRiseNanoseconds = qaRiseNanoseconds;
        QaFallNanoseconds = qaFallNanoseconds;
        QbStageNanoseconds = qbStageNanoseconds;
        QcStageNanoseconds = qcStageNanoseconds;
        QdStageNanoseconds = qdStageNanoseconds;
        ClearNanoseconds = clearNanoseconds;
        MaximumRippleNanoseconds = Math.Max(qaRiseNanoseconds, ripple);
    }

    private static void Validate(long delay, long maximum)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(delay);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(delay, maximum);
    }
}
