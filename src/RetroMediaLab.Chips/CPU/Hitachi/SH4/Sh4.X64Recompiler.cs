using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using RetroMediaLab.Chips.Common.Compilation.X64;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

using static Sh4BlockEndType;
using static Sh4ShilOperation;
using static X64Register;
using static X64Width;

#if !SH4_DYNAREC_NONE
internal sealed unsafe partial class Sh4X64Recompiler
{
    internal enum MemorySize { S8, S16, S32, S64, Count }
    internal enum MemoryOperation { Read, Write, Count }
    internal enum MemoryType { Fast, StoreQueue, Slow, Count }

    internal static delegate* unmanaged<void> MainLoopEntry;
    internal static delegate* unmanaged<void> HandleExceptionEntry;
    internal static ulong JumpStack;
    internal static readonly nuint[,,] MemoryHandlers = new nuint[(int)MemoryType.Count, (int)MemorySize.Count, (int)MemoryOperation.Count];
    internal static nuint MemoryHandlerStart;
    internal static nuint MemoryHandlerEnd;
    internal static readonly Sh4UnwindInfo Unwinder = new();
    internal static readonly float[]? XmmSave = OperatingSystem.IsWindows() ? null : GC.AllocateArray<float>(4, pinned: true);

    [UnmanagedCallersOnly]
    internal static void BlockCheckFailed(uint pc) => Sh4RecompilerDriver.Current.OnBlockCheckFailed(pc);

    private static void HandleSh4Exception(Sh4Context context, Sh4InstructionException exception, uint pc)
    {
        if ((pc & 1) != 0)
        {
            exception.AdjustForDelaySlot();
            pc--;
        }
        context.RaiseException(pc, exception.Event);
        context.CycleCounter += 4;
        HandleExceptionEntry();
    }

    [UnmanagedCallersOnly]
    internal static void InterpreterFallback(nint contextAddress, ushort opcode, nuint handler, uint pc)
    {
        Sh4Context context = Sh4Context.FromNative(contextAddress);
        try { ((delegate* unmanaged<nint, uint, void>)handler)(contextAddress, opcode); }
        catch (Sh4InstructionException exception) { HandleSh4Exception(context, exception, pc); }
    }

    [UnmanagedCallersOnly]
    internal static void WriteStoreQueueWithMmu(uint address, nint contextAddress, uint pc)
    {
        Sh4Context context = Sh4Context.FromNative(contextAddress);
        try { ((delegate* unmanaged<uint, nint, void>)context.StoreQueueWriter)(address, contextAddress); }
        catch (Sh4InstructionException exception) { HandleSh4Exception(context, exception, pc); }
    }
}

internal sealed unsafe partial class Sh4X64BlockCompiler : Sh4XbyakBaseCompiler
{
    private static readonly X64Register[] CallRegisters = OperatingSystem.IsWindows()
        ? [Rcx, Rdx, R8, R9] : [Rdi, Rsi, Rdx, Rcx];
    private static readonly int[] CallFloatRegisters = [0, 1, 2, 3];
    private static readonly int StackAlignment = OperatingSystem.IsWindows() ? 0x28 : 8;

    private readonly Sh4X64RegisterAllocator _allocator;
    private int _currentOperation;
    protected override Sh4SsaRegisterAllocator Allocator => _allocator;
    protected override X64Register MapRegister(Sh4ShilParameter parameter) => _allocator.MapRegister(parameter);
    protected override int MapXRegister(Sh4ShilParameter parameter, int index = 0) => _allocator.MapXRegister(parameter, index);
    private static bool AllocateFloatPairs => OperatingSystem.IsWindows();

    internal Sh4X64BlockCompiler(Sh4Context context, Sh4CodeBuffer codeBuffer)
        : this(context, codeBuffer, codeBuffer.Current) { }

    internal Sh4X64BlockCompiler(Sh4Context context, Sh4CodeBuffer codeBuffer, nuint codePointer)
        : base(context, codeBuffer, codePointer, archX64: true)
    {
        _allocator = new Sh4X64RegisterAllocator(this, context.Mmu);
    }

    internal void Compile(Sh4RuntimeBlockInfo block, bool forceChecks, bool optimize)
    {
        _currentOperation = -1;
        CheckBlock(forceChecks, block);
        Emitter.AllocateStack(StackAlignment);
        int exitBlock = -1;
        if (Context.Mmu.Enabled && block.HasFpuOperation)
        {
            Address(Rax, Context.NativeAddress + Sh4ContextLayout.SrStatus);
            Emitter.TestMemoryImmediate(0x8000, Rax, 0, Dword);
            int enabled = Emitter.ReserveShortJump(X64Condition.Zero);
            Move(CallRegisters[0], block.VirtualAddress);
            Move(CallRegisters[1], (uint)Sh4ExceptionEvent.FpuDisabled);
            GenerateCall(Sh4Interrupts.RaiseExceptionEntry);
            exitBlock = Emitter.ReserveJump();
            Emitter.PatchShortJump(enabled, Emitter.Position);
        }
        Address(Rax, Context.NativeAddress + Sh4ContextLayout.CycleCounter);
        Emitter.BinaryMemoryImmediate(X64Arithmetic.Subtract, unchecked((int)block.GuestCycles), Rax, 0, Dword);
        _allocator.Allocate(block);
        for (_currentOperation = 0; _currentOperation < block.Opcodes.Count; _currentOperation++)
        {
            Sh4ShilOpcode op = block.Opcodes[_currentOperation];
            _allocator.BeginOperation(op, _currentOperation);
            switch (op.Operation)
            {
                case shop_ifb:
                {
                    if (Context.Mmu.Enabled)
                    {
                        Address(CallRegisters[2], Sh4OpcodeTable.OpcodeDescriptors[op.Source3.Immediate].NativeInterpreterOpcode);
                        Move(CallRegisters[3], unchecked(block.VirtualAddress + (uint)op.GuestOffset - (op.DelaySlot ? 1u : 0u)));
                    }
                    if (op.Source1.Immediate != 0)
                    {
                        Address(Rax, Context.NativeAddress + Sh4ContextLayout.Pc);
                        Store(Rax, op.Source2.Immediate);
                    }
                    Move(CallRegisters[1], op.Source3.Immediate);
                    Address(CallRegisters[0], Context.NativeAddress);
                    if (!Context.Mmu.Enabled)
                        GenerateCall(Sh4OpcodeTable.OpcodeDescriptors[op.Source3.Immediate].NativeInterpreterOpcode);
                    else
                        GenerateCall((nuint)(delegate* unmanaged<nint, ushort, nuint, uint, void>)&Sh4X64Recompiler.InterpreterFallback);
                    break;
                }
                case shop_mov64:
                {
                    Sh4HostRuntime.Verify(op.Destination.IsFloat64, "op.rd.is_r64f()");
                    Sh4HostRuntime.Verify(op.Source1.IsFloat64, "op.rs1.is_r64f()");
                    if (!AllocateFloatPairs)
                    {
                        Address(Rax, ParameterAddress(op.Source1));
                        Emitter.Load(Rax, 0, Rax, Qword);
                        Address(Rcx, ParameterAddress(op.Destination));
                        Emitter.Store(Rax, Rcx, 0, Qword);
                    }
                    else
                    {
                        int rd0 = _allocator.MapXRegister(op.Destination, 0);
                        int rs0 = _allocator.MapXRegister(op.Source1, 0);
                        int rd1 = _allocator.MapXRegister(op.Destination, 1);
                        int rs1 = _allocator.MapXRegister(op.Source1, 1);
                        if (rd0 == rs1)
                        {
                            MoveScalar(0, rd0);
                            MoveScalar(rd0, rs0);
                            MoveScalar(rd1, 0);
                        }
                        else
                        {
                            if (rd0 != rs0) MoveScalar(rd0, rs0);
                            if (rd1 != rs1) MoveScalar(rd1, rs1);
                        }
                    }
                    break;
                }
                case shop_readm:
                    if (!GenerateReadMemoryImmediate(op, block))
                    {
                        ShilParameterToHostRegister(op.Source1, CallRegisters[0]);
                        if (!op.Source3.IsNull)
                        {
                            if (op.Source3.IsImmediate)
                                Emitter.BinaryImmediate(X64Arithmetic.Add, unchecked((int)op.Source3.Immediate), CallRegisters[0], Dword);
                            else if (_allocator.IsGeneralAllocated(op.Source3))
                                Emitter.Binary(X64Arithmetic.Add, _allocator.MapRegister(op.Source3), CallRegisters[0], Dword);
                            else
                            {
                                Address(Rax, ParameterAddress(op.Source3));
                                Emitter.BinaryLoad(X64Arithmetic.Add, Rax, 0, CallRegisters[0], Dword);
                            }
                        }
                        GenerateMmuLookup(block, op, 0);
                        int size = SizeIndex(op.Size);
                        GenerateCall(Sh4X64Recompiler.MemoryHandlers[(int)(optimize ? Sh4X64Recompiler.MemoryType.Fast : Sh4X64Recompiler.MemoryType.Slow), size, 0], Context.Mmu.Enabled);
                        if (!AllocateFloatPairs && size == (int)Sh4X64Recompiler.MemorySize.S64)
                        {
                            Address(Rcx, ParameterAddress(op.Destination));
                            Emitter.Store(Rax, Rcx, 0, Qword);
                        }
                        else
                        {
                            Emitter.Move(Rax, Rcx, Qword);
                            HostRegisterToShilParameter(op.Destination, Rcx);
                        }
                    }
                    break;
                case shop_writem:
                    if (!GenerateWriteMemoryImmediate(op, block))
                    {
                        ShilParameterToHostRegister(op.Source1, CallRegisters[0]);
                        if (!op.Source3.IsNull)
                        {
                            if (op.Source3.IsImmediate)
                                Emitter.BinaryImmediate(X64Arithmetic.Add, unchecked((int)op.Source3.Immediate), CallRegisters[0], Dword);
                            else if (_allocator.IsGeneralAllocated(op.Source3))
                                Emitter.Binary(X64Arithmetic.Add, _allocator.MapRegister(op.Source3), CallRegisters[0], Dword);
                            else
                            {
                                Address(Rax, ParameterAddress(op.Source3));
                                Emitter.BinaryLoad(X64Arithmetic.Add, Rax, 0, CallRegisters[0], Dword);
                            }
                        }
                        GenerateMmuLookup(block, op, 1);
                        if (!AllocateFloatPairs && op.Size == 8)
                        {
                            Address(Rax, ParameterAddress(op.Source2));
                            Emitter.Load(Rax, 0, CallRegisters[1], Qword);
                        }
                        else
                            ShilParameterToHostRegister(op.Source2, CallRegisters[1], Qword);
                        int size = SizeIndex(op.Size);
                        GenerateCall(Sh4X64Recompiler.MemoryHandlers[(int)(optimize ? Sh4X64Recompiler.MemoryType.Fast : Sh4X64Recompiler.MemoryType.Slow), size, 1], Context.Mmu.Enabled);
                    }
                    break;
                case shop_jcond:
                case shop_jdyn:
                case shop_mov32:
                    GenerateBaseOpcode(op);
                    break;
#if !CANONICAL_TEST
                case shop_sync_sr:
                    GenerateCall(Sh4Context.UpdateSrEntry);
                    break;
                case shop_sync_fpscr:
                    Address(CallRegisters[0], Context.NativeAddress);
                    GenerateCall(Sh4Context.UpdateFpscrEntry);
                    break;
                case shop_negc:
                {
                    X64Register rs2 = default;
                    if (op.Source2.IsRegister)
                    {
                        rs2 = _allocator.MapRegister(op.Source2);
                        if (_allocator.MapGeneral(op.Destination) == _allocator.MapGeneral(op.Source2))
                        {
                            Emitter.Move(rs2, Rcx, Dword);
                            rs2 = Rcx;
                        }
                    }
                    X64Register rd = _allocator.MapRegister(op.Destination);
                    if (op.Source1.IsImmediate)
                        Move(rd, op.Source1.Immediate);
                    else if (_allocator.MapGeneral(op.Destination) != _allocator.MapGeneral(op.Source1))
                        Emitter.Move(_allocator.MapRegister(op.Source1), rd, Dword);
                    Emitter.Unary(X64Unary.Negate, rd, Qword);
                    if (op.Source2.IsImmediate)
                        Emitter.BinaryImmediate(X64Arithmetic.Subtract, unchecked((int)op.Source2.Immediate), rd, Qword);
                    else
                        Emitter.Binary(X64Arithmetic.Subtract, rs2, rd, Qword);
                    X64Register rd2 = _allocator.MapRegister(op.Destination2);
                    Emitter.Move(rd, rd2, Qword);
                    Emitter.Shift(X64Shift.Right, 63, rd2, Qword);
                    break;
                }
                case shop_mul_s64:
                    Emitter.MoveExtend(_allocator.MapRegister(op.Source1), Rax, Dword, Qword, true);
                    if (op.Source2.IsRegister)
                        Emitter.MoveExtend(_allocator.MapRegister(op.Source2), Rcx, Dword, Qword, true);
                    else
                        Emitter.MoveImmediate(unchecked((int)op.Source2.Immediate), Rcx, Qword);
                    Emitter.Unary(X64Unary.Multiply, Rcx, Qword);
                    Emitter.Move(Rax, _allocator.MapRegister(op.Destination), Dword);
                    Emitter.Shift(X64Shift.Right, 32, Rax, Qword);
                    Emitter.Move(Rax, _allocator.MapRegister(op.Destination2), Dword);
                    break;
                case shop_pref:
                {
                    int noStoreQueue = -1;
                    if (op.Source1.IsImmediate)
                    {
                        if ((op.Source1.Immediate & 0xFC000000) != 0xE0000000)
                            break;
                        Move(CallRegisters[0], op.Source1.Immediate);
                    }
                    else
                    {
                        X64Register rn;
                        if (_allocator.IsGeneralAllocated(op.Source1))
                            rn = _allocator.MapRegister(op.Source1);
                        else
                        {
                            Address(Rax, ParameterAddress(op.Source1));
                            Emitter.Load(Rax, 0, Rax, Dword);
                            rn = Rax;
                        }
                        Emitter.Move(rn, Rcx, Dword);
                        Emitter.Shift(X64Shift.Right, 26, Rcx, Dword);
                        Emitter.BinaryImmediate(X64Arithmetic.Compare, 0x38, Rcx, Dword);
                        noStoreQueue = Emitter.ReserveShortJump(X64Condition.NotZero);
                        Emitter.Move(rn, CallRegisters[0], Dword);
                    }
                    Address(CallRegisters[1], Context.NativeAddress);
                    if (Context.Mmu.Enabled)
                    {
                        Move(CallRegisters[2], unchecked(block.VirtualAddress + (uint)op.GuestOffset - (op.DelaySlot ? 1u : 0u)));
                        GenerateCall((nuint)(delegate* unmanaged<uint, nint, uint, void>)&Sh4X64Recompiler.WriteStoreQueueWithMmu);
                    }
                    else
                    {
                        Address(Rax, Context.NativeAddress + Sh4ContextLayout.StoreQueueWriter);
                        SaveXmmRegisters();
                        Emitter.CallMemory(Rax, 0);
                        RestoreXmmRegisters();
                    }
                    if (noStoreQueue >= 0)
                        Emitter.PatchShortJump(noStoreQueue, Emitter.Position);
                    break;
                }
                case shop_frswap:
                    Address(Rax, ParameterAddress(op.Source1));
                    Address(Rcx, ParameterAddress(op.Destination));
                    if (Avx512F.IsSupported)
                    {
                        Emitter.LoadAlignedPackedSingle(0, Rax, 0, 512);
                        Emitter.LoadAlignedPackedSingle(1, Rcx, 0, 512);
                        Emitter.StoreAlignedPackedSingle(1, Rax, 0, 512);
                        Emitter.StoreAlignedPackedSingle(0, Rcx, 0, 512);
                    }
                    else if (Avx.IsSupported)
                    {
                        Emitter.LoadAlignedPackedSingle(0, Rax, 0, 256);
                        Emitter.LoadAlignedPackedSingle(1, Rcx, 0, 256);
                        Emitter.StoreAlignedPackedSingle(1, Rax, 0, 256);
                        Emitter.StoreAlignedPackedSingle(0, Rcx, 0, 256);
                        Emitter.LoadAlignedPackedSingle(0, Rax, 32, 256);
                        Emitter.LoadAlignedPackedSingle(1, Rcx, 32, 256);
                        Emitter.StoreAlignedPackedSingle(1, Rax, 32, 256);
                        Emitter.StoreAlignedPackedSingle(0, Rcx, 32, 256);
                    }
                    else
                    {
                        for (int index = 0; index < 4; index++)
                        {
                            Emitter.LoadAlignedPackedSingle(0, Rax, index * 16, 128);
                            Emitter.LoadAlignedPackedSingle(1, Rcx, index * 16, 128);
                            Emitter.StoreAlignedPackedSingle(1, Rax, index * 16, 128);
                            Emitter.StoreAlignedPackedSingle(0, Rcx, index * 16, 128);
                        }
                    }
                    break;
                case shop_fmac:
                {
                    int rs1 = _allocator.MapXRegister(op.Source1);
                    int rs2 = _allocator.MapXRegister(op.Source2);
                    int rs3 = _allocator.MapXRegister(op.Source3);
                    int rd = _allocator.MapXRegister(op.Destination);
                    if (rd == rs2)
                    {
                        MoveScalar(1, rs2);
                        rs2 = 1;
                    }
                    if (rd == rs3)
                    {
                        MoveScalar(2, rs3);
                        rs3 = 2;
                    }
                    if (op.Source1.IsImmediate)
                    {
                        Move(Rax, op.Source1.Immediate);
                        MoveGeneralToFloat(rd, Rax);
                    }
                    else if (rd != rs1)
                        MoveScalar(rd, rs1);
                    if (Fma.IsSupported && !Sh4Configuration.GgpoEnabled)
                        Emitter.FusedMultiplyAdd231ScalarSingle(rd, rs2, rs3);
                    else
                    {
                        MoveScalar(0, rs2);
                        EmitSseRegister(0xF3, 0x59, 0, rs3);
                        EmitSseRegister(0xF3, 0x58, rd, 0);
                    }
                    break;
                }
#endif
                default:
#if !CANONICAL_TEST
                    if (!GenerateBaseOpcode(op))
#endif
                        Sh4ShilCanonical.Compile(Sh4RecompilerDriver.Current.NativeBackend, op);
                    break;
            }
            _allocator.EndOperation(op);
        }
        _allocator.Cleanup();
        _currentOperation = -1;

        Address(Rax, Context.NativeAddress + Sh4ContextLayout.Pc);
        switch (block.BlockType)
        {
            case BET_StaticJump:
            case BET_StaticCall:
                Store(Rax, block.BranchAddress);
                break;
            case BET_Cond_0:
            case BET_Cond_1:
                Store(Rax, block.NextAddress);
                if (block.HasConditionalJump)
                    Address(Rdx, Context.NativeAddress + Sh4ContextLayout.DynamicPc);
                else
                    Address(Rdx, Context.NativeAddress + Sh4ContextLayout.SrT);
                Emitter.BinaryMemoryImmediate(X64Arithmetic.Compare, (int)block.BlockType & 1, Rdx, 0, Dword);
                int notTaken = Emitter.ReserveShortJump(X64Condition.NotZero);
                Store(Rax, block.BranchAddress);
                Emitter.PatchShortJump(notTaken, Emitter.Position);
                break;
            case BET_DynamicJump:
            case BET_DynamicCall:
            case BET_DynamicRet:
                Address(Rdx, Context.NativeAddress + Sh4ContextLayout.DynamicPc);
                Emitter.Load(Rdx, 0, Rdx, Dword);
                Emitter.Store(Rdx, Rax, 0, Dword);
                break;
            case BET_DynamicIntr:
            case BET_StaticIntr:
                if (block.BlockType == BET_DynamicIntr)
                {
                    Address(Rdx, Context.NativeAddress + Sh4ContextLayout.DynamicPc);
                    Emitter.Load(Rdx, 0, Rdx, Dword);
                    Emitter.Store(Rdx, Rax, 0, Dword);
                }
                else
                    Store(Rax, block.NextAddress);
                GenerateCall(Sh4Interrupts.UpdateEntry);
                break;
            default:
                Sh4HostRuntime.Die("Invalid block end type");
                break;
        }
        if (exitBlock >= 0)
            Emitter.PatchJump(exitBlock, Emitter.Position);
        Emitter.ReleaseStack(StackAlignment);
        Emitter.Return();
        Ready();
        block.Code = WritableCode;
        block.HostCodeSize = (uint)Emitter.Position;
        CodeBuffer.Advance((uint)Emitter.Position);
    }

    private readonly List<(Sh4CanonicalParameterType Type, Sh4ShilParameter Parameter)> _canonicalParameters = [];

    internal void CanonicalStart(Sh4ShilOpcode opcode) => _canonicalParameters.Clear();

    internal void CanonicalParameter(Sh4ShilOpcode opcode, Sh4ShilParameter parameter, Sh4CanonicalParameterType type)
    {
        switch (type)
        {
            case Sh4CanonicalParameterType.UInt32:
            case Sh4CanonicalParameterType.RegisterPointer:
            case Sh4CanonicalParameterType.Float32:
            case Sh4CanonicalParameterType.ContextPointer:
                _canonicalParameters.Add((type, parameter));
                break;
            case Sh4CanonicalParameterType.UInt64LowReturn:
            case Sh4CanonicalParameterType.UInt32Return:
                Emitter.Move(Rax, Rcx, Qword);
                HostRegisterToShilParameter(parameter, Rcx);
                break;
            case Sh4CanonicalParameterType.UInt64HighReturn:
                Emitter.Shift(X64Shift.Right, 32, Rcx, Qword);
                HostRegisterToShilParameter(parameter, Rcx);
                break;
            case Sh4CanonicalParameterType.Float32Return:
                HostRegisterToShilParameter(parameter, 0);
                break;
        }
    }

    internal void CanonicalCall(Sh4ShilOpcode opcode, nuint function)
    {
        int general = 0;
        int floatingPoint = 0;
        for (int i = _canonicalParameters.Count - 1; i >= 0; i--)
        {
            Sh4HostRuntime.Verify(floatingPoint < 4 && general < 4, "xmmused < 4 && regused < 4");
            var (type, parameter) = _canonicalParameters[i];
            switch (type)
            {
                case Sh4CanonicalParameterType.UInt32:
                    ShilParameterToHostRegister(parameter, CallRegisters[general++]);
                    break;
                case Sh4CanonicalParameterType.Float32:
                    ShilParameterToHostRegister(parameter, CallFloatRegisters[floatingPoint++]);
                    break;
                case Sh4CanonicalParameterType.RegisterPointer:
                    Sh4HostRuntime.Verify(parameter.IsRegister, "prm.is_reg()");
                    Address(CallRegisters[general++], ParameterAddress(parameter));
                    break;
                case Sh4CanonicalParameterType.ContextPointer:
                    Address(CallRegisters[general++], Context.NativeAddress);
                    break;
            }
        }
        GenerateCall(function);
        if (AllocateFloatPairs)
        {
            foreach (var (type, parameter) in _canonicalParameters)
            {
                if (type == Sh4CanonicalParameterType.RegisterPointer && parameter.Count == 2 && _allocator.IsFloatAllocated(parameter)
                    && (opcode.Destination.Register == parameter.Register || opcode.Destination2.Register == parameter.Register))
                {
                    Address(Rax, Context.NativeAddress + Sh4Shil.GetRegisterOffset(parameter.Register));
                    LoadFloat(_allocator.MapXRegister(parameter, 0), Rax, 0);
                    Address(Rax, Context.NativeAddress + Sh4Shil.GetRegisterOffset(parameter.Register + 1));
                    LoadFloat(_allocator.MapXRegister(parameter, 1), Rax, 0);
                }
            }
        }
    }

    internal void PreloadRegister(Sh4Register register, X64Register host)
    {
        Address(Rax, Context.NativeAddress + Sh4Shil.GetRegisterOffset(register));
        Emitter.Load(Rax, 0, host, Dword);
    }

    internal void WriteBackRegister(Sh4Register register, X64Register host)
    {
        Address(Rax, Context.NativeAddress + Sh4Shil.GetRegisterOffset(register));
        Emitter.Store(host, Rax, 0, Dword);
    }

    internal void PreloadFloat(Sh4Register register, int host)
    {
        Address(Rax, Context.NativeAddress + Sh4Shil.GetRegisterOffset(register));
        LoadFloat(host, Rax, 0);
    }

    internal void WriteBackFloat(Sh4Register register, int host)
    {
        Address(Rax, Context.NativeAddress + Sh4Shil.GetRegisterOffset(register));
        StoreFloat(host, Rax, 0);
    }

    internal void GenerateMainLoop()
    {
        Sh4UnwindInfo unwind = Sh4X64Recompiler.Unwinder;
        unwind.Start(WritableCode);
        Push(Rbx);
        Push(Rbp);
        if (OperatingSystem.IsWindows())
        {
            Push(Rdi);
            Push(Rsi);
        }
        Push(R12);
        Push(R13);
        Push(R14);
        Push(R15);
        Emitter.AllocateStack(StackAlignment);
        unwind.AllocateStack((uint)Emitter.Position, StackAlignment);
        unwind.EndProlog((uint)Emitter.Position);
        fixed (ulong* stack = &Sh4X64Recompiler.JumpStack)
            Emitter.StoreRipRelative(Rsp, ExecutableStart, (nuint)stack, Qword);

        int runLoop = Emitter.Position;
        Address(Rax, Context.NativeAddress + Sh4ContextLayout.CpuRunning);
        Emitter.Load(Rax, 0, Rdx, Dword);
        Emitter.Test(Rdx, Rdx, Dword);
        int endRunLoop = Emitter.ReserveShortJump(X64Condition.Zero);

        int sliceLoop = Emitter.Position;
        Address(Rax, Context.NativeAddress + Sh4ContextLayout.Pc);
        Emitter.Load(Rax, 0, CallRegisters[0], Dword);
        Call(Sh4BlockManager.GetCodeByVirtualAddressEntry);
        Emitter.CallRegister(Rax);
        Address(Rax, Context.NativeAddress + Sh4ContextLayout.CycleCounter);
        Emitter.Load(Rax, 0, Rcx, Dword);
        Emitter.Test(Rcx, Rcx, Dword);
        Emitter.JumpRelativeAddress(ExecutableStart, ExecutableStart + (uint)sliceLoop, X64Condition.Greater);
        Emitter.BinaryImmediate(X64Arithmetic.Add, Sh4Scheduler.TimeSlice, Rcx, Dword);
        Emitter.Store(Rcx, Rax, 0, Dword);
        Call(Sh4RecompilerDriver.UpdateSystemInterruptsEntry);
        Jump(ExecutableStart + (uint)runLoop);

        Emitter.PatchShortJump(endRunLoop, Emitter.Position);
        Emitter.ReleaseStack(StackAlignment);
        Emitter.Pop(R15);
        Emitter.Pop(R14);
        Emitter.Pop(R13);
        Emitter.Pop(R12);
        if (OperatingSystem.IsWindows())
        {
            Emitter.Pop(Rsi);
            Emitter.Pop(Rdi);
        }
        Emitter.Pop(Rbp);
        Emitter.Pop(Rbx);
        Emitter.Return();
        nuint unwindSize = unwind.End((uint)Emitter.Position);
        Emitter.Append(new ReadOnlySpan<byte>((void*)(WritableCode + (uint)Emitter.Position), (int)unwindSize));

        unwind.Start(WritableCode + (uint)Emitter.Position);
        int startOffset = Emitter.Position;
        unwind.AllocateStack(0, StackAlignment);
        unwind.EndProlog(0);
        int handleException = Emitter.Position;
        fixed (ulong* stack = &Sh4X64Recompiler.JumpStack)
            Emitter.LoadRipRelative(ExecutableStart, (nuint)stack, Rsp, Qword);
        Jump(ExecutableStart + (uint)runLoop);
        GenerateMemoryHandlers();
        nuint exceptionEntry = ExecutableStart + (uint)handleException;

        unwindSize = unwind.End(CodeBuffer.FreeSpace - 128 - (uint)startOffset);
        Sh4HostRuntime.Verify(unwindSize <= 128, "unwindSize <= 128");
        Ready();
        Sh4X64Recompiler.MainLoopEntry = (delegate* unmanaged<void>)ExecutableStart;
        Sh4X64Recompiler.HandleExceptionEntry = (delegate* unmanaged<void>)exceptionEntry;
        CodeBuffer.Advance((uint)Emitter.Position);

        void Push(X64Register register)
        {
            Emitter.Push(register);
            unwind.PushRegister((uint)Emitter.Position, (int)register);
        }
    }

    internal bool RewriteMemoryAccess(ref Sh4HostContext context)
    {
        if (!Sh4RecompilerDriver.Current.AddressSpace.VirtualMemoryEnabled)
            return false;
        if (context.ProgramCounter < Sh4X64Recompiler.MemoryHandlerStart || context.ProgramCounter >= Sh4X64Recompiler.MemoryHandlerEnd)
            return false;

        nuint returnAddress = *(nuint*)context.StackPointer;
        nuint callAddress = unchecked(returnAddress + (nuint)*(int*)(returnAddress - 4));
        for (int size = 0; size < (int)Sh4X64Recompiler.MemorySize.Count; size++)
        {
            for (int operation = 0; operation < (int)Sh4X64Recompiler.MemoryOperation.Count; operation++)
            {
                if (Sh4X64Recompiler.MemoryHandlers[(int)Sh4X64Recompiler.MemoryType.Fast, size, operation] != callAddress)
                    continue;
                int start = Emitter.Position;
                uint memoryAddress = (uint)context.R9;
                if (operation == (int)Sh4X64Recompiler.MemoryOperation.Write && size >= (int)Sh4X64Recompiler.MemorySize.S32 && (memoryAddress >> 26) == 0x38)
                    Call(Sh4X64Recompiler.MemoryHandlers[(int)Sh4X64Recompiler.MemoryType.StoreQueue, size, operation]);
                else
                    Call(Sh4X64Recompiler.MemoryHandlers[(int)Sh4X64Recompiler.MemoryType.Slow, size, operation]);
                Sh4HostRuntime.Verify(Emitter.Position - start == 5, "getCurr() - start == 5");
                Ready();
                context.ProgramCounter = returnAddress - 5;
                context.StackPointer += 8;
                if (OperatingSystem.IsWindows())
                    context.Rcx = memoryAddress;
                else
                    context.Rdi = memoryAddress;
                return true;
            }
        }
        Trace.TraceError($"rewriteMemAccess code not found: host pc 0x{context.ProgramCounter:x}");
        Sh4HostRuntime.Die("Failed to match the code");
        return false;
    }

    private void GenerateMmuLookup(Sh4RuntimeBlockInfo block, Sh4ShilOpcode op, uint write)
    {
        if (!Context.Mmu.Enabled)
            return;

#if !SH4_STRICT_MODE
        Emitter.Move(CallRegisters[0], Rax, Dword);
        Emitter.Shift(X64Shift.Right, 12, Rax, Dword);
        nuint table = Context.Mmu.AddressLookupTablePointer;
        if ((ulong)table >> 32 != 0)
        {
            Address(R9, table);
            Emitter.LoadIndexed(R9, Rax, 2, 0, Rax, Dword);
        }
        else
            Emitter.LoadAbsoluteIndexed((uint)table, Rax, 4, Rax, Dword);
        Emitter.Test(Rax, Rax, Dword);
        int inCache = Emitter.ReserveShortJump(X64Condition.NotZero);
#endif
        Move(CallRegisters[1], write);
        Move(CallRegisters[2], unchecked(block.VirtualAddress + (uint)op.GuestOffset - (op.DelaySlot ? 2u : 0u)));
        GenerateCall(Sh4Mmu.DynarecLookupEntry);
        Emitter.Move(Rax, CallRegisters[0], Dword);
#if !SH4_STRICT_MODE
        int done = Emitter.ReserveShortJump();
        Emitter.PatchShortJump(inCache, Emitter.Position);
        Emitter.BinaryImmediate(X64Arithmetic.And, 0xFFF, CallRegisters[0], Dword);
        Emitter.Binary(X64Arithmetic.Or, Rax, CallRegisters[0], Dword);
        Emitter.PatchShortJump(done, Emitter.Position);
#endif
    }

    private bool GenerateReadMemoryImmediate(Sh4ShilOpcode op, Sh4RuntimeBlockInfo block)
    {
        if (!op.Source1.IsImmediate)
            return false;
        nuint pointer = 0;
        bool isRam = false;
        uint address = 0;
        if (!Sh4RecompilerDriver.Current.TryReadMemoryImmediate(op.Source1.Immediate, (int)op.Size, ref pointer, ref isRam, ref address, block))
            return false;

        if (isRam)
        {
            Address(Rax, pointer);
            switch (op.Size)
            {
                case 1:
                    if (_allocator.IsGeneralAllocated(op.Destination))
                        Emitter.LoadExtend(Rax, 0, _allocator.MapRegister(op.Destination), Byte, Dword, true);
                    else
                    {
                        Emitter.LoadExtend(Rax, 0, Rax, Byte, Dword, true);
                        Address(Rcx, ParameterAddress(op.Destination));
                        Emitter.Store(Rax, Rcx, 0, Dword);
                    }
                    break;
                case 2:
                    if (_allocator.IsGeneralAllocated(op.Destination))
                        Emitter.LoadExtend(Rax, 0, _allocator.MapRegister(op.Destination), Word, Dword, true);
                    else
                    {
                        Emitter.LoadExtend(Rax, 0, Rax, Word, Dword, true);
                        Address(Rcx, ParameterAddress(op.Destination));
                        Emitter.Store(Rax, Rcx, 0, Dword);
                    }
                    break;
                case 4:
                    if (_allocator.IsGeneralAllocated(op.Destination))
                        Emitter.Load(Rax, 0, _allocator.MapRegister(op.Destination), Dword);
                    else if (_allocator.IsFloatAllocated(op.Destination))
                        Emitter.LoadXmmDword(_allocator.MapXRegister(op.Destination), Rax, 0);
                    else
                    {
                        Emitter.Load(Rax, 0, Rax, Dword);
                        Address(Rcx, ParameterAddress(op.Destination));
                        Emitter.Store(Rax, Rcx, 0, Dword);
                    }
                    break;
                case 8:
                    if (!AllocateFloatPairs)
                    {
                        Emitter.Load(Rax, 0, Rcx, Qword);
                        Address(Rax, ParameterAddress(op.Destination));
                        Emitter.Store(Rcx, Rax, 0, Qword);
                    }
                    else
                    {
                        Emitter.LoadXmmDword(_allocator.MapXRegister(op.Destination, 0), Rax, 0);
                        Emitter.LoadXmmDword(_allocator.MapXRegister(op.Destination, 1), Rax, 4);
                    }
                    break;
                default:
                    Sh4HostRuntime.Die("Invalid immediate size");
                    break;
            }
        }
        else
        {
            if (op.Size == 8)
            {
                Move(CallRegisters[0], address);
                GenerateCall(pointer);
                if (!AllocateFloatPairs)
                {
                    Address(Rcx, ParameterAddress(op.Destination));
                    Emitter.Store(Rax, Rcx, 0, Dword);
                }
                else
                    MoveGeneralToFloat(_allocator.MapXRegister(op.Destination, 0), Rax);

                Move(CallRegisters[0], unchecked(address + 4));
                GenerateCall(pointer);
                if (!AllocateFloatPairs)
                {
                    Address(Rcx, ParameterAddress(op.Destination) + 4);
                    Emitter.Store(Rax, Rcx, 0, Dword);
                }
                else
                    MoveGeneralToFloat(_allocator.MapXRegister(op.Destination, 1), Rax);
            }
            else
            {
                Move(CallRegisters[0], address);
                switch (op.Size)
                {
                    case 1:
                        GenerateCall(pointer);
                        Emitter.MoveExtend(Rax, Rax, Byte, Dword, true);
                        break;
                    case 2:
                        GenerateCall(pointer);
                        Emitter.MoveExtend(Rax, Rax, Word, Dword, true);
                        break;
                    case 4:
                        GenerateCall(pointer);
                        break;
                    default:
                        Sh4HostRuntime.Die("Invalid immediate size");
                        break;
                }
                Emitter.Move(Rax, Rcx, Dword);
                HostRegisterToShilParameter(op.Destination, Rcx);
            }
        }
        return true;
    }

    private bool GenerateWriteMemoryImmediate(Sh4ShilOpcode op, Sh4RuntimeBlockInfo block)
    {
        if (!op.Source1.IsImmediate)
            return false;
        nuint pointer = 0;
        bool isRam = false;
        uint address = 0;
        if (!Sh4RecompilerDriver.Current.TryWriteMemoryImmediate(op.Source1.Immediate, (int)op.Size, ref pointer, ref isRam, ref address, block))
            return false;

        if (isRam)
        {
            Address(Rax, pointer);
            switch (op.Size)
            {
                case 1:
                    if (_allocator.IsGeneralAllocated(op.Source2))
                        Emitter.Store(_allocator.MapRegister(op.Source2), Rax, 0, Byte);
                    else if (op.Source2.IsImmediate)
                        Emitter.StoreImmediate((byte)op.Source2.Immediate, Rax, 0, Byte);
                    else
                    {
                        Address(Rcx, ParameterAddress(op.Source2));
                        Emitter.Load(Rcx, 0, Rcx, Byte);
                        Emitter.Store(Rcx, Rax, 0, Byte);
                    }
                    break;
                case 2:
                    if (_allocator.IsGeneralAllocated(op.Source2))
                        Emitter.Store(_allocator.MapRegister(op.Source2), Rax, 0, Word);
                    else if (op.Source2.IsImmediate)
                        Emitter.StoreImmediate((ushort)op.Source2.Immediate, Rax, 0, Word);
                    else
                    {
                        Address(Rcx, ParameterAddress(op.Source2));
                        Emitter.Load(Rcx, 0, Rcx, Word);
                        Emitter.Store(Rcx, Rax, 0, Word);
                    }
                    break;
                case 4:
                    if (_allocator.IsGeneralAllocated(op.Source2))
                        Emitter.Store(_allocator.MapRegister(op.Source2), Rax, 0, Dword);
                    else if (_allocator.IsFloatAllocated(op.Source2))
                        Emitter.StoreXmmDword(_allocator.MapXRegister(op.Source2), Rax, 0);
                    else if (op.Source2.IsImmediate)
                        Store(Rax, op.Source2.Immediate);
                    else
                    {
                        Address(Rcx, ParameterAddress(op.Source2));
                        Emitter.Load(Rcx, 0, Rcx, Dword);
                        Emitter.Store(Rcx, Rax, 0, Dword);
                    }
                    break;
                case 8:
                    if (!AllocateFloatPairs)
                    {
                        Address(Rcx, ParameterAddress(op.Source2));
                        Emitter.Load(Rcx, 0, Rcx, Qword);
                        Emitter.Store(Rcx, Rax, 0, Qword);
                    }
                    else
                    {
                        Emitter.StoreXmmDword(_allocator.MapXRegister(op.Source2, 0), Rax, 0);
                        Emitter.StoreXmmDword(_allocator.MapXRegister(op.Source2, 1), Rax, 4);
                    }
                    break;
                default:
                    Sh4HostRuntime.Die("Invalid immediate size");
                    break;
            }
        }
        else
        {
            Move(CallRegisters[0], address);
            ShilParameterToHostRegister(op.Source2, CallRegisters[1]);
            GenerateCall(pointer);
        }
        return true;
    }

    private void CheckBlock(bool forceChecks, Sh4RuntimeBlockInfo block)
    {
        if (Context.Mmu.Enabled || forceChecks)
            Move(CallRegisters[0], block.Address);
        nuint failure = (nuint)(delegate* unmanaged<uint, void>)&Sh4X64Recompiler.BlockCheckFailed;
        if (Context.Mmu.Enabled)
        {
            Address(Rax, Context.NativeAddress + Sh4ContextLayout.Pc);
            Emitter.BinaryMemoryImmediate(X64Arithmetic.Compare, unchecked((int)block.VirtualAddress), Rax, 0, Dword);
            Emitter.JumpRelativeAddress(WritableCode, failure, X64Condition.NotZero);
        }
        if (!forceChecks)
            return;

        int size = (int)block.GuestCodeSize;
        uint sourceAddress = block.Address;
        byte* pointer = MemoryPointer(sourceAddress, size > 8 ? 8 : size);
        if (pointer != null)
        {
            while (size > 0)
            {
                nuint address = (nuint)pointer;
                Address(Rax, address);
                if (size >= 8 && (address & 7) == 0)
                {
                    Emitter.MoveImmediate(unchecked((long)*(ulong*)pointer), Rdx, Qword);
                    Emitter.BinaryStore(X64Arithmetic.Compare, Rdx, Rax, 0, Qword);
                    size -= 8;
                    sourceAddress += 8;
                }
                else if (size >= 4 && (address & 3) == 0)
                {
                    Move(Rdx, *(uint*)pointer);
                    Emitter.BinaryStore(X64Arithmetic.Compare, Rdx, Rax, 0, Dword);
                    size -= 4;
                    sourceAddress += 4;
                }
                else
                {
                    Move(Rdx, *(ushort*)pointer);
                    Emitter.BinaryStore(X64Arithmetic.Compare, Rdx, Rax, 0, Word);
                    size -= 2;
                    sourceAddress += 2;
                }
                Emitter.JumpRelativeAddress(ExecutableStart, failure, X64Condition.NotZero);
                pointer = MemoryPointer(sourceAddress, size > 8 ? 8 : size);
            }
        }
    }

    private byte* MemoryPointer(uint address, int size) =>
        Sh4RecompilerDriver.Current.AddressSpace.GetMemoryPointer(address, unchecked((uint)size));

    private void GenerateMemoryHandlers()
    {
        Sh4AddressSpace addressSpace = Sh4RecompilerDriver.Current.AddressSpace;
        Sh4HostRuntime.Verify(addressSpace.MemoryHandlersInitialized, "ReadMem8 != nullptr");
        Sh4X64Recompiler.MemoryHandlerStart = ExecutableAddress;
        for (int type = 0; type < (int)Sh4X64Recompiler.MemoryType.Count; type++)
        {
            for (int size = 0; size < (int)Sh4X64Recompiler.MemorySize.Count; size++)
            {
                for (int operation = 0; operation < (int)Sh4X64Recompiler.MemoryOperation.Count; operation++)
                {
                    Sh4X64Recompiler.MemoryHandlers[type, size, operation] = ExecutableAddress;
                    bool read = operation == (int)Sh4X64Recompiler.MemoryOperation.Read;
                    if (type == (int)Sh4X64Recompiler.MemoryType.Fast && addressSpace.VirtualMemoryEnabled)
                    {
                        Address(Rax, addressSpace.RamBase);
                        Emitter.Move(CallRegisters[0], R9, Qword);
                        Emitter.BinaryImmediate(X64Arithmetic.And, 0x1FFF_FFFF, CallRegisters[0], Dword);
                        switch ((Sh4X64Recompiler.MemorySize)size)
                        {
                            case Sh4X64Recompiler.MemorySize.S8:
                                if (read)
                                    Emitter.LoadExtendIndexed(Rax, CallRegisters[0], 1, 0, Rax, Byte, Dword, true);
                                else
                                    Emitter.StoreIndexed(CallRegisters[1], Rax, CallRegisters[0], 0, 0, Byte);
                                break;
                            case Sh4X64Recompiler.MemorySize.S16:
                                if (read)
                                    Emitter.LoadExtendIndexed(Rax, CallRegisters[0], 1, 0, Rax, Word, Dword, true);
                                else
                                    Emitter.StoreIndexed(CallRegisters[1], Rax, CallRegisters[0], 0, 0, Word);
                                break;
                            case Sh4X64Recompiler.MemorySize.S32:
                                if (read)
                                    Emitter.LoadIndexed(Rax, CallRegisters[0], 0, 0, Rax, Dword);
                                else
                                    Emitter.StoreIndexed(CallRegisters[1], Rax, CallRegisters[0], 0, 0, Dword);
                                break;
                            case Sh4X64Recompiler.MemorySize.S64:
                                if (read)
                                    Emitter.LoadIndexed(Rax, CallRegisters[0], 0, 0, Rax, Qword);
                                else
                                    Emitter.StoreIndexed(CallRegisters[1], Rax, CallRegisters[0], 0, 0, Qword);
                                break;
                        }
                    }
                    else if (type == (int)Sh4X64Recompiler.MemoryType.StoreQueue)
                    {
                        if (read || size < (int)Sh4X64Recompiler.MemorySize.S32)
                            continue;
                        Emitter.Move(CallRegisters[0], R9, Dword);
                        Emitter.Shift(X64Shift.Right, 26, R9, Dword);
                        Emitter.BinaryImmediate(X64Arithmetic.Compare, 0x38, R9, Dword);
                        int noStoreQueue = Emitter.ReserveShortJump(X64Condition.NotZero);
                        Address(Rax, Context.NativeAddress + Sh4ContextLayout.StoreQueueBuffer);
                        Emitter.BinaryImmediate(X64Arithmetic.And, 0x3F, CallRegisters[0], Dword);
                        if (size == (int)Sh4X64Recompiler.MemorySize.S32)
                            Emitter.StoreIndexed(CallRegisters[1], Rax, CallRegisters[0], 0, 0, Dword);
                        else
                            Emitter.StoreIndexed(CallRegisters[1], Rax, CallRegisters[0], 0, 0, Qword);
                        Emitter.Return();
                        Emitter.PatchShortJump(noStoreQueue, Emitter.Position);
                        Jump(size == (int)Sh4X64Recompiler.MemorySize.S32
                            ? Sh4AddressSpace.Write32Entry
                            : Sh4AddressSpace.Write64Entry);
                        continue;
                    }
                    else
                    {
                        if (read)
                        {
                            switch ((Sh4X64Recompiler.MemorySize)size)
                            {
                                case Sh4X64Recompiler.MemorySize.S8:
                                    Emitter.AllocateStack(StackAlignment);
                                    Call(Sh4AddressSpace.Read8Entry);
                                    Emitter.MoveExtend(Rax, Rax, Byte, Dword, true);
                                    Emitter.ReleaseStack(StackAlignment);
                                    break;
                                case Sh4X64Recompiler.MemorySize.S16:
                                    Emitter.AllocateStack(StackAlignment);
                                    Call(Sh4AddressSpace.Read16Entry);
                                    Emitter.MoveExtend(Rax, Rax, Word, Dword, true);
                                    Emitter.ReleaseStack(StackAlignment);
                                    break;
                                case Sh4X64Recompiler.MemorySize.S32:
                                    Jump(Sh4AddressSpace.Read32Entry);
                                    continue;
                                case Sh4X64Recompiler.MemorySize.S64:
                                    Jump(Sh4AddressSpace.Read64Entry);
                                    continue;
                                default:
                                    Sh4HostRuntime.Die("1..8 bytes");
                                    break;
                            }
                        }
                        else
                        {
                            switch ((Sh4X64Recompiler.MemorySize)size)
                            {
                                case Sh4X64Recompiler.MemorySize.S8:
                                    Jump(Sh4AddressSpace.Write8Entry);
                                    continue;
                                case Sh4X64Recompiler.MemorySize.S16:
                                    Jump(Sh4AddressSpace.Write16Entry);
                                    continue;
                                case Sh4X64Recompiler.MemorySize.S32:
                                    Jump(Sh4AddressSpace.Write32Entry);
                                    continue;
                                case Sh4X64Recompiler.MemorySize.S64:
                                    Jump(Sh4AddressSpace.Write64Entry);
                                    continue;
                                default:
                                    Sh4HostRuntime.Die("1..8 bytes");
                                    break;
                            }
                        }
                    }
                    Emitter.Return();
                }
            }
        }
        Sh4X64Recompiler.MemoryHandlerEnd = ExecutableAddress;
    }

    private void SaveXmmRegisters()
    {
        if (OperatingSystem.IsWindows() || _currentOperation == -1)
            return;
        fixed (float* save = Sh4X64Recompiler.XmmSave)
        {
            if (_allocator.IsMapped(8, _currentOperation)) Emitter.StoreXmmDwordRipRelative(8, ExecutableStart, (nuint)(save + 0));
            if (_allocator.IsMapped(9, _currentOperation)) Emitter.StoreXmmDwordRipRelative(9, ExecutableStart, (nuint)(save + 1));
            if (_allocator.IsMapped(10, _currentOperation)) Emitter.StoreXmmDwordRipRelative(10, ExecutableStart, (nuint)(save + 2));
            if (_allocator.IsMapped(11, _currentOperation)) Emitter.StoreXmmDwordRipRelative(11, ExecutableStart, (nuint)(save + 3));
        }
    }

    private void RestoreXmmRegisters()
    {
        if (OperatingSystem.IsWindows() || _currentOperation == -1)
            return;
        fixed (float* save = Sh4X64Recompiler.XmmSave)
        {
            if (_allocator.IsMapped(8, _currentOperation)) Emitter.LoadXmmDwordRipRelative(8, ExecutableStart, (nuint)(save + 0));
            if (_allocator.IsMapped(9, _currentOperation)) Emitter.LoadXmmDwordRipRelative(9, ExecutableStart, (nuint)(save + 1));
            if (_allocator.IsMapped(10, _currentOperation)) Emitter.LoadXmmDwordRipRelative(10, ExecutableStart, (nuint)(save + 2));
            if (_allocator.IsMapped(11, _currentOperation)) Emitter.LoadXmmDwordRipRelative(11, ExecutableStart, (nuint)(save + 3));
        }
    }

    private void GenerateCall(nuint function, bool skipFloats = false)
    {
        if (!skipFloats)
            SaveXmmRegisters();
        Call(function);
        if (!skipFloats)
            RestoreXmmRegisters();
    }

    private nuint ParameterAddress(Sh4ShilParameter parameter) => Context.NativeAddress + parameter.GetRegisterOffset();
    private nuint ExecutableStart => CodeBuffer.ToExecutable(WritableCode);
    private nuint ExecutableAddress => ExecutableStart + (uint)Emitter.Position;
    private void Address(X64Register target, nuint value) => Emitter.MoveImmediate(unchecked((long)value), target, Qword);
    private void Move(X64Register target, uint value) => Emitter.MoveImmediate(value, target, Dword);
    private void Store(X64Register address, uint value) => Emitter.StoreImmediate(unchecked((int)value), address, 0, Dword);
    private void MoveScalar(int target, int source) => EmitSseRegister(0xF3, 0x10, target, source);
    private void Call(nuint target) => Emitter.CallRelativeAddress(ExecutableStart, target);
    private void Jump(nuint target) => Emitter.JumpRelativeAddress(ExecutableStart, target);
    private static int SizeIndex(uint size) => size == 1 ? 0 : size == 2 ? 1 : size == 4 ? 2 : 3;

    private void Ready()
    {
        byte[] bytes = Emitter.ToArray();
        bytes.AsSpan().CopyTo(new Span<byte>((void*)WritableCode, bytes.Length));
    }
}

internal sealed partial class Sh4X64RegisterAllocator
{
    protected override partial void Preload(Sh4Register register, int hostRegister) =>
        _compiler.PreloadRegister(register, (X64Register)hostRegister);
    protected override partial void WriteBack(Sh4Register register, int hostRegister) =>
        _compiler.WriteBackRegister(register, (X64Register)hostRegister);
    protected override partial void PreloadFloat(Sh4Register register, int hostRegister) =>
        _compiler.PreloadFloat(register, hostRegister);
    protected override partial void WriteBackFloat(Sh4Register register, int hostRegister) =>
        _compiler.WriteBackFloat(register, hostRegister);
}

internal sealed unsafe partial class Sh4X64Recompiler : Sh4NativeBackend
{
    internal static Sh4X64Recompiler Instance { get; private set; } = null!;

    private Sh4Context _context = null!;
    private Sh4CodeBuffer? _codeBuffer;
    private Sh4X64BlockCompiler? _compiler;

    private Sh4X64Recompiler() => Current = this;

    [ModuleInitializer]
    internal static void RegisterBackend()
    {
        if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
            Instance = new Sh4X64Recompiler();
    }

    internal override void Compile(Sh4RuntimeBlockInfo block, bool blockCheck, bool optimize)
    {
        Sh4CodeBuffer codeBuffer = _codeBuffer!;
        nuint start = codeBuffer.Current;
        uint size = codeBuffer.FreeSpace;
        Sh4VirtualMemory.SetJitExecutable(start, size, false);
        _compiler = new Sh4X64BlockCompiler(_context, codeBuffer);
        try
        {
            _compiler.Compile(block, blockCheck, optimize);
        }
        catch (X64EmitterException exception)
        {
            Trace.TraceError($"Fatal xbyak error: {exception.Message}");
        }
        _compiler = null;
        Sh4VirtualMemory.SetJitExecutable(start, size, true);
    }

    internal override void Initialize(Sh4Context context, Sh4CodeBuffer codeBuffer)
    {
        _context = context;
        _codeBuffer = codeBuffer;
    }

    internal override void MainLoop(nuint controlBlock)
    {
        Sh4HostRuntime.Verify(MainLoopEntry != null, "::mainloop != nullptr");
        try
        {
            MainLoopEntry();
        }
        catch (Sh4InstructionException exception)
        {
            Trace.TraceError($"SH4ThrownException in mainloop code {(uint)exception.Event:x}");
            throw new InvalidOperationException("Fatal: Unhandled SH4 exception");
        }
    }

    internal override void CanonicalStart(Sh4ShilOpcode opcode) => _compiler!.CanonicalStart(opcode);

    internal override void CanonicalParameter(Sh4ShilOpcode opcode, Sh4ShilParameter parameter, Sh4CanonicalParameterType type) =>
        _compiler!.CanonicalParameter(opcode, parameter, type);

    internal override void CanonicalCall(Sh4ShilOpcode opcode, nuint function) => _compiler!.CanonicalCall(opcode, function);

    internal override void CanonicalFinish(Sh4ShilOpcode opcode) { }

    internal override bool Rewrite(ref Sh4HostContext context, nuint faultAddress)
    {
        if (_codeBuffer is not { } codeBuffer)
            return false;
        nuint returnAddress = *(nuint*)context.StackPointer - 5;
        if (returnAddress < codeBuffer.WritableBase || returnAddress >= codeBuffer.WritableBase + Sh4CodeBuffer.FullSize)
            return false;
        Sh4VirtualMemory.SetJitExecutable(returnAddress, 16, false);
        var compiler = new Sh4X64BlockCompiler(_context, codeBuffer, returnAddress);
        bool result = false;
        try
        {
            result = compiler.RewriteMemoryAccess(ref context);
        }
        catch (X64EmitterException exception)
        {
            Trace.TraceError($"Fatal xbyak error: {exception.Message}");
        }
        Sh4VirtualMemory.SetJitExecutable(returnAddress, 16, true);
        return result;
    }

    internal override void HandleException(ref Sh4HostContext context) => context.ProgramCounter = (nuint)HandleExceptionEntry;

    internal override void Reset()
    {
        Unwinder.Clear();
        Sh4CodeBuffer codeBuffer = _codeBuffer!;
        nuint start = codeBuffer.Current;
        if (MainLoopEntry != null && (nuint)MainLoopEntry != start)
            return;
        uint size = codeBuffer.FreeSpace;
        Sh4VirtualMemory.SetJitExecutable(start, size, false);
        var compiler = new Sh4X64BlockCompiler(_context, codeBuffer);
        try
        {
            compiler.GenerateMainLoop();
        }
        catch (X64EmitterException exception)
        {
            Trace.TraceError($"Fatal xbyak error: {exception.Message}");
        }
        Sh4VirtualMemory.SetJitExecutable(start, size, true);
    }
}
#endif
