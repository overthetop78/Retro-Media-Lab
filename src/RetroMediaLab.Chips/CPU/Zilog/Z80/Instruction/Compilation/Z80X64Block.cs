using RetroMediaLab.Chips.Common.Compilation.X64;
using R = RetroMediaLab.Chips.Common.Compilation.X64.X64Register;
using W = RetroMediaLab.Chips.Common.Compilation.X64.X64Width;
using A = RetroMediaLab.Chips.Common.Compilation.X64.X64Arithmetic;
using C = RetroMediaLab.Chips.Common.Compilation.X64.X64Condition;
using F = RetroMediaLab.Chips.CPU.Zilog.Z80.Instruction.Compilation.Z80X64Field;

namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Instruction.Compilation;

internal enum Z80X64Exit : uint { Boundary, CodeChanged, Interrupt, BusFault }
internal sealed class Z80X64Block : IDisposable
{
    private readonly X64CodeBuffer _buffer;
    private readonly int _linkedOffset;
    public ushort Start { get; }
    public nint LinkedEntry => _buffer.DangerousGetHandle() + _linkedOffset;
    private Z80X64Block(ushort start, X64CodeBuffer buffer, int linkedOffset)
    { Start = start; _buffer = buffer; _linkedOffset = linkedOffset; }
    public static Z80X64Block Compile(ushort start, Func<int, byte> read, Z80X64Layout layout,
        int[][] cycles, ulong ratio, int shift)
    {
        var code = new X64Emitter(); var context = new Z80X64ContextEmitter(code, layout);
        var boundaries = new List<int>(); var changed = new List<int>(); var interrupts = new List<int>(); var faults = new List<int>();
        code.Push(R.Rbx); code.Push(R.Rbp); code.Push(R.R12); code.Push(R.R13); code.Push(R.R14); code.Push(R.R15);
        code.Move(R.Rcx, R.R12, W.Qword); code.Move(R.Rdx, R.R13, W.Qword); code.Move(R.R8, R.Rbp, W.Dword);
        context.LoadHostCycles();
        int linked = code.Position, frame = code.StackBytes; ushort address = start;
        for (int count = 0; count < 16; count++)
        {
            var instruction = Z80NativeDecoder.Decode(address, read, cycles);
            context.LoadInt(F.Cycles, R.R8); code.Binary(A.Compare, R.Rbp, R.R8, W.Dword); boundaries.Add(code.ReserveJump(C.NoCarry));
            context.LoadInt(F.IrqState, R.R8); code.Test(R.R8, R.R8, W.Dword); int noIrq = code.ReserveJump(C.Zero);
            context.LoadInt(F.Iff1, R.R8); code.Test(R.R8, R.R8, W.Dword); int disabled = code.ReserveJump(C.Zero);
            context.LoadInt(F.AfterEi, R.R8); code.Test(R.R8, R.R8, W.Dword); interrupts.Add(code.ReserveJump(C.Zero));
            code.PatchJump(noIrq, code.Position); code.PatchJump(disabled, code.Position);
            context.LoadWord(F.Pc, R.R8); code.BinaryImmediate(A.Compare, address, R.R8, W.Dword); boundaries.Add(code.ReserveJump(C.NotZero));
            foreach (var guard in instruction.Guards)
            {
                code.Load(R.R13, (guard.Address >> 10) * 8, R.R9, W.Qword); code.Test(R.R9, R.R9, W.Qword); changed.Add(code.ReserveJump(C.Zero));
                code.BinaryMemoryImmediate(A.Compare, guard.Value, R.R9, guard.Address & 1023, W.Byte); changed.Add(code.ReserveJump(C.NotZero));
            }
            context.StoreImmediate(F.AfterEi, 0);
            context.LoadInt(F.R, R.R8); code.BinaryImmediate(A.Add, instruction.RefreshIncrement, R.R8, W.Dword);
            code.BinaryImmediate(A.And, 255, R.R8, W.Dword); context.StoreInt(F.R, R.R8);
            context.AddWord(F.Pc, instruction.OpcodeBytes);
            code.StoreImmediate(instruction.LastFetch, R.R13, Z80X64MemoryMap.LastFetch * 8, W.Dword);
            foreach (int value in instruction.CycleCosts)
            {
                int cost = unchecked((int)((uint)value * ratio >> shift));
                if (cost != 0) context.AddInt(F.Cycles, cost);
            }
            new Z80X64InstructionEmitter(code, layout, instruction, cycles[5], ratio, shift, faults).Emit();
            address = instruction.NextAddress;
            if (instruction.EndsBlock) break;
        }
        context.LoadWord(F.Pc, R.R8); code.Load(R.R13, Z80X64MemoryMap.Links * 8, R.R9, W.Qword);
        code.LoadIndexed(R.R9, R.R8, 3, 0, R.R11, W.Qword); code.Test(R.R11, R.R11, W.Qword); boundaries.Add(code.ReserveJump(C.Zero));
        code.JumpRegister(R.R11);
        Exit(Z80X64Exit.Boundary, boundaries); Exit(Z80X64Exit.CodeChanged, changed);
        Exit(Z80X64Exit.Interrupt, interrupts); Exit(Z80X64Exit.BusFault, faults);
        return new(start, X64CodeBuffer.Create(code.ToArray()), linked);
        void Exit(Z80X64Exit reason, List<int> jumps)
        {
            foreach (int jump in jumps) code.PatchJump(jump, code.Position);
            uint cacheKey = ((uint)start << 8) | read(start);
            code.MoveImmediate(unchecked((long)(((ulong)cacheKey << 32) | (uint)reason)), R.Rax, W.Qword);
            context.FlushHostCycles();
            code.StackBytes = frame;
            code.Pop(R.R15); code.Pop(R.R14); code.Pop(R.R13); code.Pop(R.R12); code.Pop(R.Rbp); code.Pop(R.Rbx); code.Return();
        }
    }
    public Z80X64Exit Execute(nint state, nint map, uint target, out uint exitBlock)
    {
        ulong result = _buffer.ExecuteLinked(state, map, unchecked((nint)(nuint)target), 0);
        exitBlock = unchecked((uint)(result >> 32)); return (Z80X64Exit)(uint)result;
    }
    public void Dispose() => _buffer.Dispose();
}
