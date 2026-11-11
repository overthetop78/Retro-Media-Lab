using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Adf;
using RetroMediaLab.MediaEngine.Images.Models.Sectors;
using RetroMediaLab.MediaEngine.Images.Reading;
using RetroMediaLab.MediaEngine.Images.Reading.Reconstruction;
using RetroMediaLab.MediaEngine.Interfaces.Conversion;
using RetroMediaLab.MediaFileSystems.FileSystems.Acorn.Adfs;
using RetroMediaLab.MediaFileSystems.FileSystems.Acorn.FileCore;

namespace RetroMediaLab.MediaEngine.Images.Conversion.Acorn;

/// <summary>Retrouve une structure ADFS dans des octets bruts interprétés en secteurs de 512 octets.</summary>
internal sealed class RawToAcornAdfRepresentationConverter : IMediaRepresentationConverter
{
    public string Id => "raw-to-acorn-adf";
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
            || source.Representation is not SectorMediaImageRepresentation sectors) return false;
        var geometry = AcornAdfGeometry.All.FirstOrDefault(item => item.FormatId.Equals(targetFormatId, StringComparison.OrdinalIgnoreCase)
            && item.FormatId.StartsWith(DiskImageFormatIds.AcornAdfsPrefix, StringComparison.OrdinalIgnoreCase)
            && item.FormatId != DiskImageFormatIds.AcornAdfs1600);
        var image = sectors.Image;
        if (geometry is null || image.BlockSize != 512 || image.Capacity != geometry.Capacity
            || (long)image.BlockCount * image.BlockSize != geometry.Capacity) return false;
        var blocks = image.AvailableBlocks;
        if (blocks.Count != image.BlockCount || !image.TryGetBlock(0, out var first) || first.Data.Count != 512) return false;
        var header = first.Data.ToArray();
        var oldMap = AcornAdfPartialImage.ValidOldMapChecksum(header.AsSpan(0, 256))
            && AcornAdfPartialImage.ValidOldMapChecksum(header.AsSpan(256, 256))
            && (header[252] | header[253] << 8 | header[254] << 16) * 256L == geometry.Capacity;
        var newMap = geometry.BlockSize == 1024
            && AcornFileCoreDiscRecord.TryParse(header.AsSpan(AcornFileCoreLayout.DiscRecordOffset), geometry.Capacity, out var record)
            && record is not null && record.DiscSize == geometry.Capacity && (1 << record.Log2SectorSize) == geometry.BlockSize;
        if (!oldMap && !newMap) return false;
        var bytes = new byte[geometry.Capacity];
        foreach (var block in blocks)
        {
            token.ThrowIfCancellationRequested();
            // A raw image has no recorded sector metadata to reinterpret or silently discard.
            if (block.Data.Count != 512 || block.IntegrityValid == false || block.HeaderIntegrityValid == false
                || block.DataIntegrityValid == false || block.DeletedDataMark == true || block.Tag is { Count: > 0 }
                || block.FormatCode.HasValue || block.DiagnosticCode.HasValue || block.RecordedAddress is not null
                || block.RecordedSizeCode.HasValue || block.RecordedTrackOrder.HasValue
                || !block.RecordedErrorCheckCode.IsEmpty || !block.RecordedDataPrefix.IsEmpty) return false;
            for (var offset = 0; offset < block.Data.Count; offset++) bytes[block.LogicalBlock * 512 + offset] = block.Data[offset];
        }
        var candidate = geometry.BlockSize == 256
            ? AcornAdfLayout.CreateBbc(bytes, geometry, token)
            : RegularSectorImageBuilder.Create(bytes, geometry, token);
        if (!new AcornAdfsFileSystemReader().CanRead(candidate)) return false;
        result = candidate;
        return true;
    }

    public Task<MediaRepresentationConversionResult> ConvertAsync(MediaImageDocument source, string targetFormatId,
        MediaRepresentationKind targetRepresentationKind, IReadOnlyDictionary<string, string> options,
        Action<MediaExplorationProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!TryCreate(source, targetFormatId, targetRepresentationKind, cancellationToken, out var image))
            throw new NotSupportedException($"The raw source does not contain a complete validated ADFS structure for '{targetFormatId}'.");
        var document = MediaImageDocumentFactory.CreateFloppySector(source.Source, image);
        return Task.FromResult(new MediaRepresentationConversionResult(document, [],
            ["The source raw file's 512-byte sector grouping is replaced by the validated Acorn ADF profile without changing its bytes. Raw files store no recorded sector identifiers or CRCs."]));
    }
}
