using System.IO;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Images.Models.Flux;
using C = RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Adaptec.AdaptecMfmFormat;

namespace RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Adaptec;

/// <summary>Projection des blocs et lecture des champs Adaptec ; aucune mécanique ni ressource de fichier.</summary>
internal static class AdaptecMfmTrackCodec
{
    private static readonly int[] CandidateSizes = [C.MinimumBlockSize, C.MediumBlockSize, C.MaximumBlockSize];
    internal static MediaFluxTrackData Encode(IReadOnlyList<MediaDiskSector> sectors, bool sequential, uint period,
        int sizeHint = 0, ReadOnlySpan<byte> parameterFlags = default)
    {
        ArgumentNullException.ThrowIfNull(sectors);
        if (sectors.Count is <= 0 or > C.MaximumSectors) throw new ArgumentOutOfRangeException(nameof(sectors));
        if (sizeHint != 0 && !ValidSize(sizeHint)) throw new ArgumentOutOfRangeException(nameof(sizeHint));
        if (!parameterFlags.IsEmpty)
        {
            if (parameterFlags.Length != C.ParameterFlagCount || sectors.Count < C.ParameterFlagCount)
                throw new ArgumentException(nameof(parameterFlags));
            foreach (var flag in parameterFlags)
                if (flag > C.DefectCountMask) throw new ArgumentException(nameof(parameterFlags));
        }
        var cells = checked((int)(period / C.CellNanoseconds));
        if (cells <= 0) throw new ArgumentOutOfRangeException(nameof(period));
        var bits = new List<bool>(cells);
        var maximumAddress = sectors.Max(sector => sector.Number);
        var previous = false;
        Span<byte> id = stackalloc byte[C.IdLength];
        Span<byte> data = stackalloc byte[C.MaximumBlockSize + C.DataOverhead];
        for (var index = 0; index < sectors.Count; index++)
        {
            var sector = sectors[index];
            if (sector.Data.IsEmpty && sizeHint != 0)
            {
                // An unavailable logical block has no invented readable ID or zero-filled payload.
                Gap(bits, C.GapByte, sizeHint + (index == 0 || !sequential
                    ? C.InterleavedRecordOverhead : C.SequentialRecordOverhead), ref previous);
                continue;
            }
            if ((uint)sector.Number > C.AddressMask || !ValidSize(sector.Data.Length))
                throw new ArgumentOutOfRangeException(nameof(sectors));
            if (sector.DeletedDataMark == true || sector.RecordedSizeCode is not null
                || sector.RecordedErrorCheckCode.Length is not (0 or C.CheckLength)) throw new NotSupportedException();
            Gap(bits, C.GapByte, index == 0 || !sequential ? C.FirstGapLength : C.SequentialGapLength, ref previous);
            Gap(bits, 0, C.IdPreambleLength, ref previous);
            id.Clear();
            id[0] = C.SyncByte;
            id[1] = C.IdMark;
            for (var part = 0; part < C.AddressLength; part++)
                id[C.AddressOffset + part] = (byte)(sector.Number >> ((C.AddressLength - 1 - part) * C.BitsPerByte));
            id[C.FlagsOffset] = sector.Number == maximumAddress ? C.LastSectorFlag : (byte)0;
            if (index < parameterFlags.Length) id[C.FlagsOffset] |= parameterFlags[index];
            AdaptecMfmErrorCheck.Write(id);
            if (sector.HeaderIntegrityValid == false) id[^1] ^= C.InvalidCheckMask;
            Field(bits, id, ref previous);
            Gap(bits, 0, C.DataPreambleLength, ref previous);
            var record = data[..(sector.Data.Length + C.DataOverhead)];
            record[0] = C.SyncByte;
            record[1] = C.DataMark;
            sector.Data.Span.CopyTo(record[C.DataOffset..]);
            AdaptecMfmErrorCheck.Write(record);
            if (sector.RecordedErrorCheckCode.Length == C.CheckLength)
                sector.RecordedErrorCheckCode.Span.CopyTo(record[^C.CheckLength..]);
            else if (sector.DataIntegrityValid == false || sector.IntegrityValid == false)
                record[^1] ^= C.InvalidCheckMask;
            Field(bits, record, ref previous);
            Gap(bits, 0, C.PostDataLength, ref previous);
        }
        if (bits.Count > cells) throw new NotSupportedException("The Adaptec fields exceed the supplied revolution.");
        while (bits.Count < cells) Gap(bits, C.GapByte, 1, ref previous);
        if (bits.Count > cells) bits.RemoveRange(cells, bits.Count - cells);
        var revolution = FluxRevolutionFactory.Create(bits, C.CellNanoseconds, period, includeTrailingInterval: false);
        return new([new(period, revolution.FluxIntervals)]);
    }

    /// <summary>Les ID restent internes au format ; les secteurs conservent données et contrôles effectivement lus.</summary>
    internal static IReadOnlyList<(ReadOnlyMemory<byte> Id, MediaDiskSector Sector)> Decode(
        MediaFluxRevolutionData revolution, int cylinder, int head, int sizeHint = 0)
        => DecodeCore(revolution, cylinder, head, sizeHint, null);

    private static IReadOnlyList<(ReadOnlyMemory<byte> Id, MediaDiskSector Sector)> DecodeCore(
        MediaFluxRevolutionData revolution, int cylinder, int head, int sizeHint,
        Dictionary<long, (int Offset, bool NextData, bool NextClock, bool Sync)>? locations)
    {
        ArgumentNullException.ThrowIfNull(revolution);
        if (sizeHint != 0 && !ValidSize(sizeHint)) throw new ArgumentOutOfRangeException(nameof(sizeHint));
        var period = checked((int)((revolution.IndexTimeNanoseconds + (ulong)C.CellNanoseconds - 1) / C.CellNanoseconds));
        var source = FluxTransitionDecoder.Reconstruct(revolution.FluxIntervalsNanoseconds, C.CellNanoseconds,
            period, adaptClock: false);
        var bits = new bool[period];
        source.Bits.AsSpan(0, Math.Min(period, source.Bits.Length)).CopyTo(bits);
        var marks = new List<(int Offset, byte Mark)>();
        ushort shift = 0;
        for (var offset = 0; offset < period + C.CellsPerByte - 1; offset++)
        {
            shift = (ushort)((shift << 1) | (bits[offset % period] ? 1 : 0));
            if (offset < C.CellsPerByte - 1 || shift != C.SyncWord) continue;
            var start = offset - C.CellsPerByte + 1;
            if (!HasSyncPreamble(bits, start)) continue;
            var mark = ReadByte(bits, start + C.CellsPerByte);
            marks.Add((start, mark));
        }
        var result = new List<(ReadOnlyMemory<byte> Id, MediaDiskSector Sector)>();
        for (var position = 0; position < marks.Count; position++)
        {
            var mark = marks[position];
            if (mark.Mark != C.IdMark) continue;
            var id = ReadField(bits, mark.Offset, C.IdLength);
            var validId = AdaptecMfmErrorCheck.Compute(id) == 0;
            uint address = 0;
            for (var part = 0; part < C.AddressLength; part++) address = (address << C.BitsPerByte) | id[C.AddressOffset + part];
            var nextId = mark.Offset + period;
            var dataOffset = -1;
            var blocked = false;
            for (var following = 1; following <= marks.Count; following++)
            {
                var candidate = marks[(position + following) % marks.Count];
                var candidateOffset = candidate.Offset + (position + following >= marks.Count ? period : 0);
                if (candidate.Mark == C.IdMark) { nextId = candidateOffset; break; }
                if (candidate.Mark != C.DataMark && dataOffset < 0) blocked = true;
                if (!blocked && dataOffset < 0 && candidate.Mark == C.DataMark
                    && candidateOffset >= mark.Offset + C.IdLength * C.CellsPerByte)
                    dataOffset = candidateOffset;
            }
            byte[] record = [];
            var validData = false;
            if (dataOffset >= 0)
            {
                foreach (var size in CandidateSizes)
                {
                    if (sizeHint != 0 && size != sizeHint) continue;
                    if (dataOffset + (size + C.DataOverhead) * C.CellsPerByte > nextId) continue;
                    var candidate = ReadField(bits, dataOffset, size + C.DataOverhead);
                    var valid = AdaptecMfmErrorCheck.Compute(candidate) == 0;
                    if (sizeHint != 0 || valid) { record = candidate; validData = valid; break; }
                }
            }
            var payload = record.Length == 0 ? ReadOnlyMemory<byte>.Empty : record.AsMemory(C.DataOffset, record.Length - C.DataOverhead);
            var defective = (address & C.DefectiveMask) != 0;
            var logical = (int)(address & C.AddressMask);
            if (locations is not null && validId && !defective && record.Length != 0)
            {
                var next = dataOffset + record.Length * C.CellsPerByte;
                ushort followingWord = 0;
                for (var bit = 0; bit < C.CellsPerByte; bit++)
                    followingWord = (ushort)((followingWord << 1) | (bits[(next + bit) % period] ? 1 : 0));
                var location = (dataOffset, bits[(next + 1) % period], bits[next % period], followingWord == C.SyncWord);
                if (!locations.TryAdd(logical, location)) locations[logical] = (-1, false, false, false);
            }
            result.Add((id, new(logical, payload, cylinder, head, logical, validId && validData && !defective)
            {
                HeaderIntegrityValid = validId,
                DataIntegrityValid = record.Length == 0 ? null : validData,
                RecordedDataPrefix = record.Length == 0 ? ReadOnlyMemory<byte>.Empty : record.AsMemory(0, C.DataOffset),
                RecordedErrorCheckCode = record.Length == 0 ? ReadOnlyMemory<byte>.Empty : record.AsMemory(record.Length - C.CheckLength)
            }));
        }
        return result;
    }

    internal static AdaptecMfmWriteWindow CreateSectorWrite(MediaFluxRevolutionData revolution,
        int cylinder, int head, long logicalAddress, ReadOnlySpan<byte> payload)
    {
        if (!ValidSize(payload.Length)) throw new ArgumentOutOfRangeException(nameof(payload));
        var locations = new Dictionary<long, (int Offset, bool NextData, bool NextClock, bool Sync)>();
        var fields = DecodeCore(revolution, cylinder, head, payload.Length, locations);
        if (!locations.TryGetValue(logicalAddress, out var location) || location.Offset < 0
            || fields.Count(field => field.Sector.Index == logicalAddress
                && field.Sector.HeaderIntegrityValid == true
                && (field.Id.Span[C.AddressOffset] & C.DefectiveAddressByteMask) == 0) != 1)
            throw new InvalidDataException();
        var record = new byte[payload.Length + C.DataOverhead];
        record[0] = C.SyncByte;
        record[1] = C.DataMark;
        payload.CopyTo(record.AsSpan(C.DataOffset));
        AdaptecMfmErrorCheck.Write(record);
        var bits = new List<bool>(record.Length * C.CellsPerByte + 1);
        var previous = false;
        Field(bits, record, ref previous);
        // The first clock after DATA depends on its new last data bit; a following missing-clock A1 stays intact.
        bits.Add(location.Sync ? location.NextClock : !previous && !location.NextData);
        var transitions = new List<uint>();
        for (var cell = 0; cell < bits.Count; cell++)
            if (bits[cell]) transitions.Add(checked((uint)cell * C.CellNanoseconds));
        // Reconstruct places the transition of bit n at (n+1) cells after INDEX.
        var start = (uint)(((ulong)location.Offset + 1) * C.CellNanoseconds % revolution.IndexTimeNanoseconds);
        return new(start, checked((uint)bits.Count * C.CellNanoseconds), transitions);
    }

    private static bool ValidSize(int size) => size is C.MinimumBlockSize or C.MediumBlockSize or C.MaximumBlockSize;

    private static bool HasSyncPreamble(bool[] bits, int markOffset)
    {
        // The address-mark detector is qualified by zero NRZ data, not by A1 alone.
        // Work backwards in clock/data pairs so a gap signature cannot hide the next field.
        for (var cell = 1; cell <= C.MinimumSyncBits * C.CellsPerBit; cell++)
        {
            var offset = (markOffset - cell + bits.Length) % bits.Length;
            if (bits[offset] != (cell % C.CellsPerBit == 0)) return false;
        }
        return true;
    }

    private static void Gap(List<bool> bits, byte value, int count, ref bool previous)
    {
        for (var index = 0; index < count; index++) Byte(bits, value, ref previous);
    }

    private static void Field(List<bool> bits, ReadOnlySpan<byte> record, ref bool previous)
    {
        for (var bit = C.CellsPerByte - 1; bit >= 0; bit--) bits.Add((C.SyncWord & (1 << bit)) != 0);
        previous = true;
        foreach (var value in record[1..]) Byte(bits, value, ref previous);
    }

    private static void Byte(List<bool> bits, byte value, ref bool previous)
    {
        for (var bit = C.BitsPerByte - 1; bit >= 0; bit--)
        {
            var data = (value & (1 << bit)) != 0;
            bits.Add(!previous && !data);
            bits.Add(data);
            previous = data;
        }
    }

    private static byte[] ReadField(bool[] bits, int offset, int length)
    {
        var bytes = new byte[length];
        for (var index = 0; index < length; index++) bytes[index] = ReadByte(bits, offset + index * C.CellsPerByte);
        return bytes;
    }

    private static byte ReadByte(bool[] bits, int offset)
    {
        byte value = 0;
        for (var bit = 0; bit < C.BitsPerByte; bit++)
            value = (byte)((value << 1) | (bits[(offset + bit * 2 + 1) % bits.Length] ? 1 : 0));
        return value;
    }
}
