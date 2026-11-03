using C = RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Constants.Mc6854Constants;

namespace RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Services;

public sealed partial class Mc6854
{
    private static readonly ushort[] CrcOctets = CreateCrcOctets();

    private static ushort CrcBit(ushort crc, bool bit) => (ushort)(((crc ^ (bit ? 1 : 0)) & 1) != 0
        ? (crc >> 1) ^ C.CrcPolynomial : crc >> 1);

    private static ushort CrcWord(ushort crc, uint value, int width)
    {
        if (width == C.BitsPerOctet)
            return (ushort)((crc >> C.BitsPerOctet) ^ CrcOctets[(crc ^ value) & byte.MaxValue]);
        for (var bit = 0; bit < width; bit++)
            crc = CrcBit(crc, ((value >> bit) & 1) != 0);
        return crc;
    }

    private static ushort[] CreateCrcOctets()
    {
        var table = new ushort[byte.MaxValue + 1];
        for (var index = 0; index < table.Length; index++)
        {
            var crc = (ushort)index;
            for (var bit = 0; bit < C.BitsPerOctet; bit++)
                crc = CrcBit(crc, false);
            table[index] = crc;
        }
        return table;
    }
}
