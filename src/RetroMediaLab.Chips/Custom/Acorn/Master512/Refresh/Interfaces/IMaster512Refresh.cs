namespace RetroMediaLab.Chips.Custom.Acorn.Master512.Refresh.Interfaces;
/// <summary>Contrat matériel du composant Master512Refresh ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface IMaster512Refresh
{
    bool HoldRequested { get; }

    bool RefreshActive { get; }

    void Reset();
    void TimerOutput(bool level);
    void HoldAcknowledged(bool asserted);
    void UpperMemorySelected(bool asserted);
    void AdvanceClocks(long clocks);
}
