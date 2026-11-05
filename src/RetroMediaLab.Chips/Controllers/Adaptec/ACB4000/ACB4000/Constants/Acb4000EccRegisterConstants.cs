namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants;

/// <summary>Contrôle et taps ECC AIC-010, description constructeur Rev B, pages 13–15.</summary>
public static class Acb4000EccRegisterConstants
{
    public const ushort ControlAddress = 0x4071;
    public const ushort MiddleSyndromeAddress = 0x4072;
    public const ushort HighSyndromeAddress = 0x4073;
    public const ushort FirstPolynomialAddress = 0x4074;
    public const ushort LastPolynomialAddress = 0x4077;
    public const ushort RegisterPage = 0x4000;
    public const int FirstPolynomialRegister = 0x74;
    public const int LastPolynomialRegister = 0x77;
    public const int BitsPerByte = 8;
    public const int HighByteShift = 24;
    public const int MiddleByteShift = 16;
    public const uint HighBit = 0x80000000;
    public const uint LowSyndromeMask = 0x0001ffff;
    public const uint PolynomialTapMask = 0x7fffffff;
    public const byte SerialInput = 0x01;
    public const byte Shift = 0x02;
    public const byte DisableFeedback = 0x04;
    public const byte Clear = 0x08;
    public const byte SectorBranch = 0x10;
    public const byte Reset = 0x20;
    public const byte LengthMask = 0xc0;
    public const byte Length16 = 0x00;
    public const byte Length24 = 0x40;
    public const byte UndocumentedLength = 0x80;
    public const byte Length32 = 0xc0;
    public const uint Mask16 = 0xffff0000;
    public const uint Mask24 = 0xffffff00;
    public const uint Mask32 = uint.MaxValue;
    public const int LowBit16 = 16;
    public const int LowBit24 = 8;
    public const int LowBit32 = 0;
}
