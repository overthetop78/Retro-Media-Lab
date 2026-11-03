using C = RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Constants.Upd765Constants;

namespace RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Services;

/// <summary>Registre CRC-CCITT du contrôleur, DATA MSB en premier.</summary>
internal static class Upd765Crc
{
    internal static ushort AddBit(ushort crc, bool data)
    {
        var feedback = ((crc & C.CrcHighBit) != 0) != data;
        return (ushort)(crc << 1 ^ (feedback ? C.CrcPolynomial : 0));
    }

    internal static ushort Add(ushort crc, byte value)
    {
        var folded = (crc >> C.BitsPerByte) ^ value;
        folded ^= folded >> C.CrcFoldNibbleShift;
        return (ushort)((crc << C.BitsPerByte) ^ (folded << C.CrcFoldHighShift)
            ^ (folded << C.CrcFoldMiddleShift) ^ folded);
    }
}
