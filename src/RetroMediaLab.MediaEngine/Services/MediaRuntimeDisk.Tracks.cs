using System.IO;
using RetroMediaLab.MediaEngine.Images.Models.Flux;
using RetroMediaLab.MediaEngine.Images.Reading.Decoding;
using RetroMediaLab.MediaEngine.Images.Reading.Decoding.Definitions;
using RetroMediaLab.MediaEngine.Images.Writing.Encoding;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Functions;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.CpcDsk;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.ImageDisk;
using RetroMediaLab.MediaEngine.Images.Models.Sectors;

namespace RetroMediaLab.MediaEngine.Services;

internal sealed partial class MediaRuntimeDisk
{
    private const uint NanosecondsPerSecond = 1_000_000_000;
    private readonly Dictionary<(int Cylinder, int Head), MediaFluxTrackData> _writtenTracks = new();
    private readonly Dictionary<(int Cylinder, int Head), SectorBlock[]> _trackTemplates = new();
    private readonly Dictionary<(int Cylinder, int Head), (uint Cell, Exception Error)> _trackProjectionErrors = new();

    public MediaFluxTrackData ReadTrack(int cylinder, int head, string encodingId, uint bitCellNanoseconds,
        uint indexTimeNanoseconds, IReadOnlyDictionary<string, int>? attributes = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegative(cylinder);
        ArgumentOutOfRangeException.ThrowIfNegative(head);
        if (cylinder >= Cylinders) throw new ArgumentOutOfRangeException(nameof(cylinder));
        if (head >= Heads) throw new ArgumentOutOfRangeException(nameof(head));
        ArgumentOutOfRangeException.ThrowIfZero(bitCellNanoseconds);
        ArgumentOutOfRangeException.ThrowIfZero(indexTimeNanoseconds);
        if (_writtenTracks.TryGetValue((cylinder, head), out var written)) return written;
        var document = UpdatedDocument();
        if (document.Representation is not SectorMediaImageRepresentation representation)
            throw new NotSupportedException();
        var track = new SectorImageTrackEncoder().EncodeTrack(representation.Image, cylinder, head,
            encodingId, attributes, bitCellNanoseconds, indexTimeNanoseconds);
        return new([new(indexTimeNanoseconds, track.Revolution.FluxIntervals)]);
    }

    public void WriteTrackWindow(int cylinder, int head, string encodingId, uint bitCellNanoseconds,
        uint indexTimeNanoseconds, uint startNanoseconds, uint durationNanoseconds,
        IReadOnlyList<uint> transitionOffsetsNanoseconds, IReadOnlyDictionary<string, int>? attributes = null,
        bool includeEndingInstant = false)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (IsReadOnly) throw new UnauthorizedAccessException();
        if (encodingId != IsoFmFormat.CodecId) throw new NotSupportedException(nameof(encodingId));
        var source = ReadTrack(cylinder, head, encodingId, bitCellNanoseconds, indexTimeNanoseconds, attributes);
        var revolution = MediaFluxTrackDataFunctions.ReplaceWindow(source.Revolutions[0],
            startNanoseconds, durationNanoseconds, transitionOffsetsNanoseconds, includeEndingInstant);
        if (durationNanoseconds == 0) return;
        var decoded = new IsoFmDecoder().Decode(new(revolution.IndexTimeNanoseconds,
            revolution.FluxIntervalsNanoseconds), bitCellNanoseconds);
        var fields = decoded.Sectors.Select((sector, index) => new MediaDiskSector(index,
            sector.Data?.ToArray() ?? [], sector.Cylinder, sector.Head, sector.Number, sector.IntegrityValid)
        {
            RecordedSizeCode = sector.SizeCode,
            HeaderIntegrityValid = sector.HeaderIntegrityValid,
            DataIntegrityValid = sector.DataIntegrityValid,
            DeletedDataMark = sector.DeletedDataMark,
            RecordedErrorCheckCode = sector.RecordedErrorCheckCode.ToArray()
        }).ToArray();
        if (!_trackTemplates.ContainsKey((cylinder, head)) && Sectors is { } original)
            _trackTemplates[(cylinder, head)] = original.AvailableBlocks
                .Where(block => block.Address.Cylinder == cylinder && block.Address.Head == head).ToArray();
        _documentChanged = true;
        try
        {
            try { FormatTrack(cylinder, head, fields, checked((int)(NanosecondsPerSecond / bitCellNanoseconds)), mfm: false); }
            catch (Exception error) when (error is NotSupportedException or ArgumentException)
            {
                // La piste physique reste utilisable ; sa projection en mémoire ne vaut pas autorisation
                // de perdre des diagnostics lors de la publication dans un conteneur sectoriel plus pauvre.
                var pending = UpdatedDocument();
                if (pending.Representation is not SectorMediaImageRepresentation sectors) throw;
                var projected = SectorTrackFunctions.Project(sectors.Image, cylinder, head, fields);
                _document = MediaImageDocument.CreateUnexplored(pending.Source, pending.FormatId, pending.MediaKind,
                    new SectorMediaImageRepresentation(projected), pending.Diagnostics, pending.Metadata);
                _changes.Clear();
                _dataMarks.Clear();
                _trackProjectionErrors[(cylinder, head)] = (bitCellNanoseconds, error);
            }
        }
        finally { _writtenTracks[(cylinder, head)] = new([revolution]); }
    }

    private void RetryTrackProjection()
    {
        foreach (var entry in _trackProjectionErrors.ToArray())
        {
            var pending = UpdatedDocument();
            if (pending.Representation is not SectorMediaImageRepresentation sectors) throw new NotSupportedException();
            var fields = sectors.Image.AvailableBlocks
                .Where(block => block.Address.Cylinder == entry.Key.Cylinder && block.Address.Head == entry.Key.Head)
                .OrderBy(block => block.RecordedTrackOrder ?? block.LogicalBlock)
                .Select(block => new MediaDiskSector(block.LogicalBlock, block.Data.ToArray(),
                    (block.RecordedAddress ?? block.Address).Cylinder, (block.RecordedAddress ?? block.Address).Head,
                    (block.RecordedAddress ?? block.Address).Number, block.IntegrityValid)
                {
                    RecordedSizeCode = block.RecordedSizeCode,
                    HeaderIntegrityValid = block.HeaderIntegrityValid,
                    DataIntegrityValid = block.DataIntegrityValid,
                    DeletedDataMark = block.DeletedDataMark,
                    RecordedErrorCheckCode = block.RecordedErrorCheckCode.ToArray()
                }).ToArray();
            var raw = _writtenTracks.GetValueOrDefault(entry.Key);
            try { FormatTrack(entry.Key.Cylinder, entry.Key.Head, fields,
                checked((int)(NanosecondsPerSecond / entry.Value.Cell)), mfm: false); }
            catch (Exception error) when (error is NotSupportedException or ArgumentException)
            { throw new NotSupportedException(nameof(WriteTrackWindow), error); }
            finally { if (raw is not null) _writtenTracks[entry.Key] = raw; }
        }
    }

    private void InvalidateWrittenTrack(int cylinder, int head) => _writtenTracks.Remove((cylinder, head));

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
        else updated = SectorTrackFunctions.Replace(previous, cylinder, head, sectors, retainGeometry: FormatId != CpcDskFormat.FormatId, previousTrack: _trackTemplates.GetValueOrDefault((cylinder, head)));

        var document = MediaImageDocument.CreateUnexplored(pending.Source, updated.FormatId, pending.MediaKind,
            new SectorMediaImageRepresentation(updated), pending.Diagnostics, metadata);
        _document = document;
        _changes.Clear();
        _dataMarks.Clear();
        _documentChanged = true;
        InvalidateWrittenTrack(cylinder, head);
        _trackProjectionErrors.Remove((cylinder, head));
        _trackTemplates.Remove((cylinder, head));
    }
}
