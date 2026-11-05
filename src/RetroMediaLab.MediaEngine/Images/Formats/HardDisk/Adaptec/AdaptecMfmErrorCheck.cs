using System.Buffers.Binary;
using C = RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Adaptec.AdaptecMfmFormat;

namespace RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Adaptec;

/// <summary>Contrôle du format média ; ne simule ni la correction du contrôleur ni ses relectures.</summary>
internal static class AdaptecMfmErrorCheck
{
    private static readonly uint[] Table = CreateTable();

    internal static uint Compute(ReadOnlySpan<byte> bytes)
    {
        uint value = 0;
        foreach (var item in bytes)
            value = (value << C.BitsPerByte) ^ Table[(byte)((value >> C.CheckHighByteShift) ^ item)];
        return value;
    }

    internal static void Write(Span<byte> record)
        => BinaryPrimitives.WriteUInt32BigEndian(record[^C.CheckLength..], Compute(record[..^C.CheckLength]));

    private static uint[] CreateTable()
    {
        var table = new uint[1 << C.BitsPerByte];
        for (var index = 0; index < table.Length; index++)
        {
            var value = (uint)index << C.CheckHighByteShift;
            for (var bit = 0; bit < C.BitsPerByte; bit++)
                value = (value << 1) ^ ((value & C.CheckHighBit) != 0 ? C.CheckPolynomial : 0);
            table[index] = value;
        }
        return table;
    }
}
