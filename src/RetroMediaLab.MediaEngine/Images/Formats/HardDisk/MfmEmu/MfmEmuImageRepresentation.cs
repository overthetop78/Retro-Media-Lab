using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Interfaces;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Adaptec;
using IMediaBlockRepresentation = RetroMediaLab.MediaFileSystems.Interfaces.IMediaBlockRepresentation;
using RetroMediaLab.MediaEngine.Images.Models.Blocks;

namespace RetroMediaLab.MediaEngine.Images.Formats.HardDisk.MfmEmu;

/// <summary>Pistes physiques EMU accessibles à la demande, sans les convertir en secteurs logiques.</summary>
public sealed class MfmEmuImageRepresentation : IMediaImageRepresentation, IMediaBlockRepresentation
{
    private AdaptecMfmSectorReader? sectors;
    internal AdaptecMfmSectorReader Sectors => sectors ??= new(
        (cylinder, head) => ReadTrackAsync(cylinder, head, AdaptecMfmFormat.NominalIndexNanoseconds), Cylinders, Heads);
    internal MfmEmuImageRepresentation(MfmEmuContainer container) => Container = container;
    internal MfmEmuContainer Container { get; }

    /// <summary>Crée des pistes sans impulsions ; aucune géométrie logique ou commande de contrôleur n'est inventée.</summary>
    public static MfmEmuImageRepresentation CreateUnformatted(int cylinders, int heads, int trackByteLength,
        uint sampleRate, uint startNanoseconds = 0) => new(MfmEmuContainer.CreateBlank(
            MfmEmuHeader.Create(cylinders, heads, trackByteLength, sampleRate, startNanoseconds)));
    public MediaRepresentationKind RepresentationKind => MediaRepresentationKind.Flux;
    public long? LogicalLength => Sectors.Capacity;
    long IMediaBlockRepresentation.Capacity => Sectors.Capacity;
    int IMediaBlockRepresentation.LogicalBlockSize => Sectors.SectorSize;
    long IMediaBlockRepresentation.LogicalBlockCount => Sectors.SectorCount;
    ValueTask IMediaBlockRepresentation.ReadExactlyAsync(long address, Memory<byte> destination,
        CancellationToken cancellationToken) => Sectors.ReadExactlyAsync(address, destination, cancellationToken);

    /// <summary>Crée une vue logique pour les graphiques HDD existants ; le document physique reste inchangé.</summary>
    public MediaImageDocument? CreateLogicalView(MediaImageDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (!ReferenceEquals(document.Representation, this)) throw new ArgumentException(nameof(document));
        var reader = Sectors;
        var capacity = reader.Capacity;
        if (capacity == 0) return null;
        var recorded = reader.Geometry;
        var geometry = new HardDiskGeometry(recorded.Cylinders, recorded.Heads, recorded.SectorsPerTrack, recorded.SectorSize);
        var blocks = new BlockMediaImageRepresentation(capacity, recorded.SectorSize,
            [new(0, capacity, MediaDataRangeKind.Stored, new AdaptecMfmBlockData(reader))],
            geometry.Capacity == capacity ? geometry : null);
        return new(document.Source, document.FormatId, document.MediaKind, blocks,
            document.FileSystemVolumes, document.Diagnostics, document.Metadata);
    }
    public bool SupportsRandomAccess => true;
    public bool SupportsSequentialAccess => true;
    public int Cylinders => Container.Header.Cylinders;
    public int Heads => Container.Header.Heads;
    public int TrackByteLength => Container.Header.TrackDataLength;
    public uint SampleRate => Container.Header.SampleRate;
    public uint StartNanoseconds => Container.Header.StartNanoseconds;

    /// <summary>Possède une copie de la piste remplacée ; l'état précédent reste inchangé.</summary>
    public MfmEmuImageRepresentation WithTrackBytes(int cylinder, int head, ReadOnlyMemory<byte> bytes) =>
        new(Container.WithTrackBytes(cylinder, head, bytes));

    /// <summary>La période vient du lecteur physique ; le remplissage du conteneur ne définit pas sa rotation.</summary>
    public async ValueTask<MediaFluxTrackData> ReadTrackAsync(int cylinder, int head, uint indexTimeNanoseconds,
        CancellationToken cancellationToken = default)
    {
        var bytes = new byte[TrackByteLength];
        await ReadTrackBytesAsync(cylinder, head, 0, bytes, cancellationToken).ConfigureAwait(false);
        return MfmEmuTrackSignal.Read(bytes, SampleRate, StartNanoseconds, indexTimeNanoseconds);
    }

    /// <summary>Conserve les cellules non traversées et les octets de remplissage de la piste.</summary>
    public async ValueTask<MfmEmuImageRepresentation> WithTrackWindowAsync(int cylinder, int head,
        uint indexTimeNanoseconds, uint startNanoseconds, uint durationNanoseconds,
        IReadOnlyList<uint> transitionOffsetsNanoseconds, bool includeEndingInstant = false,
        CancellationToken cancellationToken = default)
    {
        var bytes = new byte[TrackByteLength];
        await ReadTrackBytesAsync(cylinder, head, 0, bytes, cancellationToken).ConfigureAwait(false);
        var changed = MfmEmuTrackSignal.WriteWindow(bytes, SampleRate, StartNanoseconds, indexTimeNanoseconds,
            startNanoseconds, durationNanoseconds, transitionOffsetsNanoseconds, includeEndingInstant);
        return WithTrackBytes(cylinder, head, changed);
    }

    /// <summary>Mots little-endian de quatre octets, bit 31 en premier dans chaque mot.</summary>
    public ValueTask ReadTrackBytesAsync(int cylinder, int head, int byteOffset, Memory<byte> destination,
        CancellationToken cancellationToken = default) => Container.ReadTrackBytesAsync(cylinder, head, byteOffset, destination, cancellationToken);
}
