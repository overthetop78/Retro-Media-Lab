using Circuit = RetroMediaLab.Chips.Tests.CPU.Common.Z80.Z80TestCircuit;
using RetroMediaLab.Chips.CPU.NEC.UPD780C.UPD780C1.Models;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Contracts;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Enums;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Models;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Services;

namespace RetroMediaLab.Chips.Tests.CPU.Zilog.Z80.Z80;

/// <summary>Programmes synthétiques, mémoire et ports numériques du banc. Le CPU n'accède
/// qu'aux broches ; aucun fichier de ROM, fenêtre ou processus externe n'est utilisé.</summary>
public sealed partial class ExecutionTests
{
    [Fact]
    public void TransfersExecuteThroughTheBusWithPublishedInstructionTimings()
    {
        byte[] program = [0x31, 0x00, 0xa0, 0x01, 0x00, 0x80, 0x11, 0x01, 0x80, 0x21, 0x02, 0x80,
            0x3e, 0x11, 0x02, 0x12, 0x36, 0x22, 0x46, 0x70, 0x1a, 0x32, 0x00, 0x90,
            0x21, 0x34, 0x12, 0x22, 0x01, 0x90, 0x2a, 0x01, 0x90, 0x3a, 0x00, 0x90, 0x00];
        using var circuit = new Circuit(program);
        circuit.RunInstructions(17);
        Assert.Equal(164 * 250L, circuit.Cpu.TimeNanoseconds);
        Assert.Equal(new ushort[] { 0x8000, 0x8001, 0x8002, 0x8002, 0x9000, 0x9001, 0x9002 },
            circuit.MemoryWrites.Select(write => write.Address));
        Assert.Equal(new byte[] { 0x11, 0x11, 0x22, 0x22, 0x11, 0x34, 0x12 },
            circuit.MemoryWrites.Select(write => write.Value));
        Assert.Equal((ushort)0x1234, circuit.Cpu.Registers.HL);
        Assert.Equal((ushort)0x2200, circuit.Cpu.Registers.BC);
        Assert.Equal((ushort)0xa000, circuit.Cpu.Registers.SP);
        Assert.Equal((byte)0x11, circuit.Cpu.Registers.A);
        Assert.Equal((byte)0x45, circuit.Cpu.Registers.F);
        Assert.Equal((byte)0x92, circuit.Cpu.Registers.R); // 17 instructions et début du fetch suivant.
        Assert.True(circuit.Cpu.AtInstructionBoundary);
        Assert.Equal((ushort)program.Length, circuit.Cpu.InstructionAddress);
    }

    [Fact]
    public void EdWordTransfersAndExchangesPreserveBanksAndByteOrder()
    {
        byte[] program = [0x08, 0xd9, 0xeb, 0xed, 0x43, 0x00, 0x90, 0xed, 0x53, 0x02, 0x90,
            0xed, 0x63, 0x04, 0x90, 0xed, 0x73, 0x06, 0x90, 0xed, 0x4b, 0x02, 0x90,
            0xed, 0x5b, 0x04, 0x90, 0xed, 0x6b, 0x00, 0x90, 0xed, 0x7b, 0x06, 0x90, 0x00];
        var initial = Initial();
        initial.BC = 0x1234; initial.DE = 0x5678; initial.HL = 0x9abc;
        initial.AlternateAF = 0xab80; initial.AlternateBC = 0x1111;
        initial.AlternateDE = 0x2222; initial.AlternateHL = 0x3333;
        using var circuit = new Circuit(program, initial);
        circuit.RunInstructions(12);
        Assert.Equal(176 * 250L, circuit.Cpu.TimeNanoseconds);
        var r = circuit.Cpu.Registers;
        Assert.Equal((ushort)0xab80, r.AF); Assert.Equal(initial.AF, r.AlternateAF);
        Assert.Equal((ushort)0x3333, r.BC); Assert.Equal((ushort)0x2222, r.DE); Assert.Equal((ushort)0x1111, r.HL);
        Assert.Equal(initial.BC, r.AlternateBC); Assert.Equal(initial.DE, r.AlternateDE); Assert.Equal(initial.HL, r.AlternateHL);
        Assert.Equal(initial.SP, r.SP);
        Assert.Equal(new byte[] { 0x11, 0x11, 0x33, 0x33, 0x22, 0x22, 0x00, 0xa0 }, circuit.Memory[0x9000..0x9008]);
        Assert.Equal((byte)0x95, r.R); // 21 fetchs M1 ; opérandes et données n'incrémentent pas R.
    }

    [Theory]
    [InlineData(0x00, 0x55)]
    [InlineData(0xff, 0xaa)]
    [InlineData(0x40, 0x56)]
    [InlineData(0x01, 0x59)]
    [InlineData(0x04, 0x65)]
    [InlineData(0x80, 0x95)]
    public void EveryJumpConditionReadsBothOperandsEvenWhenNotTaken(int flags, int takenConditions)
    {
        for (int condition = 0; condition < 8; condition++)
        {
            var initial = Initial(); initial.F = (byte)flags;
            using var circuit = new Circuit([(byte)(0xc2 + condition * 8), 0x00, 0x20], initial);
            circuit.RunInstructions(1);
            ushort expected = (takenConditions & (1 << condition)) != 0 ? (ushort)0x2000 : (ushort)3;
            Assert.Equal(expected, circuit.Cpu.InstructionAddress);
            Assert.Equal(10 * 250L, circuit.Cpu.TimeNanoseconds);
            Assert.Equal(new ushort[] { 0, 1, 2 }, circuit.Reads.Select(read => read.Address));
            Assert.Equal((byte)flags, circuit.Cpu.Registers.F);
        }
    }

    [Fact]
    public void PopAndReturnsUseLowByteFirstAndWrapTheStackAddress()
    {
        for (int pair = 0; pair < 4; pair++)
        {
            var initial = Initial(); initial.PC = 0x1000; initial.SP = 0xffff;
            using var circuit = new Circuit([(byte)(0xc1 + pair * 16)], initial);
            circuit.Memory[0xffff] = 0x55; circuit.Memory[0] = 0xaa;
            circuit.RunInstructions(1);
            var r = circuit.Cpu.Registers;
            ushort actual = pair switch { 0 => r.BC, 1 => r.DE, 2 => r.HL, _ => r.AF };
            Assert.Equal((ushort)0xaa55, actual);
            Assert.Equal((ushort)1, r.SP);
            Assert.Equal(10 * 250L, circuit.Cpu.TimeNanoseconds);
            Assert.Equal(new ushort[] { 0x1000, 0xffff, 0 }, circuit.Reads.Select(read => read.Address));
        }
        foreach (bool retn in new[] { false, true })
        {
            var initial = Initial(); initial.SP = 0x8000;
            var control = new Z80InterruptState(false, true, Z80InterruptMode.Mode1, 0, false, false, false);
            using var circuit = new Circuit(retn ? [0xed, 0x45] : [0xc9], initial, control);
            circuit.Memory[0x8000] = 0x34; circuit.Memory[0x8001] = 0x12;
            circuit.RunInstructions(1);
            Assert.Equal((ushort)0x1234, circuit.Cpu.InstructionAddress);
            Assert.Equal((ushort)0x8002, circuit.Cpu.Registers.SP);
            Assert.Equal((retn ? 14 : 10) * 250L, circuit.Cpu.TimeNanoseconds);
            Assert.Equal(retn, circuit.Cpu.InterruptState.Iff1);
            Assert.True(circuit.Cpu.InterruptState.Iff2);
        }
    }

    [Fact]
    public void ImmediateIoPresentsTheAccumulatorInTheUpperAddressByte()
    {
        using var circuit = new Circuit([0x3e, 0x23, 0xd3, 0x01, 0xdb, 0x02]);
        circuit.RunInstructions(3);
        Assert.Equal(29 * 250L, circuit.Cpu.TimeNanoseconds);
        var write = Assert.Single(circuit.PortWrites);
        Assert.Equal((ushort)0x2301, write.Address); Assert.Equal((byte)0x23, write.Value);
        Assert.Equal((ushort)0x2302, Assert.Single(circuit.Reads, read => read.Io).Address);
        Assert.Equal((byte)0x7b, circuit.Cpu.Registers.A);
        Assert.Equal((byte)0x45, circuit.Cpu.Registers.F);
    }

    [Fact]
    public void EiDiAndModeInstructionsRetireOncePerWholeInstruction()
    {
        using var circuit = new Circuit([0xfb, 0xed, 0x5e, 0xf3, 0xed, 0x56, 0xed, 0x46]);
        circuit.RunInstructions(1);
        Assert.True(circuit.Cpu.InterruptState.Iff1);
        Assert.Equal((byte)1, circuit.Cpu.InterruptState.EiInhibitRemaining);
        circuit.RunInstructions(2);
        Assert.Equal(Z80InterruptMode.Mode2, circuit.Cpu.InterruptState.Mode);
        Assert.Equal((byte)0, circuit.Cpu.InterruptState.EiInhibitRemaining);
        circuit.RunInstructions(3);
        Assert.False(circuit.Cpu.InterruptState.Iff1); Assert.False(circuit.Cpu.InterruptState.Iff2);
        circuit.RunInstructions(4); Assert.Equal(Z80InterruptMode.Mode1, circuit.Cpu.InterruptState.Mode);
        circuit.RunInstructions(5); Assert.Equal(Z80InterruptMode.Mode0, circuit.Cpu.InterruptState.Mode);
        Assert.Equal(32 * 250L, circuit.Cpu.TimeNanoseconds);
    }

    [Fact]
    public void CpuResumesMidWaitWithTheSameDataAndTimings()
    {
        using var original = new Circuit([0x01, 0x34, 0x12]);
        original.Cpu.SetWait(true); original.AdvanceTo(500);
        using var restored = new Circuit(original.Memory, restore: original.Cpu.CaptureState());
        original.ClearTraces();
        foreach (var circuit in new[] { original, restored })
        {
            circuit.AdvanceTo(501); circuit.Cpu.SetWait(false); circuit.RunInstructions(1);
            Assert.Equal((ushort)0x1234, circuit.Cpu.Registers.BC);
            Assert.Equal(11 * 250L, circuit.Cpu.TimeNanoseconds);
        }
        Assert.Equal(original.Pins, restored.Pins);
        AssertCpuState(original.Cpu.CaptureState(), restored.Cpu.CaptureState());
    }

    [Fact]
    public void CpuAndOwnedMemoryResumeAcrossMultiByteReadsAndWrites()
    {
        byte[] program = [0x21, 0x34, 0x12, 0x22, 0x00, 0x90, 0x2a, 0x00, 0x90, 0x00];
        foreach (long time in new long[] { 0, 100, 375, 500, 875, 1100, 1375, 2250, 2750,
            4000, 4750, 5500, 5750, 6250, 6500, 6750, 7500, 8500, 9750, 10500, 11000 })
        {
            using var original = new Circuit(program);
            original.AdvanceTo(time);
            using var restored = new Circuit(original.Memory, restore: original.Cpu.CaptureState());
            original.ClearTraces();
            original.RunInstructions(4); restored.RunInstructions(4);
            Assert.Equal(46 * 250L, original.Cpu.TimeNanoseconds);
            Assert.Equal(original.Memory, restored.Memory);
            Assert.Equal(original.Pins, restored.Pins);
            Assert.Equal(original.MemoryWrites, restored.MemoryWrites);
            AssertCpuState(original.Cpu.CaptureState(), restored.Cpu.CaptureState());
        }
    }

    [Fact]
    public void ArithmeticFamiliesExecuteEveryRegisterMemoryAndImmediateEncoding()
    {
        (byte A, byte Operand)[] boundaries = [(0, 0), (0, 1), (1, 0xff), (0x0f, 1),
            (0x7f, 1), (0x80, 1), (0x80, 0xff), (0xff, 1), (0x28, 0), (0, 0x28), (0x55, 0xaa)];
        for (int operation = 0; operation < 8; operation++)
        for (int form = 0; form < 9; form++) // B,C,D,E,H,L,(HL),A,n
        foreach (var (accumulator, operand) in boundaries)
        foreach (byte oldFlags in new byte[] { 0, 0xff })
        {
            var initial = Initial(); initial.HL = 0x8000; initial.A = accumulator; initial.F = oldFlags;
            switch (form)
            {
                case 0: initial.B = operand; break; case 1: initial.C = operand; break;
                case 2: initial.D = operand; break; case 3: initial.E = operand; break;
                case 4: initial.H = operand; break; case 5: initial.L = operand; break;
            }
            byte source = form == 7 ? accumulator : operand;
            byte opcode = (byte)(form == 8 ? 0xc6 + operation * 8 : 0x80 + operation * 8 + form);
            using var circuit = new Circuit(form == 8 ? [opcode, source] : [opcode], initial);
            if (form == 6) circuit.Memory[initial.HL] = source;
            circuit.RunInstructions(1);
            var expected = ReferenceAlu(operation, accumulator, source, oldFlags);
            Assert.Equal(expected.A, circuit.Cpu.Registers.A);
            Assert.Equal(expected.Flags, circuit.Cpu.Registers.F);
            Assert.Equal((form is 6 or 8 ? 7 : 4) * 250L, circuit.Cpu.TimeNanoseconds);
            Assert.Empty(circuit.MemoryWrites);
            Assert.Equal(form == 8 ? (ushort)2 : (ushort)1, circuit.Cpu.InstructionAddress);
            Assert.Equal(form is 6 or 8 ? 2 : 1, circuit.Reads.Count);
            if (form == 6) Assert.Equal(initial.HL, circuit.Reads[1].Address);
        }
    }

    [Fact]
    public void RegisterIncrementDecrementPreservesCarryAndHandlesSignedAndNibbleBoundaries()
    {
        foreach (byte code in new byte[] { 0, 1, 2, 3, 4, 5, 7 })
        foreach (bool decrement in new[] { false, true })
        foreach (byte value in new byte[] { 0, 1, 0x0f, 0x10, 0x27, 0x28, 0x7f, 0x80, 0xfe, 0xff })
        foreach (byte oldFlags in new byte[] { 0, 0xff })
        {
            var initial = Initial(); initial.F = oldFlags;
            switch (code)
            {
                case 0: initial.B = value; break; case 1: initial.C = value; break;
                case 2: initial.D = value; break; case 3: initial.E = value; break;
                case 4: initial.H = value; break; case 5: initial.L = value; break;
                case 7: initial.A = value; break;
            }
            using var circuit = new Circuit([(byte)(4 + code * 8 + (decrement ? 1 : 0))], initial);
            circuit.RunInstructions(1);
            byte expected = unchecked((byte)(value + (decrement ? -1 : 1)));
            byte flags = (byte)((expected & 0xa8) | (expected == 0 ? 0x40 : 0) | (oldFlags & 1)
                | (decrement ? 2 : 0)
                | ((decrement ? value % 16 == 0 : value % 16 == 15) ? 0x10 : 0)
                | ((decrement ? value == 0x80 : value == 0x7f) ? 4 : 0));
            var r = circuit.Cpu.Registers;
            Assert.Equal(expected, code switch { 0 => r.B, 1 => r.C, 2 => r.D, 3 => r.E, 4 => r.H, 5 => r.L, _ => r.A });
            Assert.Equal(flags, r.F); Assert.Equal(4 * 250L, circuit.Cpu.TimeNanoseconds);
            Assert.Empty(circuit.MemoryWrites);
        }
    }

    [Fact]
    public void AccumulatorRotatesComplementAndNegateSetTheirSpecificFlags()
    {
        for (int value = 0; value < 256; value++)
        foreach (byte oldFlags in new byte[] { 0, 0xff })
        {
            foreach (byte opcode in new byte[] { 0x07, 0x0f, 0x17, 0x1f, 0x2f, 0x44 })
            {
                var initial = Initial(); initial.A = (byte)value; initial.F = oldFlags;
                using var circuit = new Circuit(opcode == 0x44 ? [0xed, opcode] : [opcode], initial);
                circuit.RunInstructions(1);
                int carry = opcode is 0x07 or 0x17 ? value / 128 : value % 2;
                int result = opcode switch
                {
                    0x07 => value * 2 + value / 128, 0x0f => value / 2 + (value % 2) * 128,
                    0x17 => value * 2 + (oldFlags & 1), 0x1f => value / 2 + (oldFlags & 1) * 128,
                    0x2f => 255 - value, _ => -value,
                };
                byte flags = opcode switch
                {
                    0x44 => ReferenceAlu(2, 0, (byte)value, oldFlags).Flags,
                    0x2f => (byte)((oldFlags & 0xc5) | 0x12 | (result & 0x28)),
                    _ => (byte)((oldFlags & 0xc4) | (result & 0x28) | carry),
                };
                Assert.Equal(unchecked((byte)result), circuit.Cpu.Registers.A);
                Assert.Equal(flags, circuit.Cpu.Registers.F);
                Assert.Equal((opcode == 0x44 ? 8 : 4) * 250L, circuit.Cpu.TimeNanoseconds);
            }
        }
    }

    [Fact]
    public void ArithmeticOperandReadsResumeWithIdenticalFlagsBusAndTiming()
    {
        for (int operation = 0; operation < 8; operation++)
        foreach (bool immediate in new[] { false, true })
        foreach (long time in new long[] { 1000, 1250, 1500, 1625 })
        {
            var initial = Initial(); initial.HL = 0x8000; initial.A = 0x7f; initial.F = 1;
            byte opcode = (byte)((immediate ? 0xc6 : 0x86) + operation * 8);
            using var original = new Circuit(immediate ? [opcode, 0x28] : [opcode], initial);
            original.Memory[0x8000] = 0x28;
            original.AdvanceTo(time);
            using var restored = new Circuit(original.Memory, restore: original.Cpu.CaptureState());
            original.ClearTraces();
            original.RunInstructions(1); restored.RunInstructions(1);
            Assert.Equal(original.Pins, restored.Pins);
            AssertCpuState(original.Cpu.CaptureState(), restored.Cpu.CaptureState());
            Assert.Equal(7 * 250L, original.Cpu.TimeNanoseconds);
        }
    }

    [Fact]
    public void CbRegisterRotatesShiftsSetAndResetCoverAllTheirEncodings()
    {
        int encodings = 0;
        for (int opcode = 0; opcode < 256; opcode++)
        {
            int register = opcode % 8, group = opcode / 64, bit = opcode / 8 % 8;
            if (register == 6 || group == 1) continue;
            encodings++;
            foreach (byte value in new byte[] { 0, 1, 0x08, 0x28, 0x55, 0x7f, 0x80, 0xaa, 0xfe, 0xff })
            foreach (byte oldFlags in new byte[] { 0, 0xff })
            {
                var initial = Initial(); initial.BC = initial.DE = initial.HL = 0x5aa5;
                initial.F = oldFlags;
                switch (register)
                {
                    case 0: initial.B = value; break; case 1: initial.C = value; break;
                    case 2: initial.D = value; break; case 3: initial.E = value; break;
                    case 4: initial.H = value; break; case 5: initial.L = value; break;
                    case 7: initial.A = value; break;
                }
                using var circuit = new Circuit([0xcb, (byte)opcode], initial);
                circuit.RunInstructions(1);
                int carry = bit is 0 or 2 or 4 or 6 ? value / 128 : value % 2;
                int result = group switch
                {
                    2 => value & (255 - (1 << bit)),
                    3 => value | (1 << bit),
                    _ => bit switch
                    {
                        0 => value * 2 + value / 128, 1 => value / 2 + value % 2 * 128,
                        2 => value * 2 + (oldFlags & 1), 3 => value / 2 + (oldFlags & 1) * 128,
                        4 => value * 2, 5 => value / 2 + value / 128 * 128, 6 => value * 2 + 1, _ => value / 2,
                    },
                };
                byte expected = unchecked((byte)result);
                int flags = oldFlags;
                if (group == 0)
                {
                    int ones = 0;
                    for (int position = 0; position < 8; position++) if ((expected & (1 << position)) != 0) ones++;
                    flags = (expected & 0xa8) | (expected == 0 ? 0x40 : 0) | (ones % 2 == 0 ? 4 : 0) | carry;
                }
                var r = circuit.Cpu.Registers;
                Assert.Equal(expected, register switch { 0 => r.B, 1 => r.C, 2 => r.D, 3 => r.E, 4 => r.H, 5 => r.L, _ => r.A });
                // Comparer également chaque registre non ciblé, sans exiger F inchangé pour une rotation.
                if (register != 0) Assert.Equal(initial.B, r.B);
                if (register != 1) Assert.Equal(initial.C, r.C);
                if (register != 2) Assert.Equal(initial.D, r.D);
                if (register != 3) Assert.Equal(initial.E, r.E);
                if (register != 4) Assert.Equal(initial.H, r.H);
                if (register != 5) Assert.Equal(initial.L, r.L);
                if (register != 7) Assert.Equal(initial.A, r.A);
                Assert.Equal((byte)flags, r.F);
                Assert.Equal(8 * 250L, circuit.Cpu.TimeNanoseconds);
                Assert.Equal((byte)0x83, r.R); // Deux M1 plus le départ du fetch suivant.
                Assert.Equal(new ushort[] { 0, 1 }, circuit.Reads.Select(read => read.Address));
                Assert.Empty(circuit.MemoryWrites);
            }
        }
        Assert.Equal(168, encodings);
    }

    [Fact]
    public void CbFetchResumesWithoutASeparateInstructionBoundaryOrLostEiDelay()
    {
        foreach (long time in new long[] { 0, 500, 875, 1000, 1250, 1875 })
        {
            var initial = Initial(); initial.B = 0x81;
            var control = new Z80InterruptState(true, true, Z80InterruptMode.Mode0, 1, false, false, false);
            using var original = new Circuit([0xcb, 0x00], initial, control);
            original.AdvanceTo(time);
            Assert.Equal(0, original.Cpu.InstructionsCompleted);
            Assert.Equal((byte)1, original.Cpu.InterruptState.EiInhibitRemaining);
            if (time >= 1000) Assert.False(original.Cpu.AtInstructionBoundary);
            using var restored = new Circuit(original.Memory, restore: original.Cpu.CaptureState());
            original.ClearTraces();
            original.RunInstructions(1); restored.RunInstructions(1);
            Assert.Equal((byte)3, original.Cpu.Registers.B);
            Assert.Equal((byte)0, original.Cpu.InterruptState.EiInhibitRemaining);
            Assert.Equal(original.Pins, restored.Pins);
            AssertCpuState(original.Cpu.CaptureState(), restored.Cpu.CaptureState());
        }
    }

    // Oracle mathématique indépendant : sommes signées, chiffres hexadécimaux
    // et comptage scalaire de parité, sans réutiliser les helpers de production.
    private static (byte A, byte Flags) ReferenceAlu(int operation, byte a, byte operand, byte oldFlags)
    {
        int inputCarry = operation is 1 or 3 ? oldFlags & 1 : 0;
        bool subtract = operation is 2 or 3 or 7;
        int wide = operation switch
        {
            0 or 1 => a + operand + inputCarry,
            2 or 3 or 7 => a - operand - inputCarry,
            4 => a & operand, 5 => a ^ operand, _ => a | operand,
        };
        byte result = unchecked((byte)wide);
        int flags = (result & 0x80) | (result == 0 ? 0x40 : 0)
            | ((operation == 7 ? operand : result) & 0x28);
        if (operation is 4 or 5 or 6)
        {
            int bits = 0;
            for (int bit = 0; bit < 8; bit++) if ((result & (1 << bit)) != 0) bits++;
            if (bits % 2 == 0) flags |= 4;
            if (operation == 4) flags |= 0x10;
        }
        else
        {
            int signedA = a < 128 ? a : a - 256, signedOperand = operand < 128 ? operand : operand - 256;
            int signedResult = subtract ? signedA - signedOperand - inputCarry : signedA + signedOperand + inputCarry;
            if (signedResult < -128 || signedResult > 127) flags |= 4;
            int digit = subtract ? a % 16 - operand % 16 - inputCarry : a % 16 + operand % 16 + inputCarry;
            if (digit < 0 || digit > 15) flags |= 0x10;
            if (wide < 0 || wide > 255) flags |= 1;
            if (subtract) flags |= 2;
        }
        return (operation == 7 ? a : result, (byte)flags);
    }

    [Fact]
    public void RestoreRejectsInvalidCpuOrBusStateBeforeReplacingRegisters()
    {
        using var circuit = new Circuit([0x21, 0x34, 0x12]); circuit.AdvanceTo(1375);
        var before = circuit.Cpu.CaptureState();
        Assert.Throws<ArgumentException>(() => circuit.Cpu.RestoreState(before with { FormatVersion = before.FormatVersion + 1 }));
        Assert.Throws<ArgumentException>(() => circuit.Cpu.RestoreState(before with { PairCode = 4 }));
        Assert.Throws<ArgumentException>(() => circuit.Cpu.RestoreState(before with
            { Bus = before.Bus with { Timing = before.Bus.Timing with { Address = 111 } } }));
        AssertCpuState(before, circuit.Cpu.CaptureState());
    }

    private static Z80Registers Initial() => Circuit.InitialRegisters();

    private static void AssertCpuState(CpuZ80State expected, CpuZ80State actual)
    {
        Assert.Equal(expected.Bus.Values, actual.Bus.Values);
        Assert.Equal(expected.Bus.Targets, actual.Bus.Targets);
        Assert.Equal(expected.Bus.Deadlines, actual.Bus.Deadlines);
        Assert.Equal(expected.Bus with { Values = actual.Bus.Values, Targets = actual.Bus.Targets,
            Deadlines = actual.Bus.Deadlines }, actual.Bus);
        Assert.Equal(expected with { Bus = actual.Bus }, actual);
    }

}
