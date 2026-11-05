using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;
using C = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000SearchTimingConstants;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

/// <summary>Échéance des boucles de polling 8085, à deux cadences explicitement fournies.</summary>
/// <remarks>Begin représente l'entrée dans la boucle, après chargement des registres.
/// Le propriétaire doit traiter les autres instructions et la reconnaissance du statut matériel.
/// Les fréquences ne sont ni déduites de l'hôte BBC ni choisies implicitement.</remarks>
public sealed class Acb4000SearchDeadline
{
    private readonly int ownerClock;
    private readonly int sequencerClock;
    private long remaining;
    private Acb4000SearchWait expired;

    public Acb4000SearchWait Wait { get; private set; }

    public Acb4000SearchDeadline(int ticksPerSecond, int sequencerTStatesPerSecond)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ticksPerSecond);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sequencerTStatesPerSecond);
        ownerClock = ticksPerSecond;
        sequencerClock = sequencerTStatesPerSecond;
    }

    public void BeginIndexWait(int setupTStates = 0) => Begin(Acb4000SearchWait.Index, C.IndexWaitTStates, setupTStates);
    public void BeginFirstFieldWait(int setupTStates = 0) => Begin(Acb4000SearchWait.Field, C.FirstFieldWaitTStates, setupTStates);
    public void BeginFollowingFieldWait() => Begin(Acb4000SearchWait.Field, C.FollowingFieldWaitTStates);

    private void Begin(Acb4000SearchWait wait, int tStates, int setupTStates = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(setupTStates);
        remaining = ((long)tStates + setupTStates) * ownerClock;
        expired = Acb4000SearchWait.None;
        Wait = wait;
    }

    /// <summary>Réarme après un champ absent, sans arrondir et perdre la fraction déjà dépassée.</summary>
    public void ContinueFieldWait()
    {
        if (Wait != Acb4000SearchWait.None || expired != Acb4000SearchWait.Field)
            throw new InvalidOperationException();
        remaining += (long)C.FollowingFieldWaitTStates * ownerClock;
        Wait = Acb4000SearchWait.Field;
        expired = Acb4000SearchWait.None;
    }

    public Acb4000SearchWait Tick()
    {
        if (Wait == Acb4000SearchWait.None) return Acb4000SearchWait.None;
        remaining -= sequencerClock;
        return TakeExpired();
    }

    /// <summary>Consomme une seule expiration ; après reprise, permet de traiter un tick couvrant plusieurs fenêtres.</summary>
    public Acb4000SearchWait TakeExpired()
    {
        if (Wait == Acb4000SearchWait.None || remaining > 0) return Acb4000SearchWait.None;
        expired = Wait;
        Wait = Acb4000SearchWait.None;
        return expired;
    }

    public void Reset()
    {
        remaining = 0;
        Wait = expired = Acb4000SearchWait.None;
    }
}
