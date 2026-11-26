using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Images.Formats.Optical.Gdi;
using RetroMediaLab.MediaEngine.Images.Models.Optical;
using RetroMediaLab.MediaEngine.Interfaces.Conversion;

namespace RetroMediaLab.MediaEngine.Images.Conversion.Optical;

/// <summary>Exports the indexed tracks of a GD-ROM CUE to GDI with explicit layout losses.</summary>
internal sealed class GdRomCueToGdiRepresentationConverter : IMediaRepresentationConverter
{
    public string Id => "gd-rom-cue-to-gdi";
    public IReadOnlySet<MediaRepresentationKind> SourceRepresentationKinds { get; } = new HashSet<MediaRepresentationKind> { MediaRepresentationKind.OpticalTracks };
    public IReadOnlySet<MediaRepresentationKind> TargetRepresentationKinds => SourceRepresentationKinds;

    public bool CanConvert(MediaImageDocument source, string targetFormatId, MediaRepresentationKind targetRepresentationKind) =>
        Prepare(source, targetFormatId, targetRepresentationKind) is not null;

    public Task<MediaRepresentationConversionResult> ConvertAsync(MediaImageDocument source, string targetFormatId,
        MediaRepresentationKind targetRepresentationKind, IReadOnlyDictionary<string, string> options,
        Action<MediaExplorationProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var document = Prepare(source, targetFormatId, targetRepresentationKind)
            ?? throw new NotSupportedException("The optical source cannot be represented as a GD-ROM GDI descriptor.");
        return Task.FromResult(new MediaRepresentationConversionResult(document, [],
            ["The GDI export carries each track from INDEX 01 at its original address. Stored INDEX 00 sectors, additional index markers and CUE track metadata are omitted."]));
    }

    private static MediaImageDocument? Prepare(MediaImageDocument source, string targetFormatId,
        MediaRepresentationKind targetRepresentationKind)
    {
        if (source.FormatId != OpticalImageFormatIds.BinCue || targetFormatId != OpticalImageFormatIds.Gdi ||
            targetRepresentationKind != MediaRepresentationKind.OpticalTracks ||
            source.Metadata.GetValueOrDefault("mediaProfile") != GdiConstants.MediaProfile ||
            source.Representation is not OpticalMediaImageRepresentation { Tracks: { } tracks } optical)
            return null;

        var indexedTracks = tracks.Select(track => new OpticalTrackDescriptor(track.SessionNumber, track.TrackNumber,
            track.Mode, track.FirstSector, track.SectorCount, track.StoredSectorSize,
            track.UserDataOffset, track.UserDataLength, track.DataSource, track.SourceOffset, [new OpticalTrackIndex(1, 0)])).ToArray();
        var representation = new OpticalMediaImageRepresentation(optical.LogicalLength, indexedTracks, optical.LayerCount, optical.FaceCount);
        var document = new MediaImageDocument(source.Source, OpticalImageFormatIds.Gdi, MediaKind.Optical,
            representation, [], [], source.Metadata);
        return new GdiWriter().CanWrite(document, targetFormatId, DiskImageFileExtensions.Gdi) ? document : null;
    }
}
