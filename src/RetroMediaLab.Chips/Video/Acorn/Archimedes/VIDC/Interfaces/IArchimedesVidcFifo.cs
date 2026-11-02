namespace RetroMediaLab.Chips.Video.Acorn.Archimedes.VIDC.Interfaces;
/// <summary>Contrat matériel du composant ArchimedesVidcFifo ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface IArchimedesVidcFifo
{
    int Count { get; }

    int Capacity { get; }

    void Clear();
    void Supply(ReadOnlySpan<uint> words);
    bool TryTake(out uint word);
}
