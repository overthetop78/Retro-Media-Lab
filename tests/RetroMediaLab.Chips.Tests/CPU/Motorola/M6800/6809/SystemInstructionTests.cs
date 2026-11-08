using RetroMediaLab.Chips.IO.NationalSemiconductor.INS815x.Chip8154.Services;
using RetroMediaLab.Chips.CPU.Common.Interfaces;
using RetroMediaLab.Chips.CPU.Motorola.M6800.Chip6809.Services;

namespace RetroMediaLab.Chips.Tests.CPU.Motorola.M6800.Chip6809;
public sealed class SystemInstructionTests
{
    [Fact]
    public void Cpu6809PushPullUsesItsHardwareStackAndBigEndianWords()
    {
        var bus = new CpuBus([0x10, 0xce, 0x30, 0x00, 0x86, 0x12, 0xc6, 0x34, 0x34, 0x06, 0x4f, 0x5f, 0x35, 0x06]);
        var cpu = new Cpu6809(bus);
        cpu.Reset();
        for (var step = 0; step < 7; step++)
            cpu.Step();
        Assert.Equal(0x12, cpu.A);
        Assert.Equal(0x34, cpu.B);
        Assert.Equal(0x3000, cpu.S);
        Assert.Equal(0x12, bus.Memory[0x2ffe]);
        Assert.Equal(0x34, bus.Memory[0x2fff]);
    }

    [Fact]
    public void Cpu6809IndexedDoubleIncrementAndSixteenBitStoresUseCorrectAddresses()
    {
        var bus = new CpuBus([0x8e, 0x20, 0x00, 0xcc, 0x12, 0x34, 0xfd, 0x20, 0x00, 0xa6, 0x81]);
        var cpu = new Cpu6809(bus);
        cpu.Reset();
        for (var step = 0; step < 4; step++)
            cpu.Step();
        Assert.Equal(0x12, bus.Memory[0x2000]);
        Assert.Equal(0x34, bus.Memory[0x2001]);
        Assert.Equal(0x12, cpu.A);
        Assert.Equal(0x2002, cpu.X);
    }

    [Fact]
    public void Cpu6809FastInterruptRestoresOnlyItsPartialFrame()
    {
        var bus = new CpuBus([0x10, 0xce, 0x30, 0x00, 0x1c, 0xaf, 0x12]);
        bus.Memory[0xfff6] = 0x20;
        bus.Memory[0xfff7] = 0x00;
        bus.Memory[0x2000] = 0x86;
        bus.Memory[0x2001] = 0x55;
        bus.Memory[0x2002] = 0x3b;
        var cpu = new Cpu6809(bus);
        cpu.Reset();
        cpu.Step();
        cpu.Step();
        cpu.SetFirq(true);
        cpu.Step(); // Finish the NOP while FIRQ crosses the Q-falling synchronizer.
        cpu.Step();
        Assert.Equal(0x2ffd, cpu.S);
        cpu.SetFirq(false);
        cpu.Step();
        cpu.Step();
        Assert.Equal(0x55, cpu.A);
        Assert.Equal(0x3000, cpu.S);
        Assert.Equal(0x1007, cpu.PC);
    }

    private sealed class CpuBus : ICpuBus
    {
        internal readonly byte[] Memory = new byte[65536];
        internal CpuBus(byte[] program)
        {
            program.CopyTo(Memory, 0x1000);
            Memory[0xfffe] = 0x10;
        }

        public bool Irq => false;
        public bool Nmi => false;

        public byte Read(ushort address) => Memory[address];
        public void Write(ushort address, byte value) => Memory[address] = value;
        public void Tick()
        {
        }
    }
}
