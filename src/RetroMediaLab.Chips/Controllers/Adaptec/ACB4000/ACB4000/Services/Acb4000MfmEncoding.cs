using C = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000MfmConstants;
using F = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000FieldConstants;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

/// <summary>Codage du canal horloge/données ; aucune piste ni image de fichier n'est possédée.</summary>
public static class Acb4000MfmEncoding
{
    public static ushort Encode(byte value, ref bool previousDataBit)
    {
        var data = ((value | value << C.NibbleBits) & C.PackedNibblesMask);
        data = (data | data << C.CellsPerBit) & C.PackedPairsMask;
        data = (data | data << 1) & C.DataCellsMask;
        var encoded = data | ClockCells(data, previousDataBit);
        previousDataBit = (value & 1) != 0;
        return (ushort)encoded;
    }

    public static byte Decode(ushort encoded, ref bool previousDataBit, out bool clockValid)
    {
        var data = encoded & C.DataCellsMask;
        clockValid = (encoded & C.ClockCellsMask) == ClockCells(data, previousDataBit);
        previousDataBit = (data & 1) != 0;
        data = (data | data >> 1) & C.PackedPairsMask;
        data = (data | data >> C.CellsPerBit) & C.PackedNibblesMask;
        return (byte)((data | data >> C.NibbleBits) & C.ByteMask);
    }

    private static int ClockCells(int data, bool previous) =>
        ~(data << 1 | data >> 1 | (previous ? C.PreviousDataClockMask : 0)) & C.ClockCellsMask;

    public static void WriteField(ReadOnlySpan<byte> field, Span<ushort> destination, ref bool previousDataBit)
    {
        if (field.IsEmpty || field[0] != F.SyncByte || destination.Length < field.Length) throw new ArgumentException(nameof(field));
        destination[0] = C.MissingClockA1;
        previousDataBit = (F.SyncByte & 1) != 0;
        for (var index = 1; index < field.Length; index++) destination[index] = Encode(field[index], ref previousDataBit);
    }
}
