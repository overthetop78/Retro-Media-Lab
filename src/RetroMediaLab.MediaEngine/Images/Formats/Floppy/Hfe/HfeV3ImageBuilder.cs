using System.Buffers.Binary;
using System.IO;
using RetroMediaLab.MediaEngine.Images.Conversion.Flux;
using RetroMediaLab.MediaEngine.Images.Models.Flux;

namespace RetroMediaLab.MediaEngine.Images.Formats.Floppy.Hfe;

/// <summary>Conserve les impulsions avec les débits et cellules partielles HFE v3.</summary>
internal static class HfeV3ImageBuilder
{
    private const int MaximumSideLength = ushort.MaxValue / HfeFormat.MaximumHeadCount;
    private const byte SevenBitsMask = 0x7f;

    internal static byte[] Build(ProtectedTrackImage image, IReadOnlyDictionary<string, string> metadata)
    {
        var cylinders = image.Tracks.Max(track => track.Cylinder) + 1;
        var heads = image.Tracks.Max(track => track.Head) + 1;
        if (cylinders > byte.MaxValue || heads > HfeFormat.MaximumHeadCount) throw new NotSupportedException();
        var encoded = image.Tracks.ToDictionary(track => (track.Cylinder, track.Head), Encode);
        var tableBlocks = (cylinders * HfeLayout.TrackListEntrySize + HfeLayout.BlockSize - 1) / HfeLayout.BlockSize;
        var table = new byte[tableBlocks * HfeLayout.BlockSize];
        var body = new List<byte>();
        var block = tableBlocks + 1;
        for (var cylinder = 0; cylinder < cylinders; cylinder++)
        {
            var sides = new byte[heads][];
            for (var head = 0; head < heads; head++)
                sides[head] = encoded.TryGetValue((cylinder, head), out var data) ? data : throw new NotSupportedException();
            var sideLength = sides.Max(side => side.Length);
            var entry = table.AsSpan(cylinder * HfeLayout.TrackListEntrySize, HfeLayout.TrackListEntrySize);
            BinaryPrimitives.WriteUInt16LittleEndian(entry[HfeLayout.TrackOffsetOffset..], checked((ushort)block));
            BinaryPrimitives.WriteUInt16LittleEndian(entry[HfeLayout.TrackLengthOffset..], checked((ushort)(sideLength * HfeFormat.MaximumHeadCount)));
            HfeWriter.AppendTrackData(body, sides, sideLength, HfeFormat.Version3NopOpcode);
            block += (sideLength + HfeLayout.SideChunkSize - 1) / HfeLayout.SideChunkSize;
        }
        var header = HfeWriter.BuildHeader(new(HfeFormat.Version3Revision, cylinders, heads,
            ProtectedTrackHfeImageAdapter.ResolveEncoding(metadata), HfeFormat.Version3NominalBitRate, []));
        HfeFormat.Version3Signature.CopyTo(header);
        header[HfeLayout.WriteAllowedOffset] = image.WriteProtected ? byte.MinValue : HfeFormat.WriteAllowed;
        return [.. header, .. table, .. body];
    }

    private static byte[] Encode(ProtectedTrack track)
    {
        if (track.Features.Any(feature => feature.Kind == TrackFeatureKind.WeakRegion)) throw new NotSupportedException();
        var data = ProtectedTrackFluxAdapter.Read(track);
        if (data.Revolutions.Count != 1) throw new NotSupportedException();
        var revolution = data.Revolutions[0];
        var output = new List<byte> { HfeFormat.Version3IndexOpcode };
        var buffer = 0;
        var count = 0;
        Rate(HfeFormat.Version3NominalDivider);
        ulong position = 0;
        foreach (var interval in revolution.FluxIntervalsNanoseconds)
        {
            position += interval;
            if (position > revolution.IndexTimeNanoseconds) throw new InvalidDataException();
            Interval(interval, true);
        }
        var tail = revolution.IndexTimeNanoseconds - position;
        if (tail != 0) Interval(checked((uint)tail), false);
        Flush();
        return output.ToArray();

        void Append(byte value)
        {
            if (output.Count >= MaximumSideLength) throw new NotSupportedException();
            output.Add(value);
        }
        void Rate(byte divider)
        {
            Append(HfeFormat.Version3BitRateOpcode);
            Append(Reverse(divider));
        }
        void Bit(bool transition)
        {
            if (transition) buffer |= 1 << count;
            if (++count != HfeFormat.BitsPerByte) return;
            if ((buffer & HfeFormat.Version3OpcodeMask) == HfeFormat.Version3OpcodeMask)
            {
                // Échapper sept cellules et conserver la huitième pour l'octet suivant.
                Append(HfeFormat.Version3SkipBitsOpcode);
                Append(Reverse(1));
                Append((byte)((buffer & SevenBitsMask) << 1));
                buffer >>= HfeFormat.BitsPerByte - 1;
                count = 1;
            }
            else { Append((byte)buffer); buffer = 0; count = 0; }
        }
        void Flush()
        {
            if (count == 0) return;
            var skip = HfeFormat.BitsPerByte - count;
            Append(HfeFormat.Version3SkipBitsOpcode);
            Append(Reverse((byte)skip));
            Append((byte)(buffer << skip));
            buffer = 0;
            count = 0;
        }
        void Interval(uint nanoseconds, bool transition)
        {
            var clocks = (ulong)nanoseconds * HfeFormat.Version3ClockMegahertz;
            if (clocks % HfeFormat.NanosecondsPerMicrosecond != 0) throw new NotSupportedException();
            clocks /= HfeFormat.NanosecondsPerMicrosecond;
            var cells = clocks / HfeFormat.Version3NominalDivider;
            var remainder = (byte)(clocks % HfeFormat.Version3NominalDivider);
            for (ulong cell = 0; cell < cells; cell++) Bit(transition && remainder == 0 && cell + 1 == cells);
            if (remainder == 0) return;
            Flush(); Rate(remainder); Bit(transition); Flush(); Rate(HfeFormat.Version3NominalDivider);
        }
    }

    private static byte Reverse(byte value)
    {
        var result = 0;
        for (var bit = 0; bit < HfeFormat.BitsPerByte; bit++)
            result |= ((value >> bit) & 1) << (HfeFormat.BitsPerByte - 1 - bit);
        return (byte)result;
    }
}
