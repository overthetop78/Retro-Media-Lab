using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Images.Models.Sectors;
using RetroMediaLab.MediaEngine.Images.Reading.Reconstruction;
using RetroMediaLab.MediaFileSystems.FileSystems.Acorn.Adfs;
using RetroMediaLab.MediaFileSystems.FileSystems.Acorn.FileCore;

namespace RetroMediaLab.MediaEngine.Images.Formats.Floppy.Adf;

/// <summary>Reconnaît une image ADFS partielle depuis sa structure, sans remplacer les octets absents.</summary>
internal static class AcornAdfPartialImage
{
    internal const int ProbeLength = 4096;

    internal static bool TryDescribe(ReadOnlySpan<byte> header, long storedLength, out RegularSectorGeometry geometry, out int capacity)
    {
        geometry = null!;
        capacity = 0;
        if (header.Length < 512 || storedLength >= AcornAdfGeometry.HighDensityCapacity) return false;
        if (ValidOldMapChecksum(header[..256]) && ValidOldMapChecksum(header.Slice(256, 256)))
        {
            var declaredCapacity = (header[252] | header[253] << 8 | header[254] << 16) * 256;
            capacity = declaredCapacity;
            geometry = AcornAdfGeometry.BbcGeometries.FirstOrDefault(item => item.Capacity == declaredCapacity)
                ?? AcornAdfGeometry.Geometry;
            if (capacity > storedLength && capacity <= geometry.Capacity && capacity % geometry.BlockSize == 0) return true;
        }

        if (TryRecord(header, AcornFileCoreLayout.DiscRecordOffset, storedLength, out geometry, out capacity)) return true;
        return TryRecord(header, AcornFileCoreLayout.BootBlockAddress + AcornFileCoreLayout.BootDiscRecordOffset,
            storedLength, out geometry, out capacity);
    }

    private static bool TryRecord(ReadOnlySpan<byte> header, int offset, long storedLength, out RegularSectorGeometry geometry, out int capacity)
    {
        geometry = null!;
        capacity = 0;
        if (header.Length < offset + AcornFileCoreLayout.DiscRecordLength
            || !AcornFileCoreDiscRecord.TryParse(header.Slice(offset, AcornFileCoreLayout.DiscRecordLength),
                AcornAdfGeometry.HighDensityCapacity, out var record) || record is null
            || record.Log2SectorSize != 10 || record.DiscSize <= storedLength) return false;
        geometry = record.DiscSize > AcornAdfGeometry.Capacity ? AcornAdfGeometry.HighDensityGeometry : AcornAdfGeometry.Geometry;
        capacity = checked((int)record.DiscSize);
        return capacity % geometry.BlockSize == 0;
    }

    internal static bool TryCreate(ReadOnlySpan<byte> data, out SectorImage image)
    {
        image = null!;
        if (!TryDescribe(data, data.Length, out var geometry, out var capacity)) return false;
        var blocks = new List<SectorBlock>();
        for (var offset = 0; offset < data.Length; offset += geometry.BlockSize)
        {
            var logical = offset / geometry.BlockSize;
            var address = new SectorAddress(logical / (geometry.Heads * geometry.SectorsPerTrack),
                logical / geometry.SectorsPerTrack % geometry.Heads, logical % geometry.SectorsPerTrack);
            var length = Math.Min(geometry.BlockSize, data.Length - offset);
            var block = new SectorBlock(logical, address, data.Slice(offset, length).ToArray(),
                IntegrityValid: length == geometry.BlockSize ? true : null);
            if (geometry.BlockSize == 256) block = block with { RecordedAddress = new(address.Cylinder, 0, address.Number) };
            blocks.Add(block);
        }
        var candidate = new SectorImage(geometry.FormatId, geometry.BlockSize, geometry.Cylinders, geometry.Heads,
            geometry.SectorsPerTrack, blocks, capacity: capacity, logicalBlockCount: capacity / geometry.BlockSize);
        // The same map/directory checks as normal exploration decide whether the declared geometry is credible.
        if (!new AcornAdfsFileSystemReader().CanRead(candidate)) return false;
        image = candidate;
        return true;
    }

    internal static bool ValidOldMapChecksum(ReadOnlySpan<byte> sector)
    {
        var sum = 0;
        for (var offset = sector.Length - 2; offset >= 0; offset--)
            sum = (sum & 255) + (sum >> 8) + sector[offset];
        return (byte)sum == sector[^1];
    }
}
