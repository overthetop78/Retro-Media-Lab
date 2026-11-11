using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Adf;
using RetroMediaLab.MediaEngine.Images.Models.Sectors;
using RetroMediaLab.MediaEngine.Images.Reading;
using RetroMediaLab.MediaEngine.Images.Writing.Encoding;
using RetroMediaLab.MediaEngine.Interfaces.Conversion;

namespace RetroMediaLab.MediaEngine.Images.Conversion.Acorn;

/// <summary>Exporte les secteurs ordinaires JFD, CPCEMU ou TD0 qu'un profil ADF peut représenter sans masquer d'erreur.</summary>
internal sealed class SectorContainerToAcornAdfRepresentationConverter : IMediaRepresentationConverter
{
    public string Id => "sector-container-to-acorn-adf";
    public IReadOnlySet<MediaRepresentationKind> SourceRepresentationKinds { get; } = new HashSet<MediaRepresentationKind> { MediaRepresentationKind.Sectors };
    public IReadOnlySet<MediaRepresentationKind> TargetRepresentationKinds => SourceRepresentationKinds;

    public bool CanConvert(MediaImageDocument source, string targetFormatId, MediaRepresentationKind targetRepresentationKind)
    {
        var jfd = source.FormatId.Equals(DiskImageFormatIds.Jfd, StringComparison.OrdinalIgnoreCase);
        var cpc = source.FormatId.Equals(DiskImageFormatIds.CpcEmuDsk, StringComparison.OrdinalIgnoreCase);
        var td0 = source.FormatId.Equals(DiskImageFormatIds.UcsdIbmMfm, StringComparison.OrdinalIgnoreCase);
        if (targetRepresentationKind != MediaRepresentationKind.Sectors || source.MediaKind != MediaKind.Floppy
            || !jfd && !cpc && !td0
            || source.Representation is not SectorMediaImageRepresentation sectors) return false;
        var geometry = AcornAdfGeometry.All.FirstOrDefault(item => item.FormatId.Equals(targetFormatId, StringComparison.OrdinalIgnoreCase)
            && item.FormatId.StartsWith(DiskImageFormatIds.AcornAdfsPrefix, StringComparison.OrdinalIgnoreCase));
        var image = sectors.Image;
        if (geometry is null || image.AddressingKind != SectorImageAddressingKind.Physical
            || image.BlockSize != geometry.BlockSize || image.Cylinders < geometry.Cylinders
            || image.Heads != geometry.Heads || image.SectorsPerTrack != geometry.SectorsPerTrack
            || image.BlockCount < geometry.BlockCount || image.AvailableBlocks.Count != geometry.BlockCount) return false;
        if (geometry.BlockSize == 256 && image.Cylinders != geometry.Cylinders)
        {
            if (!image.TryGetBlock(0, out var first) || !image.TryGetBlock(1, out var second)
                || first.Data.Count != 256 || second.Data.Count != 256) return false;
            var header = first.Data.ToArray();
            if (!AcornAdfPartialImage.ValidOldMapChecksum(header) || !AcornAdfPartialImage.ValidOldMapChecksum(second.Data.ToArray())
                || (header[252] | header[253] << 8 | header[254] << 16) * 256L != geometry.Capacity) return false;
        }
        var density = 160u / SectorImageTrackTimingCatalog.BitCellTicks(geometry.FormatId);
        var sizeCode = geometry.BlockSize == 256 ? 1 : 3;
        var td0Rate = geometry.FormatId.Equals(DiskImageFormatIds.AcornAdfs1600, StringComparison.OrdinalIgnoreCase) ? 2 : 0;
        foreach (var block in image.AvailableBlocks)
        {
            var logical = block.LogicalBlock;
            if (logical < 0 || logical >= geometry.BlockCount) return false;
            var address = new SectorAddress(logical / (geometry.Heads * geometry.SectorsPerTrack),
                logical / geometry.SectorsPerTrack % geometry.Heads, logical % geometry.SectorsPerTrack + geometry.FirstSectorNumber);
            var recorded = geometry.BlockSize == 256 ? address with { Head = 0 } : address;
            if (block.Address != address || (block.RecordedAddress ?? block.Address) != recorded
                || block.Data.Count != geometry.BlockSize || block.IntegrityValid != true
                || block.HeaderIntegrityValid == false || block.DataIntegrityValid == false
                || block.DeletedDataMark == true
                || (jfd ? block.FormatCode != density || block.Tag is { Count: > 4 }
                        : cpc ? block.FormatCode != sizeCode || block.Tag is { Count: > 0 }
                        : block.FormatCode != td0Rate || block.DiagnosticCode != 0 || block.RecordedSizeCode != sizeCode || block.Tag is { Count: > 0 })
                || block.RecordedSizeCode is { } code && code != sizeCode) return false;
        }
        return true;
    }

    public Task<MediaRepresentationConversionResult> ConvertAsync(MediaImageDocument source, string targetFormatId,
        MediaRepresentationKind targetRepresentationKind, IReadOnlyDictionary<string, string> options,
        Action<MediaExplorationProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!CanConvert(source, targetFormatId, targetRepresentationKind))
            throw new NotSupportedException($"'{source.FormatId}' sectors cannot be represented by ADF profile '{targetFormatId}'.");
        var image = ((SectorMediaImageRepresentation)source.Representation).Image;
        var geometry = AcornAdfGeometry.All.Single(item => item.FormatId.Equals(targetFormatId, StringComparison.OrdinalIgnoreCase));
        var jfd = source.FormatId.Equals(DiskImageFormatIds.Jfd, StringComparison.OrdinalIgnoreCase);
        var blocks = !jfd ? image.AvailableBlocks.Select(block => block with { FormatCode = null, Tag = null, RecordedTrackOrder = null, DiagnosticCode = null }) : image.AvailableBlocks;
        var converted = new SectorImage(targetFormatId, geometry.BlockSize, geometry.Cylinders, geometry.Heads, geometry.SectorsPerTrack, blocks);
        var document = MediaImageDocumentFactory.CreateFloppySector(source.Source, converted);
        var loss = source.FormatId.Equals(DiskImageFormatIds.Jfd, StringComparison.OrdinalIgnoreCase)
            ? "JFD container metadata is not preserved in the raw ADF representation."
            : source.FormatId.Equals(DiskImageFormatIds.CpcEmuDsk, StringComparison.OrdinalIgnoreCase)
                ? "CPCEMU container headers, track layout and timing are not preserved in the raw ADF representation."
                : "TeleDisk header, comment, track order and container metadata are not preserved in the raw ADF representation.";
        if (image.Cylinders != geometry.Cylinders) loss += " Empty trailing track-table slots outside the target profile are omitted; all target sectors are present.";
        return Task.FromResult(new MediaRepresentationConversionResult(document, [], [loss]));
    }
}
