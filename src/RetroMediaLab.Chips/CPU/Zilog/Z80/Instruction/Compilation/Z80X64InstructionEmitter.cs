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
    public void Emit()
    {
        if (instruction.Family == Z80NativeFamily.IndexedCb)
        { EffectiveAddress(); _context.AddWord(F.Pc, 1); EmitCb(true); }
        else if (instruction.Family == Z80NativeFamily.Cb) EmitCb(false);
        else if (instruction.Family == Z80NativeFamily.Ed) EmitEd();
        else EmitBase();
    }
    private void EmitBase()
    {
        int op = instruction.Opcode, x = op >> 6, y = op >> 3 & 7, z = op & 7, p = y >> 1, q = y & 1;
        if (x == 1)
        {
            if (op == 0x76) { _context.AddWord(F.Pc, -1); _context.StoreImmediate(F.Halt, 1); return; }
            bool halves = y != 6 && z != 6;
            if (y == 6) EffectiveAddress(); Load8(z, indexed: halves); Store8(y, indexed: halves); return;
        }
        if (x == 2) { Load8(z); Alu8(y); return; }
        if (x == 0)
        {
            switch (z)
            {
                case 0:
                    if (y == 1) Exchange(F.Af, F.Af2);
                    else if (y >= 2) Relative(y); return;
                case 1:
                    if (q == 0) { Arg16(); _context.StoreWord(Pair(p), R.R8); }
                    else Add16(IndexPair, Pair(p)); return;
                case 2: LoadIndirect(p, q != 0); return;
                case 3: _context.AddWord(Pair(p), q == 0 ? 1 : -1); return;
                case 4:
                case 5: Load8(y); IncDec(z == 5); Store8(y); return;
                case 6:
                    if (y == 6) EffectiveAddress(); Arg8(); Store8(y); return;
                case 7: Accumulator(y); return;
            }
        }
        switch (z)
        {
            case 0:
                int noReturn = SkipCondition(y); Pop(F.Pc); WzFrom(F.Pc, 0); Extra(op); code.PatchJump(noReturn, code.Position); return;
            case 1:
                if (q == 0) Pop(Pair(p, true));
                else switch (p)
                {
                    case 0: Pop(F.Pc); WzFrom(F.Pc, 0); break;
                    case 1: Exchange(F.Bc, F.Bc2); Exchange(F.De, F.De2); Exchange(F.Hl, F.Hl2); break;
                    case 2: _context.LoadWord(IndexPair, R.R8); _context.StoreInt(F.Pc, R.R8); break;
                    case 3: _context.LoadWord(IndexPair, R.R8); _context.StoreWord(F.Sp, R.R8); break;
                }
                return;
            case 2: JumpCall(y, false); return;
            case 3:
                switch (y)
                {
                    case 0: JumpCall(-1, false); break;
                    case 2:
                        Arg8(); _context.LoadA(R.R9); code.Move(R.R9, R.R15, W.Dword); code.Shift(S.Left, 8, R.R15, W.Dword);
                        code.Binary(A.Or, R.R8, R.R15, W.Dword); Call(3, value: R.R9);
                        code.Move(R.R15, R.R8, W.Dword); code.BinaryImmediate(A.Add, 1, R.R8, W.Dword); _context.StoreByte(F.Wz, R.R8);
                        _context.LoadA(R.R8); _context.StoreByte(F.Wz, R.R8, 1); break;
                    case 3:
                        Arg8(); _context.LoadA(R.R15); code.Shift(S.Left, 8, R.R15, W.Dword); code.Binary(A.Or, R.R8, R.R15, W.Dword);
                        Call(2); _context.StoreA(R.R8); code.Move(R.R15, R.R8, W.Dword); code.BinaryImmediate(A.Add, 1, R.R8, W.Dword); _context.StoreWord(F.Wz, R.R8); break;
                    case 4: ExchangeStack(); break;
                    case 5: Exchange(F.De, F.Hl); break;
                    case 6: _context.StoreImmediate(F.Iff1, 0); _context.StoreImmediate(F.Iff2, 0); break;
                    case 7: _context.StoreImmediate(F.Iff1, 1); _context.StoreImmediate(F.Iff2, 1); _context.StoreImmediate(F.AfterEi, 1); break;
                    default: throw new InvalidOperationException("A prefix reached the base Z80 body.");
                }
                return;
            case 4: JumpCall(y, true); return;
            case 5:
                if (q == 0) Push(Pair(p, true));
                else if (p == 0) JumpCall(-1, true);
                else throw new InvalidOperationException("An index prefix reached the base Z80 body.");
                return;
            case 6: Arg8(); Alu8(y); return;
            case 7:
                Push(F.Pc); _context.StoreImmediate(F.Pc, op & 0x38); code.MoveImmediate(op & 0x38, R.R8, W.Dword); _context.StoreWord(F.Wz, R.R8); return;
        }
    }
    private void Relative(int kind)
    {
        int skipped = -1;
        if (kind == 2)
        {
            code.BinaryMemoryImmediate(A.Subtract, 1, R.R12, layout[F.Bc] + 1, W.Byte);
            _context.LoadByte(F.Bc, R.R8, 1); code.Test(R.R8, R.R8, W.Dword); skipped = code.ReserveJump(C.Zero);
        }
        else if (kind >= 4) skipped = SkipCondition(kind - 4);
        Arg8(); code.MoveExtend(R.R8, R.R8, W.Byte, W.Dword, true);
        _context.LoadWord(F.Pc, R.R9); code.Binary(A.Add, R.R8, R.R9, W.Dword); _context.StoreWord(F.Pc, R.R9); _context.StoreWord(F.Wz, R.R9);
        if (kind != 3) Extra(instruction.Opcode);
        if (skipped >= 0)
        {
            int done = code.ReserveJump(); code.PatchJump(skipped, code.Position); _context.AddWord(F.Pc, 1); code.PatchJump(done, code.Position);
        }
    }
    private void JumpCall(int condition, bool call)
    {
        int skipped = condition >= 0 ? SkipCondition(condition) : -1;
        Arg16();
        if (call)
        {
            _context.StoreInt(F.Ea, R.R8); _context.StoreWord(F.Wz, R.R8); Push(F.Pc);
            _context.LoadInt(F.Ea, R.R8); _context.StoreInt(F.Pc, R.R8);
            if (condition >= 0) Extra(instruction.Opcode);
        }
        else { _context.StoreInt(F.Pc, R.R8); _context.StoreWord(F.Wz, R.R8); }
        if (skipped >= 0)
        {
            int done = code.ReserveJump(); code.PatchJump(skipped, code.Position);
            Arg16(); _context.StoreWord(F.Wz, R.R8); code.PatchJump(done, code.Position);
        }
    }
    private void LoadIndirect(int pair, bool read)
    {
        if (pair < 2)
        {
            F address = pair == 0 ? F.Bc : F.De; _context.LoadWord(address, R.R15);
            if (read) { DataMemory(false); _context.StoreA(R.R8); WzFrom(address, 1); }
            else
            {
                _context.LoadA(R.R8); DataMemory(true);
                _context.LoadWord(address, R.R8); code.BinaryImmediate(A.Add, 1, R.R8, W.Dword); _context.StoreByte(F.Wz, R.R8);
                _context.LoadA(R.R8); _context.StoreByte(F.Wz, R.R8, 1);
            }
            return;
        }
        Arg16(); _context.StoreInt(F.Ea, R.R8); code.Move(R.R8, R.R15, W.Dword);
        if (pair == 2) { if (read) ReadPair(IndexPair); else WritePair(IndexPair); WzFrom(F.Ea, 1, true); }
        else if (read) { DataMemory(false); _context.StoreA(R.R8); WzFrom(F.Ea, 1, true); }
        else
        {
            _context.LoadA(R.R8); DataMemory(true); _context.LoadInt(F.Ea, R.R8); code.BinaryImmediate(A.Add, 1, R.R8, W.Dword); _context.StoreByte(F.Wz, R.R8);
            _context.LoadA(R.R8); _context.StoreByte(F.Wz, R.R8, 1);
        }
    }
    private void ExchangeStack()
    {
        _context.LoadInt(F.Sp, R.R15); DataMemory(false); code.Move(R.R8, R.R10, W.Dword);
        IncrementAddress(); DataMemory(false); code.Shift(S.Left, 8, R.R8, W.Dword); code.Binary(A.Or, R.R10, R.R8, W.Dword); code.Move(R.R8, R.R14, W.Dword);
        _context.LoadInt(F.Sp, R.R15); WritePair(IndexPair); _context.StoreInt(IndexPair, R.R14); _context.StoreWord(F.Wz, R.R14);
    }
    private void Accumulator(int operation)
    {
        if (operation < 4) { _context.LoadA(R.R8); Rotate8(operation, true); _context.StoreA(R.R8); return; }
        if (operation == 4)
        {
            _context.LoadWord(F.Af, R.R8); Lookup(Z80X64MemoryMap.Daa, R.R8, R.R10, W.Word); _context.StoreWord(F.Af, R.R10); return;
        }
        _context.LoadA(R.R8); _context.LoadF(R.R9);
        if (operation == 5)
        {
            code.BinaryImmediate(A.Xor, 255, R.R8, W.Dword); _context.StoreA(R.R8);
            code.BinaryImmediate(A.And, 0xc5, R.R9, W.Dword); code.BinaryImmediate(A.Or, 0x12, R.R9, W.Dword);
        }
        else
        {
            if (operation == 7)
            {
                code.Move(R.R9, R.R10, W.Dword); code.BinaryImmediate(A.And, 1, R.R10, W.Dword);
                code.Move(R.R10, R.Rax, W.Dword); code.Shift(S.Left, 4, R.R10, W.Dword); code.BinaryImmediate(A.Xor, 1, R.Rax, W.Dword);
                code.Binary(A.Or, R.Rax, R.R10, W.Dword);
            }
            else code.MoveImmediate(1, R.R10, W.Dword);
            code.BinaryImmediate(A.And, 0xec, R.R9, W.Dword); code.Binary(A.Or, R.R10, R.R9, W.Dword);
        }
        code.BinaryImmediate(A.And, 0x28, R.R8, W.Dword); code.Binary(A.Or, R.R8, R.R9, W.Dword); _context.StoreF(R.R9);
    }
    private void EmitCb(bool indexed)
    {
        int op = instruction.Opcode, x = op >> 6, y = op >> 3 & 7, z = op & 7;
        if (indexed) DataMemory(false); else Load8(z, indexed: false);
        if (x == 0) Rotate8(y);
        else if (x == 1)
        {
            code.Move(R.R8, R.R10, W.Dword); code.BinaryImmediate(A.And, 1 << y, R.R10, W.Dword);
            Lookup(Z80X64MemoryMap.SzBit, R.R10, R.Rax); code.BinaryImmediate(A.And, ~0x28, R.Rax, W.Dword);
            if (indexed) { _context.LoadInt(F.Ea, R.R10); code.Shift(S.Right, 8, R.R10, W.Dword); }
            else if (z == 6) _context.LoadByte(F.Wz, R.R10, 1);
            else code.Move(R.R8, R.R10, W.Dword);
            code.BinaryImmediate(A.And, 0x28, R.R10, W.Dword); code.Binary(A.Or, R.R10, R.Rax, W.Dword);
            _context.LoadF(R.R9); code.BinaryImmediate(A.And, 1, R.R9, W.Dword); code.BinaryImmediate(A.Or, 16, R.R9, W.Dword);
            code.Binary(A.Or, R.R9, R.Rax, W.Dword); _context.StoreF(R.Rax); return;
        }
        else code.BinaryImmediate(x == 2 ? A.And : A.Or, x == 2 ? ~(1 << y) : 1 << y, R.R8, W.Dword);
        if (indexed) { if (z != 6) Store8(z, indexed: false); DataMemory(true); }
        else Store8(z, indexed: false);
    }
}
