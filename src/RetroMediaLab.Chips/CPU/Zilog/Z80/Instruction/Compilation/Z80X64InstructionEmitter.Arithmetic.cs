using RetroMediaLab.Chips.Common.Compilation.X64;
using R = RetroMediaLab.Chips.Common.Compilation.X64.X64Register;
using W = RetroMediaLab.Chips.Common.Compilation.X64.X64Width;
using A = RetroMediaLab.Chips.Common.Compilation.X64.X64Arithmetic;
using C = RetroMediaLab.Chips.Common.Compilation.X64.X64Condition;
using S = RetroMediaLab.Chips.Common.Compilation.X64.X64Shift;
using F = RetroMediaLab.Chips.CPU.Zilog.Z80.Instruction.Compilation.Z80X64Field;

namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Instruction.Compilation;

internal sealed partial class Z80X64InstructionEmitter
{
    private void Alu8(int operation)
    {
        if (operation is 4 or 5 or 6)
        {
            _context.LoadA(R.R10);
            code.Binary(operation == 4 ? A.And : operation == 5 ? A.Xor : A.Or, R.R8, R.R10, W.Dword);
            code.BinaryImmediate(A.And, 255, R.R10, W.Dword); _context.StoreA(R.R10);
            Lookup(Z80X64MemoryMap.Szp, R.R10, R.Rax);
            if (operation == 4) code.BinaryImmediate(A.Or, 16, R.Rax, W.Dword);
            _context.StoreF(R.Rax); return;
        }
        _context.LoadInt(F.Af, R.R9); code.BinaryImmediate(A.And, 0xff00, R.R9, W.Dword);
        code.Move(R.R9, R.R10, W.Dword); code.Shift(S.Right, 8, R.R10, W.Dword);
        bool subtract = operation is 2 or 3 or 7;
        code.Binary(subtract ? A.Subtract : A.Add, R.R8, R.R10, W.Dword);
        if (operation is 1 or 3)
        {
            _context.LoadF(R.Rax); code.BinaryImmediate(A.And, 1, R.Rax, W.Dword);
            code.Binary(subtract ? A.Subtract : A.Add, R.Rax, R.R10, W.Dword);
            code.Shift(S.Left, 16, R.Rax, W.Dword); code.Binary(A.Or, R.Rax, R.R9, W.Dword);
        }
        code.BinaryImmediate(A.And, 255, R.R10, W.Dword); code.Binary(A.Or, R.R10, R.R9, W.Dword);
        Lookup(subtract ? Z80X64MemoryMap.Sub : Z80X64MemoryMap.Add, R.R9, R.Rax);
        if (operation == 7)
        {
            code.BinaryImmediate(A.And, ~0x28, R.Rax, W.Dword);
            code.BinaryImmediate(A.And, 0x28, R.R8, W.Dword); code.Binary(A.Or, R.R8, R.Rax, W.Dword);
        }
        _context.StoreF(R.Rax);
        if (operation != 7) _context.StoreA(R.R10);
    }
    private void IncDec(bool decrement)
    {
        code.Move(R.R8, R.R10, W.Dword); code.BinaryImmediate(decrement ? A.Subtract : A.Add, 1, R.R10, W.Dword);
        code.BinaryImmediate(A.And, 255, R.R10, W.Dword);
        Lookup(decrement ? Z80X64MemoryMap.Dec : Z80X64MemoryMap.Inc, R.R10, R.R9);
        _context.LoadF(R.Rax); code.BinaryImmediate(A.And, 1, R.Rax, W.Dword); code.Binary(A.Or, R.R9, R.Rax, W.Dword);
        _context.StoreF(R.Rax); code.Move(R.R10, R.R8, W.Dword);
    }
    private void Rotate8(int kind, bool accumulator = false)
    {
        code.Move(R.R8, R.R10, W.Dword);
        if (kind is 2 or 3) code.BitMemoryImmediate(X64Bit.Test, 0, R.R12, layout[F.Af], W.Dword);
        code.Shift(kind switch { 0 => S.RotateLeft, 1 => S.RotateRight, 2 => S.RotateCarryLeft,
            3 => S.RotateCarryRight, 4 or 6 => S.Left, 5 => S.ArithmeticRight, _ => S.Right }, 1, R.R10, W.Byte);
        code.MoveImmediate(0, R.R9, W.Dword); code.SetCondition(C.Carry, R.R9);
        if (kind == 6) code.BinaryImmediate(A.Or, 1, R.R10, W.Dword);
        code.BinaryImmediate(A.And, 255, R.R10, W.Dword);
        if (accumulator)
        {
            _context.LoadF(R.Rax); code.BinaryImmediate(A.And, 0xc4, R.Rax, W.Dword);
            code.Move(R.R10, R.R8, W.Dword); code.BinaryImmediate(A.And, 0x28, R.R8, W.Dword); code.Binary(A.Or, R.R8, R.Rax, W.Dword);
        }
        else Lookup(Z80X64MemoryMap.Szp, R.R10, R.Rax);
        code.Binary(A.Or, R.R9, R.Rax, W.Dword); _context.StoreF(R.Rax); code.Move(R.R10, R.R8, W.Dword);
    }
    private void Add16(F destination, F source, int carryOperation = 0)
    {
        _context.LoadInt(destination, R.R8); _context.LoadInt(source, R.R9);
        code.Move(R.R8, R.R10, W.Dword);
        code.Binary(carryOperation < 0 ? A.Subtract : A.Add, R.R9, R.R10, W.Dword);
        if (carryOperation != 0)
        {
            _context.LoadF(R.Rax); code.BinaryImmediate(A.And, 1, R.Rax, W.Dword);
            code.Binary(carryOperation < 0 ? A.Subtract : A.Add, R.Rax, R.R10, W.Dword);
        }
        code.Move(R.R8, R.Rax, W.Dword); code.BinaryImmediate(A.Add, 1, R.Rax, W.Dword); _context.StoreWord(F.Wz, R.Rax);
        code.Move(R.R8, R.R14, W.Dword); code.Binary(A.Xor, R.R9, R.R14, W.Dword); code.Binary(A.Xor, R.R10, R.R14, W.Dword);
        code.Shift(S.Right, 8, R.R14, W.Dword); code.BinaryImmediate(A.And, 16, R.R14, W.Dword);
        code.Move(R.R10, R.Rax, W.Dword); code.Shift(S.Right, 16, R.Rax, W.Dword); code.BinaryImmediate(A.And, 1, R.Rax, W.Dword);
        code.Binary(A.Or, R.Rax, R.R14, W.Dword);
        code.Move(R.R10, R.Rax, W.Dword); code.Shift(S.Right, 8, R.Rax, W.Dword);
        code.BinaryImmediate(A.And, carryOperation == 0 ? 0x28 : 0xa8, R.Rax, W.Dword); code.Binary(A.Or, R.Rax, R.R14, W.Dword);
        if (carryOperation == 0)
        {
            _context.LoadF(R.Rax); code.BinaryImmediate(A.And, 0xc4, R.Rax, W.Dword); code.Binary(A.Or, R.Rax, R.R14, W.Dword);
        }
        else
        {
            code.TestImmediate(65535, R.R10, W.Dword); int nonzero = code.ReserveJump(C.NotZero);
            code.BinaryImmediate(A.Or, 64, R.R14, W.Dword); code.PatchJump(nonzero, code.Position);
            if (carryOperation < 0)
            {
                code.BinaryImmediate(A.Or, 2, R.R14, W.Dword);
                code.Move(R.R9, R.Rax, W.Dword); code.Binary(A.Xor, R.R8, R.Rax, W.Dword);
                code.Binary(A.Xor, R.R10, R.R8, W.Dword); code.Binary(A.And, R.R8, R.Rax, W.Dword);
            }
            else
            {
                code.Move(R.R9, R.Rax, W.Dword); code.Binary(A.Xor, R.R8, R.Rax, W.Dword); code.BinaryImmediate(A.Xor, 0x8000, R.Rax, W.Dword);
                code.Binary(A.Xor, R.R10, R.R9, W.Dword); code.Binary(A.And, R.R9, R.Rax, W.Dword);
            }
            code.BinaryImmediate(A.And, 0x8000, R.Rax, W.Dword); code.Shift(S.Right, 13, R.Rax, W.Dword);
            code.Binary(A.Or, R.Rax, R.R14, W.Dword);
        }
        _context.StoreF(R.R14); _context.StoreWord(destination, R.R10);
    }
    private void Exchange(F first, F second)
    {
        _context.LoadInt(first, R.R8); _context.LoadInt(second, R.R9); _context.StoreInt(first, R.R9); _context.StoreInt(second, R.R8);
    }
    private int SkipCondition(int condition)
    {
        int mask = (condition >> 1) switch { 0 => 64, 1 => 1, 2 => 4, _ => 128 };
        _context.LoadF(R.R8); code.TestImmediate(mask, R.R8, W.Dword);
        return code.ReserveJump((condition & 1) == 0 ? C.NotZero : C.Zero);
    }
    private void WzFrom(F field, int delta, bool full = false)
    {
        if (full) _context.LoadInt(field, R.R8); else _context.LoadWord(field, R.R8);
        if (delta != 0) code.BinaryImmediate(A.Add, delta, R.R8, W.Dword); _context.StoreWord(F.Wz, R.R8);
    }
}
