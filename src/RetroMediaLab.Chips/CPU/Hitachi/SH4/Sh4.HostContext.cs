using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

[StructLayout(LayoutKind.Sequential)]
internal struct Sh4HostContext
{
    internal nuint ProgramCounter;
    private Registers _registers;

    [UnscopedRef]
    internal ref uint Eax => ref Unsafe.As<nuint, uint>(ref _registers[0]);

    [UnscopedRef]
    internal ref uint Ecx => ref Unsafe.As<nuint, uint>(ref Unsafe.Add(ref _registers[0], 1));

    [UnscopedRef]
    internal ref nuint StackPointer => ref (IntPtr.Size == sizeof(uint)
        ? ref Unsafe.Add(ref _registers[0], 2)
        : ref _registers[0]);

    [UnscopedRef]
    internal ref ulong R9 => ref Unsafe.As<nuint, ulong>(ref Unsafe.Add(ref _registers[0], 1));

    [UnscopedRef]
    internal ref ulong Rcx => ref Unsafe.As<nuint, ulong>(ref Unsafe.Add(ref _registers[0], 2));

    [UnscopedRef]
    internal ref ulong Rdi => ref Unsafe.As<nuint, ulong>(ref Unsafe.Add(ref _registers[0], 2));

    [UnscopedRef]
    internal Span<uint> Arm32Registers => MemoryMarshal.CreateSpan(ref Unsafe.As<nuint, uint>(ref _registers[0]), 15);

    [UnscopedRef]
    internal ref ulong X0 => ref Unsafe.As<nuint, ulong>(ref _registers[0]);

#if SH4_HOST_ARM
    [InlineArray(15)]
#elif SH4_HOST_ARM64
    [InlineArray(1)]
#else
    [InlineArray(3)]
#endif
    private struct Registers
    {
        private nuint _first;
    }
}
