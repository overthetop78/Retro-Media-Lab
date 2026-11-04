using RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Interfaces;
using RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Services;

namespace RetroMediaLab.Chips.Tests.Custom.Acorn.Electron.ULA;

public sealed class RamSequencerTests
{
    private sealed class Pins : IElectronRamPins
    {
        internal readonly byte[] Memory = new byte[65536];
        internal readonly List<(int Clock, byte Address, bool Ras, bool Cas, bool We, byte Data)> Trace = [];
        internal readonly List<int> Columns = [];
        internal readonly List<byte> Rows = [];
        internal int Clock = -1;
        private bool _ras = true;
        private bool _cas = true;
        private int _row;
        public byte Data { get; set; }

        public void Drive(byte address, bool rowStrobeHigh, bool columnStrobeHigh, bool writeEnableHigh, byte data)
        {
            Trace.Add((Clock, address, rowStrobeHigh, columnStrobeHigh, writeEnableHigh, data));
            if (_ras && !rowStrobeHigh)
            {
                _row = address;
                Rows.Add(address);
            }
            if (_cas && !columnStrobeHigh)
            {
                Assert.False(rowStrobeHigh);
                var physical = (_row << 8) | address;
                Columns.Add(physical);
                if (!writeEnableHigh) Memory[physical] = (byte)(data & 15);
                Data = Memory[physical];
            }
            _ras = rowStrobeHigh;
            _cas = columnStrobeHigh;
        }
    }

    private static int Row(int address)
    {
        int[] wires = [3, 4, 5, 6, 7, 2, 1, 8];
        var row = 0;
        for (var bit = 0; bit < wires.Length; bit++) row |= ((address >> wires[bit]) & 1) << bit;
        return row;
    }

    private static int Column(int address, bool odd)
    {
        int[] wires = [9, 10, 11, 12, 13, 14, 0];
        var column = odd ? 128 : 0;
        for (var bit = 0; bit < wires.Length; bit++) column |= ((address >> wires[bit]) & 1) << bit;
        return column;
    }

    private static byte Group(int data, int parity)
    {
        var pins = 0;
        for (var bit = 0; bit < 4; bit++) pins |= ((data >> (2 * bit + parity)) & 1) << bit;
        return (byte)pins;
    }

    private static void Tick(ElectronRamSequencer ram, Pins pins, int clock)
    {
        pins.Clock = clock;
        ram.Tick();
    }

    [Fact]
    public void ReadUsesTwoFallingCasEdgesAndSeparateDataLatches()
    {
        var pins = new Pins();
        var ram = new ElectronRamSequencer(pins);
        const ushort address = 0x5367;
        var row = Row(address);
        var even = Column(address, false);
        var odd = Column(address, true);
        pins.Memory[(row << 8) | even] = 0x0f;
        pins.Memory[(row << 8) | odd] = 0;
        ram.Begin(address, cpu: false);
        for (var clock = 0; clock < 8; clock++)
        {
            Tick(ram, pins, clock);
            Assert.Equal(clock == 3, ram.EvenLoaded);
            Assert.Equal(clock == 6, ram.OddLoaded);
            Assert.Equal(clock == 7, ram.Completed);
            Assert.Equal(clock < 7, ram.Busy);
            Assert.Equal(clock < 3 ? 0 : 0x55, ram.Data);
        }
        Assert.Equal(new[] { (row << 8) | even, (row << 8) | odd }, pins.Columns);
        Assert.Equal(new[]
        {
            (-1, (byte)row, true, true, true, (byte)0),
            (0, (byte)row, false, true, true, (byte)0),
            (1, (byte)even, false, false, true, (byte)0),
            (3, (byte)odd, false, true, true, (byte)0),
            (4, (byte)odd, false, false, true, (byte)0),
            (6, (byte)row, true, true, true, (byte)0)
        }, pins.Trace);
        Tick(ram, pins, 8);
        Assert.False(ram.Completed);
        Assert.Equal(6, pins.Trace.Count);
    }

    [Theory]
    [InlineData(0x0000)]
    [InlineData(0x0123)]
    [InlineData(0x5367)]
    [InlineData(0x7fff)]
    public void EveryByteWritesAndReadsBothPhysicalGroupsWithoutChangingNeighbours(int address)
    {
        var pins = new Pins();
        var ram = new ElectronRamSequencer(pins);
        var even = (Row(address) << 8) | Column(address, false);
        var odd = (Row(address) << 8) | Column(address, true);
        Array.Fill(pins.Memory, (byte)0x0a);
        for (var data = 0; data < 256; data++)
        {
            ram.Begin((ushort)address, cpu: true, write: true, data: (byte)data);
            for (var clock = 0; clock < 8; clock++) Tick(ram, pins, clock);
            Assert.Equal(Group(data, 0), pins.Memory[even]);
            Assert.Equal(Group(data, 1), pins.Memory[odd]);
            Assert.True(pins.Trace[^1].Ras && pins.Trace[^1].Cas && pins.Trace[^1].We);
            ram.Begin((ushort)address, cpu: true);
            for (var clock = 0; clock < 8; clock++) Tick(ram, pins, clock);
            Assert.Equal(data, ram.Data);
            pins.Trace.Clear();
            pins.Columns.Clear();
            pins.Rows.Clear();
        }
        for (var physical = 0; physical < pins.Memory.Length; physical++)
            if (physical != even && physical != odd) Assert.Equal(0x0a, pins.Memory[physical]);
    }

    [Theory]
    [InlineData(false, 0x0000)]
    [InlineData(false, 0x7fff)]
    [InlineData(false, 0xffff)]
    [InlineData(true, 0x8000)]
    [InlineData(true, 0xffff)]
    public void VideoOrNonRamCpuSelectionCannotAssertWriteEnable(bool cpu, int address)
    {
        var pins = new Pins();
        var ram = new ElectronRamSequencer(pins);
        Array.Fill(pins.Memory, (byte)0x03);
        ram.Begin((ushort)address, cpu, write: true, data: 0xff);
        for (var clock = 0; clock < 8; clock++) Tick(ram, pins, clock);
        Assert.All(pins.Trace, point => Assert.True(point.We));
        Assert.All(pins.Memory, value => Assert.Equal(0x03, value));
        Assert.Equal(0x0f, ram.Data);
    }

    [Fact]
    public void EachLoadSamplesItsOwnLiveInputAndKeepsTheOtherGroup()
    {
        var pins = new Pins();
        var ram = new ElectronRamSequencer(pins);
        ram.Begin(0x3456, cpu: false);
        for (var clock = 0; clock < 3; clock++) Tick(ram, pins, clock);
        pins.Data = 15;
        Tick(ram, pins, 3);
        Assert.Equal(0x55, ram.Data);
        Tick(ram, pins, 4);
        Tick(ram, pins, 5);
        pins.Data = 0x0a;
        Tick(ram, pins, 6);
        Assert.Equal(0xdd, ram.Data);
        Tick(ram, pins, 7);
        ram.Begin(0x3456, cpu: false);
        Tick(ram, pins, 0);
        Assert.Equal(0xdd, ram.Data);
        Assert.Throws<InvalidOperationException>(() => ram.Begin(0, cpu: true));
    }

    [Fact]
    public void CancelReleasesPinsAndPreservesOnlyWritesWhoseCasEdgeAlreadyOccurred()
    {
        for (var stop = 0; stop < 8; stop++)
        {
            var pins = new Pins();
            var ram = new ElectronRamSequencer(pins);
            ram.Begin(0, cpu: true, write: true, data: 0xff);
            for (var clock = 0; clock < stop; clock++) Tick(ram, pins, clock);
            ram.Cancel();
            Assert.False(ram.Busy);
            Assert.False(ram.EvenLoaded || ram.OddLoaded || ram.Completed);
            var last = pins.Trace[^1];
            Assert.True(last.Ras && last.Cas && last.We);
            Assert.Equal(stop >= 2 ? 15 : 0, pins.Memory[0]);
            Assert.Equal(stop >= 5 ? 15 : 0, pins.Memory[128]);
            var count = pins.Trace.Count;
            for (var clock = 0; clock < 16; clock++) ram.Tick();
            Assert.Equal(count, pins.Trace.Count);
        }
    }

    [Fact]
    public void RefreshSelectsEachRowWithoutAnyCasStrobeDataLoadOrWrite()
    {
        var pins = new Pins();
        var ram = new ElectronRamSequencer(pins);
        Array.Fill(pins.Memory, (byte)0x07);
        for (var row = 0; row < 256; row++)
        {
            ram.BeginRefresh((byte)row);
            for (var clock = 0; clock < 8; clock++)
            {
                Tick(ram, pins, clock);
                Assert.False(ram.EvenLoaded || ram.OddLoaded);
            }
            Assert.True(ram.Completed);
        }
        Assert.Equal(Enumerable.Range(0, 256).Select(row => (byte)row), pins.Rows);
        Assert.Empty(pins.Columns);
        Assert.All(pins.Trace, point => Assert.True(point.Cas && point.We));
        Assert.All(pins.Memory, value => Assert.Equal(7, value));
        Assert.Equal(0, ram.Data);
    }
}
