using RetroMediaLab.Chips.CPU.Common.Interfaces;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;

namespace RetroMediaLab.Chips.Tests.CPU.MOS.MCS6500.Chip65C12;

/// <summary>Différences du 65C12 utilisé par le Master : manuel Acorn, chapitre P.</summary>
public sealed class Chip65C12Tests
{
    [Fact]
    public void IndirectJumpCarriesIntoNextPageAndTakesSixCycles()
    {
        var (cpu, bus) = Start(0x6c, 0xff, 0x30);
        bus.Memory[0x30ff] = 0x34;
        bus.Memory[0x3000] = 0x12;
        bus.Memory[0x3100] = 0x56;
        var before = cpu.Cycles;
        cpu.Step();
        Assert.Equal(0x5634, cpu.PC);
        Assert.Equal(6, cpu.Cycles - before);
        Assert.Equal(new ushort[] { 0x30ff, 0x3000, 0x3100 }, bus.Reads.TakeLast(3));
    }

    [Theory]
    [InlineData(0x1e, 0x10, 6)]
    [InlineData(0x3e, 0x10, 6)]
    [InlineData(0x5e, 0x10, 6)]
    [InlineData(0x7e, 0x10, 6)]
    [InlineData(0x1e, 0xff, 7)]
    [InlineData(0xde, 0x10, 7)]
    [InlineData(0xfe, 0xff, 7)]
    public void IndexedReadModifyWriteUsesCmosTimingAndOnlyOneWrite(int opcode, int low, int cycles)
    {
        var (cpu, bus) = Start(0xa2, 1, (byte)opcode, (byte)low, 0x30);
        cpu.Step();
        var before = cpu.Cycles;
        bus.Writes.Clear();
        cpu.Step();
        Assert.Equal(cycles, cpu.Cycles - before);
        Assert.Equal((ushort)(0x3000 + low + 1), Assert.Single(bus.Writes));
    }

    [Fact]
    public void DecimalSubtractUsesCmosCorrectionForInvalidDigits()
    {
        var (cpu, _) = Start(0xf8, 0x38, 0xa9, 0, 0xe9, 0x0f);
        cpu.Step(); cpu.Step(); cpu.Step();
        var before = cpu.Cycles;
        cpu.Step();
        Assert.Equal(0x8b, cpu.A);
        Assert.Equal(3, cpu.Cycles - before);
        Assert.Equal(0x80, cpu.P & 0x82);
    }

    [Fact]
    public void IndexedLoadRereadsInstructionOperandWhileCarrying()
    {
        var (cpu, bus) = Start(0xa2, 1, 0xbd, 0xff, 0x30);
        bus.Memory[0x3100] = 0x69;
        cpu.Step();
        bus.Reads.Clear();
        cpu.Step();
        Assert.Equal(new ushort[] { 0x202, 0x203, 0x204, 0x204, 0x3100 }, bus.Reads);
        Assert.Equal(0x69, cpu.A);
    }

    [Fact]
    public void IndirectIndexedStoreDoesNotReadItsDestination()
    {
        var (cpu, bus) = Start(0x91, 0x10);
        bus.Memory[0x10] = 0;
        bus.Memory[0x11] = 0x30;
        bus.Reads.Clear();
        cpu.Step();
        Assert.Equal(new ushort[] { 0x200, 0x201, 0x10, 0x11, 0x201 }, bus.Reads);
        Assert.Equal(0x3000, Assert.Single(bus.Writes));
    }

    [Fact]
    public void CmosNopDoesNotJamOrApplyNmosCombinedOperation()
    {
        var (cpu, bus) = Start(0x07, 0xea);
        bus.Memory[0] = 0x80;
        cpu.Step();
        Assert.Equal(0x201, cpu.PC);
        cpu.Step();
        Assert.Equal(0x202, cpu.PC);
        Assert.Equal(0x80, bus.Memory[0]);
        Assert.Empty(bus.Writes);
    }

    [Fact]
    public void Every65C12OpcodeIsDefinedWithoutAddingRockwellOrWdcInstructions()
    {
        for (var opcode = 0; opcode <= byte.MaxValue; opcode++)
        {
            var (cpu, _) = Start((byte)opcode, 0, 3);
            var before = cpu.Cycles;
            cpu.Step();
            Assert.InRange(cpu.Cycles - before, 1, 8);
        }
    }

    private static (Cpu6502 Cpu, Bus Bus) Start(params byte[] program)
    {
        var bus = new Bus();
        bus.Memory[0xfffd] = 2;
        program.CopyTo(bus.Memory, 0x200);
        var cpu = new Cpu6502(bus, cmos: true);
        cpu.Reset();
        bus.Reads.Clear();
        bus.Writes.Clear();
        return (cpu, bus);
    }

    private sealed class Bus : ICpuBus
    {
        internal byte[] Memory { get; } = new byte[65536];
        internal List<ushort> Writes { get; } = [];
        internal List<ushort> Reads { get; } = [];
        public bool Irq => false;
        public bool Nmi => false;
        public byte Read(ushort address) { Reads.Add(address); return Memory[address]; }
        public void Write(ushort address, byte value) { Writes.Add(address); Memory[address] = value; }
        public void Tick() { }
    }
}
