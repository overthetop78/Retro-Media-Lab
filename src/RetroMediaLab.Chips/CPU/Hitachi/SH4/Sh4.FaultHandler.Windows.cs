using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal static unsafe class Sh4WindowsFaultHandler
{
    private const uint ApplicationErrorMask = 0x20000000;
    private const uint AccessViolation = 0xC0000005;
    private const int ContinueExecution = -1;
    private const int ContinueSearch = 0;
    private const int ExecuteHandler = 1;

    private static nint _vectoredHandler;
    private static delegate* unmanaged[Stdcall]<ExceptionPointers*, int> _previousExceptionHandler;

    private static string GetThreadName()
    {
#if LIBRETRO
        return "";
#else
        return Sh4HostRuntime.GetThreadName();
#endif
    }

    private static void ReadContext(ExceptionPointers* exception, ref Sh4HostContext context)
    {
        switch (RuntimeInformation.ProcessArchitecture)
        {
            case Architecture.X64:
                var x64 = (X64Context*)exception->ContextRecord;
                context.ProgramCounter = (nuint)x64->Rip;
                context.StackPointer = (nuint)x64->Rsp;
                context.R9 = x64->R9;
                context.Rcx = x64->Rcx;
                break;
            case Architecture.Arm:
                var arm = (ArmContext*)exception->ContextRecord;
                context.ProgramCounter = arm->Pc;
                new ReadOnlySpan<uint>(arm->Registers, 15).CopyTo(context.Arm32Registers);
                break;
            case Architecture.Arm64:
                var arm64 = (Arm64Context*)exception->ContextRecord;
                context.ProgramCounter = (nuint)arm64->Pc;
                context.X0 = arm64->X0;
                break;
            case Architecture.X86:
                var x86 = (X86Context*)exception->ContextRecord;
                context.ProgramCounter = x86->Eip;
                context.StackPointer = x86->Esp;
                context.Eax = x86->Eax;
                context.Ecx = x86->Ecx;
                break;
        }
    }

    private static void WriteContext(ExceptionPointers* exception, ref Sh4HostContext context)
    {
        switch (RuntimeInformation.ProcessArchitecture)
        {
            case Architecture.X64:
                var x64 = (X64Context*)exception->ContextRecord;
                x64->Rip = context.ProgramCounter;
                x64->Rsp = context.StackPointer;
                x64->R9 = context.R9;
                x64->Rcx = context.Rcx;
                break;
            case Architecture.Arm:
                var arm = (ArmContext*)exception->ContextRecord;
                arm->Pc = (uint)context.ProgramCounter;
                context.Arm32Registers.CopyTo(new Span<uint>(arm->Registers, 15));
                break;
            case Architecture.Arm64:
                var arm64 = (Arm64Context*)exception->ContextRecord;
                arm64->Pc = context.ProgramCounter;
                arm64->X0 = context.X0;
                break;
            case Architecture.X86:
                var x86 = (X86Context*)exception->ContextRecord;
                x86->Eip = (uint)context.ProgramCounter;
                x86->Esp = (uint)context.StackPointer;
                x86->Eax = context.Eax;
                x86->Ecx = context.Ecx;
                break;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int ExceptionHandler(ExceptionPointers* exception)
    {
        uint code = exception->ExceptionRecord->Code;
        if (code < 0x80000000u || (code & ApplicationErrorMask) != 0)
            return ContinueSearch;

        if (code != AccessViolation)
        {
            if (_previousExceptionHandler != null)
            {
                int action = _previousExceptionHandler(exception);
                if (action != ExecuteHandler)
                    return action;
            }

            RaiseFailFastException(exception->ExceptionRecord, exception->ContextRecord, 0);
            return ContinueSearch;
        }

        nuint address = exception->ExceptionRecord->Information[1];
        if (Sh4MemoryWatch.WriteAccess(address))
            return ContinueExecution;
        if (Sh4BlockManager.HandleRamWriteAccess(address))
            return ContinueExecution;
        if (Sh4TextureCache.VramLockedWrite(address))
            return ContinueExecution;
#if !SH4_DYNAREC_NONE
        if (Sh4RecompilerDriver.Current.AddressSpace.BlockManagerLockedWrite(address))
            return ContinueExecution;
#endif

        Unsafe.SkipInit(out Sh4HostContext context);
        ReadContext(exception, ref context);
#if !SH4_DYNAREC_NONE
        if (Sh4NativeBackend.Current!.Rewrite(ref context, address))
        {
            WriteContext(exception, ref context);
            return ContinueExecution;
        }
#endif

        Trace.TraceError($"[GPF] Thread:{GetThreadName()} PC 0x{context.ProgramCounter:x} unhandled access to 0x{address:x}");
        if (_previousExceptionHandler != null)
            _previousExceptionHandler(exception);

        RaiseFailFastException(exception->ExceptionRecord, exception->ContextRecord, 0);
        return ContinueSearch;
    }

    internal static void Install()
    {
#if !TARGET_UWP
        if (Environment.Is64BitProcess)
        {
            _previousExceptionHandler = SetUnhandledExceptionFilter(null);
            _vectoredHandler = AddVectoredExceptionHandler(1, &ExceptionHandler);
            return;
        }
#endif
        _previousExceptionHandler = SetUnhandledExceptionFilter(&ExceptionHandler);
    }

    internal static void Uninstall()
    {
        try
        {
#if !TARGET_UWP
            if (Environment.Is64BitProcess)
                RemoveVectoredExceptionHandler(_vectoredHandler);
#endif
        }
        finally
        {
            SetUnhandledExceptionFilter(_previousExceptionHandler);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ExceptionPointers
    {
        internal ExceptionRecord* ExceptionRecord;
        internal void* ContextRecord;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ExceptionRecord
    {
        internal uint Code;
        internal uint Flags;
        internal ExceptionRecord* NestedRecord;
        internal void* ExceptionAddress;
        internal uint NumberParameters;
        internal ExceptionInformation Information;
    }

    [InlineArray(15)]
    private struct ExceptionInformation
    {
        private nuint _first;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct X64Context
    {
        [FieldOffset(128)] internal ulong Rcx;
        [FieldOffset(152)] internal ulong Rsp;
        [FieldOffset(192)] internal ulong R9;
        [FieldOffset(248)] internal ulong Rip;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct ArmContext
    {
        [FieldOffset(4)] internal fixed uint Registers[15];
        [FieldOffset(64)] internal uint Pc;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct Arm64Context
    {
        [FieldOffset(8)] internal ulong X0;
        [FieldOffset(264)] internal ulong Pc;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct X86Context
    {
        [FieldOffset(172)] internal uint Ecx;
        [FieldOffset(176)] internal uint Eax;
        [FieldOffset(184)] internal uint Eip;
        [FieldOffset(196)] internal uint Esp;
    }

    [DllImport("kernel32.dll", ExactSpelling = true)]
    private static extern void RaiseFailFastException(ExceptionRecord* exception, void* context, uint flags);

    [DllImport("kernel32.dll", ExactSpelling = true)]
    private static extern delegate* unmanaged[Stdcall]<ExceptionPointers*, int> SetUnhandledExceptionFilter(
        delegate* unmanaged[Stdcall]<ExceptionPointers*, int> filter);

    [DllImport("kernel32.dll", ExactSpelling = true)]
    private static extern nint AddVectoredExceptionHandler(uint first, delegate* unmanaged[Stdcall]<ExceptionPointers*, int> handler);

    [DllImport("kernel32.dll", ExactSpelling = true)]
    private static extern uint RemoveVectoredExceptionHandler(nint handler);
}
