using RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Enums;
using RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Services;

namespace RetroMediaLab.Chips.Tests.CPU.Intel.MCS80x86.Chip80186;

public sealed class Chip80186Tests
{
    private const int ProgramAddress = 0x1003;

    [Fact]
    public void InvalidOpcodeEntersVectorSixWithoutRereadingItsInstructionForDiagnostics()
    {
        var (cpu, bus) = Start(0x0f);
        bus.Vector(6, 0x2000);
        bus.Reads.Clear();
        cpu.Step();
        Assert.Equal(0x2000, cpu.IP);
        // Start a déjà préchargé cet octet ; la faute ne doit pas relire son adresse.
        Assert.DoesNotContain(ProgramAddress, bus.Reads);
    }

    [Fact]
    public void DivideErrorReturnsAfterDivisionAndPreservesDividend()
    {
        var (cpu, bus) = Start(0xb8, 0, 0x10, 0xb3, 0, 0xf6, 0xf3);
        bus.Vector(0, 0x2000);
        cpu.Step(); cpu.Step(); cpu.Step();
        Assert.Equal(0x2000, cpu.IP);
        Assert.Equal(ProgramAddress + 7, bus.Word(0x7ffa));
        Assert.Equal(0x1000, cpu.Register(0));
    }

    [Fact]
    public void BoundReadsBothBoundsEvenWhenLowerBoundAlreadyFails()
    {
        var (cpu, bus) = Start(0xb8, 0xff, 0xff, 0x62, 6, 0, 0x30);
        bus.Vector(5, 0x2000);
        bus.Memory[0x3002] = 10;
        cpu.Step(); bus.Reads.Clear(); cpu.Step();
        Assert.Equal(new[] { 0x3000, 0x3001, 0x3002, 0x3003 }, bus.Reads.Where(a => a is >= 0x3000 and <= 0x3003));
        Assert.Equal(ProgramAddress + 7, bus.Word(0x7ffa));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void ShiftWithMaskedZeroCountReadsButDoesNotWriteMemory(int count)
    {
        var (cpu, bus) = Start(0xc0, 0x26, 0, 0x30, (byte)count);
        bus.Memory[0x3000] = 0x81;
        bus.Reads.Clear(); bus.Writes.Clear();
        var flags = cpu.Flags;
        cpu.Step();
        Assert.Contains(0x3000, bus.Reads);
        Assert.Empty(bus.Writes);
        Assert.Equal(flags, cpu.Flags);
        Assert.Equal(0x81, bus.Memory[0x3000]);
    }

    [Theory]
    [InlineData(0xc0)]
    [InlineData(0xd0)]
    [InlineData(0xd8)]
    public void LoadingAnySegmentDefersHardwareInterruptThroughNextInstruction(int modrm)
    {
        var (cpu, bus) = Start(0xfb, 0x90, 0xb8, 0, 0, 0x8e, (byte)modrm, 0x90, 0x90);
        bus.Vector(0x20, 0x2000);
        cpu.Step(); cpu.Step(); cpu.Step(); cpu.Step();
        cpu.InterruptVector = 0x20;
        cpu.InterruptPending = true;
        cpu.Step();
        Assert.Equal(ProgramAddress + 8, cpu.IP);
        cpu.Step();
        Assert.Equal(0x2000, cpu.IP);
    }

    [Fact]
    public void ClearingTrapWithPopfStillCompletesTheAlreadyArmedSingleStep()
    {
        var (cpu, bus) = Start(0x68, 2, 0, 0x68, 2, 1, 0x9d, 0x9d);
        bus.Vector(1, 0x2000);
        cpu.Step(); cpu.Step(); cpu.Step();
        Assert.Equal(ProgramAddress + 7, cpu.IP);
        cpu.Step();
        Assert.Equal(0x2000, cpu.IP);
        Assert.Equal(ProgramAddress + 8, bus.Word(0x7ffa));
    }

    [Fact]
    public void SoftwareInterruptDoesNotGenerateAnotherSingleStepInsideItsHandler()
    {
        var (cpu, bus) = Start(0x68, 2, 1, 0x9d, 0xcd, 0x20);
        bus.Vector(1, 0x3000);
        bus.Vector(0x20, 0x2000);
        cpu.Step(); cpu.Step(); cpu.Step();
        Assert.Equal(0x2000, cpu.IP);
        Assert.Equal(0x7ffa, cpu.Register(4));
    }

    [Theory]
    [InlineData(0xc6, 0xc8)]
    [InlineData(0xc7, 0xc8)]
    [InlineData(0x8f, 0xc8)]
    public void InvalidGroupEncodingDoesNotWriteDestinationOrPopStack(int opcode, int modrm)
    {
        var (cpu, bus) = Start((byte)opcode, (byte)modrm, 0x55, 0x55);
        bus.Vector(6, 0x2000);
        cpu.Step();
        Assert.Equal(0x2000, cpu.IP);
        Assert.Equal(0, cpu.Register(0));
        Assert.Equal(0x7ffa, cpu.Register(4));
    }

    [Fact]
    public void WordOperandCrossesSegmentEndOnContiguousPhysicalBus()
    {
        var (cpu, bus) = Start(0xa1, 0xff, 0xff, 0xa3, 0xff, 0xff);
        bus.Memory[0xffff] = 0x34;
        bus.Memory[0x10000] = 0x12;
        cpu.Step();
        Assert.Equal(0x1234, cpu.Register(0));
        bus.Writes.Clear(); cpu.Step();
        Assert.Equal(new[] { 0xffff, 0x10000 }, bus.Writes);
    }

    [Fact]
    public void FarPointerCrossesSegmentEndWithoutWrappingItsSegmentWord()
    {
        var (cpu, bus) = Start(0xc4, 6, 0xfe, 0xff);
        bus.Memory[0xfffe] = 0x34;
        bus.Memory[0xffff] = 0x12;
        bus.Memory[0x10000] = 0x78;
        bus.Memory[0x10001] = 0x56;
        cpu.Step();
        Assert.Equal(0x1234, cpu.Register(0));
        Assert.Equal(0x5678, cpu.Segment(0));
    }

    [Theory]
    [InlineData(0x54)]
    [InlineData(0xff)]
    public void BothPushSpEncodingsStoreDecrementedStackPointer(int opcode)
    {
        byte[] program = opcode == 0x54 ? [0x54] : [0xff, 0xf4];
        var (cpu, bus) = Start(program);
        cpu.Step();
        Assert.Equal(0x7ffe, cpu.Register(4));
        Assert.Equal(0x7ffe, bus.Word(0x7ffe));
    }

    [Theory]
    [InlineData(0, 0x8000, 0x8004)]
    [InlineData(6, 0x8000, 0x800a)]
    [InlineData(8, 0xfffc, 0x0008)]
    public void FarReturnUsesImmediateForStackCleanup(int adjustment, int stack, int expectedStack)
    {
        var (cpu, bus) = Start(0xbc, (byte)stack, (byte)(stack >> 8), 0xca,
            (byte)adjustment, (byte)(adjustment >> 8));
        cpu.Step();
        bus.Memory[stack] = 0x45;
        bus.Memory[stack + 1] = 0x23;
        bus.Memory[stack + 2] = 0x34;
        bus.Memory[stack + 3] = 0x12;
        var flags = cpu.Flags;

        cpu.Step();

        Assert.Equal(0x2345, cpu.IP);
        Assert.Equal(0x1234, cpu.Segment(1));
        Assert.Equal(expectedStack, cpu.Register(4));
        Assert.Equal(flags, cpu.Flags);
    }

    [Fact]
    public void PushAllAndPopAllUseSixteenBitBusTimings()
    {
        var (cpu, bus) = Start(0x60, 0x61);
        var before = cpu.Cycles;
        cpu.Step();
        Assert.Equal(36, cpu.Cycles - before);
        Assert.Equal(0x8000, bus.Word(0x7ff6)); // PUSHA conserve le SP initial.
        before = cpu.Cycles;
        cpu.Step();
        Assert.Equal(51, cpu.Cycles - before);
        Assert.Equal(0x8000, cpu.Register(4));
    }

    [Theory]
    [InlineData(0, 15)]
    [InlineData(1, 25)]
    [InlineData(2, 38)]
    public void EnterUsesItsNestingSpecificTiming(int nesting, int cycles)
    {
        var (cpu, _) = Start(0xc8, 0, 0, (byte)nesting);
        var before = cpu.Cycles;
        cpu.Step();
        Assert.Equal(cycles, cpu.Cycles - before);
    }

    [Theory]
    [InlineData(0xf6, 0xe3, 26)]
    [InlineData(0xf7, 0xe3, 35)]
    [InlineData(0xf6, 0xeb, 25)]
    [InlineData(0xf7, 0xeb, 34)]
    [InlineData(0xf6, 0xf3, 29)]
    [InlineData(0xf7, 0xf3, 38)]
    [InlineData(0xf6, 0xfb, 44)]
    [InlineData(0xf7, 0xfb, 53)]
    public void MultiplyAndDivideUseOperationAndWidthSpecificMinimumTimings(int opcode, int modrm, int cycles)
    {
        var (cpu, _) = Start(0xbb, 2, 0, 0xb8, 8, 0, (byte)opcode, (byte)modrm);
        cpu.Step(); cpu.Step();
        var before = cpu.Cycles;
        cpu.Step();
        Assert.Equal(cycles, cpu.Cycles - before);
    }

    [Theory]
    [InlineData(0, 8)]
    [InlineData(1, 12)]
    public void OddWordLoadPaysOneAdditionalBusTransfer(int offset, int cycles)
    {
        var (cpu, _) = Start(0xa1, (byte)offset, 0x30);
        var before = cpu.Cycles;
        cpu.Step();
        Assert.Equal(cycles, cpu.Cycles - before);
    }

    [Theory]
    [InlineData(0x01, 18)]
    [InlineData(0x39, 14)]
    public void OddMemoryAluChargesEachActualWordTransfer(int opcode, int cycles)
    {
        var (cpu, _) = Start((byte)opcode, 6, 1, 0x30);
        var before = cpu.Cycles;
        cpu.Step();
        Assert.Equal(cycles, cpu.Cycles - before);
    }

    [Fact]
    public void RepeatedMovePaysStartupOnceAndPerIterationCostThereafter()
    {
        var (cpu, _) = Start(0xb9, 3, 0, 0xf3, 0xa4, 0x90);
        cpu.Step();
        var before = cpu.Cycles;
        cpu.Step(); cpu.Step(); cpu.Step();
        Assert.Equal(34, cpu.Cycles - before); // Deux cycles terminent la prélecture engagée avant le premier accès de données.
        Assert.Equal(0, cpu.Register(1));
        Assert.Equal(3, cpu.Register(6));
        Assert.Equal(3, cpu.Register(7));
        Assert.Equal(ProgramAddress + 5, cpu.IP);
    }

    [Theory]
    [InlineData(0xa4, 8)]
    [InlineData(0xa6, 5)]
    [InlineData(0xaa, 6)]
    [InlineData(0xac, 6)]
    [InlineData(0xae, 5)]
    public void RepeatWithZeroCountOnlyPaysStartupAndDoesNotAccessData(int opcode, int cycles)
    {
        var (cpu, bus) = Start(0xf3, (byte)opcode);
        bus.Reads.Clear(); bus.Writes.Clear();
        var before = cpu.Cycles;
        cpu.Step();
        Assert.Equal(cycles, cpu.Cycles - before);
        Assert.Empty(bus.Writes);
        Assert.Empty(bus.DataReads);
    }

    [Fact]
    public void InterruptDuringRepeatReturnsToPrefixAndRestartsSetupAfterIret()
    {
        var (cpu, bus) = Start(0xfb, 0xb9, 2, 0, 0xf3, 0xa4);
        bus.Vector(0x20, 0x2000);
        bus.Memory[0x2000] = 0xcf;
        cpu.Step(); cpu.Step(); cpu.Step();
        cpu.InterruptPending = true; cpu.InterruptVector = 0x20;
        cpu.Step();
        Assert.Equal(ProgramAddress + 4, bus.Word(0x7ffa));
        cpu.InterruptPending = false;
        cpu.Step();
        var before = cpu.Cycles;
        cpu.Step();
        Assert.Equal(18, cpu.Cycles - before); // Reprise : terminaison du cycle anticipé avant le premier accès de données.
        Assert.Equal(0, cpu.Register(1));
    }

    [Theory]
    [InlineData(0xcc, 45)]
    [InlineData(0xcd, 47)]
    public void SoftwareInterruptIncludesEntryAndOpcodeSpecificCost(int opcode, int cycles)
    {
        var (cpu, bus) = Start((byte)opcode, 3);
        bus.Vector(3, 0x2000);
        var before = cpu.Cycles;
        cpu.Step();
        Assert.Equal(cycles, cpu.Cycles - before);
    }

    [Fact]
    public void NmiLatchesShortPulseAndHasPriorityOverMaskableInterrupt()
    {
        var (cpu, bus) = Start(0xfb, 0x90, 0x90);
        bus.Vector(2, 0x2000);
        bus.Vector(0x20, 0x3000);
        cpu.Step(); cpu.Step();
        cpu.InterruptPending = true; cpu.InterruptVector = 0x20;
        cpu.Nmi = true; cpu.Nmi = false;
        cpu.Step();
        Assert.Equal(0x2000, cpu.IP);
        Assert.Equal(ProgramAddress + 2, bus.Word(0x7ffa));
    }

    [Fact]
    public void HeldNmiDoesNotRetriggerWithoutAnotherEdge()
    {
        var (cpu, bus) = Start(0x90);
        bus.Vector(2, 0x2000);
        bus.Memory[0x2000] = 0x90;
        bus.Memory[0x2001] = 0x90;
        cpu.Nmi = true;
        cpu.Step();
        cpu.Nmi = true;
        cpu.Step();
        Assert.Equal(0x2001, cpu.IP);
        Assert.Equal(0x7ffa, cpu.Register(4));
        cpu.Nmi = false; cpu.Nmi = true;
        cpu.Step();
        Assert.Equal(0x2000, cpu.IP);
        Assert.Equal(0x7ff4, cpu.Register(4));
    }

    [Fact]
    public void NmiWakesHaltEvenWhenInterruptFlagIsClear()
    {
        var (cpu, bus) = Start(0xf4, 0x90);
        bus.Vector(2, 0x2000);
        cpu.Step(); cpu.Step();
        Assert.Equal(ProgramAddress + 1, cpu.IP);
        cpu.Nmi = true;
        cpu.Step();
        Assert.Equal(0x2000, cpu.IP);
        Assert.Equal(ProgramAddress + 1, bus.Word(0x7ffa));
    }

    [Fact]
    public void SegmentLoadDefersNmiUntilAfterNextInstruction()
    {
        var (cpu, bus) = Start(0x8e, 0xd0, 0x90, 0x90);
        bus.Vector(2, 0x2000);
        cpu.Step();
        cpu.Nmi = true;
        cpu.Step();
        Assert.Equal(ProgramAddress + 3, cpu.IP);
        cpu.Step();
        Assert.Equal(0x2000, cpu.IP);
    }

    [Fact]
    public void WaitRemainsAtItsInstructionUntilTestIndicatesReady()
    {
        var (cpu, _) = Start(0x9b, 0x90);
        cpu.CoprocessorBusy = true;
        cpu.Step(); cpu.Step();
        Assert.Equal(ProgramAddress, cpu.IP);
        cpu.CoprocessorBusy = false;
        cpu.Step();
        Assert.Equal(ProgramAddress + 1, cpu.IP);
        cpu.Step();
        Assert.Equal(ProgramAddress + 2, cpu.IP);
    }

    [Fact]
    public void InterruptedWaitRestartsAfterIretWhileCoprocessorIsStillBusy()
    {
        var (cpu, bus) = Start(0xfb, 0x90, 0x9b, 0x90);
        bus.Vector(0x20, 0x2000);
        bus.Memory[0x2000] = 0xcf;
        cpu.Step(); cpu.Step();
        cpu.CoprocessorBusy = true;
        cpu.Step();
        cpu.InterruptPending = true; cpu.InterruptVector = 0x20;
        cpu.Step();
        Assert.Equal(ProgramAddress + 2, bus.Word(0x7ffa));
        cpu.InterruptPending = false;
        cpu.Step(); cpu.Step();
        Assert.Equal(ProgramAddress + 2, cpu.IP);
    }

    [Fact]
    public void CompletedWaitDefersInterruptUntilAfterFollowingInstruction()
    {
        var (cpu, bus) = Start(0xfb, 0x90, 0x9b, 0x90, 0x90);
        bus.Vector(0x20, 0x2000);
        cpu.Step(); cpu.Step(); cpu.Step();
        cpu.InterruptPending = true; cpu.InterruptVector = 0x20;
        cpu.Step();
        Assert.Equal(ProgramAddress + 4, cpu.IP);
        cpu.Step();
        Assert.Equal(0x2000, cpu.IP);
    }

    [Fact]
    public void PrefixStreamYieldsWithoutMakingPendingNmiInterruptAnIncompleteInstruction()
    {
        var (cpu, bus) = Start(0x26, 0x26, 0x26, 0x26, 0x26, 0x26);
        Array.Fill(bus.Memory, (byte)0x26);
        var before = cpu.Cycles;
        cpu.Step();
        Assert.InRange(cpu.Cycles - before, 0x20000, 0x20008);
        Assert.Equal(ProgramAddress, cpu.IP);
        bus.Memory[ProgramAddress + 8] = 0xb0;
        bus.Memory[ProgramAddress + 9] = 0x55;
        bus.Vector(2, 0x2000);
        cpu.Nmi = true;
        cpu.Step();
        Assert.Equal(0x55, cpu.Register(0));
        Assert.Equal(ProgramAddress + 10, cpu.IP);
        cpu.Step();
        Assert.Equal(0x2000, cpu.IP);
    }

    [Fact]
    public void ResetDiscardsIncompletePrefixesAndPendingNmi()
    {
        var (cpu, bus) = Start(0xf3, 0xf3, 0xf3, 0xf3, 0xf3, 0xf3);
        Array.Fill(bus.Memory, (byte)0xf3);
        cpu.Step();
        cpu.Nmi = true;
        cpu.Reset();
        bus.Memory[0xffff0] = 0x90;
        cpu.Step();
        Assert.Equal(0xffff, cpu.Segment(1));
        Assert.Equal(1, cpu.IP);
    }

    [Fact]
    public void DmaMemoryBlockStopsAtCountZeroAndLeavesInstructionPointerUntouched()
    {
        var (cpu, bus) = Start(0x90);
        bus.Memory[0x3000] = 0x5a;
        bus.Memory[0x3001] = 0xa5;
        ProgramDma(cpu, 0, 0x3000, 0x4000, 2, 0xb606);
        var before = cpu.Cycles;
        cpu.Step(); cpu.Step();
        Assert.Equal(ProgramAddress, cpu.IP);
        Assert.Equal(16, cpu.Cycles - before);
        Assert.Equal(new byte[] { 0x5a, 0xa5 }, bus.Memory[0x4000..0x4002]);
        Assert.Equal(0, cpu.ReadIoWord(0xffc8));
        Assert.Equal(0, cpu.ReadIoWord(0xffca) & 6);
        cpu.Step();
        Assert.Equal(ProgramAddress + 1, cpu.IP);
    }

    [Fact]
    public void DmaStartChangesOnlyWhenChangeBitIsWrittenAndResetStopsBothChannels()
    {
        var (cpu, _) = Start();
        cpu.WriteIoWord(0xffca, 2);
        Assert.Equal(0, cpu.ReadIoWord(0xffca));
        cpu.WriteIoWord(0xffca, 6);
        Assert.Equal(2, cpu.ReadIoWord(0xffca));
        cpu.WriteIoWord(0xffca, 0x2000);
        Assert.Equal(0x2002, cpu.ReadIoWord(0xffca));
        cpu.WriteIoWord(0xffca, 4);
        Assert.Equal(0, cpu.ReadIoWord(0xffca));
        cpu.WriteIoWord(0xffca, 6);
        cpu.WriteIoWord(0xffda, 6);
        cpu.Reset();
        Assert.Equal(0, cpu.ReadIoWord(0xffca));
        Assert.Equal(0, cpu.ReadIoWord(0xffda));
    }

    [Theory]
    [InlineData(0, 0x3000, 0x4000)]
    [InlineData(1, 0x20, 0x4000)]
    public void DmaSelectsIoOrMemoryByControlBitsRatherThanAddressValue(int channel, int source, int destination)
    {
        var (cpu, bus) = Start(0x90);
        bus.Memory[source] = 0x55;
        bus.Inputs[(ushort)source] = 0xa5;
        ProgramDma(cpu, channel, source, destination, 1, 0x8246);
        cpu.Step();
        Assert.Equal(0, bus.Memory[destination]);
        cpu.SetDmaRequest(channel, true);
        cpu.Step();
        Assert.Equal(0xa5, bus.Memory[destination]);
        Assert.Equal(new ushort[] { (ushort)source }, bus.PortReads);
        Assert.Equal(0, cpu.ReadIoWord((ushort)(0xffca + channel * 0x10)) & 2);
    }

    [Fact]
    public void DmaMemoryToIoKeepsPortFixedAndAdvancesMemory()
    {
        var (cpu, bus) = Start();
        bus.Memory[0x3000] = 0x12;
        bus.Memory[0x3001] = 0x34;
        ProgramDma(cpu, 1, 0x3000, 0x80, 2, 0x1646);
        cpu.SetDmaRequest(1, true);
        cpu.Step(); cpu.Step();
        Assert.Equal(new (ushort, byte)[] { (0x80, 0x12), (0x80, 0x34) }, bus.PortWrites);
        Assert.Equal(0x3002, cpu.ReadIoWord(0xffd0));
        Assert.Equal(0x80, cpu.ReadIoWord(0xffd4));
    }

    [Fact]
    public void DmaWordCrossesPhysicalAddressWrapAndDecrementsTwentyBitPointers()
    {
        var (cpu, bus) = Start();
        bus.Memory[0xfffff] = 0x12;
        bus.Memory[0] = 0x34;
        ProgramDma(cpu, 0, 0xfffff, 0x4001, 1, 0xda07);
        var before = cpu.Cycles;
        cpu.Step();
        Assert.Equal(0x3412, bus.Word(0x4001));
        Assert.Equal(19, cpu.Cycles - before); // UCS RESET : trois attentes sur le premier cycle à FFFFF.
        Assert.Equal(0xfffd, cpu.ReadIoWord(0xffc0));
        Assert.Equal(15, cpu.ReadIoWord(0xffc2));
        Assert.Equal(0x3fff, cpu.ReadIoWord(0xffc4));
    }

    [Fact]
    public void DmaInitialZeroCountMeans65536TransfersRatherThanAnEmptyBlock()
    {
        var (cpu, bus) = Start();
        bus.Memory[0x3000] = 0x5a;
        ProgramDma(cpu, 0, 0x3000, 0x4000, 0, 0x9206);
        cpu.Step();
        Assert.Equal(0x5a, bus.Memory[0x4000]);
        Assert.Equal(0xffff, cpu.ReadIoWord(0xffc8));
        Assert.Equal(2, cpu.ReadIoWord(0xffca) & 2);
    }

    [Theory]
    [InlineData(0x9046, true)]
    [InlineData(0x9006, false)]
    [InlineData(0x9246, false)]
    public void DmaCountZeroContinuesOnlyForSynchronizedChannelWithoutTerminateBit(int control, bool continues)
    {
        var (cpu, _) = Start();
        ProgramDma(cpu, 0, 0x3000, 0x4000, 1, (ushort)control);
        cpu.SetDmaRequest(0, true);
        cpu.Step();
        Assert.Equal(continues, (cpu.ReadIoWord(0xffca) & 2) != 0);
        if (continues)
        {
            cpu.Step();
            Assert.Equal(0xffff, cpu.ReadIoWord(0xffc8));
        }
    }

    [Theory]
    [InlineData(0x9006, 0x9006, 0x11, 0x22)]
    [InlineData(0x9006, 0x9026, 0x22, 0x22)]
    [InlineData(0x9026, 0x9006, 0x11, 0x11)]
    public void DmaArbitratesRoundRobinAtEqualPriorityAndHonorsHigherPriority(int control0, int control1, int first, int second)
    {
        var (cpu, bus) = Start();
        bus.Memory[0x3000] = 0x11;
        bus.Memory[0x3100] = 0x22;
        ProgramDma(cpu, 0, 0x3000, 0x4000, 2, (ushort)control0);
        ProgramDma(cpu, 1, 0x3100, 0x4000, 2, (ushort)control1);
        cpu.Step();
        Assert.Equal(first, bus.Memory[0x4000]);
        cpu.Step();
        Assert.Equal(second, bus.Memory[0x4000]);
    }

    [Fact]
    public void OutWordProgramsDmaAtomicallyAndInWordReadsTheCount()
    {
        var (cpu, _) = Start(0xba, 0xc8, 0xff, 0xb8, 0x34, 0x12, 0xef, 0xb8, 0, 0, 0xed);
        for (var step = 0; step < 5; step++) cpu.Step();
        Assert.Equal(0x1234, cpu.Register(0));
        Assert.Equal(0x1234, cpu.ReadIoWord(0xffc8));
    }

    [Theory]
    [InlineData(0xffc8, 0x1234)]
    [InlineData(0xffc9, 0x3412)]
    public void OutAlPresentsBothAccumulatorBytesToInternalBus(int port, int expected)
    {
        var (cpu, _) = Start(0xba, (byte)port, (byte)(port >> 8), 0xb8, 0x34, 0x12, 0xee);
        cpu.Step(); cpu.Step(); cpu.Step();
        Assert.Equal(expected, cpu.ReadIoWord(0xffc8));
    }

    [Fact]
    public void InternalOddWordAccessSwapsLanesWithoutTouchingNextRegister()
    {
        var (cpu, _) = Start();
        cpu.WriteIoWord(0xffc9, 0x1234);
        Assert.Equal(0x3412, cpu.ReadIoWord(0xffc8));
        Assert.Equal(0x1234, cpu.ReadIoWord(0xffc9));
        Assert.Equal(0x12, cpu.ReadIoByte(0xffc8));
        Assert.Equal(0x34, cpu.ReadIoByte(0xffc9));
        Assert.Equal(0, cpu.ReadIoWord(0xffca));
        cpu.WriteIoByte(0xffc8, 0x56);
        Assert.Equal(0x56, cpu.ReadIoWord(0xffc8));
    }

    [Fact]
    public void DestinationSynchronizedDmaReleasesTheBusBetweenTransfers()
    {
        var (cpu, bus) = Start(0x90);
        bus.Memory[0x3000] = 0x5a;
        ProgramDma(cpu, 0, 0x3000, 0x4000, 2, 0xb686);
        cpu.SetDmaRequest(0, true);
        var before = cpu.Cycles;
        cpu.Step();
        Assert.Equal(10, cpu.Cycles - before);
        Assert.Equal(1, cpu.ReadIoWord(0xffc8));
        cpu.Step();
        Assert.Equal(ProgramAddress + 1, cpu.IP);
        Assert.Equal(1, cpu.ReadIoWord(0xffc8));
        cpu.Step();
        Assert.Equal(0, cpu.ReadIoWord(0xffc8));
    }

    [Fact]
    public void PeripheralRelocationMovesIoRegistersAndReleasesTheirOldPorts()
    {
        var (cpu, bus) = Start();
        Assert.Equal(0x20ff, cpu.ReadIoWord(0xfffe));
        cpu.WriteIoWord(0xffc8, 0x1234);
        cpu.WriteIoWord(0xfffe, 0x2012);
        Assert.Equal(0x2012, cpu.ReadIoWord(0x12fe));
        Assert.Equal(0x1234, cpu.ReadIoWord(0x12c8));
        cpu.WriteIoWord(0xffc8, 0xabcd);
        Assert.Equal(new (ushort, byte)[] { (0xffc8, 0xcd), (0xffc9, 0xab) }, bus.PortWrites);
        Assert.Equal(0x1234, cpu.ReadIoWord(0x12c8));
        cpu.Reset();
        Assert.Equal(0x20ff, cpu.ReadIoWord(0xfffe));
        Assert.Equal(0, cpu.ReadIoWord(0xffc8));
    }

    [Theory]
    [InlineData(0xc8, 0x1234)]
    [InlineData(0xc9, 0x3412)]
    public void MemoryMappedPeripheralWordsUseInternalBusWithoutWritingExternalRam(int offset, int expected)
    {
        var (cpu, bus) = Start(0xb8, 0, 0x10, 0x8e, 0xd8, 0xb8, 0x34, 0x12,
            0xa3, (byte)offset, 0, 0xa1, 0xc8, 0);
        cpu.WriteIoWord(0xfffe, 0x1100);
        for (var step = 0; step < 5; step++) cpu.Step();
        Assert.Equal(expected, cpu.Register(0));
        Assert.DoesNotContain(0x100c8, bus.Reads);
        Assert.DoesNotContain(0x100c8, bus.Writes);
        Assert.Equal(0, bus.Word(0x100c8));
        Assert.Equal(0xffff, cpu.ReadIoWord(0xffc8));
    }

    [Fact]
    public void DmaUsesTheRelocatedPeripheralDecodeForMemoryTransfers()
    {
        var (cpu, bus) = Start(0xb8, 0, 0x10, 0x8e, 0xd8, 0xa1, 0xd8, 0);
        bus.Memory[0x3000] = 0x34;
        bus.Memory[0x3001] = 0x12;
        ProgramDma(cpu, 0, 0x3000, 0x100d8, 1, 0x9207);
        cpu.WriteIoWord(0xfffe, 0x1100);
        cpu.Step();
        for (var step = 0; step < 3; step++) cpu.Step();
        Assert.Equal(0x1234, cpu.Register(0));
        Assert.DoesNotContain(0x100d8, bus.Writes);
    }

    [Theory]
    [InlineData(0x3000, 6)]
    [InlineData(0x3001, 10)]
    public void EscapeReadsAndDiscardsAMemoryWord(int address, int cycles)
    {
        var (cpu, bus) = Start(0xd8, 6, (byte)address, (byte)(address >> 8));
        bus.Memory[address] = 0x5a;
        bus.Memory[address + 1] = 0xa5;
        var before = cpu.Cycles;
        cpu.Step();
        Assert.Equal(cycles, cpu.Cycles - before);
        Assert.Contains(address, bus.Reads);
        Assert.Contains(address + 1, bus.Reads);
        Assert.Empty(bus.Writes);
        Assert.Equal(0, cpu.Register(0));
    }

    [Fact]
    public void EscapeRegisterFormDoesNotReadAnOperand()
    {
        var (cpu, bus) = Start(0xdf, 0xe0);
        cpu.Step();
        Assert.Empty(bus.DataReads);
        Assert.Empty(bus.Writes);
    }

    [Fact]
    public void EscapeTrapReturnsToTheFirstPrefixWithoutReadingTheModRmOperand()
    {
        var (cpu, bus) = Start(0x26, 0xd8, 6, 0, 0x30);
        bus.Vector(7, 0x2000);
        cpu.WriteIoWord(0xfffe, 0xa0ff);
        cpu.Step();
        Assert.Equal(0x2000, cpu.IP);
        Assert.Equal(ProgramAddress, bus.Word(0x7ffa));
        Assert.DoesNotContain(ProgramAddress + 2, bus.DataReads);
        Assert.DoesNotContain(0x3000, bus.Reads);
    }

    [Fact]
    public void IntegratedDmaInterruptRespectsMaskAndInterruptFlagThenAcknowledgesItsOwnVector()
    {
        var (cpu, bus) = Start(0xfb, 0x90, 0x90);
        bus.Vector(0x0a, 0x2000);
        cpu.Step(); cpu.Step();
        ProgramDma(cpu, 0, 0x3000, 0x4000, 1, 0x9306);
        cpu.Step();
        Assert.Equal(4, cpu.ReadIoWord(0xff2e));
        cpu.Step();
        Assert.Equal(ProgramAddress + 3, cpu.IP);
        cpu.WriteIoWord(0xff34, 0);
        cpu.Step();
        Assert.Equal(0x2000, cpu.IP);
        Assert.Equal(4, cpu.ReadIoWord(0xff2c));
        Assert.Equal(0, cpu.ReadIoWord(0xff2e));
        Assert.Equal(ProgramAddress + 3, bus.Word(0x7ffa));
    }

    [Fact]
    public void PollStatusDoesNotAcknowledgeAndPollDoesWithPriorityAndInServiceBlocking()
    {
        var (cpu, _) = Start();
        cpu.WriteIoWord(0xff34, 2); // DMA 0 priorité 2.
        cpu.WriteIoWord(0xff38, 1); // INT0 priorité 1.
        cpu.WriteIoWord(0xff2e, 4);
        cpu.SetExternalInterrupt(0, true);
        Assert.Equal(0x800c, cpu.ReadIoWord(0xff26));
        Assert.Equal(0, cpu.ReadIoWord(0xff2c));
        Assert.Equal(0x800c, cpu.ReadIoWord(0xff24));
        Assert.Equal(0x10, cpu.ReadIoWord(0xff2c));
        Assert.Equal(0x0c, cpu.ReadIoWord(0xff26));
        cpu.WriteIoWord(0xff22, 0x0c);
        Assert.Equal(0x800a, cpu.ReadIoWord(0xff24));
        Assert.Equal(4, cpu.ReadIoWord(0xff2c));
        cpu.WriteIoWord(0xff22, 0x8000);
        Assert.Equal(0, cpu.ReadIoWord(0xff2c));
        Assert.Equal(0x0a, cpu.ReadIoWord(0xff26));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0x10, true)]
    public void ExternalInterruptHeldHighRetriggersAfterEoiOnlyInLevelMode(int control, bool retriggers)
    {
        var (cpu, _) = Start();
        cpu.WriteIoWord(0xff38, (ushort)control);
        cpu.SetExternalInterrupt(0, true);
        Assert.Equal(0x800c, cpu.ReadIoWord(0xff24));
        cpu.WriteIoWord(0xff22, 0x0c);
        cpu.SetExternalInterrupt(0, true);
        Assert.Equal(retriggers, (cpu.ReadIoWord(0xff26) & 0x8000) != 0);
        cpu.SetExternalInterrupt(0, false);
        Assert.Equal(0x0c, cpu.ReadIoWord(0xff26));
        cpu.SetExternalInterrupt(0, true);
        Assert.Equal(0x800c, cpu.ReadIoWord(0xff26));
    }

    [Fact]
    public void PriorityMaskBlocksLowerPriorityAndMaskRegisterSharesControlMaskBits()
    {
        var (cpu, _) = Start();
        Assert.Equal(0xfd, cpu.ReadIoWord(0xff28));
        cpu.WriteIoWord(0xff38, 3);
        cpu.WriteIoWord(0xff2a, 2);
        cpu.SetExternalInterrupt(0, true);
        Assert.Equal(0, cpu.ReadIoWord(0xff26));
        cpu.WriteIoWord(0xff2a, 3);
        Assert.Equal(0x800c, cpu.ReadIoWord(0xff26));
        cpu.WriteIoWord(0xff28, 0xfd);
        Assert.Equal(0x0b, cpu.ReadIoWord(0xff38));
        Assert.Equal(0, cpu.ReadIoWord(0xff26));
        cpu.WriteIoWord(0xff28, 0xed);
        Assert.Equal(3, cpu.ReadIoWord(0xff38));
        Assert.Equal(0x800c, cpu.ReadIoWord(0xff26));
    }

    [Fact]
    public void TimersShareInServiceBitButRetainTheirIndividualPendingStatus()
    {
        var (cpu, _) = Start();
        cpu.WriteIoWord(0xff32, 0);
        cpu.WriteIoWord(0xff30, 7);
        Assert.Equal(0x8008, cpu.ReadIoWord(0xff24));
        Assert.Equal(6, cpu.ReadIoWord(0xff30));
        Assert.Equal(1, cpu.ReadIoWord(0xff2c));
        Assert.Equal(8, cpu.ReadIoWord(0xff26));
        cpu.WriteIoWord(0xff22, 8);
        Assert.Equal(0x8012, cpu.ReadIoWord(0xff24));
        cpu.WriteIoWord(0xff22, 0x12);
        Assert.Equal(0x8013, cpu.ReadIoWord(0xff24));
        Assert.Equal(0, cpu.ReadIoWord(0xff30));
    }

    [Fact]
    public void CascadeUsesTheExternalVectorAndDisablesCorrespondingAcknowledgeInput()
    {
        var (cpu, bus) = Start(0xfb, 0x90);
        bus.Vector(0x44, 0x2000);
        cpu.CascadeAcknowledge = pin => { Assert.Equal(0, pin); return 0x44; };
        cpu.WriteIoWord(0xff38, 0x20);
        cpu.WriteIoWord(0xff3c, 0);
        cpu.SetExternalInterrupt(2, true);
        Assert.Equal(0, cpu.ReadIoWord(0xff26));
        cpu.Step(); cpu.Step();
        cpu.SetExternalInterrupt(0, true);
        cpu.Step();
        Assert.Equal(0x2000, cpu.IP);
        Assert.Equal(0x10, cpu.ReadIoWord(0xff2c));
    }

    [Fact]
    public void SlaveModeUsesPriorityVectorsAndSeparateTimerAndDmaSources()
    {
        var (cpu, _) = Start();
        cpu.WriteIoWord(0xfffe, 0x60ff);
        cpu.WriteIoWord(0xff20, 0x53);
        cpu.WriteIoWord(0xff38, 2); // Timer 1.
        cpu.WriteIoWord(0xff30, 2);
        Assert.Equal(0x8052, cpu.ReadIoWord(0xff24));
        Assert.Equal(0x10, cpu.ReadIoWord(0xff2c));
        cpu.WriteIoWord(0xff22, 2);
        Assert.Equal(0, cpu.ReadIoWord(0xff2c));
        cpu.WriteIoWord(0xff34, 1);
        cpu.WriteIoWord(0xff2e, 8); // En mode esclave, DMA 0 est sur le bit 3.
        Assert.Equal(0x8051, cpu.ReadIoWord(0xff24));
        Assert.Equal(8, cpu.ReadIoWord(0xff2c));
        Assert.Equal(0, cpu.ReadIoWord(0xff3c));
    }

    [Fact]
    public void TimersResetDisabledWithAlternateModeAndHighOutputs()
    {
        var (cpu, _) = StartHalted();
        Assert.Equal(2, cpu.ReadIoWord(0xff56));
        Assert.Equal(2, cpu.ReadIoWord(0xff5e));
        Assert.Equal(0, cpu.ReadIoWord(0xff66));
        Assert.True(cpu.TimerOutput(0));
        Assert.True(cpu.TimerOutput(1));
        cpu.WriteIoWord(0xff56, 0x8000);
        Assert.Equal(0, cpu.ReadIoWord(0xff56) & 0x8000);
        cpu.WriteIoWord(0xff56, 0xc000);
        cpu.WriteIoWord(0xff56, 0);
        Assert.Equal(0x8000, cpu.ReadIoWord(0xff56) & 0x8000);
        cpu.WriteIoWord(0xff56, 0x4000);
        Assert.Equal(0, cpu.ReadIoWord(0xff56) & 0xc000);
    }

    [Fact]
    public void TimerUsesDividedClockInputGateAndOneClockOutputPulse()
    {
        var (cpu, _) = StartHalted();
        var edges = new List<(int Timer, bool Level)>();
        cpu.TimerOutputChanged = (timer, level) => edges.Add((timer, level));
        cpu.WriteIoWord(0xff52, 3);
        cpu.WriteIoWord(0xff56, 0xe001);
        IdleClocks(cpu, 8);
        Assert.Equal(0, cpu.ReadIoWord(0xff50));
        cpu.SetTimerInput(0, true);
        IdleClocks(cpu, 8);
        Assert.Equal(2, cpu.ReadIoWord(0xff50));
        IdleClocks(cpu, 4);
        Assert.Equal(0, cpu.ReadIoWord(0xff50));
        Assert.Equal(1, cpu.ReadIoWord(0xff30));
        Assert.True(cpu.TimerOutput(0));
        IdleClocks(cpu, 1);
        Assert.False(cpu.TimerOutput(0));
        IdleClocks(cpu, 1);
        Assert.True(cpu.TimerOutput(0));
        Assert.Equal(new (int, bool)[] { (0, false), (0, true) }, edges);
    }

    [Fact]
    public void TimerInputGateCanCloseAndReopenWithoutResettingCountOrClockPhase()
    {
        var (cpu, _) = StartHalted();
        cpu.WriteIoWord(0xff52, 100);
        cpu.WriteIoWord(0xff56, 0xc001);
        cpu.SetTimerInput(0, true);
        IdleClocks(cpu, 8);
        Assert.Equal(2, cpu.ReadIoWord(0xff50));
        cpu.SetTimerInput(0, false);
        IdleClocks(cpu, 9);
        Assert.Equal(2, cpu.ReadIoWord(0xff50));
        cpu.SetTimerInput(0, true);
        IdleClocks(cpu, 2);
        Assert.Equal(2, cpu.ReadIoWord(0xff50));
        IdleClocks(cpu, 1);
        Assert.Equal(3, cpu.ReadIoWord(0xff50));
    }

    [Fact]
    public void TimerClockSourceChangesTakeEffectWithoutResetOrDoubleCounting()
    {
        var (cpu, _) = StartHalted();
        cpu.WriteIoWord(0xff52, 100);
        cpu.WriteIoWord(0xff56, 0xc005);
        cpu.SetTimerInput(0, true);
        IdleClocks(cpu, 8);
        Assert.Equal(1, cpu.ReadIoWord(0xff50));
        cpu.WriteIoWord(0xff56, 0xc001);
        IdleClocks(cpu, 8);
        Assert.Equal(3, cpu.ReadIoWord(0xff50));
        cpu.WriteIoWord(0xff56, 0xc009);
        IdleClocks(cpu, 8);
        Assert.Equal(3, cpu.ReadIoWord(0xff50));
        cpu.WriteIoWord(0xff62, 2);
        cpu.WriteIoWord(0xff66, 0xc001);
        IdleClocks(cpu, 8);
        Assert.Equal(4, cpu.ReadIoWord(0xff50));
        cpu.SetTimerInput(0, false);
        IdleClocks(cpu, 8);
        Assert.Equal(4, cpu.ReadIoWord(0xff50));
        cpu.SetTimerInput(0, true);
        IdleClocks(cpu, 8);
        Assert.Equal(5, cpu.ReadIoWord(0xff50));
    }

    [Fact]
    public void ExternalTimerCountsRisingEdgesWithoutCountingHeldLevelOrCpuClocks()
    {
        var (cpu, _) = StartHalted();
        cpu.WriteIoWord(0xff52, 3);
        cpu.WriteIoWord(0xff56, 0xc004);
        cpu.SetTimerInput(0, true);
        cpu.SetTimerInput(0, true);
        IdleClocks(cpu, 12);
        Assert.Equal(1, cpu.ReadIoWord(0xff50));
        cpu.SetTimerInput(0, false); cpu.SetTimerInput(0, true);
        cpu.SetTimerInput(0, false); cpu.SetTimerInput(0, true);
        Assert.Equal(0, cpu.ReadIoWord(0xff50));
        Assert.Equal(0, cpu.ReadIoWord(0xff56) & 0x8000);
        Assert.Equal(0x20, cpu.ReadIoWord(0xff56) & 0x20);
    }

    [Fact]
    public void RetriggeredTimerWaitsForAnEdgeAndRestartsOnEachNewEdge()
    {
        var (cpu, _) = StartHalted();
        cpu.WriteIoWord(0xff52, 3);
        cpu.WriteIoWord(0xff56, 0xc010);
        IdleClocks(cpu, 8);
        Assert.Equal(0, cpu.ReadIoWord(0xff50));
        cpu.SetTimerInput(0, true);
        IdleClocks(cpu, 8);
        Assert.Equal(2, cpu.ReadIoWord(0xff50));
        cpu.SetTimerInput(0, false); cpu.SetTimerInput(0, true);
        Assert.Equal(0, cpu.ReadIoWord(0xff50));
        IdleClocks(cpu, 8);
        Assert.Equal(2, cpu.ReadIoWord(0xff50));
        IdleClocks(cpu, 4);
        Assert.Equal(0, cpu.ReadIoWord(0xff56) & 0x8000);
    }

    [Fact]
    public void AlternateOneShotCountsAThenBAndChangesOutputAtEachBoundary()
    {
        var (cpu, _) = StartHalted();
        cpu.SetTimerInput(1, true);
        cpu.WriteIoWord(0xff5a, 2);
        cpu.WriteIoWord(0xff5c, 3);
        cpu.WriteIoWord(0xff5e, 0xc002);
        IdleClocks(cpu, 8);
        Assert.Equal(0x9000, cpu.ReadIoWord(0xff5e) & 0x9000);
        IdleClocks(cpu, 1);
        Assert.False(cpu.TimerOutput(1));
        IdleClocks(cpu, 11);
        Assert.Equal(0, cpu.ReadIoWord(0xff5e) & 0x9000);
        IdleClocks(cpu, 1);
        Assert.True(cpu.TimerOutput(1));
    }

    [Fact]
    public void Timer2PrescalesAnotherTimerRatherThanGivingItTheCpuClock()
    {
        var (cpu, _) = StartHalted();
        cpu.SetTimerInput(0, true);
        cpu.WriteIoWord(0xff52, 2);
        cpu.WriteIoWord(0xff56, 0xc009);
        cpu.WriteIoWord(0xff62, 2);
        cpu.WriteIoWord(0xff66, 0xc001);
        IdleClocks(cpu, 8);
        Assert.Equal(1, cpu.ReadIoWord(0xff50));
        IdleClocks(cpu, 8);
        Assert.Equal(0, cpu.ReadIoWord(0xff50));
        Assert.Equal(0x20, cpu.ReadIoWord(0xff56) & 0x20);
    }

    [Fact]
    public void TimerZeroMaximumMeansFullSixteenBitCount()
    {
        var (cpu, _) = StartHalted();
        cpu.WriteIoWord(0xff60, 0xfffe);
        cpu.WriteIoWord(0xff62, 0);
        cpu.WriteIoWord(0xff66, 0xc000);
        IdleClocks(cpu, 4);
        Assert.Equal(0xffff, cpu.ReadIoWord(0xff60));
        Assert.Equal(0x8000, cpu.ReadIoWord(0xff66) & 0x8000);
        IdleClocks(cpu, 4);
        Assert.Equal(0, cpu.ReadIoWord(0xff60));
        Assert.Equal(0, cpu.ReadIoWord(0xff66) & 0x8000);
    }

    [Fact]
    public void Timer2TimeoutProducesOnlyOneDmaTransferWhenBothChannelsUseItsSharedLatch()
    {
        var (cpu, bus) = StartHalted();
        bus.Memory[0x3000] = 0x11;
        bus.Memory[0x3100] = 0x22;
        ProgramDma(cpu, 0, 0x3000, 0x4000, 2, 0x9216);
        ProgramDma(cpu, 1, 0x3100, 0x4100, 2, 0x9216);
        cpu.WriteIoWord(0xff62, 4);
        cpu.WriteIoWord(0xff66, 0xc000);
        RunUntilTimer2Stops(cpu);
        cpu.Step();
        Assert.Equal(0x11, bus.Memory[0x4000]);
        Assert.Equal(0, bus.Memory[0x4100]);
        cpu.Step();
        Assert.Equal(2, cpu.ReadIoWord(0xffd8));
        cpu.WriteIoWord(0xff66, 0xc000);
        RunUntilTimer2Stops(cpu);
        cpu.Step();
        Assert.Equal(0x22, bus.Memory[0x4100]);
        Assert.Equal(1, cpu.ReadIoWord(0xffd8));
    }

    [Fact]
    public void DmaHaltStatusSuspendsTransfersUntilSoftwareClearsIt()
    {
        var (cpu, bus) = StartHalted();
        bus.Memory[0x3000] = 0x5a;
        ProgramDma(cpu, 0, 0x3000, 0x4000, 1, 0x9206);
        cpu.WriteIoWord(0xff30, 0x8000);
        cpu.Step();
        Assert.Equal(1, cpu.ReadIoWord(0xffc8));
        Assert.Equal(0, bus.Memory[0x4000]);
        cpu.WriteIoWord(0xff30, 0);
        cpu.Step();
        Assert.Equal(0x5a, bus.Memory[0x4000]);
        Assert.Equal(0, cpu.ReadIoWord(0xffc8));
    }

    [Fact]
    public void HardwareNmiHaltsReadyDmaUntilIretWithoutChangingChannelRegisters()
    {
        var (cpu, bus) = StartHalted();
        bus.Vector(2, 0x2000);
        bus.Memory[0x2000] = 0xcf;
        bus.Memory[0x3000] = 0x5a;
        ProgramDma(cpu, 0, 0x3000, 0x4000, 1, 0x9206);
        cpu.Nmi = true;
        Assert.Equal(0x8000, cpu.ReadIoWord(0xff30) & 0x8000);
        cpu.Step();
        Assert.Equal(0x2000, cpu.IP);
        Assert.Equal(1, cpu.ReadIoWord(0xffc8));
        Assert.Equal(0x3000, cpu.ReadIoWord(0xffc0));
        Assert.Equal(0x4000, cpu.ReadIoWord(0xffc4));
        cpu.Step();
        Assert.Equal(0, cpu.ReadIoWord(0xff30) & 0x8000);
        Assert.Equal(1, cpu.ReadIoWord(0xffc8));
        cpu.Step();
        Assert.Equal(0x5a, bus.Memory[0x4000]);
    }

    [Fact]
    public void NmiDuringWordDmaFinishesBothByteLanesBeforeHaltingNextTransfer()
    {
        var (cpu, bus) = StartHalted();
        bus.Vector(2, 0x2000);
        bus.Memory[0x3000] = 0x34;
        bus.Memory[0x3001] = 0x12;
        ProgramDma(cpu, 0, 0x3000, 0x4000, 2, 0x9207);
        bus.Written = address => { if (address == 0x4000) cpu.Nmi = true; };
        cpu.Step();
        Assert.Equal(0x1234, bus.Word(0x4000));
        Assert.Equal(1, cpu.ReadIoWord(0xffc8));
        cpu.Step();
        Assert.Equal(0x2000, cpu.IP);
        Assert.Equal(1, cpu.ReadIoWord(0xffc8));
        Assert.Equal(0, bus.Word(0x4002));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SlaveInterruptModeDoesNotHaltDmaForStatusBitOrNmi(bool nmi)
    {
        var (cpu, bus) = StartHalted();
        cpu.WriteIoWord(0xfffe, 0x60ff);
        bus.Memory[0x3000] = 0x5a;
        ProgramDma(cpu, 0, 0x3000, 0x4000, 1, 0x9206);
        if (nmi) cpu.Nmi = true;
        else cpu.WriteIoWord(0xff30, 0x8000);
        cpu.Step();
        Assert.Equal(0x5a, bus.Memory[0x4000]);
        Assert.Equal(nmi ? 0 : 0x8000, cpu.ReadIoWord(0xff30) & 0x8000);
    }

    [Fact]
    public void SoftwareInterruptTwoDoesNotAssertDmaHalt()
    {
        var (cpu, bus) = Start(0xcd, 2);
        bus.Vector(2, 0x2000);
        cpu.Step();
        Assert.Equal(0x2000, cpu.IP);
        Assert.Equal(0, cpu.ReadIoWord(0xff30) & 0x8000);
    }

    [Fact]
    public void HoldKeepsBothCpuAndDmaOffTheBusWhileInternalTimersContinue()
    {
        var (cpu, bus) = StartHalted();
        bus.Memory[0x3000] = 0x5a;
        ProgramDma(cpu, 0, 0x3000, 0x4000, 1, 0x9206);
        cpu.WriteIoWord(0xff62, 2);
        cpu.WriteIoWord(0xff66, 0xc000);
        cpu.HoldInput = true;
        bus.Reads.Clear(); bus.Writes.Clear();
        IdleClocks(cpu, 8);
        Assert.Empty(bus.Reads);
        Assert.Empty(bus.Writes);
        Assert.Equal(0, cpu.ReadIoWord(0xff66) & 0x8000);
        Assert.Equal(1, cpu.ReadIoWord(0xffc8));
        cpu.HoldInput = false;
        cpu.Step();
        Assert.Equal(0x5a, bus.Memory[0x4000]);
    }

    [Fact]
    public void Timer1OutputPulseLatchesNmiEvenWhenBothEdgesOccurInOneCpuStep()
    {
        var (cpu, bus) = StartHalted();
        bus.Vector(2, 0x2000);
        cpu.TimerOutputChanged = (timer, level) => { if (timer == 1) cpu.Nmi = !level; };
        cpu.SetTimerInput(1, true);
        cpu.WriteIoWord(0xff5a, 1);
        cpu.WriteIoWord(0xff5e, 0xc000);
        cpu.HoldInput = true;
        IdleClocks(cpu, 6);
        Assert.True(cpu.TimerOutput(1));
        Assert.False(cpu.Nmi);
        cpu.HoldInput = false;
        cpu.Step();
        Assert.Equal(0x2000, cpu.IP);
    }

    [Fact]
    public void ClearingBothTimerRequestEnableBitsDiscardsAnUnconsumedTimerDmaLatch()
    {
        var (cpu, bus) = StartHalted();
        bus.Memory[0x3000] = 0x5a;
        ProgramDma(cpu, 0, 0x3000, 0x4000, 1, 0x9214); // TDRQ armé, canal arrêté.
        cpu.WriteIoWord(0xff62, 1);
        cpu.WriteIoWord(0xff66, 0xc000);
        RunUntilTimer2Stops(cpu);
        cpu.WriteIoWord(0xffca, 0x9204);
        cpu.WriteIoWord(0xffca, 0x9216);
        cpu.Step();
        Assert.Equal(0, bus.Memory[0x4000]);
        Assert.Equal(1, cpu.ReadIoWord(0xffc8));
        cpu.WriteIoWord(0xff66, 0xc000);
        RunUntilTimer2Stops(cpu);
        cpu.Step();
        Assert.Equal(0x5a, bus.Memory[0x4000]);
    }

    [Fact]
    public void CpuAcknowledgesHoldBeforeBusAccessAndDropsHldaBeforeResumingDma()
    {
        var (cpu, bus) = Start(0x90);
        var acknowledgements = new List<bool>();
        var clocks = new List<long>();
        cpu.HoldAcknowledgeChanged = acknowledgements.Add;
        cpu.BusClocksAdvanced = clocks.Add;
        bus.Memory[0x3000] = 0x5a;
        ProgramDma(cpu, 0, 0x3000, 0x4000, 1, 0x9206);
        cpu.HoldInput = true;
        cpu.Step();
        Assert.True(cpu.HoldAcknowledged);
        Assert.Empty(bus.Reads);
        Assert.Empty(bus.Writes);
        cpu.HoldInput = false;
        cpu.Step();
        Assert.False(cpu.HoldAcknowledged);
        Assert.Equal(new[] { true, false }, acknowledgements);
        Assert.Equal(new long[] { 1, 8 }, clocks);
        Assert.Equal(0x5a, bus.Memory[0x4000]);
    }

    [Fact]
    public void LockedRepeatedMoveDefersBothHoldAndDmaUntilTheStringFinishes()
    {
        var (cpu, bus) = Start(0xbe, 0, 0x30, 0xbf, 0, 0x40, 0xb9, 2, 0, 0xf0, 0xf3, 0xa4, 0x90);
        bus.Memory[0x3000] = 0x11;
        bus.Memory[0x3001] = 0x22;
        bus.Memory[0x3100] = 0x5a;
        for (var step = 0; step < 4; step++) cpu.Step();
        ProgramDma(cpu, 0, 0x3100, 0x4100, 1, 0x9206);
        cpu.HoldInput = true;
        cpu.Step();
        Assert.False(cpu.HoldAcknowledged);
        Assert.Equal(0x11, bus.Memory[0x4000]);
        Assert.Equal(0x22, bus.Memory[0x4001]);
        Assert.Equal(0, bus.Memory[0x4100]);
        Assert.Equal(0, cpu.Register(1));
        cpu.Step();
        Assert.True(cpu.HoldAcknowledged);
        Assert.Equal(0, bus.Memory[0x4100]);
        cpu.HoldInput = false;
        cpu.Step();
        Assert.Equal(0x5a, bus.Memory[0x4100]);
        Assert.Equal(ProgramAddress + 12, cpu.IP);
    }

    [Fact]
    public void InterruptReleasesRepeatedStringLockAndResetDropsHlda()
    {
        var (cpu, bus) = Start(0xb9, 2, 0, 0xf0, 0xf3, 0xa4);
        bus.Vector(2, 0x2000);
        cpu.Step(); cpu.Step();
        cpu.HoldInput = true;
        cpu.Nmi = true;
        cpu.Step();
        Assert.Equal(0x2000, cpu.IP);
        cpu.Step();
        Assert.True(cpu.HoldAcknowledged);
        cpu.Reset();
        Assert.False(cpu.HoldAcknowledged);
    }

    [Fact]
    public void ChipSelectResetEnablesOnlyTheTopKilobyteAndWritesPreserveFixedRegisterBits()
    {
        var (cpu, _) = Start();
        Assert.Equal(0xfffb, cpu.ReadIoWord(0xffa0));
        Assert.Equal(Cpu80186ChipSelect.Upper, cpu.SelectBusTargets(0xffc00, true));
        Assert.Equal(Cpu80186ChipSelect.None, cpu.SelectBusTargets(0xffbff, true));
        Assert.Equal(Cpu80186ChipSelect.None, cpu.SelectBusTargets(0, true));
        cpu.WriteIoWord(0xffa0, 0);
        cpu.WriteIoWord(0xffa2, 0xffff);
        cpu.WriteIoWord(0xffa4, 0);
        cpu.WriteIoWord(0xffa6, 0);
        cpu.WriteIoWord(0xffa8, 0);
        Assert.Equal(0xc038, cpu.ReadIoWord(0xffa0));
        Assert.Equal(0x3fff, cpu.ReadIoWord(0xffa2));
        Assert.Equal(0x0038, cpu.ReadIoWord(0xffa4));
        Assert.Equal(0x01f8, cpu.ReadIoWord(0xffa6));
        Assert.Equal(0x8038, cpu.ReadIoWord(0xffa8));
        cpu.Reset();
        Assert.Equal(Cpu80186ChipSelect.None, cpu.SelectBusTargets(0, true));
        Assert.Equal(Cpu80186ChipSelect.None, cpu.SelectBusTargets(0x80, false));
    }

    [Fact]
    public void LowerAndMiddleSelectionsUseTheirProgrammedBoundariesAndRequireBothMiddleRegisters()
    {
        var (cpu, _) = Start();
        cpu.WriteIoWord(0xffa2, 0x0fc4);
        Assert.Equal(Cpu80186ChipSelect.Lower, cpu.SelectBusTargets(0xffff, true));
        Assert.Equal(Cpu80186ChipSelect.None, cpu.SelectBusTargets(0x10000, true));
        cpu.WriteIoWord(0xffa6, 0x4004);
        Assert.Equal(Cpu80186ChipSelect.None, cpu.SelectBusTargets(0x40000, true));
        cpu.WriteIoWord(0xffa8, 0x0104); // 8 Kio : quatre sélections de 2 Kio.
        Assert.Equal(Cpu80186ChipSelect.Middle0, cpu.SelectBusTargets(0x407ff, true));
        Assert.Equal(Cpu80186ChipSelect.Middle1, cpu.SelectBusTargets(0x40800, true));
        Assert.Equal(Cpu80186ChipSelect.Middle3, cpu.SelectBusTargets(0x41fff, true));
        Assert.Equal(Cpu80186ChipSelect.None, cpu.SelectBusTargets(0x42000, true));
    }

    [Fact]
    public void PeripheralSelectionsNeedBothRegistersAndCanMoveToMemoryOrYieldPinsFiveAndSixToAddressBits()
    {
        var (cpu, _) = Start();
        cpu.WriteIoWord(0xffa4, 4);
        Assert.Equal(Cpu80186ChipSelect.None, cpu.SelectBusTargets(0x80, false));
        cpu.WriteIoWord(0xffa8, 0x84);
        Assert.Equal(Cpu80186ChipSelect.Peripheral1, cpu.SelectBusTargets(0xff, false));
        Assert.Equal(Cpu80186ChipSelect.Peripheral6, cpu.SelectBusTargets(0x37f, false));
        Assert.Equal(Cpu80186ChipSelect.None, cpu.SelectBusTargets(0x380, false));
        cpu.WriteIoWord(0xffa8, 4);
        Assert.Equal(Cpu80186ChipSelect.None, cpu.SelectBusTargets(0x280, false));
        cpu.ReadIoByte(6);
        Assert.True(cpu.LatchedAddress1);
        Assert.True(cpu.LatchedAddress2);
        cpu.WriteIoWord(0xffa4, 0x1204);
        cpu.WriteIoWord(0xffa8, 0xc4);
        Assert.Equal(Cpu80186ChipSelect.Peripheral1, cpu.SelectBusTargets(0x12080, true));
        Assert.Equal(Cpu80186ChipSelect.None, cpu.SelectBusTargets(0x80, false));
    }

    [Theory]
    [InlineData(0, 11)]
    [InlineData(1, 18)]
    public void MemoryWaitStatesAreChargedOncePerBusCycleIncludingBothCyclesOfOddWords(int offset, int expected)
    {
        var (cpu, _) = Start(0xb8, 0, 0xc0, 0x8e, 0xd8, 0xa1, (byte)offset, 0x30);
        cpu.Step(); cpu.Step();
        cpu.WriteIoWord(0xffa0, 0xc003); // Trois attentes UCS, code de test hors UCS.
        var before = cpu.Cycles;
        cpu.Step();
        Assert.Equal(expected, cpu.Cycles - before);
        Assert.Equal(Cpu80186ChipSelect.None, cpu.ActiveChipSelect);
    }

    [Theory]
    [InlineData(0x80, 2, 0x80, 10)]
    [InlineData(0x81, 2, 0x80, 16)]
    [InlineData(0x200, 1, 0x83, 11)]
    public void DmaIoWordsUseTheReadyBitsOfTheirPeripheralGroup(int port, int lowReady, int highReady, int expected)
    {
        var (cpu, _) = Start();
        cpu.WriteIoWord(0xffa4, (ushort)lowReady);
        cpu.WriteIoWord(0xffa8, (ushort)highReady);
        ProgramDma(cpu, 0, port, 0x4000, 1, 0x8247);
        cpu.SetDmaRequest(0, true);
        var before = cpu.Cycles;
        cpu.Step();
        Assert.Equal(expected, cpu.Cycles - before);
    }

    [Fact]
    public void InternalRegistersDriveChipSelectPinsButIgnoreTheirExternalWaitStates()
    {
        var (cpu, bus) = Start(0xb8, 0xf0, 0xff, 0x8e, 0xd8, 0xa1, 0xa0, 0);
        cpu.Step(); cpu.Step();
        cpu.WriteIoWord(0xfffe, 0x1fff); // PCB en mémoire FFF00, dans UCS.
        var selections = new List<Cpu80186ChipSelect>();
        cpu.ChipSelectChanged = selections.Add;
        var before = cpu.Cycles;
        cpu.Step();
        Assert.Equal(8, cpu.Cycles - before);
        Assert.Equal(0xfffb, cpu.Register(0));
        Assert.Contains(Cpu80186ChipSelect.Upper, selections);
        Assert.DoesNotContain(0xfffa0, bus.Reads);
    }

    [Fact]
    public void FailedExternalBusCallbackReleasesChipSelectPins()
    {
        var (cpu, _) = Start();
        cpu.WriteIoWord(0xffa4, 4);
        cpu.WriteIoWord(0xffa8, 0x84);
        cpu.ChipSelectChanged = selected => { if (selected != Cpu80186ChipSelect.None) throw new InvalidOperationException(); };
        Assert.Throws<InvalidOperationException>(() => cpu.ReadIoByte(0x80));
        Assert.Equal(Cpu80186ChipSelect.None, cpu.ActiveChipSelect);
    }

    [Fact]
    public void PrefetchedCodeKeepsOldBytesUntilATakenJumpFlushesTheQueue()
    {
        var (cpu, bus) = Start(0xb0, 0x11, 0xeb, 0, 0xb0, 0x22, 0xf4);
        bus.Memory[ProgramAddress + 1] = 0x33;
        cpu.Step();
        Assert.Equal(0x11, cpu.Register(0));
        bus.Memory[ProgramAddress + 5] = 0x44;
        cpu.Step(); // Le saut pris, même de déplacement zéro, vide la file.
        cpu.Step();
        Assert.Equal(0x44, cpu.Register(0));
    }

    [Fact]
    public void DmaWritesDoNotInvalidateAlreadyPrefetchedCode()
    {
        var (cpu, bus) = Start(0xb0, 0x11, 0xf4);
        bus.Memory[0x3000] = 0x55;
        ProgramDma(cpu, 0, 0x3000, ProgramAddress + 1, 1, 0x9206);
        cpu.Step();
        Assert.Equal(0x55, bus.Memory[ProgramAddress + 1]);
        cpu.Step();
        Assert.Equal(0x11, cpu.Register(0));
    }

    [Fact]
    public void InterruptAndIretReloadCodeRatherThanKeepingTheOldQueue()
    {
        var (cpu, bus) = Start(0xb0, 0x11, 0xf4);
        bus.Vector(2, 0x2000);
        bus.Memory[0x2000] = 0xcf;
        bus.Memory[ProgramAddress + 1] = 0x55;
        cpu.Nmi = true;
        cpu.Step();
        cpu.Step();
        cpu.Step();
        Assert.Equal(0x55, cpu.Register(0));
    }

    [Fact]
    public void RepeatUsesDecodedInstructionEvenIfItsPrefixIsOverwritten()
    {
        var (cpu, bus) = Start(0xb9, 2, 0, 0xf3, 0xaa, 0xf4);
        cpu.Step();
        cpu.Step();
        bus.Memory[ProgramAddress + 3] = 0xf4;
        cpu.Step();
        Assert.Equal(0, cpu.Register(1));
        Assert.Equal(2, cpu.Register(7));
        Assert.Equal(ProgramAddress + 5, cpu.IP);
    }

    [Fact]
    public void WaitDoesNotRefetchItsOpcodeWhileTestIsBusy()
    {
        var (cpu, bus) = Start(0x9b, 0xb0, 0x55, 0xf4);
        cpu.CoprocessorBusy = true;
        cpu.Step();
        bus.Memory[ProgramAddress] = 0xf4;
        cpu.Step();
        Assert.Equal(ProgramAddress, cpu.IP);
        cpu.CoprocessorBusy = false;
        cpu.Step();
        cpu.Step();
        Assert.Equal(0x55, cpu.Register(0));
    }

    [Fact]
    public void PrefetchUsesTheSixByteQueueWithoutFetchingWhenOnlyOneByteIsFree()
    {
        var (cpu, bus) = Start(0x90, 0x90, 0x90, 0x90, 0x90, 0x90);
        Assert.DoesNotContain(ProgramAddress, bus.Reads);
        cpu.Step();
        cpu.Step();
        Assert.Equal(new[] { ProgramAddress + 5, ProgramAddress + 6 }, bus.Reads);
        bus.Reads.Clear();
        cpu.Step();
        Assert.Empty(bus.Reads);
    }

    [Fact]
    public void OddCodeTargetAndSegmentOffsetWrapFetchTheCorrectByteLanes()
    {
        var (cpu, bus) = Start(0xea, 0xff, 0xff, 0, 0x20);
        bus.Memory[0x2ffff] = 0xb0;
        bus.Memory[0x20000] = 0x55;
        bus.Memory[0x30000] = 0xaa;
        cpu.Step();
        Assert.Contains(0x2ffff, bus.Reads);
        Assert.Contains(0x20000, bus.Reads);
        Assert.DoesNotContain(0x30000, bus.Reads);
        cpu.Step();
        Assert.Equal(0x55, cpu.Register(0));
        Assert.Equal(1, cpu.IP);
    }

    [Fact]
    public void HoldWaitsForTheStartedPrefetchBeforeAssertingHlda()
    {
        var (cpu, bus) = Start(0x90, 0x90, 0x90, 0x90);
        cpu.Step();
        cpu.Step(); // Une nouvelle prélecture est entamée sans être encore terminée.
        var readsAtAcknowledge = -1;
        cpu.HoldAcknowledgeChanged = held => { if (held) readsAtAcknowledge = bus.Reads.Count; };
        cpu.HoldInput = true;
        cpu.Step();
        Assert.True(cpu.HoldAcknowledged);
        Assert.Equal(bus.Reads.Count, readsAtAcknowledge);
        bus.Reads.Clear();
        cpu.Step();
        Assert.Empty(bus.Reads);
    }

    [Theory]
    [InlineData(0x37, 0x12fa, 0, 0x1300, true)]
    [InlineData(0x37, 0xfffa, 0, 0x0000, true)]
    [InlineData(0x37, 0x12f9, 0, 0x1209, false)]
    [InlineData(0x37, 0x12f9, 0x10, 0x130f, true)]
    [InlineData(0x3f, 0x1200, 0x10, 0x110a, true)]
    [InlineData(0x3f, 0x0000, 0x10, 0xff0a, true)]
    [InlineData(0x3f, 0x1209, 0, 0x1209, false)]
    [InlineData(0x3f, 0x120a, 0, 0x1104, true)]
    public void AsciiAdjustKeepsAccumulatorBytesSeparateOnThe80186(int opcode, int accumulator, int auxiliary, int expected, bool adjusted)
    {
        const ushort initialFlags = 0x0cc4;
        const ushort directionFlag = 0x0400;
        var flags = (ushort)(initialFlags | auxiliary | 2);
        var (cpu, _) = Start(0x68, (byte)flags, (byte)(flags >> 8), 0x9d,
            0xb8, (byte)accumulator, (byte)(accumulator >> 8), (byte)opcode);
        cpu.Step(); cpu.Step(); cpu.Step(); cpu.Step();
        Assert.Equal(expected, cpu.Register(0));
        Assert.Equal(adjusted ? 0x11 : 0, cpu.Flags & 0x11);
        Assert.Equal(directionFlag, cpu.Flags & directionFlag); // Les autres indicateurs arithmétiques sont indéfinis.
    }

    private static (Cpu80186 Cpu, Bus Bus) StartHalted()
    {
        var pair = Start(0xf4);
        pair.Cpu.Step();
        while ((pair.Cpu.Cycles & 3) != 0) pair.Cpu.Step();
        return pair;
    }

    private static void IdleClocks(Cpu80186 cpu, int clocks)
    {
        for (var clock = 0; clock < clocks; clock++) cpu.Step();
    }

    private static void RunUntilTimer2Stops(Cpu80186 cpu)
    {
        for (var clock = 0; clock < 64 && (cpu.ReadIoWord(0xff66) & 0x8000) != 0; clock++) cpu.Step();
        Assert.Equal(0, cpu.ReadIoWord(0xff66) & 0x8000);
    }

    private static void ProgramDma(Cpu80186 cpu, int channel, int source, int destination, ushort count, ushort control)
    {
        var port = (ushort)(0xffc0 + channel * 0x10);
        cpu.WriteIoWord(port, (ushort)source);
        cpu.WriteIoWord((ushort)(port + 2), (ushort)(source >> 16));
        cpu.WriteIoWord((ushort)(port + 4), (ushort)destination);
        cpu.WriteIoWord((ushort)(port + 6), (ushort)(destination >> 16));
        cpu.WriteIoWord((ushort)(port + 8), count);
        cpu.WriteIoWord((ushort)(port + 10), control);
    }

    private static (Cpu80186 Cpu, Bus Bus) Start(params byte[] program)
    {
        var bus = new Bus();
        byte[] jump = [0xea, 0, 0x10, 0, 0];
        jump.CopyTo(bus.Memory, 0xffff0);
        byte[] stack = [0xbc, 0, 0x80];
        stack.CopyTo(bus.Memory, 0x1000);
        program.CopyTo(bus.Memory, ProgramAddress);
        var cpu = new Cpu80186(bus.Read, bus.Write, bus.Input, bus.Output);
        bus.Cpu = cpu;
        cpu.Reset(); cpu.Step(); cpu.Step();
        bus.Reads.Clear(); bus.DataReads.Clear(); bus.Writes.Clear();
        return (cpu, bus);
    }

    private sealed class Bus
    {
        internal byte[] Memory { get; } = new byte[0x100000];
        internal Dictionary<ushort, byte> Inputs { get; } = [];
        internal List<ushort> PortReads { get; } = [];
        internal List<(ushort Port, byte Value)> PortWrites { get; } = [];
        internal byte Input(ushort port) { PortReads.Add(port); return Inputs.GetValueOrDefault(port, byte.MaxValue); }
        internal void Output(ushort port, byte value) => PortWrites.Add((port, value));
        internal Cpu80186? Cpu { get; set; }
        internal List<int> DataReads { get; } = [];
        internal List<int> Reads { get; } = [];
        internal List<int> Writes { get; } = [];
        internal Action<int>? Written { get; set; }
        internal byte Read(int address) { Reads.Add(address); if (Cpu?.IsInstructionFetch != true) DataReads.Add(address); return Memory[address]; }
        internal void Write(int address, byte value) { Writes.Add(address); Memory[address] = value; Written?.Invoke(address); }
        internal ushort Word(int address) => (ushort)(Memory[address] | Memory[address + 1] << 8);
        internal void Vector(int vector, ushort target)
        {
            Memory[vector * 4] = (byte)target;
            Memory[vector * 4 + 1] = (byte)(target >> 8);
        }
    }
}
