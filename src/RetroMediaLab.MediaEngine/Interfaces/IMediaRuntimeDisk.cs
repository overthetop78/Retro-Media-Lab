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
    Task FlushAsync(CancellationToken cancellationToken = default);
    /// <summary>Remplace une piste physique et conserve les identifiants dans l'ordre fourni.</summary>
    void FormatTrack(int cylinder, int head, IReadOnlyList<MediaDiskSector> sectors,
        int? dataRate = null, bool? mfm = null) => throw new NotSupportedException();
}
