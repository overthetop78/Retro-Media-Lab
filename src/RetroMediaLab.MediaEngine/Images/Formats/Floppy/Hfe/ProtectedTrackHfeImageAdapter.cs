using RetroMediaLab.MediaFileSystems.Functions;
using RetroMediaLab.MediaEngine.Images.Conversion.Flux;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Scp;
using RetroMediaLab.MediaEngine.Images.Models.Flux;

namespace RetroMediaLab.MediaEngine.Images.Formats.Floppy.Hfe;

/// <summary>Builds an HFE v1 container model from the common protected-track representation.</summary>
internal static class ProtectedTrackHfeImageAdapter
{
    internal static bool CanRepresentExactly(ProtectedTrackImage image)
    {
        try
        {
            Create(image, new Dictionary<string, string> { [HfeMetadataKeys.Encoding] = "0" });
            return true;
        }
        catch (Exception exception) when (exception is InvalidDataException or NotSupportedException or OverflowException)
        {
            try
            {
                HfeV3ImageBuilder.Build(image, new Dictionary<string, string> { [HfeMetadataKeys.Encoding] = "0" });
                return true;
            }
            catch (Exception error) when (error is InvalidDataException or NotSupportedException or OverflowException) { return false; }
        }
    }

    public static HfeImage Create(ProtectedTrackImage image, IReadOnlyDictionary<string, string> metadata)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(metadata);
        var tracks = image.Tracks.Select(ToHfeTrack).ToArray();
        var commonCellTicks = FluxBitCellConverter.GreatestCommonDivisor(tracks.Select(track => track.BitCellTicks));
        tracks = tracks.Select(track => track.BitCellTicks == commonCellTicks ? track : new HfeTrack(track.Cylinder, track.Head,
            FluxBitCellConverter.ToBits(FluxBitCellConverter.ToIntervals(track.Bits, track.BitCellTicks),
                checked((uint)(track.Bits.Count * (long)track.BitCellTicks)), commonCellTicks), commonCellTicks)).ToArray();
        ValidateTrackLayout(tracks);
        var bitRates = tracks.Select(track => CalculateBitRate(track.BitCellTicks)).Distinct().ToArray();
        if (bitRates.Length != 1)
            throw new NotSupportedException("HFE v1 requires one bit rate for every track.");
        var bitRate = bitRates[0];
        return new HfeImage(ReadByte(metadata, HfeMetadataKeys.Revision) ?? HfeFormat.Revision, tracks.Max(track => track.Cylinder) + 1, tracks.Max(track => track.Head) + 1, ResolveEncoding(metadata), bitRate, tracks)
        {
            WriteProtected = image.WriteProtected
        };
    }

    private static HfeTrack ToHfeTrack(ProtectedTrack track)
    {
        if (track.Bits is { Count: > 0 })
            return new HfeTrack(track.Cylinder, track.Head, track.Bits, ResolveBitCellTicks(track));
        if (track.Revolutions.Count != 1)
            throw new NotSupportedException("HFE v1 cannot preserve multiple separate flux revolutions.");

        var revolution = track.Revolutions[0];
        uint ToTicks(uint value)
        {
            var nanoseconds = checked((ulong)value * (uint)revolution.ResolutionNanoseconds);
            if (nanoseconds % HfeFormat.TickNanoseconds != 0)
                throw new NotSupportedException("The flux timing cannot be expressed in HFE ticks.");
            return checked((uint)(nanoseconds / HfeFormat.TickNanoseconds));
        }
        var values = revolution.Flux.FluxIntervals.Append(revolution.Flux.IndexTimeTicks).Select(ToTicks);
        var bitCellTicks = FluxBitCellConverter.GreatestCommonDivisor(values);
        if (bitCellTicks == 0) throw new InvalidDataException("The flux track does not contain representable timing.");
        var intervals = revolution.Flux.FluxIntervals.Select(ToTicks).ToArray();
        var indexTime = ToTicks(revolution.Flux.IndexTimeTicks);
        if ((long)indexTime / bitCellTicks > ushort.MaxValue / HfeFormat.MaximumHeadCount * HfeFormat.BitsPerByte)
            throw new NotSupportedException("The exact flux timing exceeds the HFE v1 track length limit.");
        var bits = FluxBitCellConverter.ToBits(intervals, indexTime, bitCellTicks);
        return new HfeTrack(track.Cylinder, track.Head, bits, bitCellTicks);
    }

    private static uint ResolveBitCellTicks(ProtectedTrack track)
    {
        if (track.Timing.Count != 1 ||
            track.Timing[0].BitOffset != 0 ||
            track.Timing[0].BitLength != track.Bits!.Count)
            throw new NotSupportedException("HFE v1 requires one uniform timing segment for each track.");
        var nanoseconds = track.Timing[0].BitCellNanoseconds;
        var ticks = nanoseconds / HfeFormat.TickNanoseconds;
        if (ticks < 1 || ticks > uint.MaxValue || ticks != Math.Truncate(ticks))
            throw new NotSupportedException("The track bit-cell timing cannot be expressed in HFE ticks.");
        return checked((uint)ticks);
    }

    private static void ValidateTrackLayout(IReadOnlyList<HfeTrack> tracks)
    {
        if (tracks.Count == 0) throw new InvalidDataException("HFE requires at least one track.");
        if (tracks.Any(track => track.Head >= HfeFormat.MaximumHeadCount))
            throw new NotSupportedException("HFE v1 supports at most two heads.");
        if (tracks.Any(track => track.Bits.Count % HfeFormat.BitsPerByte != 0))
            throw new NotSupportedException("HFE v1 would require padding at the end of a flux track.");
        for (var cylinder = 0; cylinder <= tracks.Max(track => track.Cylinder); cylinder++)
        {
            var sides = tracks.Where(track => track.Cylinder == cylinder).ToArray();
            if (sides.Length == 0) throw new NotSupportedException($"HFE v1 requires cylinder '{cylinder}'.");
            if (sides.Select(track => track.Bits.Count).Distinct().Count() != 1)
                throw new NotSupportedException("HFE v1 would require different padding between heads.");
        }
    }

    internal static byte ResolveEncoding(IReadOnlyDictionary<string, string> metadata)
    {
        var hfeEncoding = ReadByte(metadata, HfeMetadataKeys.Encoding);
        if (hfeEncoding.HasValue) return hfeEncoding.Value;
        var scpDiskType = ReadByte(metadata, ScpMetadataKeys.DiskType);
        if (scpDiskType.HasValue) return ScpHfeEncodingResolver.Resolve(scpDiskType.Value);
        return HfeFormat.UnknownEncoding;
    }

    private static ushort CalculateBitRate(uint bitCellTicks)
    {
        if (bitCellTicks == 0) throw new InvalidDataException("The HFE bit-cell duration cannot be zero.");
        var value = HfeFormat.NanosecondsPerSecond / (HfeFormat.BitsPerDataBit * HfeFormat.TickNanoseconds * 1000L * bitCellTicks);
        if (value is < 1 or > ushort.MaxValue)
            throw new NotSupportedException($"The calculated HFE bit rate '{value}' is invalid.");
        return checked((ushort)value);
    }

    private static byte? ReadByte(IReadOnlyDictionary<string, string> metadata, string key) => MediaMetadataFunctions.TryReadByte(metadata, key, out var value) ? value : null;

}
