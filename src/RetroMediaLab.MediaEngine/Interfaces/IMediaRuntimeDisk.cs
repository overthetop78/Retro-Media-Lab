using RetroMediaLab.MediaEngine.Contracts;

namespace RetroMediaLab.MediaEngine.Interfaces;

/// <summary>Image possédée par un runtime : accès sectoriel et publication par son écrivain de conteneur.</summary>
/// <remarks>Le propriétaire sérialise ses appels et libère l'image à l'arrêt. Les écritures restent en mémoire
/// jusqu'à FlushAsync ; une erreur conserve les changements pour une nouvelle tentative.</remarks>
public interface IMediaRuntimeDisk : IDisposable
{
    /// <summary>Mécanique du lecteur, empruntée lorsqu’elle est fournie par l’appelant.</summary>
    IMediaDriveMechanics Mechanics { get; }
    string FormatId { get; }
    int SectorSize { get; }
    long SectorCount { get; }
    long Cylinders { get; }
    int Heads { get; }
    int SectorsPerTrack { get; }
    bool IsReadOnly { get; }
    bool HasChanges { get; }
    IReadOnlyDictionary<string, string> Metadata { get; }
    void UpdateMetadata(IReadOnlyDictionary<string, string> metadata);
    long? FindSector(int cylinder, int head, int number);
    /// <summary>Ordre physique des secteurs de la piste ; les identifiants répétés restent distincts.</summary>
    IReadOnlyList<long> GetTrackSectors(int cylinder, int head) => Enumerable.Range(0, checked(SectorsPerTrack + 1))
        .Select(number => FindSector(cylinder, head, number)).Where(index => index.HasValue)
        .Select(index => index!.Value).Distinct().ToArray();
    ValueTask<MediaDiskSector> ReadSectorAsync(long index, CancellationToken cancellationToken = default);
    void WriteSector(long index, ReadOnlySpan<byte> data);
    /// <summary>Écrit le secteur et sa marque physique, sans convertir silencieusement une marque supprimée.</summary>
    void WriteSector(long index, ReadOnlySpan<byte> data, bool deletedDataMark)
    {
        if (deletedDataMark) throw new NotSupportedException();
        WriteSector(index, data);
    }
    /// <summary>Prépare les transitions d'une piste depuis son contenu courant, sans accès mécanique.</summary>
    /// <remarks>Les durées et les intervalles sont exprimés en nanosecondes. Les attributs appartiennent
    /// à l'encodeur demandé ; aucune puce ni machine n'est sélectionnée par ce service.</remarks>
    MediaFluxTrackData ReadTrack(int cylinder, int head, string encodingId, uint bitCellNanoseconds,
        uint indexTimeNanoseconds, IReadOnlyDictionary<string, int>? attributes = null)
        => throw new NotSupportedException();
    /// <summary>Applique les impulsions reçues pendant une fenêtre d'écriture sur la piste courante.</summary>
    /// <remarks>Les durées sont en nanosecondes. Seules les impulsions déjà émises sont fournies ;
    /// un conteneur ne pouvant préserver les champs résultants sera refusé lors de FlushAsync.</remarks>
    void WriteTrackWindow(int cylinder, int head, string encodingId, uint bitCellNanoseconds,
        uint indexTimeNanoseconds, uint startNanoseconds, uint durationNanoseconds,
        IReadOnlyList<uint> transitionOffsetsNanoseconds, IReadOnlyDictionary<string, int>? attributes = null,
        bool includeEndingInstant = false)
        => throw new NotSupportedException();
    Task FlushAsync(CancellationToken cancellationToken = default);
    /// <summary>Remplace une piste physique et conserve les identifiants dans l'ordre fourni.</summary>
    void FormatTrack(int cylinder, int head, IReadOnlyList<MediaDiskSector> sectors,
        int? dataRate = null, bool? mfm = null) => throw new NotSupportedException();
}
