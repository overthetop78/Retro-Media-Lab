using RetroMediaLab.Chips.CPU.Common.Interfaces;
using RetroMediaLab.Chips.CPU.Motorola.M6800.Chip6809.Services;

namespace RetroMediaLab.Chips.Tests.CPU.Motorola.M6800.Chip6809;

/// <summary>Documented Motorola instruction families, independent arithmetic and branch oracles.</summary>
public sealed class Cpu6809InstructionMatrixTests
{
    private static readonly byte[] RegisterSetup =
    [
        0x86, 0xde, 0x1f, 0x8b, 0xcc, 0x9a, 0xbc, 0x8e, 0x12, 0x34,
        0x10, 0x8e, 0x56, 0x78, 0xce, 0x40, 0x00, 0x10, 0xce, 0x30, 0x00,
        0x1c, 0, 0x1a, 0xa5
    ];

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EveryStackMaskUsesTheDocumentedFrameOrderAndByteCycleCount(bool user)
    {
        for (var mask = 0; mask <= 255; mask++)
        {
            var program = RegisterSetup.ToList();
            program.AddRange([(byte)(user ? 0x36 : 0x34), (byte)mask,
                (byte)(user ? 0x37 : 0x35), (byte)mask]);
            var (cpu, bus) = Start(program);
            for (var step = 0; step < 9; step++) cpu.Step();
            var nextPc = (ushort)(0x1000 + RegisterSetup.Length + 2);
            byte[][] fields =
            [
                [0xa5], [0x9a], [0xbc], [0xde], [0x12, 0x34], [0x56, 0x78],
                user ? [0x30, 0] : [0x40, 0], [(byte)(nextPc >> 8), (byte)nextPc]
            ];
            var frame = fields.Where((_, bit) => (mask & (1 << bit)) != 0).SelectMany(bytes => bytes).ToArray();
            var top = user ? 0x4000 : 0x3000;
            var before = cpu.Cycles;
            cpu.Step();
            Assert.Equal(top - frame.Length, user ? cpu.U : cpu.S);
            Assert.Equal(frame, bus.Memory.AsSpan(top - frame.Length, frame.Length).ToArray());
            Assert.Equal(5 + frame.Length, cpu.Cycles - before);
            before = cpu.Cycles;
            cpu.Step();
            Assert.Equal(top, user ? cpu.U : cpu.S);
            Assert.Equal(5 + frame.Length, cpu.Cycles - before);
            Assert.Equal((mask & 0x80) != 0 ? nextPc : nextPc + 2, cpu.PC);
            Assert.Equal(0x9a, cpu.A); Assert.Equal(0xbc, cpu.B);
            Assert.Equal(0xde, cpu.DP); Assert.Equal(0xa5, cpu.CC);
            Assert.Equal(0x1234, cpu.X); Assert.Equal(0x5678, cpu.Y);
            Assert.Equal(0x3000, cpu.S); Assert.Equal(0x4000, cpu.U);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TransfersAndExchangesCoverEverySameWidthRegisterPair(bool exchange)
    {
        int[][] families = [[0, 1, 2, 3, 4, 5], [8, 9, 10, 11]];
        foreach (var family in families)
        foreach (var source in family)
        foreach (var destination in family)
        {
            var program = RegisterSetup.ToList();
            program.AddRange([(byte)(exchange ? 0x1e : 0x1f), (byte)(source << 4 | destination)]);
            var (cpu, _) = Start(program);
            for (var step = 0; step < 9; step++) cpu.Step();
            var nextPc = (ushort)(0x1000 + program.Count);
            var values = family.ToDictionary(register => register,
                register => register == 5 ? nextPc : TransferValue(cpu, register));
            var expected = new Dictionary<int, ushort>(values) { [destination] = values[source] };
            if (exchange) expected[source] = values[destination];
            var before = cpu.Cycles;
            cpu.Step();
            Assert.Equal(exchange ? 8 : 6, cpu.Cycles - before);
            foreach (var register in family) Assert.Equal(expected[register], TransferValue(cpu, register));
            if (!family.Contains(5)) Assert.Equal(nextPc, cpu.PC);
        }
    }

    private static ushort TransferValue(Cpu6809 cpu, int register) => register switch
    {
        0 => (ushort)(cpu.A * 256 + cpu.B), 1 => cpu.X, 2 => cpu.Y, 3 => cpu.U,
        4 => cpu.S, 5 => cpu.PC, 8 => cpu.A, 9 => cpu.B, 10 => cpu.CC, 11 => cpu.DP,
        _ => throw new ArgumentOutOfRangeException(nameof(register))
    };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ByteOperationsPreserveUnaffectedRegistersAndFlagsInEveryAddressingMode(bool accumulatorB)
    {
        int[] operations = [0, 1, 2, 4, 5, 6, 7, 8, 9, 10, 11];
        (byte Value, byte Operand)[] cases = [(0, 0), (0x7f, 1), (0x80, 0xff), (0xff, 1), (0x80, 1), (0, 0x7f), (0xff, 0x80)];
        foreach (var operation in operations)
        foreach (var mode in Enumerable.Range(0, 4))
        foreach (var (value, operand) in cases)
        foreach (var carry in Enumerable.Range(0, 2))
        {
            if (operation == 7 && mode == 0) continue; // No documented immediate store.
            var flags = (byte)(0xfe | carry);
            var program = new List<byte>
            {
                0x86, accumulatorB ? (byte)0x5a : value,
                0xc6, accumulatorB ? value : (byte)0xa5,
                0x8e, 0, 0x20, 0x1c, 0, 0x1a, flags,
                (byte)((accumulatorB ? 0xc0 : 0x80) + mode * 16 + operation)
            };
            AddOperand(program, mode, operand, false);
            var (cpu, bus) = Start(program);
            bus.Memory[0x20] = operand;
            for (var step = 0; step < 5; step++) cpu.Step();
            var before = cpu.Cycles;
            cpu.Step();
            var (result, expectedFlags) = ByteResult(operation, value, operand, flags);
            var stored = operation == 7;
            var unchanged = operation is 1 or 5 || stored;
            Assert.Equal(unchanged ? value : result, accumulatorB ? cpu.B : cpu.A);
            Assert.Equal(accumulatorB ? 0x5a : 0xa5, accumulatorB ? cpu.A : cpu.B);
            Assert.Equal(stored ? value : operand, bus.Memory[0x20]);
            Assert.Equal(expectedFlags, cpu.CC);
            Assert.Equal(mode == 0 ? 2 : mode == 3 ? 5 : 4, cpu.Cycles - before);
            Assert.Equal(0x1000 + program.Count, cpu.PC);
        }
    }

    [Fact]
    public void WordOperationsCoverBothPrefixPagesMemoryByteOrderAndAddressWrap()
    {
        // Register indices: D, X, Y, U, S. Cycle counts are immediate cycles;
        // direct/indexed add two and extended adds three (Motorola tables).
        (byte Prefix, byte Opcode, int Register, string Operation, int Cycles)[] operations =
        [
            (0, 0x83, 0, "subtract", 4), (0, 0xc3, 0, "add", 4),
            (0, 0x8c, 1, "compare", 4), (0, 0xcc, 0, "load", 3),
            (0, 0x8e, 1, "load", 3), (0, 0xce, 3, "load", 3),
            (0x10, 0x83, 0, "compare", 5), (0x10, 0x8c, 2, "compare", 5),
            (0x11, 0x83, 3, "compare", 5), (0x11, 0x8c, 4, "compare", 5),
            (0x10, 0x8e, 2, "load", 4), (0x10, 0xce, 4, "load", 4),
            (0, 0xcd, 0, "store", 3), (0, 0x8f, 1, "store", 3),
            (0, 0xcf, 3, "store", 3), (0x10, 0x8f, 2, "store", 4),
            (0x10, 0xcf, 4, "store", 4)
        ];
        (ushort Value, ushort Operand)[] cases = [(0, 0), (0x7fff, 1), (0x8000, 0xffff), (0xffff, 1), (0x1234, 0xabcd)];
        foreach (var operation in operations)
        foreach (var mode in Enumerable.Range(0, 4))
        foreach (var (value, operand) in cases)
        {
            var store = operation.Operation == "store";
            if (store && mode == 0) continue;
            var program = new List<byte>();
            foreach (byte[] load in new byte[][]
            {
                [0xcc, (byte)(value >> 8), (byte)value],
                [0x8e, (byte)(value >> 8), (byte)value],
                [0x10, 0x8e, (byte)(value >> 8), (byte)value],
                [0xce, (byte)(value >> 8), (byte)value],
                [0x10, 0xce, (byte)(value >> 8), (byte)value]
            }) program.AddRange(load);
            program.AddRange([0x1c, 0, 0x1a, 0xff]);
            if (operation.Prefix != 0) program.Add(operation.Prefix);
            program.Add((byte)(operation.Opcode + mode * 16));
            AddOperand(program, mode, operand, true);
            var (cpu, bus) = Start(program);
            var address = mode == 2 ? value : (ushort)0x20;
            bus.Memory[address] = (byte)(operand >> 8);
            bus.Memory[(ushort)(address + 1)] = (byte)operand;
            for (var step = 0; step < 7; step++) cpu.Step();
            var before = cpu.Cycles;
            cpu.Step();
            var arithmetic = operation.Operation is "add" or "subtract" or "compare";
            var mathematical = operation.Operation == "add" ? value + operand : value - operand;
            var result = arithmetic ? (ushort)mathematical : store ? value : operand;
            var expectedFlags = (byte)(0xff & ~(arithmetic ? 0x0f : 0x0e));
            if (result == 0) expectedFlags |= 4;
            if ((result & 0x8000) != 0) expectedFlags |= 8;
            if (arithmetic)
            {
                if (mathematical < 0 || mathematical > 65535) expectedFlags |= 1;
                var signed = operation.Operation == "add" ? (short)value + (short)operand : (short)value - (short)operand;
                if (signed < -32768 || signed > 32767) expectedFlags |= 2;
            }
            var registers = new ushort[] { (ushort)(cpu.A << 8 | cpu.B), cpu.X, cpu.Y, cpu.U, cpu.S };
            for (var register = 0; register < registers.Length; register++)
                Assert.Equal(register == operation.Register && !store && operation.Operation != "compare" ? result : value, registers[register]);
            var memory = (ushort)(bus.Memory[address] << 8 | bus.Memory[(ushort)(address + 1)]);
            Assert.Equal(store ? value : operand, memory);
            Assert.Equal(expectedFlags, cpu.CC);
            Assert.Equal(operation.Cycles + (mode == 0 ? 0 : mode == 3 ? 3 : 2), cpu.Cycles - before);
            Assert.Equal(0x1000 + program.Count, cpu.PC);
        }
    }

    [Fact]
    public void ShortAndLongBranchesCoverEveryConditionAndBothDisplacementSigns()
    {
        foreach (var flags in Enumerable.Range(0, 16))
        foreach (var condition in Enumerable.Range(0, 16))
        foreach (var wide in new[] { false, true })
        foreach (var displacement in new[] { -5, 33 })
        {
            var c = (flags & 1) != 0;
            var v = (flags & 2) != 0;
            var z = (flags & 4) != 0;
            var n = (flags & 8) != 0;
            bool[] decisions = [true, false, !(c || z), c || z, !c, c, !z, z, !v, v, !n, n, n == v, n != v, !z && n == v, z || n != v];
            var program = new List<byte> { 0x1c, 0, 0x1a, (byte)flags };
            if (wide && condition != 0) program.Add(0x10);
            program.Add(wide && condition == 0 ? (byte)0x16 : (byte)(0x20 + condition));
            if (wide) program.Add((byte)(displacement >> 8));
            program.Add(unchecked((byte)displacement));
            var (cpu, _) = Start(program);
            cpu.Step(); cpu.Step();
            var before = cpu.Cycles;
            cpu.Step();
            Assert.Equal((ushort)(0x1000 + program.Count + (decisions[condition] ? displacement : 0)), cpu.PC);
            Assert.Equal(flags, cpu.CC);
            Assert.Equal(!wide ? 3 : condition == 0 || !decisions[condition] ? 5 : 6, cpu.Cycles - before);
        }
    }

    [Fact]
    public void DocumentedIndexedFormsResolveSignedOffsetsIndirectionAndRegisterWrap()
    {
        IndexCase[] forms =
        [
            new(0x10, -16, [], 1, IndirectAllowed: false),
            new(0x1f, -1, [], 1, IndirectAllowed: false),
            new(0, 0, [], 1, IndirectAllowed: false),
            new(0x0f, 15, [], 1, IndirectAllowed: false),
            new(0x84, 0, [], 0),
            new(0x85, -3, [], 1), new(0x86, -2, [], 1),
            new(0x8b, -259, [], 4),
            new(0x80, 0, [], 2, 1, IndirectAllowed: false),
            new(0x81, 0, [], 3, 2),
            new(0x82, -1, [], 2, -1, IndirectAllowed: false),
            new(0x83, -2, [], 3, -2),
            new(0x88, -128, [0x80], 1), new(0x88, 127, [0x7f], 1),
            new(0x89, -256, [0xff, 0], 4), new(0x89, 256, [1, 0], 4),
            new(0x8c, -128, [0x80], 1, PcRelative: true),
            new(0x8c, 127, [0x7f], 1, PcRelative: true),
            new(0x8d, -256, [0xff, 0], 5, PcRelative: true),
            new(0x8d, 256, [1, 0], 5, PcRelative: true),
            new(0x8f, 0x3456, [0x34, 0x56], 2, Absolute: true)
        ];
        foreach (var form in forms)
        foreach (var register in Enumerable.Range(0, 4))
        foreach (ushort basis in new ushort[] { 0x2000, 0xffff })
        foreach (var indirect in new[] { false, true })
        {
            if (indirect && !form.IndirectAllowed || form.Absolute && !indirect) continue;
            if ((form.PcRelative || form.Absolute) && register != 0) continue;
            var program = new List<byte>
            {
                0x8e, (byte)(basis >> 8), (byte)basis,
                0x10, 0x8e, (byte)(basis >> 8), (byte)basis,
                0xce, (byte)(basis >> 8), (byte)basis,
                0x10, 0xce, (byte)(basis >> 8), (byte)basis,
                0x86, 0xfe, 0xc6, 0xfd,
                0xa6, (byte)(form.Postbyte | register << 5 | (indirect ? 0x10 : 0))
            };
            program.AddRange(form.Extension);
            var (cpu, bus) = Start(program);
            var address = (ushort)(form.Absolute ? form.Offset : form.Offset + (form.PcRelative ? 0x1000 + program.Count : basis));
            if (indirect)
            {
                bus.Memory[address] = 0x40;
                bus.Memory[(ushort)(address + 1)] = 0;
                bus.Memory[0x4000] = 0x5a;
            }
            else bus.Memory[address] = 0x5a;
            for (var step = 0; step < 6; step++) cpu.Step();
            var before = cpu.Cycles;
            cpu.Step();
            Assert.Equal(0x5a, cpu.A);
            Assert.Equal(0xfd, cpu.B);
            var registers = new ushort[] { cpu.X, cpu.Y, cpu.U, cpu.S };
            for (var index = 0; index < registers.Length; index++)
                Assert.Equal((ushort)(basis + (index == register ? form.RegisterDelta : 0)), registers[index]);
            Assert.Equal(4 + form.ExtraCycles + (indirect ? 3 : 0), cpu.Cycles - before);
            Assert.Equal(0x1000 + program.Count, cpu.PC);
        }
    }

    private sealed record IndexCase(byte Postbyte, int Offset, byte[] Extension, int ExtraCycles,
        int RegisterDelta = 0, bool IndirectAllowed = true, bool PcRelative = false, bool Absolute = false);

    private static (byte Result, byte Flags) ByteResult(int operation, byte value, byte operand, byte flags)
    {
        var incomingCarry = flags & 1;
        var arithmetic = operation is 0 or 1 or 2 or 9 or 11;
        var add = operation is 9 or 11;
        var carry = operation is 2 or 9 ? incomingCarry : 0;
        var mathematical = add ? value + operand + carry : value - operand - carry;
        var result = operation switch
        {
            0 or 1 or 2 or 9 or 11 => (byte)mathematical,
            4 or 5 => (byte)(value & operand), 6 => operand, 7 => value,
            8 => (byte)(value ^ operand), 10 => (byte)(value | operand),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };
        var expected = (byte)(flags & ~(add ? 0x2f : arithmetic ? 0x0f : 0x0e));
        if (result == 0) expected |= 4;
        if ((result & 0x80) != 0) expected |= 8;
        if (arithmetic)
        {
            if (mathematical < 0 || mathematical > 255) expected |= 1;
            var signed = add ? (sbyte)value + (sbyte)operand + carry : (sbyte)value - (sbyte)operand - carry;
            if (signed < -128 || signed > 127) expected |= 2;
            if (add && (value & 15) + (operand & 15) + carry > 15) expected |= 0x20;
        }
        return (result, expected);
    }

    private static void AddOperand(List<byte> program, int mode, ushort operand, bool word)
    {
        switch (mode)
        {
            case 0:
                if (word) program.Add((byte)(operand >> 8));
                program.Add((byte)operand);
                break;
            case 1: program.Add(0x20); break;
            case 2: program.Add(0x84); break; // ,X
            case 3: program.AddRange([0, 0x20]); break;
        }
    }

    private static (Cpu6809 Cpu, Bus Bus) Start(List<byte> program)
    {
        var bus = new Bus();
        program.CopyTo(bus.Memory, 0x1000);
        bus.Memory[0xfffe] = 0x10;
        var cpu = new Cpu6809(bus);
        cpu.Reset();
        return (cpu, bus);
    }

    private sealed class Bus : ICpuBus
    {
        internal readonly byte[] Memory = new byte[65536];
        public bool Irq => throw new InvalidOperationException("Unexpected IRQ polling.");
        public bool Nmi => throw new InvalidOperationException("Unexpected NMI polling.");
        public byte Read(ushort address) => Memory[address];
        public void Write(ushort address, byte value) => Memory[address] = value;
        public void Tick() { }
    }
}
