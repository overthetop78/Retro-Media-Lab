using System.IO;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using K4os.Compression.LZ4;
using RetroMediaLab.MediaEngine.Images.Models.Flux;

namespace RetroMediaLab.MediaEngine.Images.Formats.Floppy.HxcStream;

/// <summary>Native HxC chunks: exact pulse units, sampled I/O, LZ4 and chunk CRC.</summary>
internal static class HxcStreamEncoding
{
    internal static bool CanEncode(ProtectedTrack track)
    {
        try { return track.Revolutions.Count > 0 && ExactResolution(track) > 0; }
        catch (InvalidDataException) { return false; }
    }
    internal static string Digest(ProtectedTrack track, bool writeProtected)
    {
        using var bytes = new MemoryStream();
        using (var writer = new BinaryWriter(bytes, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(track.Cylinder); writer.Write(track.Head); writer.Write(writeProtected);
            foreach (var revolution in track.Revolutions)
            {
                writer.Write(revolution.ResolutionNanoseconds); writer.Write(revolution.Flux.IndexTimeTicks);
                writer.Write(revolution.Flux.FluxIntervals.Count);
                foreach (var interval in revolution.Flux.FluxIntervals) writer.Write(interval);
            }
        }
        return Convert.ToHexString(SHA256.HashData(bytes.ToArray()));
    }

    internal static byte[] Encode(ProtectedTrack track, bool writeProtected, IReadOnlyDictionary<string, string> metadata)
    {
        if (track.Revolutions.Count == 0) throw new InvalidDataException("HxC Stream requires captured flux revolutions.");
        var resolution = ExactResolution(track);
        var indexes = new List<long> { 0 };
        var pulses = new List<uint>();
        long elapsed = 0, previousPulse = 0;
        foreach (var revolution in track.Revolutions)
        {
            long position = 0;
            foreach (var interval in revolution.Flux.FluxIntervals)
            {
                position = checked(position + (long)interval * revolution.ResolutionNanoseconds / resolution);
                var distance = checked(elapsed + position - previousPulse);
                if (distance is <= 0 or > 0x0fffffff) throw new InvalidDataException("HxC pulse interval cannot be represented exactly.");
                pulses.Add((uint)distance); previousPulse = elapsed + position;
            }
            var duration = checked((long)revolution.Flux.IndexTimeTicks * revolution.ResolutionNanoseconds / resolution);
            if (position > duration || duration <= 0) throw new InvalidDataException("Flux transitions exceed the index period.");
            elapsed = checked(elapsed + duration); indexes.Add(elapsed);
        }
        if (elapsed / 16 + 2 > HxcStreamFormat.MaximumUnpackedBlockSize / 2) throw new InvalidDataException("HxC I/O capture is too long.");
        var io = new byte[checked((int)(elapsed / 16 + 2) * 2)];
        if (writeProtected) for (var at = 0; at < io.Length; at += 2) io[at] = 1 << 5;
        foreach (var index in indexes) io[checked((int)(index / 16) * 2)] |= 1;
        using var packedPulses = new MemoryStream();
        foreach (var pulse in pulses) WritePulse(packedPulses, pulse);
        var text = metadata.Where(pair => !pair.Key.StartsWith("hxcstream.", StringComparison.Ordinal) && pair.Key != "sample_rate_hz"
            && !pair.Key.Contains('\n') && !pair.Value.Contains('\n') && !pair.Value.Contains('\r'))
            .Select(pair => pair.Key + " " + pair.Value);
        var info = Encoding.UTF8.GetBytes("sample_rate_hz " + (1_000_000_000 / resolution) + "\n" + string.Join('\n', text) + "\0");
        using var chunk = new MemoryStream();
        chunk.Write(new byte[HxcStreamFormat.ChunkHeaderSize]);
        Block(chunk, HxcStreamFormat.MetadataBlock, info, null);
        Block(chunk, HxcStreamFormat.PackedIoBlock, io, null);
        if (pulses.Count > 0) Block(chunk, HxcStreamFormat.PackedStreamBlock, packedPulses.ToArray(), pulses.Count);
        var bytes = new byte[checked((int)chunk.Length + 4)]; chunk.ToArray().CopyTo(bytes, 0);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, HxcStreamFormat.ChunkSignature);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4), (uint)bytes.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(bytes.Length - 4), HxcStreamReader.ComputeCrc32(bytes.AsSpan(0, bytes.Length - 4), uint.MaxValue));
        if (bytes.Length > HxcStreamFormat.MaximumChunkSize) throw new InvalidDataException("HxC chunk is too large.");
        return bytes;
    }

    private static int ExactResolution(ProtectedTrack track)
    {
        foreach (var resolution in new[] { 40, 20 })
            if (track.Revolutions.All(revolution => (long)revolution.Flux.IndexTimeTicks * revolution.ResolutionNanoseconds % (resolution * 16) == 0
                && revolution.Flux.FluxIntervals.All(interval => (long)interval * revolution.ResolutionNanoseconds % resolution == 0))) return resolution;
        throw new InvalidDataException("HxC pulse or index sampling cannot preserve these source timings exactly.");
    }
    private static void WritePulse(Stream output, uint value)
    {
        if (value < 0x80) output.WriteByte((byte)value);
        else if (value < 0x4000) { output.WriteByte((byte)(0x80 | value >> 8)); output.WriteByte((byte)value); }
        else if (value < 0x200000) { output.WriteByte((byte)(0xc0 | value >> 16)); output.WriteByte((byte)(value >> 8)); output.WriteByte((byte)value); }
        else { output.WriteByte((byte)(0xe0 | value >> 24)); output.WriteByte((byte)(value >> 16)); output.WriteByte((byte)(value >> 8)); output.WriteByte((byte)value); }
    }
    private static void Block(Stream output, uint type, byte[] data, int? count)
    {
        var compressed = type == HxcStreamFormat.MetadataBlock ? data : new byte[LZ4Codec.MaximumOutputSize(data.Length)];
        var length = type == HxcStreamFormat.MetadataBlock ? data.Length : LZ4Codec.Encode(data, compressed);
        if (length <= 0) throw new InvalidDataException("Cannot encode HxC LZ4 block.");
        var extra = type == HxcStreamFormat.MetadataBlock ? 0 : count.HasValue ? 12 : 8;
        var header = new byte[8 + extra];
        BinaryPrimitives.WriteUInt32LittleEndian(header, type);
        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(4), checked((uint)(extra + length)));
        if (extra > 0) { BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(8), (uint)length); BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(12), (uint)data.Length); }
        if (count.HasValue) BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(16), (uint)count.Value);
        output.Write(header); output.Write(compressed, 0, length);
        while (output.Position % 4 != 0) output.WriteByte(0);
    }
}
