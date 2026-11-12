using Circuit = RetroMediaLab.Chips.Tests.CPU.Common.Z80.Z80TestCircuit;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Contracts;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Enums;

namespace RetroMediaLab.Chips.Tests.CPU.Zilog.Z80.Z80;

public sealed partial class ExecutionTests
{
    [Fact]
    public void IrTransfersPreserveOrSetFlagsUsingIff2AndCountBothM1()
    {
        foreach (byte opcode in new byte[] { 0x47, 0x4f, 0x57, 0x5f })
        foreach (byte value in new byte[] { 0, 1, 0x28, 0x7e, 0x7f, 0x80, 0xfe, 0xff })
        foreach (byte oldFlags in new byte[] { 0, 0xff })
        foreach (bool iff2 in new[] { false, true })
        {
            var initial = Initial(); initial.A = value; initial.I = value; initial.R = value; initial.F = oldFlags;
            var control = new Z80InterruptState(false, iff2, Z80InterruptMode.Mode2, 0, false, false, false);
            using var circuit = new Circuit([0xed, opcode], initial, control);
            circuit.RunInstructions(1);
            var r = circuit.Cpu.Registers;
            byte fetchedR = (byte)((value & 0x80) | ((value + 2) % 128));
            byte nextR = (byte)((value & 0x80) | ((value + (opcode == 0x4f ? 1 : 3)) % 128));
            Assert.Equal(nextR, r.R);
            Assert.Equal(value, r.I);
            if (opcode is 0x47 or 0x4f)
            {
                Assert.Equal(value, r.A); Assert.Equal(oldFlags, r.F);
            }
            else
            {
                byte expected = opcode == 0x57 ? value : fetchedR;
                byte flags = (byte)((expected & 0xa8) | (expected == 0 ? 0x40 : 0)
                    | (iff2 ? 4 : 0) | (oldFlags & 1));
                Assert.Equal(expected, r.A); Assert.Equal(flags, r.F);
            }
            Assert.Equal(control, circuit.Cpu.InterruptState);
            Assert.Equal(9 * 250L, circuit.Cpu.TimeNanoseconds);
            Assert.Equal(new ushort[] { 0, 1 }, circuit.Reads.Select(read => read.Address));
            Assert.Empty(circuit.MemoryWrites);
        }
        // Le I chargé doit affecter le rafraîchissement suivant, pas les deux M1 déjà effectués.
        var start = Initial(); start.I = 0x12; start.A = 0x34;
        using var refresh = new Circuit([0xed, 0x47, 0], start);
        refresh.RunInstructions(2);
        Assert.Equal(new ushort[] { 0x1280, 0x1281, 0x3482 }, refresh.Pins
            .Where(p => !p.Value.RefreshHigh && p.Value.MemoryRequestHigh == false)
            .Select(p => p.Value.Address!.Value));
    }

    [Fact]
    public void RegisterPortInputsUseTheOldBcAddressAndAllEightFlagAndDestinationEncodings()
    {
        for (int register = 0; register < 8; register++)
        for (int value = 0; value < 256; value++)
        foreach (byte carry in new byte[] { 0, 1 })
        {
            var initial = Initial(); initial.BC = 0x1234; initial.DE = 0x5678; initial.HL = 0x9abc;
            initial.F = (byte)(0xfe | carry);
            using var circuit = new Circuit([0xed, (byte)(0x40 + register * 8)], initial);
            circuit.PortInputData = (byte)value;
            circuit.RunInstructions(1);
            var r = circuit.Cpu.Registers;
            var expected = ReferenceAlu(6, 0, (byte)value, 0);
            Assert.Equal((byte)(expected.Flags | carry), r.F);
            if (register != 6)
                Assert.Equal((byte)value, register switch { 0 => r.B, 1 => r.C, 2 => r.D, 3 => r.E, 4 => r.H, 5 => r.L, _ => r.A });
            if (register != 0) Assert.Equal(initial.B, r.B);
            if (register != 1) Assert.Equal(initial.C, r.C);
            if (register != 2) Assert.Equal(initial.D, r.D);
            if (register != 3) Assert.Equal(initial.E, r.E);
            if (register != 4) Assert.Equal(initial.H, r.H);
            if (register != 5) Assert.Equal(initial.L, r.L);
            if (register != 7) Assert.Equal(initial.A, r.A);
            Assert.Equal(initial.BC, Assert.Single(circuit.Reads, read => read.Io).Address);
            Assert.Equal(12 * 250L, circuit.Cpu.TimeNanoseconds);
            Assert.Empty(circuit.MemoryWrites); Assert.Empty(circuit.PortWrites);
        }
    }

    [Fact]
    public void RegisterPortOutputsPresentEveryDocumentedRegisterAndPreserveFlags()
    {
        foreach (byte register in new byte[] { 0, 1, 2, 3, 4, 5, 7 })
        {
            var initial = Initial(); initial.BC = 0x1234; initial.DE = 0x5678; initial.HL = 0x9abc;
            initial.A = 0xde; initial.F = 0xff;
            byte value = register switch { 0 => initial.B, 1 => initial.C, 2 => initial.D, 3 => initial.E,
                4 => initial.H, 5 => initial.L, _ => initial.A };
            using var circuit = new Circuit([0xed, (byte)(0x41 + register * 8)], initial);
            circuit.RunInstructions(1);
            var write = Assert.Single(circuit.PortWrites);
            Assert.Equal(initial.BC, write.Address); Assert.Equal(value, write.Value);
            Assert.Equal(initial.AF, circuit.Cpu.Registers.AF); Assert.Equal(initial.BC, circuit.Cpu.Registers.BC);
            Assert.Equal(12 * 250L, circuit.Cpu.TimeNanoseconds); Assert.Empty(circuit.MemoryWrites);
        }
    }

    [Fact]
    public void DigitRotationsPermuteThreeDigitsPreserveTheUpperAccumulatorAndUseEighteenClocks()
    {
        foreach (bool left in new[] { false, true })
        foreach (byte a in new byte[] { 0, 1, 0x0f, 0x28, 0x70, 0x7a, 0x80, 0xa5, 0xff })
        foreach (byte memory in new byte[] { 0, 1, 0x0f, 0x10, 0x28, 0x31, 0x7f, 0x80, 0xf0, 0xff })
        foreach (byte oldFlags in new byte[] { 0, 0xff })
        {
            var initial = Initial(); initial.A = a; initial.F = oldFlags; initial.HL = 0x8000;
            using var circuit = new Circuit([0xed, left ? (byte)0x6f : (byte)0x67], initial);
            circuit.Memory[0x8000] = memory; circuit.RunInstructions(1);
            // Oracle par permutation de chiffres hexadécimaux, sans appeler le calcul de production.
            int aLow = a % 16, memHigh = memory / 16, memLow = memory % 16;
            byte expectedA = (byte)(a / 16 * 16 + (left ? memHigh : memLow));
            byte expectedMemory = (byte)(left ? memLow * 16 + aLow : aLow * 16 + memHigh);
            byte expectedFlags = (byte)(ReferenceAlu(6, 0, expectedA, 0).Flags | (oldFlags & 1));
            Assert.Equal(expectedA, circuit.Cpu.Registers.A); Assert.Equal(expectedFlags, circuit.Cpu.Registers.F);
            Assert.Equal(expectedMemory, circuit.Memory[0x8000]);
            Assert.Equal((ushort)0x8000, Assert.Single(circuit.MemoryWrites).Address);
            Assert.Equal(new ushort[] { 0, 1, 0x8000 }, circuit.Reads.Select(read => read.Address));
            Assert.Equal(18 * 250L, circuit.Cpu.TimeNanoseconds);
        }
    }

    [Fact]
    public void EdSpecialAndIoOperationsResumeAtEveryHalfClockAndDoNotInheritDdIndexing()
    {
        (byte[] Program, int Clocks)[] programs = [([0xed, 0x47], 9), ([0xed, 0x4f], 9),
            ([0xed, 0x57], 9), ([0xed, 0x5f], 9), ([0xed, 0x40], 12), ([0xed, 0x79], 12),
            ([0xed, 0x67], 18), ([0xed, 0x6f], 18), ([0xdd, 0xed, 0x6f], 22), ([0xfd, 0xed, 0x60], 16)];
        foreach (var (program, clocks) in programs)
        for (long time = 0; time < clocks * 250L; time += 125)
        {
            var initial = Initial(); initial.A = 0x28; initial.HL = 0x8000; initial.IX = 0x9000; initial.IY = 0xa000;
            using var original = new Circuit(program, initial); original.Memory[0x8000] = 0x31;
            original.AdvanceTo(time);
            using var restored = new Circuit(original.Memory, restore: original.Cpu.CaptureState());
            original.ClearTraces(); original.RunInstructions(1); restored.RunInstructions(1);
            Assert.Equal(clocks * 250L, original.Cpu.TimeNanoseconds);
            Assert.Equal(initial.IX, original.Cpu.Registers.IX); Assert.Equal(initial.IY, original.Cpu.Registers.IY);
            Assert.Equal(original.Memory, restored.Memory); Assert.Equal(original.Pins, restored.Pins);
            Assert.Equal(original.MemoryWrites, restored.MemoryWrites); Assert.Equal(original.PortWrites, restored.PortWrites);
            AssertCpuState(original.Cpu.CaptureState(), restored.Cpu.CaptureState());
        }
    }

    [Fact]
    public void RegisterInputWaitResumesWithTheFinalInputSampleAndSingleTransfer()
    {
        var initial = Initial(); initial.BC = 0x1234;
        using var original = new Circuit([0xed, 0x40], initial);
        original.PortInputData = 0x11; original.AdvanceTo(2000); original.Cpu.SetWait(true); original.AdvanceTo(3000);
        using var restored = new Circuit(original.Memory, restore: original.Cpu.CaptureState());
        original.ClearTraces();
        foreach (var circuit in new[] { original, restored })
        {
            circuit.Cpu.SetData(0x80); circuit.Cpu.SetWait(false); circuit.RunInstructions(1);
            Assert.Equal((byte)0x80, circuit.Cpu.Registers.B);
            Assert.Equal(14 * 250L, circuit.Cpu.TimeNanoseconds);
        }
        Assert.Equal(original.Pins, restored.Pins); AssertCpuState(original.Cpu.CaptureState(), restored.Cpu.CaptureState());
    }
}
