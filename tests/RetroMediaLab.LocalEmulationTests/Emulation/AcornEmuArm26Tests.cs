using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Arm26.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Arm26.Interfaces;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Arm26.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Interfaces;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;

namespace RetroMediaLab.Tests.Emulation;

public sealed partial class AcornEmuArm26Tests
{
    [Fact]
    public void PipelineOperandsAndBranchLinkUseTheirArchitecturalAddresses()
    {
        var (cpu, _) = Machine(0xe1a0000f, 0xe28f1000, 0xeb000000, 0xe3a020ff, 0xe1a0300e);
        Run(cpu, 4);
        Assert.Equal(0x0c00000bu, cpu.GetRegister(0));
        Assert.Equal(12u, cpu.GetRegister(1));
        Assert.Equal(0u, cpu.GetRegister(2));
        Assert.Equal(0x0c00000fu, cpu.GetRegister(3));
        Assert.Equal(20u, cpu.PC);
    }

    [Fact]
    public void SignedOverflowAndUnsignedCarryAreIndependent()
    {
        var (cpu, bus) = Machine(0xe59f0010, 0xe2901001, 0x63a02001, 0x23a03001, 0xe2514001);
        bus.Words[24] = 0x7fffffff;
        Run(cpu, 5);
        Assert.Equal(0x80000000u, cpu.GetRegister(1));
        Assert.Equal(1u, cpu.GetRegister(2));
        Assert.Equal(0u, cpu.GetRegister(3));
        Assert.Equal(0x7fffffffu, cpu.GetRegister(4));
        Assert.Equal(0x30000000u, cpu.Status & 0xf0000000);
    }

    [Fact]
    public void SubtractCarryConsumesBorrowAndCompareDoesNotWriteDestination()
    {
        var (cpu, _) = Machine(0xe3a00000, 0xe3500001, 0xe2d01000, 0xe3500000, 0xe2d02000);
        Run(cpu, 5);
        Assert.Equal(uint.MaxValue, cpu.GetRegister(1));
        Assert.Equal(0u, cpu.GetRegister(2));
        Assert.Equal(0x60000000u, cpu.Status & 0xf0000000);
    }

    [Theory]
    [InlineData(0, 0xe1b00211u, 0x80000001u, true)]
    [InlineData(32, 0xe1b00211u, 0u, true)]
    [InlineData(33, 0xe1b00211u, 0u, false)]
    [InlineData(32, 0xe1b00231u, 0u, true)]
    [InlineData(255, 0xe1b00251u, 0xffffffffu, true)]
    [InlineData(32, 0xe1b00271u, 0x80000001u, true)]
    [InlineData(33, 0xe1b00271u, 0xc0000000u, true)]
    public void RegisterShiftsDoNotWrapTheirAmountAtThirtyTwo(int amount, uint instruction, uint expected, bool carry)
    {
        var (cpu, bus) = Machine(0xe59f1010, 0xe3a02000u | (uint)amount, 0xe3500000, instruction);
        bus.Words[24] = 0x80000001;
        Run(cpu, 4);
        Assert.Equal(expected, cpu.GetRegister(0));
        Assert.Equal(carry, (cpu.Status & 0x20000000) != 0);
    }

    [Fact]
    public void ImmediateRrxConsumesCarryAndImmediateLsrZeroMeansThirtyTwo()
    {
        var (cpu, _) = Machine(0xe3a01002, 0xe3500000, 0xe1b00061, 0xe1b02021);
        Run(cpu, 4);
        Assert.Equal(0x80000001u, cpu.GetRegister(0));
        Assert.Equal(0u, cpu.GetRegister(2));
        Assert.Equal(0x40000000u, cpu.Status & 0xf0000000);
    }

    [Fact]
    public void MultiplyAccumulateAndConditionalExecutionRunGuestInstructions()
    {
        var (cpu, _) = Machine(0xe3a00007, 0xe3a01009, 0xe3a02003, 0xe0232190, 0xe3530042, 0x03a04001, 0x13a05001);
        Run(cpu, 7);
        Assert.Equal(66u, cpu.GetRegister(3));
        Assert.Equal(1u, cpu.GetRegister(4));
        Assert.Equal(0u, cpu.GetRegister(5));
    }

    [Fact]
    public void LoadsRotateUnalignedWordsAndStoresKeepAlignedWordLanes()
    {
        var (cpu, bus) = Machine(0xe3a00c01, 0xe5901001, 0xe5c01004, 0xe5801005);
        bus.Words[256] = 0x44332211;
        Run(cpu, 4);
        Assert.Equal(0x11443322u, cpu.GetRegister(1));
        Assert.Equal(0x11443322u, bus.Words[260]);
        Assert.Contains(bus.Accesses, access => access.Address == 257 && !access.Instruction);
        Assert.Contains(bus.Writes, access => access.Address == 261);
    }

    [Fact]
    public void PostIndexTranslationUsesUserBusAccessWithoutChangingCpuMode()
    {
        var (cpu, bus) = Machine(0xe3a00c01, 0xe4b01004);
        bus.Words[256] = 123;
        Run(cpu, 2);
        Assert.Equal(Arm26Mode.Supervisor, cpu.Mode);
        Assert.Contains(bus.Accesses, item => item.Address == 256 && item.User && !item.Instruction);
        Assert.Equal(260u, cpu.GetRegister(0));
    }

    [Fact]
    public void SingleAbortLeavesDestinationAndBaseUntouched()
    {
        var (cpu, bus) = Machine(0xe3a00c01, 0xe3a01037, 0xe5b01004);
        bus.AbortRead = 260;
        Run(cpu, 3);
        Assert.Equal(256u, cpu.GetRegister(0));
        Assert.Equal(55u, cpu.GetRegister(1));
        Assert.Equal(16u, cpu.PC);
        Assert.Equal(0x0c000013u, cpu.GetRegister(14));
    }

    [Fact]
    public void BlockAbortCompletesBusReadsButPreservesUnwindBaseAndSuppressesPcLoad()
    {
        var (cpu, bus) = Machine(0xe3a00c01, 0xe8b08007);
        bus.Words[256] = 999;
        bus.Words[260] = 111;
        bus.Words[264] = 222;
        bus.Words[268] = 0x1000;
        bus.AbortRead = 260;
        Run(cpu, 2);
        Assert.Equal(272u, cpu.GetRegister(0));
        Assert.Equal(0u, cpu.GetRegister(1));
        Assert.Equal(0u, cpu.GetRegister(2));
        Assert.Equal(16u, cpu.PC);
        Assert.Contains(bus.Accesses, item => item.Address == 268 && !item.Instruction);
    }

    [Fact]
    public void NormalBlockLoadBaseWinsWriteBackAndStoresUseUpdatedNonFirstBase()
    {
        var (cpu, bus) = Machine(0xe3a00c01, 0xe3a01007, 0xe8a10003, 0xe8b00003);
        bus.Words[256] = 88;
        bus.Words[260] = 99;
        Run(cpu, 3);
        Assert.Equal(15u, bus.Words[8]);
        cpu.Step();
        Assert.Equal(88u, cpu.GetRegister(0));
        Assert.Equal(99u, cpu.GetRegister(1));
    }

    [Fact]
    public void UserTransfersAccessUserBankWhileFiqBankRemainsPrivate()
    {
        var (cpu, bus) = Machine(0xe3a08055, 0xe33ff001, 0xe3a08066, 0xe3a00c01,
            0xe8c00100, 0xe33ff003, 0xe1a02008, 0xe1a01008);
        Run(cpu, 8);
        Assert.Equal(0x55u, bus.Words[256]);
        Assert.Equal(0x66u, cpu.GetRegister(2)); // Une instruction de délai des banques sur ARM2.
        Assert.Equal(0x55u, cpu.GetRegister(1));
    }

    [Fact]
    public void UserStatusWritesCannotAcquireSupervisorPrivilege()
    {
        var (cpu, _) = Machine(0xe33ff000, 0xe33ff303);
        Run(cpu, 2);
        Assert.Equal(Arm26Mode.User, cpu.Mode);
        Assert.Equal(0u, cpu.Status & 0x0c000003);
    }

    [Fact]
    public void SwiAndMovsPcLrRestoreFlagsAndReturnAddress()
    {
        var (cpu, bus) = Machine(0xe3a00001, 0xef000000, 0xe1b0f00e);
        // Vecteur SWI à 8, retour à 8 : le prochain MOVS renvoie au site de reprise.
        Run(cpu, 2);
        Assert.Equal(8u, cpu.PC);
        Assert.Equal(0x0c00000bu, cpu.GetRegister(14));
        bus.Words[8] = 0xe1b0f00e;
        cpu.Step();
        Assert.Equal(8u, cpu.PC);
        Assert.Equal(0x0c000003u, cpu.Status);
    }

    [Fact]
    public void BranchDiscardsSpeculativePrefetchAbort()
    {
        var (cpu, bus) = Machine(0xea000002);
        bus.Words[16] = 0xe3a0002a;
        bus.AbortRead = 8;
        Run(cpu, 2);
        Assert.Equal(42u, cpu.GetRegister(0));
        Assert.Equal(20u, cpu.PC);
    }

    [Fact]
    public void FiqHasPriorityAndUsesItsOwnLinkBank()
    {
        var (cpu, bus) = Machine(0xe33ff003, 0xe1a00000);
        cpu.Step();
        bus.Irq = bus.Fiq = true;
        cpu.Step();
        Assert.Equal(Arm26Mode.Fiq, cpu.Mode);
        Assert.Equal(28u, cpu.PC);
        Assert.Equal(11u, cpu.GetRegister(14));
        Assert.Equal(0x0c000001u, cpu.Status & 0x0c000003);
    }

    [Fact]
    public void SwapIsUndefinedOnArm2AndLockedOnArm250()
    {
        var (arm2, _) = Machine(0xe1002091);
        arm2.Step();
        Assert.Equal(4u, arm2.PC);
        var (cpu, bus) = Machine(Arm26Model.Arm250, 0xe3a00c01, 0xe3a01037, 0xe1002091);
        bus.Words[256] = 77;
        Run(cpu, 3);
        Assert.Equal(77u, cpu.GetRegister(2));
        Assert.Equal(55u, bus.Words[256]);
        Assert.Equal(new[] { true, false }, bus.LockStates);
    }

    [Fact]
    public void FailedSwapReleasesBusBeforeExceptionFetch()
    {
        var (cpu, bus) = Machine(Arm26Model.Arm250, 0xe3a00c01, 0xe1002091);
        bus.AbortRead = 256;
        Run(cpu, 2);
        Assert.False(bus.Locked);
        Assert.Equal(16u, cpu.PC);
        Assert.DoesNotContain(bus.Accesses, item => item.Instruction && item.Locked);
    }

    [Fact]
    public void Arm3CacheKeepsDataUntilExplicitFlushAndMrcIdentifiesTheCpu()
    {
        var (cpu, bus) = Machine(Arm26Model.Arm3, 0xee100f10, 0xe3a01001,
            0xee031f10, 0xe3a01003, 0xee021f10, 0xe3a02c01,
            0xe5923000, 0xe5924000, 0xee011f10, 0xe5925000);
        bus.Words[256] = 123;
        Run(cpu, 7);
        Assert.Equal(0x41560300u, cpu.GetRegister(0));
        bus.Words[256] = 456;
        Run(cpu, 3);
        Assert.Equal(123u, cpu.GetRegister(3));
        Assert.Equal(123u, cpu.GetRegister(4));
        Assert.Equal(456u, cpu.GetRegister(5));
    }

    [Fact]
    public void Arm3UpdateableCacheWritesThroughAndDisruptiveWritesInvalidate()
    {
        var (cpu, bus) = Machine(Arm26Model.Arm3, 0xe3a01001, 0xee031f10, 0xee041f10,
            0xe3a01003, 0xee021f10, 0xe3a02c01, 0xe5923000,
            0xe3a0302a, 0xe5823000, 0xe5924000, 0xe3a01001, 0xee051f10,
            0xe5823004, 0xe5925000);
        bus.Words[256] = 9;
        Run(cpu, 10);
        Assert.Equal(42u, bus.Words[256]);
        Assert.Equal(42u, cpu.GetRegister(4));
        bus.Words[256] = 99;
        Run(cpu, 4);
        Assert.Equal(99u, cpu.GetRegister(5));
    }

    [Theory]
    [InlineData(0xe5901000u, 1, false)]
    [InlineData(0xe8900006u, 1, false)]
    [InlineData(0xe1a01312u, 1, false)]
    [InlineData(0xe5801000u, 0, false)]
    [InlineData(0xe1a00000u, 0, true)]
    public void Arm2MarksTheFinalLoadAndRegisterShiftCycleForMerging(uint instruction, int expected, bool sequential)
    {
        var (cpu, bus) = Machine(instruction, 0xe1a00000, 0xe1a00000, 0xe1a00000);
        cpu.Step();
        Assert.Equal(expected, bus.MergedCycles);
        cpu.Step();
        Assert.Equal(sequential, bus.SequentialAccesses.Last());
    }

    [Theory]
    [InlineData(0xe5901000u)]
    [InlineData(0xe8900006u)]
    [InlineData(0xe1a01312u)]
    public void Arm3KeepsTheFinalLoadAndRegisterShiftCycleIndependent(uint instruction)
    {
        var (cpu, bus) = Machine(Arm26Model.Arm3, instruction, 0xe1a00000, 0xe1a00000);
        cpu.Step();
        Assert.Equal(0, bus.MergedCycles);
    }

    [Theory]
    [InlineData(0u, 1)]
    [InlineData(1u, 1)]
    [InlineData(2u, 2)]
    [InlineData(3u, 2)]
    [InlineData(7u, 2)]
    [InlineData(8u, 3)]
    [InlineData(31u, 3)]
    [InlineData(32u, 4)]
    [InlineData(0x1fffffffu, 15)]
    [InlineData(0x20000000u, 16)]
    [InlineData(0xffffffffu, 16)]
    public void BoothCarryDeterminesMulAndMlaIterations(uint multiplier, int iterations)
    {
        foreach (var model in new[] { Arm26Model.Arm2, Arm26Model.Arm250, Arm26Model.Arm3 })
        foreach (var accumulate in new[] { false, true })
        {
            var (cpu, bus) = Machine(model, 0xe59f0010, 0xe59f1010, 0xe3a02007,
                accumulate ? 0xe0232190u : 0xe0030190u, 0xe1a00000, 0xe1a00000);
            bus.Words[24] = 5;
            bus.Words[28] = multiplier;
            Run(cpu, 3);
            var before = cpu.Cycles;
            var internalBefore = bus.InternalCycles;
            var mergedBefore = bus.MergedCycles;
            cpu.Step();
            Assert.Equal(unchecked(5 * multiplier + (accumulate ? 7u : 0)), cpu.GetRegister(3));
            Assert.Equal(iterations + 1, cpu.Cycles - before);
            Assert.Equal(model == Arm26Model.Arm3 ? iterations : iterations - 1, bus.InternalCycles - internalBefore);
            Assert.Equal(model == Arm26Model.Arm3 ? 0 : 1, bus.MergedCycles - mergedBefore);
        }
    }

    [Theory]
    [InlineData(0xe0000190u, 0u)]
    [InlineData(0xe0202190u, 0xffffffddu)]
    public void DestinationOverlappingRmUsesTheBoothAccumulator(uint instruction, uint expected)
    {
        var (cpu, _) = Machine(0xe3a00005, 0xe3a01002, 0xe3a02007, instruction);
        Run(cpu, 4);
        Assert.Equal(expected, cpu.GetRegister(0));
    }

    [Theory]
    [InlineData(0xe0030f91u, 24u)]
    [InlineData(0xe003019fu, 0x18000026u)]
    [InlineData(0xe023f191u, 0x0c000013u)]
    public void MultiplyPcOperandsUseTheirDifferentDataAndStatusViews(uint instruction, uint expected)
    {
        var (cpu, _) = Machine(0xe3a01002, instruction);
        Run(cpu, 2);
        Assert.Equal(expected, cpu.GetRegister(3));
    }

    [Theory]
    [InlineData(0xe00f0190u)]
    [InlineData(0xe01f0190u)]
    public void MultiplyWithPcDestinationDoesNotChangePcModeOrInterruptMasks(uint instruction)
    {
        var (cpu, _) = Machine(0xe3e00000, 0xe3a01002, instruction);
        Run(cpu, 2);
        var protectedStatus = cpu.Status & 0x3fffffffu;
        cpu.Step();
        Assert.Equal(12u, cpu.PC);
        Assert.Equal(protectedStatus, cpu.Status & 0x3fffffffu);
        Assert.Equal(Arm26Mode.Supervisor, cpu.Mode);
    }

    private static (CpuArm26 Cpu, ArmBus Bus) Machine(params uint[] instructions) => Machine(Arm26Model.Arm2, instructions);

    private static (CpuArm26 Cpu, ArmBus Bus) Machine(Arm26Model model, params uint[] instructions)
    {
        var bus = new ArmBus();
        for (var index = 0; index < instructions.Length; index++) bus.Words[(uint)(index * 4)] = instructions[index];
        var cpu = new CpuArm26(bus, model);
        cpu.Reset();
        return (cpu, bus);
    }

    private static void Run(CpuArm26 cpu, int instructions)
    {
        for (var index = 0; index < instructions; index++) cpu.Step();
    }

    private sealed class ArmBus : IArmBus
    {
        internal Dictionary<uint, uint> Words { get; } = [];
        internal List<bool> SequentialAccesses { get; } = [];
        internal int MergedCycles { get; private set; }
        internal int InternalCycles { get; private set; }
        internal List<(uint Address, bool User, bool Instruction, bool Locked)> Accesses { get; } = [];
        internal List<bool> LockStates { get; } = [];
        internal List<(uint Address, bool User)> Writes { get; } = [];
        internal uint? AbortRead { get; set; }
        internal Action<uint>? BeforeWrite { get; set; }
        internal bool Locked { get; private set; }
        public bool Irq { get; set; }
        public bool Fiq { get; set; }

        public bool TryReadWord(uint address, bool sequential, bool userMode, bool instructionFetch, out uint value)
        {
            Accesses.Add((address, userMode, instructionFetch, Locked));
            SequentialAccesses.Add(sequential);
            Words.TryGetValue(address & ~3u, out value);
            return address != AbortRead;
        }

        public bool TryReadByte(uint address, bool userMode, out byte value)
        {
            var success = TryReadWord(address & ~3u, false, userMode, false, out var word);
            value = (byte)(word >> ((int)(address & 3) * 8));
            return success;
        }

        public bool TryWriteWord(uint address, uint value, bool sequential, bool userMode)
        {
            BeforeWrite?.Invoke(address);
            Writes.Add((address, userMode));
            Words[address & ~3u] = value;
            return true;
        }

        public bool TryWriteByte(uint address, byte value, bool userMode)
        {
            var aligned = address & ~3u;
            Words.TryGetValue(aligned, out var word);
            var shift = (int)(address & 3) * 8;
            Words[aligned] = (word & ~(0xffu << shift)) | ((uint)value << shift);
            return true;
        }

        public void SetBusLock(bool enabled)
        {
            Locked = enabled;
            LockStates.Add(enabled);
        }

        public void InternalCycle() => InternalCycles++;
        public void MergeInternalCycle() => MergedCycles++;
    }
}
