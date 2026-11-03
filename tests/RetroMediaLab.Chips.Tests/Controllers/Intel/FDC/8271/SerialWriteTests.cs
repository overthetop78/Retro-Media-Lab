using RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Intel.FDC.Chip8271;

public sealed class SerialWriteTests
{
    [Theory]
    [InlineData(2_000_000, false)]
    [InlineData(4_000_000, false)]
    [InlineData(2_000_000, true)]
    [InlineData(4_000_000, true)]
    public void DmaWriteHasTheSame124ControllerClockDeadline(int clock, bool acknowledge)
    {
        using var wire = new Wire(clock: clock) { ServiceRequests = false };
        wire.StartWrite();
        for (var tick = 0; tick < 4096 && wire.FirstRequestTick is null; tick++) wire.Tick();
        Assert.NotNull(wire.FirstRequestTick);
        wire.Ticks(123 - (wire.TickCount - wire.FirstRequestTick.Value));
        Assert.True(wire.Chip.Drq);
        Assert.False(wire.Chip.Nmi);
        Assert.True(wire.Gate);
        if (acknowledge) wire.Chip.Write(4, 0x5a);
        wire.Tick();
        if (acknowledge) { Assert.True(wire.Chip.Active); Assert.False(wire.Chip.Nmi); }
        else
        {
            Assert.Equal(0x0a, wire.Result());
            Assert.False(wire.Chip.Drq);
            Assert.False(wire.Gate);
        }
    }
    [Theory]
    [InlineData(0x4a, 0xfb, false)]
    [InlineData(0x4e, 0xf8, false)]
    [InlineData(0x4a, 0xfb, true)]
    public void WritePreservesGap2ThenEmitsSyncMarkPayloadCrcAndPostamble(byte command, byte mark, bool nonDma)
    {
        using var wire = new Wire(nonDma);
        wire.Command(command, 0, 1);
        wire.Field(0xfe, [0, 0, 1, 0]);
        for (var i = 0; i < 10; i++) wire.Byte(0xff);
        Assert.False(wire.Gate);
        wire.Byte(0xff);
        Assert.True(wire.Gate);
        wire.Finish();
        Assert.Equal(0, wire.Result());
        Assert.Equal(128, wire.Written);
        var words = wire.Words();
        Assert.Equal(138, words.Count);
        Assert.All(words.Take(6), word => Assert.Equal((0xff, 0), word));
        Assert.Equal((0xc7, (int)mark), words[6]);
        Assert.Equal(Enumerable.Range(0, 128), words.Skip(7).Take(128).Select(word => word.Data));
        Assert.All(words.Skip(7), word => Assert.Equal(0xff, word.Clocks));
        var crc = Fcs(new[] { mark }.Concat(Enumerable.Range(0, 128).Select(value => (byte)value)));
        Assert.Equal(crc >> 8, words[135].Data);
        Assert.Equal(crc & 255, words[136].Data);
        Assert.Equal(255, words[137].Data);
        Assert.All(wire.Requests, request => Assert.Equal(nonDma, request));
    }

    [Fact]
    public void WriteDoesNotOpenTheGateWhenIdCrcIsBad()
    {
        using var wire = new Wire();
        wire.Command(0x4a, 0, 1);
        wire.Field(0xfe, [0, 0, 1, 0], corrupt: true);
        Assert.Equal(0x0c, wire.Result());
        Assert.False(wire.Gate);
        Assert.Empty(wire.Words());
        Assert.Equal(0, wire.Written);
    }

    [Fact]
    public void WriteUnderflowDropsGateAndReportsLateDma()
    {
        using var wire = new Wire { ServiceRequests = false };
        wire.StartWrite();
        wire.Finish();
        Assert.Equal(0x0a, wire.Result());
        Assert.False(wire.Gate);
        Assert.False(wire.Chip.WriteData);
    }

    [Theory]
    [InlineData(false, 0x12)]
    [InlineData(true, 0x16)]
    public void ProtectOrFaultDuringWritingStopsThePulseTrain(bool fault, byte result)
    {
        using var wire = new Wire();
        wire.StartWrite();
        wire.UntilWords(10);
        if (fault) wire.Chip.SetWriteFault(0, true); else wire.Chip.SetWriteProtected(0, true);
        Assert.Equal(result, wire.Result());
        Assert.False(wire.Gate);
        var count = wire.Rises.Count;
        wire.Ticks(100);
        Assert.Equal(count, wire.Rises.Count);
    }

    [Fact]
    public void HardwareResetStopsWritingAndReleasesTheOutput()
    {
        using var wire = new Wire();
        wire.StartWrite();
        wire.UntilWords(10);
        wire.Chip.SetReset(true);
        Assert.False(wire.Gate);
        Assert.False(wire.Chip.WriteData);
        Assert.False(wire.Chip.Drq);
        Assert.False(wire.Chip.Nmi);
        var count = wire.Rises.Count;
        wire.Ticks(100);
        Assert.Equal(count, wire.Rises.Count);
    }

    [Fact]
    public void FormatWaitsForIndexPreservesAllCpuIdsAndWritesE5UntilTheFinalIndex()
    {
        using var wire = new Wire();
        var ids = new byte[] { 4, 1, 9, 7, 5, 1, 3, 0 };
        wire.Source = index => ids[index];
        wire.Command(0x63, 0, 8, 2, 0, 10);
        wire.Ticks(1000);
        Assert.False(wire.Gate);
        Assert.Equal(0, wire.Written);
        wire.Index();
        wire.UntilWords(339); // deuxième CRC bas commencé, pas encore entièrement écrit.
        wire.Index();
        Assert.True(wire.Gate);
        Assert.True(wire.Chip.Active);
        wire.UntilWords(341); // GAP4 atteint après le dernier CRC.
        Assert.Equal(8, wire.Written);
        Assert.True(wire.Chip.Active);
        wire.Index();
        wire.Finish();
        Assert.Equal(0, wire.Result());
        var words = wire.Words();
        Assert.All(words.Take(10), word => Assert.Equal((0xff, 0xff), word));
        var headers = words.Select((word, index) => (word, index)).Where(item => item.word == (0xc7, 0xfe)).ToArray();
        Assert.Equal(2, headers.Length);
        for (var sector = 0; sector < 2; sector++)
        {
            var offset = headers[sector].index;
            Assert.Equal(ids.Skip(sector * 4).Take(4).Select(value => (int)value), words.Skip(offset + 1).Take(4).Select(word => word.Data));
            var crc = Fcs(new byte[] { 0xfe }.Concat(ids.Skip(sector * 4).Take(4)));
            Assert.Equal(crc >> 8, words[offset + 5].Data);
            Assert.Equal(crc & 255, words[offset + 6].Data);
        }
        var marks = words.Select((word, index) => (word, index)).Where(item => item.word == (0xc7, 0xfb)).ToArray();
        Assert.Equal(2, marks.Length);
        Assert.All(marks, item => Assert.All(words.Skip(item.index + 1).Take(128), word => Assert.Equal((0xff, 0xe5), word)));
        Assert.DoesNotContain(words, word => word == (0xd7, 0xfc)); // GAP5=0 : mini-disquette.
    }

    [Fact]
    public void StandardFormatWritesGap5AndTheIndexMarkWithD7Clocks()
    {
        using var wire = new Wire();
        wire.Command(0x63, 0, 8, 1, 8, 10);
        wire.Index();
        wire.UntilWords(35);
        var words = wire.Words();
        Assert.All(words.Take(8), word => Assert.Equal((0xff, 0xff), word));
        Assert.All(words.Skip(8).Take(6), word => Assert.Equal((0xff, 0), word));
        Assert.Equal((0xd7, 0xfc), words[14]);
        Assert.Equal((0xc7, 0xfe), words[31]);
    }

    [Fact]
    public void IndexDoesNotTruncateAFormatPayloadBeforeAllSectorsAreWritten()
    {
        using var wire = new Wire();
        wire.Command(0x63, 0, 8, 1, 0, 10);
        wire.Index();
        wire.UntilWords(50);
        wire.Index(); wire.Index(); wire.Index();
        Assert.True(wire.Gate);
        Assert.False(wire.Chip.Nmi);
        wire.UntilWords(172);
        wire.Index();
        wire.Finish();
        Assert.Equal(0, wire.Result());
    }

    [Fact]
    public void PulseWidthIsOneControllerClockAndHalfCellsAreEightClocksApart()
    {
        using var wire = new Wire();
        wire.StartWrite();
        wire.UntilWords(2);
        Assert.NotEmpty(wire.Edges);
        for (var i = 0; i + 1 < wire.Edges.Count; i += 2)
        {
            Assert.True(wire.Edges[i].Level);
            Assert.False(wire.Edges[i + 1].Level);
            Assert.Equal(1, wire.Edges[i + 1].Tick - wire.Edges[i].Tick);
        }
        // Les deux premiers octets de synchronisation sont 00 : impulsions clock seules tous les 16 cycles.
        Assert.All(wire.Rises.Zip(wire.Rises.Skip(1)), pair => Assert.Equal(16, pair.Second - pair.First));
    }

    [Theory]
    [InlineData(2_000_000, false)]
    [InlineData(4_000_000, false)]
    [InlineData(2_000_000, true)]
    [InlineData(4_000_000, true)]
    public void WriteGateLeadsAndTrailsThePulseTrainByTwoMicroseconds(int clock, bool nonDma)
    {
        using var wire = new Wire(nonDma, clock);
        wire.StartWrite();
        wire.Finish();
        Assert.Equal(0, wire.Result());
        Assert.Single(wire.GateStarts);
        Assert.Single(wire.GateStops);
        var delay = clock / 500_000;
        Assert.Equal(delay, wire.Rises[0] - wire.GateStarts[0]);
        Assert.Equal(delay, wire.GateStops[0] - wire.Rises[^1]);
        Assert.Equal(138, wire.Words().Count);
    }

    [Theory]
    [InlineData(2_000_000, 0)]
    [InlineData(2_000_000, 1)]
    [InlineData(2_000_000, 7)]
    [InlineData(2_000_000, 15)]
    [InlineData(4_000_000, 0)]
    [InlineData(4_000_000, 1)]
    [InlineData(4_000_000, 7)]
    [InlineData(4_000_000, 15)]
    public void FinalIndexFinishesOnAPulseBoundaryAndPreservesTheWriteGateTail(int clock, int indexOffset)
    {
        using var wire = new Wire(clock: clock);
        wire.Command(0x63, 0, 8, 1, 0, 10);
        wire.Index();
        wire.UntilWords(172);
        wire.Ticks(indexOffset);
        wire.Index();
        Assert.True(wire.Gate);
        wire.Finish();
        Assert.Equal(0, wire.Result());
        var delay = clock / 500_000;
        Assert.Equal(delay, wire.Rises[0] - wire.GateStarts[0]);
        Assert.Equal(delay, wire.GateStops[0] - wire.Rises[^1]);
    }

    private static ushort Fcs(IEnumerable<byte> bytes)
    {
        ushort crc = 0xffff;
        foreach (var value in bytes)
        {
            crc ^= (ushort)(value << 8);
            for (var bit = 0; bit < 8; bit++) crc = (ushort)((crc & 0x8000) != 0 ? (crc << 1) ^ 0x1021 : crc << 1);
        }
        return crc;
    }

    private sealed class Wire : IDisposable
    {
        internal readonly Intel8271 Chip;
        internal readonly List<int> Rises = [];
        internal readonly List<(int Tick, bool Level)> Edges = [];
        internal readonly List<bool> Requests = [];
        internal readonly List<int> GateStarts = [];
        internal readonly List<int> GateStops = [];
        internal bool ServiceRequests = true;
        internal Func<int, byte> Source = index => (byte)index;
        internal int Written;
        private int _tick;
        private int? _lastInputPulse;
        private int? _stopped;
        internal int TickCount => _tick;
        internal int? FirstRequestTick;
        internal bool Gate => (Chip.DriveControl & 1) != 0;

        internal Wire(bool nonDma = false, int clock = 2_000_000)
        {
            Chip = new Intel8271(ticksPerSecond: clock, chipClock: clock);
            Chip.WriteDataChanged += Edge;
            Chip.SetReady(0, true);
            Chip.SetTrackZero(0, true);
            Chip.SetIndex(0, false);
            Command(0x35, 0x0d, 1, 0, 0xf0);
            Command(0x3a, 0x17, nonDma ? (byte)1 : (byte)0);
        }
        public void Dispose()
        {
            try { Chip.SetReset(true); }
            finally { Chip.WriteDataChanged -= Edge; }
        }
        private void Edge(bool level)
        {
            Edges.Add((_tick, level));
            if (level) Rises.Add(_tick);
        }
        internal void Command(byte command, params byte[] parameters)
        {
            Chip.Write(0, command);
            foreach (var value in parameters) Chip.Write(1, value);
            Tick();
        }
        internal void Index()
        {
            var oldGate = Gate;
            Chip.SetIndex(0, true);
            Chip.SetIndex(0, false);
            if (!oldGate && Gate) GateStarts.Add(_tick);
        }
        internal void StartWrite()
        {
            Command(0x4a, 0, 1);
            Field(0xfe, [0, 0, 1, 0]);
            for (var i = 0; i < 11; i++) Byte(0xff);
        }
        internal void Tick()
        {
            _tick++;
            var oldGate = Gate;
            Chip.Tick();
            if (FirstRequestTick is null && (Chip.Drq || (Chip.Read(0) & 4) != 0)) FirstRequestTick = _tick;
            if (!oldGate && Gate) GateStarts.Add(_tick);
            if (oldGate && !Gate) { _stopped = _tick; GateStops.Add(_tick); }
            if (ServiceRequests && (Chip.Drq || (Chip.Read(0) & 4) != 0))
            {
                Requests.Add(Chip.Nmi);
                Chip.Write(4, Source(Written++));
            }
        }
        internal void Ticks(int count) { for (var i = 0; i < count; i++) Tick(); }
        internal void UntilWords(int count)
        {
            for (var i = 0; i < 1_000_000; i++)
            {
                if (Rises.Count != 0 && (_tick - Rises[0]) / 128 >= count) return;
                Tick();
                if ((Chip.Read(0) & 0x90) == 0x10) Assert.Fail("Command completed before the expected output word.");
            }
            Assert.Fail("No expected output word.");
        }
        internal void Finish()
        {
            for (var i = 0; i < 1_000_000; i++)
            {
                if ((Chip.Read(0) & 0x90) == 0x10) return;
                Tick();
            }
            Assert.Fail("Write command did not complete.");
        }
        internal byte Result()
        {
            Assert.Equal(0x10, Chip.Read(0) & 0x90);
            Assert.True(Chip.Nmi);
            return Chip.Read(1);
        }
        internal List<(int Clocks, int Data)> Words()
        {
            var words = new List<(int, int)>();
            if (Rises.Count == 0) return words;
            var origin = Rises[0];
            var pulses = Rises.ToHashSet();
            var length = ((_stopped ?? _tick) - origin + 8) / 128;
            for (var word = 0; word < length; word++)
            {
                var clocks = 0; var data = 0;
                for (var bit = 0; bit < 8; bit++)
                {
                    clocks = (clocks << 1) | (pulses.Contains(origin + word * 128 + bit * 16) ? 1 : 0);
                    data = (data << 1) | (pulses.Contains(origin + word * 128 + bit * 16 + 8) ? 1 : 0);
                }
                words.Add((clocks, data));
            }
            return words;
        }
        internal void Byte(byte data, byte clocks = 0xff)
        {
            for (var bit = 7; bit >= 0; bit--)
            {
                Chip.SetDataWindow(true);
                if ((clocks & (1 << bit)) != 0) Pulse();
                Ticks(8);
                Chip.SetDataWindow(false);
                if ((data & (1 << bit)) != 0) Pulse();
                Ticks(8);
            }
        }
        private void Pulse()
        {
            Chip.SetDataWindow(_lastInputPulse is null || _tick - _lastInputPulse >= 16);
            _lastInputPulse = _tick;
            Chip.SetUnseparatedData(true);
            Chip.SetUnseparatedData(false);
        }
        internal void Field(byte mark, byte[] bytes, bool corrupt = false)
        {
            Byte(255); Byte(0); Byte(0); Byte(mark, 0xc7);
            foreach (var value in bytes) Byte(value);
            var crc = Fcs(new[] { mark }.Concat(bytes));
            Byte((byte)(crc >> 8)); Byte((byte)(crc ^ (corrupt ? 1 : 0)));
        }
    }
}
