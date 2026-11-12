using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Contracts;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Services;

namespace RetroMediaLab.Chips.Tests.CPU.Zilog.Z80.Z80;

/// <summary>Références mathématiques indépendantes des formules bit à bit de production.</summary>
public sealed class RegisterArithmeticTests
{
    [Fact]
    public void ByteViewsAndPairsShareStorageInBothBanks()
    {
        var r = new Z80Registers
        {
            AF = 0x1234, BC = 0x5678, DE = 0x9abc, HL = 0xdef0,
            AlternateAF = 0x2143, AlternateBC = 0x6587, AlternateDE = 0xa9cb, AlternateHL = 0xed0f,
        };
        Assert.Equal(new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9a, 0xbc, 0xde, 0xf0,
            0x21, 0x43, 0x65, 0x87, 0xa9, 0xcb, 0xed, 0x0f },
            new[] { r.A, r.F, r.B, r.C, r.D, r.E, r.H, r.L,
                r.AlternateA, r.AlternateF, r.AlternateB, r.AlternateC,
                r.AlternateD, r.AlternateE, r.AlternateH, r.AlternateL });
        r.A = 0xff; r.F = 0x80; r.B = 0xfe; r.C = 0x81;
        r.D = 0xfd; r.E = 0x82; r.H = 0xfc; r.L = 0x83;
        r.AlternateA = 0xfb; r.AlternateF = 0x84; r.AlternateB = 0xfa; r.AlternateC = 0x85;
        r.AlternateD = 0xf9; r.AlternateE = 0x86; r.AlternateH = 0xf8; r.AlternateL = 0x87;
        Assert.Equal(new ushort[] { 0xff80, 0xfe81, 0xfd82, 0xfc83, 0xfb84, 0xfa85, 0xf986, 0xf887 },
            new[] { r.AF, r.BC, r.DE, r.HL, r.AlternateAF, r.AlternateBC, r.AlternateDE, r.AlternateHL });
    }

    [Fact]
    public void ExchangesKeepOtherRegistersAndRefreshWrapsOnlySevenBits()
    {
        var r = new Z80Registers
        {
            AF = 1, BC = 2, DE = 3, HL = 4, AlternateAF = 5, AlternateBC = 6,
            AlternateDE = 7, AlternateHL = 8, IX = 9, IY = 10, SP = 11, PC = 12, I = 13, R = 0xff,
        };
        var initial = r;
        r.ExchangeAccumulator();
        Assert.Equal((ushort)5, r.AF); Assert.Equal((ushort)1, r.AlternateAF);
        Assert.Equal(initial.BC, r.BC); Assert.Equal(initial.DE, r.DE); Assert.Equal(initial.HL, r.HL);
        r.ExchangeGeneral();
        Assert.Equal(new ushort[] { 6, 7, 8, 2, 3, 4 },
            new[] { r.BC, r.DE, r.HL, r.AlternateBC, r.AlternateDE, r.AlternateHL });
        Assert.Equal((ushort)5, r.AF); Assert.Equal((ushort)1, r.AlternateAF);
        r.ExchangeAccumulator(); r.ExchangeGeneral();
        Assert.Equal(initial, r);
        r.ExchangeDeHl();
        Assert.Equal(initial.DE, r.HL); Assert.Equal(initial.HL, r.DE);
        r.ExchangeDeHl();
        Assert.Equal(initial, r);
        for (int value = 0; value <= 255; value++)
        {
            r.R = (byte)value;
            r.IncrementRefresh();
            Assert.Equal((byte)(value < 128 ? (value + 1) % 128 : 128 + (value - 127) % 128), r.R);
        }
        r.R = initial.R;
        Assert.Equal(initial, r);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AddAndSubtractCoverEveryOperandAndCarry(bool carry)
    {
        int c = carry ? 1 : 0;
        for (int a = 0; a <= 255; a++)
        for (int b = 0; b <= 255; b++)
        {
            int sum = a + b + c;
            int signedSum = unchecked((sbyte)a) + unchecked((sbyte)b) + c;
            byte result = Z80Arithmetic.Add8((byte)a, (byte)b, carry, out byte flags);
            Assert.Equal((byte)(sum % 256), result);
            Assert.Equal(Expected(result, (a % 16) + (b % 16) + c > 15,
                signedSum is < -128 or > 127, false, sum > 255), flags);

            int difference = a - b - c;
            int signedDifference = unchecked((sbyte)a) - unchecked((sbyte)b) - c;
            result = Z80Arithmetic.Subtract8((byte)a, (byte)b, carry, out flags);
            Assert.Equal(unchecked((byte)difference), result);
            Assert.Equal(Expected(result, a % 16 < b % 16 + c,
                signedDifference is < -128 or > 127, true, difference < 0), flags);
        }
    }

    [Fact]
    public void IncrementAndDecrementKeepCarryForAllPriorFlags()
    {
        for (int value = 0; value <= 255; value++)
        for (int previous = 0; previous <= 255; previous++)
        {
            byte expected = (byte)((value + 1) % 256);
            Assert.Equal(expected, Z80Arithmetic.Increment8((byte)value, (byte)previous, out byte flags));
            Assert.Equal(Expected(expected, value % 16 == 15, value == 127, false, previous % 2 == 1), flags);
            expected = unchecked((byte)(value - 1));
            Assert.Equal(expected, Z80Arithmetic.Decrement8((byte)value, (byte)previous, out flags));
            Assert.Equal(Expected(expected, value % 16 == 0, value == 128, true, previous % 2 == 1), flags);
        }
    }

    [Fact]
    public void LogicCoversEveryOperandAndParity()
    {
        for (int a = 0; a <= 255; a++)
        for (int b = 0; b <= 255; b++)
        {
            byte and = (byte)(a & b), or = (byte)(a | b), xor = (byte)(a ^ b);
            Assert.Equal(and, Z80Arithmetic.And8((byte)a, (byte)b, out byte flags));
            Assert.Equal(Expected(and, true, EvenParity(and), false, false), flags);
            Assert.Equal(or, Z80Arithmetic.Or8((byte)a, (byte)b, out flags));
            Assert.Equal(Expected(or, false, EvenParity(or), false, false), flags);
            Assert.Equal(xor, Z80Arithmetic.Xor8((byte)a, (byte)b, out flags));
            Assert.Equal(Expected(xor, false, EvenParity(xor), false, false), flags);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WordArithmeticCoversEveryLeftOperandAgainstCarryAndSignBoundaries(bool carry)
    {
        ushort[] rightOperands = [0, 1, 0x0f, 0xff, 0xfff, 0x1000, 0x7fff, 0x8000, 0xfffe, 0xffff];
        int c = carry ? 1 : 0;
        for (int a = 0; a <= 65535; a++)
        foreach (int b in rightOperands)
        {
            int sum = a + b + c;
            int signedSum = unchecked((short)a) + unchecked((short)b) + c;
            ushort result = Z80Arithmetic.AddCarry16((ushort)a, (ushort)b, carry, out byte flags);
            Assert.Equal(unchecked((ushort)sum), result);
            Assert.Equal(ExpectedWord(result, a % 4096 + b % 4096 + c > 4095,
                signedSum is < -32768 or > 32767, false, sum > 65535), flags);

            int difference = a - b - c;
            int signedDifference = unchecked((short)a) - unchecked((short)b) - c;
            result = Z80Arithmetic.SubtractCarry16((ushort)a, (ushort)b, carry, out flags);
            Assert.Equal(unchecked((ushort)difference), result);
            Assert.Equal(ExpectedWord(result, a % 4096 < b % 4096 + c,
                signedDifference is < -32768 or > 32767, true, difference < 0), flags);

            byte previous = carry ? (byte)255 : (byte)0;
            result = Z80Arithmetic.Add16((ushort)a, (ushort)b, previous, out flags);
            Assert.Equal(unchecked((ushort)(a + b)), result);
            byte preserved = carry ? (byte)0xc4 : (byte)0;
            Assert.Equal((byte)(preserved | (a % 4096 + b % 4096 > 4095 ? 0x10 : 0)
                | (a + b > 65535 ? 1 : 0)), flags);
        }
    }

    private static byte Expected(byte result, bool half, bool parityOverflow, bool subtract, bool carry)
        => (byte)((result >= 128 ? 0x80 : 0) | (result == 0 ? 0x40 : 0)
            | (half ? 0x10 : 0) | (parityOverflow ? 4 : 0) | (subtract ? 2 : 0) | (carry ? 1 : 0));

    private static byte ExpectedWord(ushort result, bool half, bool overflow, bool subtract, bool carry)
        => (byte)((result >= 32768 ? 0x80 : 0) | (result == 0 ? 0x40 : 0)
            | (half ? 0x10 : 0) | (overflow ? 4 : 0) | (subtract ? 2 : 0) | (carry ? 1 : 0));

    private static bool EvenParity(byte value)
    {
        int ones = 0;
        for (int remaining = value; remaining != 0; remaining /= 2) ones += remaining % 2;
        return ones % 2 == 0;
    }
}
