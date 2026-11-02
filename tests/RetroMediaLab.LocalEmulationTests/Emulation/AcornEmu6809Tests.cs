using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Common.Interfaces;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Mc6809.Services;

namespace RetroMediaLab.Tests.Emulation;

/// <summary>Régressions autonomes du MC6809, suivant le manuel de programmation Motorola.</summary>
public sealed class AcornEmu6809Tests
{
    [Fact]
    public void NmiBeforeStackInitializationIsLatchedUntilLdsCompletes()
    {
        var (cpu, bus) = Start(0x12, 0x10, 0xce, 0x30, 0, 0x12);
        bus.Memory[0xfffc] = 0x20;
        bus.Nmi = true;
        cpu.Step();
        Assert.Equal(0x1001, cpu.PC);
        cpu.Step();
        Assert.Equal(0x3000, cpu.S);
        Assert.Equal(0x1005, cpu.PC);
        cpu.Step();
        Assert.Equal(0x2000, cpu.PC);
        Assert.Equal(0x2ff4, cpu.S);
    }

    [Fact]
    public void PuluLoadingSystemStackAlsoReleasesLatchedNmi()
    {
        var (cpu, bus) = Start(0xce, 0x40, 0, 0x37, 0x40, 0x12);
        bus.Memory[0x4000] = 0x30;
        bus.Memory[0xfffc] = 0x20;
        bus.Nmi = true;
        cpu.Step(); cpu.Step();
        Assert.Equal(0x3000, cpu.S);
        Assert.Equal(0x4002, cpu.U);
        cpu.Step();
        Assert.Equal(0x2000, cpu.PC);
        Assert.Equal(0x2ff4, cpu.S);
    }

    [Fact]
    public void CwaiStacksOnceAndReadsInterruptVectorAtEndOfWakeup()
    {
        var (cpu, bus) = Start(0x10, 0xce, 0x30, 0, 0x3c, 0xaf);
        bus.Memory[0xfff8] = 0x20;
        cpu.Step(); cpu.Step();
        Assert.Equal(0x2ff4, cpu.S);
        bus.Irq = true;
        bus.Accesses.Clear(); bus.Ticks = 0;
        var before = cpu.Cycles;
        cpu.Step();
        Assert.Equal(7, cpu.Cycles - before);
        Assert.Equal(0x2ff4, cpu.S);
        Assert.DoesNotContain(bus.Accesses, access => access.Write);
        Assert.Equal(new[] { (5, 0xfff8), (6, 0xfff9) }, bus.Accesses.Select(a => (a.Tick, (int)a.Address)));
    }

    [Fact]
    public void SyncResumesOnMaskedInterruptWithoutTakingItsVector()
    {
        var (cpu, bus) = Start(0x13, 0x12);
        cpu.Step();
        cpu.Step();
        Assert.Equal(0x1001, cpu.PC);
        bus.Irq = true;
        cpu.Step();
        Assert.Equal(0x1002, cpu.PC);
    }

    [Fact]
    public void FullIrqFrameAndVectorUseTheirHardwareBusCycles()
    {
        var (cpu, bus) = Start(0x10, 0xce, 0x30, 0, 0x1c, 0xef, 0x12);
        bus.Memory[0xfff8] = 0x20;
        cpu.Step(); cpu.Step();
        bus.Irq = true;
        bus.Accesses.Clear(); bus.Ticks = 0;
        var before = cpu.Cycles;
        cpu.Step();
        Assert.Equal(19, cpu.Cycles - before);
        Assert.Equal(4, bus.Accesses.First(a => a.Write).Tick);
        Assert.Equal(17, bus.Accesses.Single(a => a.Address == 0xfff8).Tick);
        Assert.Equal(18, bus.Accesses.Single(a => a.Address == 0xfff9).Tick);
        Assert.Equal(0x2000, cpu.PC);
        Assert.Equal(0x2ff4, cpu.S);
    }

    [Theory]
    [InlineData(0x96, 4)]
    [InlineData(0xb6, 5)]
    public void DirectAndExtendedLoadPlaceInternalCycleBeforeOperandRead(int opcode, int cycles)
    {
        var program = opcode == 0x96 ? new byte[] { 0x96, 0x20 } : [0xb6, 0, 0x20];
        var (cpu, bus) = Start(program);
        bus.Memory[0x20] = 0x55;
        cpu.Step();
        Assert.Equal(cycles, bus.Ticks);
        Assert.Equal(cycles, bus.Accesses.Single(a => a.Address == 0x20).Tick);
        Assert.Equal(0x55, cpu.A);
    }

    [Theory]
    [InlineData(0x84, 4)]
    [InlineData(0x88, 5)]
    [InlineData(0x89, 8)]
    public void IndexedAddressingCompletesBeforeReadingOperand(int postbyte, int cycles)
    {
        var (cpu, bus) = Start(0x8e, 0x20, 0, 0xa6, (byte)postbyte, 0, 0);
        bus.Memory[0x2000] = 0x55;
        cpu.Step();
        bus.Accesses.Clear(); bus.Ticks = 0;
        cpu.Step();
        Assert.Equal(cycles, bus.Ticks);
        Assert.Equal(cycles, bus.Accesses.Single(a => a.Address == 0x2000).Tick);
        Assert.Equal(0x55, cpu.A);
    }

    [Fact]
    public void MemoryIncrementReadsPrefetchBetweenOperandReadAndWrite()
    {
        var (cpu, bus) = Start(0x0c, 0x20, 0x12);
        bus.Memory[0x20] = 0x7f;
        cpu.Step();
        Assert.Equal(new[] { (1, 0x1000, false), (2, 0x1001, false), (4, 0x20, false), (5, 0x1002, false), (6, 0x20, true) },
            bus.Accesses.Select(a => (a.Tick, (int)a.Address, a.Write)));
        Assert.Equal(0x80, bus.Memory[0x20]);
        Assert.Equal(0x0a, cpu.CC & 0x0f);
    }

    [Fact]
    public void PushAndPullPerformStackAddressCyclesBeforeTransferringRegisters()
    {
        var (cpu, bus) = Start(0x10, 0xce, 0x30, 0, 0x86, 0x55, 0x34, 2, 0x35, 2);
        cpu.Step(); cpu.Step();
        bus.Accesses.Clear(); bus.Ticks = 0;
        cpu.Step();
        Assert.Equal(new[] { (1, 0x1006, false), (2, 0x1007, false), (5, 0x3000, false), (6, 0x2fff, true) },
            bus.Accesses.Select(a => (a.Tick, (int)a.Address, a.Write)));
        Assert.Equal(0x55, bus.Memory[0x2fff]);
        bus.Accesses.Clear(); bus.Ticks = 0;
        cpu.Step();
        Assert.Equal(new[] { (1, 0x1008, false), (2, 0x1009, false), (5, 0x2fff, false), (6, 0x3000, false) },
            bus.Accesses.Select(a => (a.Tick, (int)a.Address, a.Write)));
        Assert.Equal(0x3000, cpu.S);
        Assert.Equal(0x55, cpu.A);
    }

    [Theory]
    [InlineData(0x39, 0, 5)]
    [InlineData(0x3b, 0, 6)]
    [InlineData(0x3b, 0x80, 15)]
    public void ReturnReadsNewStackTopAfterRestoringProgramCounter(int opcode, int flags, int cycles)
    {
        var (cpu, bus) = Start(0x10, 0xce, 0x30, 0, (byte)opcode);
        cpu.Step();
        var frameSize = opcode == 0x39 ? 2 : flags == 0 ? 3 : 12;
        bus.Memory[0x3000] = (byte)flags;
        bus.Memory[0x3000 + frameSize - 2] = 0x20;
        bus.Accesses.Clear(); bus.Ticks = 0;
        cpu.Step();
        Assert.Equal(cycles, bus.Ticks);
        Assert.Equal(0x2000, cpu.PC);
        Assert.Equal((cycles, (ushort)(0x3000 + frameSize), false), bus.Accesses.Last());
    }

    [Fact]
    public void CwaiReadsNextOpcodeBeforeWritingItsSavedFrame()
    {
        var (cpu, bus) = Start(0x10, 0xce, 0x30, 0, 0x3c, 0xff, 0x12);
        cpu.Step();
        bus.Accesses.Clear(); bus.Ticks = 0;
        cpu.Step();
        Assert.Equal(20, bus.Ticks);
        Assert.Equal((3, (ushort)0x1006, false), bus.Accesses[2]);
        Assert.Equal(5, bus.Accesses.First(a => a.Write).Tick);
        Assert.Equal(16, bus.Accesses.Last(a => a.Write).Tick);
    }

    [Theory]
    [InlineData(0x41, 1, 0, 0xff, 1)]
    [InlineData(0x42, 1, 0, 0xff, 1)]
    [InlineData(0x42, 1, 1, 0xfe, 1)]
    [InlineData(0x45, 2, 0, 1, 0)]
    [InlineData(0x4b, 0, 1, 0xff, 0)]
    [InlineData(0x4b, 1, 0, 0, 1)]
    [InlineData(0x4e, 0xff, 1, 0, 1)]
    public void PhysicalUnaryAliasesRunWithoutHostExceptions(int opcode, int value, int carry, int result, int expectedCarry)
    {
        var (cpu, bus) = Start(0x86, (byte)value, 0x1c, 0xfe, 0x1a, (byte)carry, (byte)opcode);
        cpu.Step(); cpu.Step(); cpu.Step();
        var before = cpu.Cycles;
        cpu.Step();
        Assert.Equal(2, cpu.Cycles - before);
        Assert.Equal(result, cpu.A);
        Assert.Equal(expectedCarry, cpu.CC & 1);
    }

    [Theory]
    [InlineData(0x14)]
    [InlineData(0x15)]
    [InlineData(0xcd)]
    public void HardwareTestModeFetchesSequentialAddressesUntilReset(int opcode)
    {
        var (cpu, bus) = Start((byte)opcode, 0x86, 0x55);
        cpu.Step();
        bus.Irq = true; bus.Nmi = true;
        bus.Accesses.Clear(); bus.Ticks = 0;
        cpu.Step(); cpu.Step();
        Assert.Equal(0x1003, cpu.PC);
        Assert.Equal(0, cpu.A);
        Assert.Equal(new[] { (1, 0x1001), (2, 0x1002) }, bus.Accesses.Select(a => (a.Tick, (int)a.Address)));
        cpu.Reset();
        Assert.Equal(0x1000, cpu.PC);
    }

    [Theory]
    [InlineData(0x90, 0x2001)]
    [InlineData(0x92, 0x1fff)]
    public void UndocumentedSingleByteIndirectIndexingUsesPointerAndUpdatesRegister(int postbyte, int expectedX)
    {
        var (cpu, bus) = Start(0x8e, 0x20, 0, 0xa6, (byte)postbyte);
        var pointer = postbyte == 0x90 ? 0x2000 : 0x1fff;
        bus.Memory[pointer] = 0x30;
        bus.Memory[pointer + 1] = 0;
        bus.Memory[0x3000] = 0x55;
        cpu.Step(); cpu.Step();
        Assert.Equal(expectedX, cpu.X);
        Assert.Equal(0x55, cpu.A);
    }

    [Fact]
    public void TransferDirectPageToWordReplicatesBothBytes()
    {
        var (cpu, _) = Start(0x86, 0x25, 0x1f, 0x8b, 0x1f, 0xb1);
        cpu.Step(); cpu.Step(); cpu.Step();
        Assert.Equal(0x2525, cpu.X);
    }

    [Theory]
    [InlineData(0xb1, 0x2525)]
    [InlineData(0x1b, 0xff25)]
    public void ExchangeDirectPageAndWordDependsOnSourceWidth(int postbyte, int expectedX)
    {
        var (cpu, _) = Start(0x86, 0x25, 0x1f, 0x8b, 0x8e, 0x12, 0x34, 0x1e, (byte)postbyte);
        cpu.Step(); cpu.Step(); cpu.Step(); cpu.Step();
        Assert.Equal(expectedX, cpu.X);
        Assert.Equal(0x34, cpu.DP);
    }

    [Theory]
    [InlineData(0x79, 1, 0xd9, 0)]
    [InlineData(0x80, 0, 0x80, 2)]
    [InlineData(0x99, 1, 0xf9, 0)]
    [InlineData(0x9a, 0, 0, 2)]
    public void DecimalAdjustOverflowMatchesCarryXorAdjustedSign(int value, int carry, int expected, int overflow)
    {
        var (cpu, _) = Start(0x86, (byte)value, 0x1c, 0xde, 0x1a, (byte)carry, 0x19);
        cpu.Step(); cpu.Step(); cpu.Step(); cpu.Step();
        Assert.Equal(expected, cpu.A);
        Assert.Equal(overflow, cpu.CC & 2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0x10)]
    [InlineData(0x11)]
    public void EveryPhysicalOpcodeDecodesWithoutThrowingHostExceptions(int prefix)
    {
        for (var opcode = 0; opcode <= byte.MaxValue; opcode++)
        {
            byte[] program = prefix == 0 ? [(byte)opcode, 0, 0, 0] : [(byte)prefix, (byte)opcode, 0, 0, 0];
            var (cpu, _) = Start(program);
            cpu.Step();
        }
    }

    [Theory]
    [InlineData(0x10, 0x11, 0x20)]
    [InlineData(0x11, 0x10, 0x30)]
    public void OnlyFirstPrefixSelectsTheSoftwareInterruptVector(int first, int second, int targetHigh)
    {
        var (cpu, bus) = Start(0x10, 0xce, 0x40, 0, (byte)first, (byte)second, 0x3f);
        bus.Memory[0xfff4] = 0x20;
        bus.Memory[0xfff2] = 0x30;
        cpu.Step();
        var before = cpu.Cycles;
        cpu.Step();
        Assert.Equal(21, cpu.Cycles - before);
        Assert.Equal(targetHigh << 8, cpu.PC);
        Assert.Equal(0x3ff4, cpu.S);
    }

    [Fact]
    public void PrefixOfOrdinaryInstructionAddsOneCycleWithoutChangingItsOperation()
    {
        var (cpu, _) = Start(0x10, 0x86, 0x55);
        var before = cpu.Cycles;
        cpu.Step();
        Assert.Equal(3, cpu.Cycles - before);
        Assert.Equal(0x55, cpu.A);
        Assert.Equal(0x1003, cpu.PC);
    }

    [Theory]
    [InlineData(0, 0xfffe)]
    [InlineData(0x10, 0xfff4)]
    [InlineData(0x11, 0xfff6)]
    public void UndocumentedSoftwareInterruptPreservesEntireStateFlag(int prefix, int vector)
    {
        byte[] program = prefix == 0 ? [0x10, 0xce, 0x40, 0, 0x3e] : [0x10, 0xce, 0x40, 0, (byte)prefix, 0x3e];
        var (cpu, bus) = Start(program);
        cpu.Step();
        var flags = cpu.CC;
        bus.Memory[vector] = 0x20;
        bus.Memory[vector + 1] = 0;
        var before = cpu.Cycles;
        cpu.Step();
        Assert.Equal(prefix == 0 ? 19 : 20, cpu.Cycles - before);
        Assert.Equal(0x2000, cpu.PC);
        Assert.Equal(flags, cpu.CC);
        Assert.Equal(flags, bus.Memory[0x3ff4]);
    }

    [Theory]
    [InlineData(0, 0x04)]
    [InlineData(0x80, 0x08)]
    [InlineData(0x55, 0)]
    public void ImmediateByteStoreRetainsLoadAluFlagsAndDoesNotWriteOperand(int value, int expectedFlags)
    {
        var (cpu, bus) = Start(0x86, (byte)value, 0x87, 0x77);
        cpu.Step();
        bus.Accesses.Clear(); bus.Ticks = 0;
        cpu.Step();
        Assert.Equal(expectedFlags, cpu.CC & 0x0e);
        Assert.Equal(0x77, bus.Memory[0x1003]);
        Assert.DoesNotContain(bus.Accesses, a => a.Write);
        Assert.Equal(2, bus.Ticks);
    }

    [Fact]
    public void ImmediateStoreAfterCompareUsesCompareGroupInsteadOfAccumulatorFlags()
    {
        var (cpu, _) = Start(0x86, 0x80, 0x81, 1, 0x87, 0);
        cpu.Step(); cpu.Step(); cpu.Step();
        Assert.Equal(4, cpu.CC & 0x0e);
    }

    [Fact]
    public void PrefixedImmediateWordStoreWritesOnlyLowOperandByte()
    {
        var (cpu, bus) = Start(0x10, 0x8e, 0x12, 0x34, 0x10, 0x8f, 0xaa, 0xbb);
        cpu.Step();
        bus.Accesses.Clear(); bus.Ticks = 0;
        cpu.Step();
        Assert.Equal(4, bus.Ticks);
        Assert.Equal(0xaa, bus.Memory[0x1006]);
        Assert.Equal(0x34, bus.Memory[0x1007]);
        Assert.Equal(8, cpu.CC & 0x0e);
        Assert.Equal((4, (ushort)0x1007, true), bus.Accesses.Last());
    }

    [Fact]
    public void PrefixStreamYieldsForHostWithoutAcceptingInterruptBetweenPrefixes()
    {
        var (cpu, bus) = Start();
        Array.Fill(bus.Memory, (byte)0x10);
        cpu.Step();
        Assert.Equal(0x1000, cpu.PC);
        bus.Irq = true;
        bus.Memory[0x1000] = 0x86;
        bus.Memory[0x1001] = 0x55;
        cpu.Step();
        Assert.Equal(0x55, cpu.A);
        Assert.Equal(0x1002, cpu.PC);
    }

    [Fact]
    public void ResetPreservesArithmeticFlagsAndReadsVectorAfterInternalCycles()
    {
        var (cpu, bus) = Start(0x1a, 0xaf);
        cpu.Step();
        var flags = cpu.CC;
        bus.Accesses.Clear(); bus.Ticks = 0;
        cpu.Reset();
        Assert.Equal(flags, cpu.CC);
        Assert.Equal(0, cpu.DP);
        Assert.Equal(7, bus.Ticks);
        Assert.Equal(new[] { (5, 0xfffe), (6, 0xffff) }, bus.Accesses.Select(a => (a.Tick, (int)a.Address)));
    }

    [Fact]
    public void X18UsesNextOpcodeWithoutConsumingIt()
    {
        var (cpu, _) = Start(0x1a, 0xff, 0x18, 0x12);
        cpu.Step();
        var before = cpu.Cycles;
        cpu.Step();
        Assert.Equal(3, cpu.Cycles - before);
        Assert.Equal(0x26, cpu.CC);
        Assert.Equal(0x1003, cpu.PC);
        cpu.Step();
        Assert.Equal(0x1004, cpu.PC);
    }

    [Fact]
    public void XandccMasksFlagsWithItsExtraInternalCycle()
    {
        var (cpu, bus) = Start(0x1a, 0xff, 0x38, 0x20);
        cpu.Step();
        bus.Accesses.Clear(); bus.Ticks = 0;
        cpu.Step();
        Assert.Equal(0x20, cpu.CC);
        Assert.Equal(4, bus.Ticks);
        Assert.Equal((3, (ushort)0x1003, false), bus.Accesses.Last());
    }

    [Theory]
    [InlineData(0x10, 0)]
    [InlineData(0x11, 5)]
    public void PrefixedAddCompareUpdatesFlagsWithoutChangingRegister(int prefix, int flags)
    {
        var (cpu, _) = Start(0xcc, 0x12, 0x34, 0xce, 0, 0, (byte)prefix, 0xc3, 1, 0);
        cpu.Step(); cpu.Step();
        var before = cpu.Cycles;
        cpu.Step();
        Assert.Equal(5, cpu.Cycles - before);
        Assert.Equal(0x12, cpu.A);
        Assert.Equal(0x34, cpu.B);
        Assert.Equal(0, cpu.U);
        Assert.Equal(flags, cpu.CC & 0x0f);
    }

    [Theory]
    [InlineData(0x80, 0x7f, 0x04)]
    [InlineData(1, 0xff, 0x09)]
    public void ImmediateStoreAfterArithmeticUsesComplementedAccumulator(int initial, int addend, int flags)
    {
        var (cpu, _) = Start(0x86, (byte)initial, 0x8b, (byte)addend, 0x87, 0);
        cpu.Step(); cpu.Step(); cpu.Step();
        Assert.Equal(flags, cpu.CC & 0x0f);
    }

    private static (Cpu6809 Cpu, Bus Bus) Start(params byte[] program)
    {
        var bus = new Bus();
        program.CopyTo(bus.Memory, 0x1000);
        bus.Memory[0xfffe] = 0x10;
        var cpu = new Cpu6809(bus);
        cpu.Reset();
        bus.Accesses.Clear(); bus.Ticks = 0;
        return (cpu, bus);
    }

    private sealed class Bus : ICpuBus
    {
        internal byte[] Memory { get; } = new byte[65536];
        internal int Ticks { get; set; }
        internal List<(int Tick, ushort Address, bool Write)> Accesses { get; } = [];
        public bool Irq { get; set; }
        public bool Nmi { get; set; }
        public byte Read(ushort address) { Accesses.Add((Ticks, address, false)); return Memory[address]; }
        public void Write(ushort address, byte value) { Accesses.Add((Ticks, address, true)); Memory[address] = value; }
        public void Tick() { Ticks++; }
    }
}
