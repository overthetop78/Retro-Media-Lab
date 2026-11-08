using System.IO;
using System.IO.Compression;
using System.Buffers.Binary;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Images.Models.Sectors;
using RetroMediaLab.MediaEngine.Images.Visualization;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Functions;

namespace RetroMediaLab.MediaEngine.Images.Formats.Floppy.Jfd;

/// <summary>JASPP tables and native records, including overlay tables and error stubs.</summary>
internal static class JfdCodec
{
    internal const int MaximumLength = 64 * 1024 * 1024;
    internal const string NativeKey = "jfd.native";
    internal sealed record Record(int Track, int Order, uint Header, int? Offset, int Length, int DescriptorOffset)
    {
        internal bool RawTrack => (Header & 0x00100000) != 0;
    }
    internal sealed record Image(byte[] Bytes, IReadOnlyList<Record> Records, SectorImage Sectors, IReadOnlyList<string> Diagnostics);

    internal static byte[] Unpack(byte[] bytes)
    {
        if (bytes.Length > MaximumLength) throw new InvalidDataException("JFD exceeds the supported size.");
        if (bytes.Length < 2 || bytes[0] != 0x1f || bytes[1] != 0x8b) return bytes;
        using var source = new MemoryStream(bytes, false);
        using var gzip = new GZipStream(source, CompressionMode.Decompress);
        using var output = new MemoryStream();
        var buffer = new byte[16384];
        int count;
        while ((count = gzip.Read(buffer)) != 0)
        {
            if (output.Length + count > MaximumLength) throw new InvalidDataException("JFD decompression exceeds the supported size.");
            output.Write(buffer, 0, count);
        }
        return output.ToArray();
    }

    internal static Image Decode(byte[] source)
    {
        var bytes = Unpack(source);
        if (bytes.Length < 304 || !bytes.AsSpan(0, 4).SequenceEqual("JFDI"u8)) throw new InvalidDataException("Invalid JFD signature or header.");
        if (U32(bytes, 8) != bytes.Length) throw new InvalidDataException("JFD uncompressed size does not match the header.");
        var tracks = Offset(bytes, 24); var sectors = Offset(bytes, 28); var data = Offset(bytes, 32);
        if (tracks < 304 || tracks > sectors || sectors > data || data > bytes.Length || (sectors - tracks) % 4 != 0)
            throw new InvalidDataException("Invalid JFD table offsets.");
        var trackCount = (sectors - tracks) / 4;
        if (trackCount is < 1 or > 512) throw new InvalidDataException("Invalid JFD track count.");
        var active = ReadTables(bytes, tracks, sectors, data, trackCount, bytes.Length);
        var dt = Offset(bytes, 36); var ds = Offset(bytes, 40); var dd = Offset(bytes, 44);
        if (dt != 0 || ds != 0 || dd != 0)
        {
            if (dt < data || ds < dt || dd < ds || dd > bytes.Length || ds - dt != trackCount * 4)
                throw new InvalidDataException("Invalid JFD delta table offsets.");
            var changes = ReadTables(bytes, dt, ds, dd, trackCount, bytes.Length);
            foreach (var pair in changes) active[pair.Key] = pair.Value;
        }
        var records = active.OrderBy(pair => pair.Key).SelectMany(pair => pair.Value).ToArray();
        var ordinary = records.Where(record => !record.RawTrack).ToArray();
        var cylinders = Math.Max(1, (active.Keys.DefaultIfEmpty(0).Max() / 2) + 1);
        var heads = active.Keys.Any(track => track % 2 != 0) ? 2 : 1;
        var spt = Math.Max(1, ordinary.GroupBy(record => record.Track).Select(group => group.Select(record => (record.Header >> 8) & 255).Distinct().Count()).DefaultIfEmpty(1).Max());
        var size = ordinary.GroupBy(record => record.Length).OrderByDescending(group => group.Count()).Select(group => group.Key).FirstOrDefault(1024);
        var zeroBased = ordinary.Any(record => ((record.Header >> 8) & 255) == 0);
        var blocks = new List<SectorBlock>();
        var requiresNativeLayout = records.Any(record => record.RawTrack || record.Offset is null || (record.Header >> 24) != 255);
        var used = new HashSet<int>();
        var extra = cylinders * heads * spt;
        foreach (var record in ordinary)
        {
            var number = (int)(record.Header >> 8) & 255;
            var logicalTrack = record.Track / 2 * heads + record.Track % 2;
            var logical = logicalTrack * spt + number - (zeroBased ? 0 : 1);
            if (logical < logicalTrack * spt || logical >= (logicalTrack + 1) * spt || !used.Add(logical))
            { logical = extra++; used.Add(logical); }
            if (record.Offset is not { } offset) continue; // A stub is not readable zero-filled sector data.
            var idValid = (record.Header & 0x10) == 0;
            var dataValid = (record.Header & 0x20) == 0;
            blocks.Add(new(logical, new(record.Track / 2, record.Track % 2, number), bytes.AsSpan(offset, record.Length).ToArray(), idValid && dataValid,
                Tag: requiresNativeLayout ? BitConverter.GetBytes(record.Header).Concat(new byte[] { 1 }).ToArray() : BitConverter.GetBytes(record.Header),
                FormatCode: (byte)((record.Header >> 16) & 15))
            { HeaderIntegrityValid = idValid, DataIntegrityValid = dataValid, RecordedSizeCode = (byte)(record.Header & 15), RecordedTrackOrder = record.Order });
        }
        var image = new SectorImage(DiskImageFormatIds.Jfd, size, cylinders, heads, spt, blocks, allowVariableBlockSize: true,
            logicalBlockCount: Math.Max(cylinders * heads * spt, extra));
        var diagnostics = new List<string>();
        if (records.Any(record => record.RawTrack)) diagnostics.Add("JFD contains native DiscOp 3 track records retained in the native structure.");
        if (ordinary.Any(record => (record.Header >> 24) != 255)) diagnostics.Add("JFD contains explicit rotational sector positions retained in the native structure.");
        return new(bytes, records, image, diagnostics);
    }

    private static Dictionary<int, IReadOnlyList<Record>> ReadTables(byte[] bytes, int tracks, int sectors, int data, int count, int end)
    {
        var result = new Dictionary<int, IReadOnlyList<Record>>();
        for (var track = 0; track < count; track++)
        {
            var relative = U32(bytes, tracks + track * 4);
            if (relative == uint.MaxValue) continue;
            if (relative > data - sectors || relative % 4 != 0) throw new InvalidDataException("JFD track points outside the sector table.");
            var position = checked(sectors + (int)relative);
            var records = new List<Record>();
            while (true)
            {
                Require(bytes, position, 4, data);
                var header = U32(bytes, position);
                if (header == uint.MaxValue) break;
                Require(bytes, position, 8, data);
                var location = U32(bytes, position + 4);
                var size = (int)(header & 15);
                var density = (header >> 16) & 15;
                if (size > 7 || density is not (1 or 2 or 4 or 8)) throw new InvalidDataException("Invalid JFD sector size or density.");
                int? offset = location == uint.MaxValue ? null : checked(data + (int)location);
                var length = 128 << size;
                if ((header & 0x00100000) != 0 && offset is { } raw)
                {
                    Require(bytes, raw, 4, end); length = checked(4 + (int)U32(bytes, raw));
                }
                if (offset is { } present) Require(bytes, present, length, end);
                records.Add(new(track, records.Count, header, offset, length, position));
                if (records.Count > 256) throw new InvalidDataException("Too many JFD records on one track.");
                position += 8;
            }
            result[track] = records;
        }
        return result;
    }

    internal static byte[] Rewrite(Image native, SectorImage sectors)
    {
        var bytes = native.Bytes.ToArray();
        var supplied = sectors.AvailableBlocks.ToDictionary(block => (block.Address.Cylinder * 2 + block.Address.Head, block.RecordedTrackOrder ?? -1));
        var writes = new Dictionary<int, byte[]>();
        var headerWrites = new Dictionary<int, uint>();
        foreach (var record in native.Records.Where(record => !record.RawTrack && record.Offset is not null))
        {
            if (!supplied.Remove((record.Track, record.Order), out var block) || block.Data.Count != record.Length
                || block.Address.Number != ((record.Header >> 8) & 255)
                || block.DeletedDataMark == true
                || block.RecordedAddress is not null && block.RecordedAddress != block.Address
                || block.RecordedSizeCode != (byte)(record.Header & 15))
                throw new InvalidDataException("JFD sector edits must retain native geometry.");
            var header = record.Header & ~0x30u;
            if ((block.HeaderIntegrityValid ?? ((record.Header & 0x10) == 0)) == false) header |= 0x10;
            if ((block.DataIntegrityValid ?? block.IntegrityValid ?? ((record.Header & 0x20) == 0)) == false) header |= 0x20;
            if (headerWrites.TryGetValue(record.DescriptorOffset, out var previousHeader) && previousHeader != header)
                throw new InvalidDataException("Conflicting edits of shared JFD descriptors.");
            headerWrites[record.DescriptorOffset] = header;
            var data = block.Data.ToArray(); var offset = record.Offset!.Value;
            if (writes.TryGetValue(offset, out var previous) && !previous.AsSpan().SequenceEqual(data))
                throw new InvalidDataException("Conflicting edits of shared JFD sector data.");
            writes[offset] = data;
        }
        if (supplied.Count != 0) throw new InvalidDataException("JFD contains sectors absent from its native tables.");
        var coveredEnd = 0;
        foreach (var pair in writes.OrderBy(pair => pair.Key))
        {
            var overlap = Math.Min(pair.Value.Length, Math.Max(0, coveredEnd - pair.Key));
            if (overlap > 0 && !bytes.AsSpan(pair.Key, overlap).SequenceEqual(pair.Value.AsSpan(0, overlap)))
                throw new InvalidDataException("Conflicting edits of overlapping JFD data.");
            pair.Value.CopyTo(bytes, pair.Key); coveredEnd = Math.Max(coveredEnd, pair.Key + pair.Value.Length);
        }
        foreach (var pair in headerWrites) BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(pair.Key), pair.Value);
        return bytes;
    }

    internal static bool CanCreate(SectorImage image)
    {
        if (image.AddressingKind != SectorImageAddressingKind.Physical || image.Cylinders > 256 || image.Heads > 2
            || image.AvailableBlocks.Count != image.Cylinders * image.Heads * image.SectorsPerTrack) return false;
        var policy = new SectorImageVisualizationPolicyRegistry().Resolve(image);
        if (policy is null || policy.EncoderId(image) is not (FluxCodecIds.IsoFm or FluxCodecIds.IsoMfm)) return false;
        return image.AvailableBlocks.All(block => block.Address.Cylinder >= 0 && block.Address.Cylinder < image.Cylinders
            && block.Address.Head >= 0 && block.Address.Head < image.Heads
            && block.Address.Number is >= 0 and <= 255 && block.DeletedDataMark != true
            && (block.RecordedAddress is null || block.RecordedAddress == block.Address)
            && SectorSizeCode.TryFromByteCount(block.Data.Count, out var code) && code <= 3
            && (block.RecordedSizeCode is null || block.RecordedSizeCode == code))
            && image.AvailableBlocks.Select(block => block.Address).Distinct().Count() == image.AvailableBlocks.Count;
    }

    internal static byte[] Create(SectorImage image)
    {
        if (!CanCreate(image)) throw new InvalidDataException("These sectors cannot be represented by native JFD descriptors.");
        var policy = new SectorImageVisualizationPolicyRegistry().Resolve(image)!;
        var trackCount = Math.Max(160, image.Cylinders * 2);
        var groups = image.AvailableBlocks.GroupBy(block => block.Address.Cylinder * 2 + block.Address.Head)
            .ToDictionary(group => group.Key, group => group.OrderBy(block => block.RecordedTrackOrder ?? block.Address.Number).ToArray());
        const int trackOffset = 320;
        var sectorOffset = checked(trackOffset + trackCount * 4);
        var dataOffset = checked(sectorOffset + groups.Values.Sum(group => group.Length * 8 + 4));
        var bytes = new byte[checked(dataOffset + image.AvailableBlocks.Sum(block => block.Data.Count))];
        "JFDI"u8.CopyTo(bytes); Put(4, 204); Put(8, (uint)bytes.Length); Put(12, 0x00010001);
        Put(24, trackOffset); Put(28, (uint)sectorOffset); Put(32, (uint)dataOffset);
        var recordAt = sectorOffset; var dataAt = dataOffset;
        for (var track = 0; track < trackCount; track++)
        {
            if (!groups.TryGetValue(track, out var blocks)) { Put(trackOffset + track * 4, uint.MaxValue); continue; }
            Put(trackOffset + track * 4, (uint)(recordAt - sectorOffset));
            var ticks = policy.BitCellTicks(image, track / 2);
            var density = ticks == 0 ? 0 : 160u / ticks;
            if (density is not (1 or 2 or 4 or 8)) throw new InvalidDataException("JFD cannot represent this recording density.");
            foreach (var block in blocks)
            {
                SectorSizeCode.TryFromByteCount(block.Data.Count, out var code);
                var header = 0xff000000 | density << 16 | (uint)block.Address.Number << 8 | code;
                if (block.HeaderIntegrityValid == false) header |= 0x10;
                if ((block.DataIntegrityValid ?? block.IntegrityValid) == false) header |= 0x20;
                Put(recordAt, header); Put(recordAt + 4, (uint)(dataAt - dataOffset));
                block.Data.ToArray().CopyTo(bytes, dataAt); dataAt += block.Data.Count; recordAt += 8;
            }
            Put(recordAt, uint.MaxValue); recordAt += 4;
        }
        return bytes;
        void Put(int at, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at), value);
    }

    private static uint U32(byte[] bytes, int position) { Require(bytes, position, 4, bytes.Length); return BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(position)); }
    private static int Offset(byte[] bytes, int position)
    {
        var value = U32(bytes, position);
        if (value > bytes.Length) throw new InvalidDataException("JFD offset exceeds the image length.");
        return (int)value;
    }
    private static void Require(byte[] bytes, int position, int count, int end)
    {
        if (position < 0 || count < 0 || position > end - count || end > bytes.Length) throw new InvalidDataException("Truncated JFD structure.");
    }
}
