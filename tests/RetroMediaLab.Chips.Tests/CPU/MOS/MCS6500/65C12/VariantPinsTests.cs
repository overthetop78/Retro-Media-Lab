using RetroMediaLab.Chips.CPU.Common.Interfaces;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip65C12.Services;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip65C102.Services;

namespace RetroMediaLab.Chips.Tests.CPU.MOS.MCS6500.Chip65C12;

public sealed class VariantPinsTests
{
    private const long MasterFrequency = 2_000_000;
    private const long TurboFrequency = 4_000_000;

    [Fact]
    public void DirectClockHasNoReadyPinAndDbeOnlyReleasesWriteData()
    {
        var bus = new Bus();
        var cpu = new Cpu65C12(bus, MasterFrequency);
        Load(cpu, bus, 0xa9, 0x63, 0x85, 0x80);
        bus.Ready = false;
        cpu.Step();
        cpu.SetDataBusEnable(false);
        var before = cpu.Cycles;
        cpu.Step();
        Assert.Equal(3, cpu.Cycles - before);
        Assert.True(cpu.AtInstructionBoundary);
        Assert.Equal(0x63, cpu.A);
        Assert.Equal(0, bus.Memory[0x80]);
        Assert.Equal((ushort)0x80, Assert.Single(bus.Undriven));
        Assert.False(cpu.DataBusEnabled);
        Assert.Equal(MasterFrequency, cpu.ClockFrequency);
        Assert.Equal(MasterFrequency, cpu.InputClockFrequency);
        Assert.Equal(cpu.Cycles, cpu.InputClocks);
        Assert.Throws<NotSupportedException>(() => cpu.SetReady(false));
    }

    [Fact]
    public void ExplicitNmosFrequencyAndInputPeriodsBelongToTheCpuInstance()
    {
        var bus = new Bus();
        var cpu = new Cpu6502(bus, MasterFrequency);
        Load(cpu, bus, 0xea);
        var before = cpu.Cycles;
        cpu.AdvanceInputClocks(2);
        Assert.Equal(before + 2, cpu.Cycles);
        Assert.Equal(MasterFrequency, cpu.ClockFrequency);
        Assert.Equal(MasterFrequency, cpu.InputClockFrequency);
        Assert.Equal(cpu.Cycles, cpu.InputClocks);
        Assert.Throws<ArgumentOutOfRangeException>(() => new Cpu6502(bus, 0L));
    }

    [Fact]
    public void AdvancedClockKeepsItsDividerPhaseAndNeverInventsADbePin()
    {
        var bus = new Bus();
        var cpu = new Cpu65C102(bus, TurboFrequency);
        Load(cpu, bus, 0xa9, 0x63, 0x85, 0x80);
        var before = cpu.Cycles;
        var input = cpu.InputClocks;
        cpu.AdvanceInputClocks(3);
        Assert.Equal(before, cpu.Cycles);
        Assert.Equal(input + 3, cpu.InputClocks);
        Assert.Throws<InvalidOperationException>(cpu.Step);
        Assert.Throws<InvalidOperationException>(cpu.Reset);
        cpu.AdvanceInputClocks(1);
        Assert.Equal(before + 1, cpu.Cycles);
        Assert.False(cpu.AtInstructionBoundary);
        cpu.AdvanceInputClocks(4);
        Assert.Equal(0x63, cpu.A);
        bus.DataBusEnabled = false;
        cpu.Step();
        Assert.Equal(0x63, bus.Memory[0x80]);
        Assert.Empty(bus.Undriven);
        Assert.Equal(TurboFrequency * 4, cpu.InputClockFrequency);
        Assert.Equal(cpu.Cycles * 4, cpu.InputClocks);
        Assert.Throws<ArgumentOutOfRangeException>(() => cpu.AdvanceInputClocks(-1));
    }

    [Fact]
    public void BusEnableIsImmediateAndDoesNotStopTheInternalClockOrAccessMemory()
    {
        var bus = new Bus();
        var cpu = new Cpu65C102(bus, TurboFrequency);
        Load(cpu, bus, 0xa9, 0x63, 0x85, 0x80);
        cpu.Step();
        cpu.Clock(); // STA opcode remains decoded when the external bus is released.
        var changes = new List<bool>();
        cpu.BusEnableChanged = changes.Add;
        var presented = bus.Presented;
        var transfers = bus.Transfers.Count;
        cpu.SetBusEnable(false);
        cpu.SetBusEnable(false);
        Assert.Equal(1, bus.Releases);
        var before = cpu.Cycles;
        cpu.Clock(); // Floating operand, not a read of address PC.
        cpu.Clock(); // Write phase advances internally, without driving any bus output.
        Assert.Equal(before + 2, cpu.Cycles);
        Assert.True(cpu.AtInstructionBoundary);
        Assert.Equal(transfers, bus.Transfers.Count);
        Assert.Equal(presented, bus.Presented);
        Assert.Empty(bus.Undriven);
        Assert.Equal(0, bus.Memory[0xff]);
        cpu.SetBusEnable(true);
        cpu.SetBusEnable(true);
        Assert.Equal(new[] { false, true }, changes);
        Assert.Equal(presented + 1, bus.Presented);
    }

    [Fact]
    public void BusEnableReleasedDuringTheClockSuppressesThePendingTransfer()
    {
        var bus = new Bus();
        var cpu = new Cpu65C102(bus, TurboFrequency);
        Load(cpu, bus, 0xea);
        bus.OnTick = () => cpu.SetBusEnable(false);
        cpu.Clock();
        Assert.Empty(bus.Transfers);
        Assert.Equal(1, bus.Releases);
    }

    [Fact]
    public void MemoryLockCoversModifyAndWriteIncludingReadyWaitsWithoutRepeatedNotifications()
    {
        var bus = new Bus();
        var cpu = new Cpu65C102(bus, TurboFrequency);
        Load(cpu, bus, 0xe6, 0x80);
        bus.Memory[0x80] = 9;
        var changes = new List<(long Tick, bool Locked)>();
        cpu.MemoryLockChanged = locked => changes.Add((cpu.Cycles, locked));
        var before = cpu.Cycles;
        cpu.Clock(); cpu.Clock(); cpu.Clock();
        bus.Ready = false;
        cpu.Clock(); cpu.Clock();
        Assert.True(cpu.MemoryLocked);
        Assert.Single(changes);
        bus.Ready = true;
        cpu.Clock(); cpu.Clock();
        Assert.False(cpu.MemoryLocked);
        Assert.Equal(new[] { (before + 3, true), (before + 7, false) }, changes);
        Assert.Equal(10, bus.Memory[0x80]);
        Assert.Single(bus.Transfers, transfer => transfer.Writing);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GteResetWritesPreviousPcAndStatusThenReadsTheVectorInSixCycles(bool advanced)
    {
        var bus = new Bus();
        Cpu6502 cpu = advanced ? new Cpu65C102(bus, TurboFrequency) : new Cpu65C12(bus, MasterFrequency);
        Load(cpu, bus, 0xf8, 0xea);
        cpu.Step();
        var pc = cpu.PC;
        var stack = cpu.S;
        var flags = cpu.P;
        var before = cpu.Cycles;
        bus.Transfers.Clear();
        cpu.Reset();
        Assert.Equal(6, cpu.Cycles - before);
        Assert.Equal(new Transfer[]
        {
            new(pc, bus.Memory[pc], false),
            new((ushort)(0x100 | stack), (byte)(pc >> 8), true),
            new((ushort)(0x100 | (byte)(stack - 1)), (byte)pc, true),
            new((ushort)(0x100 | (byte)(stack - 2)), (byte)((flags | 0x20) & ~0x10), true),
            new(0xfffc, 0, false), new(0xfffd, 2, false)
        }, bus.Transfers);
        Assert.Equal(4, cpu.P & 12);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SingleCycleAndInstructionEntryPointsProduceIdenticalCmosTransfersForEveryOpcode(bool advanced)
    {
        for (var opcode = 0; opcode <= byte.MaxValue; opcode++)
        {
            var expectedBus = new Bus();
            var actualBus = new Bus();
            Cpu6502 expected = advanced ? new Cpu65C102(expectedBus, TurboFrequency) : new Cpu65C12(expectedBus, MasterFrequency);
            Cpu6502 actual = advanced ? new Cpu65C102(actualBus, TurboFrequency) : new Cpu65C12(actualBus, MasterFrequency);
            Load(expected, expectedBus, (byte)opcode, 0xff, 0x10);
            Load(actual, actualBus, (byte)opcode, 0xff, 0x10);
            var before = expected.Cycles;
            expected.Step();
            for (var cycle = before; cycle < expected.Cycles; cycle++) actual.Clock();
            Assert.True(actual.AtInstructionBoundary);
            Assert.Equal((expected.PC, expected.A, expected.X, expected.Y, expected.S, expected.P, expected.Cycles),
                (actual.PC, actual.A, actual.X, actual.Y, actual.S, actual.P, actual.Cycles));
            Assert.Equal(expectedBus.Memory, actualBus.Memory);
            Assert.Equal(expectedBus.Transfers, actualBus.Transfers);
        }
    }

    private static void Load(Cpu6502 cpu, Bus bus, params byte[] program)
    {
        bus.Memory[0xfffd] = 2;
        program.CopyTo(bus.Memory, 0x200);
        cpu.Reset();
        bus.Transfers.Clear();
        bus.Presented = 0;
    }

    private readonly record struct Transfer(ushort Address, byte Data, bool Writing);

    private sealed class Bus : ICpuBus, ICpuBusCyclePins
    {
        internal byte[] Memory { get; } = new byte[ushort.MaxValue + 1];
        internal List<Transfer> Transfers { get; } = [];
        internal List<ushort> Undriven { get; } = [];
        internal int Presented { get; set; }
        internal int Releases { get; private set; }
        internal Action? OnTick { get; set; }
        public bool Ready { get; set; } = true;
        public bool DataBusEnabled { get; set; } = true;
        public bool Irq => false;
        public bool Nmi => false;
        public void Tick() => OnTick?.Invoke();
        public byte Read(ushort address) { var value = Memory[address]; Transfers.Add(new(address, value, false)); return value; }
        public void Write(ushort address, byte value) { Memory[address] = value; Transfers.Add(new(address, value, true)); }
        public void WriteUndriven(ushort address) => Undriven.Add(address);
        public void PrepareRead(ushort address) => Presented++;
        public void PrepareWrite(ushort address, byte data) => Presented++;
        public void ReleaseBus() => Releases++;
    }
}
