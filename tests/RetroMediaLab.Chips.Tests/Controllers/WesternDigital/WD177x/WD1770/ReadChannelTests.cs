using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Enums;
using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.WesternDigital.WD177x.WD1770;

public sealed partial class ReadChannelTests
{
    [Theory]
    [InlineData(0xf57e, 0xfe, (int)Wd1770ReadWordKind.AddressMark)]
    [InlineData(0xf56f, 0xfb, (int)Wd1770ReadWordKind.AddressMark)]
    [InlineData(0xf56a, 0xf8, (int)Wd1770ReadWordKind.AddressMark)]
    [InlineData(0xf77a, 0xfc, (int)Wd1770ReadWordKind.IndexMark)]
    public void FmReferenceWords(int raw, int data, int kind)
    {
        var received = Observe(false, [0xaaaa, (ushort)raw]);
        Assert.Contains(((byte)data, (Wd1770ReadWordKind)kind, true), received);
    }

    [Theory]
    [InlineData(0x00, true)]
    [InlineData(0xf0, true)]
    [InlineData(0x80, true)]
    [InlineData(0x01, false)]
    [InlineData(0x0f, false)]
    [InlineData(0xff, false)]
    public void FmRequiresFourPrecedingZeroDataBitsBeforeAnAddressClock(byte prefix, bool recognized)
    {
        var previous = false;
        var received = Observe(false, [Encode(prefix, false, ref previous), 0xf57e]);
        Assert.Equal(recognized, received.Any(item => item.Item2 == Wd1770ReadWordKind.AddressMark));
    }

    [Theory]
    [InlineData(0x4489, 0xa1, (int)Wd1770ReadWordKind.SyncA1)]
    [InlineData(0x5224, 0xc2, (int)Wd1770ReadWordKind.SyncC2)]
    public void MfmMissingClockReferenceWords(int raw, int data, int kind)
    {
        var received = Observe(true, [0xaaaa, (ushort)raw]);
        Assert.Contains(((byte)data, (Wd1770ReadWordKind)kind, true), received);
    }

    [Theory]
    [InlineData(false, 0, false)]
    [InlineData(false, 2, false)]
    [InlineData(false, 0, true)]
    [InlineData(true, 0, false)]
    [InlineData(true, 2, false)]
    [InlineData(true, 0, true)]
    public void AllByteValuesSurvivePulseJitterAndSmallContinuousClockDrift(bool mfm, int jitter, bool drift)
    {
        var raw = Prefix(mfm);
        var previous = false; // FE se termine par un bit de données nul.
        for (var value = 0; value <= byte.MaxValue; value++) raw.Add(Encode((byte)value, mfm, ref previous));
        var received = Observe(mfm, raw, jitter: jitter, drift: drift);
        var marker = received.FindIndex(item => item.Item1 == 0xfe && item.Item2 == Wd1770ReadWordKind.AddressMark);
        Assert.True(marker >= 0);
        var bytes = received.Skip(marker + 1).Take(256).ToArray();
        Assert.Equal(256, bytes.Length);
        Assert.Equal(Enumerable.Range(0, 256).Select(value => (byte)value), bytes.Select(item => item.Item1));
        Assert.All(bytes, item => { Assert.Equal(Wd1770ReadWordKind.Data, item.Item2); Assert.True(item.Item3); });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MissingClockMarksRestoreByteAlignmentAfterEveryPossibleBitOffset(bool mfm)
    {
        for (var offset = 0; offset < 16; offset++)
        {
            var words = Prefix(mfm);
            var previous = false;
            words.Add(Encode(0x39, mfm, ref previous));
            words.Add(Encode(0x72, mfm, ref previous));
            var received = Observe(mfm, words, prefixCells: offset);
            var marker = received.FindIndex(item => item.Item1 == 0xfe && item.Item2 == Wd1770ReadWordKind.AddressMark);
            Assert.True(marker >= 0, $"Offset {offset}");
            Assert.Equal(new byte[] { 0x39, 0x72 }, received.Skip(marker + 1).Take(2).Select(item => item.Item1));
        }
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, true)]
    [InlineData(4, true)]
    public void MfmAddressMarkRequiresAThreeWordSyncRun(int syncWords, bool marked)
    {
        var raw = Enumerable.Repeat((ushort)0x4489, syncWords).ToList();
        var previous = true;
        raw.Add(Encode(0xfe, true, ref previous));
        var received = Observe(true, raw);
        var data = Assert.Single(received, item => item.Item1 == 0xfe);
        Assert.Equal(marked ? Wd1770ReadWordKind.AddressMark : Wd1770ReadWordKind.Data, data.Item2);
    }

    [Fact]
    public void IndexAndDataMarksUseDifferentMissingClockRunsInMfm()
    {
        var raw = new List<ushort> { 0xaaaa, 0x5224, 0x5224, 0x5224 };
        var previous = false;
        raw.Add(Encode(0xfc, true, ref previous));
        Assert.Contains(((byte)0xfc, Wd1770ReadWordKind.IndexMark, true), Observe(true, raw));
        raw = Enumerable.Repeat((ushort)0x4489, 3).ToList();
        previous = true;
        raw.Add(Encode(0xfc, true, ref previous));
        Assert.DoesNotContain(Observe(true, raw), item => item.Item2 == Wd1770ReadWordKind.IndexMark);
    }

    [Fact]
    public void OrdinaryMfmSequence01Fe29DoesNotBecomeAnIndexSyncInTheWrongClockPhase()
    {
        var words = Prefix(true);
        var previous = false;
        foreach (var value in new byte[] { 0x01, 0xfe, 0x29 }) words.Add(Encode(value, true, ref previous));
        var received = Observe(true, words);
        var mark = received.FindIndex(item => item.Item2 == Wd1770ReadWordKind.AddressMark);
        var data = received.Skip(mark + 1).Take(3).ToArray();
        Assert.Equal(new byte[] { 0x01, 0xfe, 0x29 }, data.Select(item => item.Item1));
        Assert.All(data, item => Assert.Equal(Wd1770ReadWordKind.Data, item.Item2));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void IllegalOrdinaryClockDoesNotBecomeAnAddressMark(bool mfm)
    {
        var words = Prefix(mfm);
        var previous = false;
        words.Add((ushort)(Encode(0xff, mfm, ref previous) ^ 0x8000));
        var received = Observe(mfm, words);
        Assert.Contains(((byte)0xff, Wd1770ReadWordKind.Data, false), received);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ASteadyReadDataLevelDoesNotManufactureTransitions(bool mfm)
    {
        var channel = new Wd1770ReadChannel { DoubleDensity = mfm };
        var count = 0;
        channel.ByteReceived += (_, _, _) => count++;
        for (var clock = 0; clock < 20_000; clock++) channel.Tick(false);
        for (var clock = 0; clock < 20_000; clock++) channel.Tick(true);
        Assert.Equal(0, count);
    }

    [Fact]
    public void ResetAndDensityChangeDiscardTheOldByteAlignment()
    {
        var channel = new Wd1770ReadChannel();
        var received = new List<(byte, Wd1770ReadWordKind, bool)>();
        channel.ByteReceived += (value, kind, valid) => received.Add((value, kind, valid));
        Feed(channel, true, Prefix(true));
        Assert.Contains(received, item => item.Item2 == Wd1770ReadWordKind.AddressMark);
        received.Clear();
        channel.Reset();
        for (var clock = 0; clock < 20_000; clock++) channel.Tick(false);
        Assert.Empty(received);
        channel.DoubleDensity = false;
        Feed(channel, false, [0xaaaa, 0xf56f]);
        Assert.Contains(((byte)0xfb, Wd1770ReadWordKind.AddressMark, true), received);
    }

    [Fact]
    public void FmAddressMarksKeepTheirRecordedValue()
    {
        for (var data = 0xf8; data <= 0xfe; data++)
        {
            var raw = 0;
            for (var bit = 7; bit >= 0; bit--)
                raw = raw << 2 | ((0xc7 >> bit & 1) << 1) | (data >> bit & 1);
            Assert.Contains(((byte)data, Wd1770ReadWordKind.AddressMark, true), Observe(false, [0xaaaa, (ushort)raw]));
        }
    }

    [Theory]
    [InlineData(false, 512)]
    [InlineData(true, 256)]
    public void ConsecutiveByteEventsFollowTheSiliconClockRate(bool mfm, int clocksPerByte)
    {
        var channel = new Wd1770ReadChannel { DoubleDensity = mfm };
        var times = new List<int>();
        var clock = 0;
        channel.ByteReceived += (_, kind, _) =>
        {
            if (kind == Wd1770ReadWordKind.Data) times.Add(clock);
        };
        var words = Prefix(mfm);
        var previous = false;
        for (var value = 0; value < 8; value++) words.Add(Encode(0x5a, mfm, ref previous));
        var cellClocks = mfm ? 16 : 32;
        foreach (var word in words)
            for (var bit = 15; bit >= 0; bit--)
                for (var cellClock = 0; cellClock < cellClocks; cellClock++)
                {
                    channel.Tick((word >> bit & 1) != 0 && cellClock < 2);
                    clock++;
                }
        Assert.Equal(8, times.Count);
        for (var index = 1; index < times.Count; index++) Assert.Equal(clocksPerByte, times[index] - times[index - 1]);
    }

    private static List<(byte, Wd1770ReadWordKind, bool)> Observe(bool mfm, IEnumerable<ushort> words,
        int prefixCells = 0, int jitter = 0, bool drift = false)
    {
        var channel = new Wd1770ReadChannel { DoubleDensity = mfm };
        var received = new List<(byte, Wd1770ReadWordKind, bool)>();
        channel.ByteReceived += (value, kind, valid) => received.Add((value, kind, valid));
        Feed(channel, mfm, words, prefixCells, jitter, drift);
        return received;
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(12)]
    [InlineData(15)]
    public void WriteSpliceRecoversTheCellPhaseBeforeTheNextAddressMark(int phaseClocks)
    {
        var channel = new Wd1770ReadChannel();
        var received = new List<(byte Value, Wd1770ReadWordKind Kind)>();
        channel.ByteReceived += (value, kind, _) => received.Add((value, kind));
        var before = Prefix(true);
        var previous = false;
        for (var index = 0; index < 22; index++) before.Add(Encode(0x4e, true, ref previous));
        var after = new List<(byte Value, bool Format)>();
        for (var index = 0; index < 12; index++) after.Add((0, false));
        for (var index = 0; index < 3; index++) after.Add((0xf5, true));
        after.Add((0xfb, true));
        for (var index = 0; index < 256; index++) after.Add(((byte)(index ^ 0xa5), false));
        var pulses = new List<int>();
        AddPulses(before, 32);
        var splice = 32 + before.Count * 256 + phaseClocks;
        var writer = new Wd1770WriteChannel { PrecompensationEnabled = true };
        writer.WriteDataChanged += (time, high) => { if (high) pulses.Add(splice + (int)(time / 2)); };
        writer.SetGate(true);
        for (var index = 0; index < after.Count; index++)
        {
            var item = after[index];
            Assert.True(item.Format ? writer.TryLoadFormat(item.Value) : writer.TryLoadData(item.Value));
            if (index + 1 < after.Count) writer.SetLookAhead(after[index + 1].Value);
            do { writer.Tick(); } while (!writer.CanLoad);
        }
        for (var clock = 0; clock < 16; clock++) writer.Tick();
        writer.SetGate(false);
        var next = 0;
        for (var clock = 0; clock < splice + after.Count * 256 + 64; clock++)
        {
            while (next < pulses.Count && clock >= pulses[next] + 2) next++;
            channel.Tick(next < pulses.Count && clock >= pulses[next]);
        }
        void AddPulses(IEnumerable<ushort> words, int start)
        {
            var cell = 0;
            foreach (var word in words)
                for (var bit = 15; bit >= 0; bit--, cell++)
                    if ((word >> bit & 1) != 0) pulses.Add(start + cell * 16);
        }
        var marker = received.FindIndex(item => item.Value == 0xfb && item.Kind == Wd1770ReadWordKind.AddressMark);
        Assert.True(marker >= 0, $"Splice phase {phaseClocks}");
        Assert.Equal(Enumerable.Range(0, 256).Select(index => (byte)(index ^ 0xa5)),
            received.Skip(marker + 1).Take(256).Select(item => item.Value));
    }

    private static List<ushort> Prefix(bool mfm)
    {
        if (!mfm) return [0xaaaa, 0xf57e];
        var previous = true;
        return [0x4489, 0x4489, 0x4489, Encode(0xfe, true, ref previous)];
    }

    private static ushort Encode(byte data, bool mfm, ref bool previous)
    {
        var word = 0;
        for (var bit = 7; bit >= 0; bit--)
        {
            var value = (data >> bit & 1) != 0;
            word = word << 1 | (!mfm || !previous && !value ? 1 : 0);
            word = word << 1 | (value ? 1 : 0);
            previous = value;
        }
        return (ushort)word;
    }

    private static void Feed(Wd1770ReadChannel channel, bool mfm, IEnumerable<ushort> words,
        int prefixCells = 0, int jitter = 0, bool drift = false)
    {
        var cellClocks = mfm ? 16 : 32;
        var pulseWidth = mfm ? 2 : 4;
        var bits = Enumerable.Range(0, prefixCells).Select(index => index % 3 != 0).ToList();
        foreach (var word in words)
            for (var bit = 15; bit >= 0; bit--) bits.Add((word >> bit & 1) != 0);
        var pulses = new List<int>();
        var pulseIndex = 0;
        for (var cell = 0; cell < bits.Count; cell++)
            if (bits[cell])
            {
                var nominal = (cell + 2) * cellClocks;
                pulses.Add(nominal + (drift ? nominal / 1024 : 0) + (pulseIndex++ % 2 == 0 ? jitter : -jitter));
            }
        var next = 0;
        var nominalEnd = (bits.Count + 5) * cellClocks;
        // La fin du banc doit subir la même dérive que son dernier front RD.
        var end = nominalEnd + (drift ? nominalEnd / 1024 : 0);
        for (var clock = 0; clock < end; clock++)
        {
            while (next < pulses.Count && clock >= pulses[next] + pulseWidth) next++;
            channel.Tick(next < pulses.Count && clock >= pulses[next]);
        }
    }
}
