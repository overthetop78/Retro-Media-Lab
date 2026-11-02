using System.IO;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Functions;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.CpcDsk;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.ImageDisk;
using RetroMediaLab.MediaEngine.Images.Models.Sectors;

namespace RetroMediaLab.MediaEngine.Services;

internal sealed partial class MediaRuntimeDisk
{
    public void FormatTrack(int cylinder, int head, IReadOnlyList<MediaDiskSector> sectors,
        int? dataRate = null, bool? mfm = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (IsReadOnly) throw new UnauthorizedAccessException();
        var pending = UpdatedDocument();
        if (pending.Representation is not SectorMediaImageRepresentation representation) throw new NotSupportedException();
        var previous = representation.Image;
        SectorTrackFunctions.Validate(previous, cylinder, head, sectors);
        SectorImage updated;
        var metadata = new Dictionary<string, string>(pending.Metadata);
        if (metadata.ContainsKey(ImdMetadataKeys.Structure))
        {
            var original = ImdMetadataFunctions.Restore(previous, metadata) ?? throw new NotSupportedException();
            var tracks = original.Tracks.ToList();
            var trackIndex = tracks.FindIndex(track => track.Cylinder == cylinder && track.Head == head);
            var old = trackIndex < 0 ? null : tracks[trackIndex];
            var mode = (dataRate, mfm) switch
            {
                (null, null) when old is not null => old.Mode,
                (ImdLayout.Rate500Kbps, false) => ImdMode.Fm500Kbps,
                (ImdLayout.Rate300Kbps, false) => ImdMode.Fm300Kbps,
                (ImdLayout.Rate250Kbps, false) => ImdMode.Fm250Kbps,
                (ImdLayout.Rate500Kbps, true) => ImdMode.Mfm500Kbps,
                (ImdLayout.Rate300Kbps, true) => ImdMode.Mfm300Kbps,
                (ImdLayout.Rate250Kbps, true) => ImdMode.Mfm250Kbps,
                _ => throw new NotSupportedException()
            };
            if (sectors.Any(sector => sector.HeaderIntegrityValid == false || sector.RecordedSizeCode is { } size
                && (size > SectorSizeCode.MaximumCode || SectorSizeCode.ToByteCount(size) != sector.Data.Length)))
                throw new NotSupportedException();
            var fields = sectors.Select(sector => new ImdSector(checked((byte)sector.Cylinder), checked((byte)sector.Head),
                checked((byte)sector.Number), sector.Data.Length,
                ImdSectorRecordTypeExtensions.ForState(false, sector.IntegrityValid != false, sector.DeletedDataMark == true),
                sector.Data.ToArray())).ToArray();
            var replacement = new ImdTrack(mode, checked((byte)cylinder), checked((byte)head), fields);
            if (trackIndex < 0) tracks.Add(replacement); else tracks[trackIndex] = replacement;
            updated = ImdReader.BuildImage(tracks);
            foreach (var pair in ImdMetadataFunctions.Create(new(original.Comment, tracks, updated))) metadata[pair.Key] = pair.Value;
        }
        else updated = SectorTrackFunctions.Replace(previous, cylinder, head, sectors, retainGeometry: FormatId != CpcDskFormat.FormatId);

        var document = MediaImageDocument.CreateUnexplored(pending.Source, updated.FormatId, pending.MediaKind,
            new SectorMediaImageRepresentation(updated), pending.Diagnostics, metadata);
        _document = document;
        _changes.Clear();
        _dataMarks.Clear();
        _documentChanged = true;
    }
}
