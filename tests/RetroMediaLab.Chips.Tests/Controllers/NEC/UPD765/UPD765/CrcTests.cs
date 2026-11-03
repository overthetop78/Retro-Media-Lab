using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.NEC.UPD765.UPD765;

public sealed class CrcTests
{
    [Theory]
    [InlineData("", 0xffff)]
    [InlineData("313233343536373839", 0x29b1)]
    [InlineData("a1a1a1", 0xcdb4)]
    [InlineData("a1a1a1fe00000102", 0xca6f)]
    [InlineData("fe00000100", 0xd2c3)]
    public void KnownCcittAndIbmIdVectors(string hex, ushort expected)
    {
        Assert.Equal(expected, Compute(Convert.FromHexString(hex)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0xffff)]
    [InlineData(0xa5a5)]
    public void SerialAndByteUpdatesAgreeForEveryInputByte(ushort seed)
    {
        for (var value = 0; value < 256; value++)
        {
            var serial = seed;
            for (var bit = 7; bit >= 0; bit--) serial = Upd765Crc.AddBit(serial, (value & 1 << bit) != 0);
            Assert.Equal(Upd765Crc.Add(seed, (byte)value), serial);
        }
    }

    [Fact]
    public void RecordedCrcHasZeroResidueAndDetectsEverySingleBitChange()
    {
        byte[] record = [0xa1, 0xa1, 0xa1, 0xfe, 0, 0, 1, 2, 0xca, 0x6f];
        Assert.Equal(0, Compute(record));
        for (var bit = 0; bit < record.Length * 8; bit++)
        {
            record[bit / 8] ^= (byte)(1 << bit % 8);
            Assert.NotEqual(0, Compute(record));
            record[bit / 8] ^= (byte)(1 << bit % 8);
        }
    }

    [Fact]
    public void FoldedByteUpdateMatchesSerialFeedbackForEveryPossibleRegisterAndByte()
    {
        // Exhaustive linear basis: all states with a zero byte, and all byte values
        // at zero state. CRC feedback is linear over GF(2), so these span every pair.
        for (var seed = 0; seed <= ushort.MaxValue; seed++)
        {
            var serial = (ushort)seed;
            for (var bit = 0; bit < 8; bit++) serial = Upd765Crc.AddBit(serial, false);
            Assert.Equal(serial, Upd765Crc.Add((ushort)seed, 0));
        }
        for (var value = 0; value <= byte.MaxValue; value++)
        {
            ushort serial = 0;
            for (var bit = 7; bit >= 0; bit--) serial = Upd765Crc.AddBit(serial, (value & 1 << bit) != 0);
            Assert.Equal(serial, Upd765Crc.Add(0, (byte)value));
        }
    }

    private static ushort Compute(byte[] bytes)
    {
        ushort crc = 0xffff;
        foreach (var value in bytes) crc = Upd765Crc.Add(crc, value);
        return crc;
    }
}
