namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS93.Models;

/// <summary>Stage delays in ns. The C and D stage delays in CalculatedTypical
/// are differences of TI's cumulative typical delays, not separate measurements.</summary>
public sealed class Sn74Ls93Timing
{
    public static Sn74Ls93Timing CalculatedTypical { get; } = new(10, 12, 10, 14, 7, 9, 11, 11, 26);
    public long QaRise { get; }
    public long QaFall { get; }
    public long QbRise { get; }
    public long QbFall { get; }
    public long QcRise { get; }
    public long QcFall { get; }
    public long QdRise { get; }
    public long QdFall { get; }
    public long Reset { get; }
    public long MaximumRipple { get; }

    public Sn74Ls93Timing(long qaRise, long qaFall, long qbRise, long qbFall,
        long qcRise, long qcFall, long qdRise, long qdFall, long reset)
    {
        Validate(qaRise, 16); Validate(qaFall, 18);
        Validate(qbRise, 16); Validate(qbFall, 21);
        Validate(qcRise, 32); Validate(qcFall, 35);
        Validate(qdRise, 51); Validate(qdFall, 51); Validate(reset, 40);
        var cRise = checked(qbFall + qcRise);
        var cFall = checked(qbFall + qcFall);
        var dRise = checked(cFall + qdRise);
        var dFall = checked(cFall + qdFall);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(cRise, 32);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(cFall, 35);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(dRise, 51);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(dFall, 51);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(checked(qaFall + Math.Max(dRise, dFall)), 70);
        QaRise = qaRise; QaFall = qaFall; QbRise = qbRise; QbFall = qbFall;
        QcRise = qcRise; QcFall = qcFall; QdRise = qdRise; QdFall = qdFall; Reset = reset;
        MaximumRipple = Math.Max(Math.Max(qaRise, qaFall), Math.Max(dRise, dFall));
    }
    private static void Validate(long value, long maximum)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, maximum);
    }
}
