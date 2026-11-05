using System.Buffers.Binary;
using System.Numerics;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed class Acb4000ErrorCheckTests
{
    [Theory]
    [InlineData("A1FE00000000", 0x99b7f53eu)]
    [InlineData("A1FE00123480", 0xcfffce8fu)]
    [InlineData("313233343536373839", 0xffe64cebu)]
    public void IndependentBitwiseReferenceVectorsMatchTheFirmwarePolynomial(string hex, uint expected)
    {
        var data = Convert.FromHexString(hex);
        Assert.Equal(expected, Acb4000ErrorCheck.Compute(data));
        var record = new byte[data.Length + 4];
        data.CopyTo(record, 0);
        Acb4000ErrorCheck.Write(record);
        Assert.Equal(expected, BinaryPrimitives.ReadUInt32BigEndian(record.AsSpan(data.Length)));
        Assert.Equal(0u, Acb4000ErrorCheck.Compute(record));
    }

    [Fact]
    public void ProgrammedPolynomialControlsGenerationVerificationAndBurstCorrection()
    {
        const uint polynomial = 0x04c11db7;
        var record = new byte[262];
        Acb4000FieldCodec.WriteData(record, Enumerable.Range(0, 256).Select(index => (byte)index).ToArray(), polynomial);
        Assert.Equal(0u, Acb4000ErrorCheck.Compute(record, polynomial));
        Assert.NotEqual(0u, Acb4000ErrorCheck.Compute(record));
        var damaged = record.ToArray();
        damaged[19] ^= 0x38;
        Assert.True(Acb4000ErrorCheck.TryCorrect(damaged, out var correction, polynomial));
        Assert.Equal(3, correction.BitCount);
        Assert.Equal(record, damaged);
    }

    [Fact]
    public void DefaultFormatFillHasTheIndependentDataCheckVector()
    {
        var bytes = Enumerable.Repeat((byte)0x6c, 258).ToArray();
        bytes[0] = 0xa1;
        bytes[1] = 0xf8;
        Assert.Equal(0x5c89f9fau, Acb4000ErrorCheck.Compute(bytes));
    }

    [Fact]
    public void EverySingleBitOfTheMaximumRecordCanBeCorrectedIncludingSyncAndCheck()
    {
        var original = Record(1024);
        var damaged = original.ToArray();
        for (var bit = 0; bit < original.Length * 8; bit++)
        {
            Flip(damaged, bit);
            Assert.True(Acb4000ErrorCheck.TryCorrect(damaged, out var correction), $"bit {bit}");
            Assert.Equal(bit, correction.FirstBit);
            Assert.Equal(1, correction.BitCount);
            Assert.Equal(1, correction.Pattern);
            Assert.Equal(original, damaged);
        }
    }

    [Theory]
    [InlineData(256)]
    [InlineData(512)]
    [InlineData(1024)]
    public void EveryNonzeroEightBitBurstPatternIsCorrectedAtDifferentRecordPositions(int size)
    {
        var original = Record(size);
        for (var pattern = 1; pattern <= byte.MaxValue; pattern++)
        {
            var damaged = original.ToArray();
            var width = BitOperations.Log2((uint)pattern) + 1;
            var first = (pattern * 37) % (original.Length * 8 - width + 1);
            Burst(damaged, first, pattern, width);
            Assert.True(Acb4000ErrorCheck.TryCorrect(damaged, out var correction), $"size {size}, pattern {pattern:x2}");
            Assert.Equal(first, correction.FirstBit);
            Assert.Equal(width - BitOperations.TrailingZeroCount((uint)pattern), correction.BitCount);
            Assert.Equal(original, damaged);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(12)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(2057)]
    [InlineData(2063)]
    [InlineData(2081)]
    public void FullEightBitBurstCrossesSyncDataAndCheckBoundaries(int first)
    {
        var original = Record(256);
        var damaged = original.ToArray();
        Burst(damaged, first, 0xff, 8);
        Assert.True(Acb4000ErrorCheck.TryCorrect(damaged, out var correction));
        Assert.Equal(first, correction.FirstBit);
        Assert.Equal(8, correction.BitCount);
        Assert.Equal(original, damaged);
    }

    [Fact]
    public void IdFieldAlsoCorrectsBurstsAndAnIntactFieldIsNotReportedAsCorrected()
    {
        var original = Convert.FromHexString("A1FE00123480CFFFCE8F");
        Assert.True(Acb4000ErrorCheck.TryCorrect(original, out var clean));
        Assert.Equal(default, clean);
        for (var first = 0; first <= original.Length * 8 - 8; first++)
        {
            var damaged = original.ToArray();
            Burst(damaged, first, 0xa7, 8);
            Assert.True(Acb4000ErrorCheck.TryCorrect(damaged, out _));
            Assert.Equal(original, damaged);
        }
    }

    [Fact]
    public void SeparatedErrorsAreRejectedWithoutChangingTheReceivedRecord()
    {
        var damaged = Record(1024);
        Flip(damaged, 0);
        Flip(damaged, 40);
        var received = damaged.ToArray();
        Assert.False(Acb4000ErrorCheck.TryCorrect(damaged, out var correction));
        Assert.Equal(default, correction);
        Assert.Equal(received, damaged);
        Assert.Throws<ArgumentOutOfRangeException>(() => Acb4000ErrorCheck.TryCorrect(new byte[3], out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => Acb4000ErrorCheck.TryCorrect(new byte[1031], out _));
    }

    [Fact]
    public void FieldCalculationAndCorrectionDoNotAllocateAfterTableInitialization()
    {
        var bytes = Record(256);
        Acb4000ErrorCheck.TryCorrect(bytes, out _);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 32; index++)
        {
            Flip(bytes, index);
            Acb4000ErrorCheck.TryCorrect(bytes, out _);
            Acb4000ErrorCheck.Compute(bytes);
        }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(0, allocated);
        Assert.Equal(0u, Acb4000ErrorCheck.Compute(bytes));
    }

    private static byte[] Record(int size)
    {
        var record = new byte[size + 6];
        record[0] = 0xa1;
        record[1] = 0xf8;
        for (var index = 0; index < size; index++) record[index + 2] = (byte)(index * 37 + 11);
        Acb4000ErrorCheck.Write(record);
        return record;
    }

    private static void Flip(byte[] bytes, int bit) => bytes[bit / 8] ^= (byte)(1 << (7 - bit % 8));
    private static void Burst(byte[] bytes, int first, int pattern, int width)
    {
        for (var bit = 0; bit < width; bit++) if ((pattern & (1 << (width - 1 - bit))) != 0) Flip(bytes, first + bit);
    }
}
