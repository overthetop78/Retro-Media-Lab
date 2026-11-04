namespace RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Interfaces;
/// <summary>Contrat matériel du composant AtomVideo ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface IAtomVideo
{
    byte[] CompletedFrame { get; }

    long Frame { get; }

    bool FrameSync { get; }

    void SetMode(byte value);
    void SetColorSet(bool value);
    void Reset();
    bool Tick();
}
