using RetroMediaLab.MediaEngine.Images.Reading.Decoding.Definitions;
using RetroMediaLab.MediaEngine.Functions;
using RetroMediaLab.MediaEngine.Images.Models.Sectors;
using RetroMediaLab.MediaEngine.Images.Models.Flux;

namespace RetroMediaLab.MediaEngine.Images.Writing.Encoding;

/// <summary>Encode les pistes utilisant le format ISO FM.</summary>
public sealed class IsoFmTrackEncoder : TrackEncoderBase
{
    /// <summary>Obtient l'identifiant technique.</summary>
    public override string Id => IsoFmFormat.CodecId;
    /// <summary>Obtient le nom affiché.</summary>
    public override string DisplayName => IsoFmFormat.CodecDisplayName;

    /// <summary>Encode une piste ISO FM avec marques d'adresse, tailles sectorielles et CRC.</summary>
    /// <param name="request">Piste logique contenant cylindre, face et secteurs à encoder.</param>
    /// <returns>Cellules FM de la piste dans leur ordre d'émission.</returns>
    /// <exception cref="ArgumentOutOfRangeException">La taille d'un secteur ne correspond à aucun code ISO pris en charge.</exception>
    protected override IReadOnlyList<bool> EncodeBits(TrackEncodeRequest request)
    => Attribute(request, TrackEncodingAttributeKeys.FmPhysicalLayout, 0) == IsoFmFormat.PhysicalLayoutEnabled
        ? EncodePhysicalBits(request)
        : EncodeIsoBits(request, ValidateAddress, ResolveSizeCode, WriteAddress, WriteData);

    /// <inheritdoc/>
    protected override FluxRevolution EncodeRevolution(TrackEncodeRequest request, IReadOnlyList<bool> bits)
        => FluxRevolutionFactory.Create(bits, request.BitCellTicks, request.IndexTimeTicks,
            includeTrailingInterval: !CanEncodeEmptyTrack(request));

    /// <inheritdoc/>
    protected override bool CanEncodeEmptyTrack(TrackEncodeRequest request)
        => Attribute(request, TrackEncodingAttributeKeys.FmPhysicalLayout, 0) == IsoFmFormat.PhysicalLayoutEnabled;

    /// <summary>Synthétise une révolution physique ; les champs excédentaires sont tronqués à l'index.</summary>
    private static IReadOnlyList<bool> EncodePhysicalBits(TrackEncodeRequest request)
    {
        var bits = TrackBitEncoding.Bits();
        var capacity = checked((int)(request.IndexTimeTicks / request.BitCellTicks));
        WriteRun(bits, IsoFmFormat.GapFill, IsoFmFormat.InitialGapByteCount);
        foreach (var sector in request.Sectors)
        {
            ValidateAddress(nameof(sector.Number), sector.Number);
            var sizeCode = ResolveSizeCode(sector);
            WriteRun(bits, IsoFmFormat.SyncFill, IsoFmFormat.SyncByteCount);
            WriteAddressField(bits, request, sector, sizeCode, 0);
            WriteRun(bits, IsoFmFormat.GapFill, IsoFmFormat.InterFieldGapByteCount);
            WriteRun(bits, IsoFmFormat.SyncFill, IsoFmFormat.SyncByteCount);
            WriteDataField(bits, sector, 0);
            WriteRun(bits, IsoFmFormat.GapFill, IsoFmFormat.InterSectorGapByteCount);
            if (bits.Count >= capacity) break;
        }
        var remaining = Math.Max(0, capacity - bits.Count);
        WriteRun(bits, IsoFmFormat.GapFill,
            (remaining + IsoFmFormat.EncodedByteBitCount - 1) / IsoFmFormat.EncodedByteBitCount);
        if (bits.Count > capacity) bits.RemoveRange(capacity, bits.Count - capacity);
        return bits;
    }

    private static void WriteRun(List<bool> bits, byte value, int count)
        => bits.Fm(Enumerable.Repeat(value, count));

    /// <summary>Calcule ou valide le code de taille associé aux données du secteur.</summary>
    private static byte ResolveSizeCode(TrackSector sector)
    {
        if (sector.RecordedSizeCode is { } recorded) return recorded;
        var sizeCode = sector.SizeCode ?? SectorSizeCode.FromByteCount(sector.Data.Count);
        if (IsoFmFormat.SectorSize(sizeCode) != sector.Data.Count) throw IsoFmFormat.InvalidSizeCode(sizeCode, sector.Data.Count);
        return sizeCode;
    }

    /// <summary>Écrit la marque spéciale, les champs CHRN, leur CRC fort puis faible et le gap d'adresse.</summary>
    private static void WriteAddress(List<bool> bits, TrackEncodeRequest request, TrackSector sector, byte sizeCode)
        => WriteAddressField(bits, request, sector, sizeCode, IsoFmFormat.HeaderGapBitCount);

    private static void WriteAddressField(List<bool> bits, TrackEncodeRequest request, TrackSector sector,
        byte sizeCode, int gapBitCount)
    {
        var cylinder = sector.RecordedCylinder ?? request.Cylinder;
        var head = sector.RecordedHead ?? request.Head;
        ValidateAddress(nameof(sector.RecordedCylinder), cylinder);
        ValidateAddress(nameof(sector.RecordedHead), head);
        byte[] header = [IsoFmFormat.IdAddressMark, (byte)cylinder, (byte)head, (byte)sector.Number, sizeCode];
        var crc = Crc16Calculator.Compute(header, IsoFmFormat.CrcPolynomial, IsoFmFormat.CrcInitialValue);
        if (sector.HeaderIntegrityValid == false) crc ^= 1;
        WriteField(bits, IsoFmFormat.EncodedIdAddressMark, header.Skip(1), crc, gapBitCount);
    }

    /// <summary>Écrit la marque normale ou supprimée, les données, leur CRC et le gap final.</summary>
    private static void WriteData(List<bool> bits, TrackSector sector)
        => WriteDataField(bits, sector, IsoFmFormat.DataGapBitCount);

    private static void WriteDataField(List<bool> bits, TrackSector sector, int gapBitCount)
    {
        var mark = IsoFmFormat.DataMark(sector.Deleted);
        var crc = sector.RecordedErrorCheckCode.Length == IsoFmFormat.CrcByteCount
            ? (ushort)((sector.RecordedErrorCheckCode.Span[0] << BitPrimitives.BitsPerByte) | sector.RecordedErrorCheckCode.Span[1])
            : Crc16Calculator.Compute(new[] { mark.Mark }.Concat(sector.Data), IsoFmFormat.CrcPolynomial, IsoFmFormat.CrcInitialValue);
        if (sector.RecordedErrorCheckCode.Length != IsoFmFormat.CrcByteCount && sector.DataIntegrityValid == false) crc ^= 1;
        WriteField(bits, mark.Pattern, sector.Data, crc, gapBitCount);
    }

    /// <summary>Écrit un motif à horloge spéciale, son contenu, le CRC fort puis faible et son gap.</summary>
    private static void WriteField(List<bool> bits, ushort pattern, IEnumerable<byte> values, ushort crc, int gapBitCount)
    {
        AddMark(bits, pattern);
        bits.Fm(values.Concat([(byte)(crc >> BitPrimitives.BitsPerByte), (byte)crc]));
        bits.Gap(gapBitCount);
    }

    /// <summary>Valide un champ CHRN avant sa conversion en octet.</summary>
    private static void ValidateAddress(string field, int value)
    {
        if (value is < 0 || value > IsoFmFormat.MaximumAddressValue) throw TrackEncodingExceptions.FormatValueOutOfRange("ISO FM", field, value, IsoFmFormat.MaximumAddressValue);
    }

    /// <summary>Ajoute une marque binaire au flux.</summary>
    private static void AddMark(List<bool> bits, ushort mark) => bits.Raw((byte)(mark >> BitPrimitives.BitsPerByte), (byte)mark);
}
