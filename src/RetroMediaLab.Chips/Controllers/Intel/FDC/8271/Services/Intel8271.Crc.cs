using C = RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Constants.Intel8271Constants;

namespace RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Services;

public sealed partial class Intel8271
{
    private static byte SizeCode(int length)
    {
        byte size = 0;
        while ((C.MinimumSectorSize << size) < length && size < 7) size++;
        return size;
    }

    private static int CrcByte(int crc, byte value)
    {
        crc ^= value << C.BitsPerByte;
        for (var bit = 0; bit < C.BitsPerByte; bit++)
            crc = ((crc << 1) ^ ((crc & 0x8000) != 0 ? C.CrcPolynomial : 0)) & C.CrcInitial;
        return crc;
    }

}
