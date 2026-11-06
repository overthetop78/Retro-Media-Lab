using RetroMediaLab.Chips.CPU.Common.Interfaces;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip65C102.Services;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip65C12.Services;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;

namespace RetroMediaLab.Chips.Tests.CPU.MOS.MCS6500.Chip65C12;

public sealed class ClockPhaseTests
{
    [Fact]
    public void DirectPhi2InputCanBeStretchedWithoutInventingAReadyPinOrRepeatingTransfers()
    {
        var bus = new Bus();
        bus.Memory[0xfffd] = 2;
        bus.Memory[0x200] = 0xea;
        var cpu = new Cpu65C12(bus, 2_000_000);
        bus.Cpu = cpu;
        cpu.Reset();
        bus.Transfers.Clear();
        bus.Preparations = 0;
        var levels = new List<bool>();
        cpu.ClockOutputChanged = levels.Add;
        var cycles = cpu.Cycles;
        cpu.SetClockInput(false);
        Assert.Equal(0x200, bus.Address);
        cpu.SetClockInput(false);
        Assert.Equal(cycles, cpu.Cycles);
        cpu.SetClockInput(true);
        cpu.SetClockInput(true);
        Assert.Equal(cycles, cpu.Cycles);
        Assert.Empty(bus.Transfers);
        cpu.SetClockInput(false);
        Assert.Equal(cycles + 1, cpu.Cycles);
        Assert.Single(bus.Transfers);
        Assert.False(cpu.AtInstructionBoundary);
        cpu.SetClockInput(true);
        cpu.SetClockInput(false);
        Assert.True(cpu.AtInstructionBoundary);
        Assert.Equal(cycles + 2, cpu.Cycles);
        Assert.Equal(new[] { true, false, true, false }, levels);
        Assert.Throws<InvalidOperationException>(cpu.Step);
    }

    [Fact]
    public void ExternalOscillatorEdgesDriveTheAdvancedDividerInsideTheChip()
    {
        var (cpu, bus) = Start(0xea);
        var cycles = cpu.Cycles;
        cpu.SetClockInput(false);
        for (var period = 0; period < 4; period++)
        {
            cpu.SetClockInput(true);
            cpu.SetClockInput(true);
            cpu.SetClockInput(false);
        }
        Assert.Equal(cycles + 1, cpu.Cycles);
        Assert.Single(bus.Transfers);
        Assert.False(cpu.InputClockHigh);
        Assert.False(cpu.Phase2High);
        Assert.False(cpu.Phase4High);
        Assert.Throws<InvalidOperationException>(cpu.Clock);
    }

    [Fact]
    public void DirectClockOutputIsAlsoProducedByInstructionEntryPointWhenConnected()
    {
        var bus = new Bus();
        bus.Memory[0xfffd] = 2;
        bus.Memory[0x200] = 0xea;
        var cpu = new Cpu65C12(bus, 2_000_000);
        bus.Cpu = cpu;
        cpu.Reset();
        var outputs = new List<bool>();
        cpu.ClockOutputChanged = outputs.Add;
        cpu.Step();
        Assert.Equal(new[] { true, false, true, false }, outputs);
        Assert.True(cpu.AtInstructionBoundary);
    }

    [Fact]
    public void ObservingTheClockDoesNotChangeWhenANotifiedSoEdgeIsCaptured()
    {
        var (whole, expected) = Start(0xea);
        var (partial, actual) = Start(0xea);
        whole.SetOverflowInput(false);
        partial.SetOverflowInput(false);
        expected.OnTick = () => whole.SetOverflowInput(true);
        actual.OnTick = () => partial.SetOverflowInput(true);
        partial.ClockOutputsChanged = (_, _) => { };
        whole.Clock(); partial.Clock();
        Assert.Equal(0, whole.P & 0x40);
        Assert.Equal(whole.P, partial.P);
        whole.Clock(); partial.Clock();
        Assert.Equal(0x40, whole.P & 0x40);
        Assert.Equal(whole.P, partial.P);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ObservingQuarterClocksPreservesEveryOpcodeIncludingDecimalAndIndexedAccesses(bool decimalMode)
    {
        for (var opcode = 0; opcode <= byte.MaxValue; opcode++)
        {
            byte[] program = [0xa2, 1, 0xa0, 1, 0xa9, 0x81, decimalMode ? (byte)0xf8 : (byte)0xd8, (byte)opcode, 0xff, 0x10];
            var (whole, expected) = Start(program);
            var (partial, actual) = Start(program);
            foreach (var bus in new[] { expected, actual })
            {
                bus.Memory[0xff] = 0xff;
                bus.Memory[0] = 0x10;
                bus.Memory[0x10ff] = bus.Memory[0x1100] = 0x69;
            }
            for (var instruction = 0; instruction < 4; instruction++) { whole.Step(); partial.Step(); }
            expected.Transfers.Clear(); actual.Transfers.Clear();
            partial.ClockOutputsChanged = (_, _) => { };
            whole.Step(); partial.Step();
            Assert.Equal((whole.PC, whole.A, whole.X, whole.Y, whole.S, whole.P, whole.Cycles),
                (partial.PC, partial.A, partial.X, partial.Y, partial.S, partial.P, partial.Cycles));
            Assert.Equal(expected.Transfers, actual.Transfers);
            Assert.Equal(expected.Memory, actual.Memory);
        }
    }

    [Fact]
    public void AdvancedClockPresentsTheAddressThenRaisesPhi4BeforePhi2AndCapturesAtTheEnd()
    {
        var (cpu, bus) = Start(0xea);
        var clocks = cpu.InputClocks;
        var cycles = cpu.Cycles;
        var outputs = new List<(long Clock, bool Phi2, bool Phi4)>();
        cpu.ClockOutputsChanged = (phi2, phi4) => outputs.Add((cpu.InputClocks - clocks, phi2, phi4));
        cpu.AdvanceInputClocks(1);
        Assert.Equal(0x200, bus.Address);
        Assert.Empty(bus.Transfers);
        Assert.False(cpu.Phase2High);
        Assert.True(cpu.Phase4High);
        cpu.AdvanceInputClocks(1);
        Assert.True(cpu.Phase2High);
        Assert.True(cpu.Phase4High);
        Assert.Equal(cycles, cpu.Cycles);
        cpu.AdvanceInputClocks(1);
        Assert.True(cpu.Phase2High);
        Assert.False(cpu.Phase4High);
        cpu.AdvanceInputClocks(1);
        Assert.False(cpu.Phase2High);
        Assert.False(cpu.Phase4High);
        Assert.Equal(cycles + 1, cpu.Cycles);
        Assert.Equal(new[] { (1L, false, true), (2L, true, true), (3L, true, false), (4L, false, false) }, outputs);
        Assert.Equal(clocks + 4, Assert.Single(bus.Transfers).Clock);
        Assert.Equal(1, bus.Preparations);
    }

    [Fact]
    public void SoIsConsumedAtThePhi1TrailingEdgeRatherThanAtReception()
    {
        var (cpu, _) = Start(0xea);
        cpu.SetOverflowInput(false);
        cpu.ClockOutputsChanged = (_, _) => { };
        cpu.AdvanceInputClocks(1);
        cpu.SetOverflowInput(true);
        Assert.Equal(0, cpu.P & 0x40);
        cpu.AdvanceInputClocks(1); // Rising PHI2, trailing PHI1.
        Assert.Equal(0x40, cpu.P & 0x40);
        cpu.AdvanceInputClocks(2);
    }

    [Fact]
    public void InstructionAndQuarterClockEntryPointsProduceTheSamePhasesTransfersAndState()
    {
        byte[] program = [0xf8, 0xa9, 0x99, 0x38, 0x69, 1, 0x85, 0x80, 0xe6, 0x80, 0xda, 0xfa];
        var (whole, expected) = Start(program);
        var (partial, actual) = Start(program);
        var expectedOutputs = new List<(long, bool, bool)>();
        var actualOutputs = new List<(long, bool, bool)>();
        whole.ClockOutputsChanged = (p2, p4) => expectedOutputs.Add((whole.InputClocks, p2, p4));
        partial.ClockOutputsChanged = (p2, p4) => actualOutputs.Add((partial.InputClocks, p2, p4));
        for (var instruction = 0; instruction < 8; instruction++) whole.Step();
        while (partial.Cycles < whole.Cycles) partial.AdvanceInputClocks(1);
        Assert.True(partial.AtInstructionBoundary);
        Assert.Equal((whole.A, whole.X, whole.Y, whole.P, whole.S, whole.PC, whole.Cycles),
            (partial.A, partial.X, partial.Y, partial.P, partial.S, partial.PC, partial.Cycles));
        Assert.Equal(expected.Memory, actual.Memory);
        Assert.Equal(expected.Transfers, actual.Transfers);
        Assert.Equal(expectedOutputs, actualOutputs);
        Assert.Equal(expected.Preparations, actual.Preparations);
    }

    [Fact]
    public void AnObservedClockStillReturnsAfterAReadyHoldAndKeepsAllFourInputPeriods()
    {
        var (cpu, bus) = Start(0xea);
        cpu.ClockOutputsChanged = (_, _) => { };
        cpu.SetReady(false);
        var cycles = cpu.Cycles;
        var clocks = cpu.InputClocks;
        cpu.Step();
        Assert.Equal(cycles + 1, cpu.Cycles);
        Assert.Equal(clocks + 4, cpu.InputClocks);
        Assert.False(cpu.AtInstructionBoundary);
        cpu.SetReady(true);
        cpu.Step();
        Assert.True(cpu.AtInstructionBoundary);
        Assert.Equal(cycles + 3, cpu.Cycles);
        Assert.Equal(3, bus.Preparations);
    }

    [Fact]
    public void ResetWithAClockReceiverEmitsTheSameSixBusCyclesWithoutRecursion()
    {
        var (cpu, bus) = Start(0xea);
        var outputs = 0;
        cpu.ClockOutputsChanged = (_, _) => outputs++;
        var cycles = cpu.Cycles;
        cpu.Reset();
        Assert.Equal(cycles + 6, cpu.Cycles);
        Assert.Equal(24, outputs);
        Assert.Equal(6, bus.Transfers.Count);
        Assert.Equal(3, bus.Transfers.Count(transfer => transfer.Writing));
        Assert.Equal(0x200, cpu.PC);
    }

    private static (Cpu65C102 Cpu, Bus Bus) Start(params byte[] program)
    {
        var bus = new Bus();
        bus.Memory[0xfffd] = 2;
        program.CopyTo(bus.Memory, 0x200);
        var cpu = new Cpu65C102(bus, 4_000_000);
        bus.Cpu = cpu;
        cpu.Reset();
        bus.Transfers.Clear();
        bus.Preparations = 0;
        return (cpu, bus);
    }

    private readonly record struct Transfer(long Clock, ushort Address, byte Value, bool Writing);

    private sealed class Bus : ICpuBus, ICpuBusCyclePins
    {
        internal byte[] Memory { get; } = new byte[ushort.MaxValue + 1];
        internal List<Transfer> Transfers { get; } = [];
        internal Cpu6502 Cpu { get; set; } = null!;
        internal ushort Address { get; private set; }
        internal int Preparations { get; set; }
        internal Action? OnTick { get; set; }
        public bool Irq => false;
        public bool Nmi => false;
        public void Tick() => OnTick?.Invoke();
        public byte Read(ushort address) { var value = Memory[address]; Transfers.Add(new(Cpu.InputClocks, address, value, false)); return value; }
        public void Write(ushort address, byte value) { Memory[address] = value; Transfers.Add(new(Cpu.InputClocks, address, value, true)); }
        public void PrepareRead(ushort address) { Address = address; Preparations++; }
        public void PrepareWrite(ushort address, byte value) { Address = address; Preparations++; }
    }
}
