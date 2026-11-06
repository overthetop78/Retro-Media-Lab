using RetroMediaLab.Chips.CPU.Common.Interfaces;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip65C12.Services;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip65C102.Services;

namespace RetroMediaLab.Chips.Tests.CPU.MOS.MCS6500.Chip65C12;

public sealed class BusCycleTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EveryOpcodeResumesAtEveryReadAndWriteWithoutReplayingItsEffects(bool decimalMode)
    {
        for (var opcode = 0; opcode <= byte.MaxValue; opcode++)
        {
            var (reference, expected) = Start((byte)opcode, decimalMode);
            var start = reference.Cycles;
            reference.Step();
            var duration = reference.Cycles - start;
            var trace = expected.Trace.ToArray();
            for (var phase = 0; phase < duration; phase++)
            {
                var (cpu, bus) = Start((byte)opcode, decimalMode);
                var heldAt = cpu.Cycles + phase + 1;
                bus.OnTick = tick => bus.Ready = tick < heldAt || tick >= heldAt + 2;
                var calls = 0;
                do
                {
                    cpu.Step();
                    Assert.True(++calls <= 4, $"Opcode {opcode:X2}, phase {phase}: no progress after RDY");
                }
                while (!cpu.AtInstructionBoundary);
                Assert.Equal(duration + 2, cpu.Cycles - start);
                Assert.Equal((reference.A, reference.X, reference.Y, reference.S, reference.P, reference.PC),
                    (cpu.A, cpu.X, cpu.Y, cpu.S, cpu.P, cpu.PC));
                Assert.Equal(expected.Memory, bus.Memory);
                Assert.Equal(trace, bus.Trace);
            }
        }
    }

    [Theory]
    [InlineData(0x8d)]
    [InlineData(0xee)]
    [InlineData(0x20)]
    [InlineData(0x00)]
    public void ResetDuringEachPhaseAbortsTheTransferAndRestartsThroughTheVector(int opcode)
    {
        var (reference, _) = Start((byte)opcode);
        var start = reference.Cycles;
        reference.Step();
        var duration = reference.Cycles - start;
        for (var phase = 0; phase < duration; phase++)
        {
            var (cpu, bus) = Start((byte)opcode);
            var resetAt = cpu.Cycles + phase + 1;
            bus.OnTick = tick => { if (tick == resetAt) bus.ResetRequested = true; };
            cpu.Step();
            Assert.Equal(resetAt, cpu.Cycles);
            Assert.False(cpu.AtInstructionBoundary);
            var accesses = bus.Trace.Count;
            cpu.Step(); cpu.Step();
            Assert.Equal(accesses, bus.Trace.Count);
            bus.OnTick = null;
            bus.ResetRequested = false;
            bus.Trace.Clear();
            start = cpu.Cycles;
            cpu.Step();
            Assert.Equal(6, cpu.Cycles - start);
            Assert.True(cpu.AtInstructionBoundary);
            Assert.Equal(0x0200, cpu.PC);
            Assert.Equal(4, cpu.P & 12);
            Assert.Equal(3, bus.Trace.Count(item => item.Writing));
            Assert.Equal(new ushort[] { 0xfffc, 0xfffd }, bus.Trace.TakeLast(2).Select(x => x.Address));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NmiWaitsForCmosBrkButCanRedirectAHardwareIrq(bool hardwareIrq)
    {
        var (cpu, bus) = Start(hardwareIrq ? (byte)0xea : (byte)0);
        if (hardwareIrq)
        {
            // CLI followed by a NOP supplies the documented penultimate-cycle poll.
            bus.Memory[cpu.PC] = 0x58;
            bus.Memory[cpu.PC + 1] = 0xea;
            cpu.Step();
            bus.Irq = true;
            cpu.Step();
        }
        var start = cpu.Cycles;
        bus.OnTick = tick => { if (tick == start + 3) bus.Nmi = true; };
        cpu.Step();
        Assert.Equal(hardwareIrq ? 0x4000 : 0x3000, cpu.PC);
        Assert.Equal(7, cpu.Cycles - start);
        bus.OnTick = null;
        if (!hardwareIrq)
        {
            cpu.Step();
            Assert.Equal(0x4000, cpu.PC);
        }
    }

    [Fact]
    public void DataEnableCanReleaseAWriteWithoutDroppingItsClock()
    {
        var (cpu, bus) = Start(0x8d, directClock: true);
        var before = cpu.Cycles;
        bus.DataBusEnabled = false;
        cpu.Step();
        Assert.Equal(4, cpu.Cycles - before);
        Assert.Equal(0x10ff, Assert.Single(bus.Undriven));
        Assert.Equal(0x69, bus.Memory[0x10ff]);
    }

    [Theory]
    [InlineData(0x44, 3, 2)]
    [InlineData(0x54, 4, 2)]
    [InlineData(0x5c, 8, 3)]
    public void LongNopsPerformTheirOperandCyclesRatherThanClockOnlyPadding(int opcode, int cycles, int length)
    {
        var (cpu, bus) = Start((byte)opcode);
        var pc = cpu.PC;
        var before = cpu.Cycles;
        cpu.Step();
        Assert.Equal(cycles, cpu.Cycles - before);
        Assert.Equal(pc + length, cpu.PC);
        Assert.Equal(cycles, bus.Trace.Count);
        Assert.All(bus.Trace.Skip(1).Take(cycles - length), x => Assert.Equal(pc + 1, x.Address));
    }

    private static (Cpu6502 Cpu, Bus Bus) Start(byte opcode, bool decimalMode = false, bool directClock = false)
    {
        var bus = new Bus();
        bus.Memory[0xfffd] = 2;
        bus.Memory[0xffff] = 0x30;
        bus.Memory[0xfffb] = 0x40;
        byte[] setup = [0xa2, 1, 0xa0, 1, 0xa9, 0x81, decimalMode ? (byte)0xf8 : (byte)0xd8];
        setup.CopyTo(bus.Memory, 0x200);
        var at = 0x200 + setup.Length;
        bus.Memory[at] = opcode;
        bus.Memory[at + 1] = 0xff;
        bus.Memory[at + 2] = 0x10;
        bus.Memory[0xff] = 0xff;
        bus.Memory[0] = 0x10;
        bus.Memory[0x10ff] = bus.Memory[0x1100] = 0x69;
        Cpu6502 cpu = directClock ? new Cpu65C12(bus, 2_000_000) : new Cpu65C102(bus, 4_000_000);
        cpu.Reset();
        for (var instruction = 0; instruction < 4; instruction++) cpu.Step();
        bus.Trace.Clear();
        return (cpu, bus);
    }

    private readonly record struct Transfer(ushort Address, byte Data, bool Writing, bool Sync);

    private sealed class Bus : ICpuBus
    {
        internal byte[] Memory { get; } = new byte[65536];
        internal List<Transfer> Trace { get; } = [];
        internal List<ushort> Undriven { get; } = [];
        internal Action<long>? OnTick { get; set; }
        private long _ticks;
        private bool _sync;
        public bool Ready { get; set; } = true;
        public bool ResetRequested { get; set; }
        public bool DataBusEnabled { get; set; } = true;
        public bool Irq { get; set; }
        public bool Nmi { get; set; }
        public void Tick() { _ticks++; OnTick?.Invoke(_ticks); }
        public void SetSync(bool asserted) => _sync = asserted;
        public byte Read(ushort address)
        {
            var value = Memory[address];
            if (Ready) Trace.Add(new(address, value, false, _sync));
            return value;
        }
        public void Write(ushort address, byte value)
        {
            Memory[address] = value;
            Trace.Add(new(address, value, true, _sync));
        }
        public void WriteUndriven(ushort address) => Undriven.Add(address);
    }
}
