using System.Text;
using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.WesternDigital.WD177x.WD1770;

public sealed class CrcTests
{
    [Theory]
    [InlineData("", 0xffff)]
    [InlineData("313233343536373839", 0x29b1)]
    [InlineData("00", 0xe1f0)]
    [InlineData("0000", 0x1d0f)]
    [InlineData("a1a1a1", 0xcdb4)]
    [InlineData("a1a1a1fe00000102", 0xca6f)]
    [InlineData("fe00000100", 0xd2c3)]
    public void KnownCcittFalseVectors(string hex, ushort expected)
    {
        ushort crc = 0xffff;
        foreach (var value in Convert.FromHexString(hex)) crc = Wd1770Crc.Add(crc, value);
        Assert.Equal(expected, crc);
    }

    [Fact]
    public void TransmittedCrcLeavesAZeroSyndromeAndDetectsEverySingleBitError()
    {
        var bytes = Encoding.ASCII.GetBytes("123456789").Concat(new byte[] { 0x29, 0xb1 }).ToArray();
        Assert.Equal(0, Compute(bytes));
        for (var bit = 0; bit < bytes.Length * 8; bit++)
        {
            bytes[bit / 8] ^= (byte)(1 << bit % 8);
            Assert.NotEqual(0, Compute(bytes));
            bytes[bit / 8] ^= (byte)(1 << bit % 8);
        }
    }

    private static ushort Compute(byte[] bytes)
    {
        ushort crc = 0xffff;
        foreach (var value in bytes) crc = Wd1770Crc.Add(crc, value);
        return crc;
    }
}
