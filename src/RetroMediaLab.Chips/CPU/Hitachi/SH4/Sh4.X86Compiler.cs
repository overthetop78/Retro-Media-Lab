#define BLOCK_LINKING

using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using RetroMediaLab.Chips.Common.Compilation.X64;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

using static X64Register;
using static X64Width;
using static Sh4BlockEndType;
using static Sh4CanonicalParameterType;

#if !SH4_DYNAREC_NONE
internal sealed partial class Sh4X86RegisterAllocator
{
    private static readonly int[] GeneralRegisters = [(int)Rbx, (int)Rbp, (int)Rsi, (int)Rdi, -1];
    internal static readonly int[] FloatRegisters = [7, 6, 5, 4, -1];

    internal partial void Allocate(Sh4RuntimeBlockInfo block) => Allocate(block, GeneralRegisters, FloatRegisters);
    protected override partial void Preload(Sh4Register guestRegister, int hostRegister) =>
        _compiler.PreloadRegister(guestRegister, (X64Register)hostRegister);
    protected override partial void WriteBack(Sh4Register guestRegister, int hostRegister) =>
        _compiler.WriteBackRegister(guestRegister, (X64Register)hostRegister);
    protected override partial void PreloadFloat(Sh4Register guestRegister, int hostRegister) =>
        _compiler.PreloadFloat(guestRegister, hostRegister);
    protected override partial void WriteBackFloat(Sh4Register guestRegister, int hostRegister) =>
        _compiler.WriteBackFloat(guestRegister, hostRegister);
}

internal sealed class Sh4X86RuntimeBlockInfo(
    Sh4BlockManager manager, Sh4Context context, Sh4CodeBuffer codeBuffer) : Sh4RuntimeBlockInfo(manager)
{
    public override uint Relink()
    {
        var compiler = new Sh4X86Compiler(context, codeBuffer, Code + RelinkOffset);
        uint codeSize = compiler.RelinkBlock(this);
        return codeSize;
    }
}

internal sealed unsafe partial class Sh4X86Compiler
{
    private static readonly nuint NoOperation = nuint.MaxValue;
    private static readonly float[] ThawRegisters = GC.AllocateArray<float>(7, pinned: true);
    private static readonly uint[] JumpStack = GC.AllocateArray<uint>(2, pinned: true);
    private static readonly Sh4UnwindInfo Unwinder = new();
    internal static nuint MainLoopAddress { get; set; }
    internal static nuint FailedToFindBlockAddress { get; private set; }
    private static nuint _interruptScheduler;
    private static nuint _noUpdate;
    private static nuint _linkConditionalNext;
    private static nuint _linkConditionalBranch;
    private static nuint _linkGeneric;
    private static nuint _blockCheckFailed;

    private nuint PositionAddress => WritableCode + (nuint)Emitter.Position;
    private bool MmuEnabled => Context.Mmu.Enabled;
    private nuint PcAddress => Context.NativeAddress + Sh4ContextLayout.Pc;
    private nuint CycleCounterAddress => Context.NativeAddress + Sh4ContextLayout.CycleCounter;
    private nuint RunningAddress => Context.NativeAddress + Sh4ContextLayout.CpuRunning;
    private nuint JumpAddress => Context.NativeAddress + Sh4ContextLayout.DynamicPc;
    private nuint TAddress => RegisterAddress(Sh4Register.reg_sr_T);
    private nuint StatusAddress => RegisterAddress(Sh4Register.reg_sr_status);
    private nuint RegisterAddress(Sh4Register register) => Context.NativeAddress + Sh4Shil.GetRegisterOffset(register);
    private nuint RegisterAddress(Sh4ShilParameter parameter) => RegisterAddress(parameter.Register);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void RaiseException(uint pc, uint exception) =>
        Sh4Context.Active.RaiseException(pc, (Sh4ExceptionEvent)exception);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static int UpdateInterrupts() => Sh4Context.Active.Interrupts.Update();

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static nuint GetCodeByVirtualAddress(uint address) =>
        Sh4RecompilerDriver.Current.GetCodeByVirtualAddress(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static nuint LinkBlock(nuint code, uint dynamicProgramCounter) =>
        Sh4RecompilerDriver.Current.LinkBlock(code, dynamicProgramCounter);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static nuint FindMissingBlock(uint pc) => Sh4RecompilerDriver.Current.FindMissingBlock(pc);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static nuint BlockCheckFailed(uint address) => Sh4RecompilerDriver.Current.OnBlockCheckFailed(address);

    private partial void AlignStack(int amount)
    {
        if (OperatingSystem.IsWindows())
            return;
        if (amount > 0)
            RegisterImmediate(X64Arithmetic.Add, amount, Rsp, Dword);
        else
            RegisterImmediate(X64Arithmetic.Subtract, -amount, Rsp, Dword);
        Unwinder.AllocateStackAt(PositionAddress, -amount);
    }

    internal partial void Compile(Sh4RuntimeBlockInfo block, bool forceChecks, bool optimise)
    {
        Debug.WriteLine($"X86 compiling {block.Address:x8} to 0x{CodeBuffer.Current:x}");
        _currentOperationIndex = NoOperation;
        Unwinder.Start(PositionAddress);
        RecordInheritedProlog();
        CheckBlock(forceChecks, block);
        if (MmuEnabled && block.HasFpuOperation)
        {
            MoveFromMemory(Rax, StatusAddress);
            Emitter.Append([0xA9]);
            AppendDword(0x8000);
            int fpuEnabled = ReserveShortJump(X64Condition.Zero);
            EmitPushImmediate((uint)Sh4ExceptionEvent.FpuDisabled);
            EmitPushImmediate(block.VirtualAddress);
            CallAbsolute((nuint)(delegate* unmanaged[Cdecl]<uint, uint, void>)&RaiseException);
            RegisterImmediate(X64Arithmetic.Add, 8, Rsp, Dword);
            MoveFromMemory(Rcx, PcAddress);
            JumpAbsolute(_noUpdate);
            Bind(fpuEnabled);
        }

        MoveFromMemory(Rax, CycleCounterAddress);
        Emitter.Test(Rax, Rax, Dword);
        int noUpdate = ReserveShortJump(X64Condition.Greater);
        MoveImmediate(Rcx, block.VirtualAddress);
        CallAbsolute(_interruptScheduler);
        Bind(noUpdate);
        MemoryImmediate(X64Arithmetic.Subtract, CycleCounterAddress, block.GuestCycles, Dword);

        _registers.Allocate(block);
        for (_currentOperationIndex = 0; _currentOperationIndex < (nuint)block.Opcodes.Count; _currentOperationIndex++)
        {
            Sh4ShilOpcode opcode = block.Opcodes[(int)_currentOperationIndex];
            _registers.BeginOperation(opcode, (int)_currentOperationIndex);
            GenerateOpcode(block, optimise, opcode);
            _registers.EndOperation(opcode);
        }
        _registers.Cleanup();
        _currentOperationIndex = NoOperation;

        block.RelinkOffset = (uint)Emitter.Position;
        block.RelinkData = 0;
        RelinkBlock(block);
        block.Code = WritableCode;
        block.HostCodeSize = (uint)Emitter.Position;
        EndUnwind(0);
        Publish();
        CodeBuffer.Advance((uint)Emitter.Position);
    }

    internal partial uint RelinkBlock(Sh4RuntimeBlockInfo block)
    {
        int startPosition = Emitter.Position;
#if !BLOCK_LINKING
        switch (block.BlockType)
        {
            case BET_StaticJump:
            case BET_StaticCall:
                MoveImmediate(Rcx, block.BranchAddress);
                break;

            case BET_Cond_0:
            case BET_Cond_1:
                MoveImmediate(Rcx, block.NextAddress);
                MemoryImmediate(X64Arithmetic.Compare, block.HasConditionalJump ? JumpAddress : TAddress,
                    (uint)block.BlockType & 1, Dword);
                int notTaken = ReserveShortJump(X64Condition.NotZero);
                MoveImmediate(Rcx, block.BranchAddress);
                Bind(notTaken);
                break;

            case BET_DynamicJump:
            case BET_DynamicCall:
            case BET_DynamicRet:
                MoveFromMemory(Rcx, JumpAddress);
                break;

            case BET_DynamicIntr:
            case BET_StaticIntr:
                if (block.BlockType == BET_DynamicIntr)
                {
                    MoveFromMemory(Rcx, JumpAddress);
                    MoveToMemory(PcAddress, Rcx);
                }
                else
                    MoveMemoryImmediate(PcAddress, block.NextAddress, Dword);
                CallAbsolute((nuint)(delegate* unmanaged[Cdecl]<int>)&UpdateInterrupts);
                MoveFromMemory(Rcx, PcAddress);
                break;

            default:
                Sh4HostRuntime.Die("Invalid block end type");
                break;
        }
        JumpAbsolute(_noUpdate);
#else
        switch (block.BlockType)
        {
            case BET_Cond_0:
            case BET_Cond_1:
                MemoryImmediate(X64Arithmetic.Compare, block.HasConditionalJump ? JumpAddress : TAddress,
                    (uint)block.BlockType & 1, Dword);
                if (MmuEnabled)
                {
                    MoveImmediate(Rcx, block.BranchAddress);
                    MoveImmediate(Rax, block.NextAddress);
                    Emitter.Append([0x0F, 0x45, 0xC8]);
                    JumpAbsolute(_noUpdate);
                }
                else
                {
                    int noBranch = ReserveShortJump(X64Condition.NotZero);
                    if (block.Branch is not null)
                        JumpAbsolute(CodeBuffer.ToExecutable(block.Branch.Code), forceNear: true);
                    else
                        CallAbsolute(_linkConditionalBranch);
                    Bind(noBranch);
                    if (block.Next is not null)
                        JumpAbsolute(CodeBuffer.ToExecutable(block.Next.Code), forceNear: true);
                    else
                        CallAbsolute(_linkConditionalNext);
                    Emitter.Append([0x90, 0x90, 0x90, 0x90, 0x90, 0x90]);
                }
                break;

            case BET_DynamicRet:
            case BET_DynamicCall:
            case BET_DynamicJump:
                MoveFromMemory(Rcx, JumpAddress);
                JumpAbsolute(_noUpdate);
                break;

            case BET_StaticCall:
            case BET_StaticJump:
                if (!MmuEnabled)
                {
                    if (block.Branch is not null)
                        JumpAbsolute(CodeBuffer.ToExecutable(block.Branch.Code), forceNear: true);
                    else
                        CallAbsolute(_linkGeneric);
                    Emitter.Append([0x90, 0x90, 0x90, 0x90, 0x90]);
                }
                else
                {
                    MoveImmediate(Rcx, block.BranchAddress);
                    JumpAbsolute(_noUpdate);
                }
                break;

            case BET_StaticIntr:
            case BET_DynamicIntr:
                if (block.BlockType == BET_StaticIntr)
                {
                    MoveMemoryImmediate(PcAddress, block.NextAddress, Dword);
                }
                else
                {
                    MoveFromMemory(Rax, JumpAddress);
                    MoveToMemory(PcAddress, Rax);
                }
                CallAbsolute((nuint)(delegate* unmanaged[Cdecl]<int>)&UpdateInterrupts);
                MoveFromMemory(Rcx, PcAddress);
                JumpAbsolute(_noUpdate);
                break;

            default:
                Sh4HostRuntime.Die("Invalid block end type");
                break;
        }
#endif
        Publish();
        return (uint)(Emitter.Position - startPosition);
    }

    internal partial void AddCanonicalParameter(Sh4ShilOpcode opcode, Sh4ShilParameter parameter,
        Sh4CanonicalParameterType type)
    {
        switch (type)
        {
            case UInt32:
            case Float32:
                if (parameter.IsRegister)
                {
                    if (_registers.IsGeneralAllocated(parameter))
                        PushRegister(_registers.MapRegister(parameter));
                    else
                    {
                        RegisterImmediate(X64Arithmetic.Subtract, 4, Rsp, Dword);
                        Emitter.Append([0xF3, 0x0F, 0x11, (byte)(0x04 | _registers.MapXRegister(parameter) << 3), 0x24]);
                    }
                }
                else if (parameter.IsImmediate)
                    EmitPushImmediate(parameter.Immediate);
                else
                    Sh4HostRuntime.Die("invalid combination");
                RecordCanonicalPush();
                break;

            case RegisterPointer:
                Sh4HostRuntime.Verify(parameter.IsRegister, "param.is_reg()");
                EmitPushImmediate((uint)RegisterAddress(parameter));
                RecordCanonicalPush();
                break;

            case ContextPointer:
                EmitPushImmediate((uint)Context.NativeAddress);
                RecordCanonicalPush();
                break;

            case UInt64LowReturn:
            case UInt32Return:
                Emitter.Move(Rax, _registers.MapRegister(parameter), Dword);
                break;

            case UInt64HighReturn:
                Emitter.Move(Rdx, _registers.MapRegister(parameter), Dword);
                break;

            case Float32Return:
                Emitter.Append([0xD9]);
                AppendAbsoluteOperand(3, RegisterAddress(parameter));
                EmitAbsoluteSse(0xF3, 0x10, _registers.MapXRegister(parameter), RegisterAddress(parameter));
                break;
        }
    }

    private void RecordCanonicalPush()
    {
        _canonicalStackSize += 4;
        Unwinder.AllocateStackAt(PositionAddress, 4);
    }

    internal partial void FinishCanonicalCall(Sh4ShilOpcode opcode)
    {
        RegisterImmediate(X64Arithmetic.Add, (int)_canonicalStackSize, Rsp, Dword);
        Unwinder.AllocateStackAt(PositionAddress, -(int)_canonicalStackSize);
    }

    private partial void FreezeXmm()
    {
        if (_currentOperationIndex == NoOperation)
            return;
        fixed (float* thaw = ThawRegisters)
        {
            float* slot = (float*)(((nuint)thaw + 15) & ~(nuint)15);
            foreach (int register in Sh4X86RegisterAllocator.FloatRegisters)
            {
                if (register == -1)
                    break;
                if (_registers.IsMapped(register, (int)_currentOperationIndex))
                    EmitAbsoluteSse(0xF3, 0x11, register, (nuint)slot++);
            }
        }
    }

    private partial void ThawXmm()
    {
        if (_currentOperationIndex == NoOperation)
            return;
        fixed (float* thaw = ThawRegisters)
        {
            float* slot = (float*)(((nuint)thaw + 15) & ~(nuint)15);
            foreach (int register in Sh4X86RegisterAllocator.FloatRegisters)
            {
                if (register == -1)
                    break;
                if (_registers.IsMapped(register, (int)_currentOperationIndex))
                    EmitAbsoluteSse(0xF3, 0x10, register, (nuint)slot++);
            }
        }
    }


    internal partial void GenerateMainLoop()
    {
        Unwinder.Start(PositionAddress);
        foreach (X64Register register in new[] { Rsi, Rdi, Rbp, Rbx })
        {
            PushRegister(register);
            Unwinder.PushRegister((uint)Emitter.Position, (int)register);
        }
        if (!OperatingSystem.IsWindows())
        {
            RegisterImmediate(X64Arithmetic.Subtract, 12, Rsp, Dword);
            Unwinder.AllocateStack((uint)Emitter.Position, 12);
        }
        Unwinder.EndProlog((uint)Emitter.Position);

        fixed (uint* stack = JumpStack)
            MoveToMemory((nuint)stack, Rsp);

        int longJump = Emitter.Position;
        MoveFromMemory(Rcx, PcAddress);
        int noUpdate = Emitter.Position;
        MoveFromMemory(Rdx, RunningAddress);
        RegisterImmediate(X64Arithmetic.Compare, 0, Rdx, Dword);
        int cleanup = ReserveShortJump(X64Condition.Zero);
        if (!MmuEnabled)
        {
            Emitter.Move(Rcx, Rsi, Dword);
            MoveImmediate(Rax, (uint)Context.RecompilerControlBlock);
            RegisterImmediate(X64Arithmetic.And, Sh4ContextLayout.FpcbMask << 1, Rcx, Dword);
            Emitter.Append([0xFF, 0x24, 0x48]);
        }
        else
        {
            MoveToMemory(PcAddress, Rcx);
            CallAbsolute((nuint)(delegate* unmanaged[Fastcall]<uint, nuint>)&GetCodeByVirtualAddress);
            Emitter.JumpRegister(Rax);
        }

        Bind(cleanup);
        MoveToMemory(PcAddress, Rcx);
        if (!OperatingSystem.IsWindows())
            RegisterImmediate(X64Arithmetic.Add, 12, Rsp, Dword);
        foreach (X64Register register in new[] { Rbx, Rbp, Rdi, Rsi })
            Emitter.Append([(byte)(0x58 + (int)register)]);
        Emitter.Return();

        int doIteration = Emitter.Position;
        RegisterImmediate(X64Arithmetic.Add, 4, Rsp, Dword);
        MoveFromMemory(Rcx, PcAddress);
        JumpAbsolute(ExecutableAt(noUpdate));

        int sharedLink = Emitter.Position;
        Emitter.Append([0x59]);
        RegisterImmediate(X64Arithmetic.Subtract, 5, Rcx, Dword);
        CallAbsolute((nuint)(delegate* unmanaged[Fastcall]<nuint, uint, nuint>)&LinkBlock);
        Emitter.JumpRegister(Rax);
        EndUnwind(0);

        Unwinder.Start(PositionAddress);
        int startOffset = Emitter.Position;
        Unwinder.EndProlog(0);
        int interruptScheduler = Emitter.Position;
        MemoryImmediate(X64Arithmetic.Add, CycleCounterAddress, (uint)Sh4Scheduler.TimeSlice, Dword);
        MoveToMemory(PcAddress, Rcx);
        CallAbsolute(Sh4RecompilerDriver.UpdateSystemInterruptsEntry);
        RegisterImmediate(X64Arithmetic.Compare, 0, Rax, Dword);
        JumpAbsolute(ExecutableAt(doIteration), X64Condition.NotZero);
        Emitter.Return();

        int linkConditionalNext = Emitter.Position;
        MoveImmediate(Rdx, 0);
        JumpAbsolute(ExecutableAt(sharedLink));
        int linkConditionalBranch = Emitter.Position;
        MoveImmediate(Rdx, 1);
        JumpAbsolute(ExecutableAt(sharedLink));
        int linkGeneric = Emitter.Position;
        MoveFromMemory(Rdx, JumpAddress);
        JumpAbsolute(ExecutableAt(sharedLink));
        GenerateMemoryHandlers();
        EndUnwind(startOffset);

        Unwinder.Start(PositionAddress);
        startOffset = Emitter.Position;
        RecordInheritedProlog();

        int failedToFindBlock = Emitter.Position;
        if (MmuEnabled)
            CallAbsolute(Sh4RecompilerDriver.FindMissingBlockAtProgramCounterEntry);
        else
        {
            Emitter.Move(Rsi, Rcx, Dword);
            CallAbsolute((nuint)(delegate* unmanaged[Fastcall]<uint, nuint>)&FindMissingBlock);
        }
        Emitter.JumpRegister(Rax);

        int blockCheckFailed = Emitter.Position;
        CallAbsolute((nuint)(delegate* unmanaged[Fastcall]<uint, nuint>)&BlockCheckFailed);
        if (MmuEnabled)
        {
            RegisterImmediate(X64Arithmetic.Compare, 0, Rax, Dword);
            int jumpBlock = ReserveShortJump(X64Condition.NotZero);
            MoveFromMemory(Rcx, PcAddress);
            JumpAbsolute(ExecutableAt(noUpdate));
            Bind(jumpBlock);
        }
        Emitter.JumpRegister(Rax);

        int handleException = Emitter.Position;
        fixed (uint* stack = JumpStack)
            MoveFromMemory(Rsp, (nuint)stack);
        JumpAbsolute(ExecutableAt(longJump));
        EndUnwind(startOffset);

        Publish();
        MainLoopAddress = ExecutableAt(0);
        FailedToFindBlockAddress = ExecutableAt(failedToFindBlock);
        _interruptScheduler = ExecutableAt(interruptScheduler);
        _noUpdate = ExecutableAt(noUpdate);
        _linkConditionalNext = ExecutableAt(linkConditionalNext);
        _linkConditionalBranch = ExecutableAt(linkConditionalBranch);
        _linkGeneric = ExecutableAt(linkGeneric);
        _blockCheckFailed = ExecutableAt(blockCheckFailed);
        ExceptionHandlerAddress = ExecutableAt(handleException);
        CodeBuffer.Advance((uint)Emitter.Position);
    }

    private partial bool GenerateReadMemoryImmediate(Sh4ShilOpcode opcode, Sh4RuntimeBlockInfo block)
    {
        if (!opcode.Source1.IsImmediate)
            return false;
        nuint pointer = 0;
        bool ram = false;
        uint address = 0;
        if (!Sh4RecompilerDriver.Current.TryReadMemoryImmediate(opcode.Source1.Immediate, (int)opcode.Size,
                ref pointer, ref ram, ref address, block))
            return false;

        Sh4ShilParameter destination = opcode.Destination;
        if (ram)
        {
            switch (opcode.Size)
            {
                case 1:
                case 2:
                    if (_registers.IsGeneralAllocated(destination))
                    {
                        X64Register register = _registers.MapRegister(destination);
                        Emitter.Append([0x0F, opcode.Size == 1 ? (byte)0xBE : (byte)0xBF]);
                        AppendAbsoluteOperand((int)register, pointer);
                    }
                    else
                    {
                        Emitter.Append([0x0F, opcode.Size == 1 ? (byte)0xBE : (byte)0xBF]);
                        AppendAbsoluteOperand((int)Rax, pointer);
                        MoveToMemory(RegisterAddress(destination), Rax);
                    }
                    break;
                case 4:
                    if (_registers.IsGeneralAllocated(destination))
                        MoveFromMemory(_registers.MapRegister(destination), pointer);
                    else if (_registers.IsFloatAllocated(destination))
                        EmitAbsoluteSse(0x66, 0x6E, _registers.MapXRegister(destination), pointer);
                    else
                    {
                        MoveFromMemory(Rax, pointer);
                        MoveToMemory(RegisterAddress(destination), Rax);
                    }
                    break;
                case 8:
                    if (destination.Count == 2 && _registers.IsFloatAllocated(destination))
                    {
                        EmitAbsoluteSse(0x66, 0x6E, _registers.MapXRegister(destination), pointer);
                        EmitAbsoluteSse(0x66, 0x6E, _registers.MapXRegister(destination, 1), pointer + 4);
                    }
                    else
                    {
                        EmitAbsoluteSse(0xF3, 0x7E, 0, pointer);
                        EmitAbsoluteSse(0x66, 0xD6, 0, RegisterAddress(destination));
                    }
                    break;
                default:
                    Sh4HostRuntime.Die("Invalid immediate size");
                    break;
            }
        }
        else if (opcode.Size == 8)
        {
            Sh4HostRuntime.Verify(!_registers.IsAllocated(destination), "!regalloc.IsAllocAny(op.rd)");
            MoveImmediate(Rcx, address);
            GenerateCall(pointer);
            MoveToMemory(RegisterAddress(destination), Rax);
            MoveImmediate(Rcx, address + 4);
            GenerateCall(pointer);
            MoveToMemory(RegisterAddress(destination) + 4, Rax);
        }
        else
        {
            MoveImmediate(Rcx, address);
            switch (opcode.Size)
            {
                case 1:
                    GenerateCall(pointer);
                    Emitter.MoveExtend(Rax, Rax, Byte, Dword, signed: true);
                    break;
                case 2:
                    GenerateCall(pointer);
                    Emitter.MoveExtend(Rax, Rax, Word, Dword, signed: true);
                    break;
                case 4:
                    GenerateCall(pointer);
                    break;
                default:
                    Sh4HostRuntime.Die("Invalid immediate size");
                    break;
            }
            HostRegisterToShilParameter(destination, Rax);
        }
        return true;
    }

    private partial bool GenerateWriteMemoryImmediate(Sh4ShilOpcode opcode, Sh4RuntimeBlockInfo block)
    {
        if (!opcode.Source1.IsImmediate)
            return false;
        nuint pointer = 0;
        bool ram = false;
        uint address = 0;
        if (!Sh4RecompilerDriver.Current.TryWriteMemoryImmediate(opcode.Source1.Immediate, (int)opcode.Size,
                ref pointer, ref ram, ref address, block))
            return false;

        Sh4ShilParameter source = opcode.Source2;
        if (ram)
        {
            switch (opcode.Size)
            {
                case 1:
                    if (_registers.IsGeneralAllocated(source))
                    {
                        X64Register register = _registers.MapRegister(source);
                        if ((int)register >= 4)
                        {
                            Emitter.Move(register, Rax, Dword);
                            MoveToMemory(pointer, Rax, Byte);
                        }
                        else
                            MoveToMemory(pointer, register, Byte);
                    }
                    else if (source.IsImmediate)
                        MoveMemoryImmediate(pointer, source.Immediate, Byte);
                    else
                    {
                        MoveFromMemory(Rax, RegisterAddress(source), Byte);
                        MoveToMemory(pointer, Rax, Byte);
                    }
                    break;
                case 2:
                    if (_registers.IsGeneralAllocated(source))
                        MoveToMemory(pointer, _registers.MapRegister(source), Word);
                    else if (source.IsImmediate)
                        MoveMemoryImmediate(pointer, source.Immediate, Word);
                    else
                    {
                        MoveFromMemory(Rcx, RegisterAddress(source), Word);
                        MoveToMemory(pointer, Rcx, Word);
                    }
                    break;
                case 4:
                    if (_registers.IsGeneralAllocated(source))
                        MoveToMemory(pointer, _registers.MapRegister(source));
                    else if (_registers.IsFloatAllocated(source))
                        EmitAbsoluteSse(0x66, 0x7E, _registers.MapXRegister(source), pointer);
                    else if (source.IsImmediate)
                        MoveMemoryImmediate(pointer, source.Immediate, Dword);
                    else
                    {
                        MoveFromMemory(Rcx, RegisterAddress(source));
                        MoveToMemory(pointer, Rcx);
                    }
                    break;
                case 8:
                    if (source.Count == 2 && _registers.IsFloatAllocated(source))
                    {
                        EmitAbsoluteSse(0x66, 0x7E, _registers.MapXRegister(source), pointer);
                        EmitAbsoluteSse(0x66, 0x7E, _registers.MapXRegister(source, 1), pointer + 4);
                    }
                    else
                    {
                        EmitAbsoluteSse(0xF3, 0x7E, 0, RegisterAddress(source));
                        EmitAbsoluteSse(0x66, 0xD6, 0, pointer);
                    }
                    break;
                default:
                    Sh4HostRuntime.Die("Invalid immediate size");
                    break;
            }
        }
        else
        {
            MoveImmediate(Rcx, address);
            ShilParameterToHostRegister(source, Rdx);
            GenerateCall(pointer);
        }
        return true;
    }

    private partial void CheckBlock(bool selfModifyingCodeChecks, Sh4RuntimeBlockInfo block)
    {
        if (MmuEnabled || selfModifyingCodeChecks)
            MoveImmediate(Rcx, block.Address);
        if (MmuEnabled)
        {
            MoveFromMemory(Rax, PcAddress);
            RegisterImmediate(X64Arithmetic.Compare, (int)block.VirtualAddress, Rax, Dword);
            JumpAbsolute(_blockCheckFailed, X64Condition.NotZero);
        }
        if (!selfModifyingCodeChecks)
            return;

        int remaining = (int)block.GuestCodeSize;
        uint address = block.Address;
        while (remaining > 0)
        {
            nuint pointer = (nuint)Sh4RecompilerDriver.Current.AddressSpace.GetMemoryPointer(address, 4);
            if (pointer != 0)
            {
                if (remaining == 2)
                    MemoryImmediate(X64Arithmetic.Compare, pointer, unchecked((uint)*(short*)pointer), Word);
                else
                    MemoryImmediate(X64Arithmetic.Compare, pointer, *(uint*)pointer, Dword);
                JumpAbsolute(_blockCheckFailed, X64Condition.NotZero);
            }

            remaining -= 4;
            address += 4;
        }
    }

    private void RecordInheritedProlog()
    {
        Unwinder.PushRegister(0, (int)Rsi);
        Unwinder.PushRegister(0, (int)Rdi);
        Unwinder.PushRegister(0, (int)Rbp);
        Unwinder.PushRegister(0, (int)Rbx);
        if (!OperatingSystem.IsWindows())
            Unwinder.AllocateStack(0, 12);
        Unwinder.EndProlog(0);
    }

    private void EndUnwind(int start)
    {
        nuint address = PositionAddress;
        nuint size = Unwinder.End((uint)(Emitter.Position - start));
        Emitter.Append(new ReadOnlySpan<byte>((void*)address, (int)size));
    }

    internal static void ClearUnwind() => Unwinder.Clear();
    internal void Publish()
    {
        byte[] code = Emitter.ToArray();
        code.AsSpan().CopyTo(new Span<byte>((void*)WritableCode, code.Length));
    }

    private nuint ExecutableAt(int offset) => CodeBuffer.ToExecutable(WritableCode + (nuint)offset);

    private int ReserveShortJump(X64Condition condition) => Emitter.ReserveShortJump(condition);

    private void Bind(int offset) => Emitter.PatchShortJump(offset, Emitter.Position);

    private void CallAbsolute(nuint target)
    {
        Emitter.Append([0xE8]);
        AppendDword(unchecked((uint)(target - ExecutableAt(Emitter.Position + 4))));
    }

    private void JumpAbsolute(nuint target, X64Condition? condition = null, bool forceNear = false)
    {
        int shortDisplacement = unchecked((int)(target - ExecutableAt(Emitter.Position + 2)));
        if (!forceNear && shortDisplacement is >= sbyte.MinValue and <= sbyte.MaxValue)
        {
            byte opcode = condition is { } shortCondition ? (byte)(0x70 | (int)shortCondition) : (byte)0xEB;
            Emitter.Append([opcode, unchecked((byte)shortDisplacement)]);
            return;
        }
        if (condition is { } cc)
            Emitter.Append([0x0F, (byte)(0x80 | (int)cc)]);
        else
            Emitter.Append([0xE9]);
        AppendDword(unchecked((uint)(target - ExecutableAt(Emitter.Position + 4))));
    }

    private void EmitPushImmediate(uint value)
    {
        if (FitsSignedByte(value))
            Emitter.Append([0x6A, (byte)value]);
        else
        {
            Emitter.Append([0x68]);
            AppendDword(value);
        }
    }

    private void MoveImmediate(X64Register register, uint value) => Emitter.MoveImmediate(value, register, Dword);

    private void PushRegister(X64Register register) => Emitter.Append([(byte)(0x50 + (int)register)]);

    private void AppendDword(uint value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
        Emitter.Append(bytes);
    }

    private void AppendAbsoluteOperand(int register, nuint address)
    {
        Emitter.Append([(byte)(0x05 | register << 3)]);
        AppendDword((uint)address);
    }

    private void MoveFromMemory(X64Register register, nuint address, X64Width width = Dword)
    {
        if (width == Word)
            Emitter.Append([0x66]);
        if (register == Rax)
        {
            Emitter.Append([width == Byte ? (byte)0xA0 : (byte)0xA1]);
            AppendDword((uint)address);
        }
        else
            EmitAbsoluteInteger(width == Byte ? (byte)0x8A : (byte)0x8B, (int)register, address);
    }

    private void MoveToMemory(nuint address, X64Register register, X64Width width = Dword)
    {
        if (width == Word)
            Emitter.Append([0x66]);
        if (register == Rax)
        {
            Emitter.Append([width == Byte ? (byte)0xA2 : (byte)0xA3]);
            AppendDword((uint)address);
        }
        else
            EmitAbsoluteInteger(width == Byte ? (byte)0x88 : (byte)0x89, (int)register, address);
    }

    private void MoveMemoryImmediate(nuint address, uint value, X64Width width)
    {
        if (width == Word)
            Emitter.Append([0x66]);
        Emitter.Append([width == Byte ? (byte)0xC6 : (byte)0xC7]);
        AppendAbsoluteOperand(0, address);
        AppendImmediate(value, width);
    }

    private void MemoryImmediate(X64Arithmetic operation, nuint address, uint value, X64Width width)
    {
        if (width == Word)
            Emitter.Append([0x66]);
        bool shortImmediate = FitsSignedByte(value);
        Emitter.Append([shortImmediate ? (byte)0x83 : (byte)0x81]);
        AppendAbsoluteOperand((int)operation, address);
        AppendImmediate(value, shortImmediate ? Byte : width);
    }

    private void RegisterImmediate(X64Arithmetic operation, int immediate, X64Register register, X64Width width)
    {
        Debug.Assert(width == Dword);
        if (FitsSignedByte(unchecked((uint)immediate)))
            Emitter.Append([0x83, (byte)(0xC0 | (int)operation << 3 | (int)register), unchecked((byte)immediate)]);
        else
        {
            if (register == Rax)
                Emitter.Append([(byte)(0x05 | (int)operation << 3)]);
            else
                Emitter.Append([0x81, (byte)(0xC0 | (int)operation << 3 | (int)register)]);
            AppendDword(unchecked((uint)immediate));
        }
    }

    private static bool FitsSignedByte(uint value) =>
        unchecked((uint)(sbyte)value) == value;

    private void AppendImmediate(uint value, X64Width width)
    {
        switch (width)
        {
            case Byte:
                Emitter.Append([(byte)value]);
                break;
            case Word:
                Emitter.Append([(byte)value, (byte)(value >> 8)]);
                break;
            default:
                AppendDword(value);
                break;
        }
    }
}

internal sealed unsafe class Sh4X86Dynarec : Sh4NativeBackend
{
    internal static Sh4X86Dynarec Instance { get; }

    static Sh4X86Dynarec() => Instance = new();

    private Sh4Context? _context;
    private Sh4CodeBuffer? _codeBuffer;
    private Sh4X86Compiler? _compiler;
    private bool _restarting;

    private Sh4X86Dynarec() => Sh4NativeBackend.Current = this;

    internal override void Initialize(Sh4Context context, Sh4CodeBuffer codeBuffer)
    {
        _context = context;
        _codeBuffer = codeBuffer;
    }

    internal override void HandleException(ref Sh4HostContext context) =>
        context.ProgramCounter = Sh4X86Compiler.ExceptionHandlerAddress;

    private void GenerateMainLoop()
    {
        if (Sh4X86Compiler.MainLoopAddress != 0)
            return;
        _compiler = new Sh4X86Compiler(_context!, _codeBuffer!);
        try
        {
            _compiler.GenerateMainLoop();
        }
        catch (X64EmitterException exception)
        {
            Trace.TraceError("Fatal xbyak error: {0}", exception.Message);
        }
        finally
        {
            _compiler = null;
        }
        Sh4RecompilerDriver.Current.SetFailedToFindBlockHandler(Sh4X86Compiler.FailedToFindBlockAddress);
    }

    internal override void Reset()
    {
        Sh4X86Compiler.MainLoopAddress = 0;
        Sh4X86Compiler.ClearUnwind();
        if (_context!.CpuRunning)
        {
            _context.CpuRunning = false;
            _restarting = true;
        }
        else
            GenerateMainLoop();
    }

    internal override Sh4RuntimeBlockInfo AllocateBlock()
    {
        GenerateMainLoop();
        return new Sh4X86RuntimeBlockInfo(Sh4RecompilerDriver.Current.BlockManager, _context!, _codeBuffer!);
    }

    internal override void MainLoop(nuint controlBlock)
    {
        try
        {
            do
            {
                _restarting = false;
                GenerateMainLoop();
                ((delegate* unmanaged<void>)Sh4X86Compiler.MainLoopAddress)();
                if (_restarting && !Sh4DreamcastRuntime.RestartCpu())
                    _restarting = false;
            }
            while (_restarting);
        }
        catch (Sh4InstructionException exception)
        {
            Trace.TraceError($"SH4ThrownException in mainloop {(uint)exception.Event:x} pc {exception.ProgramCounter:x}");
            throw new InvalidOperationException("Fatal: Unhandled SH4 exception");
        }
    }

    internal override void Compile(Sh4RuntimeBlockInfo block, bool blockCheck, bool optimize)
    {
        _compiler = new Sh4X86Compiler(_context!, _codeBuffer!);
        try
        {
            _compiler.Compile(block, blockCheck, optimize);
        }
        catch (X64EmitterException exception)
        {
            Trace.TraceError("Fatal xbyak error: {0}", exception.Message);
        }
        finally
        {
            _compiler = null;
        }
    }

    internal override bool Rewrite(ref Sh4HostContext context, nuint faultAddress)
    {
        if (_codeBuffer is null)
            return false;
        nuint rewriteAddress = *(uint*)context.StackPointer - 5u;
        var compiler = new Sh4X86Compiler(_context!, _codeBuffer, _codeBuffer.ToWritable(rewriteAddress));
        return compiler.RewriteMemoryAccess(ref context);
    }

    internal override void CanonicalStart(Sh4ShilOpcode opcode) => _compiler!.StartCanonicalCall(opcode);

    internal override void CanonicalParameter(Sh4ShilOpcode opcode, Sh4ShilParameter parameter,
        Sh4CanonicalParameterType parameterType) =>
        _compiler!.AddCanonicalParameter(opcode, parameter, parameterType);

    internal override void CanonicalCall(Sh4ShilOpcode opcode, nuint function) =>
        _compiler!.EmitCanonicalCall(opcode, function);

    internal override void CanonicalFinish(Sh4ShilOpcode opcode) => _compiler!.FinishCanonicalCall(opcode);
}

internal static class Sh4X86BackendInitializer
{
    [ModuleInitializer]
    internal static void Register()
    {
        if (RuntimeInformation.ProcessArchitecture == Architecture.X86)
            _ = Sh4X86Dynarec.Instance;
    }
}
#endif
