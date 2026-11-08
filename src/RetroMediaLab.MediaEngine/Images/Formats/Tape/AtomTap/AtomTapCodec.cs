using System.IO;
using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Images.Models.Sequential;
using RetroMediaLab.MediaEngine.Images.Reading.Blocks;
using RetroMediaLab.MediaEngine.Images.Reading.Sources;
using C = RetroMediaLab.MediaEngine.Constants.AtomTapeConstants;

namespace RetroMediaLab.MediaEngine.Images.Formats.Tape.AtomTap;

/// <summary>Validates Atom serial blocks and reassembles complete files without discarding errors.</summary>
internal static class AtomTapCodec
{
    internal static async Task<SequentialDecodeResult> DecodeStreamAsync(byte[] serial, CancellationToken token)
    {
        var segments = new List<SequentialMediaSegment>();
        var consumed = new bool[serial.Length];
        for (var at = 0; at <= serial.Length - C.SyncBytes; at++)
        {
            token.ThrowIfCancellationRequested();
            if (!serial.AsSpan(at, C.SyncBytes).SequenceEqual(new byte[] { C.Sync, C.Sync, C.Sync, C.Sync })) continue;
            var end = Array.IndexOf(serial, C.NameTerminator, at + C.SyncBytes);
            if (end < at + C.SyncBytes + 1 || end > at + C.SyncBytes + C.FileNameBytes || end + 9 > serial.Length) continue;
            var count = (serial[end + 1] & C.ContainsData) == 0 ? 0 : serial[end + 4] + 1;
            var payloadAt = end + 9;
            if (payloadAt > serial.Length - count - 1) continue;
            Add(serial.AsSpan(at, payloadAt - at));
            Add(serial.AsSpan(payloadAt, count + 1));
            Array.Fill(consumed, true, at, payloadAt + count + 1 - at);
            at = payloadAt + count;
        }
        var decoded = await DecodeAsync(new SequentialMediaImageRepresentation(null, segments: segments), token).ConfigureAwait(false);
        return consumed.All(value => value) ? decoded : new(decoded.DecoderId, decoded.Confidence, decoded.Blocks,
            decoded.UndecodedSegments, decoded.Diagnostics.Concat(new[] { "Unrecognized bytes remain in the Atom serial stream." }).ToArray());

        void Add(ReadOnlySpan<byte> data)
        {
            var bytes = new byte[data.Length + 3]; bytes[0] = C.DataBits; bytes[1] = C.NoParity; bytes[2] = C.StopBits;
            data.CopyTo(bytes.AsSpan(3));
            segments.Add(new(segments.Count, SequentialSegmentKind.DataBlock,
                dataRange: new(0, bytes.Length, MediaDataRangeKind.Stored, new MemoryRandomAccessData(bytes)),
                metadata: new Dictionary<string, string> { [UefConstants.ChunkIdMetadataKey] = "0104" }));
        }
    }

    internal static async Task<SequentialDecodeResult> DecodeAsync(SequentialMediaImageRepresentation representation, CancellationToken token)
    {
        var files = new List<SequentialDecodedBlock>();
        var retained = new List<SequentialMediaSegment>();
        var diagnostics = new List<string>();
        var pending = new List<SequentialMediaSegment>();
        var payload = new List<byte>();
        var positions = new List<long>();
        string? name = null;
        var load = 0;
        var execute = 0;
        var number = 0;
        byte[]? header = null;
        var valid = true;
        foreach (var segment in (representation.Segments ?? []).OrderBy(item => item.Position))
        {
            token.ThrowIfCancellationRequested();
            if (segment.Kind is SequentialSegmentKind.Carrier or SequentialSegmentKind.Silence) continue;
            var bytes = await BlockMediaDataReader.ReadRangeAsync(segment.DataRange, token).ConfigureAwait(false);
            if (segment.Kind == SequentialSegmentKind.TapeMark
                && segment.Metadata.GetValueOrDefault(UefConstants.ChunkIdMetadataKey) == "0117"
                && bytes is { Length: 2 } && BinaryPrimitives.ReadUInt16LittleEndian(bytes) == C.Baud) continue;
            if (segment.Kind != SequentialSegmentKind.DataBlock || bytes is null || bytes.Length < 3
                || segment.Metadata.GetValueOrDefault(UefConstants.ChunkIdMetadataKey) != "0104"
                || bytes[0] != C.DataBits || bytes[1] != C.NoParity || bytes[2] != C.StopBits)
            {
                retained.Add(segment);
                continue;
            }
            pending.Add(segment);
            positions.Add(segment.Position);
            var data = bytes.AsSpan(3).ToArray();
            if (header is null)
            {
                var end = Array.IndexOf(data, C.NameTerminator, C.SyncBytes);
                if (end < C.SyncBytes + 1 || end > C.SyncBytes + C.FileNameBytes || data.Length != end + 9
                    || !data.Take(C.SyncBytes).All(value => value == C.Sync))
                {
                    retained.AddRange(pending); pending.Clear(); positions.Clear();
                    continue;
                }
                var blockNumber = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(end + 2));
                var address = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(end + 7));
                var entry = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(end + 5));
                var blockName = Encoding.ASCII.GetString(data, C.SyncBytes, end - C.SyncBytes);
                if (name is null)
                {
                    name = blockName; load = address; execute = entry; number = 0;
                }
                valid &= blockName == name && entry == execute && blockNumber == number
                    && address == unchecked((ushort)(load + payload.Count))
                    && (data[end + 1] & C.Continuation) == (number == 0 ? 0 : C.Continuation);
                header = data;
                continue;
            }
            var terminator = header.Length - 9;
            var flags = header[terminator + 1];
            var count = (flags & C.ContainsData) == 0 ? 0 : header[terminator + 4] + 1;
            valid &= data.Length == count + 1;
            if (data.Length != count + 1)
            {
                retained.AddRange(pending); pending.Clear(); positions.Clear();
                header = null; name = null; payload.Clear(); valid = true;
                diagnostics.Add("Truncated Atom tape block.");
                continue;
            }
            valid &= unchecked((byte)(header.Skip(C.SyncBytes).Sum(value => (int)value)
                + data.Take(count).Sum(value => (int)value))) == data[count];
            payload.AddRange(data.Take(count));
            header = null; number++;
            if ((flags & C.MoreBlocks) != 0) continue;
            var metadata = new Dictionary<string, string>
            {
                ["fileName"] = name!, ["blockNumber"] = "0",
                ["loadAddress"] = load.ToString("x4", CultureInfo.InvariantCulture),
                ["executionAddress"] = execute.ToString("x4", CultureInfo.InvariantCulture)
            };
            files.Add(new(files.Count, payload.ToArray(), positions.ToArray(), valid, metadata));
            pending.Clear(); positions.Clear(); payload.Clear(); name = null; valid = true;
        }
        if (pending.Count > 0) { retained.AddRange(pending); diagnostics.Add("Incomplete Atom tape file."); }
        if (files.Any(file => file.IntegrityValid != true)) diagnostics.Add("Invalid Atom tape checksum or block sequence.");
        return new("acorn-atom-tape", files.Count == 0 ? 0 : files.Count(file => file.IntegrityValid == true) / (double)files.Count,
            files, retained, diagnostics);
    }

    internal static byte[] Encode(IReadOnlyList<SequentialDecodedBlock> files)
    {
        if (files.Count == 0) throw new InvalidDataException("An Atom TAP requires at least one complete file.");
        using var output = new MemoryStream();
        foreach (var file in files)
        {
            if (file.IntegrityValid != true || file.Data.Length > ushort.MaxValue
                || !file.Metadata.TryGetValue("fileName", out var name) || name.Length is < 1 or > C.FileNameBytes
                || name.Any(value => value is < ' ' or > '~')
                || !ushort.TryParse(file.Metadata.GetValueOrDefault("loadAddress"), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var load)
                || !ushort.TryParse(file.Metadata.GetValueOrDefault("executionAddress"), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var execute))
                throw new InvalidDataException("Atom TAP requires valid files with a name and 16-bit load/execute addresses.");
            var header = new byte[C.FileHeaderBytes];
            Encoding.ASCII.GetBytes(name, header);
            BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(C.LoadAddressOffset), load);
            BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(C.ExecutionAddressOffset), execute);
            BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(C.LengthOffset), (ushort)file.Data.Length);
            output.Write(header); output.Write(file.Data.Span);
        }
        return output.ToArray();
    }
}
