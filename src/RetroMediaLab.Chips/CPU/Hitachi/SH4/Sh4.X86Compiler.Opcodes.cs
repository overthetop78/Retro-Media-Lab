using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using RetroMediaLab.Chips.Common.Compilation.X64;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

using static Sh4ShilOperation;
using static X64Register;
using static X64Width;

#if !SH4_DYNAREC_NONE
internal sealed unsafe partial class Sh4X86Compiler
{
    private enum MemorySize { S8, S16, S32, F32, F64, Count }
    private enum MemoryOperation { Read, Write, Count }
    private enum MemoryPath { Fast, StoreQueue, Slow, Count }

    private static readonly nuint[,,] MemoryHandlers = new nuint[
        (int)MemoryPath.Count, (int)MemorySize.Count, (int)MemoryOperation.Count];
    private static nuint _memoryHandlerStart;
    private static nuint _memoryHandlerEnd;

    private nuint CurrentInstruction => WritableCode + (nuint)Emitter.Position;

    private partial void GenerateMemoryHandlers()
    {
        Sh4AddressSpace addressSpace = Sh4RecompilerDriver.Current.AddressSpace;
        _memoryHandlerStart = CurrentInstruction;
        for (int path = 0; path < (int)MemoryPath.Count; path++)
        {
            for (int size = 0; size < (int)MemorySize.Count; size++)
            {
                for (int operation = 0; operation < (int)MemoryOperation.Count; operation++)
                {
                    MemoryHandlers[path, size, operation] = CurrentInstruction;
                    var valueSize = (MemorySize)size;
                    bool read = operation == (int)MemoryOperation.Read;

                    if (path == (int)MemoryPath.Fast && addressSpace.VirtualMemoryEnabled)
                    {
                        Emitter.Move(Rcx, Rax, Dword);
                        Emitter.BinaryImmediate(X64Arithmetic.And, 0x1FFFFFFF, Rcx, Dword);
                        int address = unchecked((int)addressSpace.RamBase);
                        X64Width width;
                        X64Register register;
                        switch (valueSize)
                        {
                            case MemorySize.S8:
                                width = Byte;
                                register = read ? Rax : Rdx;
                                break;
                            case MemorySize.S16:
                                width = Word;
                                register = read ? Rax : Rdx;
                                break;
                            case MemorySize.S32:
                                width = Dword;
                                register = read ? Rax : Rdx;
                                break;
                            default:
                                width = Dword;
                                register = default;
                                break;
                        }
                        if (valueSize >= MemorySize.F32)
                        {
                            if (read)
                                LoadFloat(0, Rcx, address);
                            else
                                StoreFloat(0, Rcx, address);
                            if (valueSize == MemorySize.F64)
                            {
                                address += 4;
                                if (read)
                                    LoadFloat(1, Rcx, address);
                                else
                                    StoreFloat(1, Rcx, address);
                            }
                        }
                        else if (read)
                        {
                            if (valueSize <= MemorySize.S16)
                                Emitter.LoadExtend(Rcx, address, register, width, Dword, signed: true);
                            else
                                Emitter.Load(Rcx, address, register, width);
                        }
                        else
                        {
                            Emitter.Store(register, Rcx, address, width);
                        }
                    }
                    else if (path == (int)MemoryPath.StoreQueue)
                    {
                        if (read || valueSize < MemorySize.S32)
                            continue;

                        Emitter.Move(Rcx, Rax, Dword);
                        Emitter.Shift(X64Shift.Right, 28, Rax, Dword);
                        Emitter.BinaryImmediate(X64Arithmetic.Compare, 0xE, Rax, Dword);
                        int noStoreQueue = Emitter.ReserveShortJump(X64Condition.NotEqual);
                        Emitter.BinaryImmediate(X64Arithmetic.And, 0x3F, Rcx, Dword);

                        int storeQueue = unchecked((int)(Context.NativeAddress + Sh4ContextLayout.StoreQueueBuffer));
                        if (valueSize == MemorySize.S32)
                        {
                            Emitter.Store(Rdx, Rcx, storeQueue, Dword);
                        }
                        else if (valueSize >= MemorySize.F32)
                        {
                            StoreFloat(0, Rcx, storeQueue);
                            if (valueSize == MemorySize.F64)
                                StoreFloat(1, Rcx, storeQueue + 4);
                        }
                        Emitter.Return();
                        Emitter.PatchShortJump(noStoreQueue, Emitter.Position);

                        if (valueSize == MemorySize.F64)
                        {
                            if (!OperatingSystem.IsWindows())
                            {
                                AlignStack(-12);
                            }
                            else
                            {
                                Emitter.BinaryImmediate(X64Arithmetic.Subtract, 8, Rsp, Dword);
                                Unwinder.AllocateStackAt(CurrentInstruction, 8);
                            }
                            StoreFloat(0, Rsp, 0);
                            StoreFloat(1, Rsp, 4);
                            CallAbsolute(Sh4AddressSpace.Write64Entry);
                            AlignStack(4);
                        }
                        else
                        {
                            if (valueSize == MemorySize.F32)
                                MoveFloatToGeneral(Rdx, 0);
                            JumpAbsolute(Sh4AddressSpace.Write32Entry);
                            continue;
                        }
                    }
                    else
                    {
                        if (read)
                        {
                            switch (valueSize)
                            {
                                case MemorySize.S8:
                                    AlignStack(-12);
                                    CallAbsolute(Sh4AddressSpace.Read8Entry);
                                    Emitter.MoveExtend(Rax, Rax, Byte, Dword, signed: true);
                                    AlignStack(12);
                                    break;
                                case MemorySize.S16:
                                    AlignStack(-12);
                                    CallAbsolute(Sh4AddressSpace.Read16Entry);
                                    Emitter.MoveExtend(Rax, Rax, Word, Dword, signed: true);
                                    AlignStack(12);
                                    break;
                                case MemorySize.S32:
                                    JumpAbsolute(Sh4AddressSpace.Read32Entry);
                                    continue;
                                case MemorySize.F32:
                                    AlignStack(-12);
                                    CallAbsolute(Sh4AddressSpace.Read32Entry);
                                    MoveGeneralToFloat(0, Rax);
                                    AlignStack(12);
                                    break;
                                case MemorySize.F64:
                                    AlignStack(-12);
                                    CallAbsolute(Sh4AddressSpace.Read64Entry);
                                    MoveGeneralToFloat(0, Rax);
                                    MoveGeneralToFloat(1, Rdx);
                                    AlignStack(12);
                                    break;
                                default:
                                    Sh4HostRuntime.Die("1..8 bytes");
                                    break;
                            }
                        }
                        else
                        {
                            switch (valueSize)
                            {
                                case MemorySize.S8:
                                    JumpAbsolute(Sh4AddressSpace.Write8Entry);
                                    continue;
                                case MemorySize.S16:
                                    JumpAbsolute(Sh4AddressSpace.Write16Entry);
                                    continue;
                                case MemorySize.S32:
                                    JumpAbsolute(Sh4AddressSpace.Write32Entry);
                                    continue;
                                case MemorySize.F32:
                                    MoveFloatToGeneral(Rdx, 0);
                                    JumpAbsolute(Sh4AddressSpace.Write32Entry);
                                    continue;
                                case MemorySize.F64:
                                    if (!OperatingSystem.IsWindows())
                                    {
                                        AlignStack(-12);
                                    }
                                    else
                                    {
                                        Emitter.BinaryImmediate(X64Arithmetic.Subtract, 8, Rsp, Dword);
                                        Unwinder.AllocateStackAt(CurrentInstruction, 8);
                                    }
                                    StoreFloat(0, Rsp, 0);
                                    StoreFloat(1, Rsp, 4);
                                    CallAbsolute(Sh4AddressSpace.Write64Entry);
                                    AlignStack(4);
                                    break;
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
        _memoryHandlerEnd = CurrentInstruction;
    }

    private partial void GenerateMmuLookup(Sh4RuntimeBlockInfo block, Sh4ShilOpcode opcode, uint write)
    {
        if (!Context.Mmu.Enabled)
            return;

#if !SH4_STRICT_MODE
        Emitter.Move(Rcx, Rax, Dword);
        Emitter.Shift(X64Shift.Right, 12, Rax, Dword);
        EmitMmuTableLoad(Context.Mmu.AddressLookupTablePointer);
        Emitter.Test(Rax, Rax, Dword);
        int inCache = Emitter.ReserveShortJump(X64Condition.NotZero);
#endif
        Emitter.MoveImmediate(write, Rdx, Dword);
        EmitPushImmediate(GuestAddress(block, opcode, 2));
        CallAbsolute(Sh4Mmu.DynarecLookupX86Entry);
        Emitter.Move(Rax, Rcx, Dword);
#if !SH4_STRICT_MODE
        int done = Emitter.ReserveShortJump();
        Emitter.PatchShortJump(inCache, Emitter.Position);
        Emitter.BinaryImmediate(X64Arithmetic.And, 0xfff, Rcx, Dword);
        Emitter.Binary(X64Arithmetic.Or, Rax, Rcx, Dword);
        Emitter.PatchShortJump(done, Emitter.Position);
#endif
    }

    private static void HandleSh4Exception(Sh4Context context, Sh4InstructionException exception, uint pc)
    {
        if ((pc & 1) != 0)
        {
            exception.AdjustForDelaySlot();
            pc--;
        }
        context.RaiseException(pc, exception.Event);
        context.CycleCounter += 4;
        ((delegate* unmanaged<void>)ExceptionHandlerAddress)();
        if (OperatingSystem.IsWindows())
            AbortWindows();
        else
            AbortUnix();
    }

    [DllImport("ucrtbase.dll", EntryPoint = "abort", CallingConvention = CallingConvention.Cdecl)]
    private static extern void AbortWindows();

    [DllImport("libc", EntryPoint = "abort", CallingConvention = CallingConvention.Cdecl)]
    private static extern void AbortUnix();

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void InterpreterFallback(nint contextAddress, ushort opcode, nuint handler, uint pc)
    {
        Sh4Context context = Sh4Context.FromNative(contextAddress);
        try { ((delegate* unmanaged[Fastcall]<nint, uint, void>)handler)(contextAddress, opcode); }
        catch (Sh4InstructionException exception) { HandleSh4Exception(context, exception, pc); }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void WriteStoreQueueWithMmu(uint address, nint contextAddress, uint pc)
    {
        Sh4Context context = Sh4Context.FromNative(contextAddress);
        try { ((delegate* unmanaged[Fastcall]<uint, nint, void>)context.StoreQueueWriter)(address, contextAddress); }
        catch (Sh4InstructionException exception) { HandleSh4Exception(context, exception, pc); }
    }

    private partial void GenerateOpcode(Sh4RuntimeBlockInfo block, bool optimise, Sh4ShilOpcode opcode)
    {
        switch (opcode.Operation)
        {
            case shop_ifb:
            {
                uint instruction = opcode.Source3.Immediate;
                if (MmuEnabled)
                {
                    EmitPushImmediate((uint)Sh4OpcodeTable.OpcodeDescriptors[instruction].NativeInterpreterOpcode);
                    EmitPushImmediate(GuestAddress(block, opcode, 1));
                }
                if (opcode.Source1.IsImmediate && opcode.Source1.Immediate != 0)
                    StoreAbsoluteImmediate(Context.NativeAddress + Sh4ContextLayout.Pc,
                        opcode.Source2.Immediate);
                Emitter.MoveImmediate((long)Context.NativeAddress, Rcx, Dword);
                Emitter.MoveImmediate(instruction, Rdx, Dword);
                if (!MmuEnabled)
                    GenerateCall(Sh4OpcodeTable.OpcodeDescriptors[instruction].NativeInterpreterOpcode);
                else
                    GenerateCall((nuint)(delegate* unmanaged[Fastcall]<nint, ushort, nuint, uint, void>)&InterpreterFallback);
                break;
            }

            case shop_mov64:
                Sh4HostRuntime.Verify(opcode.Destination.IsFloat64, "op.rd.is_r64f()");
                Sh4HostRuntime.Verify(opcode.Source1.IsFloat64, "op.rs1.is_r64f()");
#if ALLOC_F64
                MoveFloatRegister(MapXRegister(opcode.Destination, 0), MapXRegister(opcode.Source1, 0));
                MoveFloatRegister(MapXRegister(opcode.Destination, 1), MapXRegister(opcode.Source1, 1));
#else
                Sh4HostRuntime.Verify(!Allocator.IsAllocated(opcode.Destination), "!regalloc.IsAllocAny(op.rd)");
                EmitAbsoluteSse(0xf3, 0x7e, 0, (nuint)opcode.Source1.GetRegisterPointer(Context));
                EmitAbsoluteSse(0x66, 0xd6, 0, (nuint)opcode.Destination.GetRegisterPointer(Context));
#endif
                break;

            case shop_readm:
                if (!GenerateReadMemoryImmediate(opcode, block))
                {
                    ShilParameterToHostRegister(opcode.Source1, Rcx);
                    if (!opcode.Source3.IsNull)
                    {
                        if (opcode.Source3.IsImmediate)
                            Emitter.BinaryImmediate(X64Arithmetic.Add, unchecked((int)opcode.Source3.Immediate), Rcx, Dword);
                        else if (Allocator.IsGeneralAllocated(opcode.Source3))
                            Emitter.Binary(X64Arithmetic.Add, MapRegister(opcode.Source3), Rcx, Dword);
                        else
                            EmitAbsoluteInteger(0x03, (int)Rcx, (nuint)opcode.Source3.GetRegisterPointer(Context));
                    }

                    Sh4ShilParameter value = opcode.Destination;
                    Unsafe.SkipInit(out MemorySize valueSize);
                    switch (opcode.Size)
                    {
                        case 1:
                            valueSize = MemorySize.S8;
                            break;
                        case 2:
                            valueSize = MemorySize.S16;
                            break;
                        case 4:
                            valueSize = Allocator.IsFloatAllocated(value) ? MemorySize.F32 : MemorySize.S32;
                            break;
                        case 8:
                            valueSize = MemorySize.F64;
                            break;
                    }

                    if (Context.Mmu.Enabled)
                        FreezeXmm();
                    GenerateMmuLookup(block, opcode, 0);

                    int start = Emitter.Position;
                    CallAbsolute(MemoryHandlers[(int)(optimise ? MemoryPath.Fast : MemoryPath.Slow),
                        (int)valueSize, (int)MemoryOperation.Read]);
                    Sh4HostRuntime.Verify(Emitter.Position - start == 5, "getCurr() - start == 5");
                    if (Context.Mmu.Enabled)
                        ThawXmm();

                    if (valueSize <= MemorySize.S32)
                    {
                        HostRegisterToShilParameter(value, Rax);
                    }
                    else if (valueSize == MemorySize.F32)
                    {
                        HostRegisterToShilParameter(value, 0);
                    }
                    else
                    {
                        if (value.Count == 2 && Allocator.IsFloatAllocated(value))
                        {
                            MoveFloatRegister(MapXRegister(value, 0), 0);
                            MoveFloatRegister(MapXRegister(value, 1), 1);
                        }
                        else
                        {
                            Sh4HostRuntime.Verify(!Allocator.IsAllocated(value), "!regalloc.IsAllocAny(op.rd)");
                            nuint pointer = (nuint)value.GetRegisterPointer(Context);
                            EmitAbsoluteSse(0xF3, 0x11, 0, pointer);
                            EmitAbsoluteSse(0xF3, 0x11, 1, pointer + 4);
                        }
                    }
                }
                break;

            case shop_writem:
                if (!GenerateWriteMemoryImmediate(opcode, block))
                {
                    ShilParameterToHostRegister(opcode.Source1, Rcx);
                    if (!opcode.Source3.IsNull)
                    {
                        if (opcode.Source3.IsImmediate)
                            Emitter.BinaryImmediate(X64Arithmetic.Add, unchecked((int)opcode.Source3.Immediate), Rcx, Dword);
                        else if (Allocator.IsGeneralAllocated(opcode.Source3))
                            Emitter.Binary(X64Arithmetic.Add, MapRegister(opcode.Source3), Rcx, Dword);
                        else
                            EmitAbsoluteInteger(0x03, (int)Rcx, (nuint)opcode.Source3.GetRegisterPointer(Context));
                    }

                    Sh4ShilParameter value = opcode.Source2;
                    Unsafe.SkipInit(out MemorySize valueSize);
                    switch (opcode.Size)
                    {
                        case 1:
                            valueSize = MemorySize.S8;
                            break;
                        case 2:
                            valueSize = MemorySize.S16;
                            break;
                        case 4:
                            valueSize = Allocator.IsFloatAllocated(value) ? MemorySize.F32 : MemorySize.S32;
                            break;
                        case 8:
                            valueSize = MemorySize.F64;
                            break;
                    }

                    if (Context.Mmu.Enabled)
                        FreezeXmm();
                    GenerateMmuLookup(block, opcode, 1);

                    if (valueSize <= MemorySize.S32)
                    {
                        ShilParameterToHostRegister(value, Rdx);
                    }
                    else if (valueSize == MemorySize.F32)
                    {
                        ShilParameterToHostRegister(value, 0);
                    }
                    else
                    {
                        if (value.Count == 2 && Allocator.IsFloatAllocated(value))
                        {
                            MoveFloatRegister(0, MapXRegister(value, 0));
                            MoveFloatRegister(1, MapXRegister(value, 1));
                        }
                        else
                        {
                            nuint pointer = (nuint)value.GetRegisterPointer(Context);
                            EmitAbsoluteSse(0x66, 0x6E, 0, pointer);
                            EmitAbsoluteSse(0x66, 0x6E, 1, pointer + 4);
                        }
                    }

                    int start = Emitter.Position;
                    CallAbsolute(MemoryHandlers[(int)(optimise ? MemoryPath.Fast : MemoryPath.Slow),
                        (int)valueSize, (int)MemoryOperation.Write]);
                    Sh4HostRuntime.Verify(Emitter.Position - start == 5, "getCurr() - start == 5");
                    if (Context.Mmu.Enabled)
                        ThawXmm();
                }
                break;

            case shop_jcond:
            case shop_jdyn:
            case shop_mov32:
                GenerateBaseOpcode(opcode);
                break;

#if !CANONICAL_TEST
            case shop_sync_sr:
                GenerateCdeclCall(Sh4Context.UpdateSrEntry);
                break;

            case shop_sync_fpscr:
                Emitter.MoveImmediate((long)Context.NativeAddress, Rcx, Dword);
                GenerateCall(Sh4Context.UpdateFpscrEntry);
                break;

            case shop_pref:
            {
                int noStoreQueue = -1;
                if (opcode.Source1.IsImmediate)
                {
                    uint address = opcode.Source1.Immediate;
                    if ((address & 0xf000_0000) != 0xe000_0000)
                        break;
                    Emitter.MoveImmediate(address, Rcx, Dword);
                }
                else
                {
                    X64Register address = Rax;
                    if (Allocator.IsGeneralAllocated(opcode.Source1))
                        address = MapRegister(opcode.Source1);
                    else
                        MoveFromMemory(Rax, (nuint)opcode.Source1.GetRegisterPointer(Context));
                    Emitter.Move(address, Rcx, Dword);
                    Emitter.Shift(X64Shift.Right, 28, Rcx, Dword);
                    Emitter.BinaryImmediate(X64Arithmetic.Compare, 0xe, Rcx, Dword);
                    noStoreQueue = Emitter.ReserveShortJump(X64Condition.NotZero);
                    Emitter.Move(address, Rcx, Dword);
                }
                Emitter.MoveImmediate((long)Context.NativeAddress, Rdx, Dword);
                if (Context.Mmu.Enabled)
                {
                    EmitPushImmediate(GuestAddress(block, opcode, 1));
                    GenerateCall((nuint)(delegate* unmanaged[Fastcall]<uint, nint, uint, void>)&WriteStoreQueueWithMmu);
                }
                else
                {
                    FreezeXmm();
                    EmitAbsoluteInteger(0xff, 2, Context.NativeAddress + Sh4ContextLayout.StoreQueueWriter);
                    ThawXmm();
                }
                if (noStoreQueue >= 0)
                    Emitter.PatchShortJump(noStoreQueue, Emitter.Position);
                break;
            }

            case shop_mul_s64:
                Emitter.Move(MapRegister(opcode.Source1), Rax, Dword);
                if (opcode.Source2.IsRegister)
                    Emitter.Move(MapRegister(opcode.Source2), Rdx, Dword);
                else
                    Emitter.MoveImmediate(opcode.Source2.Immediate, Rdx, Dword);
                Emitter.Unary(X64Unary.MultiplySigned, Rdx, Dword);
                Emitter.Move(Rax, MapRegister(opcode.Destination), Dword);
                Emitter.Move(Rdx, MapRegister(opcode.Destination2), Dword);
                break;

            case shop_frswap:
                Emitter.MoveImmediate((long)opcode.Source1.GetRegisterPointer(Context), Rax, Dword);
                Emitter.MoveImmediate((long)opcode.Destination.GetRegisterPointer(Context), Rcx, Dword);
                for (int i = 0; i < 4; i++)
                {
                    if (i == 0)
                    {
                        Emitter.Append([0x0F, 0x28, 0x00]);
                        Emitter.Append([0x0F, 0x28, 0x09]);
                        Emitter.Append([0x0F, 0x29, 0x08]);
                        Emitter.Append([0x0F, 0x29, 0x01]);
                    }
                    else
                    {
                        byte offset = (byte)(i * 16);
                        Emitter.Append([0x0F, 0x28, 0x40, offset]);
                        Emitter.Append([0x0F, 0x28, 0x49, offset]);
                        Emitter.Append([0x0F, 0x29, 0x48, offset]);
                        Emitter.Append([0x0F, 0x29, 0x41, offset]);
                    }
                }
                break;
#endif

            default:
#if !CANONICAL_TEST
                if (!GenerateBaseOpcode(opcode))
#endif
                    Sh4ShilCanonical.Compile(Sh4RecompilerDriver.Current.NativeBackend, opcode);
                break;
        }
    }

    internal partial bool RewriteMemoryAccess(ref Sh4HostContext context)
    {
        uint returnAddress = *(uint*)context.StackPointer;
        if (context.ProgramCounter < _memoryHandlerStart || context.ProgramCounter >= _memoryHandlerEnd)
            return false;

        uint caller = unchecked(returnAddress + *(uint*)(returnAddress - 4));
        for (int size = 0; size < (int)MemorySize.Count; size++)
        {
            for (int operation = 0; operation < (int)MemoryOperation.Count; operation++)
            {
                if (MemoryHandlers[(int)MemoryPath.Fast, size, operation] != caller)
                    continue;

                int start = Emitter.Position;
                if (operation == (int)MemoryOperation.Write && size >= (int)MemorySize.S32 && (context.Eax >> 28) == 0xE)
                    CallAbsolute(MemoryHandlers[(int)MemoryPath.StoreQueue, size, (int)MemoryOperation.Write]);
                else
                    CallAbsolute(MemoryHandlers[(int)MemoryPath.Slow, size, operation]);
                Sh4HostRuntime.Verify(Emitter.Position - start == 5, "getCurr() - start == 5");

                Publish();

                context.ProgramCounter = returnAddress - 5;
                context.StackPointer += 4;
                context.Ecx = context.Eax;
                return true;
            }
        }
        Trace.TraceError("rewriteMemAccess code not found: hpc {0:x8} retadr 0x{1:x} acc {2:x8}",
            context.ProgramCounter, returnAddress, context.Eax);
        Sh4HostRuntime.Die("Failed to match the code");
        return false;
    }

    private static uint GuestAddress(Sh4RuntimeBlockInfo block, Sh4ShilOpcode opcode, int delayOffset) =>
        unchecked(block.VirtualAddress + (uint)(opcode.GuestOffset - (opcode.DelaySlot ? delayOffset : 0)));

    private void EmitAbsoluteInteger(byte opcode, int register, nuint address)
    {
        Span<byte> instruction = stackalloc byte[6];
        instruction[0] = opcode;
        instruction[1] = (byte)(0x05 | register << 3);
        BinaryPrimitives.WriteUInt32LittleEndian(instruction[2..], (uint)address);
        Emitter.Append(instruction);
    }

    private void EmitAbsoluteSse(byte prefix, byte opcode, int register, nuint address)
    {
        Emitter.Append([prefix, 0x0f]);
        EmitAbsoluteInteger(opcode, register, address);
    }

    private void StoreAbsoluteImmediate(nuint address, uint value)
    {
        EmitAbsoluteInteger(0xc7, 0, address);
        Span<byte> immediate = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(immediate, value);
        Emitter.Append(immediate);
    }

#if !SH4_STRICT_MODE
    private void EmitMmuTableLoad(nuint address)
    {
        Span<byte> instruction = stackalloc byte[7];
        instruction[0] = 0x8b;
        instruction[1] = 0x04;
        instruction[2] = 0x85;
        BinaryPrimitives.WriteUInt32LittleEndian(instruction[3..], (uint)address);
        Emitter.Append(instruction);
    }
#endif
}
#endif
