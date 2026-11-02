using RetroMediaLab.Chips.CPU.Common.Interfaces;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;

namespace RetroMediaLab.Chips.Tests.CPU.MOS.MCS6500.Chip6502;

/// <summary>Traces NMOS autonomes : cycles visibles, pile et effets des instructions combinées.</summary>
public sealed class Chip6502BusTests
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

    [Theory]
    [InlineData(0x02)]
    [InlineData(0x12)]
    [InlineData(0x22)]
    [InlineData(0x32)]
    [InlineData(0x42)]
    [InlineData(0x52)]
    [InlineData(0x62)]
    [InlineData(0x72)]
    [InlineData(0x92)]
    [InlineData(0xb2)]
    [InlineData(0xd2)]
    [InlineData(0xf2)]
    public void JamReadsItsBusSequenceThenHoldsTheLastAddressUntilReset(int opcode)
    {
        var (cpu, bus) = Start((byte)opcode, 0xa5);
        bus.Bytes[0xffff] = 0x6c;
        bus.Bytes[0xfffe] = 0x37;
        cpu.Step();
        Assert.Equal(new[] { 0x200, 0x201, 0xffff, 0xfffe, 0xfffe },
            bus.Accesses.Select(access => (int)access.Address));
        Assert.Equal(new byte[] { (byte)opcode, 0xa5, 0x6c, 0x37, 0x37 },
            bus.Accesses.Select(access => access.Value));
        Assert.All(bus.Accesses, access => Assert.False(access.Write));
        bus.Accesses.Clear();
        bus.Irq = bus.Nmi = true;
        for (var cycle = 0; cycle < 6; cycle++) cpu.Step();
        Assert.Equal(6, bus.Accesses.Count);
        Assert.All(bus.Accesses, access => Assert.Equal((0xffff, (byte)0x6c, false),
            ((int)access.Address, access.Value, access.Write)));
        Assert.Equal(0x201, cpu.PC);
        bus.Irq = bus.Nmi = false;
        cpu.Reset();
        bus.Bytes[0x200] = 0xa9;
        cpu.Step();
        Assert.Equal(0xa5, cpu.A);
        Assert.Equal(0x202, cpu.PC);
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

    [Fact]
    public void OverflowInputIsEdgeSensitiveAndDoesNotUndoClvWhileHeld()
    {
        var (cpu, bus) = Start(0xea, 0xb8, 0xea, 0xea);
        bus.OverflowInput = true;
        cpu.Step();
        Assert.Equal(0x40, cpu.P & 0x40);
        cpu.Step();
        Assert.Equal(0, cpu.P & 0x40);
        bus.OverflowInput = false;
        cpu.Step();
        Assert.Equal(0, cpu.P & 0x40);
        bus.OverflowInput = true;
        cpu.Step();
        Assert.Equal(0x40, cpu.P & 0x40);
    }

    [Fact]
    public void OverflowInputStillChangesVOnAJammedProcessor()
    {
        var (cpu, bus) = Start(0x02);
        cpu.Step();
        bus.OverflowInput = true;
        cpu.Step();
        Assert.Equal(0x40, cpu.P & 0x40);
        Assert.Equal(0x201, cpu.PC);
    }

    [Fact]
    public void SyncMarksOpcodeButNotItsOperandsOrResetReads()
    {
        var (cpu, bus) = Start(0xad, 0, 3);
        cpu.Step();
        Assert.Equal(new ushort[] { 0x200 }, bus.SyncReads);
        Assert.False(bus.Sync);
        bus.SyncReads.Clear();
        cpu.Reset();
        Assert.Empty(bus.SyncReads);
        Assert.False(bus.Sync);
    }

    [Fact]
    public void InterruptEntryMarksOnlyItsAbandonedOpcodeReadAsSync()
    {
        var (cpu, bus) = Start(0x58, 0xea, 0xea);
        bus.Irq = true;
        cpu.Step();
        cpu.Step();
        bus.SyncReads.Clear();
        cpu.Step();
        Assert.Equal(new ushort[] { 0x202 }, bus.SyncReads);
        Assert.False(bus.Sync);
    }

    [Fact]
    public void ReadyHoldsOpcodeAndSyncWithoutBlockingTheHostOrChangingRegisters()
    {
        var (cpu, bus) = Start(0xa9, 0x42);
        bus.ReadyInput = false;
        for (var cycle = 0; cycle < 3; cycle++)
        {
            cpu.Step();
            Assert.Equal(cycle + 1, bus.Ticks);
            Assert.Equal(0x200, cpu.PC);
            Assert.Equal(0, cpu.A);
            Assert.True(bus.Sync);
            Assert.False(cpu.AtInstructionBoundary);
        }
        bus.ReadyInput = true;
        cpu.Step();
        Assert.Equal(0x42, cpu.A);
        Assert.Equal(0x202, cpu.PC);
        Assert.Equal(new ushort[] { 0x200, 0x200, 0x200, 0x200 }, bus.SyncReads);
        Assert.False(bus.Sync);
        Assert.True(cpu.AtInstructionBoundary);
    }

    [Fact]
    public void ReadyResumesAnImmediateOperandWithoutFetchingTheOpcodeAgain()
    {
        var (cpu, bus) = Start(0xa9, 0x42);
        bus.ReadySignal = tick => tick != 2;
        cpu.Step();
        Assert.Equal(0x201, cpu.PC);
        Assert.Equal(0, cpu.A);
        Assert.False(bus.Sync);
        cpu.Step();
        Assert.Equal(0x42, cpu.A);
        Assert.Equal(new[] { 0x200, 0x201, 0x201 }, bus.Accesses.Select(access => (int)access.Address));
    }

    [Fact]
    public void ReadyDoesNotBlockEitherWriteOfANmosReadModifyWrite()
    {
        var (cpu, bus) = Start(0x06, 0x10);
        bus.Bytes[0x10] = 0x81;
        bus.ReadySignal = tick => tick < 4;
        cpu.Step();
        Assert.True(cpu.AtInstructionBoundary);
        Assert.Equal(2, bus.Bytes[0x10]);
        Assert.Equal(new byte[] { 0x81, 2 }, bus.Accesses.Where(access => access.Write).Select(access => access.Value));
        Assert.Equal(5, bus.Ticks);
    }

    [Fact]
    public void ReadyCanHoldTheReadBeforeRmwWithoutDuplicatingItsWrites()
    {
        var (cpu, bus) = Start(0x06, 0x10);
        bus.Bytes[0x10] = 0x81;
        bus.ReadySignal = tick => tick != 3;
        cpu.Step();
        Assert.False(cpu.AtInstructionBoundary);
        Assert.Equal(0x81, bus.Bytes[0x10]);
        Assert.DoesNotContain(bus.Accesses, access => access.Write);
        cpu.Step();
        Assert.True(cpu.AtInstructionBoundary);
        Assert.Equal(2, bus.Bytes[0x10]);
        Assert.Equal(new byte[] { 0x81, 2 }, bus.Accesses.Where(access => access.Write).Select(access => access.Value));
        Assert.Equal(6, bus.Ticks);
    }

    [Fact]
    public void ReadyHoldsStackReadWithoutIncrementingTheStackPointerTwice()
    {
        var (cpu, bus) = Start(0x68);
        bus.Bytes[0x1fe] = 0x5a;
        bus.ReadySignal = tick => tick != 4;
        cpu.Step();
        Assert.Equal(0xfe, cpu.S);
        Assert.Equal(0, cpu.A);
        cpu.Step();
        Assert.Equal(0xfe, cpu.S);
        Assert.Equal(0x5a, cpu.A);
        Assert.Equal(new[] { 0x200, 0x201, 0x1fd, 0x1fe, 0x1fe }, bus.Accesses.Select(access => (int)access.Address));
    }

    [Fact]
    public void ReadyHoldsJsrBeforeItsPushesAndDoesNotRepeatThem()
    {
        var (cpu, bus) = Start(0x20, 0, 3);
        bus.ReadySignal = tick => tick != 3;
        cpu.Step();
        Assert.Equal(0xfd, cpu.S);
        Assert.DoesNotContain(bus.Accesses, access => access.Write);
        cpu.Step();
        Assert.Equal(0x300, cpu.PC);
        Assert.Equal(0xfb, cpu.S);
        Assert.Equal(2, bus.Accesses.Count(access => access.Write));
        Assert.Equal(0x02, bus.Bytes[0x1fd]);
        Assert.Equal(0x02, bus.Bytes[0x1fc]);
    }

    [Fact]
    public void NmiDuringReadyWaitOnSyncIsAcceptedBeforeTheHeldOpcode()
    {
        var (cpu, bus) = Start(0x58, 0xea, 0xa9, 0x42);
        bus.Bytes[0xfffb] = 3;
        cpu.Step();
        cpu.Step();
        bus.ReadyInput = false;
        bus.Nmi = true;
        cpu.Step();
        cpu.Step();
        Assert.Equal(0x202, cpu.PC);
        bus.ReadyInput = true;
        cpu.Step();
        Assert.Equal(0x300, cpu.PC);
        Assert.Equal(0, cpu.A);
        Assert.Equal(2, bus.Bytes[0x1fd]);
        Assert.Equal(2, bus.Bytes[0x1fc]);
    }

    [Fact]
    public void ResetCancelsASuspendedOperandAndReleasesSync()
    {
        var (cpu, bus) = Start(0xa9, 0x42);
        bus.ReadySignal = tick => tick != 2;
        cpu.Step();
        Assert.False(cpu.AtInstructionBoundary);
        cpu.Reset();
        Assert.True(cpu.AtInstructionBoundary);
        Assert.False(bus.Sync);
        Assert.Equal(0x200, cpu.PC);
        bus.ReadySignal = null;
        cpu.Step();
        Assert.Equal(0x42, cpu.A);
        Assert.Equal(0x202, cpu.PC);
    }

    [Fact]
    public void EveryOpcodeResumesAfterTwoReadyWaitCyclesAtEachOfItsReads()
    {
        for (var opcode = 0; opcode <= byte.MaxValue; opcode++)
        {
            var (reference, referenceBus) = IndexedReadyCase((byte)opcode);
            reference.Step();
            for (var held = 0; held < referenceBus.Accesses.Count; held++)
            {
                if (referenceBus.Accesses[held].Write) continue;
                var (cpu, bus) = IndexedReadyCase((byte)opcode);
                var firstWait = held + 1;
                bus.ReadySignal = tick => tick < firstWait || tick >= firstWait + 2;
                var expectedClocks = referenceBus.Ticks + 2;
                for (var call = 0; bus.Ticks < expectedClocks && call < expectedClocks; call++) cpu.Step();
                Assert.True(bus.Ticks == expectedClocks, $"Opcode {opcode:X2}, lecture {held} : durée {bus.Ticks}/{expectedClocks}");
                Assert.Equal((reference.A, reference.X, reference.Y, reference.S, reference.P, reference.PC),
                    (cpu.A, cpu.X, cpu.Y, cpu.S, cpu.P, cpu.PC));
                var expected = referenceBus.Accesses.Take(held)
                    .Concat(Enumerable.Repeat(referenceBus.Accesses[held], 2))
                    .Concat(referenceBus.Accesses.Skip(held));
                Assert.Equal(expected, bus.Accesses);
                Assert.Equal(referenceBus.Bytes, bus.Bytes);
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OpcodeReadPublishesInstructionAddressBeforeMemoryMapping(bool cmos)
    {
        var (cpu, bus) = Start(cmos, 0xa9, 1, 0x4c, 0, 3);
        bus.Bytes[0x300] = 0xa9;
        bus.Bytes[0x301] = 0x42;
        cpu.Step();
        ushort expected = 0x202;
        bus.ReadObserver = _ => { if (bus.Sync) Assert.Equal(expected, cpu.InstructionAddress); };
        cpu.Step();
        expected = 0x300;
        cpu.Step();
        Assert.Equal(0x42, cpu.A);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void NmiDuringResetUsesTheFinalInterruptSamplingWindow(int pulseCycle)
    {
        var (cpu, bus) = Start(0xa9, 0x42);
        bus.Bytes[0xfffb] = 3;
        bus.Bytes[0x300] = 0x40;
        bus.NmiSignal = tick => tick == pulseCycle;
        cpu.Reset();
        cpu.Step();
        if (pulseCycle == 7)
        {
            Assert.Equal(0x42, cpu.A);
            Assert.Equal(0x202, cpu.PC);
            cpu.Step();
        }
        else Assert.Equal(0, cpu.A);
        Assert.Equal(0x300, cpu.PC);
        cpu.Step();
        Assert.Equal(pulseCycle == 7 ? 0x202 : 0x200, cpu.PC);
        if (pulseCycle != 7) { cpu.Step(); Assert.Equal(0x42, cpu.A); }
    }

    [Fact]
    public void NmosResetMasksIrqAndPreservesDecimalAndOverflowFlags()
    {
        var (cpu, bus) = Start(0xf8, 0xea);
        cpu.Step();
        bus.OverflowInput = true;
        cpu.Step();
        Assert.Equal(0x48, cpu.P & 0x48);
        bus.Irq = true;
        cpu.Reset();
        Assert.Equal(0x4c, cpu.P & 0x4c);
        cpu.Step();
        Assert.Equal(0x201, cpu.PC);
    }

    [Fact]
    public void ResetHeldDuringAStoreInhibitsTransfersUntilReleased()
    {
        var (cpu, bus) = Start(0xa9, 0x42, 0x8d, 0, 0x10);
        cpu.Step();
        bus.ResetSignal = tick => tick >= 4 && tick < 8;
        cpu.Step();
        var heldAccesses = bus.Accesses.Count;
        Assert.False(cpu.AtInstructionBoundary);
        Assert.False(bus.Sync);
        for (var cycle = 0; cycle < 4; cycle++) cpu.Step();
        Assert.Equal(heldAccesses, bus.Accesses.Count);
        Assert.Equal(0, bus.Bytes[0x1000]);
        cpu.Step();
        Assert.Equal(0x200, cpu.PC);
        Assert.Equal(0x42, cpu.A);
        Assert.Equal(heldAccesses + 7, bus.Accesses.Count);
        Assert.DoesNotContain(bus.Accesses, access => access.Write);
        cpu.Step();
        cpu.Step();
        Assert.Equal(0x42, bus.Bytes[0x1000]);
    }

    [Fact]
    public void ReadyCanSuspendTheResetStackReadWithoutASecondStackDecrement()
    {
        var (cpu, bus) = Start(0xea);
        bus.ReadyInput = false;
        cpu.Reset();
        Assert.Equal(0xfd, cpu.S);
        Assert.False(cpu.AtInstructionBoundary);
        bus.ReadyInput = true;
        bus.ReadySignal = tick => tick != 5;
        cpu.Step();
        Assert.Equal(0xfc, cpu.S);
        cpu.Step();
        Assert.Equal(0xfa, cpu.S);
        Assert.Equal(0x200, cpu.PC);
        Assert.Equal(new[] { 0x200, 0x200, 0x200, 0x1fd, 0x1fc, 0x1fc, 0x1fb, 0xfffc, 0xfffd },
            bus.Accesses.Select(access => (int)access.Address));
    }

    [Fact]
    public void DisabledDataBusSignalsUndrivenWriteButDoesNotChangeMemoryOrHaltExecution()
    {
        var (cpu, bus) = Start(0xa9, 0x66, 0x8d, 0, 0x10, 0xad, 0, 0x10, 0x8d, 1, 0x10);
        bus.Bytes[0x1000] = 0x11;
        cpu.Step();
        bus.DrivenData = false;
        cpu.Step();
        Assert.Equal(new ushort[] { 0x1000 }, bus.UndrivenWrites);
        Assert.Equal(0x11, bus.Bytes[0x1000]);
        Assert.Equal(0x66, cpu.A);
        cpu.Step();
        Assert.Equal(0x11, cpu.A);
        bus.DrivenData = true;
        cpu.Step();
        Assert.Equal(0x11, bus.Bytes[0x1001]);
    }

    [Fact]
    public void UndrivenStackWriteStillDecrementsTheCpuStackPointer()
    {
        var (cpu, bus) = Start(0x48);
        bus.Bytes[0x1fd] = 0x37;
        bus.DrivenData = false;
        cpu.Step();
        Assert.Equal(new ushort[] { 0x1fd }, bus.UndrivenWrites);
        Assert.Equal(0xfc, cpu.S);
        Assert.Equal(0x37, bus.Bytes[0x1fd]);
        Assert.Equal(3, bus.Ticks);
    }

    private static (Cpu6502 Cpu, TraceBus Bus) IndexedReadyCase(byte opcode)
    {
        var (cpu, bus) = Start(0xa2, 1, 0xa0, 1, opcode, 0xff, 2);
        cpu.Step();
        cpu.Step();
        bus.Accesses.Clear();
        bus.SyncReads.Clear();
        bus.Ticks = 0;
        return (cpu, bus);
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
        internal List<ushort> SyncReads { get; } = [];
        internal bool Sync { get; private set; }
        internal bool OverflowInput { get; set; }
        internal bool ReadyInput { get; set; } = true;
        internal Func<int, bool>? ReadySignal { get; set; }
        internal Action<ushort>? ReadObserver { get; set; }
        internal Func<int, bool>? ResetSignal { get; set; }
        internal bool DrivenData { get; set; } = true;
        internal List<ushort> UndrivenWrites { get; } = [];
        private bool _irq;
        internal int Ticks { get; set; }
        internal Func<int, bool>? IrqSignal { get; set; }
        internal Func<int, bool>? NmiSignal { get; set; }
        public bool Irq { get => IrqSignal?.Invoke(Ticks) ?? _irq; set => _irq = value; }
        private bool _nmi;
        public bool Nmi { get => NmiSignal?.Invoke(Ticks) ?? _nmi; set => _nmi = value; }
        public bool SetOverflow => OverflowInput;
        public bool Ready => ReadySignal?.Invoke(Ticks) ?? ReadyInput;
        public bool ResetRequested => ResetSignal?.Invoke(Ticks) ?? false;
        public bool DataBusEnabled => DrivenData;
        public void WriteUndriven(ushort address) => UndrivenWrites.Add(address);
        public void SetSync(bool asserted) => Sync = asserted;
        public byte Read(ushort address)
        {
            ReadObserver?.Invoke(address);
            if (Sync) SyncReads.Add(address);
            Accesses.Add((address, Bytes[address], false));
            return Bytes[address];
        }
        public void Write(ushort address, byte value) { Accesses.Add((address, value, true)); Bytes[address] = value; }
        public void Tick() { Ticks++; }
    }
}
