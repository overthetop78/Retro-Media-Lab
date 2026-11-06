using RetroMediaLab.Chips.CPU.Common.Enums;
using RetroMediaLab.Chips.CPU.Common.Interfaces;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip65C12.Services;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip65C102.Services;

namespace RetroMediaLab.Chips.Tests.CPU.MOS.MCS6500.Chip65C12;

public sealed class InterruptPinsTests
{
    [Fact]
    public void NotifiedReadyResetAndOverflowNeverReadTheSourceAndKeepTheirHardwareSemantics()
    {
        var bus = new Bus(true);
        bus.Memory[0xfffd] = 2;
        bus.Memory[0x200] = 0xea;
        bus.Memory[0x201] = 0xb8; // NOP, CLV, NOP.
        bus.Memory[0x202] = 0xea;
        var cpu = new Cpu65C102(bus, 4_000_000);
        cpu.SetIrq(false);
        cpu.SetNmi(false);
        cpu.SetReset(false);
        cpu.SetReady(false);
        cpu.SetOverflowInput(false);
        cpu.Reset();
        Assert.Equal(1, cpu.Cycles);
        Assert.False(cpu.AtInstructionBoundary);
        cpu.SetReady(true);
        cpu.Step();
        Assert.Equal(7, cpu.Cycles); // One held cycle plus the six GTE reset cycles.
        Assert.Equal(0x200, cpu.PC);
        cpu.SetOverflowInput(true);
        Assert.Equal(0, cpu.P & 0x40); // Input reception does not run the CPU's sampling phase.
        cpu.Step();
        Assert.Equal(0x40, cpu.P & 0x40);
        cpu.Step();
        Assert.Equal(0, cpu.P & 0x40);
        cpu.SetOverflowInput(true);
        Assert.Equal(0, cpu.P & 0x40);
        cpu.SetOverflowInput(false);
        cpu.SetOverflowInput(true);
        cpu.Step();
        Assert.Equal(0x40, cpu.P & 0x40);
        var transfers = bus.Trace.Count;
        var cycles = cpu.Cycles;
        cpu.SetReset(true);
        cpu.Clock(); cpu.Clock();
        Assert.Equal(cycles + 2, cpu.Cycles);
        Assert.Equal(transfers, bus.Trace.Count);
        cpu.SetReset(false);
        cpu.Step();
        Assert.Equal(0x200, cpu.PC);
        Assert.Equal(cycles + 8, cpu.Cycles);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void NotificationsPreserveEveryInterruptSamplingWindowWithoutConsultingTheSource(bool cmos, bool nmi)
    {
        for (var phase = 1; phase <= 14; phase++)
        for (var width = 1; width <= 3; width++)
        {
            var polled = Run(cmos, nmi, phase, width, false);
            var notified = Run(cmos, nmi, phase, width, true);
            Assert.Equal(polled.State, notified.State);
            Assert.Equal(polled.Bus.Trace, notified.Bus.Trace);
            Assert.Equal(polled.Bus.Memory, notified.Bus.Memory);
        }
    }

    [Fact]
    public void HoldingTheNotifiedNmiLineDoesNotGenerateRepeatedInterrupts()
    {
        var result = Run(true, true, 3, int.MaxValue - 3, true);
        Assert.Single(result.Bus.Trace, access => access.Address == 0xfffa);
    }

    private static ((ushort Pc, byte P, byte S, long Cycles) State, Bus Bus) Run(bool cmos, bool nmi, int phase, int width, bool notified)
    {
        var bus = new Bus(notified);
        bus.Memory[0xfffd] = 2;
        bus.Memory[0xffff] = 3;
        bus.Memory[0xfffb] = 4;
        Array.Fill(bus.Memory, (byte)0xea, 0x200, 0x80);
        bus.Memory[0x200] = 0x58; // CLI, then NOPs; handlers return with RTI.
        bus.Memory[0x300] = bus.Memory[0x400] = 0x40;
        Cpu6502 cpu = cmos ? new Cpu65C12(bus, 2_000_000) : new Cpu6502(bus);
        if (notified) { cpu.SetIrq(false); cpu.SetNmi(false); }
        cpu.Reset();
        bus.Trace.Clear();
        var tick = 0;
        bus.OnTick = () =>
        {
            tick++;
            var active = tick >= phase && tick < phase + width;
            if (nmi)
            {
                if (bus.NmiInput == active) return;
                bus.NmiInput = active;
                if (notified) cpu.SetNmi(active);
            }
            else
            {
                if (bus.IrqInput == active) return;
                bus.IrqInput = active;
                if (notified) cpu.SetIrq(active);
            }
        };
        for (var instruction = 0; instruction < 20; instruction++) cpu.Step();
        return ((cpu.PC, cpu.P, cpu.S, cpu.Cycles), bus);
    }

    private readonly record struct Access(ushort Address, byte Value, bool Writing);

    private sealed class Bus(bool notified) : ICpuBus
    {
        internal byte[] Memory { get; } = new byte[ushort.MaxValue + 1];
        internal List<Access> Trace { get; } = [];
        internal Action? OnTick { get; set; }
        internal bool IrqInput { get; set; }
        internal bool NmiInput { get; set; }
        public CpuBusControlSignals ControlSignals => CpuBusControlSignals.None;
        public bool Irq => notified ? throw new InvalidOperationException("IRQ source queried") : IrqInput;
        public bool Nmi => notified ? throw new InvalidOperationException("NMI source queried") : NmiInput;
        public bool Ready => notified ? throw new InvalidOperationException("RDY source queried") : true;
        public bool ResetRequested => notified ? throw new InvalidOperationException("RESET source queried") : false;
        public bool SetOverflow => notified ? throw new InvalidOperationException("SO source queried") : false;
        public void Tick() => OnTick?.Invoke();
        public byte Read(ushort address) { var value = Memory[address]; Trace.Add(new(address, value, false)); return value; }
        public void Write(ushort address, byte value) { Memory[address] = value; Trace.Add(new(address, value, true)); }
    }
}
