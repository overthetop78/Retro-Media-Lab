using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Enums;
using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.NEC.UPD765.UPD765;

public sealed class WriteChannelTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EveryByteAndItsCrcAreSerializedOnTheExternalWriteClock(bool mfm)
    {
        var output = new Output(mfm);
        var previous = false;
        ushort crc = 0xffff;
        for (var value = 0; value < 256; value++)
        {
            var expected = Encode((byte)value, mfm, ref previous);
            var actual = output.Word((byte)value);
            Assert.Equal(expected, actual);
            crc = Upd765Crc.Add(crc, (byte)value);
            Assert.Equal(crc, output.Pins.Crc);
        }
        Assert.Equal(256, output.Completed);
    }

    [Theory]
    [InlineData(true, 0xa1, (int)Upd765WriteWordKind.SyncA1, 0x4489)]
    [InlineData(true, 0xc2, (int)Upd765WriteWordKind.SyncC2, 0x5224)]
    [InlineData(false, 0xfe, (int)Upd765WriteWordKind.FmAddress, 0xf57e)]
    [InlineData(false, 0xfb, (int)Upd765WriteWordKind.FmAddress, 0xf56f)]
    [InlineData(false, 0xf8, (int)Upd765WriteWordKind.FmAddress, 0xf56a)]
    [InlineData(false, 0xfc, (int)Upd765WriteWordKind.FmIndex, 0xf77a)]
    public void AddressAndSyncWordsUseTheRecordedClockPatterns(bool mfm, byte value,
        int kind, ushort expected)
    {
        var output = new Output(mfm);
        Assert.Equal(expected, output.Word(value, (Upd765WriteWordKind)kind));
        Assert.Equal(Upd765Crc.Add(0xffff, value), output.Pins.Crc);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void F5F6F7AreOrdinaryDataUnlessTheSequencerRequestsASpecialWord(bool mfm)
    {
        var output = new Output(mfm);
        var previous = false;
        foreach (byte value in new byte[] { 0xf5, 0xf6, 0xf7 })
            Assert.Equal(Encode(value, mfm, ref previous), output.Word(value));
        Assert.Equal(3, output.Completed);
    }

    [Fact]
    public void OutputDelayAndPulseWidthFollowTheExternalClock()
    {
        var output = new Output(false);
        var edges = new List<(ulong Clock, bool High)>();
        output.Pins.WriteDataChanged += (time, high) => edges.Add((time, high));
        Assert.True(output.Pins.TryQueue(0xff));
        output.Pins.SetGate(true);
        output.Pins.SetClock(true);
        Assert.False(output.Pins.WriteData);
        Assert.False(output.Pins.WriteEnable);
        output.Pins.AdvanceHalfClocks(1);
        Assert.True(output.Pins.WriteData);
        Assert.True(output.Pins.WriteEnable);
        output.Pins.AdvanceHalfClocks(3);
        output.Pins.SetClock(false);
        Assert.True(output.Pins.WriteData);
        output.Pins.AdvanceHalfClocks(1);
        Assert.False(output.Pins.WriteData);
        Assert.Equal(new[] { (1UL, true), (5UL, false) }, edges);
    }

    [Fact]
    public void MainClockAdvanceWithoutWclkCannotShiftAWord()
    {
        var output = new Output(true);
        Assert.True(output.Pins.TryQueue(0xa5));
        output.Pins.SetGate(true);
        output.Pins.Advance(1_000_000);
        Assert.Equal(2_000_000UL, output.Pins.HalfClocks);
        Assert.False(output.Pins.WriteEnable);
        Assert.False(output.Pins.WriteData);
        Assert.Equal(0xffff, output.Pins.Crc);
        Assert.Equal(0, output.Completed);
    }

    [Fact]
    public void AStretchedWclkDoesNotShiftOrUpdateCrcTwice()
    {
        var output = new Output(true);
        Assert.True(output.Pins.TryQueue(0xa5));
        output.Pins.SetGate(true);
        output.Pins.SetClock(true);
        output.Pins.Advance(1);
        var crc = output.Pins.Crc;
        for (var index = 0; index < 32; index++) output.Pins.SetClock(true);
        output.Pins.Advance(100);
        Assert.Equal(crc, output.Pins.Crc);
        Assert.Equal(0, output.Completed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CrcFollowsEachDataBitAndCanBeDisabledForGaps(bool mfm)
    {
        var output = new Output(mfm);
        output.Pins.CrcEnabled = false;
        output.Word(0);
        Assert.Equal(0xffff, output.Pins.Crc);
        output.Pins.CrcEnabled = true;
        output.Pins.ResetCrc(0x1234);
        Assert.True(output.Pins.TryQueue(0x97));
        ushort expected = 0x1234;
        for (var cell = 0; cell < 16; cell++)
        {
            output.Cell();
            if ((cell & 1) != 0) expected = Upd765Crc.AddBit(expected, (0x97 & 1 << (7 - cell / 2)) != 0);
            Assert.Equal(expected, output.Pins.Crc);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ClosingTheGateFromWordCompletionPreservesTheFinalPulse(bool mfm)
    {
        var output = new Output(mfm);
        var edges = new List<bool>();
        output.Pins.WriteDataChanged += (_, high) => edges.Add(high);
        output.Pins.WordCompleted += () => output.Pins.SetGate(false);
        output.Word(0xff);
        Assert.Equal(mfm ? 8 : 16, edges.Count(high => high));
        Assert.Equal(edges.Count(high => high), edges.Count(high => !high));
        Assert.False(output.Pins.WriteEnable);
        Assert.False(output.Pins.WriteData);
        Assert.Equal(1, output.Completed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ResetCancelsPendingEdgesAtEveryCellBeforeAndDuringAPulse(bool mfm)
    {
        for (var stop = 0; stop < 16; stop++)
        foreach (var afterEdge in new[] { false, true })
        {
            var output = new Output(mfm);
            Assert.True(output.Pins.TryQueue(0xff));
            output.Pins.SetGate(true);
            for (var cell = 0; cell < stop; cell++) output.Cell();
            output.Pins.SetClock(true);
            if (afterEdge) output.Pins.AdvanceHalfClocks(1);
            output.Pins.Reset();
            var futureEdges = 0;
            output.Pins.WriteDataChanged += (_, _) => futureEdges++;
            output.Pins.Advance(1_000_000);
            Assert.Equal(0, futureEdges);
            Assert.False(output.Pins.WriteEnable);
            Assert.False(output.Pins.WriteData);
            Assert.False(output.Pins.Gate);
            Assert.Equal(0, output.Completed);
        }
    }

    [Fact]
    public void QueuedWordIsLoadedOnlyAfterTheCurrentWordAndRequestCanSupplyTheNext()
    {
        var output = new Output(false);
        Assert.True(output.Pins.TryQueue(0xaa));
        Assert.False(output.Pins.TryQueue(0xff));
        output.Pins.SetGate(true);
        var first = output.Cell();
        Assert.True(output.Pins.TryQueue(0x55));
        Assert.False(output.Pins.TryQueue(0xff));
        for (var cell = 1; cell < 16; cell++) first = first << 1 | output.Cell();
        Assert.Equal(0xeeee, first);
        var second = 0;
        for (var cell = 0; cell < 16; cell++) second = second << 1 | output.Cell();
        Assert.Equal(0xbbbb, second);
        output.Pins.WordRequested += () => Assert.True(output.Pins.TryQueue(0xff));
        var third = 0;
        for (var cell = 0; cell < 16; cell++) third = third << 1 | output.Cell();
        Assert.Equal(0xffff, third);
        Assert.Equal(3, output.Completed);
    }

    [Fact]
    public void ResetFromTheWriteEnableCallbackCannotPublishThePendingDataPulse()
    {
        var output = new Output(false);
        output.Pins.WriteEnableChanged += (_, high) => { if (high) output.Pins.Reset(); };
        Assert.True(output.Pins.TryQueue(0xff));
        output.Pins.SetGate(true);
        output.Pins.SetClock(true);
        output.Pins.AdvanceHalfClocks(1);
        Assert.False(output.Pins.WriteEnable);
        Assert.False(output.Pins.WriteData);
        Assert.False(output.Pins.Gate);
    }

    private static ushort Encode(byte data, bool mfm, ref bool previous)
    {
        var result = 0;
        for (var bit = 7; bit >= 0; bit--)
        {
            var value = (data & 1 << bit) != 0;
            result = result << 2 | (!mfm || !previous && !value ? 2 : 0) | (value ? 1 : 0);
            previous = value;
        }
        return (ushort)result;
    }

    private sealed class Output
    {
        internal readonly Upd765WriteChannel Pins;
        internal int Completed;
        private readonly int _period;

        internal Output(bool mfm)
        {
            Pins = new() { DoubleDensity = mfm };
            Pins.WordCompleted += () => Completed++;
            _period = mfm ? 16 : 32;
        }

        internal ushort Word(byte value, Upd765WriteWordKind kind = Upd765WriteWordKind.Data)
        {
            Assert.True(Pins.TryQueue(value, kind));
            Pins.SetGate(true);
            var word = 0;
            for (var bit = 0; bit < 16; bit++) word = word << 1 | Cell();
            return (ushort)word;
        }

        internal int Cell()
        {
            Pins.SetClock(true);
            Pins.AdvanceHalfClocks(1);
            var bit = Pins.WriteData ? 1 : 0;
            Pins.AdvanceHalfClocks(3);
            Pins.SetClock(false);
            Pins.AdvanceHalfClocks(1);
            Pins.AdvanceHalfClocks(_period - 5);
            return bit;
        }
    }
}
