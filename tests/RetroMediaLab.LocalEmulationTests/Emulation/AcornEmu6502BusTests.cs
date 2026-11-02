using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Common.Interfaces;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Mos6502.Services;

namespace RetroMediaLab.Tests.Emulation;

/// <summary>Traces NMOS autonomes : cycles visibles, pile et effets des instructions combinées.</summary>
public sealed class AcornEmu6502BusTests
{
    [Fact]
    public void ResetReadsStackWithoutWritingAndDecrementsExistingStackPointer()
    {
        var bus = new TraceBus();
        bus.Bytes[0xfffd] = 2;
        var cpu = new Cpu6502(bus);
        cpu.Reset();
        Assert.Equal(new[] { 0, 0, 0x100, 0x1ff, 0x1fe, 0xfffc, 0xfffd }, bus.Accesses.Select(a => (int)a.Address));
        Assert.All(bus.Accesses, a => Assert.False(a.Write));
        Assert.Equal(0xfd, cpu.S);
        bus.Accesses.Clear();
        cpu.Reset();
        Assert.Equal(new[] { 0x200, 0x200, 0x1fd, 0x1fc, 0x1fb, 0xfffc, 0xfffd }, bus.Accesses.Select(a => (int)a.Address));
        Assert.Equal(0xfa, cpu.S);
    }

    [Theory]
    [InlineData(0xbd, 0xff, 0x2000, 0x2100)]
    [InlineData(0x9d, 0x10, 0x2011, 0x2011)]
    [InlineData(0x9d, 0xff, 0x2000, 0x2100)]
    public void IndexedAccessIncludesDiscardedRead(int opcode, int low, int discarded, int effective)
    {
        var (cpu, bus) = Start(0xa2, 1, (byte)opcode, (byte)low, 0x20);
        cpu.Step();
        bus.Accesses.Clear();
        cpu.Step();
        Assert.Equal(new[] { 0x202, 0x203, 0x204, discarded, effective }, bus.Accesses.Select(a => (int)a.Address));
        Assert.Equal(opcode == 0x9d, bus.Accesses[^1].Write);
    }

    [Fact]
    public void IndexedZeroPageReadsUnindexedAddressBeforeWrapping()
    {
        var (cpu, bus) = Start(0xa2, 2, 0xb5, 0xff);
        bus.Bytes[1] = 0x37;
        cpu.Step();
        bus.Accesses.Clear();
        cpu.Step();
        Assert.Equal(new[] { 0x202, 0x203, 0xff, 1 }, bus.Accesses.Select(a => (int)a.Address));
        Assert.Equal(0x37, cpu.A);
    }

    [Fact]
    public void JsrAndRtsUseHardwareStackSequence()
    {
        var (cpu, bus) = Start(0x20, 0, 3);
        bus.Bytes[0x300] = 0x60;
        cpu.Step();
        Assert.Equal(new[] { 0x200, 0x201, 0x1fd, 0x1fd, 0x1fc, 0x202 }, bus.Accesses.Select(a => (int)a.Address));
        Assert.True(bus.Accesses[3].Write);
        Assert.True(bus.Accesses[4].Write);
        bus.Accesses.Clear();
        cpu.Step();
        Assert.Equal(new[] { 0x300, 0x301, 0x1fb, 0x1fc, 0x1fd, 0x202 }, bus.Accesses.Select(a => (int)a.Address));
        Assert.Equal(0x203, cpu.PC);
    }

    [Fact]
    public void TakenBranchReadsOldPcAndUncorrectedPage()
    {
        var (cpu, bus) = Start(0xd0, 0xfd);
        cpu.Step();
        Assert.Equal(new[] { 0x200, 0x201, 0x202, 0x2ff }, bus.Accesses.Select(a => (int)a.Address));
        Assert.Equal(0x1ff, cpu.PC);
    }

    [Fact]
    public void CliThenSeiAcceptsOnePendingIrqWithSeiFlagsOnStack()
    {
        var (cpu, bus) = Start(0x58, 0x78, 0xea);
        bus.Bytes[0xffff] = 4;
        bus.Irq = true;
        cpu.Step();
        Assert.Equal(0x201, cpu.PC);
        cpu.Step();
        Assert.Equal(0x202, cpu.PC);
        bus.Accesses.Clear();
        cpu.Step();
        Assert.Equal(0x400, cpu.PC);
        Assert.Equal(7, bus.Accesses.Count);
        Assert.Equal(4, bus.Bytes[0x1fb] & 4);
    }

    [Theory]
    [InlineData(0x07, 0x81, 1, 2, 3)]
    [InlineData(0x27, 0x81, 3, 3, 3)]
    [InlineData(0x47, 6, 7, 3, 4)]
    [InlineData(0x67, 2, 1, 0x81, 0x82)]
    [InlineData(0xc7, 4, 3, 3, 3)]
    [InlineData(0xe7, 2, 7, 3, 4)]
    public void CombinedOpcodesWriteOriginalThenModifiedValue(int opcode, int initial, int accumulator, int modified, int result)
    {
        var (cpu, bus) = Start(0xa9, (byte)accumulator, 0x38, (byte)opcode, 0x10);
        bus.Bytes[0x10] = (byte)initial;
        cpu.Step(); cpu.Step();
        bus.Accesses.Clear();
        cpu.Step();
        Assert.Equal(5, bus.Accesses.Count);
        Assert.Equal(new[] { (byte)initial, (byte)modified }, bus.Accesses.Where(a => a.Write).Select(a => a.Value));
        Assert.Equal(result, cpu.A);
    }

    [Fact]
    public void JamIgnoresInterruptsUntilReset()
    {
        var (cpu, bus) = Start(0x02, 0xea);
        cpu.Step();
        bus.Nmi = true;
        cpu.Step(); cpu.Step();
        Assert.Equal(0x201, cpu.PC);
        cpu.Reset();
        Assert.Equal(0x200, cpu.PC);
    }

    [Fact]
    public void EveryNmosOpcodeHasPhysicalBehaviorInsteadOfHostException()
    {
        for (var opcode = 0; opcode <= byte.MaxValue; opcode++)
        {
            var (cpu, bus) = Start((byte)opcode, 0, 3);
            var previous = cpu.Cycles;
            cpu.Step();
            Assert.InRange(cpu.Cycles - previous, 2, 8);
            Assert.Equal(cpu.Cycles - previous, bus.Accesses.Count);
        }
    }

    [Fact]
    public void DecimalAdcCorrectsHighNibbleWithoutTreatingInvalidLowDigitAsCarry()
    {
        var (cpu, _) = Start(0xf8, 0x18, 0xa9, 0x79, 0x69, 0x1e);
        cpu.Step(); cpu.Step(); cpu.Step(); cpu.Step();
        Assert.Equal(0x9d, cpu.A);
        Assert.Equal(0, cpu.P & 1);
    }

    [Theory]
    [InlineData(0x02, 3, true)]
    [InlineData(0x02, 4, false)]
    [InlineData(0xfc, 3, true)]
    [InlineData(0xfc, 4, false)]
    [InlineData(0xfc, 5, true)]
    public void NmosTakenBranchPollsEarlyAndAgainOnlyWhenCrossingPage(int displacement, int pulseTick, bool accepted)
    {
        var (cpu, bus) = Start(0x58, 0xd0, (byte)displacement);
        bus.Bytes[0xffff] = 4;
        bus.IrqSignal = tick => tick == pulseTick;
        var target = (ushort)(0x203 + (sbyte)displacement);
        bus.Bytes[target] = 0xea;
        cpu.Step();
        cpu.Step();
        Assert.Equal(target, cpu.PC);
        cpu.Step();
        Assert.Equal(accepted ? 0x400 : target + 1, cpu.PC);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NmiArrivingOnLastCycleWaitsForFollowingInstruction(bool cmos)
    {
        var (cpu, bus) = Start(cmos, 0xea, 0xea, 0xea);
        bus.Bytes[0xfffb] = 3;
        bus.NmiSignal = tick => tick >= 2;
        cpu.Step();
        Assert.Equal(0x201, cpu.PC);
        cpu.Step();
        Assert.Equal(0x202, cpu.PC);
        cpu.Step();
        Assert.Equal(0x300, cpu.PC);
    }

    [Theory]
    [InlineData(false, 0x300)]
    [InlineData(true, 0x400)]
    public void NmiDuringBrkRedirectsOnlyTheNmosVector(bool cmos, int expected)
    {
        var (cpu, bus) = Start(cmos, 0, 0xea);
        bus.Bytes[0xfffb] = 3;
        bus.Bytes[0xffff] = 4;
        bus.Bytes[0x300] = 0xea;
        bus.NmiSignal = tick => tick >= 4;
        cpu.Step();
        Assert.Equal(expected, cpu.PC);
        Assert.Equal(0x10, bus.Bytes[0x1fb] & 0x10);
        cpu.Step();
        Assert.Equal(cmos ? 0x300 : 0x301, cpu.PC);
    }

    [Fact]
    public void CmosCliThenSeiAlsoUsesTheInterruptPollInsteadOfLiveLevel()
    {
        var (cpu, bus) = Start(true, 0x58, 0x78, 0xea);
        bus.Bytes[0xffff] = 4;
        bus.Irq = true;
        cpu.Step();
        Assert.Equal(0x201, cpu.PC);
        cpu.Step();
        Assert.Equal(0x202, cpu.PC);
        cpu.Step();
        Assert.Equal(0x400, cpu.PC);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NmiCanRedirectAnIrqEntryWithoutSettingBreakInStack(bool cmos)
    {
        var (cpu, bus) = Start(cmos, 0xf8, 0x58, 0xea, 0xea);
        bus.Bytes[0xfffb] = 3;
        bus.Bytes[0xffff] = 4;
        bus.Irq = true;
        cpu.Step(); cpu.Step(); cpu.Step();
        bus.NmiSignal = tick => tick >= 9;
        cpu.Step();
        Assert.Equal(0x300, cpu.PC);
        Assert.Equal(0, bus.Bytes[0x1fb] & 0x10);
        Assert.Equal(8, bus.Bytes[0x1fb] & 8);
        Assert.Equal(cmos ? 0 : 8, cpu.P & 8);
    }

    private static (Cpu6502 Cpu, TraceBus Bus) Start(params byte[] program) => Start(false, program);

    private static (Cpu6502 Cpu, TraceBus Bus) Start(bool cmos, params byte[] program)
    {
        var bus = new TraceBus();
        bus.Bytes[0xfffd] = 2;
        program.CopyTo(bus.Bytes, 0x200);
        var cpu = new Cpu6502(bus, cmos);
        cpu.Reset();
        bus.Accesses.Clear();
        bus.Ticks = 0;
        return (cpu, bus);
    }

    private sealed class TraceBus : ICpuBus
    {
        internal byte[] Bytes { get; } = new byte[65536];
        internal List<(ushort Address, byte Value, bool Write)> Accesses { get; } = [];
        private bool _irq;
        internal int Ticks { get; set; }
        internal Func<int, bool>? IrqSignal { get; set; }
        internal Func<int, bool>? NmiSignal { get; set; }
        public bool Irq { get => IrqSignal?.Invoke(Ticks) ?? _irq; set => _irq = value; }
        private bool _nmi;
        public bool Nmi { get => NmiSignal?.Invoke(Ticks) ?? _nmi; set => _nmi = value; }
        public byte Read(ushort address) { Accesses.Add((address, Bytes[address], false)); return Bytes[address]; }
        public void Write(ushort address, byte value) { Accesses.Add((address, value, true)); Bytes[address] = value; }
        public void Tick() { Ticks++; }
    }
}
