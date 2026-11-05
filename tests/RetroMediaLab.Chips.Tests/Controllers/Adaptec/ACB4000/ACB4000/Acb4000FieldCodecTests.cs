using System.Buffers.Binary;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed class Acb4000FieldCodecTests
{
    [Fact]
    public void IdMatchesTheRawReferenceRecord()
    {
        var record = new byte[10];
        Acb4000FieldCodec.WriteId(record, 0x1234, 0x80);
        Assert.Equal(Convert.FromHexString("A1FE00123480CFFFCE8F"), record);
        Assert.True(Acb4000FieldCodec.TryReadId(record, false, out var field, out var correction));
        Assert.Equal(0x1234u, field.LogicalAddress);
        Assert.True(field.IsLastSector);
        Assert.False(field.IsDefective);
        Assert.Equal(0, field.DefectCount);
        Assert.Equal(default, correction);
    }

    [Theory]
    [InlineData(0u, 0, false)]
    [InlineData(0x123456u, 0x85, true)]
    [InlineData(0x7fffffu, 0xff, true)]
    public void DefectiveAddressAndFlagBitsAreKeptSeparately(uint address, int flags, bool defective)
    {
        var record = new byte[10];
        Acb4000FieldCodec.WriteId(record, address, (byte)flags, defective);
        Assert.True(Acb4000FieldCodec.TryReadId(record, false, out var field, out _));
        Assert.Equal(address, field.LogicalAddress);
        Assert.Equal(defective, field.IsDefective);
        Assert.Equal((flags & 0x80) != 0, field.IsLastSector);
        Assert.Equal(flags & 0x7f, field.DefectCount);
        Assert.Equal((byte)((address >> 16) | (defective ? 0x80u : 0u)), record[2]);
    }

    [Fact]
    public void IdCorrectionIncludesTheMarkButDoesNotChangeTheReceivedBytes()
    {
        var record = Convert.FromHexString("A1FE00123480CFFFCE8F");
        record[1] ^= 1;
        var received = record.ToArray();
        Assert.False(Acb4000FieldCodec.TryReadId(record, false, out _, out _));
        Assert.True(Acb4000FieldCodec.TryReadId(record, true, out var field, out var correction));
        Assert.Equal(0x1234u, field.LogicalAddress);
        Assert.Equal(15, correction.FirstBit);
        Assert.Equal(received, record);
    }

    [Theory]
    [InlineData(256)]
    [InlineData(512)]
    [InlineData(1024)]
    public void DataSizesRoundTripWithTheFirmwareF8Mark(int size)
    {
        var data = Enumerable.Range(0, size).Select(index => (byte)(index * 37 + 11)).ToArray();
        var record = new byte[size + 6];
        Acb4000FieldCodec.WriteData(record, data);
        Assert.Equal(0xa1, record[0]);
        Assert.Equal(0xf8, record[1]);
        Assert.Equal(0u, Acb4000ErrorCheck.Compute(record));
        var destination = Enumerable.Repeat((byte)0xa7, size + 3).ToArray();
        Assert.True(Acb4000FieldCodec.TryReadData(record, destination, false, out var valid, out var correction));
        Assert.True(valid);
        Assert.Equal(default, correction);
        Assert.Equal(data, destination[..size]);
        Assert.Equal(new byte[] { 0xa7, 0xa7, 0xa7 }, destination[size..]);
    }

    [Fact]
    public void FormatFillHasTheIndependentRawDataCheck()
    {
        var record = new byte[262];
        Acb4000FieldCodec.WriteData(record, Enumerable.Repeat((byte)0x6c, 256).ToArray());
        Assert.Equal(0x5c89f9fau, BinaryPrimitives.ReadUInt32BigEndian(record.AsSpan(258)));
    }

    [Fact]
    public void DataCanBePrefixedInItsOwnOverlappingBuffer()
    {
        var record = new byte[262];
        for (var index = 0; index < 256; index++) record[index] = (byte)index;
        var expected = record[..256];
        Acb4000FieldCodec.WriteData(record, record.AsSpan(0, 256));
        Assert.Equal(expected, record.AsSpan(2, 256).ToArray());
        Assert.Equal(0u, Acb4000ErrorCheck.Compute(record));
    }

    [Fact]
    public void CorrectedDataComesFromTheReadBufferWithoutRepairingThePhysicalRecord()
    {
        var record = new byte[262];
        var data = Enumerable.Repeat((byte)0x6c, 256).ToArray();
        Acb4000FieldCodec.WriteData(record, data);
        record[11] ^= 0xff;
        var received = record.ToArray();
        var destination = new byte[256];
        Assert.True(Acb4000FieldCodec.TryReadData(record, destination, true, out var valid, out var correction));
        Assert.True(valid);
        Assert.Equal(88, correction.FirstBit);
        Assert.Equal(8, correction.BitCount);
        Assert.Equal(data, destination);
        Assert.Equal(received, record);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UncorrectableDataStillTransfersItsActualPayload(bool correctErrors)
    {
        var record = new byte[262];
        Acb4000FieldCodec.WriteData(record, new byte[256]);
        record[2] ^= 0x80;
        record[7] ^= 0x80;
        var received = record.ToArray();
        var destination = new byte[256];
        Assert.True(Acb4000FieldCodec.TryReadData(record, destination, correctErrors, out var valid, out var correction));
        Assert.False(valid);
        Assert.Equal(default, correction);
        Assert.Equal(record.AsSpan(2, 256).ToArray(), destination);
        Assert.Equal(received, record);
    }

    [Fact]
    public void WrongDataMarkEvenWithValidCheckDoesNotModifyTheDestination()
    {
        var record = new byte[262];
        Acb4000FieldCodec.WriteData(record, new byte[256]);
        record[1] = 0xfb;
        Acb4000ErrorCheck.Write(record);
        var destination = Enumerable.Repeat((byte)0xa5, 256).ToArray();
        Assert.False(Acb4000FieldCodec.TryReadData(record, destination, true, out var valid, out var correction));
        Assert.False(valid);
        Assert.Equal(default, correction);
        Assert.All(destination, value => Assert.Equal(0xa5, value));
    }

    [Fact]
    public void InvalidLengthsAndAddressesFailBeforeOutputMutation()
    {
        var id = Enumerable.Repeat((byte)0xa7, 10).ToArray();
        Assert.Throws<ArgumentOutOfRangeException>(() => Acb4000FieldCodec.WriteId(id, 0x800000, 0));
        Assert.All(id, value => Assert.Equal(0xa7, value));
        Assert.False(Acb4000FieldCodec.TryReadId(new byte[9], true, out _, out _));
        var record = Enumerable.Repeat((byte)0xa7, 262).ToArray();
        Assert.Throws<ArgumentOutOfRangeException>(() => Acb4000FieldCodec.WriteData(record, new byte[255]));
        Assert.Throws<ArgumentException>(() => Acb4000FieldCodec.WriteData(record.AsSpan(0, 261), new byte[256]));
        Assert.All(record, value => Assert.Equal(0xa7, value));
        Assert.False(Acb4000FieldCodec.TryReadData(record, new byte[255], true, out _, out _));
        Assert.False(Acb4000FieldCodec.TryReadData(record.AsSpan(0, 261), new byte[256], true, out _, out _));
    }
}
