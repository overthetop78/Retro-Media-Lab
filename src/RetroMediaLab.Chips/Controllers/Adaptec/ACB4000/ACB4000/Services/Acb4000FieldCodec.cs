using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants;
using C = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000FieldConstants;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

/// <summary>Champs de la carte, sans conteneur de fichier ; la correction concerne seulement le tampon lu.</summary>
public static class Acb4000FieldCodec
{
    public static void WriteId(Span<byte> destination, uint logicalAddress, byte flags, bool defective = false, uint polynomial = Acb4000ErrorCheckConstants.Polynomial)
    {
        if (destination.Length != C.IdRecordLength) throw new ArgumentException(nameof(destination));
        if (logicalAddress > C.LogicalAddressMask) throw new ArgumentOutOfRangeException(nameof(logicalAddress));
        var address = logicalAddress | (defective ? C.DefectiveAddressMask : 0);
        destination[C.SyncOffset] = C.SyncByte;
        destination[C.MarkOffset] = C.IdMark;
        for (var index = 0; index < C.AddressByteCount; index++)
            destination[C.AddressOffset + index] = (byte)(address >> ((C.AddressByteCount - 1 - index) * Acb4000ErrorCheckConstants.BitsPerByte));
        destination[C.FlagsOffset] = flags;
        Acb4000ErrorCheck.Write(destination, polynomial);
    }

    public static bool TryReadId(ReadOnlySpan<byte> record, bool correctErrors, out Acb4000IdField field,
        out Acb4000ErrorCorrection correction, uint polynomial = Acb4000ErrorCheckConstants.Polynomial)
    {
        field = default;
        correction = default;
        if (record.Length != C.IdRecordLength) return false;
        Span<byte> work = stackalloc byte[C.IdRecordLength];
        record.CopyTo(work);
        if (!Check(work, correctErrors, out var corrected, polynomial) || !HasMark(work, C.IdMark)) return false;
        if (!TryDecodeId(work, out field)) return false;
        correction = corrected;
        return true;
    }

    internal static bool TryDecodeId(ReadOnlySpan<byte> record, out Acb4000IdField field)
    {
        field = default;
        if (record.Length != C.IdRecordLength || !HasMark(record, C.IdMark)) return false;
        uint address = 0;
        for (var index = 0; index < C.AddressByteCount; index++)
            address = (address << Acb4000ErrorCheckConstants.BitsPerByte) | record[C.AddressOffset + index];
        field = new(address & C.LogicalAddressMask, record[C.FlagsOffset], (address & C.DefectiveAddressMask) != 0);
        return true;
    }

    public static void WriteData(Span<byte> destination, ReadOnlySpan<byte> data, uint polynomial = Acb4000ErrorCheckConstants.Polynomial)
    {
        if (!ValidSize(data.Length)) throw new ArgumentOutOfRangeException(nameof(data));
        if (destination.Length != data.Length + C.DataOverhead) throw new ArgumentException(nameof(destination));
        // Memmove avant les marques permet aussi d'ajouter l'en-tête sur un tampon chevauchant.
        data.CopyTo(destination[C.DataOffset..]);
        destination[C.SyncOffset] = C.SyncByte;
        destination[C.MarkOffset] = C.DataMark;
        Acb4000ErrorCheck.Write(destination, polynomial);
    }

    /// <summary>Vrai si la marque et la taille conviennent ; les données sont copiées même avec une intégrité fausse.</summary>
    public static bool TryReadData(ReadOnlySpan<byte> record, Span<byte> destination, bool correctErrors,
        out bool integrityValid, out Acb4000ErrorCorrection correction, uint polynomial = Acb4000ErrorCheckConstants.Polynomial)
    {
        integrityValid = false;
        correction = default;
        var size = record.Length - C.DataOverhead;
        if (!ValidSize(size) || destination.Length < size) return false;
        Span<byte> work = stackalloc byte[Acb4000ErrorCheckConstants.MaximumRecordLength];
        work = work[..record.Length];
        record.CopyTo(work);
        var valid = Check(work, correctErrors, out var corrected, polynomial);
        if (!TryCopyData(work, destination)) return false;
        integrityValid = valid;
        correction = corrected;
        return true;
    }

    internal static bool TryCopyData(ReadOnlySpan<byte> record, Span<byte> destination)
    {
        var size = record.Length - C.DataOverhead;
        if (!ValidSize(size) || destination.Length < size || !HasMark(record, C.DataMark)) return false;
        record.Slice(C.DataOffset, size).CopyTo(destination);
        return true;
    }

    private static bool HasMark(ReadOnlySpan<byte> bytes, byte mark) =>
        bytes[C.SyncOffset] == C.SyncByte && bytes[C.MarkOffset] == mark;

    private static bool Check(Span<byte> bytes, bool correctErrors, out Acb4000ErrorCorrection correction, uint polynomial = Acb4000ErrorCheckConstants.Polynomial)
    {
        correction = default;
        return Acb4000ErrorCheck.Compute(bytes, polynomial) == Acb4000ErrorCheckConstants.InitialValue
            || correctErrors && Acb4000ErrorCheck.TryCorrect(bytes, out correction, polynomial);
    }

    private static bool ValidSize(int size) => size is Acb4000ParameterConstants.MinimumBlockSize
        or Acb4000ParameterConstants.MediumBlockSize or Acb4000ParameterConstants.MaximumBlockSize;
}
