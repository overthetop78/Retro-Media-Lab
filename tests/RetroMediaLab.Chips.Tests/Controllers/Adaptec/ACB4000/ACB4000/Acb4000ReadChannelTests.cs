using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed class Acb4000ReadChannelTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AllPhysicalWordsRetainTheirSerialDataAndClockValidity(bool previous)
    {
        for (var word = 0; word <= ushort.MaxValue; word++)
        {
            var expectedPrevious = previous;
            byte expected = 0;
            var expectedClocks = true;
            for (var bit = 14; bit >= 0; bit -= 2)
            {
                var data = (word & (1 << bit)) != 0;
                expectedClocks &= ((word & (1 << (bit + 1))) != 0) == (!expectedPrevious && !data);
                expected = (byte)((expected << 1) | (data ? 1 : 0));
                expectedPrevious = data;
            }
            var actualPrevious = previous;
            Assert.Equal(expected, Acb4000MfmEncoding.Decode((ushort)word, ref actualPrevious, out var clocks));
            Assert.Equal(expectedClocks, clocks);
            Assert.Equal(expectedPrevious, actualPrevious);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeDecoderCompletesOnlyAfterTheFourBitNrzDelay(bool dataField)
    {
        var owner = new Acb4000(_ => null, 2_000_000);
        var channel = new Acb4000ReadChannel(owner.Aic);
        byte[] record;
        if (dataField)
        {
            record = new byte[262];
            Acb4000FieldCodec.WriteData(record, Enumerable.Repeat((byte)0x6c, 256).ToArray());
            channel.BeginDataRead();
        }
        else { record = Id(); channel.BeginIdRead(); }
        var results = new List<ReadEvent>();
        for (var index = 0; index < 12; index++) FeedWord(channel, 0xaaaa, results);
        FeedField(channel, record, results);
        Assert.Empty(results);
        for (var cell = 0; cell < 7; cell++)
            Assert.Equal(Acb4000FieldKind.None, channel.Feed((cell & 1) == 0));
        Assert.Equal(dataField ? Acb4000FieldKind.Data : Acb4000FieldKind.Id, channel.Feed(false));
        Assert.Equal(record, channel.Field.ToArray());
        Assert.True(channel.IntegrityValid);
        Assert.True(channel.FieldClockValid);
    }

    [Fact]
    public void ReadGateResetDiscardsDelayedCellsAndRequiresANewPreamble()
    {
        var owner = new Acb4000(_ => null, 2_000_000);
        var channel = new Acb4000ReadChannel(owner.Aic);
        var results = new List<ReadEvent>();
        channel.BeginIdRead();
        for (var index = 0; index < 12; index++) FeedWord(channel, 0xaaaa, results);
        FeedWord(channel, 0x4489, results);
        channel.StopRead();
        channel.BeginIdRead();
        FeedField(channel, Id(), results);
        FeedWord(channel, 0xaaaa, results);
        Assert.Empty(results);
        channel.Reset();
        channel.BeginIdRead();
        for (var index = 0; index < 12; index++) FeedWord(channel, 0xaaaa, results);
        FeedField(channel, Id(), results);
        FeedWord(channel, 0xaaaa, results);
        Assert.Equal(Id(), Assert.Single(results).Bytes);
    }

    [Fact]
    public void EveryLowAddressAndParameterFlagRetainsTheCompleteId()
    {
        for (uint address = 0; address < 256; address++)
            foreach (byte flags in new byte[] { 0, 1, 0x80, 0xff })
            {
                var channel = new Acb4000ReadChannel();
                var id = new byte[10];
                Acb4000FieldCodec.WriteId(id, address, flags);
                var results = new List<ReadEvent>();
                FeedField(channel, id, results);
                var field = Assert.Single(results);
                Assert.Equal(Acb4000FieldKind.Id, field.Kind);
                Assert.Equal(id, field.Bytes);
                Assert.True(channel.IntegrityValid, $"ID {address:X6}, flags {flags:X2}");
            }
    }

    [Theory]
    [InlineData(0xfe)]
    [InlineData(0xf8)]
    public void SyndromeAdvancesOnEachDataCellButNotOnClockCells(byte mark)
    {
        var actual = Acb4000EccRegisters.CreateFirmwareConfiguration();
        var expected = Acb4000EccRegisters.CreateFirmwareConfiguration();
        var channel = new Acb4000ReadChannel(actual);
        var results = new List<ReadEvent>();
        FeedWord(channel, 0x4489, results);
        expected.TryClockDataByte(0xa1);
        var previous = true;
        var word = Acb4000MfmEncoding.Encode(mark, ref previous);
        for (var bit = 15; bit >= 0; bit--)
        {
            var cell = (word & (1 << bit)) != 0;
            channel.Feed(cell);
            if ((bit & 1) == 0) expected.TryClockDataBit(cell);
            Assert.Equal(expected.ReadMiddleSyndrome(), actual.ReadMiddleSyndrome());
            Assert.Equal(expected.ReadHighSyndrome(), actual.ReadHighSyndrome());
            Assert.Equal(expected.CheckPassed, actual.CheckPassed);
        }
        Assert.Empty(results);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EveryByteAndClockCellIsCheckedForBothPreviousDataStates(bool previous)
    {
        for (var value = 0; value <= byte.MaxValue; value++)
        {
            var writing = previous;
            var word = Acb4000MfmEncoding.Encode((byte)value, ref writing);
            var reading = previous;
            Assert.Equal(value, Acb4000MfmEncoding.Decode(word, ref reading, out var valid));
            Assert.True(valid);
            Assert.Equal((value & 1) != 0, writing);
            Assert.Equal(writing, reading);
            for (var clock = 15; clock >= 1; clock -= 2)
            {
                reading = previous;
                Assert.Equal(value, Acb4000MfmEncoding.Decode((ushort)(word ^ (1 << clock)), ref reading, out valid));
                Assert.False(valid);
            }
        }
    }

    [Fact]
    public void AddressMarkUsesTheMissingClockRatherThanAnOrdinaryA1()
    {
        var previous = false;
        Assert.Equal(0x44a9, Acb4000MfmEncoding.Encode(0xa1, ref previous));
        Assert.Equal(0x5554, Acb4000MfmEncoding.Encode(0xfe, ref previous));
        previous = true;
        Assert.Equal(0x554a, Acb4000MfmEncoding.Encode(0xf8, ref previous));
        var words = new ushort[10];
        Acb4000MfmEncoding.WriteField(Id(), words, ref previous);
        Assert.Equal(0x4489, words[0]);
        Assert.Equal(0x5554, words[1]);
    }

    [Theory]
    [InlineData(256)]
    [InlineData(512)]
    [InlineData(1024)]
    public void IdAndDataAreDetectedAtEveryCellPhaseAndDecodedThroughEcc(int size)
    {
        var payload = Enumerable.Range(0, size).Select(index => (byte)(index * 37 + 11)).ToArray();
        var data = new byte[size + 6];
        Acb4000FieldCodec.WriteData(data, payload);
        for (var phase = 0; phase < 16; phase++)
        {
            var channel = new Acb4000ReadChannel(size);
            for (var bit = 0; bit < phase; bit++) channel.Feed(false);
            var results = new List<ReadEvent>();
            FeedField(channel, Id(), results);
            FeedField(channel, data, results);
            Assert.Equal(2, results.Count);
            Assert.Equal(Acb4000FieldKind.Id, results[0].Kind);
            Assert.Equal(Acb4000FieldKind.Data, results[1].Kind);
            Assert.All(results, result => Assert.True(result.ClocksValid));
            Assert.True(Acb4000FieldCodec.TryReadId(results[0].Bytes, false, out var id, out _));
            Assert.Equal(0x1234u, id.LogicalAddress);
            var decoded = new byte[size];
            Assert.True(Acb4000FieldCodec.TryReadData(results[1].Bytes, decoded, false, out var valid, out _));
            Assert.True(valid);
            Assert.Equal(payload, decoded);
        }
    }

    [Fact]
    public void OrdinaryA1CannotStartAFieldWithoutItsMissingClock()
    {
        var channel = new Acb4000ReadChannel();
        var results = new List<ReadEvent>();
        var previous = false;
        foreach (var value in Id()) FeedWord(channel, Acb4000MfmEncoding.Encode(value, ref previous), results);
        Assert.Empty(results);
        Assert.True(channel.Field.IsEmpty);
    }

    [Fact]
    public void ANewSyncAbandonsTheIncompleteFieldAndAnUnknownMarkIsIgnored()
    {
        var channel = new Acb4000ReadChannel();
        var results = new List<ReadEvent>();
        FeedField(channel, new byte[] { 0xa1, 0xf8, 0, 0, 0 }, results);
        Assert.Empty(results);
        Assert.True(channel.Field.IsEmpty);
        FeedField(channel, new byte[] { 0xa1, 0x42, 0, 0, 0 }, results);
        Assert.Empty(results);
        FeedField(channel, Id(), results);
        Assert.Equal(Id(), Assert.Single(results).Bytes);
    }

    [Fact]
    public void ClockFaultsAreRetainedWithoutInventingDataBitErrors()
    {
        var data = new byte[262];
        Acb4000FieldCodec.WriteData(data, Enumerable.Repeat((byte)0x6c, 256).ToArray());
        var words = new ushort[data.Length];
        var previous = false;
        Acb4000MfmEncoding.WriteField(data, words, ref previous);
        words[10] ^= 0x8000;
        var channel = new Acb4000ReadChannel();
        var results = new List<ReadEvent>();
        foreach (var word in words) FeedWord(channel, word, results);
        var received = Assert.Single(results);
        Assert.False(received.ClocksValid);
        Assert.Equal(data, received.Bytes);
        Assert.Equal(0u, Acb4000ErrorCheck.Compute(received.Bytes));
    }

    [Fact]
    public void DataBitFaultPassesThroughTheChannelAndCanBeCorrectedInTheControllerBuffer()
    {
        var payload = Enumerable.Repeat((byte)0x6c, 256).ToArray();
        var data = new byte[262];
        Acb4000FieldCodec.WriteData(data, payload);
        var words = new ushort[data.Length];
        var previous = false;
        Acb4000MfmEncoding.WriteField(data, words, ref previous);
        words[10] ^= 1;
        var results = new List<ReadEvent>();
        var channel = new Acb4000ReadChannel();
        foreach (var word in words) FeedWord(channel, word, results);
        var received = Assert.Single(results);
        Assert.Equal(data[10] ^ 1, received.Bytes[10]);
        var output = new byte[256];
        Assert.True(Acb4000FieldCodec.TryReadData(received.Bytes, output, true, out var valid, out var correction));
        Assert.True(valid);
        Assert.Equal(87, correction.FirstBit);
        Assert.Equal(payload, output);
    }

    [Fact]
    public void ResetAndReconfigurationDoNotRetainPartialOrCompletedFields()
    {
        var channel = new Acb4000ReadChannel();
        var results = new List<ReadEvent>();
        FeedField(channel, Id(), results);
        Assert.False(channel.Field.IsEmpty);
        channel.Reset();
        Assert.True(channel.Field.IsEmpty);
        Assert.False(channel.FieldClockValid);
        for (var bit = 15; bit >= 8; bit--) channel.Feed((0x4489 & (1 << bit)) != 0);
        channel.Reset();
        for (var bit = 7; bit >= 0; bit--) channel.Feed((0x4489 & (1 << bit)) != 0);
        Assert.True(channel.Field.IsEmpty);
        channel.Configure(512);
        var data = new byte[518];
        Acb4000FieldCodec.WriteData(data, new byte[512]);
        results.Clear();
        FeedField(channel, data, results);
        Assert.Equal(data, Assert.Single(results).Bytes);
        Assert.Throws<ArgumentOutOfRangeException>(() => channel.Configure(255));
        Assert.Equal(data, channel.Field.ToArray());
    }

    [Fact]
    public void InvalidFieldOutputDoesNotChangeTheDestinationOrPreviousBit()
    {
        var output = Enumerable.Repeat((ushort)0xa7a7, 10).ToArray();
        var previous = false;
        Assert.Throws<ArgumentException>(() => Acb4000MfmEncoding.WriteField(new byte[10], output, ref previous));
        Assert.False(previous);
        Assert.All(output, value => Assert.Equal(0xa7a7, value));
    }

    private static byte[] Id() => Convert.FromHexString("A1FE00123480CFFFCE8F");
    private sealed record ReadEvent(Acb4000FieldKind Kind, byte[] Bytes, bool ClocksValid);

    private static void FeedField(Acb4000ReadChannel channel, byte[] field, List<ReadEvent> results)
    {
        var words = new ushort[field.Length];
        var previous = false;
        Acb4000MfmEncoding.WriteField(field, words, ref previous);
        foreach (var word in words) FeedWord(channel, word, results);
    }

    private static void FeedWord(Acb4000ReadChannel channel, ushort word, List<ReadEvent> results)
    {
        for (var bit = 15; bit >= 0; bit--)
        {
            var kind = channel.Feed((word & (1 << bit)) != 0);
            if (kind != Acb4000FieldKind.None) results.Add(new(kind, channel.Field.ToArray(), channel.FieldClockValid));
        }
    }
}
