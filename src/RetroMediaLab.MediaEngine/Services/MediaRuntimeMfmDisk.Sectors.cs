using System.IO;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Adaptec;
using C = RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Adaptec.AdaptecMfmFormat;

namespace RetroMediaLab.MediaEngine.Services;

internal sealed partial class MediaRuntimeMfmDisk
{
    public long? FindSector(int cylinder, int head, int number)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return representation.Sectors.FindSector(cylinder, head, number);
    }

    public IReadOnlyList<long> GetTrackSectors(int cylinder, int head)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return representation.Sectors.GetTrackSectors(cylinder, head);
    }

    public ValueTask<MediaDiskSector> ReadSectorAsync(long index, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return representation.Sectors.ReadSectorAsync(index, cancellationToken);
    }

    public void WriteSector(long index, ReadOnlySpan<byte> data)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (IsReadOnly) throw new UnauthorizedAccessException();
        if (data.Length != SectorSize || data.IsEmpty)
            throw new ArgumentException(nameof(data));
        var sector = ReadSectorAsync(index).GetAwaiter().GetResult();
        var track = ReadTrack(sector.Cylinder, sector.Head, C.EncodingId, C.CellNanoseconds, C.NominalIndexNanoseconds);
        var window = AdaptecMfmTrackCodec.CreateSectorWrite(track.Revolutions[0],
            sector.Cylinder, sector.Head, index, data);
        WriteTrackWindow(sector.Cylinder, sector.Head, C.EncodingId, C.CellNanoseconds, C.NominalIndexNanoseconds,
            window.StartNanoseconds, window.DurationNanoseconds, window.Transitions);
    }
}
