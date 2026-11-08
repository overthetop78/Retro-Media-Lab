using RetroMediaLab.Chips.CPU.Common.Interfaces;
using RetroMediaLab.Chips.CPU.Motorola.M6800.Chip6809.Interfaces;
using RetroMediaLab.Chips.CPU.Motorola.M6800.Chip6809.Services;

namespace RetroMediaLab.Chips.Tests.CPU.Motorola.M6800.Chip6809;

/// <summary>Motorola MC6809 figures 6, 8, 9 and 12–14, without an external ROM.</summary>
public sealed class Cpu6809PinsTests
{
    [Fact]
    public void ConnectedClockEmitsFourOrderedEdgesPerBusCycle()
    {
        var bus = new SourceBus();
        var cpu = Start(bus, 0x12);
        var edges = new List<(long Clock, bool E, bool Q)>();
        cpu.ClockOutputsChanged = (e, q) => edges.Add((cpu.InputClockCount, e, q));
        var start = cpu.InputClockCount;
        var cycles = cpu.Cycles;
        cpu.Step();
        Assert.Equal(8, cpu.InputClockCount - start);
        Assert.Equal(2, cpu.Cycles - cycles);
        Assert.Equal(Enumerable.Range(0, 2).SelectMany(cycle => new[]
        {
            (start + cycle * 4 + 1, false, true),
            (start + cycle * 4 + 2, true, true),
            (start + cycle * 4 + 3, true, false),
            (start + cycle * 4 + 4, false, false)
        }), edges);
        Assert.Equal(cpu.InputClockCount, bus.SourceClocks);
        Assert.Equal(0, bus.LegacyClocks);
    }

    [Fact]
    public void LegacyClockHasIdenticalTimestampWithAndWithoutClockObserver()
    {
        static (long Clock, bool E, bool Q)[] Run(bool observe)
        {
            var bus = new Bus();
            var cpu = Start(bus, 0x12);
            if (observe) cpu.ClockOutputsChanged = (_, _) => { };
            var samples = new List<(long, bool, bool)>();
            bus.OnTick = () => samples.Add((cpu.InputClockCount, cpu.ClockE, cpu.ClockQ));
            cpu.Step();
            return samples.ToArray();
        }
        Assert.Equal(Run(true), Run(false));
    }

    [Fact]
    public void RepeatedReadPreparesEachTransactionButNotifiesOnlyChangedPins()
    {
        var bus = new Bus();
        var cpu = Start(bus, 0x12, 0x12);
        var changes = new List<ushort?>();
        cpu.BusPinsChanged = (address, _, _) => changes.Add(address);
        cpu.Step(); cpu.Step();
        Assert.Equal(new ushort[] { 0x1000, 0x1001, 0x1001, 0x1002 }, bus.PreparedReads);
        Assert.Equal(bus.PreparedReads, bus.Reads);
        Assert.Equal(new ushort?[] { 0x1000, 0x1001, 0x1002 }, changes);
    }

    [Fact]
    public void HaltCompletesInstructionThenReleasesBusUntilOneDeadReacquisitionCycle()
    {
        var bus = new Bus();
        var cpu = Start(bus, 0xb7, 0x20, 0, 0x12);
        bus.OnTick = () => cpu.SetHalt(true);
        cpu.Step();
        Assert.Equal(0x1003, cpu.PC);
        Assert.Single(bus.Writes);
        Assert.False(cpu.BusAvailable);
        bus.OnTick = null;
        var active = cpu.ProcessorCycles;
        cpu.Step();
        Assert.True(cpu.BusAvailable);
        Assert.True(cpu.BusStatus);
        Assert.Null(cpu.BusAddress);
        Assert.Null(cpu.BusRead);
        Assert.Null(cpu.BusOutputData);
        Assert.Equal(active, cpu.ProcessorCycles);
        var transfers = bus.Reads.Count;
        cpu.SetHalt(false);
        cpu.Step();
        Assert.False(cpu.BusAvailable);
        Assert.Null(cpu.BusAddress); // Drivers remain released throughout the dead cycle.
        Assert.Equal(transfers, bus.Reads.Count);
        cpu.Step();
        Assert.Equal(0x1004, cpu.PC);
    }

    [Fact]
    public void MrdyStretchesEHighQLowAndPerformsPendingReadOnlyOnce()
    {
        var bus = new SourceBus();
        var cpu = Start(bus, 0x86, 0x55);
        var start = cpu.InputClockCount;
        cpu.SetMemoryReady(false);
        var stretched = new List<(bool E, bool Q)>();
        bus.OnSourceClock = () =>
        {
            var elapsed = cpu.InputClockCount - start;
            if (elapsed >= 5 && elapsed <= 8) stretched.Add((cpu.ClockE, cpu.ClockQ));
            if (elapsed == 8) cpu.SetMemoryReady(true);
            Assert.True(elapsed < 40, "MRDY did not release the pending cycle.");
        };
        var active = cpu.ProcessorCycles;
        cpu.Step();
        Assert.Equal(0x55, cpu.A);
        Assert.Equal(12, cpu.InputClockCount - start);
        Assert.Equal(2, cpu.ProcessorCycles - active);
        Assert.Equal(4, stretched.Count);
        Assert.All(stretched, level => Assert.Equal((true, false), level));
        Assert.Equal(new ushort[] { 0x1000, 0x1001 }, bus.Reads);
        Assert.Equal(new ushort[] { 0x1000, 0x1001 }, bus.PreparedReads);
    }

    [Fact]
    public void ResetDuringMrdyCancelsPreparedTransferAndWaitsForRelease()
    {
        var bus = new SourceBus();
        var cpu = Start(bus, 0xb7, 0x20, 0);
        var start = cpu.InputClockCount;
        cpu.SetMemoryReady(false);
        bus.OnSourceClock = () =>
        {
            if (cpu.InputClockCount == start + 5) cpu.SetReset(true);
            Assert.True(cpu.InputClockCount - start < 50);
        };
        cpu.Step();
        Assert.Empty(bus.Reads);
        Assert.Empty(bus.Writes);
        Assert.Single(bus.PreparedReads);
        cpu.SetMemoryReady(true);
        bus.OnSourceClock = null;
        cpu.Step();
        Assert.Empty(bus.Reads);
        cpu.SetReset(false);
        cpu.Step();
        Assert.Equal(0x1000, cpu.PC);
        Assert.Equal(new ushort[] { 0xfffe, 0xffff }, bus.Reads);
    }

    [Fact]
    public void HeldResetDrivesFffeThenThreeSettlingCyclesBeforeVectorFetch()
    {
        var bus = new Bus();
        var cpu = Start(bus, 0x12);
        var addresses = new List<ushort?>();
        bus.OnTick = () => addresses.Add(cpu.BusAddress);
        cpu.SetReset(true);
        cpu.Step(); cpu.Step();
        Assert.Equal(new ushort?[] { 0xfffe, 0xfffe }, addresses);
        Assert.Empty(bus.Reads);
        Assert.Empty(bus.PreparedReads);
        Assert.False(cpu.BusAvailable);
        Assert.False(cpu.BusStatus);
        cpu.SetReset(false);
        addresses.Clear();
        var cycles = cpu.Cycles;
        cpu.Step();
        Assert.Equal(6, cpu.Cycles - cycles);
        Assert.Equal(new ushort?[] { 0xfffe, 0xfffe, 0xfffe, 0xfffe, 0xffff, 0xffff }, addresses);
        Assert.Equal(new ushort[] { 0xfffe, 0xffff }, bus.Reads);
        Assert.Equal(bus.Reads, bus.PreparedReads);
        Assert.Equal(0x1000, cpu.PC);
    }

    [Fact]
    public void HaltKeepsResetBusReleasedUntilHaltIsRemoved()
    {
        var bus = new Bus();
        var cpu = Start(bus, 0x12);
        cpu.SetHalt(true);
        cpu.Step();
        cpu.SetReset(true);
        cpu.Step(); cpu.Step();
        Assert.True(cpu.BusAvailable);
        Assert.True(cpu.BusStatus);
        Assert.Null(cpu.BusAddress);
        cpu.SetReset(false);
        cpu.Step();
        Assert.Empty(bus.Reads);
        cpu.SetHalt(false);
        cpu.Step();
        Assert.False(cpu.BusAvailable);
        Assert.False(cpu.BusStatus);
        Assert.Equal(0x1000, cpu.PC);
        Assert.Equal(new ushort[] { 0xfffe, 0xffff }, bus.Reads);
    }

    [Fact]
    public void ResetAcknowledgementPublishesOnlyTheTwoVectorCycles()
    {
        var bus = new Bus();
        var cpu = new Cpu6809(bus);
        bus.Memory[0xfffe] = 0x10;
        var status = new List<(bool BA, bool BS)>();
        var vector = new List<ushort>();
        cpu.BusStatusChanged = (ba, bs) => status.Add((ba, bs));
        bus.OnRead = address =>
        {
            Assert.False(cpu.BusAvailable);
            Assert.True(cpu.BusStatus);
            vector.Add(address);
        };
        cpu.Reset();
        Assert.Equal(new[] { (false, true), (false, false) }, status);
        Assert.Equal(new ushort[] { 0xfffe, 0xffff }, vector);
        Assert.Equal(7, cpu.Cycles);
    }

    [Fact]
    public void ShortDmaGrantPreservesRemainingInstructionAndDoesNotRepeatTransfers()
    {
        var bus = new Bus();
        var cpu = Start(bus, 0x86, 0x55);
        var status = new List<(long Clock, bool BA, bool BS)>();
        cpu.BusStatusChanged = (ba, bs) => status.Add((cpu.Cycles, ba, bs));
        bus.OnTick = () =>
        {
            if (cpu.BusAvailable) cpu.SetDmaRequest(false);
            else if (bus.Reads.Count == 0) cpu.SetDmaRequest(true);
        };
        var active = cpu.ProcessorCycles;
        var cycles = cpu.Cycles;
        cpu.Step();
        Assert.Equal(0x55, cpu.A);
        Assert.Equal(2, cpu.ProcessorCycles - active);
        Assert.Equal(4, cpu.Cycles - cycles); // Two CPU cycles and the two dead cycles.
        Assert.Equal(new ushort[] { 0x1000, 0x1001 }, bus.Reads);
        Assert.Equal(new[] { (true, true), (false, false) }, status.Select(s => (s.BA, s.BS)));
        Assert.Equal(1, bus.Releases);
    }

    [Fact]
    public void SustainedDmaReturnsOneCpuCycleBetweenFourteenCycleBursts()
    {
        var bus = new Bus();
        var cpu = Start(bus, 0x86, 0x55);
        var status = new List<(long Cycle, bool BA)>();
        cpu.BusStatusChanged = (ba, _) => status.Add((cpu.Cycles, ba));
        cpu.SetDmaRequest(true);
        var cycles = cpu.Cycles;
        var active = cpu.ProcessorCycles;
        cpu.Step();
        Assert.Equal(18, cpu.Cycles - cycles);
        Assert.Equal(2, cpu.ProcessorCycles - active);
        Assert.Equal(15, status[1].Cycle - status[0].Cycle); // Dead entry + fourteen DMA cycles.
        Assert.Equal(0x55, cpu.A);
        cpu.Step();
        Assert.True(status.Count >= 4);
        Assert.Equal(2, status[2].Cycle - status[1].Cycle); // Dead exit + one CPU cycle.
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void IrqAndFirqAreRecognizedAfterQFallingSynchronization(bool fast)
    {
        var bus = new Bus();
        var cpu = Start(bus, 0x10, 0xce, 0x30, 0, 0x1c, 0xaf, 0x12);
        bus.Memory[fast ? 0xfff6 : 0xfff8] = 0x20;
        bus.Memory[fast ? 0xfff7 : 0xfff9] = 0;
        cpu.Step(); cpu.Step();
        if (fast) cpu.SetFirq(true); else cpu.SetIrq(true);
        cpu.Step();
        Assert.Equal(0x1007, cpu.PC);
        cpu.Step();
        Assert.Equal(0x2000, cpu.PC);
        Assert.Equal(fast ? 0x2ffd : 0x2ff4, cpu.S);
    }

    [Fact]
    public void NmiEdgeDuringResetIsNotLatchedAfterAcknowledge()
    {
        var bus = new Bus();
        var cpu = Start(bus, 0x10, 0xce, 0x30, 0, 0x12, 0x12);
        bus.Memory[0xfffc] = 0x20;
        bus.Memory[0xfffd] = 0;
        cpu.SetReset(true);
        cpu.Step(); cpu.Step();
        cpu.SetNmi(true);
        cpu.SetReset(false);
        cpu.Step();
        cpu.Step(); cpu.Step();
        Assert.Equal(0x1005, cpu.PC);
        cpu.SetNmi(false);
        cpu.Step();
        cpu.SetNmi(true);
        cpu.Step(); cpu.Step();
        Assert.Equal(0x2000, cpu.PC);
    }

    private static Cpu6809 Start(Bus bus, params byte[] program)
    {
        Array.Fill(bus.Memory, (byte)0x12);
        program.CopyTo(bus.Memory, 0x1000);
        bus.Memory[0xfffe] = 0x10;
        bus.Memory[0xffff] = 0;
        var cpu = new Cpu6809(bus);
        cpu.Reset();
        bus.Reads.Clear(); bus.PreparedReads.Clear(); bus.Writes.Clear();
        return cpu;
    }

    private class Bus : ICpuBus, ICpuBusCyclePins
    {
        internal readonly byte[] Memory = new byte[65536];
        internal readonly List<ushort> Reads = [];
        internal readonly List<ushort> PreparedReads = [];
        internal readonly List<(ushort Address, byte Data)> Writes = [];
        internal Action? OnTick;
        internal Action<ushort>? OnRead;
        internal int Releases;
        internal int LegacyClocks;
        public bool Irq => throw new InvalidOperationException("IRQ must arrive through its input.");
        public bool Nmi => throw new InvalidOperationException("NMI must arrive through its input.");
        public byte Read(ushort address) { Reads.Add(address); OnRead?.Invoke(address); return Memory[address]; }
        public void Write(ushort address, byte data) { Writes.Add((address, data)); Memory[address] = data; }
        public void Tick() { LegacyClocks++; OnTick?.Invoke(); }
        public void PrepareRead(ushort address) => PreparedReads.Add(address);
        public void PrepareWrite(ushort address, byte data) { }
        public void ReleaseBus() => Releases++;
    }

    private sealed class SourceBus : Bus, ICpu6809ClockBus
    {
        internal long SourceClocks;
        internal Action? OnSourceClock;
        public void TickInputClock() { SourceClocks++; OnSourceClock?.Invoke(); }
    }
}
