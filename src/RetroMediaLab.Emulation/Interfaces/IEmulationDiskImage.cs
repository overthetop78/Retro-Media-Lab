namespace RetroMediaLab.Emulation.Interfaces;

/// <summary>Accès disque commun aux émulateurs internes, relayé vers les lecteurs et écrivains de MediaEngine.</summary>
/// <remarks>Le cœur possède cette image, sérialise ses appels, la sauvegarde et la libère dans finally.
/// Une géométrie nulle est représentée par zéro ; elle n'est pas déduite d'une simple capacité.</remarks>
public interface IEmulationDiskImage : IDisposable, RetroMediaLab.Chips.Controllers.Common.Interfaces.IDiskImage
{
    /// <summary>Mécanique du lecteur emprunté ; fermer cette image ne détruit pas un lecteur fourni.</summary>
    new IEmulationDriveMechanics Mechanics { get; }
    new string FormatId { get; }
    new int SectorSize { get; }
    new long SectorCount { get; }
    new long Cylinders { get; }
    new int Heads { get; }
    new int SectorsPerTrack { get; }
    new bool IsReadOnly { get; }
    new bool HasChanges { get; }
    new IReadOnlyDictionary<string, string> Metadata { get; }
    /// <summary>Prépare les métadonnées prises en charge par le format ; FlushAsync les publie avec les secteurs.</summary>
    /// <remarks>Une clé non prise en charge ou une géométrie modifiant la capacité est refusée.</remarks>
    new void UpdateMetadata(IReadOnlyDictionary<string, string> metadata);
    /// <summary>Paramètres matériels binaires conservés par le format, ou null lorsqu'ils sont absents.</summary>
    byte[]? ReadDeviceParameters() => Functions.EmulationDiskImageFunctions.ReadDeviceParameters(this);
    /// <summary>Prépare les paramètres matériels ; le format valide leur prise en charge et FlushAsync les publie.</summary>
    void WriteDeviceParameters(ReadOnlySpan<byte> parameters)
        => Functions.EmulationDiskImageFunctions.WriteDeviceParameters(this, parameters);
    /// <summary>Prépare une géométrie pour les écritures de formatage, sans remplir ni publier l'image.</summary>
    void PrepareBlockFormat(HardDisks.DiskImageGeometry geometry) => throw new NotSupportedException();
    /// <summary>Recherche l'identifiant physique d'un secteur sans supposer une numérotation universelle à zéro ou un.</summary>
    new long? FindSector(int cylinder, int head, int number);
    /// <summary>Indices des champs d'une piste physique dans leur ordre enregistré, doublons compris.</summary>
    new IReadOnlyList<long> GetTrackSectors(int cylinder, int head) => Enumerable.Range(0, checked(SectorsPerTrack + 1))
        .Select(number => FindSector(cylinder, head, number)).Where(index => index.HasValue)
        .Select(index => index!.Value).Distinct().ToArray();
    new ValueTask<EmulationDiskSector> ReadSectorAsync(long index, CancellationToken cancellationToken = default);
    /// <summary>Conserve un secteur modifié ; l'écriture physique utilise FlushAsync.</summary>
    new void WriteSector(long index, ReadOnlySpan<byte> data);
    /// <summary>Écrit le champ de données avec une marque explicite ; une marque non représentable est refusée.</summary>
    new void WriteSector(long index, ReadOnlySpan<byte> data, bool deletedDataMark)
    {
        if (deletedDataMark) throw new NotSupportedException();
        WriteSector(index, data);
    }
    /// <summary>Prépare les révolutions physiques depuis l'état actuel du média, sans accès mécanique.</summary>
    EmulationDiskTrack ReadTrack(int cylinder, int head, string encodingId, uint bitCellNanoseconds,
        uint indexTimeNanoseconds, IReadOnlyDictionary<string, int>? attributes = null)
        => throw new NotSupportedException();
    /// <summary>Prépare une piste selon l'encodage physique du contrat, sans exposer l'identifiant du moteur média.</summary>
    EmulationDiskTrack ReadTrack(int cylinder, int head, Enums.EmulationDiskTrackEncoding encoding,
        uint bitCellNanoseconds, uint indexTimeNanoseconds, IReadOnlyDictionary<string, int>? attributes = null)
        => ReadTrack(cylinder, head, Functions.EmulationDiskImageFunctions.EncodingId(encoding),
            bitCellNanoseconds, indexTimeNanoseconds, attributes);
    /// <summary>Transmet les impulsions réellement émises pendant une fenêtre d'écriture ; durées en nanosecondes.</summary>
    void WriteTrackWindow(int cylinder, int head, string encodingId, uint bitCellNanoseconds,
        uint indexTimeNanoseconds, uint startNanoseconds, uint durationNanoseconds,
        IReadOnlyList<uint> transitionOffsetsNanoseconds, IReadOnlyDictionary<string, int>? attributes = null,
        bool includeEndingInstant = false)
        => throw new NotSupportedException();
    /// <summary>Transmet une fenêtre d'écriture au relais SDK avec un encodage physique typé.</summary>
    void WriteTrackWindow(int cylinder, int head, Enums.EmulationDiskTrackEncoding encoding,
        uint bitCellNanoseconds, uint indexTimeNanoseconds, uint startNanoseconds, uint durationNanoseconds,
        IReadOnlyList<uint> transitionOffsetsNanoseconds, IReadOnlyDictionary<string, int>? attributes = null,
        bool includeEndingInstant = false)
        => WriteTrackWindow(cylinder, head, Functions.EmulationDiskImageFunctions.EncodingId(encoding),
            bitCellNanoseconds, indexTimeNanoseconds, startNanoseconds, durationNanoseconds,
            transitionOffsetsNanoseconds, attributes, includeEndingInstant);
    new Task FlushAsync(CancellationToken cancellationToken = default);
    /// <summary>Remplace les champs d'une piste physique dans l'ordre fourni, avec leurs identifiants enregistrés.</summary>
    void FormatTrack(int cylinder, int head, IReadOnlyList<EmulationDiskSector> sectors,
        int? dataRate = null, bool? mfm = null) => throw new NotSupportedException();
    RetroMediaLab.Chips.Controllers.Common.Interfaces.IDriveSignals RetroMediaLab.Chips.Controllers.Common.Interfaces.IDiskImage.Mechanics => Mechanics;

    ValueTask<RetroMediaLab.Chips.Controllers.Common.Contracts.DiskSector> RetroMediaLab.Chips.Controllers.Common.Interfaces.IDiskImage.ReadSectorAsync(long index, CancellationToken cancellationToken)
    {
        var read = ReadSectorAsync(index, cancellationToken);
        return read.IsCompletedSuccessfully
            ? new ValueTask<RetroMediaLab.Chips.Controllers.Common.Contracts.DiskSector>(read.Result)
            : AwaitSectorAsync(read);
    }

    private static async ValueTask<RetroMediaLab.Chips.Controllers.Common.Contracts.DiskSector> AwaitSectorAsync(ValueTask<EmulationDiskSector> read) => await read.ConfigureAwait(false);

    void RetroMediaLab.Chips.Controllers.Common.Interfaces.IDiskImage.FormatTrack(int cylinder, int head,
        IReadOnlyList<RetroMediaLab.Chips.Controllers.Common.Contracts.DiskSector> sectors, int? dataRate, bool? mfm)
        => FormatTrack(cylinder, head, sectors.Select(sector => sector as EmulationDiskSector ?? new EmulationDiskSector(
            sector.Index, sector.Data, sector.Cylinder, sector.Head, sector.Number, sector.IntegrityValid, sector.DiagnosticCode)
        {
            RecordedErrorCheckCode = sector.RecordedErrorCheckCode,
            RecordedDataPrefix = sector.RecordedDataPrefix,
            HeaderIntegrityValid = sector.HeaderIntegrityValid,
            DataIntegrityValid = sector.DataIntegrityValid,
            DeletedDataMark = sector.DeletedDataMark,
            RecordedSizeCode = sector.RecordedSizeCode
        }).ToArray(), dataRate, mfm);
    IReadOnlyList<long> RetroMediaLab.Chips.Controllers.Common.Interfaces.IDiskImage.GetTrackSectors(int cylinder, int head) => GetTrackSectors(cylinder, head);
    void RetroMediaLab.Chips.Controllers.Common.Interfaces.IDiskImage.WriteSector(long index, ReadOnlySpan<byte> data, bool deletedDataMark) => WriteSector(index, data, deletedDataMark);
}
