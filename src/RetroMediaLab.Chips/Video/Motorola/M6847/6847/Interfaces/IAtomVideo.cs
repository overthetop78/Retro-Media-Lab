namespace RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Interfaces;
/// <summary>Contrat matériel du composant AtomVideo ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface IAtomVideo
{
    byte[] CompletedFrame { get; }

    long Frame { get; }

    bool FrameSync { get; }
    Action<bool>? FrameSyncChanged { get; set; }

    void SetMode(byte value);
    void SetColorSet(bool value);
    void Reset();
    bool Tick();
    /// <summary>Périodes CLK fournies sans entrée extérieure ni observation intermédiaire.
    /// Le propriétaire borne l'appel avant leurs changements ; les accès mémoire et FS
    /// restent traités à chaque frontière physique pendant cet intervalle.</summary>
    void AdvanceClocks(int clocks);
}
