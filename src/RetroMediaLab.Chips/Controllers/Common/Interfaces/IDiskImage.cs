using RetroMediaLab.Chips.Controllers.Common.Contracts;

namespace RetroMediaLab.Chips.Controllers.Common.Interfaces;

/// <summary>Accès disque commun aux émulateurs internes, relayé vers les lecteurs et écrivains de MediaEngine.</summary>
/// <remarks>Le cœur possède cette image, sérialise ses appels, la sauvegarde et la libère dans finally.
/// Une géométrie nulle est représentée par zéro ; elle n'est pas déduite d'une simple capacité.</remarks>
public interface IDiskImage
{
    /// <summary>Mécanique du lecteur emprunté ; fermer cette image ne détruit pas un lecteur fourni.</summary>
    IDriveSignals Mechanics { get; }
    string FormatId { get; }
    int SectorSize { get; }
    long SectorCount { get; }
    long Cylinders { get; }
    int Heads { get; }
    int SectorsPerTrack { get; }
    bool IsReadOnly { get; }
    bool HasChanges { get; }
    IReadOnlyDictionary<string, string> Metadata { get; }
    /// <summary>Prépare les métadonnées prises en charge par le format ; FlushAsync les publie avec les secteurs.</summary>
    /// <remarks>Une clé non prise en charge ou une géométrie modifiant la capacité est refusée.</remarks>
    void UpdateMetadata(IReadOnlyDictionary<string, string> metadata);
    /// <summary>Recherche l'identifiant physique d'un secteur sans supposer une numérotation universelle à zéro ou un.</summary>
    long? FindSector(int cylinder, int head, int number);
    /// <summary>Indices des champs d'une piste physique dans leur ordre enregistré, doublons compris.</summary>
    IReadOnlyList<long> GetTrackSectors(int cylinder, int head) => Enumerable.Range(0, checked(SectorsPerTrack + 1))
        .Select(number => FindSector(cylinder, head, number)).Where(index => index.HasValue)
        .Select(index => index!.Value).Distinct().ToArray();
    ValueTask<DiskSector> ReadSectorAsync(long index, CancellationToken cancellationToken = default);
    /// <summary>Conserve un secteur modifié ; l'écriture physique utilise FlushAsync.</summary>
    void WriteSector(long index, ReadOnlySpan<byte> data);
    /// <summary>Écrit le champ de données avec une marque explicite ; une marque non représentable est refusée.</summary>
    void WriteSector(long index, ReadOnlySpan<byte> data, bool deletedDataMark)
    {
        if (deletedDataMark) throw new NotSupportedException();
        WriteSector(index, data);
    }
    Task FlushAsync(CancellationToken cancellationToken = default);
    /// <summary>Remplace les champs d'une piste physique dans l'ordre fourni, avec leurs identifiants enregistrés.</summary>
    void FormatTrack(int cylinder, int head, IReadOnlyList<DiskSector> sectors,
        int? dataRate = null, bool? mfm = null) => throw new NotSupportedException();
}
