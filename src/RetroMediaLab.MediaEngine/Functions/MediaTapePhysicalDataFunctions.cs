using System.Buffers.Binary;
using System.Globalization;
using System.IO;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;

namespace RetroMediaLab.MediaEngine.Functions;

/// <summary>Transfers the existing mono PCM tape signal through neutral physical-media units.</summary>
/// <remarks>No resampling, amplitude adjustment, protocol decoding or device access occurs here.
/// A unit's position is its first sample offset, not its time in rounded milliseconds.</remarks>
public static class MediaTapePhysicalDataFunctions
{
    public static MediaPhysicalDataUnit CreateUnit(MediaTapeSignal signal, long firstSample = 0)
    {
        ArgumentNullException.ThrowIfNull(signal);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(signal.SampleRate);
        ArgumentOutOfRangeException.ThrowIfNegative(firstSample);
        if (signal.Samples.IsEmpty) throw new InvalidDataException("The tape signal contains no sample.");
        var bytes = new byte[checked(signal.Samples.Length * sizeof(short))];
        for (var index = 0; index < signal.Samples.Length; index++)
            BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(index * sizeof(short)), signal.Samples.Span[index]);
        return new(firstSample, bytes, new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [MediaPhysicalMetadataKeys.Encoding] = MediaPhysicalEncodingIds.TapePcm16Mono,
            [MediaPhysicalMetadataKeys.SampleRate] = signal.SampleRate.ToString(CultureInfo.InvariantCulture)
        });
    }

    /// <summary>Reassembles ordered, contiguous capture fragments without filling missing samples.</summary>
    public static MediaTapeSignal ReadSignal(IReadOnlyList<MediaPhysicalDataUnit> units)
    {
        ArgumentNullException.ThrowIfNull(units);
        if (units.Count == 0) throw new InvalidDataException("The tape acquisition contains no PCM unit.");
        long count = 0;
        var rate = 0;
        foreach (var unit in units)
        {
            if (unit is null || unit.Position != count)
                throw new InvalidDataException("Tape PCM units must be ordered and contiguous from sample zero.");
            if (!unit.Metadata.TryGetValue(MediaPhysicalMetadataKeys.Encoding, out var encoding)
                || encoding != MediaPhysicalEncodingIds.TapePcm16Mono)
                throw new InvalidDataException("The tape PCM unit encoding is unsupported.");
            if (!unit.Metadata.TryGetValue(MediaPhysicalMetadataKeys.SampleRate, out var text)
                || !int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var unitRate) || unitRate <= 0)
                throw new InvalidDataException("The tape PCM sample rate is missing or invalid.");
            if (rate != 0 && rate != unitRate)
                throw new InvalidDataException("Tape PCM units have different sample rates.");
            if (unit.Data.IsEmpty || unit.Data.Length % sizeof(short) != 0)
                throw new InvalidDataException("The tape PCM unit is empty or ends inside a sample.");
            rate = unitRate;
            count = checked(count + unit.Data.Length / sizeof(short));
            if (count > int.MaxValue) throw new InvalidDataException("The tape signal is too large for an in-memory signal.");
        }
        var samples = new short[(int)count];
        var offset = 0;
        foreach (var unit in units)
        {
            for (var index = 0; index < unit.Data.Length; index += sizeof(short))
                samples[offset++] = BinaryPrimitives.ReadInt16LittleEndian(unit.Data.Span[index..]);
        }
        return new(samples, rate);
    }
}
