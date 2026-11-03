namespace RetroMediaLab.Emulation.Contracts;

/// <summary>Transitions d'une révolution physique, exprimées en nanosecondes depuis l'index.</summary>
public sealed record EmulationDiskRevolution
{
    public EmulationDiskRevolution(uint indexTimeNanoseconds, IReadOnlyList<uint> fluxIntervalsNanoseconds)
    {
        ArgumentNullException.ThrowIfNull(fluxIntervalsNanoseconds);
        ArgumentOutOfRangeException.ThrowIfZero(indexTimeNanoseconds);
        if (fluxIntervalsNanoseconds.Any(interval => interval == 0))
            throw new ArgumentOutOfRangeException(nameof(fluxIntervalsNanoseconds));
        IndexTimeNanoseconds = indexTimeNanoseconds;
        FluxIntervalsNanoseconds = Array.AsReadOnly(fluxIntervalsNanoseconds.ToArray());
    }

    public uint IndexTimeNanoseconds { get; }
    public IReadOnlyList<uint> FluxIntervalsNanoseconds { get; }
}
