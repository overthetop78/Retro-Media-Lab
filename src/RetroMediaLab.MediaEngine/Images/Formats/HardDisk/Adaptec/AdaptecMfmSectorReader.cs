using System.IO;
using RetroMediaLab.MediaEngine.Contracts;
using C = RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Adaptec.AdaptecMfmFormat;

namespace RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Adaptec;

/// <summary>Lecture logique Adaptec sur pistes empruntées ; conserve un seul décodage de piste.</summary>
internal sealed class AdaptecMfmSectorReader(Func<int, int, ValueTask<MediaFluxTrackData>> readTrack,
    int physicalCylinders, int physicalHeads)
{
    private readonly object sync = new();
    internal int SectorSize { get { lock (sync) { EnsureSectorGeometry(); return sectorGeometry.SectorSize; } } }
    internal long SectorCount { get { lock (sync) { EnsureSectorGeometry(); return sectorCount; } } }
    internal int SectorsPerTrack { get { lock (sync) { EnsureSectorGeometry(); return sectorGeometry.SectorsPerTrack; } } }
    internal long Capacity => checked(SectorCount * SectorSize);
    internal AdaptecMfmGeometry Geometry { get { lock (sync) { EnsureSectorGeometry(); return sectorGeometry; } } }

    private AdaptecMfmGeometry sectorGeometry;
    private bool sectorGeometryLoaded;
    private long sectorCount;
    private IReadOnlyList<(ReadOnlyMemory<byte> Id, MediaDiskSector Sector)>? sectorTrack;
    private (int Cylinder, int Head, int Size) sectorTrackAddress;

    private void EnsureSectorGeometry()
    {
        if (sectorGeometryLoaded) return;
        var first = DecodeSectors(0, 0, 0);
        if (AdaptecMfmGeometry.TryRead(first, out var geometry)
            && geometry.Cylinders <= physicalCylinders && geometry.Heads <= physicalHeads)
        {
            sectorGeometry = geometry;
            var last = DecodeSectors(geometry.Cylinders - 1, geometry.Heads - 1, geometry.SectorSize);
            foreach (var field in last)
                if (UsableIdentity(field) && (field.Id.Span[C.FlagsOffset] & C.LastSectorFlag) != 0)
                    sectorCount = Math.Max(sectorCount, (long)field.Sector.Number + 1);
        }
        sectorGeometryLoaded = true;
    }

    private IReadOnlyList<(ReadOnlyMemory<byte> Id, MediaDiskSector Sector)> DecodeSectors(int cylinder, int head, int size)
    {
        var address = (cylinder, head, size);
        if (sectorTrack is not null && sectorTrackAddress == address) return sectorTrack;
        var track = readTrack(cylinder, head).GetAwaiter().GetResult();
        var fields = AdaptecMfmTrackCodec.Decode(track.Revolutions[0], cylinder, head, size);
        sectorTrack = fields;
        sectorTrackAddress = address;
        return fields;
    }

    private static bool UsableIdentity((ReadOnlyMemory<byte> Id, MediaDiskSector Sector) field) =>
        field.Sector.HeaderIntegrityValid == true
        && (field.Id.Span[C.AddressOffset] & C.DefectiveAddressByteMask) == 0;

    internal long? FindSector(int cylinder, int head, int number)
    {
        lock (sync)
        {
            EnsureSectorGeometry();
            if (sectorGeometry.SectorSize == 0) return null;
            long? found = null;
            foreach (var field in DecodeSectors(cylinder, head, sectorGeometry.SectorSize))
                if (UsableIdentity(field) && field.Sector.Number == number)
                {
                    if (found is not null) return null;
                    found = field.Sector.Index;
                }
            return found;
        }
    }

    internal IReadOnlyList<long> GetTrackSectors(int cylinder, int head)
    {
        lock (sync)
        {
            EnsureSectorGeometry();
            if (sectorGeometry.SectorSize == 0) return [];
            return DecodeSectors(cylinder, head, sectorGeometry.SectorSize)
                .Where(UsableIdentity).Select(field => field.Sector.Index).ToArray();
        }
    }

    internal ValueTask<MediaDiskSector> ReadSectorAsync(long index, CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureSectorGeometry();
            if (index < 0 || index >= sectorCount) throw new ArgumentOutOfRangeException(nameof(index));
            // Defective physical slots move a logical address to a later track, never an earlier one.
            var firstTrack = index / sectorGeometry.SectorsPerTrack;
            var trackCount = (long)sectorGeometry.Cylinders * sectorGeometry.Heads;
            for (var track = firstTrack; track < trackCount; track++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                MediaDiskSector? found = null;
                foreach (var field in DecodeSectors((int)(track / sectorGeometry.Heads),
                    (int)(track % sectorGeometry.Heads), sectorGeometry.SectorSize))
                    if (UsableIdentity(field) && field.Sector.Index == index)
                    {
                        if (found is not null) throw new InvalidDataException();
                        found = field.Sector;
                    }
                if (found is not null) return ValueTask.FromResult(found);
            }
            throw new InvalidDataException();
        }
    }

    internal async ValueTask ReadExactlyAsync(long address, Memory<byte> destination, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (address < 0 || address > Capacity || destination.Length > Capacity - address)
            throw new ArgumentOutOfRangeException(nameof(address));
        while (!destination.IsEmpty)
        {
            token.ThrowIfCancellationRequested();
            var sector = await ReadSectorAsync(address / SectorSize, token).ConfigureAwait(false);
            if (sector.IntegrityValid != true || sector.Data.Length != SectorSize) throw new InvalidDataException();
            var offset = (int)(address % SectorSize);
            var count = Math.Min(destination.Length, SectorSize - offset);
            sector.Data.Slice(offset, count).CopyTo(destination);
            destination = destination[count..];
            address += count;
        }
    }
}
