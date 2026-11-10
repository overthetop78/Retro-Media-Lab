using RetroMediaLab.MediaEngine.Images.Reading.Decoding.Definitions;
using RetroMediaLab.MediaEngine.Functions;
using RetroMediaLab.MediaEngine.Images.Models.Sectors;
using RetroMediaLab.MediaEngine.Images.Models.Flux;

namespace RetroMediaLab.MediaEngine.Images.Writing.Encoding;

/// <summary>Encode les pistes utilisant le format ISO MFM.</summary>
public sealed class IsoMfmTrackEncoder : TrackEncoderBase
{
    /// <summary>Obtient l'identifiant technique.</summary>
    public override string Id => IsoMfmFormat.CodecId;
    /// <summary>Obtient le nom affiché.</summary>
    public override string DisplayName => IsoMfmFormat.CodecDisplayName;

    /// <summary>Encode une piste ISO MFM avec marques de synchronisation, tailles sectorielles et CRC.</summary>
    /// <param name="request">Piste logique contenant cylindre, face et secteurs à encoder.</param>
    /// <returns>Cellules MFM de la piste dans leur ordre d'émission.</returns>
    /// <exception cref="ArgumentOutOfRangeException">La taille d'un secteur ne correspond à aucun code ISO pris en charge.</exception>
    protected override IReadOnlyList<bool> EncodeBits(TrackEncodeRequest request)
    => Physical(request) ? EncodePhysicalBits(request)
        : EncodeIsoBits(request, ValidateAddress, ResolveSizeCode, WriteAddress, WriteData);

    protected override bool CanEncodeEmptyTrack(TrackEncodeRequest request) => Physical(request);
    protected override FluxRevolution EncodeRevolution(TrackEncodeRequest request, IReadOnlyList<bool> bits)
        => FluxRevolutionFactory.Create(bits, request.BitCellTicks, request.IndexTimeTicks,
            includeTrailingInterval: !Physical(request));
    private static bool Physical(TrackEncodeRequest request)
        => Attribute(request, TrackEncodingAttributeKeys.MfmPhysicalLayout, 0) == IsoMfmFormat.PhysicalLayoutEnabled;

    private static IReadOnlyList<bool> EncodePhysicalBits(TrackEncodeRequest request)
    {
        var bits = TrackBitEncoding.Bits();
        var capacity = checked((int)(request.IndexTimeTicks / request.BitCellTicks));
        var requireComplete = Attribute(request, TrackEncodingAttributeKeys.RequireCompletePhysicalTrack, 0) != 0;
        var completed = 0;
        WriteRun(bits, IsoMfmFormat.GapFill, IsoMfmFormat.InitialGapByteCount);
        foreach (var sector in request.Sectors)
        {
            ValidateAddress(nameof(sector.Number), sector.Number);
            var sizeCode = ResolveSizeCode(sector);
            WriteRun(bits, IsoMfmFormat.SyncFill, IsoMfmFormat.SyncZeroByteCount);
            WriteAddressField(bits, request, sector, sizeCode, 0);
            WriteRun(bits, IsoMfmFormat.GapFill, IsoMfmFormat.InterFieldGapByteCount);
            WriteRun(bits, IsoMfmFormat.SyncFill, IsoMfmFormat.SyncZeroByteCount);
            WriteDataField(bits, sector, 0);
            completed++;
            if (requireComplete && bits.Count > capacity)
                throw TrackEncodingExceptions.PhysicalTrackCapacityExceeded(IsoMfmFormat.CodecId, request.Cylinder, request.Head, capacity);
            WriteRun(bits, IsoMfmFormat.GapFill, IsoMfmFormat.InterSectorGapByteCount);
            if (bits.Count >= capacity)
            {
                if (requireComplete && completed < request.Sectors.Count)
                    throw TrackEncodingExceptions.PhysicalTrackCapacityExceeded(IsoMfmFormat.CodecId, request.Cylinder, request.Head, capacity);
                break;
            }
        }
        var remaining = Math.Max(0, capacity - bits.Count);
        WriteRun(bits, IsoMfmFormat.GapFill,
            (remaining + IsoMfmFormat.EncodedByteBitCount - 1) / IsoMfmFormat.EncodedByteBitCount);
        if (bits.Count > capacity) bits.RemoveRange(capacity, bits.Count - capacity);
        return bits;
    }
    private static void WriteRun(List<bool> bits, byte value, int count)
        => bits.Mfm(Enumerable.Repeat(value, count), bits.Count != 0 && bits[^1]);

    /// <summary>Calcule ou valide le code de taille sectorielle.</summary>
    private static byte ResolveSizeCode(TrackSector sector)
    {
        if (sector.RecordedSizeCode is { } recorded) return recorded;
        var sizeCode = sector.SizeCode ?? SectorSizeCode.FromByteCount(sector.Data.Count);
        if (IsoMfmFormat.SectorSize(sizeCode) != sector.Data.Count) throw IsoMfmFormat.InvalidSizeCode(sizeCode, sector.Data.Count);
        return sizeCode;
    }

    /// <summary>Écrit les synchronisations spéciales, l'adresse CHRN, son CRC et le gap.</summary>
    private static void WriteAddress(List<bool> bits, TrackEncodeRequest request, TrackSector sector, byte sizeCode)
        => WriteAddressField(bits, request, sector, sizeCode, IsoMfmFormat.HeaderGapBitCount);

    private static void WriteAddressField(List<bool> bits, TrackEncodeRequest request, TrackSector sector, byte sizeCode, int gap)
    {
        var cylinder = sector.RecordedCylinder ?? request.Cylinder;
        var head = sector.RecordedHead ?? request.Head;
        ValidateAddress(nameof(sector.RecordedCylinder), cylinder);
        ValidateAddress(nameof(sector.RecordedHead), head);
        byte[] values = [IsoMfmFormat.SyncByte, IsoMfmFormat.SyncByte, IsoMfmFormat.SyncByte,
            IsoMfmFormat.IdAddressMark, (byte)cylinder, (byte)head, (byte)sector.Number, sizeCode];
        var crc = Crc16Calculator.Compute(values, IsoMfmFormat.CrcPolynomial, IsoMfmFormat.CrcInitialValue);
        if (sector.HeaderIntegrityValid == false) crc ^= 1;
        WriteField(bits, values.Skip(IsoMfmFormat.SyncByteCount), crc, gap);
    }

    private static void WriteData(List<bool> bits, TrackSector sector)
        => WriteDataField(bits, sector, IsoMfmFormat.DataGapBitCount);

    private static void WriteDataField(List<bool> bits, TrackSector sector, int gap)
    {
        var mark = IsoMfmFormat.DataMark(sector.Deleted);
        var values = Enumerable.Repeat(IsoMfmFormat.SyncByte, IsoMfmFormat.SyncByteCount).Append(mark).Concat(sector.Data).ToArray();
        var crc = sector.RecordedErrorCheckCode.Length == IsoMfmFormat.CrcByteCount
            ? (ushort)((sector.RecordedErrorCheckCode.Span[0] << BitPrimitives.BitsPerByte) | sector.RecordedErrorCheckCode.Span[1])
            : Crc16Calculator.Compute(values, IsoMfmFormat.CrcPolynomial, IsoMfmFormat.CrcInitialValue);
        if (sector.RecordedErrorCheckCode.Length != IsoMfmFormat.CrcByteCount && sector.DataIntegrityValid == false) crc ^= 1;
        WriteField(bits, values.Skip(IsoMfmFormat.SyncByteCount), crc, gap);
    }

    /// <summary>Écrit une synchronisation spéciale unique, son contenu MFM, le CRC fort puis faible et le gap.</summary>
    private static void WriteField(List<bool> bits, IEnumerable<byte> values, ushort crc, int gapBitCount)
    {
        bits.Raw(IsoMfmFormat.EncodedSync.ToArray());
        bits.Mfm(values.Concat([(byte)(crc >> BitPrimitives.BitsPerByte), (byte)crc]));
        bits.Gap(gapBitCount);
    }

    /// <summary>Valide un champ CHRN avant sa conversion en octet.</summary>
    private static void ValidateAddress(string field, int value)
    {
        if (value is < 0 || value > IsoMfmFormat.MaximumAddressValue) throw TrackEncodingExceptions.FormatValueOutOfRange("ISO MFM", field, value, IsoMfmFormat.MaximumAddressValue);
    }
}
