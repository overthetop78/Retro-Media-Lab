using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Enums;
using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.WesternDigital.WD177x.WD1770;

public sealed class WriteChannelTests
{
    [Theory]
    [InlineData(false, 0x00, false, 0xaaaa)]
    [InlineData(false, 0xff, false, 0xffff)]
    [InlineData(false, 0xfe, true, 0xf57e)]
    [InlineData(false, 0xfb, true, 0xf56f)]
    [InlineData(false, 0xf8, true, 0xf56a)]
    [InlineData(false, 0xfc, true, 0xf77a)]
    [InlineData(true, 0x00, false, 0xaaaa)]
    [InlineData(true, 0xff, false, 0x5555)]
    [InlineData(true, 0xf5, true, 0x4489)]
    [InlineData(true, 0xf6, true, 0x5224)]
    public void ReferenceWordsProduceTheExpectedPhysicalTransitions(bool mfm, byte value, bool formatting, ushort expected)
    {
        var channel = new Wd1770WriteChannel { DoubleDensity = mfm };
        channel.SetGate(true);
        var words = WriteWord(channel, value, formatting);
        Assert.Equal(new[] { expected }, words);
    }

    [Fact]
    public void MfmKeepsThePreviousDataBitAcrossByteBoundaries()
    {
        var channel = new Wd1770WriteChannel();
        channel.SetGate(true);
        Assert.Equal(new ushort[] { 0x5555 }, WriteWord(channel, 0xff, false));
        Assert.Equal(new ushort[] { 0x2aaa }, WriteWord(channel, 0x00, false));
        Assert.Equal(new ushort[] { 0xaaaa }, WriteWord(channel, 0x00, false));
    }

    [Theory]
    [InlineData(false, 64, 22)]
    [InlineData(true, 32, 11)]
    public void NominalPulseWidthsAndCellTimesFollowTheClock(bool mfm, int cellHalfClocks, int width)
    {
        var channel = new Wd1770WriteChannel { DoubleDensity = mfm };
        var edges = new List<(ulong Time, bool High)>();
        channel.WriteDataChanged += (time, high) => edges.Add((time, high));
        channel.SetGate(true);
        Assert.True(channel.TryLoadData(0xff));
        while (!channel.CanLoad) channel.Tick();
        for (var clock = 0; clock < cellHalfClocks; clock++) channel.Tick();
        var rises = edges.Where(edge => edge.High).ToArray();
        Assert.Equal(mfm ? 8 : 16, rises.Length);
        // FF MFM n'a pas d'impulsion dans sa première cellule clock ; FF FM en a une.
        Assert.Equal((ulong)(cellHalfClocks * (mfm ? 2 : 1)), rises[0].Time);
        for (var edge = 0; edge < edges.Count; edge += 2)
        {
            Assert.True(edges[edge].High);
            Assert.False(edges[edge + 1].High);
            Assert.Equal((ulong)width, edges[edge + 1].Time - edges[edge].Time);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EveryDataByteIncludingFormatTokensReadsBackAsOrdinaryData(bool mfm)
    {
        var rig = new Rig(mfm);
        rig.Preamble();
        for (var value = 0; value <= byte.MaxValue; value++) rig.Write((byte)value, formatting: false);
        rig.Flush();
        var mark = rig.Received.FindIndex(item => item.Data == 0xfe && item.Kind == Wd1770ReadWordKind.AddressMark);
        Assert.True(mark >= 0);
        var actual = rig.Received.Skip(mark + 1).Take(256).ToArray();
        Assert.Equal(Enumerable.Range(0, 256).Select(value => (byte)value), actual.Select(item => item.Data));
        Assert.All(actual, item => { Assert.True(item.Valid); Assert.Equal(Wd1770ReadWordKind.Data, item.Kind); });
    }

    [Theory]
    [InlineData(false, 0xd2, 0xc3)]
    [InlineData(true, 0xca, 0x6f)]
    public void FormattingGeneratesTheTwoRecordedCrcBytesFromOneF7(bool mfm, byte high, byte low)
    {
        var rig = new Rig(mfm);
        rig.Preamble();
        foreach (var value in new byte[] { 0, 0, 1, (byte)(mfm ? 2 : 0) }) rig.Write(value, true);
        var firstCrcClock = rig.Writer.HalfClocks;
        Assert.True(rig.Writer.TryLoadFormat(0xf7));
        var oneByteClocks = (mfm ? 16 : 32) * 16;
        for (var clock = 0; clock < oneByteClocks; clock++) rig.Tick();
        Assert.False(rig.Writer.CanLoad);
        while (!rig.Writer.CanLoad) rig.Tick();
        Assert.Equal((ulong)(oneByteClocks * 4), rig.Writer.HalfClocks - firstCrcClock);
        Assert.Equal(0, rig.Writer.Crc);
        rig.Flush();
        var mark = rig.Received.FindIndex(item => item.Data == 0xfe && item.Kind == Wd1770ReadWordKind.AddressMark);
        var field = rig.Received.Skip(mark).Take(7).ToArray();
        Assert.Equal(7, field.Length);
        Assert.Equal(new[] { high, low }, field.Skip(5).Select(item => item.Data));
        ushort crc = mfm ? (ushort)0xcdb4 : (ushort)0xffff;
        foreach (var item in field) crc = Wd1770Crc.Add(crc, item.Data);
        Assert.Equal(0, crc);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ClosingGateAtEveryCellStopsAllFutureTransitionsAndDiscardsTheShiftRegister(bool mfm)
    {
        var cellClocks = mfm ? 16 : 32;
        for (var cells = 0; cells <= 16; cells++)
        {
            var writer = new Wd1770WriteChannel { DoubleDensity = mfm };
            var edges = new List<(ulong, bool)>();
            writer.WriteDataChanged += (time, high) => edges.Add((time, high));
            writer.SetGate(true);
            Assert.True(writer.TryLoadData(0xff));
            for (var clock = 0; clock < cells * cellClocks; clock++) writer.Tick();
            writer.SetGate(false);
            var saved = edges.ToArray();
            for (var clock = 0; clock < cellClocks * 32; clock++) writer.Tick();
            Assert.Equal(saved, edges);
            Assert.False(writer.WriteData);
            Assert.True(writer.CanLoad);
            Assert.False(writer.TryLoadData(0x55));
            writer.SetGate(true);
            Assert.Equal(new ushort[] { 0xaaaa }, WriteWord(writer, 0, false));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AFullShiftRegisterCannotBeOverwritten(bool mfm)
    {
        var writer = new Wd1770WriteChannel { DoubleDensity = mfm };
        writer.SetGate(true);
        Assert.True(writer.TryLoadData(0xff));
        Assert.False(writer.TryLoadData(0));
        Assert.False(writer.TryLoadFormat(0xf7));
        for (var clock = 0; clock < (mfm ? 16 : 32) * 16; clock++) writer.Tick();
        Assert.True(writer.CanLoad);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CrcContainsOnlyTheDataBitsActuallyShiftedBeforeAnInterruptedWrite(bool mfm)
    {
        ushort[] expected = [0xffff, 0xefdf, 0xcf9f, 0x8f1f, 0x0e1f, 0x1c3e, 0x387c, 0x70f8, 0xe1f0];
        var cellClocks = mfm ? 16 : 32;
        for (var cells = 0; cells <= 16; cells++)
        {
            var writer = new Wd1770WriteChannel { DoubleDensity = mfm };
            writer.SetGate(true);
            Assert.True(writer.TryLoadData(0));
            Assert.Equal(0xffff, writer.Crc);
            for (var clock = 0; clock < cells * cellClocks; clock++) writer.Tick();
            writer.SetGate(false);
            Assert.Equal(expected[cells / 2], writer.Crc);
            for (var clock = 0; clock < cellClocks * 32; clock++) writer.Tick();
            Assert.Equal(expected[cells / 2], writer.Crc);
        }
    }

    [Fact]
    public void FormatA1PresetsTheStateBeforeShiftingAndReachesTheThreeA1Syndrome()
    {
        var writer = new Wd1770WriteChannel();
        writer.SetGate(true);
        Assert.True(writer.TryLoadFormat(0xf5));
        Assert.Equal(0x968b, writer.Crc);
        while (!writer.CanLoad) writer.Tick();
        Assert.Equal(0xcdb4, writer.Crc);
    }

    [Theory]
    [InlineData(0x60, 192, -2)]
    [InlineData(0xe0, 192, -2)]
    [InlineData(0x30, 192, 2)]
    [InlineData(0xb0, 192, 2)]
    [InlineData(0x10, 160, -2)]
    [InlineData(0x80, 160, 2)]
    [InlineData(0x00, 160, 0)]
    [InlineData(0x20, 192, 0)]
    [InlineData(0xf0, 192, 0)]
    public void PrecompensationMatchesTheFourPatternsOfTheWesternDigitalTable(byte value, int nominal, int offset)
    {
        var writer = new Wd1770WriteChannel { PrecompensationEnabled = true };
        var edges = new List<(ulong Time, bool High)>();
        writer.WriteDataChanged += (time, high) => edges.Add((time, high));
        writer.SetGate(true);
        Assert.True(writer.TryLoadData(value));
        while (!writer.CanLoad) writer.Tick();
        for (var tick = 0; tick < 16; tick++) writer.Tick();
        var rise = edges.FindIndex(item => item.High && item.Time == (ulong)(nominal + offset));
        Assert.True(rise >= 0);
        Assert.False(edges[rise + 1].High);
        Assert.Equal((ulong)(nominal + 11), edges[rise + 1].Time);
        Assert.Equal((ulong)(11 - offset), edges[rise + 1].Time - edges[rise].Time);
    }

    [Theory]
    [InlineData(false, 512)]
    [InlineData(true, 514)]
    public void LastDataBitUsesTheNextDataRegisterBitAvailableBeforeItsDeadline(bool nextHigh, int expected)
    {
        var writer = new Wd1770WriteChannel { PrecompensationEnabled = true };
        var rising = new List<ulong>();
        writer.WriteDataChanged += (time, high) => { if (high) rising.Add(time); };
        writer.SetGate(true);
        Assert.True(writer.TryLoadData(0x01));
        for (var clock = 0; clock < 200; clock++) writer.Tick();
        writer.SetLookAhead(nextHigh ? (byte)0x80 : (byte)0);
        while (!writer.CanLoad) writer.Tick();
        for (var clock = 0; clock < 16; clock++) writer.Tick();
        Assert.Equal((ulong)expected, rising[^1]);
    }

    [Fact]
    public void FmIgnoresThePrecompensationControl()
    {
        static List<(ulong, bool)> Capture(bool precompensation)
        {
            var writer = new Wd1770WriteChannel { DoubleDensity = false, PrecompensationEnabled = precompensation };
            var edges = new List<(ulong, bool)>();
            writer.WriteDataChanged += (time, high) => edges.Add((time, high));
            writer.SetGate(true);
            Assert.True(writer.TryLoadData(0x16));
            while (!writer.CanLoad) writer.Tick();
            for (var clock = 0; clock < 32; clock++) writer.Tick();
            return edges;
        }
        Assert.Equal(Capture(false), Capture(true));
    }

    [Fact]
    public void FirstDataBitKeepsTheHistoryFromThePreviousByte()
    {
        var writer = new Wd1770WriteChannel { PrecompensationEnabled = true };
        var rising = new List<ulong>();
        writer.WriteDataChanged += (time, high) => { if (high) rising.Add(time); };
        writer.SetGate(true);
        Assert.True(writer.TryLoadData(0x01));
        writer.SetLookAhead(0x80);
        while (!writer.CanLoad) writer.Tick();
        var start = writer.HalfClocks;
        Assert.True(writer.TryLoadData(0x80));
        while (!writer.CanLoad) writer.Tick();
        Assert.Contains(start + 62, rising); // X110 : deux cellules, avancées d'une clock.
        Assert.DoesNotContain(start + 64, rising);
    }

    [Fact]
    public void CompensatedMfmPulsesStillReadBackWithCorrectDataAndCrc()
    {
        var rig = new Rig(true);
        rig.Writer.PrecompensationEnabled = true;
        rig.Preamble();
        foreach (var value in new byte[] { 0x60, 0x30, 0x10, 0x80 }) rig.Write(value, false);
        rig.Write(0xf7, true);
        rig.Flush();
        var mark = rig.Received.FindIndex(item => item.Data == 0xfe && item.Kind == Wd1770ReadWordKind.AddressMark);
        var field = rig.Received.Skip(mark).Take(7).ToArray();
        Assert.Equal(7, field.Length);
        Assert.Equal(new byte[] { 0xfe, 0x60, 0x30, 0x10, 0x80 }, field.Take(5).Select(item => item.Data));
        ushort crc = 0xcdb4;
        foreach (var item in field) { Assert.True(item.Valid); crc = Wd1770Crc.Add(crc, item.Data); }
        Assert.Equal(0, crc);
    }

    private static ushort[] WriteWord(Wd1770WriteChannel writer, byte value, bool formatting)
    {
        var start = writer.HalfClocks;
        var cell = writer.DoubleDensity ? 32 : 64;
        var words = new ushort[formatting && value == 0xf7 ? 2 : 1];
        void Observe(ulong time, bool high)
        {
            if (!high) return;
            var position = (int)((time - start) / (ulong)cell) - 1;
            words[position / 16] |= (ushort)(1 << (15 - position % 16));
        }
        writer.WriteDataChanged += Observe;
        try
        {
            Assert.True(formatting ? writer.TryLoadFormat(value) : writer.TryLoadData(value));
            while (!writer.CanLoad) writer.Tick();
        }
        finally { writer.WriteDataChanged -= Observe; }
        return words;
    }

    private sealed class Rig
    {
        private readonly bool _mfm;
        private readonly Wd1770ReadChannel _reader;
        internal readonly Wd1770WriteChannel Writer;
        internal readonly List<(byte Data, Wd1770ReadWordKind Kind, bool Valid)> Received = [];
        internal Rig(bool mfm)
        {
            _mfm = mfm;
            _reader = new() { DoubleDensity = mfm };
            _reader.ByteReceived += (data, kind, valid) => Received.Add((data, kind, valid));
            Writer = new() { DoubleDensity = mfm };
            Writer.SetGate(true);
        }
        internal void Tick() { Writer.Tick(); _reader.Tick(Writer.WriteData); }
        internal void Write(byte value, bool formatting)
        {
            Assert.True(formatting ? Writer.TryLoadFormat(value) : Writer.TryLoadData(value));
            while (!Writer.CanLoad) Tick();
        }
        internal void Preamble()
        {
            for (var index = 0; index < (_mfm ? 12 : 6); index++) Write(0, true);
            if (_mfm) for (var index = 0; index < 3; index++) Write(0xf5, true);
            Write(0xfe, true);
        }
        internal void Flush()
        {
            for (var clock = 0; clock < 64; clock++) Tick();
            Writer.SetGate(false);
        }
    }
}
