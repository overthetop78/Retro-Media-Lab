using C = RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Constants.Wd1770Constants;

namespace RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Services;

/// <summary>Registre CRC matériel, x^16 + x^12 + x^5 + 1, bit de poids fort en premier.</summary>
internal static class Wd1770Crc
{
    internal static ushort AddBit(ushort crc, bool data)
    {
        var feedback = ((crc & C.CrcHighBit) != 0) != data;
        return (ushort)(crc << 1 ^ (feedback ? C.CrcPolynomial : 0));
    }

    internal static ushort Add(ushort crc, byte value)
    {
        crc ^= (ushort)(value << C.BitsPerByte);
        for (var bit = 0; bit < C.BitsPerByte; bit++)
            crc = (ushort)((crc & C.CrcHighBit) != 0 ? crc << 1 ^ C.CrcPolynomial : crc << 1);
        return crc;
    }
}
