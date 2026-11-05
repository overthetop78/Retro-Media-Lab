using System.IO;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Images.Formats.HardDisk.MfmEmu;
using RetroMediaLab.MediaEngine.Interfaces;
using A = RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Acorn.AcornWinchesterFormat;
using F = RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Adaptec.AdaptecMfmFormat;
using RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Adaptec;

namespace RetroMediaLab.MediaEngine.Services;

/// <summary>Possède l'état physique EMU et publie ses pistes sans projection sectorielle inventée.</summary>
internal sealed partial class MediaRuntimeMfmDisk : IMediaRuntimeDisk
{
    private readonly MediaEngineComposition engine;
    private readonly bool ownsMechanics;
    private MediaImageDocument document;
    private MfmEmuImageRepresentation representation;
    private MediaFluxTrackData? cached;
    private (int Cylinder, int Head, uint Period) cachedAddress;
    private bool disposed;

    internal MediaRuntimeMfmDisk(MediaEngineComposition engine, MediaImageDocument document, bool writable, bool ownsMechanics)
    {
        this.engine = engine;
        this.document = document;
        this.ownsMechanics = ownsMechanics;
        representation = document.Representation as MfmEmuImageRepresentation ?? throw new ArgumentException(nameof(document));
        IsReadOnly = !writable || new FileInfo(document.Source.PrimaryPath).IsReadOnly
            || !engine.Writing.Writers.Any(writer => writer.CanWrite(document, document.FormatId, Path.GetExtension(document.Source.PrimaryPath)));
    }

    public IMediaDriveMechanics Mechanics => document.Source.Drive!;
    public string FormatId => document.FormatId;
    public int SectorSize { get { ObjectDisposedException.ThrowIf(disposed, this); return representation.Sectors.SectorSize; } }
    public long SectorCount { get { ObjectDisposedException.ThrowIf(disposed, this); return representation.Sectors.SectorCount; } }
    public long Cylinders => representation.Cylinders;
    public int Heads => representation.Heads;
    public int SectorsPerTrack { get { ObjectDisposedException.ThrowIf(disposed, this); return representation.Sectors.SectorsPerTrack; } }
    public bool IsReadOnly { get; }
    public bool HasChanges { get; private set; }
    public IReadOnlyDictionary<string, string> Metadata => document.Metadata;

    public void UpdateMetadata(IReadOnlyDictionary<string, string> metadata)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(metadata);
        if (metadata.Count == 0) return;
        if (IsReadOnly) throw new UnauthorizedAccessException();
        if (metadata.Count != 1 || !metadata.TryGetValue(A.DescriptorMetadataKey, out var encoded))
            throw new NotSupportedException();
        // EMU stores these parameters in the physical ID fields, not in an invented DSC sidecar.
        var descriptor = Convert.FromBase64String(encoded);
        Span<byte> flags = stackalloc byte[F.ParameterFlagCount];
        if (!A.TryWritePhysicalParameters(descriptor, representation.Sectors.Capacity, flags))
            throw new InvalidDataException();
        var track = ReadTrack(0, 0, F.EncodingId, F.CellNanoseconds, F.NominalIndexNanoseconds);
        var fields = AdaptecMfmTrackCodec.Decode(track.Revolutions[0], 0, 0, SectorSize);
        var anchor = -1;
        for (var index = 0; index < fields.Count; index++)
            if (fields[index].Sector.Number == 0 && fields[index].Sector.HeaderIntegrityValid == true)
            { anchor = index; break; }
        if (anchor < 0 || fields.Count < flags.Length) throw new InvalidDataException();
        for (var index = 0; index < flags.Length; index++)
        {
            var field = fields[(anchor + index) % fields.Count];
            if (field.Sector.HeaderIntegrityValid != true
                || (field.Id.Span[F.FlagsOffset] & F.DefectCountMask) != flags[index])
                throw new InvalidDataException();
        }
    }

    public void PrepareBlockFormat(HardDiskGeometry geometry)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(geometry);
        if (IsReadOnly) throw new UnauthorizedAccessException();
        if (!A.SupportsGeometry(geometry) || geometry.Cylinders > Cylinders || geometry.Heads > Heads)
            throw new NotSupportedException();
        // Only the forthcoming physical write windows change cells. Preparing FORMAT must
        // neither erase untouched tracks nor resize the physical container from logical geometry.
    }

    public MediaFluxTrackData ReadTrack(int cylinder, int head, string encodingId, uint bitCellNanoseconds,
        uint indexTimeNanoseconds, IReadOnlyDictionary<string, int>? attributes = null)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentOutOfRangeException.ThrowIfZero(bitCellNanoseconds);
        var address = (cylinder, head, indexTimeNanoseconds);
        if (cached is not null && cachedAddress == address) return cached;
        var track = representation.ReadTrackAsync(cylinder, head, indexTimeNanoseconds).GetAwaiter().GetResult();
        cached = track;
        cachedAddress = address;
        return track;
    }

    public void WriteTrackWindow(int cylinder, int head, string encodingId, uint bitCellNanoseconds,
        uint indexTimeNanoseconds, uint startNanoseconds, uint durationNanoseconds,
        IReadOnlyList<uint> transitionOffsetsNanoseconds, IReadOnlyDictionary<string, int>? attributes = null,
        bool includeEndingInstant = false)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (IsReadOnly) throw new UnauthorizedAccessException();
        ArgumentOutOfRangeException.ThrowIfZero(bitCellNanoseconds);
        var changed = representation.WithTrackWindowAsync(cylinder, head, indexTimeNanoseconds, startNanoseconds,
            durationNanoseconds, transitionOffsetsNanoseconds, includeEndingInstant).GetAwaiter().GetResult();
        if (durationNanoseconds == 0) return;
        representation = changed;
        cached = null;
        HasChanges = true;
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        cancellationToken.ThrowIfCancellationRequested();
        if (!HasChanges) return;
        var pending = MediaImageDocument.CreateUnexplored(document.Source with { Drive = null }, FormatId,
            document.MediaKind, representation, document.Diagnostics, document.Metadata);
        await engine.Writing.WritingService.WriteAsync(pending, document.Source.PrimaryPath, FormatId,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        var reopened = await engine.ReadingService.ReadAsync(document.Source with { KnownLength = null, RequestedFormatId = FormatId }, cancellationToken).ConfigureAwait(false);
        representation = (MfmEmuImageRepresentation)reopened.Representation;
        document = reopened;
        HasChanges = false;
    }

    public void Dispose()
    {
        if (disposed) return;
        try { FlushAsync().GetAwaiter().GetResult(); }
        finally
        {
            try { if (ownsMechanics) Mechanics.Reset(); }
            finally { cached = null; disposed = true; }
        }
    }
}
