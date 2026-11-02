namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Hd63463.Constants;

internal static class Hd63463ErrorCodeConstants
{
    internal const uint FirePolynomial = 0x00a00805;
    internal const uint FireHighBit = 0x80000000;
    internal const ushort CcittPolynomial = 0x1021;
    internal const ushort AlternateCrcPolynomial = 0x0001;
    internal const ushort CrcHighBit = 0x8000;
    internal const int FireBits = 32;
    internal const int FireBytes = 4;
    internal const int FireByteShift = 24;
    internal const int CrcBytes = 2;
    internal const int PatternBytes = 3;
    internal const int PatternHighShift = 16;
    internal const int MaximumBurstBits = 11;
    internal const uint PatternOverflowMask = 0xff000000;
    internal const uint PatternHighByteMask = 0x00ff0000;
    internal const uint PatternMiddleByteMask = 0x0000ff00;
    internal const uint PatternMask = 0x00ffffff;
    internal const byte EccMode = 0x08;
    internal const byte CrcPolynomialSelect = 0x04;
    internal const byte CrcInitialOnes = 0x02;
    internal const byte AddressMarkExcluded = 0x02;
    internal const int AddressMarkBytes = 1;
    internal const byte HostCorrection = 0x40;
    internal const byte Corrected = 0x48;
    internal const byte Uncorrectable = 0x4c;
    internal const int ResultPatternOffset = 2;
}
