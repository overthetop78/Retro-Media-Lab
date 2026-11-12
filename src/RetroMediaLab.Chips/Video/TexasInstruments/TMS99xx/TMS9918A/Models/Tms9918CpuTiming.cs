namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Models;

/// <summary>Délais CPU en ns. TI MP010A §5.5 : accès max 150 ns,
/// libération max 100 ns, pour la charge spécifiée de 300 pF. Le profil
/// choisit ces bornes ; elles ne sont pas des mesures typiques du silicium.</summary>
public sealed record Tms9918CpuTiming
{
    public static Tms9918CpuTiming SpecifiedMaximum { get; } = new(150, 100);
    public long AccessNanoseconds { get; }
    public long ReleaseNanoseconds { get; }

    public Tms9918CpuTiming(long accessNanoseconds, long releaseNanoseconds)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(accessNanoseconds);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(accessNanoseconds, 150);
        ArgumentOutOfRangeException.ThrowIfNegative(releaseNanoseconds);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(releaseNanoseconds, 100);
        AccessNanoseconds = accessNanoseconds;
        ReleaseNanoseconds = releaseNanoseconds;
    }
}
