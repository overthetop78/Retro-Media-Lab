using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.BbcDfs;
using RetroMediaLab.MediaEngine.Images.Models.Sectors;
using RetroMediaLab.MediaEngine.Images.Reading;
using RetroMediaLab.MediaEngine.Interfaces.Conversion;

namespace RetroMediaLab.MediaEngine.Images.Conversion.Acorn;

/// <summary>Retrouve les catalogues DFS dans les octets d'une image brute sans modifier leur contenu.</summary>
internal sealed class RawToBbcDfsRepresentationConverter : IMediaRepresentationConverter
{
    public string Id => "raw-to-bbc-dfs";
    public IReadOnlySet<MediaRepresentationKind> SourceRepresentationKinds { get; } = new HashSet<MediaRepresentationKind> { MediaRepresentationKind.Sectors };
    public IReadOnlySet<MediaRepresentationKind> TargetRepresentationKinds => SourceRepresentationKinds;

    public bool CanConvert(MediaImageDocument source, string targetFormatId, MediaRepresentationKind targetRepresentationKind)
        => TryCreate(source, targetFormatId, targetRepresentationKind, CancellationToken.None, out _);

    private static bool TryCreate(MediaImageDocument source, string targetFormatId, MediaRepresentationKind kind,
        CancellationToken token, out SectorImage result)
    {
        result = null!;
        if (kind != MediaRepresentationKind.Sectors || source.MediaKind != MediaKind.Floppy
            || !(source.FormatId.StartsWith(DiskImageFormatIds.IbmPrefix, StringComparison.OrdinalIgnoreCase)
                || source.FormatId.StartsWith(DiskImageFormatIds.AtariStPrefix, StringComparison.OrdinalIgnoreCase))
            || source.Representation is not SectorMediaImageRepresentation sectors
            || BbcDfsGeometry.FindByFormatId(targetFormatId) is not { } geometry) return false;
        var image = sectors.Image;
        var blocks = image.AvailableBlocks;
        if (image.BlockSize != 512 || image.Capacity != geometry.Capacity || image.BlockCount * 512L != geometry.Capacity
            || blocks.Count != image.BlockCount) return false;
        var bytes = new byte[geometry.Capacity];
        foreach (var block in blocks)
        {
            token.ThrowIfCancellationRequested();
            if (block.Data.Count != 512 || block.IntegrityValid == false || block.HeaderIntegrityValid == false
                || block.DataIntegrityValid == false || block.DeletedDataMark == true || block.Tag is { Count: > 0 }
                || block.FormatCode.HasValue || block.DiagnosticCode.HasValue || block.RecordedAddress is not null
                || block.RecordedSizeCode.HasValue || block.RecordedTrackOrder.HasValue
                || !block.RecordedErrorCheckCode.IsEmpty || !block.RecordedDataPrefix.IsEmpty) return false;
            for (var offset = 0; offset < 512; offset++) bytes[block.LogicalBlock * 512 + offset] = block.Data[offset];
        }
        if (!BbcDfsReader.HasDfsCatalogues(bytes, geometry)) return false;
        var candidate = BbcDfsLayout.CreateImage(bytes, geometry, token);
        result = new(candidate.FormatId, candidate.BlockSize, candidate.Cylinders, candidate.Heads, candidate.SectorsPerTrack,
            candidate.AvailableBlocks.Select(block => block with { IntegrityValid = null }));
        return true;
    }

    public Task<MediaRepresentationConversionResult> ConvertAsync(MediaImageDocument source, string targetFormatId,
        MediaRepresentationKind targetRepresentationKind, IReadOnlyDictionary<string, string> options,
        Action<MediaExplorationProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        if (!TryCreate(source, targetFormatId, targetRepresentationKind, cancellationToken, out var image))
            throw new NotSupportedException($"The raw source does not contain validated complete DFS catalogues for '{targetFormatId}'.");
        var document = MediaImageDocumentFactory.CreateFloppySector(source.Source, image);
        return Task.FromResult(new MediaRepresentationConversionResult(document, [],
            ["The raw file's 512-byte sector grouping is replaced by the validated BBC DFS profile without changing its bytes. Raw files store no recorded sector identifiers or CRCs."]));
    }
}
