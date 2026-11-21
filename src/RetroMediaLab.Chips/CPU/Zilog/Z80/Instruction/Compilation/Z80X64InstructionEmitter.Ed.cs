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
    private void EmitEd()
    {
        int op = instruction.Opcode, x = op >> 6, y = op >> 3 & 7, z = op & 7, p = y >> 1, q = y & 1;
        if (x == 2 && y >= 4 && z <= 3) { BlockOperation(z, (y & 1) != 0, y >= 6); return; }
        if (x != 1) return; // Existing core treats undefined ED bytes as NOPs.
        switch (z)
        {
            case 0:
                _context.LoadWord(F.Bc, R.R15); Call(2);
                if (y != 6) Store8(y, indexed: false);
                Lookup(Z80X64MemoryMap.Szp, R.R8, R.Rax); _context.LoadF(R.R9); code.BinaryImmediate(A.And, 1, R.R9, W.Dword);
                code.Binary(A.Or, R.R9, R.Rax, W.Dword); _context.StoreF(R.Rax);
                if (y == 7) WzFrom(F.Bc, 1); return;
            case 1:
                _context.LoadWord(F.Bc, R.R15);
                if (y == 6) code.MoveImmediate(0, R.R8, W.Dword); else Load8(y, indexed: false);
                Call(3); if (y == 7) WzFrom(F.Bc, 1); return;
            case 2: Add16(F.Hl, Pair(p), q == 0 ? -1 : 1); return;
            case 3:
                Arg16(); _context.StoreInt(F.Ea, R.R8); code.Move(R.R8, R.R15, W.Dword);
                if (q == 0) WritePair(Pair(p)); else ReadPair(Pair(p));
                WzFrom(F.Ea, 1, true); return;
            case 4:
                _context.LoadA(R.R8); code.MoveImmediate(0, R.R9, W.Dword); _context.StoreA(R.R9); Alu8(2); return;
            case 5:
                Pop(F.Pc); WzFrom(F.Pc, 0); _context.LoadInt(F.Iff2, R.R8); code.BinaryImmediate(A.And, 255, R.R8, W.Dword); _context.StoreInt(F.Iff1, R.R8); return;
            case 6:
                _context.StoreImmediate(F.Im, (y & 3) switch { 2 => 1, 3 => 2, _ => 0 }); return;
            case 7:
                switch (y)
                {
                    case 0: _context.LoadA(R.R8); _context.StoreInt(F.I, R.R8); break;
                    case 1:
                        _context.LoadA(R.R8); _context.StoreInt(F.R, R.R8); code.BinaryImmediate(A.And, 128, R.R8, W.Dword); _context.StoreInt(F.R2, R.R8); break;
                    case 2:
                    case 3:
                        _context.LoadInt(y == 2 ? F.I : F.R, R.R8);
                        if (y == 3) { code.BinaryImmediate(A.And, 127, R.R8, W.Dword); _context.LoadInt(F.R2, R.R9); code.Binary(A.Or, R.R9, R.R8, W.Dword); }
                        code.BinaryImmediate(A.And, 255, R.R8, W.Dword); _context.StoreA(R.R8);
                        Lookup(Z80X64MemoryMap.Sz, R.R8, R.Rax); _context.LoadF(R.R9); code.BinaryImmediate(A.And, 1, R.R9, W.Dword); code.Binary(A.Or, R.R9, R.Rax, W.Dword);
                        _context.LoadInt(F.Iff2, R.R9); code.Shift(S.Left, 2, R.R9, W.Dword); code.Binary(A.Or, R.R9, R.Rax, W.Dword); _context.StoreF(R.Rax); break;
                    case 4: NibbleRotate(false); break;
                    case 5: NibbleRotate(true); break;
                }
                return;
        }
    }
    private void NibbleRotate(bool left)
    {
        _context.LoadWord(F.Hl, R.R15); DataMemory(false); code.Move(R.R8, R.R14, W.Dword); WzFrom(F.Hl, 1);
        _context.LoadA(R.R8); code.Move(R.R14, R.R9, W.Dword);
        if (left) { code.Shift(S.Left, 4, R.R9, W.Dword); code.BinaryImmediate(A.And, 15, R.R8, W.Dword); }
        else { code.Shift(S.Right, 4, R.R9, W.Dword); code.Shift(S.Left, 4, R.R8, W.Dword); }
        code.Binary(A.Or, R.R9, R.R8, W.Dword); DataMemory(true);
        _context.LoadA(R.R8); code.BinaryImmediate(A.And, 240, R.R8, W.Dword);
        if (left) code.Shift(S.Right, 4, R.R14, W.Dword); else code.BinaryImmediate(A.And, 15, R.R14, W.Dword);
        code.Binary(A.Or, R.R14, R.R8, W.Dword); _context.StoreA(R.R8);
        Lookup(Z80X64MemoryMap.Szp, R.R8, R.Rax); _context.LoadF(R.R9); code.BinaryImmediate(A.And, 1, R.R9, W.Dword);
        code.Binary(A.Or, R.R9, R.Rax, W.Dword); _context.StoreF(R.Rax);
    }
    private void BlockOperation(int kind, bool decrement, bool repeat)
    {
        int delta = decrement ? -1 : 1;
        if (kind == 0)
        {
            _context.LoadWord(F.Hl, R.R15); DataMemory(false); code.Move(R.R8, R.R14, W.Dword);
            _context.LoadWord(F.De, R.R15); DataMemory(true);
            _context.LoadF(R.R10); code.BinaryImmediate(A.And, 0xc1, R.R10, W.Dword);
            _context.LoadA(R.R8); code.Binary(A.Add, R.R14, R.R8, W.Dword);
            code.TestImmediate(2, R.R8, W.Dword); int noY = code.ReserveJump(C.Zero); code.BinaryImmediate(A.Or, 32, R.R10, W.Dword); code.PatchJump(noY, code.Position);
            code.BinaryImmediate(A.And, 8, R.R8, W.Dword); code.Binary(A.Or, R.R8, R.R10, W.Dword);
            _context.AddWord(F.Hl, delta); _context.AddWord(F.De, delta); _context.AddWord(F.Bc, -1);
            _context.LoadWord(F.Bc, R.R8); code.Test(R.R8, R.R8, W.Dword); int zero = code.ReserveJump(C.Zero);
            code.BinaryImmediate(A.Or, 4, R.R10, W.Dword); code.PatchJump(zero, code.Position); _context.StoreF(R.R10);
        }
        else if (kind == 1)
        {
            _context.LoadWord(F.Hl, R.R15); DataMemory(false); code.Move(R.R8, R.R14, W.Dword);
            _context.LoadA(R.R8); code.Move(R.R8, R.R10, W.Dword); code.Binary(A.Subtract, R.R14, R.R10, W.Dword); code.BinaryImmediate(A.And, 255, R.R10, W.Dword);
            _context.AddWord(F.Wz, delta); _context.AddWord(F.Hl, delta); _context.AddWord(F.Bc, -1);
            Lookup(Z80X64MemoryMap.Sz, R.R10, R.Rax); code.BinaryImmediate(A.And, ~0x28, R.Rax, W.Dword);
            code.Binary(A.Xor, R.R14, R.R8, W.Dword); code.Binary(A.Xor, R.R10, R.R8, W.Dword); code.BinaryImmediate(A.And, 16, R.R8, W.Dword); code.Binary(A.Or, R.R8, R.Rax, W.Dword);
            _context.LoadF(R.R9); code.BinaryImmediate(A.And, 1, R.R9, W.Dword); code.BinaryImmediate(A.Or, 2, R.R9, W.Dword); code.Binary(A.Or, R.R9, R.Rax, W.Dword);
            code.TestImmediate(16, R.Rax, W.Dword); int noHalf = code.ReserveJump(C.Zero);
            code.BinaryImmediate(A.Subtract, 1, R.R10, W.Dword); code.BinaryImmediate(A.And, 255, R.R10, W.Dword); code.PatchJump(noHalf, code.Position);
            code.TestImmediate(2, R.R10, W.Dword); int noY = code.ReserveJump(C.Zero); code.BinaryImmediate(A.Or, 32, R.Rax, W.Dword); code.PatchJump(noY, code.Position);
            code.BinaryImmediate(A.And, 8, R.R10, W.Dword); code.Binary(A.Or, R.R10, R.Rax, W.Dword);
            _context.LoadWord(F.Bc, R.R8); code.Test(R.R8, R.R8, W.Dword); int zero = code.ReserveJump(C.Zero);
            code.BinaryImmediate(A.Or, 4, R.Rax, W.Dword); code.PatchJump(zero, code.Position); _context.StoreF(R.Rax);
        }
        else BlockPort(kind == 2, delta);
        if (!repeat) return;
        if (kind < 2) _context.LoadWord(F.Bc, R.R8); else _context.LoadByte(F.Bc, R.R8, 1);
        code.Test(R.R8, R.R8, W.Dword); int finished = code.ReserveJump(C.Zero); int matched = -1;
        if (kind == 1) { _context.LoadF(R.R8); code.TestImmediate(64, R.R8, W.Dword); matched = code.ReserveJump(C.NotZero); }
        _context.AddWord(F.Pc, -2); if (kind < 2) WzFrom(F.Pc, 1);
        Extra(instruction.Opcode); code.PatchJump(finished, code.Position); if (matched >= 0) code.PatchJump(matched, code.Position);
    }
    private void BlockPort(bool input, int delta)
    {
        if (input)
        {
            _context.LoadWord(F.Bc, R.R15); Call(2); code.Move(R.R8, R.R14, W.Dword); WzFrom(F.Bc, delta);
            Extra(delta > 0 ? 0xa2 : 0xaa);
            code.BinaryMemoryImmediate(A.Subtract, 1, R.R12, layout[F.Bc] + 1, W.Byte);
            _context.LoadWord(F.Hl, R.R15); code.Move(R.R14, R.R8, W.Dword); DataMemory(true);
        }
        else
        {
            _context.LoadWord(F.Hl, R.R15); DataMemory(false); code.Move(R.R8, R.R14, W.Dword);
            code.BinaryMemoryImmediate(A.Subtract, 1, R.R12, layout[F.Bc] + 1, W.Byte); WzFrom(F.Bc, delta);
            _context.LoadWord(F.Bc, R.R15); code.Move(R.R14, R.R8, W.Dword); Call(3);
        }
        _context.AddWord(F.Hl, delta); _context.LoadByte(F.Bc, R.R10, 1); Lookup(Z80X64MemoryMap.Sz, R.R10, R.Rax);
        if (input)
        {
            _context.LoadByte(F.Bc, R.R8); code.BinaryImmediate(A.Add, delta, R.R8, W.Dword); code.BinaryImmediate(A.And, 255, R.R8, W.Dword);
        }
        else _context.LoadByte(F.Hl, R.R8);
        code.Binary(A.Add, R.R14, R.R8, W.Dword);
        code.TestImmediate(128, R.R14, W.Dword); int positive = code.ReserveJump(C.Zero); code.BinaryImmediate(A.Or, 2, R.Rax, W.Dword); code.PatchJump(positive, code.Position);
        code.TestImmediate(256, R.R8, W.Dword); int noCarry = code.ReserveJump(C.Zero); code.BinaryImmediate(A.Or, 17, R.Rax, W.Dword); code.PatchJump(noCarry, code.Position);
        code.BinaryImmediate(A.And, 7, R.R8, W.Dword); code.Binary(A.Xor, R.R10, R.R8, W.Dword);
        code.Move(R.Rax, R.R14, W.Dword); Lookup(Z80X64MemoryMap.Szp, R.R8, R.Rax); code.BinaryImmediate(A.And, 4, R.Rax, W.Dword);
        code.Binary(A.Or, R.R14, R.Rax, W.Dword); _context.StoreF(R.Rax);
    }
}
