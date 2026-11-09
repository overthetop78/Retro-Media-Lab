using RetroMediaLab.Chips.CPU.Common.Interfaces;
using RetroMediaLab.Chips.Controllers.Acorn.SystemClock.Services;
using RetroMediaLab.Chips.Controllers.Acorn.SystemCpuClock.Services;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip65C12.Services;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip65C102.Services;

namespace RetroMediaLab.Chips.Tests.CPU.MOS.MCS6500.Chip6502;

public sealed class ClockPhaseTests
{
    public static IEnumerable<object[]> Opcodes => Enumerable.Range(0, 256)
        .SelectMany(value => new[] { new object[] { value, false }, new object[] { value, true } });

    [Theory]
    [MemberData(nameof(Opcodes))]
    public void AllOpcodesKeepTheirBusTraceAndRegistersWhenDrivenByLevels(int opcode, bool initialized)
    {
        byte[] instruction = [(byte)opcode, 0xff, 0x20];
        byte[] prefix = initialized ? [0xa2, 0x7f, 0xa0, 0x80, 0xa9, 0x5a, 0xf8, 0x38] : [];
        var program = prefix.Concat(instruction).ToArray();
        var (unit, first) = Start(program);
        var (edges, second) = Start(program);
        edges.SetPhaseZeroInput(false);
        for (var period = 0; period < 40; period++)
        {
            unit.Clock();
            Period(edges);
            AssertRegisters(unit, edges);
            Assert.Equal(first.Transfers, second.Transfers);
        }
        Assert.Equal(first.Memory, second.Memory);
    }

    [Fact]
    public void PreparedWriteAndBothPhasesStayStableUntilTheTrailingEdge()
    {
        var (cpu, bus) = Start(0xa9, 0x5a, 0x8d, 0, 0x20);
        var phases = new List<(bool, bool)>();
        cpu.PhaseOutputsChanged = (one, two) =>
        {
            Assert.Equal(one, cpu.PhaseOneHigh); Assert.Equal(two, cpu.PhaseTwoHigh);
            Assert.NotEqual(one, two);
            phases.Add((one, two));
        };
        cpu.SetPhaseZeroInput(false);
        for (var i = 0; i < 5; i++) Period(cpu);
        Assert.True(bus.Writing); Assert.Equal(0x2000, bus.Address); Assert.Equal(0x5a, bus.Data);
        var cycles = cpu.Cycles;
        var transfers = bus.Transfers.Count;
        var presentations = bus.Presentations;
        for (var i = 0; i < 8; i++) cpu.SetPhaseZeroInput(false);
        cpu.SetPhaseZeroInput(true);
        for (var i = 0; i < 8; i++) cpu.SetPhaseZeroInput(true);
        Assert.False(cpu.PhaseOneHigh); Assert.True(cpu.PhaseTwoHigh);
        Assert.Equal(cycles, cpu.Cycles); Assert.Equal(transfers, bus.Transfers.Count);
        Assert.Equal(presentations, bus.Presentations);
        cpu.SetPhaseZeroInput(false);
        Assert.Equal(cycles + 1, cpu.Cycles);
        Assert.Equal(transfers + 1, bus.Transfers.Count);
        Assert.Equal(0x5a, bus.Memory[0x2000]);
        Assert.True(cpu.PhaseOneHigh); Assert.False(cpu.PhaseTwoHigh);
        Assert.Equal(12, phases.Count);
        Assert.Throws<InvalidOperationException>(cpu.Step);
        Assert.Throws<InvalidOperationException>(cpu.Clock);
        Assert.Throws<InvalidOperationException>(cpu.Reset);
        Assert.Throws<InvalidOperationException>(() => cpu.AdvanceInputClocks(1));
    }

    [Fact]
    public void ReadyRetainsTheReadButDoesNotBlockEitherRmwWrite()
    {
        var (cpu, bus) = Start(0x06, 0x10, 0xea);
        bus.Memory[0x10] = 0x81;
        cpu.SetPhaseZeroInput(false);
        cpu.SetReady(false);
        for (var i = 0; i < 3; i++) Period(cpu);
        Assert.Equal(0x200, cpu.PC); Assert.Equal(3, bus.Transfers.Count);
        Assert.All(bus.Transfers, transfer => Assert.Equal((ushort)0x200, transfer.Address));
        cpu.SetReady(true);
        for (var i = 0; i < 3; i++) Period(cpu);
        Assert.True(bus.Writing);
        cpu.SetReady(false);
        Period(cpu); Period(cpu);
        Assert.Equal(2, bus.Memory[0x10]);
        Assert.Equal(new byte[] { 0x81, 2 }, bus.Transfers.Where(t => t.Write).Select(t => t.Data));
    }

    [Fact]
    public void ResetAssertedDuringHighCancelsTheTransferAndReleaseRunsSevenRealCycles()
    {
        var (cpu, bus) = Start(0xea);
        cpu.SetPhaseZeroInput(false);
        cpu.SetPhaseZeroInput(true);
        cpu.SetReset(true);
        cpu.SetPhaseZeroInput(false);
        Assert.Empty(bus.Transfers);
        for (var i = 0; i < 3; i++) Period(cpu);
        Assert.Empty(bus.Transfers);
        cpu.SetReset(false);
        for (var i = 0; i < 7; i++) Period(cpu);
        Assert.Equal(7, bus.Transfers.Count);
        Assert.Equal((ushort)0xfffc, bus.Transfers[^2].Address);
        Assert.Equal((ushort)0xfffd, bus.Transfers[^1].Address);
        Assert.Equal(0x200, cpu.PC);
        Assert.DoesNotContain(bus.Transfers, transfer => transfer.Write);
    }

    [Fact]
    public void NotifiedInterruptsAndOverflowKeepTheUnitCycleTrace()
    {
        var (unit, first) = Start(0x58, 0xea, 0x4c, 0, 2);
        var (edges, second) = Start(0x58, 0xea, 0x4c, 0, 2);
        first.Memory[0x300] = second.Memory[0x300] = 0x40;
        edges.SetPhaseZeroInput(false);
        for (var i = 0; i < 80; i++)
        {
            unit.SetIrq(i >= 3 && i < 8); edges.SetIrq(i >= 3 && i < 8);
            unit.SetNmi(i == 20); edges.SetNmi(i == 20);
            unit.SetOverflowInput(i == 35); edges.SetOverflowInput(i == 35);
            unit.Clock(); Period(edges);
            AssertRegisters(unit, edges);
            Assert.Equal(first.Transfers, second.Transfers);
        }
        Assert.Equal(first.Memory, second.Memory);
    }

    [Fact]
    public void OverflowArrivingDuringHighWaitsForTheNextRisingPhase()
    {
        var (cpu, _) = Start(0xea, 0xea);
        cpu.SetPhaseZeroInput(false);
        cpu.SetPhaseZeroInput(true);
        cpu.SetOverflowInput(true);
        Assert.Equal(0, cpu.P & 0x40);
        cpu.SetPhaseZeroInput(false);
        Assert.Equal(0, cpu.P & 0x40);
        cpu.SetPhaseZeroInput(true);
        Assert.Equal(0x40, cpu.P & 0x40);
    }

    [Fact]
    public void ObservedStepAndResetEmitTheActualNmosCyclesAndJamStillReturns()
    {
        var (cpu, bus) = Start(0x02);
        var rising = 0;
        cpu.PhaseOutputsChanged = (_, high) => { if (high) rising++; };
        cpu.Step();
        Assert.Equal(bus.Transfers.Count, rising);
        Assert.True(rising > 0);
        bus.Transfers.Clear(); rising = 0;
        cpu.Reset();
        Assert.Equal(7, rising); Assert.Equal(7, bus.Transfers.Count);
        Assert.Equal(0x200, cpu.PC);
    }

    [Fact]
    public void CmosDevicesRejectTheNmosPhysicalInput()
    {
        Assert.Throws<NotSupportedException>(() => new Cpu65C12(new Bus(), 2_000_000).SetPhaseZeroInput(false));
        Assert.Throws<NotSupportedException>(() => new Cpu65C102(new Bus(), 4_000_000).SetPhaseZeroInput(false));
    }

    [Fact]
    public void PhysicalClockAndWaitReadyCircuitSuspendAWriteAndReleaseTheExternalBus()
    {
        var (cpu, bus) = Start(0xa9, 0x5a, 0x8d, 0, 0x20, 0x4c, 5, 2);
        var source = new SystemClockGenerator();
        var control = new SystemCpuClockControl(2_000_000);
        long independentEdges = 0;
        try
        {
            bus.OnPreparation = control.SetCpuRead;
            control.ReadyChanged = high => { bus.BuffersEnabled = high; cpu.SetReady(high); };
            cpu.SetReady(control.Ready); bus.BuffersEnabled = control.BusBuffersEnabled;
            control.CpuClockChanged = cpu.SetPhaseZeroInput;
            cpu.SetPhaseZeroInput(control.CpuClockHigh);
            source.Clock2MHzChanged = control.SetSelectedClock;
            control.SetSelectedClock(source.Clock2MHz);
            source.Clock1MHzChanged = _ => independentEdges++;
            source.AdvanceClocks(60); // LDA and STA address prepared; write still pending.
            Assert.True(bus.Writing);
            Assert.Equal(0x2000, bus.Address);
            var beforeWrite = bus.Transfers.Count;
            var beforeCycles = cpu.Cycles;
            var beforeIndependent = independentEdges;
            control.SetMemoryReady(false);
            source.AdvanceClocks(240);
            Assert.True(cpu.PhaseTwoHigh);
            Assert.Equal(beforeWrite, bus.Transfers.Count);
            Assert.Equal(beforeCycles, cpu.Cycles);
            Assert.True(independentEdges > beforeIndependent);
            control.SetMemoryReady(true);
            source.AdvanceClocks(12);
            Assert.Equal(beforeWrite + 1, bus.Transfers.Count);
            Assert.Equal(0x5a, bus.Memory[0x2000]);
            source.AdvanceClocks(240);
            Assert.Single(bus.Transfers, t => t.Write && t.Address == 0x2000);

            beforeCycles = cpu.Cycles;
            var beforeGrant = bus.Transfers.Count;
            var pc = cpu.PC;
            control.SetBusRequest(false);
            source.AdvanceClocks(240);
            Assert.False(control.Ready); Assert.False(bus.BuffersEnabled);
            Assert.Equal(beforeGrant, bus.Transfers.Count);
            Assert.Equal(pc, cpu.PC);
            Assert.Equal(beforeCycles + 20, cpu.Cycles);
            control.SetBusRequest(true);
            source.AdvanceClocks(24);
            Assert.True(bus.BuffersEnabled);
            Assert.True(bus.Transfers.Count > beforeGrant);
        }
        finally
        {
            source.Clock2MHzChanged = null; source.Clock1MHzChanged = null;
            control.CpuClockChanged = null; control.ReadyChanged = null;
            bus.OnPreparation = null;
        }
    }

    private static void Period(Cpu6502 cpu)
    {
        cpu.SetPhaseZeroInput(true); cpu.SetPhaseZeroInput(false);
    }

    private static void AssertRegisters(Cpu6502 first, Cpu6502 second) =>
        Assert.Equal((first.A, first.X, first.Y, first.S, first.P, first.PC, first.Cycles),
            (second.A, second.X, second.Y, second.S, second.P, second.PC, second.Cycles));

    private static (Cpu6502 Cpu, Bus Bus) Start(params byte[] program)
    {
        var bus = new Bus();
        program.CopyTo(bus.Memory, 0x200);
        var cpu = new Cpu6502(bus, 2_000_000);
        cpu.SetReset(false); cpu.SetReady(true); cpu.SetIrq(false); cpu.SetNmi(false); cpu.SetOverflowInput(false);
        cpu.Reset();
        bus.Transfers.Clear(); bus.Presentations = 0;
        return (cpu, bus);
    }

    private sealed class Bus : ICpuBus, ICpuBusCyclePins
    {
        public byte[] Memory { get; } = new byte[65536];
        public List<(bool Write, ushort Address, byte Data, bool Sync)> Transfers { get; } = [];
        public int Presentations;
        public ushort Address;
        public bool Writing;
        public byte Data;
        public bool Sync;
        public bool BuffersEnabled = true;
        public Action<bool>? OnPreparation;
        public bool Irq => false;
        public bool Nmi => false;
        public Bus()
        {
            Memory[0xfffd] = 2;
            Memory[0xfffb] = Memory[0xffff] = 3;
        }
        public void PrepareRead(ushort address) { Address = address; Writing = false; Presentations++; OnPreparation?.Invoke(true); }
        public void PrepareWrite(ushort address, byte value) { Address = address; Data = value; Writing = true; Presentations++; OnPreparation?.Invoke(false); }
        public byte Read(ushort address)
        {
            Assert.Equal(address, Address); Assert.False(Writing);
            if (!BuffersEnabled) return byte.MaxValue;
            var value = Memory[address]; Transfers.Add((false, address, value, Sync)); return value;
        }
        public void Write(ushort address, byte value)
        {
            Assert.Equal(address, Address); Assert.True(Writing); Assert.Equal(value, Data);
            Assert.True(BuffersEnabled);
            Memory[address] = value; Transfers.Add((true, address, value, Sync));
        }
        public void Tick() { }
        public void SetSync(bool asserted) => Sync = asserted;
    }
}
