using Circuit = RetroMediaLab.Chips.Tests.CPU.Common.Z80.Z80TestCircuit;
using RetroMediaLab.Chips.CPU.NEC.UPD780C.UPD780C1.Models;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Models;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Services;
using System.Text.Json;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Contracts;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Enums;
using Xunit;

namespace RetroMediaLab.Chips.Tests.CPU.Zilog.Z80.Z80;

public sealed partial class ExecutionTests
{
    [Fact]
    public void EveryOpcodeSpaceExecutesWithoutMissingDecodeIncludingAliases()
    {
        foreach (var silicon in Enum.GetValues<Z80SiliconVariant>())
        foreach (int space in Enumerable.Range(0, 7))
        foreach (int opcode in Enumerable.Range(0, 256))
        {
            byte op = (byte)opcode;
            byte[] program = space switch
            {
                0 => [op, 0, 0, 0, 0], 1 => [0xcb, op], 2 => [0xed, op, 0, 0, 0],
                3 => [0xdd, op, 0, 0, 0], 4 => [0xfd, op, 0, 0, 0],
                5 => [0xdd, 0xcb, 0, op], _ => [0xfd, 0xcb, 0, op]
            };
            var initial = Initial(); initial.BC = 1; initial.HL = 0x4000;
            initial.DE = 0x5000; initial.IX = 0x6000; initial.IY = 0x7000;
            using var circuit = new Circuit(program, initial, silicon: silicon);
            circuit.RunInstructions(1);
            Assert.False(circuit.Cpu.IsFaulted);
            Assert.True(circuit.Cpu.InputClockCount >= 5);
        }
    }

    [Fact]
    public void DecimalAdjustCoversAllInputsAndBothDirections()
    {
        // Les opérations BCD valides sont vérifiées avec l'arithmétique décimale,
        // indépendante du calcul de correction du CPU.
        for (int a = 0; a < 100; a++)
        for (int b = 0; b < 100; b++)
        foreach (bool subtract in new[] { false, true })
        {
            byte packedA = (byte)((a / 10 << 4) | a % 10);
            byte packedB = (byte)((b / 10 << 4) | b % 10);
            var initial = Initial(); initial.A = packedA;
            using var circuit = new Circuit([subtract ? (byte)0xd6 : (byte)0xc6, packedB, 0x27], initial);
            circuit.RunInstructions(2);
            int expected = (subtract ? a - b + 100 : a + b) % 100;
            Assert.Equal((byte)((expected / 10 << 4) | expected % 10), circuit.Cpu.Registers.A);
            Assert.Equal(subtract ? a < b : a + b > 99, (circuit.Cpu.Registers.F & 1) != 0);
            Assert.Equal(12, circuit.Cpu.InputClockCount); // 7 + 4 T, puis T1 suivant.
        }
    }

    [Fact]
    public void BitUsesOperandForRegistersAndMemptrForMemoryIncludingIndexedAliases()
    {
        foreach (int bit in Enumerable.Range(0, 8))
        foreach (byte value in new byte[] { 0, 0xff, 0x80, 0x28 })
        {
            var initial = Initial(); initial.B = value; initial.HL = 0x4000;
            initial.IX = 0x2801; initial.WZ = 0x0800; initial.F = 1;
            using var register = new Circuit([0xcb, (byte)(0x40 | bit << 3)], initial);
            register.RunInstructions(1);
            int tested = value & (1 << bit);
            byte flags = (byte)(1 | 0x10 | (value & 0x28) | (tested == 0 ? 0x44 : tested & 0x80));
            Assert.Equal(flags, register.Cpu.Registers.F);
            using var memory = new Circuit([0xcb, (byte)(0x46 | bit << 3)], initial);
            memory.Memory[0x4000] = value; memory.RunInstructions(1);
            Assert.Equal((byte)((flags & ~0x28) | 8), memory.Cpu.Registers.F);
            Assert.Equal(13, memory.Cpu.InputClockCount);
            foreach (int alias in Enumerable.Range(0, 8))
            {
                using var indexed = new Circuit([0xdd, 0xcb, 0xff, (byte)(0x40 | bit << 3 | alias)], initial);
                indexed.Memory[0x2800] = value; indexed.RunInstructions(1);
                Assert.Equal((byte)((flags & ~0x28) | 0x28), indexed.Cpu.Registers.F);
                Assert.Empty(indexed.MemoryWrites);
                Assert.Equal(initial.B, indexed.Cpu.Registers.B);
                Assert.Equal(21, indexed.Cpu.InputClockCount);
            }
        }
    }

    [Fact]
    public void CarryInstructionsRespectIdentifiedSiliconAndQHistory()
    {
        foreach (var silicon in Enum.GetValues<Z80SiliconVariant>())
        foreach (bool prefix in new[] { false, true })
        foreach (bool complement in new[] { false, true })
        {
            var initial = Initial(); initial.A = 0; initial.F = 0x29; initial.Q = 0;
            byte op = complement ? (byte)0x3f : (byte)0x37;
            using var circuit = new Circuit(prefix ? [0xdd, op] : [op], initial, silicon: silicon);
            circuit.RunInstructions(1);
            int xy = silicon == Z80SiliconVariant.ZilogNmos ? 0x28 : 0;
            Assert.Equal((byte)(xy | (complement ? 0x10 : 1)), circuit.Cpu.Registers.F);
            Assert.Equal(circuit.Cpu.Registers.F, circuit.Cpu.Registers.Q);
        }
    }

    [Fact]
    public void AllSixteenBlockInstructionsTransferCompareOrPerformRealIo()
    {
        foreach (bool backward in new[] { false, true })
        foreach (bool repeat in new[] { false, true })
        foreach (int kind in Enumerable.Range(0, 4))
        {
            var initial = Initial(); initial.HL = 0x4100; initial.DE = 0x5100;
            initial.BC = kind < 2 ? (ushort)3 : (ushort)0x0320; initial.A = 0x99;
            byte opcode = (byte)(0xa0 | (backward ? 8 : 0) | (repeat ? 0x10 : 0) | kind);
            using var circuit = new Circuit([0xed, opcode], initial);
            int step = backward ? -1 : 1;
            for (int i = 0; i < 3; i++) circuit.Memory[0x4100 + step * i] = (byte)(0x31 + i);
            circuit.RunInstructions(repeat ? 3 : 1);
            int count = repeat ? 3 : 1;
            Assert.Equal((ushort)(0x4100 + step * count), circuit.Cpu.Registers.HL);
            Assert.Equal((repeat ? 58 : 16) + 1, circuit.Cpu.InputClockCount);
            if (kind == 0)
            {
                Assert.Equal(count, circuit.MemoryWrites.Count);
                for (int i = 0; i < count; i++) Assert.Equal((byte)(0x31 + i), circuit.Memory[0x5100 + step * i]);
            }
            if (kind == 1) Assert.Empty(circuit.MemoryWrites);
            if (kind == 2)
            {
                Assert.Equal(count, circuit.Reads.Count(x => x.Io));
                Assert.Equal(count, circuit.MemoryWrites.Count);
                Assert.Equal((ushort)0x0320, circuit.Reads.First(x => x.Io).Address);
            }
            if (kind == 3)
            {
                Assert.Equal(count, circuit.PortWrites.Count);
                Assert.Equal((ushort)0x0220, circuit.PortWrites[0].Address);
                Assert.Equal((byte)0x31, circuit.PortWrites[0].Value);
            }
        }
    }

    [Fact]
    public void HaltKeepsFetchingAndRefreshingWithoutAdvancingPcThenNmiPushesNextPc()
    {
        using var circuit = new Circuit([0x76, 0x3c]);
        circuit.RunInstructions(1); circuit.AdvanceTo(4000);
        Assert.True(circuit.Cpu.IsHalted);
        Assert.Equal((ushort)1, circuit.Cpu.Registers.PC);
        Assert.False(circuit.Cpu.Pins.HaltHigh);
        Assert.Equal(1, circuit.Cpu.InstructionsCompleted);
        Assert.True(circuit.Reads.Count(x => x.Address == 1) >= 2);
        circuit.Cpu.SetNmi(true); circuit.AdvanceTo(8000);
        Assert.False(circuit.Cpu.IsHalted);
        Assert.True(circuit.Cpu.Pins.HaltHigh);
        Assert.Equal((byte)1, circuit.Memory[0x9ffe]);
        Assert.Equal((byte)0, circuit.Memory[0x9fff]);
        Assert.Contains(circuit.Reads, x => x.Address == 0x66);
    }

    [Fact]
    public void InterruptModesHaveRealAcknowledgeStackAndUnmaskedOddVector()
    {
        foreach (var mode in Enum.GetValues<Z80InterruptMode>())
        {
            var initial = Initial(); initial.I = 0x80;
            var interrupts = new Z80InterruptState(true, true, mode, 0, true, false, false);
            using var circuit = new Circuit([0x00, 0x76], initial, interrupts);
            circuit.PortInputData = mode == Z80InterruptMode.Mode2 ? (byte)0xff : (byte)0xff;
            circuit.Memory[0x80ff] = 0x34; circuit.Memory[0x8100] = 0x12;
            int duration = mode == Z80InterruptMode.Mode2 ? 19 : 13;
            circuit.AdvanceTo((4 + duration) * 250);
            Assert.Equal((ushort)(mode == Z80InterruptMode.Mode2 ? 0x1234 : 0x38), circuit.Cpu.InstructionAddress);
            Assert.Equal((byte)1, circuit.Memory[0x9ffe]);
            Assert.Equal((byte)0, circuit.Memory[0x9fff]);
            Assert.Contains(circuit.Pins, x => !x.Value.MachineCycleOneHigh && x.Value.IoRequestHigh == false);
            Assert.False(circuit.Cpu.InterruptState.Iff1);
            Assert.False(circuit.Cpu.InterruptState.Iff2);
        }
    }

    [Fact]
    public void EiDefersIntThroughWholeFollowingInstructionAndLdAiLosesParityOnAcceptance()
    {
        using var circuit = new Circuit([0xfb, 0xdd, 0x21, 0x34, 0x12, 0x76]);
        circuit.Cpu.SetInt(true);
        circuit.RunInstructions(2);
        Assert.Equal((ushort)0x1234, circuit.Cpu.Registers.IX);
        Assert.Equal(CpuZ80Flow.InterruptAcknowledge, circuit.Cpu.CaptureState().Flow);
        var initial = Initial(); initial.I = 0x24;
        using var transfer = new Circuit([0xed, 0x57], initial,
            new Z80InterruptState(true, true, Z80InterruptMode.Mode1, 0, true, false, false));
        transfer.RunInstructions(1);
        Assert.Equal((byte)0x24, transfer.Cpu.Registers.A);
        Assert.Equal(0, transfer.Cpu.Registers.F & 4);
    }

    [Fact]
    public void DmaWaitsForExtendedMachineCycleAndResumesWithoutRepeatingWrites()
    {
        foreach (byte[] program in new byte[][]
            { [0x03, 0x00], [0x09, 0], [0xc5, 0], [0x34, 0], [0xed, 0xa0, 0] })
        {
            var initial = Initial(); initial.HL = 0x4000; initial.DE = 0x5000; initial.BC = 1;
            using var reference = new Circuit(program, initial); reference.Memory[0x4000] = 0x55;
            reference.RunInstructions(1);
            using var dma = new Circuit(program, initial); dma.Memory[0x4000] = 0x55;
            dma.Cpu.SetBusRequest(true);
            int limit = 200;
            while (!dma.Cpu.IsBusGranted && limit-- > 0) dma.Edge();
            Assert.True(dma.Cpu.IsBusGranted);
            long granted = dma.Cpu.TimeNanoseconds;
            dma.AdvanceTo(granted + 1000);
            Assert.False(dma.Cpu.Pins.BusAcknowledgeHigh);
            Assert.Null(dma.Cpu.Pins.Address); Assert.Null(dma.Cpu.Pins.MemoryRequestHigh);
            Assert.Equal(0, dma.Cpu.InstructionsCompleted);
            dma.Cpu.SetBusRequest(false);
            dma.RunInstructions(1);
            Assert.Equal(reference.Cpu.Registers, dma.Cpu.Registers);
            Assert.Equal(reference.Memory, dma.Memory);
            Assert.Equal(reference.MemoryWrites.Count, dma.MemoryWrites.Count);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NormalResetAbortsWaitOrHaltClearsDocumentedRegistersAndRecovers(bool fromHalt)
        => Circuit.VerifyNormalReset(Z80SiliconVariant.ZilogNmos, fromHalt);

    [Fact]
    public void RestoreAcrossEveryHalfClockRetainsBlocksControlAndInterruptResponses()
    {
        foreach (byte[] program in new byte[][] { [0xed, 0xb0, 0x76], [0xed, 0xb2, 0x76], [0x76] })
        for (int edge = 1; edge < 100; edge++)
        {
            var initial = Initial(); initial.HL = 0x4000; initial.DE = 0x5000; initial.BC = 3;
            using var original = new Circuit(program, initial);
            original.Memory[0x4000] = 0xab;
            original.AdvanceTo(edge * 125 + 60);
            var state = original.Cpu.CaptureState();
            using var restored = new Circuit(original.Memory, restore: state);
            original.ClearTraces();
            original.AdvanceTo(16000); restored.AdvanceTo(16000);
            AssertCpuState(original.Cpu.CaptureState(), restored.Cpu.CaptureState());
            Assert.Equal(original.Memory, restored.Memory);
            Assert.Equal(original.Pins, restored.Pins);
        }
    }

    [Fact]
    public void SpecialResetInM1T2FinishesInstructionThenPerformsIgnoredFetch()
    {
        var initial = Initial(); initial.I = 0x42;
        using var circuit = new Circuit([0x3e, 0x55, 0x00], initial);
        circuit.AdvanceTo(200); circuit.Cpu.SetReset(true);
        circuit.AdvanceTo(400); circuit.Cpu.SetReset(false);
        circuit.AdvanceTo(1750);
        Assert.Equal((byte)0x55, circuit.Cpu.Registers.A);
        Assert.Equal((byte)0x42, circuit.Cpu.Registers.I);
        Assert.Equal(CpuZ80Flow.SpecialResetFetch, circuit.Cpu.CaptureState().Flow);
        Assert.Equal((ushort)0, circuit.Cpu.Registers.PC);
        circuit.AdvanceTo(2750);
        Assert.Equal((ushort)0, circuit.Cpu.InstructionAddress);
        Assert.Equal(1, circuit.Cpu.InstructionsCompleted);
        Assert.Contains(circuit.Reads, x => x.Address == 2);
    }

    [Fact]
    public void SpecialResetDuringHaltExecutesFetchedByteWithoutPcIncrement()
    {
        var initial = Initial(); initial.A = 0x20;
        using var circuit = new Circuit([0x76, 0x3c, 0x00], initial);
        circuit.AdvanceTo(1200); circuit.Cpu.SetReset(true);
        circuit.AdvanceTo(1400); circuit.Cpu.SetReset(false);
        circuit.AdvanceTo(2000);
        Assert.False(circuit.Cpu.IsHalted);
        Assert.Equal((byte)0x21, circuit.Cpu.Registers.A);
        Assert.Equal(CpuZ80Flow.SpecialResetFetch, circuit.Cpu.CaptureState().Flow);
        Assert.Equal(2, circuit.Cpu.InstructionsCompleted);
        circuit.AdvanceTo(3000);
        Assert.Equal((ushort)0, circuit.Cpu.InstructionAddress);
    }

    [Fact]
    public void IntSamplesAtLastTAndNmiRetainsPriorityAndElevenClockResponse()
    {
        foreach (bool early in new[] { false, true })
        {
            using var circuit = new Circuit([0x00, 0x00], interrupts:
                new Z80InterruptState(true, true, Z80InterruptMode.Mode1, 0, false, false, false));
            circuit.AdvanceTo(early ? 700 : 800); circuit.Cpu.SetInt(true);
            circuit.AdvanceTo(1000);
            Assert.Equal(early ? CpuZ80Flow.InterruptAcknowledge : CpuZ80Flow.Fetch,
                circuit.Cpu.CaptureState().Flow);
        }
        using var nmi = new Circuit([0x00], interrupts:
            new Z80InterruptState(true, true, Z80InterruptMode.Mode1, 0, true, false, false));
        nmi.Cpu.SetNmi(true); nmi.AdvanceTo(15 * 250);
        Assert.Equal((ushort)0x66, nmi.Cpu.InstructionAddress);
        Assert.False(nmi.Cpu.InterruptState.Iff1); Assert.True(nmi.Cpu.InterruptState.Iff2);
        Assert.DoesNotContain(nmi.Pins, x => !x.Value.MachineCycleOneHigh && x.Value.IoRequestHigh == false);
    }

    [Fact]
    public void DmaAndInterruptAndResetStatesResumeAtEveryHalfClock()
    {
        for (int scenario = 0; scenario < 3; scenario++)
        for (int edge = 1; edge < 100; edge++)
        {
            using var original = new Circuit([0x21, 0x34, 0x12, 0x76], interrupts:
                new Z80InterruptState(true, true, Z80InterruptMode.Mode2, 0, false, false, false));
            if (scenario == 0) original.Cpu.SetBusRequest(true);
            if (scenario == 1) original.Cpu.SetNmi(true);
            if (scenario == 2) original.Cpu.SetReset(true);
            original.AdvanceTo(edge * 125 + 60);
            using var restored = new Circuit(original.Memory, restore: original.Cpu.CaptureState());
            original.Cpu.SetBusRequest(false); restored.Cpu.SetBusRequest(false);
            original.Cpu.SetReset(false); restored.Cpu.SetReset(false);
            original.ClearTraces();
            original.AdvanceTo(18000); restored.AdvanceTo(18000);
            AssertCpuState(original.Cpu.CaptureState(), restored.Cpu.CaptureState());
            Assert.Equal(original.Pins, restored.Pins);
            Assert.Equal(original.Memory, restored.Memory);
        }
    }

    [Fact]
    public void SerializedCpuStatePreservesControlInputsAndContinuationWithoutExternalFiles()
    {
        for (int scenario = 0; scenario < 4; scenario++)
        {
            using var original = new Circuit([0x21, 0x34, 0x12, 0x76]);
            if (scenario == 0) original.Cpu.SetWait(true);
            if (scenario == 1) original.Cpu.SetBusRequest(true);
            if (scenario == 2) original.Cpu.SetReset(true);
            if (scenario == 3) original.Cpu.SetNmi(true);
            original.AdvanceTo(2125);
            var captured = original.Cpu.CaptureState();
            string json = JsonSerializer.Serialize(captured);
            var transported = JsonSerializer.Deserialize<CpuZ80State>(json)!;
            AssertCpuState(captured, transported);
            using var restored = new Circuit(original.Memory, restore: transported);
            original.Cpu.SetWait(false); restored.Cpu.SetWait(false);
            original.Cpu.SetReset(false); restored.Cpu.SetReset(false);
            original.Cpu.SetBusRequest(false); restored.Cpu.SetBusRequest(false);
            original.AdvanceTo(18000); restored.AdvanceTo(18000);
            AssertCpuState(original.Cpu.CaptureState(), restored.Cpu.CaptureState());
            Assert.Equal(original.Memory, restored.Memory);
        }
    }

    [Fact]
    public void OriginalZilogZ80AndZ80AUseTheirOwnClockAndBothReleaseEdgeDelays()
    {
        foreach (var timing in new[] { Z80BusTiming.ZilogZ80Maximum, Z80BusTiming.ZilogZ80AMaximum })
        {
            using var circuit = new Circuit([0x3e, 0x42, 0x32, 0, 0x40, 0xdb, 0xff, 0x76], timing: timing);
            circuit.RunInstructions(4);
            long period = timing.MinimumPeriod;
            Assert.Equal(35 * period, circuit.Cpu.TimeNanoseconds);
            Assert.Equal(36, circuit.Cpu.InputClockCount);
            Assert.Equal((byte)0x42, circuit.Memory[0x4000]);
            Assert.Equal((byte)0x7b, circuit.Cpu.Registers.A);
            Assert.Equal(Z80HardwareProfile.ZilogNmos, circuit.Cpu.HardwareProfile);
            Assert.Contains(circuit.Pins, p => p.Time == 2 * period + timing.ReadRelease && p.Value.ReadHigh == true);
            Assert.Contains(circuit.Pins, p => p.Time == 30 * period + period / 2 + timing.IoReleaseFalling
                && p.Value.IoRequestHigh == true && p.Value.ReadHigh == true);
            Assert.Equal(19 * period + period / 2 + timing.WriteRelease, Assert.Single(circuit.MemoryWrites).Time);
        }
    }

    [Fact]
    public void NecIsOnlyAHardwareProfileOfTheSameEngineAndCannotContaminateZilogState()
    {
        using var zilog = new Circuit([0x00]);
        using var nec = new Circuit([0x00], silicon: Z80SiliconVariant.NecNmos, timing: Upd780C1Profile.Timing);
        Assert.IsType<CpuZ80>(zilog.Cpu); Assert.IsType<CpuZ80>(nec.Cpu);
        Assert.IsAssignableFrom<RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Interfaces.ICpuZ80>(nec.Cpu);
        Assert.Equal(Z80BehaviorReservations.None, zilog.Cpu.HardwareProfile.Reservations);
        Assert.True(nec.Cpu.HardwareProfile.Reservations.HasFlag(Z80BehaviorReservations.SpecialReset));
        Assert.True(nec.Cpu.HardwareProfile.Reservations.HasFlag(Z80BehaviorReservations.HaltBusDrive));
        var original = zilog.Cpu.CaptureState();
        Assert.Throws<ArgumentException>(() => zilog.Cpu.RestoreState(original with { HardwareProfile = Upd780C1Profile.Hardware }));
        AssertCpuState(original, zilog.Cpu.CaptureState());
        Assert.Equal(Z80CarryFlagSource.AccumulatorWithQHistory, zilog.Cpu.HardwareProfile.CarryFlagSource);
        Assert.Equal(Z80CarryFlagSource.Accumulator, nec.Cpu.HardwareProfile.CarryFlagSource);
    }

    [Fact]
    public void ReceivedClockTrainsMatchEveryPinDeadlineAndStateAcrossControlsAndInternalBoundaries()
    {
        byte[][] programs = [[0x00, 0xc3, 0, 0], [0x09, 0x18, 0xfd], [0x18, 0xfe],
            [0xed, 0x6f, 0xc3, 0, 0], [0xed, 0xb1, 0xc3, 0, 0], [0xed, 0xb2, 0xc3, 0, 0],
            [0xdd, 0x7e, 1, 0xc3, 0, 0], [0xe3, 0xc3, 0, 0], [0x76]];
        foreach (byte[] program in programs)
        foreach (int model in Enumerable.Range(0, 3))
        foreach (int edges in new[] { 1, 2, 3, 7, 15, 16, 31, 64, 257 })
        foreach (int control in Enumerable.Range(0, 5))
        {
            var timing = model == 0 ? Z80BusTiming.ZilogZ80Maximum : model == 1
                ? Z80BusTiming.ZilogZ80AMaximum : Upd780C1Profile.Timing;
            var silicon = model == 2 ? Z80SiliconVariant.NecNmos : Z80SiliconVariant.ZilogNmos;
            var initial = Initial(); initial.HL = 0x4000; initial.DE = 0x5000;
            initial.IX = 0x6000; initial.BC = 4;
            using var scalar = new Circuit(program, initial, silicon: silicon, timing: timing);
            using var train = new Circuit(program, initial, silicon: silicon, timing: timing);
            scalar.Memory[0x4000] = train.Memory[0x4000] = 0x9a;
            var scalarDeadlines = new List<(long, long?)>(); var trainDeadlines = new List<(long, long?)>();
            scalar.Cpu.NextEventNanosecondsChanged = next => scalarDeadlines.Add((scalar.Cpu.TimeNanoseconds, next));
            train.Cpu.NextEventNanosecondsChanged = next => trainDeadlines.Add((train.Cpu.TimeNanoseconds, next));
            AttachClockTrainControl(scalar, control); AttachClockTrainControl(train, control);
            long half = timing.MinimumPeriod / 2;
            scalar.AdvanceTo(edges * half);
            train.Cpu.ReceiveClockEdges(half, half, edges);
            AssertCpuState(scalar.Cpu.CaptureState(), train.Cpu.CaptureState());
            Assert.Equal(scalar.Pins, train.Pins); Assert.Equal(scalarDeadlines, trainDeadlines);
            Assert.Equal(scalar.Memory, train.Memory); Assert.Equal(scalar.MemoryWrites, train.MemoryWrites);
            Assert.Equal(scalar.PortWrites, train.PortWrites); Assert.Equal(scalar.Reads, train.Reads);
        }
    }

    private static void AttachClockTrainControl(Circuit circuit, int control)
    {
        var previous = circuit.Cpu.PinsChanged;
        bool applied = false;
        circuit.Cpu.PinsChanged = pins =>
        {
            previous!(pins);
            if (applied || circuit.Cpu.TimeNanoseconds < 900) return;
            applied = true;
            switch (control)
            {
                case 1: circuit.Cpu.SetNmi(true); break;
                case 2: circuit.Cpu.SetBusRequest(true); break;
                case 3: circuit.Cpu.SetReset(true); break;
                case 4: circuit.Cpu.SetWait(true); break;
            }
        };
    }

    [Fact]
    public void ClockTrainRejectsInvalidTimingOverflowAndReentranceBeforeChangingState()
    {
        using var circuit = new Circuit([0x00, 0xc3, 0, 0]);
        var before = circuit.Cpu.CaptureState();
        Assert.Throws<ArgumentOutOfRangeException>(() => circuit.Cpu.ReceiveClockEdges(125, 125, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => circuit.Cpu.ReceiveClockEdges(-1, 125, 10));
        Assert.Throws<ArgumentException>(() => circuit.Cpu.ReceiveClockEdges(125, 100, 10));
        Assert.Throws<ArgumentException>(() => circuit.Cpu.ReceiveClockEdges(110, 125, 10));
        Assert.Throws<OverflowException>(() => circuit.Cpu.ReceiveClockEdges(long.MaxValue - 200, 125, 10));
        AssertCpuState(before, circuit.Cpu.CaptureState());
        var listener = circuit.Cpu.PinsChanged;
        bool observed = false;
        circuit.Cpu.PinsChanged = pins =>
        {
            listener!(pins);
            if (observed) return;
            observed = true;
            Assert.Throws<InvalidOperationException>(() => circuit.Cpu.ReceiveClockEdges(circuit.Cpu.TimeNanoseconds + 125, 125, 4));
        };
        circuit.Cpu.ReceiveClockEdges(125, 125, 16);
        Assert.True(observed);
    }

    [Fact]
    public void DeadlineListenersCanAttachFromPinsAndReplaceDetachReattachWithoutLostEvents()
    {
        List<(long Time, long? Next, string Listener)> Run(bool grouped)
        {
            using var circuit = new Circuit([0x00, 0xc3, 0, 0]);
            var events = new List<(long, long?, string)>();
            Action<long?> third = next => events.Add((circuit.Cpu.TimeNanoseconds, next, "C"));
            Action<long?> second = next =>
            {
                events.Add((circuit.Cpu.TimeNanoseconds, next, "B"));
                if (circuit.Cpu.TimeNanoseconds == 210)
                {
                    circuit.Cpu.NextEventNanosecondsChanged = null;
                    circuit.Cpu.NextEventNanosecondsChanged = third;
                }
            };
            Action<long?> first = next =>
            {
                events.Add((circuit.Cpu.TimeNanoseconds, next, "A"));
                circuit.Cpu.NextEventNanosecondsChanged = second;
            };
            var pinsListener = circuit.Cpu.PinsChanged;
            circuit.Cpu.PinsChanged = pins =>
            {
                pinsListener!(pins);
                if (circuit.Cpu.TimeNanoseconds == 100) circuit.Cpu.NextEventNanosecondsChanged = first;
            };
            if (grouped) circuit.Cpu.ReceiveClockEdges(125, 125, 8);
            else circuit.AdvanceTo(1000);
            return events;
        }
        var scalar = Run(false); var train = Run(true);
        Assert.Equal(scalar, train);
        Assert.Equal(new (long, long?, string)[] { (100, 110, "A"), (110, null, "B"),
            (125, 210, "B"), (210, 220, "B"), (220, null, "C") }, scalar.Take(5));
    }

    [Fact]
    public void PackedPinsPreserveAllDrivenUnknownAndInitValuesAndPhysicalReadWriteQualifications()
    {
        bool?[] levels = [null, false, true];
        ushort?[] addresses = [null, 0, ushort.MaxValue];
        byte?[] dataValues = [null, 0, byte.MaxValue];
        foreach (ushort? address in addresses)
        foreach (byte? data in dataValues)
        for (int control = 0; control < 81; control++)
        for (int flags = 0; flags < 16; flags++)
        {
            bool? memory = levels[control % 3], io = levels[control / 3 % 3],
                read = levels[control / 9 % 3], write = levels[control / 27 % 3];
            bool m1 = (flags & 1) != 0, refresh = (flags & 2) != 0,
                halt = (flags & 4) != 0, acknowledge = (flags & 8) != 0;
            var pins = new Z80BusPins(address, data, memory, io, read, write, m1, refresh, halt, acknowledge);
            Assert.Equal(address, pins.Address); Assert.Equal(data, pins.Data);
            Assert.Equal(memory, pins.MemoryRequestHigh); Assert.Equal(io, pins.IoRequestHigh);
            Assert.Equal(read, pins.ReadHigh); Assert.Equal(write, pins.WriteHigh);
            Assert.Equal(m1, pins.MachineCycleOneHigh); Assert.Equal(refresh, pins.RefreshHigh);
            Assert.Equal(halt, pins.HaltHigh); Assert.Equal(acknowledge, pins.BusAcknowledgeHigh);
            Assert.Equal(address.HasValue && memory == false && read == false, pins.IsMemoryRead());
            Assert.Equal(address.HasValue && io == false && read == false, pins.IsIoRead());
            Assert.Equal(address.HasValue && data.HasValue && memory == false && write == false, pins.IsMemoryWrite());
            Assert.Equal(address.HasValue && data.HasValue && io == false && write == false, pins.IsIoWrite());
            var changed = pins with { Address = null, Data = 0, WriteHigh = false };
            Assert.Null(changed.Address); Assert.Equal((byte)0, changed.Data); Assert.False(changed.WriteHigh);
            Assert.Equal(read, changed.ReadHigh); Assert.Equal(address, pins.Address);
            var (a, d, mr, ir, rd, wr, mc, rf, ht, ba) = pins;
            Assert.Equal((address, data, memory, io, read, write, m1, refresh, halt, acknowledge),
                (a, d, mr, ir, rd, wr, mc, rf, ht, ba));
            if (control is 0 or 40 or 80 && flags is 0 or 15 && address == ushort.MaxValue && data == byte.MaxValue)
                Assert.Equal(pins, JsonSerializer.Deserialize<Z80BusPins>(JsonSerializer.Serialize(pins)));
        }
        Assert.Equal(new Z80BusPins(null, null, null, null, null, null, false, false, false, false), default);
    }
}
