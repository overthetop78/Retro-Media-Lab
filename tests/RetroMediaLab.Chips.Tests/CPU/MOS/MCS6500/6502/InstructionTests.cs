using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;
using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Services;
using RetroMediaLab.Chips.CPU.Common.Interfaces;
using System.IO;

namespace RetroMediaLab.Chips.Tests.CPU.MOS.MCS6500.Chip6502;
public sealed class InstructionTests
{
    [Fact]
    public void NmiIsEdgeTriggeredAndReturnsThroughTheNormalStack()
    {
        var bus = new MemoryBus();
        Array.Fill(bus.Bytes, (byte)0xea, 0x200, 16);
        bus.Bytes[0xfffa] = 0;
        bus.Bytes[0xfffb] = 3;
        bus.Bytes[0x300] = 0xe6;
        bus.Bytes[0x301] = 0x10;
        bus.Bytes[0x302] = 0x40;
        var cpu = Start(bus);
        bus.Nmi = true;
        cpu.Step();
        Assert.Equal(0x201, cpu.PC);
        var cycles = cpu.Cycles;
        cpu.Step();
        Assert.Equal(0x300, cpu.PC);
        Assert.Equal(cycles + 7, cpu.Cycles);
        Assert.Equal(0x02, bus.Bytes[0x1fd]);
        Assert.Equal(0x01, bus.Bytes[0x1fc]);
        Assert.Equal(0, bus.Bytes[0x1fb] & 0x10);
        cpu.Step();
        cpu.Step();
        Assert.Equal(1, bus.Bytes[0x10]);
        Assert.Equal(0x201, cpu.PC);
        cpu.Step();
        Assert.Equal(0x202, cpu.PC); // NMI toujours haute : aucune seconde interruption.
        bus.Nmi = false;
        cpu.Step();
        bus.Nmi = true;
        cpu.Step();
        cpu.Step();
        Assert.Equal(0x300, cpu.PC);
    }

    [Fact]
    public void NmiTakesPriorityOverAnUnmaskedIrq()
    {
        var bus = new MemoryBus();
        bus.Bytes[0x200] = 0x58; // CLI
        bus.Bytes[0xfffa] = 0;
        bus.Bytes[0xfffb] = 3;
        bus.Bytes[0x300] = 0x40;
        bus.Bytes[0xfffe] = 0;
        bus.Bytes[0xffff] = 4;
        var cpu = Start(bus);
        bus.Irq = true;
        bus.Nmi = true;
        cpu.Step();
        cpu.Step();
        Assert.Equal(0x300, cpu.PC);
        cpu.Step();
        Assert.Equal(0x201, cpu.PC);
        cpu.Step();
        Assert.Equal(0x400, cpu.PC);
    }

    [Fact]
    public void CpuAddsWithCarryAndWritesResult()
    {
        var bus = new MemoryBus();
        byte[] program = [0xa9, 0xff, 0x18, 0x69, 0x01, 0x85, 0x20];
        program.CopyTo(bus.Bytes, 0x200);
        var cpu = Start(bus);
        for (var i = 0; i < 4; i++)
            cpu.Step();
        Assert.Equal(0, bus.Bytes[0x20]);
        Assert.Equal(3, cpu.P & 3);
    }

    [Fact]
    public void IndirectJumpRetainsNmosPageWrap()
    {
        var bus = new MemoryBus();
        bus.Bytes[0x200] = 0x6c;
        bus.Bytes[0x201] = 0xff;
        bus.Bytes[0x202] = 0x12;
        bus.Bytes[0x12ff] = 0x34;
        bus.Bytes[0x1200] = 0x56;
        bus.Bytes[0x1300] = 0x78;
        var cpu = Start(bus);
        cpu.Step();
        Assert.Equal(0x5634, cpu.PC);
    }

    [Fact]
    public void DecimalSubtractionAndAdditionUseBcd()
    {
        var bus = new MemoryBus();
        byte[] program = [0xf8, 0x18, 0xa9, 0x45, 0x69, 0x55, 0x85, 0x20, 0x38, 0xe9, 0x01, 0x85, 0x21];
        program.CopyTo(bus.Bytes, 0x200);
        var cpu = Start(bus);
        for (var i = 0; i < 8; i++)
            cpu.Step();
        Assert.Equal(0, bus.Bytes[0x20]);
        Assert.Equal(0x99, bus.Bytes[0x21]);
    }

    private static Cpu6502 Start(MemoryBus bus)
    {
        bus.Bytes[0xfffc] = 0;
        bus.Bytes[0xfffd] = 2;
        var cpu = new Cpu6502(bus);
        cpu.Reset();
        return cpu;
    }

    private sealed class MemoryBus : ICpuBus
    {
        internal byte[] Bytes { get; } = new byte[65536];
        public bool Irq { get; set; }
        public bool Nmi { get; set; }

        public byte Read(ushort address) => Bytes[address];
        public void Write(ushort address, byte value) => Bytes[address] = value;
        public void Tick()
        {
        }
    }
}
