using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Acorn;
using RetroMediaLab.MediaEngine.Images.Models.Blocks;

namespace RetroMediaLab.MediaEngine.Services;

internal sealed partial class MediaRuntimeDisk
{
    public void PrepareBlockFormat(HardDiskGeometry geometry)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(geometry);
        if (IsReadOnly) throw new UnauthorizedAccessException();
        if (FormatId != Constants.HardDiskImageFormatIds.AcornWinchester || Blocks is null
            || !AcornWinchesterFormat.SupportsGeometry(geometry)) throw new NotSupportedException();

        // Materialize pending sector overlays before changing their indexing/block size.
        var previous = (BlockMediaImageRepresentation)UpdatedDocument().Representation;
        var ranges = new List<MediaDataRange>();
        foreach (var range in previous.Ranges)
        {
            if (range.Address >= geometry.Capacity) break;
            var length = Math.Min(range.Length, geometry.Capacity - range.Address);
            ranges.Add(new(range.Address, length, range.Kind, range.Source, range.SourceOffset));
        }
        if (geometry.Capacity > previous.Capacity)
            ranges.Add(new(previous.Capacity, geometry.Capacity - previous.Capacity, MediaDataRangeKind.Unavailable));
        var representation = new BlockMediaImageRepresentation(geometry.Capacity, geometry.SectorSize, ranges, geometry);
        _document = MediaImageDocument.CreateUnexplored(_document.Source, FormatId, _document.MediaKind,
            representation, _document.Diagnostics, _document.Metadata);
        _changes.Clear();
        _dataMarks.Clear();
        _writtenTracks.Clear();
        _trackProjectionErrors.Clear();
        _winchesterProjectionErrors.Clear();
        _trackTemplates.Clear();
        _documentChanged = true;
    }
}
