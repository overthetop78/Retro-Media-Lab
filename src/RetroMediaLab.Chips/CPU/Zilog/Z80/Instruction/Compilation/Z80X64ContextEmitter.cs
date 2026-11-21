using RetroMediaLab.Chips.Common.Compilation.X64;
using R = RetroMediaLab.Chips.Common.Compilation.X64.X64Register;
using W = RetroMediaLab.Chips.Common.Compilation.X64.X64Width;
using A = RetroMediaLab.Chips.Common.Compilation.X64.X64Arithmetic;

namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Instruction.Compilation;

internal sealed class Z80X64ContextEmitter(X64Emitter code, Z80X64Layout layout)
{
    public int Offset(Z80X64Field field) => layout[field];
    public void LoadByte(Z80X64Field field, R result, int part = 0) => code.LoadExtend(R.R12, layout[field] + part, result, W.Byte, W.Dword, false);
    public void StoreByte(Z80X64Field field, R value, int part = 0) => code.Store(value, R.R12, layout[field] + part, W.Byte);
    public void LoadWord(Z80X64Field field, R result) => code.LoadExtend(R.R12, layout[field], result, W.Word, W.Dword, false);
    public void StoreWord(Z80X64Field field, R value) => code.Store(value, R.R12, layout[field], W.Word);
    public void LoadInt(Z80X64Field field, R result)
    { if(field==Z80X64Field.Cycles)code.Move(R.Rbx,result,W.Dword);else code.Load(R.R12,layout[field],result,W.Dword); }
    public void StoreInt(Z80X64Field field, R value)
    { if(field==Z80X64Field.Cycles)code.Move(value,R.Rbx,W.Dword);else code.Store(value,R.R12,layout[field],W.Dword); }
    public void StoreImmediate(Z80X64Field field, int value)
    { if(field==Z80X64Field.Cycles)code.MoveImmediate(value,R.Rbx,W.Dword);else code.StoreImmediate(value,R.R12,layout[field],W.Dword); }
    public void AddWord(Z80X64Field field, int value) => code.BinaryMemoryImmediate(A.Add, value, R.R12, layout[field], W.Word);
    public void AddInt(Z80X64Field field, int value)
    { if(field==Z80X64Field.Cycles)code.BinaryImmediate(A.Add,value,R.Rbx,W.Dword);else code.BinaryMemoryImmediate(A.Add,value,R.R12,layout[field],W.Dword); }
    public void LoadHostCycles()=>code.Load(R.R12,layout[Z80X64Field.Cycles],R.Rbx,W.Dword);
    public void FlushHostCycles()=>code.Store(R.Rbx,R.R12,layout[Z80X64Field.Cycles],W.Dword);
    public void LoadA(R result) => LoadByte(Z80X64Field.Af, result, 1);
    public void LoadF(R result) => LoadByte(Z80X64Field.Af, result);
    public void StoreA(R value) => StoreByte(Z80X64Field.Af, value, 1);
    public void StoreF(R value) => StoreByte(Z80X64Field.Af, value);
}
