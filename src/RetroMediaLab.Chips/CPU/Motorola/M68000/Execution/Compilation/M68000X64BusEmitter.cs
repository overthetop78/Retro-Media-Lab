using RetroMediaLab.Chips.Common.Compilation.X64;
using R = RetroMediaLab.Chips.Common.Compilation.X64.X64Register;
using W = RetroMediaLab.Chips.Common.Compilation.X64.X64Width;
using A = RetroMediaLab.Chips.Common.Compilation.X64.X64Arithmetic;
using C = RetroMediaLab.Chips.Common.Compilation.X64.X64Condition;

namespace RetroMediaLab.Chips.CPU.Motorola.M68000.Execution.Compilation;

/// <summary>Calls the bus from a native instruction without redispatching that instruction.</summary>
internal sealed class M68000X64BusEmitter(X64Emitter code, List<int> faults)
{
    public void Emit(int function, int addressAdvance, R value = R.R10, R result = R.R10)
    {
        if ((uint)function > 5) throw new ArgumentOutOfRangeException(nameof(function));
        EmitCall(function, addressAdvance, value, result, null);
    }
    public void EmitControl(Cpu68000NativeControl operation, R argument = R.R14, R result = R.R10) =>
        EmitCall(0, 0, argument, result, operation);
    private void EmitCall(int function, int addressAdvance, R value, R result, Cpu68000NativeControl? control)
    {
        var context = new M68000X64ContextEmitter(code);
        context.FlushHostState();
        code.Push(R.Rcx); code.Push(R.Rdx); code.Push(R.R8); code.Push(R.R9);
        code.Push(R.R10); code.Push(R.R11); code.Push(R.Rax);
        // On Windows x64, RSP must be 16-byte aligned at CALL and provide
        // 32 bytes of shadow space. StackBytes includes the owning block frame.
        int shadow = 32 + ((8 - code.StackBytes) & 15);
        code.AllocateStack(shadow);
        if (control.HasValue)
        {
            code.Move(value, R.Rdx, W.Dword); code.MoveImmediate((uint)control.Value, R.Rcx, W.Dword);
        }
        else
        {
            code.Move(R.R15, R.Rcx, W.Dword);
            if (addressAdvance != 0) code.BinaryImmediate(A.Add, addressAdvance, R.Rcx, W.Dword);
            if (function is 2 or 3)
            {
                code.Move(value, R.Rdx, W.Dword); code.BinaryImmediate(A.And, function == 2 ? 255 : 65535, R.Rdx, W.Dword);
            }
        }
        code.Load(R.R13, control.HasValue ? M68000X64MemoryMap.ControlFunctionOffset : function >= 4
            ? M68000X64MemoryMap.MappedFunctionsOffset + (function - 4) * 8 : M68000X64MemoryMap.BusFunctionsOffset + function * 8, R.Rax, W.Qword);
        code.CallRegister(R.Rax);
        if (function < 2 || function >= 4)
        {
            int savedResult = result switch { R.R8 => 32, R.R9 => 24, R.R10 => 16, R.R11 => 8, R.Rax => 0,
                _ => throw new ArgumentOutOfRangeException(nameof(result)) };
            code.Store(R.Rax, R.Rsp, shadow + savedResult, W.Dword);
        }
        code.ReleaseStack(shadow);
        code.Pop(R.Rax); code.Pop(R.R11); code.Pop(R.R10); code.Pop(R.R9);
        code.Pop(R.R8); code.Pop(R.Rdx); code.Pop(R.Rcx);
        context.InitializeHostState();
        code.Load(R.R13, M68000X64MemoryMap.BusFaultOffset, R.Rax, W.Dword);
        code.Test(R.Rax, R.Rax, W.Dword); faults.Add(code.ReserveJump(C.NotZero));
    }
}

