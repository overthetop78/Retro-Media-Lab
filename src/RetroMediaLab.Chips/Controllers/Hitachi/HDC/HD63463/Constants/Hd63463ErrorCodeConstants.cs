namespace RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Constants;

public static class Hd63463ErrorCodeConstants
{
    public const uint FirePolynomial = 0x00a00805;
    public const uint FireHighBit = 0x80000000;
    public const ushort CcittPolynomial = 0x1021;
    public const ushort AlternateCrcPolynomial = 0x0001;
    public const ushort CrcHighBit = 0x8000;
    public const int FireBits = 32;
    public const int FireBytes = 4;
    public const int FireByteShift = 24;
    public const int CrcBytes = 2;
    public const int PatternBytes = 3;
    public const int PatternHighShift = 16;
    public const int MaximumBurstBits = 11;
    public const uint PatternOverflowMask = 0xff000000;
    public const uint PatternHighByteMask = 0x00ff0000;
    public const uint PatternMiddleByteMask = 0x0000ff00;
    public const uint PatternMask = 0x00ffffff;
    public const byte EccMode = 0x08;
    public const byte CrcPolynomialSelect = 0x04;
    public const byte CrcInitialOnes = 0x02;
    public const byte AddressMarkExcluded = 0x02;
    public const int AddressMarkBytes = 1;
    public const byte HostCorrection = 0x40;
    public const byte Corrected = 0x48;
    public const byte Uncorrectable = 0x4c;
    public const int ResultPatternOffset = 2;
}
