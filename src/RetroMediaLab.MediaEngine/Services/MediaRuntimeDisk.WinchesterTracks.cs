using System.IO;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Functions;
using RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Adaptec;
using RetroMediaLab.MediaEngine.Images.Models.Blocks;
using RetroMediaLab.MediaEngine.Images.Reading.Blocks;
using RetroMediaLab.MediaEngine.Enums;
using A = RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Acorn.AcornWinchesterFormat;
using C = RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Adaptec.AdaptecMfmFormat;

namespace RetroMediaLab.MediaEngine.Services;

internal sealed partial class MediaRuntimeDisk
{
    private readonly Dictionary<(int Cylinder, int Head), Exception> _winchesterProjectionErrors = new();

    private MediaFluxTrackData ReadWinchesterTrack(int cylinder, int head, uint period)
    {
        var blocks = (BlockMediaImageRepresentation)UpdatedDocument().Representation;
        if (blocks.Geometry is not { } geometry) throw new NotSupportedException();
        var count = geometry.SectorsPerTrack;
        var size = blocks.LogicalBlockSize;
        var first = ((long)cylinder * geometry.Heads + head) * count;
        var bytes = new byte[checked(count * size)];
        var sequential = (size, count) is (C.MinimumBlockSize, A.SmallSequentialSectors)
            or (C.MediumBlockSize, A.MediumSequentialSectors);
        var sectors = new MediaDiskSector[count];
        var position = 0;
        for (var logical = 0; logical < count; logical++)
        {
            while (sectors[position] is not null) if (++position == count) position = 0;
            var address = checked(first + logical);
            var destination = bytes.AsMemory(logical * size, size);
            var available = ReadableBlock(blocks, checked(address * size), size);
            if (available) BlockMediaDataReader.ReadExactlyAsync(blocks, checked(address * size), destination).GetAwaiter().GetResult();
            sectors[position] = new(address, available ? destination : ReadOnlyMemory<byte>.Empty,
                cylinder, head, checked((int)address));
            position += sequential ? 1 : C.DefaultInterleave;
            if (position >= count) position -= count;
        }
        Span<byte> flags = stackalloc byte[C.ParameterFlagCount];
        var initialTrack = cylinder == 0 && head == 0;
        if (initialTrack) WriteWinchesterPhysicalParameters(flags);
        return AdaptecMfmTrackCodec.Encode(sectors, sequential, period, size, initialTrack ? flags : default);
    }

    private void WriteWinchesterWindow(int cylinder, int head, uint period, uint start, uint duration,
        IReadOnlyList<uint> offsets, bool includeEndingInstant)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(cylinder);
        ArgumentOutOfRangeException.ThrowIfNegative(head);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(cylinder, Cylinders);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(head, Heads);
        ArgumentOutOfRangeException.ThrowIfZero(period);
        var original = duration >= period
            ? new MediaFluxTrackData([new(period, Array.Empty<uint>())])
            : ReadTrack(cylinder, head, C.EncodingId, C.CellNanoseconds, period);
        var changed = MediaFluxTrackDataFunctions.ReplaceWindow(original.Revolutions[0], start, duration,
            offsets, includeEndingInstant);
        if (duration == 0) return;
        _writtenTracks[(cylinder, head)] = new([changed]);
        _documentChanged = true;
        try
        {
            ProjectWinchesterTrack(cylinder, head);
            _winchesterProjectionErrors.Remove((cylinder, head));
        }
        catch (Exception error) when (error is NotSupportedException or ArgumentException or InvalidDataException)
        {
            // Keep the physical write, even when DAT/DSC cannot publish it without losing fields or defects.
            _winchesterProjectionErrors[(cylinder, head)] = error;
        }
    }

    private void ProjectWinchesterTrack(int cylinder, int head)
    {
        var track = _writtenTracks[(cylinder, head)];
        var fields = AdaptecMfmTrackCodec.Decode(track.Revolutions[0], cylinder, head, SectorSize);
        var first = ((long)cylinder * Heads + head) * SectorsPerTrack;
        if (fields.Count != SectorsPerTrack) throw new NotSupportedException();
        var seen = new HashSet<long>();
        Span<byte> flags = stackalloc byte[C.ParameterFlagCount];
        var initialTrack = cylinder == 0 && head == 0;
        var anchor = -1;
        if (initialTrack)
        {
            WriteWinchesterPhysicalParameters(flags);
            for (var index = 0; index < fields.Count; index++)
                if (fields[index].Sector.Number == 0) { anchor = index; break; }
            if (anchor < 0) throw new NotSupportedException();
        }
        for (var index = 0; index < fields.Count; index++)
        {
            var field = fields[index];
            var sector = field.Sector;
            var physicalPosition = initialTrack ? (index - anchor + fields.Count) % fields.Count : C.ParameterFlagCount;
            var expectedFlags = physicalPosition < flags.Length ? flags[physicalPosition] : (byte)0;
            if (sector.Number < first || sector.Number >= first + SectorsPerTrack || !seen.Add(sector.Number)
                || sector.Data.Length != SectorSize || sector.HeaderIntegrityValid != true || sector.DataIntegrityValid != true
                || (field.Id.Span[C.AddressOffset] & C.DefectiveAddressByteMask) != 0
                || (field.Id.Span[C.FlagsOffset] & C.DefectCountMask) != expectedFlags
                || ((field.Id.Span[C.FlagsOffset] & C.LastSectorFlag) != 0) != (sector.Number == first + SectorsPerTrack - 1))
                throw new NotSupportedException();
        }
        // Validate the entire track before changing logical blocks. No invalid data becomes a successful DAT sector.
        foreach (var field in fields) _changes[field.Sector.Index] = field.Sector.Data.ToArray();
    }

    private void WriteWinchesterPhysicalParameters(Span<byte> flags)
    {
        if (!Metadata.TryGetValue(A.DescriptorMetadataKey, out var encoded)) throw new NotSupportedException();
        byte[] descriptor;
        try { descriptor = Convert.FromBase64String(encoded); }
        catch (FormatException error) { throw new InvalidDataException(nameof(A.DescriptorMetadataKey), error); }
        if (!A.TryWritePhysicalParameters(descriptor, Blocks!.Capacity, flags)) throw new NotSupportedException();
    }

    private MediaDiskSector ReadWrittenWinchesterSector(long index, int cylinder, int head)
    {
        var fields = AdaptecMfmTrackCodec.Decode(_writtenTracks[(cylinder, head)].Revolutions[0], cylinder, head, SectorSize);
        foreach (var field in fields)
            if (field.Sector.Number == index && field.Sector.HeaderIntegrityValid == true
                && (field.Id.Span[C.AddressOffset] & C.DefectiveAddressByteMask) == 0 && field.Sector.Data.Length == SectorSize)
                return field.Sector;
        throw new InvalidDataException();
    }

    private void RetryWinchesterTrackProjection()
    {
        foreach (var address in _winchesterProjectionErrors.Keys.ToArray())
        {
            try { ProjectWinchesterTrack(address.Cylinder, address.Head); }
            catch (Exception error) when (error is NotSupportedException or ArgumentException or InvalidDataException)
            { throw new NotSupportedException(nameof(WriteTrackWindow), error); }
            _winchesterProjectionErrors.Remove(address);
        }
    }

    private static bool ReadableBlock(BlockMediaImageRepresentation blocks, long offset, int length)
    {
        var end = offset + length;
        foreach (var range in blocks.Ranges)
        {
            if (range.Address + range.Length <= offset) continue;
            if (range.Address > offset || range.Kind == MediaDataRangeKind.Unavailable) return false;
            offset = Math.Min(end, range.Address + range.Length);
            if (offset == end) return true;
        }
        return false;
    }
}
