using RetroMediaLab.Chips.Common.Compilation.X64;
using R = RetroMediaLab.Chips.Common.Compilation.X64.X64Register;
using W = RetroMediaLab.Chips.Common.Compilation.X64.X64Width;
using A = RetroMediaLab.Chips.Common.Compilation.X64.X64Arithmetic;
using C = RetroMediaLab.Chips.Common.Compilation.X64.X64Condition;
using S = RetroMediaLab.Chips.Common.Compilation.X64.X64Shift;
using F = RetroMediaLab.Chips.CPU.Zilog.Z80.Instruction.Compilation.Z80X64Field;

namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Instruction.Compilation;

internal sealed partial class Z80X64InstructionEmitter(X64Emitter code, Z80X64Layout layout,
    Z80NativeInstruction instruction, int[] extraCycles, ulong ratio, int cycleShift, List<int> faults)
{
    private readonly Z80X64ContextEmitter _context = new(code, layout);
    private bool _eaReady;
    private void Extra(int opcode)
    {
        int cost = unchecked((int)((uint)extraCycles[opcode] * ratio >> cycleShift));
        if (cost != 0) _context.AddInt(F.Cycles, cost);
    }
    private void Call(int function, R address = R.R15, R value = R.R8, R result = R.R8)
    {
        _context.FlushHostCycles();
        code.Push(R.Rcx); code.Push(R.Rdx); code.Push(R.R8); code.Push(R.R9); code.Push(R.R10); code.Push(R.R11); code.Push(R.Rax);
        int shadow = 32 + ((8 - code.StackBytes) & 15); code.AllocateStack(shadow);
        code.Move(address, R.Rcx, W.Dword);
        if (function is 1 or 3) code.Move(value, R.Rdx, W.Dword);
        int slot = function == 4 ? Z80X64MemoryMap.OpcodeFunction : Z80X64MemoryMap.ReadFunction + function;
        code.Load(R.R13, slot * 8, R.Rax, W.Qword); code.CallRegister(R.Rax);
        if (function is 0 or 2 or 4)
        {
            int offset = result switch { R.R8 => 32, R.R9 => 24, R.R10 => 16, R.R11 => 8, R.Rax => 0,
                _ => throw new ArgumentOutOfRangeException(nameof(result)) };
            code.Store(R.Rax, R.Rsp, shadow + offset, W.Dword);
        }
        code.ReleaseStack(shadow);
        code.Pop(R.Rax); code.Pop(R.R11); code.Pop(R.R10); code.Pop(R.R9); code.Pop(R.R8); code.Pop(R.Rdx); code.Pop(R.Rcx);
        _context.LoadHostCycles();
        code.Load(R.R13, Z80X64MemoryMap.Fault * 8, R.Rax, W.Dword);
        code.Test(R.Rax, R.Rax, W.Dword); faults.Add(code.ReserveJump(C.NotZero));
    }
    private void DataMemory(bool write, R value = R.R8, R result = R.R8)
    {
        code.Move(R.R15, R.Rax, W.Dword); code.Shift(S.Right, 10, R.Rax, W.Dword); code.BinaryImmediate(A.And, 63, R.Rax, W.Dword);
        code.LoadIndexed(R.R13, R.Rax, 3, (write ? 128 : 64) * 8, R.R11, W.Qword);
        code.Move(R.R15, R.R9, W.Dword); code.BinaryImmediate(A.And, 1023, R.R9, W.Dword);
        code.Test(R.R11, R.R11, W.Qword); int handler = code.ReserveJump(C.Zero);
        code.BinaryImmediate(A.Compare, 65535, R.R15, W.Dword); int outside = code.ReserveJump(C.Above);
        if (write) code.StoreIndexed(value, R.R11, R.R9, 0, 0, W.Byte);
        else { code.MoveImmediate(0, result, W.Dword); code.LoadIndexed(R.R11, R.R9, 0, 0, result, W.Byte); }
        int done = code.ReserveJump(); code.PatchJump(handler, code.Position); code.PatchJump(outside, code.Position);
        Call(write ? 1 : 0, value: value, result: result); code.PatchJump(done, code.Position);
    }
    // R14 contains the full code address. R15 (a pending data EA) is preserved.
    private void ReadCode()
    {
        code.Move(R.R14, R.Rax, W.Dword); code.Shift(S.Right, 10, R.Rax, W.Dword); code.BinaryImmediate(A.And, 63, R.Rax, W.Dword);
        code.LoadIndexed(R.R13, R.Rax, 3, 0, R.R11, W.Qword);
        code.Move(R.R14, R.R9, W.Dword); code.BinaryImmediate(A.And, 1023, R.R9, W.Dword);
        code.Test(R.R11, R.R11, W.Qword); int missing = code.ReserveJump(C.Zero);
        code.BinaryImmediate(A.Compare, 65535, R.R14, W.Dword); int outside = code.ReserveJump(C.Above);
        code.MoveImmediate(0, R.R8, W.Dword); code.LoadIndexed(R.R11, R.R9, 0, 0, R.R8, W.Byte);
        int done = code.ReserveJump(); code.PatchJump(missing, code.Position); code.PatchJump(outside, code.Position);
        Call(4, R.R14); code.PatchJump(done, code.Position);
    }
    private void Arg8()
    {
        _context.LoadInt(F.Pc, R.R14); _context.AddWord(F.Pc, 1); ReadCode();
    }
    private void Arg16()
    {
        _context.LoadInt(F.Pc, R.R14); _context.AddWord(F.Pc, 2); ReadCode(); code.Move(R.R8, R.R10, W.Dword);
        code.BinaryImmediate(A.Add, 1, R.R14, W.Dword); code.BinaryImmediate(A.And, 65535, R.R14, W.Dword); ReadCode();
        code.Shift(S.Left, 8, R.R8, W.Dword); code.Binary(A.Or, R.R10, R.R8, W.Dword);
    }
    private F IndexPair => instruction.Index == 1 ? F.Ix : instruction.Index == 2 ? F.Iy : F.Hl;
    private void EffectiveAddress()
    {
        if (_eaReady) return; _eaReady = true;
        if (instruction.Index == 0) { _context.LoadWord(F.Hl, R.R15); return; }
        Arg8(); code.MoveExtend(R.R8, R.R8, W.Byte, W.Dword, true);
        _context.LoadWord(IndexPair, R.R15); code.Binary(A.Add, R.R8, R.R15, W.Dword); code.BinaryImmediate(A.And, 65535, R.R15, W.Dword);
        _context.StoreInt(F.Ea, R.R15); _context.StoreWord(F.Wz, R.R15);
    }
    private (F Field, int Part) Register8(int register, bool indexed = true) => register switch
    {
        0 => (F.Bc, 1), 1 => (F.Bc, 0), 2 => (F.De, 1), 3 => (F.De, 0),
        4 => (indexed ? IndexPair : F.Hl, 1), 5 => (indexed ? IndexPair : F.Hl, 0), 7 => (F.Af, 1),
        _ => throw new ArgumentOutOfRangeException(nameof(register))
    };
    private void Load8(int register, R result = R.R8, bool indexed = true)
    {
        if (register == 6) { EffectiveAddress(); DataMemory(false, result: result); return; }
        var r = Register8(register, indexed); _context.LoadByte(r.Field, result, r.Part);
    }
    private void Store8(int register, R value = R.R8, bool indexed = true)
    {
        if (register == 6) { EffectiveAddress(); DataMemory(true, value); return; }
        var r = Register8(register, indexed); _context.StoreByte(r.Field, value, r.Part);
    }
    private F Pair(int pair, bool af = false) => pair switch { 0 => F.Bc, 1 => F.De, 2 => IndexPair, 3 => af ? F.Af : F.Sp,
        _ => throw new ArgumentOutOfRangeException(nameof(pair)) };
    private void IncrementAddress()
    {
        code.BinaryImmediate(A.Add, 1, R.R15, W.Dword); code.BinaryImmediate(A.And, 65535, R.R15, W.Dword);
    }
    private void ReadPair(F pair)
    {
        DataMemory(false); _context.StoreByte(pair, R.R8); IncrementAddress(); DataMemory(false); _context.StoreByte(pair, R.R8, 1);
    }
    private void WritePair(F pair)
    {
        _context.LoadByte(pair, R.R8); DataMemory(true); IncrementAddress(); _context.LoadByte(pair, R.R8, 1); DataMemory(true);
    }
    private void Pop(F pair)
    {
        _context.LoadInt(F.Sp, R.R15); ReadPair(pair); _context.AddWord(F.Sp, 2);
    }
    private void Push(F pair)
    {
        _context.AddWord(F.Sp, -2); _context.LoadInt(F.Sp, R.R15); WritePair(pair);
    }
    private void Lookup(int table, R index, R result, W width = W.Byte)
    {
        if (index == result) throw new ArgumentException("Flag lookup requires a distinct result register.");
        code.Load(R.R13, table * 8, R.R11, W.Qword); code.MoveImmediate(0, result, W.Dword);
        code.LoadIndexed(R.R11, index, width == W.Word ? (byte)1 : (byte)0, 0, result, width);
    }
}
