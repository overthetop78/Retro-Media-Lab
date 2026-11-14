using System.Buffers.Binary;
using RetroMediaLab.Chips.CPU.Motorola.M68000.Execution;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Instruction;

namespace RetroMediaLab.Chips.Tests.SegaEmu;

public sealed class InstructionCpuTests
{
    [Fact]
    public void M68000ExecutesArithmeticAndAbsoluteMemoryWrite()
    {
        M68000Bus bus = new();
        bus.Long(0, 0x8000); bus.Long(4, 0x100);
        byte[] program = [0x70, 0x05, 0x52, 0x80, 0x23, 0xc0, 0, 0, 2, 0, 0x4e, 0x72, 0x27, 0];
        program.CopyTo(bus.Memory, 0x100);
        Cpu68000<M68000Bus> cpu = new(bus); bus.Cpu = cpu;
        try
        {
            cpu.Reset(); cpu.Run(10000);
            Assert.Equal(6u, cpu.GetRegister(Cpu68000Register.D0));
            Assert.Equal(6u, bus.ReadLong(0x200));
            Assert.NotEqual(0u, cpu.State.stopped);
        }
        finally { cpu.Halt(); bus.Cpu = null; }
    }

    [Fact]
    public void M68000InterruptWakesStoppedCpuAndReturnsThroughVector()
    {
        M68000Bus bus = new();
        bus.Long(0, 0x8000); bus.Long(4, 0x100); bus.Long(26 * 4, 0x300);
        new byte[] {0x4e,0x72,0x20,0,0x60,0xfe}.CopyTo(bus.Memory, 0x100);
        new byte[] {0x72,9,0x4e,0x73}.CopyTo(bus.Memory, 0x300);
        Cpu68000<M68000Bus> cpu = new(bus); bus.Cpu = cpu;
        try
        {
            cpu.Reset(); cpu.Run(1000); Assert.NotEqual(0u, cpu.State.stopped);
            cpu.SetIrq(2); cpu.Run(4000);
            Assert.Equal(9u, cpu.GetRegister(Cpu68000Register.D1));
            Assert.Equal(2u, bus.AcknowledgedLevel);
            Assert.Equal(0x104u, cpu.GetRegister(Cpu68000Register.PC));
        }
        finally { cpu.Halt(); bus.Cpu = null; }
    }

    [Fact]
    public void Z80ResumesSnapshotAndPerformsMappedFetchesAndBusWrites()
    {
        Z80Bus bus = new();
        new byte[] {0x3e,7,0x32,0,0x20,0x3c,0x76}.CopyTo(bus.Memory, 0);
        CpuZ80<Z80Bus> cpu = new(bus, static _ => 0xff);
        CpuZ80<Z80Bus>? restored = null;
        try
        {
            cpu.Reset(); cpu.Run(7 * 15);
            CpuZ80State saved = cpu.Context;
            Assert.Equal(7, saved.af.High);
            cpu.Run(2000);
            Assert.Equal(7, bus.Memory[0x2000]); Assert.Equal(8, cpu.Context.af.High);
            Assert.NotEqual(0, cpu.Context.halt);
            bus.Memory[0x2000] = 0;
            restored = new(bus, static _ => 0xff) { Context = saved };
            restored.Run(2000);
            Assert.Equal(7, bus.Memory[0x2000]); Assert.Equal(cpu.Context.af.Word, restored.Context.af.Word);
            Assert.Equal(cpu.Context.pc.Word, restored.Context.pc.Word);
        }
        finally
        {
            cpu.Cycles = uint.MaxValue; cpu.State.irq_callback = null;
            if (restored is not null) { restored.Cycles = uint.MaxValue; restored.State.irq_callback = null; }
        }
    }

    [Fact]
    public void Z80NmiPushesReturnAddressAndRunsHandler()
    {
        Z80Bus bus = new();
        new byte[] {0x31,0,0x80,0x76}.CopyTo(bus.Memory, 0);
        new byte[] {0x3e,0x42,0xed,0x45}.CopyTo(bus.Memory, 0x66);
        CpuZ80<Z80Bus> cpu = new(bus, static _ => 0xff);
        try
        {
            cpu.Reset(); cpu.Run(1000);
            int returnAddress = cpu.Context.pc.Word + 1;
            cpu.SetNmiLine(1); cpu.Run(2500);
            Assert.Equal(0x42, cpu.Context.af.High);
            Assert.Equal(returnAddress, bus.Memory[0x7ffe] | bus.Memory[0x7fff] << 8);
            Assert.Equal(0x8000, cpu.Context.sp.Word);
        }
        finally { cpu.Cycles = uint.MaxValue; cpu.State.irq_callback = null; }
    }

    private sealed class Z80Bus : ICpuZ80Bus
    {
        public byte[] Memory { get; } = new byte[65536];
        public Memory<byte>[] ReadMap { get; }
        public Z80Bus() => ReadMap = Enumerable.Range(0, 64).Select(page => Memory.AsMemory(page * 1024, 1024)).ToArray();
        public byte ReadMemory(int address) => Memory[address & 65535];
        public void WriteMemory(int address, byte value) => Memory[address & 65535] = value;
        public byte ReadPort(int port) => 255;
        public void WritePort(int port, byte value) { }
    }
    private sealed class M68000Bus : ICpu68000Bus
    {
        public byte[] Memory { get; } = new byte[65536];
        public Cpu68000<M68000Bus>? Cpu { get; set; }
        public uint AcknowledgedLevel { get; private set; }
        public int MainCycles => Cpu?.State.cycles ?? 0;
        public int MainRefreshCycles => Cpu?.State.refresh_cycles ?? 0;
        public uint Read8(uint address) => Memory[address & 65535];
        public uint Read16(uint address) => BinaryPrimitives.ReadUInt16BigEndian(Memory.AsSpan((int)(address & 65535), 2));
        public void Write8(uint address, uint value) => Memory[address & 65535] = (byte)value;
        public void Write16(uint address, uint value) => BinaryPrimitives.WriteUInt16BigEndian(Memory.AsSpan((int)(address & 65535), 2), (ushort)value);
        public uint ReadMapped8(uint address) => Read8(address);
        public uint ReadMapped16(uint address) => Read16(address);
        public void AcknowledgeInterrupt(uint level) { AcknowledgedLevel = level; Cpu!.SetIrq(0); }
        public void Long(int address, uint value) => BinaryPrimitives.WriteUInt32BigEndian(Memory.AsSpan(address, 4), value);
        public uint ReadLong(int address) => BinaryPrimitives.ReadUInt32BigEndian(Memory.AsSpan(address, 4));
    }
}
