namespace RetroMediaLab.Chips.Common.Compilation.X64;

// Hardware register numbers are used; high byte registers have separate identities.
internal enum X64Register : byte
{
    Rax, Rcx, Rdx, Rbx, Rsp, Rbp, Rsi, Rdi, R8, R9, R10, R11, R12, R13, R14, R15,
    Ah, Ch, Dh, Bh
}
internal enum X64Width : byte { Byte, Word, Dword, Qword }
internal enum X64Arithmetic : byte { Add, Or, AddCarry, SubtractBorrow, And, Subtract, Xor, Compare }
internal enum X64Shift : byte { RotateLeft, RotateRight, RotateCarryLeft, RotateCarryRight, Left, Right, ArithmeticRight = 7 }
internal enum X64Unary : byte { Complement = 2, Negate, Multiply, MultiplySigned, Divide, DivideSigned }
internal enum X64Bit : byte { Test, Set, Reset, Complement }
internal enum X64Condition : byte
{
    Overflow, NoOverflow, Carry, NoCarry, Zero, NotZero, BelowOrEqual, Above,
    Sign, NoSign, Parity, NoParity, Less, GreaterOrEqual, LessOrEqual, Greater
}

/// <summary>
/// Managed port of the scalar encoding helpers in BlastEm gen_x86.c,
/// tree ec73825ece2a134beb7825a5fd6fd21a398ff12e. Owns bytes, not executable memory.
/// Register identities are normalized to hardware numbers. Stack alignment and
/// the Windows x64 shadow space belong to the caller of CallRegister/CallRelative.
/// </summary>
internal sealed class X64Emitter
{
    private readonly List<byte> _code = [];
    // Shared by all context emitters contributing to this code buffer.
    internal bool CacheExecutionState { get; init; }
    internal bool CachePrefetch { get; init; }
    internal bool CacheGuestRegisters { get; init; }
    internal bool ObserveInstructionFetches { get; init; }
    internal long ReadOnlyDataBase { get; init; }
    internal int StackBytes { get; set; }
    public int Position => _code.Count;
    public byte[] ToArray() => _code.ToArray();
    public void Append(ReadOnlySpan<byte> code)
    {
        foreach (byte value in code) _code.Add(value);
    }
    private void Byte(int value) => _code.Add(unchecked((byte)value));
    private void Integer(ulong value, int bytes)
    {
        for (int i = 0; i < bytes; i++) { Byte((int)value); value >>= 8; }
    }
    private static bool High(X64Register register) => register >= X64Register.Ah;
    private static int Encoded(X64Register register) => High(register) ? (int)register - 12 : (int)register;
    private static void Register(X64Register register)
    {
        if (register > X64Register.Bh) throw new ArgumentOutOfRangeException(nameof(register));
    }
    private void Prefix(X64Width width, X64Register? reg, X64Register rm, bool memory = false,
        X64Register? index = null)
    {
        Register(rm);
        if (reg is { } r) Register(r);
        if (index is { } i)
        {
            Register(i);
            if (High(i) || i == X64Register.Rsp) throw new ArgumentException("Invalid address index.", nameof(index));
        }
        if (width > X64Width.Qword) throw new ArgumentOutOfRangeException(nameof(width));
        if (memory && High(rm)) throw new ArgumentException("A byte register cannot address memory.", nameof(rm));
        if (width != X64Width.Byte && (High(rm) || reg is { } high && High(high)))
            throw new ArgumentException("High byte registers require byte operands.");
        int rex = width == X64Width.Qword ? 0x48 : 0x40;
        if (reg is { } field && !High(field) && (int)field >= 8) rex |= 4;
        if (!High(rm) && (int)rm >= 8) rex |= 1;
        if (index is { } extendedIndex && (int)extendedIndex >= 8) rex |= 2;
        bool lowByte = width == X64Width.Byte &&
            (reg is >= X64Register.Rsp and <= X64Register.Rdi
                || !memory && rm is >= X64Register.Rsp and <= X64Register.Rdi);
        if ((rex != 0x40 || lowByte) && (High(rm) || reg is { } h && High(h)))
            throw new ArgumentException("High byte registers cannot be encoded with REX.");
        if (width == X64Width.Word) Byte(0x66);
        if (rex != 0x40 || lowByte) Byte(rex);
    }
    private void Memory(int field, X64Register @base, int displacement)
    {
        int b = Encoded(@base) & 7;
        bool shortDisplacement = displacement is >= -128 and <= 127;
        Byte((shortDisplacement ? 0x40 : 0x80) | ((field & 7) << 3) | b);
        if (b == 4) Byte(0x24); // RSP/R12 base, no index.
        Integer(unchecked((uint)displacement), shortDisplacement ? 1 : 4);
    }
    private static int ImmediateBytes(X64Width width) => width switch
    {
        X64Width.Byte => 1, X64Width.Word => 2, _ => 4
    };
    private static int Sized(int opcode, X64Width width) => opcode | (width == X64Width.Byte ? 0 : 1);
    private static void Arithmetic(X64Arithmetic operation)
    {
        if (operation > X64Arithmetic.Compare) throw new ArgumentOutOfRangeException(nameof(operation));
    }
    private void RegisterOperation(int opcode, X64Register source, X64Register destination, X64Width width)
    {
        Prefix(width, source, destination);
        Byte(Sized(opcode, width));
        Byte(0xc0 | ((Encoded(source) & 7) << 3) | (Encoded(destination) & 7));
    }
    public void Move(X64Register source, X64Register destination, X64Width width) =>
        RegisterOperation(0x88, source, destination, width);
    public void Binary(X64Arithmetic operation, X64Register source, X64Register destination, X64Width width)
    {
        Arithmetic(operation);
        RegisterOperation((int)operation << 3, source, destination, width);
    }
    public void Test(X64Register source, X64Register destination, X64Width width) =>
        RegisterOperation(0x84, source, destination, width);
    public void MoveImmediate(long value, X64Register destination, X64Width width)
    {
        Prefix(width, null, destination);
        int register = Encoded(destination) & 7;
        bool signed32 = width == X64Width.Qword && value is >= int.MinValue and <= int.MaxValue;
        if (signed32) { Byte(0xc7); Byte(0xc0 | register); }
        else Byte((width == X64Width.Byte ? 0xb0 : 0xb8) | register);
        Integer(unchecked((ulong)value), width == X64Width.Qword && !signed32 ? 8 : ImmediateBytes(width));
    }
    public void Load(X64Register @base, int displacement, X64Register destination, X64Width width)
    {
        Prefix(width, destination, @base, true);
        Byte(Sized(0x8a, width)); Memory(Encoded(destination), @base, displacement);
    }
    public void Store(X64Register source, X64Register @base, int displacement, X64Width width)
    {
        Prefix(width, source, @base, true);
        Byte(Sized(0x88, width)); Memory(Encoded(source), @base, displacement);
    }
    public void StoreImmediate(int value, X64Register @base, int displacement, X64Width width)
    {
        Prefix(width, null, @base, true); Byte(Sized(0xc6, width)); Memory(0, @base, displacement);
        Integer(unchecked((uint)value), ImmediateBytes(width));
    }
    public void BinaryLoad(X64Arithmetic operation, X64Register @base, int displacement,
        X64Register destination, X64Width width)
    {
        Arithmetic(operation); Prefix(width, destination, @base, true);
        Byte(Sized(((int)operation << 3) | 2, width)); Memory(Encoded(destination), @base, displacement);
    }
    public void BinaryStore(X64Arithmetic operation, X64Register source,
        X64Register @base, int displacement, X64Width width)
    {
        Arithmetic(operation); Prefix(width, source, @base, true);
        Byte(Sized((int)operation << 3, width)); Memory(Encoded(source), @base, displacement);
    }
    public void BinaryImmediate(X64Arithmetic operation, int value, X64Register destination, X64Width width)
    {
        Arithmetic(operation); Prefix(width, null, destination);
        bool small = width is X64Width.Dword or X64Width.Qword && value is >= -128 and <= 127;
        if (destination == X64Register.Rax && !small)
            Byte(Sized(((int)operation << 3) | 4, width));
        else
        {
            Byte(small ? 0x83 : Sized(0x80, width));
            Byte(0xc0 | ((int)operation << 3) | (Encoded(destination) & 7));
        }
        Integer(unchecked((uint)value), small ? 1 : ImmediateBytes(width));
    }
    public void BinaryMemoryImmediate(X64Arithmetic operation, int value, X64Register @base,
        int displacement, X64Width width)
    {
        Arithmetic(operation); Prefix(width, null, @base, true);
        bool small = width is X64Width.Dword or X64Width.Qword && value is >= -128 and <= 127;
        Byte(small ? 0x83 : Sized(0x80, width)); Memory((int)operation, @base, displacement);
        Integer(unchecked((uint)value), small ? 1 : ImmediateBytes(width));
    }
    public void MoveExtend(X64Register source, X64Register destination,
        X64Width sourceWidth, X64Width destinationWidth, bool signed)
    {
        if (sourceWidth >= destinationWidth || destinationWidth > X64Width.Qword)
            throw new ArgumentException("An extension must increase the operand width.");
        if (High(destination)) throw new ArgumentException("Cannot extend to a high byte register.");
        if (sourceWidth == X64Width.Dword)
        {
            if (!signed) { Move(source, destination, X64Width.Dword); return; }
            Prefix(destinationWidth, destination, source);
            Byte(0x63);
        }
        else
        {
            // Prefix width describes the destination, but REX constraints also apply
            // to a byte source such as SPL or AH.
            if (sourceWidth == X64Width.Byte && High(source))
            {
                if (destinationWidth == X64Width.Qword || (int)destination >= 8)
                    throw new ArgumentException("High byte source cannot be encoded with REX.");
                if (destinationWidth == X64Width.Word) Byte(0x66);
            }
            else
            {
                Prefix(destinationWidth, destination, source);
                if (sourceWidth == X64Width.Byte && source is >= X64Register.Rsp and <= X64Register.Rdi
                    && destinationWidth != X64Width.Qword && (int)destination < 8)
                    Byte(0x40);
            }
            Byte(0x0f); Byte((signed ? 0xbe : 0xb6) | (sourceWidth == X64Width.Word ? 1 : 0));
        }
        Byte(0xc0 | (((int)destination & 7) << 3) | (Encoded(source) & 7));
    }
    public void Shift(X64Shift operation, byte count, X64Register destination, X64Width width)
    {
        if ((int)operation is 6 or > 7) throw new ArgumentOutOfRangeException(nameof(operation));
        Prefix(width, null, destination); Byte(Sized(0xc0, width));
        Byte(0xc0 | ((int)operation << 3) | (Encoded(destination) & 7)); Byte(count);
    }
    public void ShiftByCl(X64Shift operation, X64Register destination, X64Width width)
    {
        if ((int)operation is 6 or > 7) throw new ArgumentOutOfRangeException(nameof(operation));
        Prefix(width, null, destination); Byte(Sized(0xd2, width));
        Byte(0xc0 | ((int)operation << 3) | (Encoded(destination) & 7));
    }
    private static void Condition(X64Condition condition)
    {
        if (condition > X64Condition.Greater) throw new ArgumentOutOfRangeException(nameof(condition));
    }
    public void SetCondition(X64Condition condition, X64Register destination)
    {
        Condition(condition); Prefix(X64Width.Byte, null, destination);
        Byte(0x0f); Byte(0x90 | (int)condition); Byte(0xc0 | (Encoded(destination) & 7));
    }
    public void SetMemoryCondition(X64Condition condition, X64Register @base, int displacement)
    {
        Condition(condition); Prefix(X64Width.Byte, null, @base, true);
        Byte(0x0f); Byte(0x90 | (int)condition); Memory(0, @base, displacement);
    }
    public void Jump(int targetOffset, X64Condition? condition = null)
    {
        if (condition is { } cc) Condition(cc);
        long displacement = (long)targetOffset - (Position + 2);
        if (displacement is >= -128 and <= 127)
        {
            Byte(condition is { } shortCc ? 0x70 | (int)shortCc : 0xeb); Byte((int)displacement);
        }
        else
        {
            int length = condition is null ? 5 : 6;
            int relative = checked((int)((long)targetOffset - (Position + length)));
            if (condition is { } nearCc) { Byte(0x0f); Byte(0x80 | (int)nearCc); }
            else Byte(0xe9);
            Integer(unchecked((uint)relative), 4);
        }
    }
    public int ReserveJump(X64Condition? condition = null)
    {
        if (condition is { } cc) { Condition(cc); Byte(0x0f); Byte(0x80 | (int)cc); }
        else Byte(0xe9);
        int displacementOffset = Position; Integer(0, 4); return displacementOffset;
    }
    public void PatchJump(int displacementOffset, int targetOffset)
    {
        if (displacementOffset < 1 || displacementOffset > Position - 4)
            throw new ArgumentOutOfRangeException(nameof(displacementOffset));
        uint displacement = unchecked((uint)checked((int)((long)targetOffset - (displacementOffset + 4))));
        for (int i = 0; i < 4; i++) _code[displacementOffset + i] = (byte)(displacement >> (i * 8));
    }
    public void CallRelative(int targetOffset)
    {
        int displacement = checked((int)((long)targetOffset - (Position + 5)));
        Byte(0xe8); Integer(unchecked((uint)displacement), 4);
    }
    public void CallRegister(X64Register target) => IndirectControl(target, 2);
    public void JumpRegister(X64Register target) => IndirectControl(target, 4);
    private void IndirectControl(X64Register target, int operation)
    {
        Register(target);
        if (High(target)) throw new ArgumentException("A control target requires a full register.", nameof(target));
        if ((int)target >= 8) Byte(0x41);
        Byte(0xff); Byte(0xc0 | (operation << 3) | ((int)target & 7));
    }
    public void Push(X64Register register) { Stack(register, 0x50); StackBytes += 8; }
    public void Pop(X64Register register) { Stack(register, 0x58); StackBytes -= 8; }
    public void PushFlags() { Byte(0x9c); StackBytes += 8; }
    public void PopFlags() { Byte(0x9d); StackBytes -= 8; }
    public void AllocateStack(int bytes)
    {
        BinaryImmediate(X64Arithmetic.Subtract, bytes, X64Register.Rsp, X64Width.Qword); StackBytes += bytes;
    }
    public void ReleaseStack(int bytes)
    {
        BinaryImmediate(X64Arithmetic.Add, bytes, X64Register.Rsp, X64Width.Qword); StackBytes -= bytes;
    }
    private void Stack(X64Register register, int opcode)
    {
        Register(register);
        if (High(register)) throw new ArgumentException("A stack operand requires a full register.", nameof(register));
        if ((int)register >= 8) Byte(0x41);
        Byte(opcode | ((int)register & 7));
    }
    public void Return() => Byte(0xc3);

    private static void Wide(X64Width width)
    {
        if (width is < X64Width.Word or > X64Width.Qword)
            throw new ArgumentOutOfRangeException(nameof(width), "Instruction requires 16, 32 or 64 bits.");
    }
    private static void UnaryOperation(X64Unary operation)
    {
        if (operation is < X64Unary.Complement or > X64Unary.DivideSigned)
            throw new ArgumentOutOfRangeException(nameof(operation));
    }
    public void Unary(X64Unary operation, X64Register operand, X64Width width)
    {
        UnaryOperation(operation); Prefix(width, null, operand);
        Byte(Sized(0xf6, width)); Byte(0xc0 | ((int)operation << 3) | (Encoded(operand) & 7));
    }
    public void UnaryMemory(X64Unary operation, X64Register @base, int displacement, X64Width width)
    {
        UnaryOperation(operation); Prefix(width, null, @base, true);
        Byte(Sized(0xf6, width)); Memory((int)operation, @base, displacement);
    }
    public void MultiplySigned(X64Register source, X64Register destination, X64Width width)
    {
        Wide(width); Prefix(width, destination, source);
        Byte(0x0f); Byte(0xaf); Byte(0xc0 | ((Encoded(destination) & 7) << 3) | (Encoded(source) & 7));
    }
    public void MultiplySignedMemory(X64Register @base, int displacement, X64Register destination, X64Width width)
    {
        Wide(width); Prefix(width, destination, @base, true);
        Byte(0x0f); Byte(0xaf); Memory(Encoded(destination), @base, displacement);
    }
    public void MultiplyImmediate(int value, X64Register source, X64Register destination, X64Width width)
    {
        Wide(width); Prefix(width, destination, source);
        bool small = width is X64Width.Dword or X64Width.Qword && value is >= -128 and <= 127;
        Byte(small ? 0x6b : 0x69); Byte(0xc0 | ((Encoded(destination) & 7) << 3) | (Encoded(source) & 7));
        Integer(unchecked((uint)value), small ? 1 : ImmediateBytes(width));
    }
    public void SignExtendDividend(X64Width width)
    {
        Wide(width);
        if (width == X64Width.Word) Byte(0x66);
        if (width == X64Width.Qword) Byte(0x48);
        Byte(0x99); // CWD/CDQ/CQO.
    }
    public void Exchange(X64Register source, X64Register destination, X64Width width) =>
        RegisterOperation(0x86, source, destination, width);
    public void TestImmediate(int value, X64Register destination, X64Width width)
    {
        Prefix(width, null, destination);
        if (destination == X64Register.Rax) Byte(Sized(0xa8, width));
        else { Byte(Sized(0xf6, width)); Byte(0xc0 | (Encoded(destination) & 7)); }
        Integer(unchecked((uint)value), ImmediateBytes(width));
    }
    public void TestMemoryImmediate(int value, X64Register @base, int displacement, X64Width width)
    {
        Prefix(width, null, @base, true); Byte(Sized(0xf6, width)); Memory(0, @base, displacement);
        Integer(unchecked((uint)value), ImmediateBytes(width));
    }
    public void LoadExtend(X64Register @base, int displacement, X64Register destination,
        X64Width sourceWidth, X64Width destinationWidth, bool signed)
    {
        if (sourceWidth >= destinationWidth || destinationWidth > X64Width.Qword)
            throw new ArgumentException("An extension must increase the operand width.");
        if (sourceWidth == X64Width.Dword && !signed)
        {
            Load(@base, displacement, destination, X64Width.Dword); return;
        }
        Prefix(destinationWidth, destination, @base, true);
        if (sourceWidth == X64Width.Dword) Byte(0x63);
        else { Byte(0x0f); Byte((signed ? 0xbe : 0xb6) | (sourceWidth == X64Width.Word ? 1 : 0)); }
        Memory(Encoded(destination), @base, displacement);
    }
    private void Indexed(int field, X64Register @base, X64Register index, byte scale, int displacement)
    {
        bool small = displacement is >= -128 and <= 127;
        Byte((small ? 0x40 : 0x80) | ((field & 7) << 3) | 4);
        Byte((scale << 6) | (((int)index & 7) << 3) | ((int)@base & 7));
        Integer(unchecked((uint)displacement), small ? 1 : 4);
    }
    public void LoadIndexed(X64Register @base, X64Register index, byte scale, int displacement,
        X64Register destination, X64Width width)
    {
        if (scale > 3) throw new ArgumentOutOfRangeException(nameof(scale));
        Prefix(width, destination, @base, true, index); Byte(Sized(0x8a, width));
        Indexed(Encoded(destination), @base, index, scale, displacement);
    }
    public void StoreIndexed(X64Register source, X64Register @base, X64Register index,
        byte scale, int displacement, X64Width width)
    {
        if (scale > 3) throw new ArgumentOutOfRangeException(nameof(scale));
        Prefix(width, source, @base, true, index); Byte(Sized(0x88, width));
        Indexed(Encoded(source), @base, index, scale, displacement);
    }
    private static void BitOperation(X64Bit operation, X64Width width)
    {
        Wide(width);
        if (operation > X64Bit.Complement) throw new ArgumentOutOfRangeException(nameof(operation));
    }
    public void BitImmediate(X64Bit operation, byte bit, X64Register destination, X64Width width)
    {
        BitOperation(operation, width);
        if (High(destination))
        {
            Register(destination);
            destination = (X64Register)((int)destination - (int)X64Register.Ah);
            bit = unchecked((byte)(bit + 8));
        }
        Prefix(width, null, destination); Byte(0x0f); Byte(0xba);
        Byte(0xc0 | (((int)operation + 4) << 3) | (Encoded(destination) & 7)); Byte(bit);
    }
    public void BitRegister(X64Bit operation, X64Register bit, X64Register destination, X64Width width)
    {
        BitOperation(operation, width); Prefix(width, bit, destination);
        Byte(0x0f); Byte(0xa3 | ((int)operation << 3));
        Byte(0xc0 | ((Encoded(bit) & 7) << 3) | (Encoded(destination) & 7));
    }
    public void BitMemoryImmediate(X64Bit operation, byte bit, X64Register @base,
        int displacement, X64Width width)
    {
        BitOperation(operation, width); Prefix(width, null, @base, true);
        Byte(0x0f); Byte(0xba); Memory((int)operation + 4, @base, displacement); Byte(bit);
    }
    public void BitMemoryRegister(X64Bit operation, X64Register bit, X64Register @base,
        int displacement, X64Width width)
    {
        BitOperation(operation, width); Prefix(width, bit, @base, true);
        Byte(0x0f); Byte(0xa3 | ((int)operation << 3)); Memory(Encoded(bit), @base, displacement);
    }
    public void SwapWordBytes(X64Register register) => Shift(X64Shift.RotateRight, 8, register, X64Width.Word);
    public void SwapLongWords(X64Register register) => Shift(X64Shift.RotateLeft, 16, register, X64Width.Dword);
}

