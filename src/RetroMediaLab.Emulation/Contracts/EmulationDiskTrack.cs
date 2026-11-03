namespace RetroMediaLab.Emulation.Contracts;

/// <summary>Piste physique d'un média, sans interprétation des registres d'un contrôleur.</summary>
public sealed record EmulationDiskTrack
{
    public EmulationDiskTrack(IReadOnlyList<EmulationDiskRevolution> revolutions)
    {
        ArgumentNullException.ThrowIfNull(revolutions);
        if (revolutions.Count == 0 || revolutions.Any(revolution => revolution is null))
            throw new ArgumentException(null, nameof(revolutions));
        Revolutions = Array.AsReadOnly(revolutions.ToArray());
    }

    public IReadOnlyList<EmulationDiskRevolution> Revolutions { get; }
}
