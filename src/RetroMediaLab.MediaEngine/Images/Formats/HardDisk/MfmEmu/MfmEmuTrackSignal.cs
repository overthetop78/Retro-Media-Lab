using System.Buffers.Binary;
using RetroMediaLab.MediaEngine.Contracts;
using C = RetroMediaLab.MediaEngine.Images.Formats.HardDisk.MfmEmu.MfmEmuFormat;

namespace RetroMediaLab.MediaEngine.Images.Formats.HardDisk.MfmEmu;

/// <summary>Projection des cellules EMU sur le temps physique, sans décodage des champs de secteurs.</summary>
internal static class MfmEmuTrackSignal
{
    internal static MediaFluxTrackData Read(ReadOnlySpan<byte> bytes, uint rate, uint captureStart, uint period)
    {
        var count = CellCount(bytes.Length, rate, period);
        var transitions = new List<uint>();
        var start = captureStart % period;
        for (long cell = 0; cell < count; cell++)
        {
            if (!Get(bytes, cell)) continue;
            var phase = (uint)((start + CellTime(cell, rate)) % period);
            transitions.Add(phase == 0 ? period : phase);
        }
        transitions.Sort();
        var intervals = new uint[transitions.Count];
        uint previous = 0;
        for (var index = 0; index < intervals.Length; index++)
        {
            intervals[index] = transitions[index] - previous;
            previous = transitions[index];
        }
        return new([new(period, intervals)]);
    }

    internal static byte[] WriteWindow(ReadOnlySpan<byte> bytes, uint rate, uint captureStart, uint period,
        uint start, uint duration, IReadOnlyList<uint> offsets, bool includeEndingInstant)
    {
        ArgumentNullException.ThrowIfNull(offsets);
        var count = CellCount(bytes.Length, rate, period);
        uint previous = 0;
        for (var index = 0; index < offsets.Count; index++)
        {
            var offset = offsets[index];
            if (offset > duration || !includeEndingInstant && offset == duration || index > 0 && offset <= previous)
                throw new ArgumentOutOfRangeException(nameof(offsets));
            previous = offset;
        }
        var result = bytes.ToArray();
        if (duration == 0) return result;
        start %= period;
        captureStart %= period;
        for (long cell = 0; cell < count; cell++)
        {
            var phase = (captureStart + CellTime(cell, rate)) % period;
            var distance = (phase + period - start) % period;
            if (distance < duration || includeEndingInstant && distance == duration) Set(result, cell, false);
        }
        foreach (var offset in offsets)
        {
            var nextVisit = (ulong)offset + period;
            if (nextVisit < duration || includeEndingInstant && nextVisit == duration) continue;
            var phase = ((ulong)start + offset) % period;
            var storedTime = (phase + period - captureStart) % period;
            // Une impulsion reçue entre cellules est échantillonnée à la cellule suivante.
            var cell = (long)((storedTime * rate + C.NanosecondsPerSecond - 1) / C.NanosecondsPerSecond);
            var fullCount = ((ulong)period * rate + C.NanosecondsPerSecond - 1) / C.NanosecondsPerSecond;
            if ((ulong)cell == fullCount) cell = 0;
            if (cell >= count) throw new NotSupportedException(C.UncapturedWrite);
            Set(result, cell, true);
        }
        return result;
    }

    private static long CellCount(int byteCount, uint rate, uint period)
    {
        ArgumentOutOfRangeException.ThrowIfZero(rate);
        ArgumentOutOfRangeException.ThrowIfZero(period);
        if (rate > C.NanosecondsPerSecond) throw new NotSupportedException(C.UnsupportedSignalRate);
        if (byteCount == 0 || byteCount % C.WordLength != 0) throw new ArgumentException(nameof(byteCount));
        return Math.Min((long)byteCount * C.BitsPerByte,
            (long)(((ulong)period * rate + C.NanosecondsPerSecond - 1) / C.NanosecondsPerSecond));
    }

    private static ulong CellTime(long cell, uint rate) => (ulong)cell * C.NanosecondsPerSecond / rate;
    private static bool Get(ReadOnlySpan<byte> bytes, long cell) =>
        (BinaryPrimitives.ReadUInt32LittleEndian(bytes[(int)(cell / C.BitsPerWord * C.WordLength)..])
            & C.FirstWordBit >> (int)(cell % C.BitsPerWord)) != 0;

    private static void Set(Span<byte> bytes, long cell, bool value)
    {
        var offset = (int)(cell / C.BitsPerWord * C.WordLength);
        var word = BinaryPrimitives.ReadUInt32LittleEndian(bytes[offset..]);
        var mask = C.FirstWordBit >> (int)(cell % C.BitsPerWord);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes[offset..], value ? word | mask : word & ~mask);
    }
}
