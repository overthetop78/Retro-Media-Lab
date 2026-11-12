using Circuit = RetroMediaLab.Chips.Tests.CPU.Common.Z80.Z80TestCircuit;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Enums;

namespace RetroMediaLab.Chips.Tests.CPU.Zilog.Z80.Z80;

public sealed partial class ExecutionTests
{
    [Fact]
    public void IndexedCbOperationsUseTwoM1ThenTwoOperandReadsAndCopyToRealRegisters()
    {
        foreach (byte prefix in new byte[] { 0xdd, 0xfd })
        for (int opcode = 0; opcode < 256; opcode++)
        {
            int group = opcode >> 6, bit = (opcode >> 3) & 7, register = opcode & 7;
            if (group == 1) continue;
            foreach (byte value in new byte[] { 0, 1, 0x7f, 0x80, 0xff })
            foreach (byte carry in new byte[] { 0, 1 })
            {
                var initial = Initial(); initial.IX = initial.IY = 0x8001;
                initial.HL = 0x1234; initial.F = (byte)(0xfe | carry);
                using var circuit = new Circuit([prefix, 0xcb, 0xff, (byte)opcode], initial);
                circuit.Memory[0x8000] = value; circuit.RunInstructions(1);
                int shifted = group switch
                {
                    2 => value & ~(1 << bit), 3 => value | (1 << bit),
                    _ => bit switch
                    {
                        0 => value * 2 + value / 128, 1 => value / 2 + value % 2 * 128,
                        2 => value * 2 + carry, 3 => value / 2 + carry * 128,
                        4 => value * 2, 5 => value / 2 + value / 128 * 128,
                        6 => value * 2 + 1, _ => value / 2,
                    },
                };
                byte expected = unchecked((byte)shifted);
                int flags = initial.F;
                if (group == 0)
                {
                    int ones = 0;
                    for (int position = 0; position < 8; position++) if ((expected & (1 << position)) != 0) ones++;
                    int outputCarry = bit is 0 or 2 or 4 or 6 ? value / 128 : value % 2;
                    flags = (expected & 0xa8) | (expected == 0 ? 0x40 : 0) | (ones % 2 == 0 ? 4 : 0) | outputCarry;
                }
                var r = circuit.Cpu.Registers;
                Assert.Equal((ushort)0x8001, r.IX); Assert.Equal((ushort)0x8001, r.IY);
                if (register != 6)
                    Assert.Equal(expected, register switch { 0 => r.B, 1 => r.C, 2 => r.D, 3 => r.E, 4 => r.H, 5 => r.L, _ => r.A });
                else Assert.Equal(initial.HL, r.HL);
                Assert.Equal((byte)flags, r.F); Assert.Equal((byte)0x83, r.R);
                Assert.Equal(expected, circuit.Memory[0x8000]);
                Assert.Equal((ushort)0x8000, Assert.Single(circuit.MemoryWrites).Address);
                Assert.Equal(new ushort[] { 0, 1, 2, 3, 0x8000 }, circuit.Reads.Select(read => read.Address));
                Assert.Equal(23 * 250L, circuit.Cpu.TimeNanoseconds);
            }
        }
    }

    [Fact]
    public void ExchangeStackPairKeepsTheLastDataDrivenDuringItsInternalStates()
    {
        foreach (byte prefix in new byte[] { 0, 0xdd, 0xfd })
        {
            var initial = Initial(); initial.PC = 0x4000; initial.SP = 0xffff;
            initial.HL = initial.IX = initial.IY = 0x1234;
            using var circuit = new Circuit(prefix == 0 ? [0xe3] : [prefix, 0xe3], initial);
            circuit.Memory[0xffff] = 0x55; circuit.Memory[0] = 0xaa;
            int extra = prefix == 0 ? 0 : 4;
            circuit.AdvanceTo((17 + extra) * 250L + 100);
            Assert.Equal(CpuZ80Flow.InternalExchangeFinish, circuit.Cpu.CaptureState().Flow);
            Assert.True(circuit.Cpu.CaptureState().Bus.HoldWriteData);
            Assert.Equal((byte)0x34, circuit.Cpu.Pins.Data);
            Assert.True(circuit.Cpu.Pins.MemoryRequestHigh); Assert.True(circuit.Cpu.Pins.WriteHigh);
            Assert.Equal(new ushort[] { 0, 0xffff }, circuit.MemoryWrites.Select(w => w.Address));
            Assert.Equal(new byte[] { 0x12, 0x34 }, circuit.MemoryWrites.Select(w => w.Value));
            circuit.RunInstructions(1);
            var r = circuit.Cpu.Registers;
            Assert.Equal((ushort)0xaa55, prefix switch { 0 => r.HL, 0xdd => r.IX, _ => r.IY });
            Assert.Equal((ushort)0xffff, r.SP); Assert.Equal(initial.F, r.F);
            Assert.Equal((19 + extra) * 250L, circuit.Cpu.TimeNanoseconds);
            circuit.AdvanceTo(circuit.Cpu.TimeNanoseconds + 100);
            Assert.Null(circuit.Cpu.Pins.Data);
        }
    }

    [Fact]
    public void IndexedCbAndStackExchangeResumeIncludingHeldDataAndOperandWait()
    {
        (byte[] Program, int Clocks)[] programs = [([0xdd, 0xcb, 0xff, 0x04], 23),
            ([0xfd, 0xcb, 0xff, 0x36], 23), ([0xe3], 19), ([0xdd, 0xe3], 23), ([0xfd, 0xe3], 23)];
        foreach (var (program, clocks) in programs)
        for (long time = 0; time < clocks * 250L; time += 125)
        {
            var initial = Initial(); initial.HL = 0x1234; initial.IX = initial.IY = 0x8001;
            using var original = new Circuit(program, initial);
            original.Memory[0x8000] = 0x7f; original.Memory[initial.SP] = 0x55; original.Memory[initial.SP + 1] = 0xaa;
            original.AdvanceTo(time);
            using var restored = new Circuit(original.Memory, restore: original.Cpu.CaptureState());
            original.ClearTraces(); original.RunInstructions(1); restored.RunInstructions(1);
            Assert.Equal(clocks * 250L, original.Cpu.TimeNanoseconds);
            Assert.Equal(original.Memory, restored.Memory); Assert.Equal(original.MemoryWrites, restored.MemoryWrites);
            Assert.Equal(original.Pins, restored.Pins); AssertCpuState(original.Cpu.CaptureState(), restored.Cpu.CaptureState());
        }
        var start = Initial(); start.IX = 0x8001;
        using var wait = new Circuit([0xdd, 0xcb, 0xff, 0x00], start);
        wait.Memory[0x8000] = 0x81;
        wait.AdvanceTo(2750); wait.Cpu.SetWait(true); wait.AdvanceTo(3500);
        using var resume = new Circuit(wait.Memory, restore: wait.Cpu.CaptureState());
        wait.ClearTraces(); wait.Cpu.SetWait(false); resume.Cpu.SetWait(false);
        wait.RunInstructions(1); resume.RunInstructions(1);
        Assert.Equal((byte)3, wait.Cpu.Registers.B); Assert.Equal((byte)0x83, wait.Cpu.Registers.R);
        Assert.Equal(25 * 250L, wait.Cpu.TimeNanoseconds);
        Assert.Equal(wait.Pins, resume.Pins); AssertCpuState(wait.Cpu.CaptureState(), resume.Cpu.CaptureState());
    }

    [Fact]
    public void IndexPairTransfersStackAndJumpKeepTheOtherPairsIndependent()
    {
        foreach (byte prefix in new byte[] { 0xdd, 0xfd })
        {
            byte[] program = [prefix, 0x21, 0x34, 0x12, prefix, 0x22, 0x00, 0x90,
                prefix, 0x21, 0x00, 0x00, prefix, 0x2a, 0x00, 0x90, prefix, 0x23, prefix, 0x2b,
                prefix, 0xe5, prefix, 0xe1, prefix, 0xf9, prefix, 0xe9];
            var initial = Initial(); initial.HL = 0xa55a; initial.IX = 0xabcd; initial.IY = 0x5678;
            using var circuit = new Circuit(program, initial);
            circuit.RunInstructions(10);
            var r = circuit.Cpu.Registers;
            Assert.Equal((ushort)0x1234, prefix == 0xdd ? r.IX : r.IY);
            Assert.Equal(prefix == 0xdd ? initial.IY : initial.IX, prefix == 0xdd ? r.IY : r.IX);
            Assert.Equal(initial.HL, r.HL); Assert.Equal(initial.AF, r.AF);
            Assert.Equal((ushort)0x1234, r.SP); Assert.Equal((ushort)0x1234, circuit.Cpu.InstructionAddress);
            Assert.Equal(135 * 250L, circuit.Cpu.TimeNanoseconds);
            Assert.Equal(new ushort[] { 0x9000, 0x9001, 0x9fff, 0x9ffe }, circuit.MemoryWrites.Select(w => w.Address));
            Assert.Equal(new byte[] { 0x34, 0x12, 0x12, 0x34 }, circuit.MemoryWrites.Select(w => w.Value));
            Assert.Equal((byte)0x95, r.R);
        }
    }

    [Fact]
    public void IndexAddUsesEveryPairIncludingTheIndexItself()
    {
        foreach (byte prefix in new byte[] { 0xdd, 0xfd })
        for (int pair = 0; pair < 4; pair++)
        foreach (byte flags in new byte[] { 0, 0xff })
        {
            var initial = Initial(); initial.HL = 0x7777; initial.IX = initial.IY = 0x0fff;
            initial.BC = initial.DE = initial.SP = 1; initial.F = flags;
            using var circuit = new Circuit([prefix, (byte)(0x09 + pair * 16)], initial);
            circuit.RunInstructions(1);
            ushort expected = pair == 2 ? (ushort)0x1ffe : (ushort)0x1000;
            var r = circuit.Cpu.Registers;
            Assert.Equal(expected, prefix == 0xdd ? r.IX : r.IY);
            Assert.Equal((byte)((flags & 0xc4) | 0x10 | ((expected >> 8) & 0x28)), r.F);
            Assert.Equal(initial.HL, r.HL);
            Assert.Equal(15 * 250L, circuit.Cpu.TimeNanoseconds);
        }
    }

    [Fact]
    public void IndexByteRegisterTransfersAndArithmeticUseTheSelectedByteViews()
    {
        foreach (byte prefix in new byte[] { 0xdd, 0xfd })
        {
            // Sélecteurs H/L sans mémoire : vues de la paire indexée.
            for (int destination = 0; destination < 8; destination++)
            for (int source = 0; source < 8; source++)
            {
                if (destination == 6 || source == 6) continue;
                var initial = Initial(); initial.BC = 0x1122; initial.DE = 0x3344; initial.HL = 0xaabb;
                initial.IX = initial.IY = 0x5566; initial.A = 0x77;
                using var circuit = new Circuit([prefix, (byte)(0x40 + destination * 8 + source)], initial);
                circuit.RunInstructions(1);
                byte[] expected = [0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0, 0x77];
                expected[destination] = expected[source];
                var r = circuit.Cpu.Registers;
                Assert.Equal(expected[0], r.B); Assert.Equal(expected[1], r.C);
                Assert.Equal(expected[2], r.D); Assert.Equal(expected[3], r.E); Assert.Equal(expected[7], r.A);
                Assert.Equal((ushort)((expected[4] << 8) | expected[5]), prefix == 0xdd ? r.IX : r.IY);
                Assert.Equal(initial.HL, r.HL); Assert.Equal(initial.F, r.F);
                Assert.Equal(8 * 250L, circuit.Cpu.TimeNanoseconds);
            }
            for (int operation = 0; operation < 8; operation++)
            foreach (int source in new[] { 4, 5 })
            {
                var initial = Initial(); initial.A = 0x7f; initial.F = 1; initial.IX = initial.IY = 0x0128;
                using var circuit = new Circuit([prefix, (byte)(0x80 + operation * 8 + source)], initial);
                circuit.RunInstructions(1);
                var expected = ReferenceAlu(operation, 0x7f, source == 4 ? (byte)1 : (byte)0x28, 1);
                Assert.Equal(expected.A, circuit.Cpu.Registers.A); Assert.Equal(expected.Flags, circuit.Cpu.Registers.F);
                Assert.Equal(8 * 250L, circuit.Cpu.TimeNanoseconds);
            }
            using var bytes = new Circuit([prefix, 0x26, 0x7f, prefix, 0x2e, 0xff, prefix, 0x24, prefix, 0x2d]);
            bytes.RunInstructions(4);
            Assert.Equal((ushort)0x80fe, prefix == 0xdd ? bytes.Cpu.Registers.IX : bytes.Cpu.Registers.IY);
            Assert.Equal(38 * 250L, bytes.Cpu.TimeNanoseconds); // 11+11+8+8
        }
    }

    [Fact]
    public void IndexedLoadsPreserveRealHlAndUseSignedWrappedEffectiveAddresses()
    {
        foreach (byte prefix in new byte[] { 0xdd, 0xfd })
        foreach (byte displacement in new byte[] { 0, 1, 0x7f, 0x80, 0xff })
        foreach (ushort index in new ushort[] { 0, 0x8000, 0xffff })
        for (int code = 0; code < 8; code++)
        foreach (bool write in new[] { false, true })
        {
            if (code == 6) continue;
            var initial = Initial(); initial.PC = 0x4000; initial.HL = 0x1234;
            initial.IX = initial.IY = index; initial.BC = 0x5678; initial.DE = 0x9abc;
            ushort address = unchecked((ushort)(index + (displacement < 128 ? displacement : displacement - 256)));
            byte value = code switch { 0 => initial.B, 1 => initial.C, 2 => initial.D, 3 => initial.E,
                4 => initial.H, 5 => initial.L, _ => initial.A };
            byte opcode = (byte)(write ? 0x70 + code : 0x46 + code * 8);
            using var circuit = new Circuit([prefix, opcode, displacement], initial);
            circuit.Memory[address] = 0xe7;
            circuit.RunInstructions(1);
            var r = circuit.Cpu.Registers;
            Assert.Equal(index, r.IX); Assert.Equal(index, r.IY);
            if (write)
            {
                Assert.Equal(value, circuit.Memory[address]); Assert.Single(circuit.MemoryWrites);
                Assert.Equal(initial.HL, r.HL);
            }
            else
            {
                Assert.Equal((byte)0xe7, code switch { 0 => r.B, 1 => r.C, 2 => r.D, 3 => r.E, 4 => r.H, 5 => r.L, _ => r.A });
                Assert.Empty(circuit.MemoryWrites);
                Assert.Equal(address, circuit.Reads.Last().Address);
            }
            Assert.Equal(initial.F, r.F);
            Assert.Equal(19 * 250L, circuit.Cpu.TimeNanoseconds);
        }
    }

    [Fact]
    public void IndexedImmediateArithmeticAndModificationUseTheirRealBusCycles()
    {
        foreach (byte prefix in new byte[] { 0xdd, 0xfd })
        {
            var initial = Initial(); initial.IX = initial.IY = 0x8001; initial.HL = 0x9000;
            byte[] program = [prefix, 0x36, 0xff, 0x7f, prefix, 0x34, 0xff, prefix, 0x35, 0xff];
            using var circuit = new Circuit(program, initial);
            circuit.RunInstructions(3);
            Assert.Equal(new byte[] { 0x7f, 0x80, 0x7f }, circuit.MemoryWrites.Select(w => w.Value));
            Assert.All(circuit.MemoryWrites, w => Assert.Equal((ushort)0x8000, w.Address));
            Assert.Equal(65 * 250L, circuit.Cpu.TimeNanoseconds); // 19+23+23
            Assert.Equal(initial.HL, circuit.Cpu.Registers.HL);
            for (int operation = 0; operation < 8; operation++)
            {
                initial.A = 0x7f; initial.F = 1;
                using var alu = new Circuit([prefix, (byte)(0x86 + operation * 8), 0xff], initial);
                alu.Memory[0x8000] = 0x28; alu.RunInstructions(1);
                var expected = ReferenceAlu(operation, 0x7f, 0x28, 1);
                Assert.Equal(expected.A, alu.Cpu.Registers.A); Assert.Equal(expected.Flags, alu.Cpu.Registers.F);
                Assert.Equal(19 * 250L, alu.Cpu.TimeNanoseconds);
                Assert.Equal(new ushort[] { 0, 1, 2, 0x8000 }, alu.Reads.Select(r => r.Address));
            }
        }
    }

    [Fact]
    public void PrefixChainsUseTheLastIndexAndEdExchangesAndIgnoredInstructionsKeepTheirMeaning()
    {
        using var chain = new Circuit([0xdd, 0xfd, 0xdd, 0xfd, 0x21, 0x34, 0x12]);
        chain.RunInstructions(1);
        Assert.Equal((ushort)0x1234, chain.Cpu.Registers.IY); Assert.Equal((ushort)0, chain.Cpu.Registers.IX);
        Assert.Equal(26 * 250L, chain.Cpu.TimeNanoseconds); Assert.Equal((byte)0x86, chain.Cpu.Registers.R);
        foreach (byte prefix in new byte[] { 0xdd, 0xfd })
        {
            var initial = Initial(); initial.HL = 0x1234; initial.DE = 0x5678;
            initial.IX = 0x8000; initial.IY = 0x9000; initial.AlternateHL = 0xabcd;
            using var circuit = new Circuit([prefix, 0xeb, prefix, 0xd9, prefix, 0xed, 0x63, 0x00, 0xa0,
                prefix, 0x3e, 0x77, 0x21, 0x22, 0x11], initial);
            circuit.RunInstructions(5);
            var r = circuit.Cpu.Registers;
            Assert.Equal((ushort)0x8000, r.IX); Assert.Equal((ushort)0x9000, r.IY);
            Assert.Equal(new byte[] { 0xcd, 0xab }, circuit.Memory[0xa000..0xa002]);
            Assert.Equal((ushort)0x1122, r.HL); Assert.Equal((byte)0x77, r.A);
            Assert.Equal(61 * 250L, circuit.Cpu.TimeNanoseconds); // 8+8+24+11+10
        }
    }

    [Fact]
    public void IndexedAccessAndPrefixSelectionResumeAtEveryHalfClockWithWait()
    {
        (byte[] Program, int Clocks)[] programs = [([0xdd, 0x21, 0x34, 0x12], 14),
            ([0xfd, 0x66, 0xff], 19), ([0xdd, 0x74, 0xff], 19), ([0xfd, 0x36, 0xff, 0x55], 19),
            ([0xdd, 0x34, 0xff], 23), ([0xfd, 0x86, 0xff], 19), ([0xfd, 0xe5], 15),
            ([0xdd, 0xfd, 0x21, 0x34, 0x12], 18), ([0xdd, 0xed, 0x6b, 0x00, 0x80], 24)];
        foreach (var (program, clocks) in programs)
        for (long time = 0; time < clocks * 250L; time += 125)
        {
            var initial = Initial(); initial.IX = initial.IY = 0x8001; initial.HL = 0x1234;
            using var original = new Circuit(program, initial); original.Memory[0x8000] = 0x7f;
            original.AdvanceTo(time);
            using var restored = new Circuit(original.Memory, restore: original.Cpu.CaptureState());
            original.ClearTraces(); original.RunInstructions(1); restored.RunInstructions(1);
            Assert.Equal(clocks * 250L, original.Cpu.TimeNanoseconds);
            Assert.Equal(original.Memory, restored.Memory); Assert.Equal(original.MemoryWrites, restored.MemoryWrites);
            Assert.Equal(original.Pins, restored.Pins); AssertCpuState(original.Cpu.CaptureState(), restored.Cpu.CaptureState());
        }
        var waitStart = Initial(); waitStart.IX = 0x8001;
        using var wait = new Circuit([0xdd, 0x46, 0xff], waitStart);
        wait.Memory[0x8000] = 0x28; wait.AdvanceTo(4000); wait.Cpu.SetWait(true); wait.AdvanceTo(4750);
        using var resume = new Circuit(wait.Memory, restore: wait.Cpu.CaptureState());
        wait.ClearTraces(); wait.Cpu.SetWait(false); resume.Cpu.SetWait(false);
        wait.RunInstructions(1); resume.RunInstructions(1);
        Assert.Equal((byte)0x28, wait.Cpu.Registers.B);
        Assert.Equal(21 * 250L, wait.Cpu.TimeNanoseconds);
        Assert.Equal(wait.Pins, resume.Pins); AssertCpuState(wait.Cpu.CaptureState(), resume.Cpu.CaptureState());
        Assert.Throws<ArgumentException>(() => resume.Cpu.RestoreState(resume.Cpu.CaptureState() with { Index = (CpuZ80Index)3 }));
    }
}
