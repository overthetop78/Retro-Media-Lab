using System.IO;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Functions;
using RetroMediaLab.MediaEngine.Images.Conversion.Flux;
using RetroMediaLab.MediaEngine.Images.Models.Flux;
using RetroMediaLab.MediaEngine.Interfaces;

namespace RetroMediaLab.MediaEngine.Services;

/// <summary>Expose les impulsions des conteneurs de flux sans inventer de secteurs décodés.</summary>
internal sealed class MediaRuntimeFluxDisk : IMediaRuntimeDisk
{
    private const int NanosecondResolution = 1;
    private readonly MediaEngineComposition engine;
    private readonly bool ownsMechanics;
    private readonly int revolutionCount;
    private readonly Dictionary<(int Cylinder, int Head), ProtectedTrack> tracks;
    private readonly Dictionary<(int Cylinder, int Head), MediaFluxTrackData> cache = [];
    private MediaImageDocument document;
    private bool disposed;

    internal MediaRuntimeFluxDisk(MediaEngineComposition engine, MediaImageDocument document, bool writable, bool ownsMechanics)
    {
        this.engine = engine;
        this.document = document;
        this.ownsMechanics = ownsMechanics;
        var representation = document.Representation as FluxMediaImageRepresentation
            ?? throw new ArgumentException(nameof(document));
        tracks = representation.Tracks.ToDictionary(track => (track.Cylinder, track.Head));
        revolutionCount = Math.Max(1, representation.Tracks.Max(track => track.Revolutions.Count));
        Cylinders = tracks.Keys.Max(address => address.Cylinder) + 1L;
        Heads = tracks.Keys.Max(address => address.Head) + 1;
        IsReadOnly = !writable || representation.Image.WriteProtected
            || new FileInfo(document.Source.PrimaryPath).IsReadOnly
            // Une écriture ne doit pas figer silencieusement les régions faibles du conteneur.
            || representation.Tracks.Any(track => track.Features.Any(feature => feature.Kind == TrackFeatureKind.WeakRegion))
            || !engine.Writing.Writers.Any(writer => writer.CanWrite(document, FormatId, Path.GetExtension(document.Source.PrimaryPath)));
    }

    public IMediaDriveMechanics Mechanics => document.Source.Drive!;
    public string FormatId => document.FormatId;
    public int SectorSize => 0;
    public long SectorCount => 0;
    public long Cylinders { get; }
    public int Heads { get; }
    public int SectorsPerTrack => 0;
    public bool IsReadOnly { get; }
    public bool HasChanges { get; private set; }
    public IReadOnlyDictionary<string, string> Metadata => document.Metadata;

    public void UpdateMetadata(IReadOnlyDictionary<string, string> metadata)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(metadata);
        if (metadata.Count != 0) throw new NotSupportedException();
    }

    public long? FindSector(int cylinder, int head, int number)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return null;
    }

    public ValueTask<MediaDiskSector> ReadSectorAsync(long index, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        cancellationToken.ThrowIfCancellationRequested();
        throw new NotSupportedException();
    }

    public void WriteSector(long index, ReadOnlySpan<byte> data)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        throw new NotSupportedException();
    }

    public MediaFluxTrackData ReadTrack(int cylinder, int head, string encodingId, uint bitCellNanoseconds,
        uint indexTimeNanoseconds, IReadOnlyDictionary<string, int>? attributes = null)
    {
        ValidateAddress(cylinder, head);
        ArgumentOutOfRangeException.ThrowIfZero(bitCellNanoseconds);
        ArgumentOutOfRangeException.ThrowIfZero(indexTimeNanoseconds);
        var address = (cylinder, head);
        if (cache.TryGetValue(address, out var result)) return result;
        // Une piste absente n'acquiert ni secteurs ni impulsions synthétiques.
        if (!tracks.TryGetValue(address, out var track))
            return new(Enumerable.Repeat(new MediaFluxRevolutionData(indexTimeNanoseconds, []), revolutionCount).ToArray());
        result = ToRuntimeTrack(track);
        cache.Add(address, result);
        return result;
    }

    public void WriteTrackWindow(int cylinder, int head, string encodingId, uint bitCellNanoseconds,
        uint indexTimeNanoseconds, uint startNanoseconds, uint durationNanoseconds,
        IReadOnlyList<uint> transitionOffsetsNanoseconds, IReadOnlyDictionary<string, int>? attributes = null,
        bool includeEndingInstant = false)
    {
        var source = ReadTrack(cylinder, head, encodingId, bitCellNanoseconds, indexTimeNanoseconds, attributes);
        if (IsReadOnly) throw new UnauthorizedAccessException();
        var changed = new MediaFluxTrackData(source.Revolutions.Select(revolution =>
            MediaFluxTrackDataFunctions.ReplaceWindow(revolution, startNanoseconds, durationNanoseconds,
                transitionOffsetsNanoseconds, includeEndingInstant)).ToArray());
        if (durationNanoseconds == 0) return;
        var replacement = FromRuntimeTrack(cylinder, head, changed);
        tracks[(cylinder, head)] = replacement;
        cache[(cylinder, head)] = changed;
        HasChanges = true;
    }

    internal static MediaFluxTrackData ToRuntimeTrack(ProtectedTrack track) => ProtectedTrackFluxAdapter.Read(track);

    internal static ProtectedTrack FromRuntimeTrack(int cylinder, int head, MediaFluxTrackData data)
    {
        var revolutions = data.Revolutions.Select(revolution => new TrackFluxRevolution(NanosecondResolution,
            new FluxRevolution(revolution.IndexTimeNanoseconds, revolution.FluxIntervalsNanoseconds))).ToArray();
        return new(cylinder, head, null, [], [], [], revolutions);
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        cancellationToken.ThrowIfCancellationRequested();
        if (!HasChanges) return;
        var representation = new FluxMediaImageRepresentation(new ProtectedTrackImage(tracks.Values.ToArray(), false));
        var pending = MediaImageDocument.CreateUnexplored(document.Source with { Drive = null }, FormatId,
            document.MediaKind, representation, document.Diagnostics, document.Metadata);
        await engine.Writing.WritingService.WriteAsync(pending, document.Source.PrimaryPath, FormatId,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        document = MediaImageDocument.CreateUnexplored(document.Source, FormatId, document.MediaKind,
            representation, document.Diagnostics, document.Metadata);
        HasChanges = false;
    }

    public void Dispose()
    {
        if (disposed) return;
        try { FlushAsync().GetAwaiter().GetResult(); }
        finally
        {
            try { if (ownsMechanics) Mechanics.Reset(); }
            finally { cache.Clear(); tracks.Clear(); disposed = true; }
        }
    }

    private void ValidateAddress(int cylinder, int head)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (cylinder < 0 || cylinder >= Cylinders) throw new ArgumentOutOfRangeException(nameof(cylinder));
        if (head < 0 || head >= Heads) throw new ArgumentOutOfRangeException(nameof(head));
    }
}
