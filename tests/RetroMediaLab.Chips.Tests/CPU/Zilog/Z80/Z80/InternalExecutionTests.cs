using Circuit = RetroMediaLab.Chips.Tests.CPU.Common.Z80.Z80TestCircuit;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Enums;

namespace RetroMediaLab.Chips.Tests.CPU.Zilog.Z80.Z80;

public sealed partial class ExecutionTests
{
    [Fact]
    public void RefreshUsesTheCurrentCounterBeforeIncrementAcrossPrefixedM1AndWrap()
    {
        var initial = Initial(); initial.I = 0x12; initial.R = 0xfe;
        using var circuit = new Circuit([0xcb, 0x00, 0x00], initial);
        circuit.RunInstructions(2);
        ushort[] addresses = circuit.Pins.Where(p => p.Value.RefreshHigh == false
                && p.Value.MemoryRequestHigh == false)
            .Select(p => p.Value.Address!.Value).ToArray();
        Assert.Equal(new ushort[] { 0x12fe, 0x12ff, 0x1280 }, addresses);
        Assert.Equal((byte)0x82, circuit.Cpu.Registers.R); // Trois M1 et début du suivant.
        Assert.Equal(12 * 250L, circuit.Cpu.TimeNanoseconds);
        var zero = Initial(); zero.R = 0;
        using var first = new Circuit([0x00], zero);
        first.AdvanceTo(750);
        Assert.Equal((ushort)0, first.Cpu.Pins.Address);
    }

    [Fact]
    public void PairIncrementDecrementAndStackPointerLoadUseSixClocksWithoutExtraReads()
    {
        for (int pair = 0; pair < 4; pair++)
        foreach (bool decrement in new[] { false, true })
        foreach (ushort value in new ushort[] { 0, 1, 0x7fff, 0x8000, 0xffff })
        {
            var initial = Initial(); initial.F = 0xff;
            switch (pair)
            {
                case 0: initial.BC = value; break; case 1: initial.DE = value; break;
                case 2: initial.HL = value; break; case 3: initial.SP = value; break;
            }
            using var circuit = new Circuit([(byte)((decrement ? 0x0b : 0x03) + pair * 16)], initial);
            circuit.RunInstructions(1);
            var r = circuit.Cpu.Registers;
            Assert.Equal(unchecked((ushort)(value + (decrement ? -1 : 1))),
                pair switch { 0 => r.BC, 1 => r.DE, 2 => r.HL, _ => r.SP });
            Assert.Equal((byte)0xff, r.F);
            Assert.Equal(6 * 250L, circuit.Cpu.TimeNanoseconds);
            Assert.Single(circuit.Reads); Assert.Empty(circuit.MemoryWrites);
        }
        var start = Initial(); start.HL = 0x28fe;
        using var load = new Circuit([0xf9], start);
        load.RunInstructions(1);
        Assert.Equal(start.HL, load.Cpu.Registers.SP);
        Assert.Equal(start.F, load.Cpu.Registers.F);
        Assert.Equal(6 * 250L, load.Cpu.TimeNanoseconds);
    }

    [Fact]
    public void PairArithmeticUsesAllSourcesAndCompleteFlagsAtSignedAndCarryBoundaries()
    {
        for (int operation = 0; operation < 3; operation++) // ADD HL,dd ; ADC HL,dd ; SBC HL,dd
        for (int pair = 0; pair < 4; pair++)
        foreach (ushort left in new ushort[] { 0, 0x0fff, 0x7fff, 0x8000, 0xffff })
        foreach (ushort right in new ushort[] { 1, 0x1000, 0x8000, 0xffff })
        foreach (byte oldFlags in new byte[] { 0, 0xff })
        {
            var initial = Initial(); initial.HL = left; initial.F = oldFlags;
            switch (pair) { case 0: initial.BC = right; break; case 1: initial.DE = right; break; case 3: initial.SP = right; break; }
            ushort operand = pair == 2 ? left : right;
            byte opcode = (byte)((operation == 0 ? 0x09 : operation == 1 ? 0x4a : 0x42) + pair * 16);
            using var circuit = new Circuit(operation == 0 ? [opcode] : [0xed, opcode], initial);
            circuit.RunInstructions(1);
            int carry = operation == 0 ? 0 : oldFlags & 1;
            bool subtraction = operation == 2;
            int wide = subtraction ? left - operand - carry : left + operand + carry;
            ushort result = unchecked((ushort)wide);
            int flags = ((result / 256) & 0x28) | (wide < 0 || wide > 65535 ? 1 : 0)
                | (subtraction ? 2 : 0);
            int nibble = subtraction ? left % 4096 - operand % 4096 - carry : left % 4096 + operand % 4096 + carry;
            if (nibble < 0 || nibble > 4095) flags |= 0x10;
            if (operation == 0) flags |= oldFlags & 0xc4;
            else
            {
                flags |= (result / 256) & 0x80;
                if (result == 0) flags |= 0x40;
                int signedLeft = left < 32768 ? left : left - 65536, signedRight = operand < 32768 ? operand : operand - 65536;
                int signedResult = subtraction ? signedLeft - signedRight - carry : signedLeft + signedRight + carry;
                if (signedResult < -32768 || signedResult > 32767) flags |= 4;
            }
            Assert.Equal(result, circuit.Cpu.Registers.HL);
            Assert.Equal((byte)flags, circuit.Cpu.Registers.F);
            Assert.Equal((operation == 0 ? 11 : 15) * 250L, circuit.Cpu.TimeNanoseconds);
            Assert.Equal(operation == 0 ? 1 : 2, circuit.Reads.Count);
            Assert.Empty(circuit.MemoryWrites);
        }
    }

    [Fact]
    public void PushCallRestartAndConditionalReturnPreserveStackByteOrderAndWrap()
    {
        for (int pair = 0; pair < 4; pair++)
        {
            var initial = Initial(); initial.PC = 0x2000; initial.SP = 1;
            initial.BC = 0x1234; initial.DE = 0x5678; initial.HL = 0x9abc; initial.AF = 0xdeff;
            using var circuit = new Circuit([(byte)(0xc5 + pair * 16)], initial);
            circuit.RunInstructions(1);
            ushort word = pair switch { 0 => initial.BC, 1 => initial.DE, 2 => initial.HL, _ => initial.AF };
            Assert.Equal(new ushort[] { 0, 0xffff }, circuit.MemoryWrites.Select(w => w.Address));
            Assert.Equal(new byte[] { (byte)(word >> 8), (byte)word }, circuit.MemoryWrites.Select(w => w.Value));
            Assert.Equal((ushort)0xffff, circuit.Cpu.Registers.SP);
            Assert.Equal(initial.AF, circuit.Cpu.Registers.AF);
            Assert.Equal(11 * 250L, circuit.Cpu.TimeNanoseconds);
        }
        for (int vector = 0; vector < 8; vector++)
        {
            var initial = Initial(); initial.PC = 0x2000; initial.SP = 1;
            using var circuit = new Circuit([(byte)(0xc7 + vector * 8)], initial);
            circuit.RunInstructions(1);
            Assert.Equal((ushort)(vector * 8), circuit.Cpu.InstructionAddress);
            Assert.Equal(new byte[] { 0x20, 0x01 }, circuit.MemoryWrites.Select(w => w.Value));
            Assert.Equal(new ushort[] { 0, 0xffff }, circuit.MemoryWrites.Select(w => w.Address));
            Assert.Equal(11 * 250L, circuit.Cpu.TimeNanoseconds);
        }
        for (int condition = 0; condition < 8; condition++)
        foreach (byte flags in new byte[] { 0, 0xff, 1, 4, 0x40, 0x80 })
        foreach (bool returning in new[] { false, true })
        {
            bool flag = (flags & ((condition / 2) switch { 0 => 0x40, 1 => 1, 2 => 4, _ => 0x80 })) != 0;
            bool taken = condition % 2 == 1 ? flag : !flag;
            var initial = Initial(); initial.F = flags; initial.PC = 0x2000; initial.SP = 0x8000;
            byte opcode = (byte)((returning ? 0xc0 : 0xc4) + condition * 8);
            using var circuit = new Circuit(returning ? [opcode] : [opcode, 0x34, 0x12], initial);
            circuit.Memory[0x8000] = 0x34; circuit.Memory[0x8001] = 0x12;
            circuit.RunInstructions(1);
            Assert.Equal(taken ? (ushort)0x1234 : returning ? (ushort)0x2001 : (ushort)0x2003, circuit.Cpu.InstructionAddress);
            Assert.Equal(flags, circuit.Cpu.Registers.F);
            Assert.Equal(returning ? (taken ? 11 : 5) * 250L : (taken ? 17 : 10) * 250L, circuit.Cpu.TimeNanoseconds);
            Assert.Equal((ushort)(0x8000 + (taken ? returning ? 2 : -2 : 0)), circuit.Cpu.Registers.SP);
            Assert.Equal(returning ? taken ? 3 : 1 : 3, circuit.Reads.Count);
            if (!returning && taken)
                Assert.Equal(new byte[] { 0x20, 0x03 }, circuit.MemoryWrites.Select(w => w.Value));
            else Assert.Empty(circuit.MemoryWrites);
        }
        using var call = new Circuit([0xcd, 0x34, 0x12]);
        call.RunInstructions(1);
        Assert.Equal((ushort)0x1234, call.Cpu.InstructionAddress);
        Assert.Equal(17 * 250L, call.Cpu.TimeNanoseconds);
    }

    [Fact]
    public void RelativeBranchesReadSignedDisplacementsAndDjnzPreservesFlags()
    {
        foreach (byte opcode in new byte[] { 0x10, 0x18, 0x20, 0x28, 0x30, 0x38 })
        foreach (byte displacement in new byte[] { 0, 1, 0x7f, 0x80, 0xff })
        foreach (byte flags in new byte[] { 0, 0xff })
        foreach (byte b in new byte[] { 0, 1, 2 })
        {
            var initial = Initial(); initial.PC = 0xfffe; initial.F = flags; initial.B = b;
            using var circuit = new Circuit([opcode, displacement], initial);
            circuit.RunInstructions(1);
            bool taken = opcode switch { 0x10 => b != 1, 0x18 => true, 0x20 => (flags & 0x40) == 0,
                0x28 => (flags & 0x40) != 0, 0x30 => (flags & 1) == 0, _ => (flags & 1) != 0 };
            int relative = displacement < 128 ? displacement : displacement - 256;
            Assert.Equal(unchecked((ushort)(taken ? relative : 0)), circuit.Cpu.InstructionAddress);
            Assert.Equal(opcode == 0x10 ? unchecked((byte)(b - 1)) : b, circuit.Cpu.Registers.B);
            Assert.Equal(flags, circuit.Cpu.Registers.F);
            Assert.Equal(((opcode == 0x10 ? 8 : 7) + (taken ? 5 : 0)) * 250L, circuit.Cpu.TimeNanoseconds);
            Assert.Equal(new ushort[] { 0xfffe, 0xffff }, circuit.Reads.Select(r => r.Address));
        }
    }

    [Fact]
    public void ReadModifyWriteMemoryHasOneReadOneWriteAndItsInternalClock()
    {
        foreach (byte value in new byte[] { 0, 1, 0x0f, 0x7f, 0x80, 0xff })
        foreach (byte opcode in new byte[] { 0x34, 0x35, 0x06, 0x0e, 0x16, 0x1e, 0x26, 0x2e, 0x36, 0x3e,
            0x86, 0x8e, 0x96, 0x9e, 0xa6, 0xae, 0xb6, 0xbe, 0xc6, 0xce, 0xd6, 0xde, 0xe6, 0xee, 0xf6, 0xfe })
        {
            bool cb = opcode != 0x34 && opcode != 0x35;
            var initial = Initial(); initial.HL = 0x8000; initial.F = 1;
            using var circuit = new Circuit(cb ? [0xcb, opcode] : [opcode], initial);
            circuit.Memory[0x8000] = value;
            circuit.RunInstructions(1);
            byte expected;
            if (!cb) expected = unchecked((byte)(value + (opcode == 0x34 ? 1 : -1)));
            else
            {
                int bit = opcode / 8 % 8;
                expected = opcode >= 0xc0 ? (byte)(value | (1 << bit))
                    : opcode >= 0x80 ? (byte)(value & ~(1 << bit))
                    : unchecked((byte)(bit switch { 0 => value * 2 + value / 128, 1 => value / 2 + value % 2 * 128,
                        2 => value * 2 + 1, 3 => value / 2 + 128, 4 => value * 2,
                        5 => value / 2 + value / 128 * 128, 6 => value * 2 + 1, _ => value / 2 }));
            }
            var write = Assert.Single(circuit.MemoryWrites);
            Assert.Equal((ushort)0x8000, write.Address); Assert.Equal(expected, write.Value);
            Assert.Equal(expected, circuit.Memory[0x8000]);
            Assert.Equal(cb ? 3 : 2, circuit.Reads.Count);
            Assert.Equal((cb ? 15 : 11) * 250L, circuit.Cpu.TimeNanoseconds);
            // Début d'écriture après l'état interne : T9 (base) ou T13 (CB), WR descendant T10/T14.
            Assert.Equal((cb ? 14 : 10) * 250L + 125 + 80, write.Time);
            if (cb && opcode >= 0x80) Assert.Equal(initial.F, circuit.Cpu.Registers.F);
        }
    }

    [Fact]
    public void InternalClocksKeepTheBusInactiveAndWaitDoesNotStretchThem()
    {
        using var circuit = new Circuit([0x03]);
        circuit.AdvanceTo(1130); // Rafraîchissement déjà désactivé, T5 interne.
        Assert.Equal(CpuZ80Flow.InternalOperation, circuit.Cpu.CaptureState().Flow);
        Assert.Equal(2, circuit.Cpu.CaptureState().InternalClocksRemaining);
        Assert.True(circuit.Cpu.Pins.MemoryRequestHigh);
        Assert.True(circuit.Cpu.Pins.IoRequestHigh);
        Assert.True(circuit.Cpu.Pins.ReadHigh); Assert.True(circuit.Cpu.Pins.WriteHigh);
        Assert.True(circuit.Cpu.Pins.RefreshHigh);
        circuit.Cpu.SetWait(true);
        circuit.RunInstructions(1);
        Assert.Equal(6 * 250L, circuit.Cpu.TimeNanoseconds);
        Assert.Equal((ushort)1, circuit.Cpu.Registers.BC);
    }

    [Fact]
    public void InternalOperationsAndStackWritesResumeAtEveryHalfClock()
    {
        (byte[] Program, int Clocks)[] programs = [([0x03], 6), ([0x09], 11), ([0xed, 0x4a], 15),
            ([0xc5], 11), ([0xcd, 0x34, 0x12], 17), ([0xc7], 11), ([0xc0], 11),
            ([0x18, 0xfe], 12), ([0x10, 0xfe], 13), ([0x34], 11), ([0xcb, 0x06], 15)];
        foreach (var (program, clocks) in programs)
        for (long time = 0; time < clocks * 250L; time += 125)
        {
            var initial = Initial(); initial.BC = 0x0200; initial.HL = 0x8000; initial.F = 0;
            using var original = new Circuit(program, initial);
            original.Memory[0x8000] = 0x7f;
            original.Memory[initial.SP] = 0x34; original.Memory[initial.SP + 1] = 0x12;
            original.AdvanceTo(time);
            using var restored = new Circuit(original.Memory, restore: original.Cpu.CaptureState());
            original.ClearTraces();
            original.RunInstructions(1); restored.RunInstructions(1);
            Assert.Equal(clocks * 250L, original.Cpu.TimeNanoseconds);
            Assert.Equal(original.Memory, restored.Memory);
            Assert.Equal(original.MemoryWrites, restored.MemoryWrites);
            Assert.Equal(original.Pins, restored.Pins);
            AssertCpuState(original.Cpu.CaptureState(), restored.Cpu.CaptureState());
        }
        using var invalid = new Circuit([0x03]); invalid.AdvanceTo(1250);
        var snapshot = invalid.Cpu.CaptureState();
        Assert.Throws<ArgumentException>(() => invalid.Cpu.RestoreState(snapshot with { InternalClocksRemaining = 0 }));
        AssertCpuState(snapshot, invalid.Cpu.CaptureState());
    }
}
