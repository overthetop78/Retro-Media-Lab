using System.ComponentModel;
using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.Common.Compilation.X64;

/// <summary>Owns one immutable Windows x64 executable allocation.</summary>
internal sealed class X64CodeBuffer : SafeHandle
{
    // RCX/RDX carry borrowed register-file/state pointers. The owner pins them
    // for the call; this buffer never owns or releases either pointer.
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate ulong Entry(nint registers, nint state, nint memory, nint links);
    private Entry? _entry;
    private nint _functionTable;
    public int Length { get; }
    public override bool IsInvalid => handle == 0;

    private X64CodeBuffer(int length) : base(0, true) => Length = length;

    public static X64CodeBuffer Create(ReadOnlySpan<byte> code) => Create(code, default);

    /// <summary>Registers optional Windows v1 unwind data owned by this allocation.</summary>
    public static unsafe X64CodeBuffer Create(ReadOnlySpan<byte> code, ReadOnlySpan<byte> unwindInfo)
    {
        if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64)
            throw new PlatformNotSupportedException("Native code execution requires Windows x64.");
        if (code.IsEmpty) throw new ArgumentException("Native code cannot be empty.", nameof(code));
        if (!unwindInfo.IsEmpty && (unwindInfo.Length < 4 || unwindInfo[0] != 1
            || unwindInfo[1] > code.Length || unwindInfo[3] != 0
            || unwindInfo.Length != ((4 + unwindInfo[2] * 2 + 3) & ~3)))
            throw new ArgumentException("Expected aligned Windows x64 UNWIND_INFO v1 without a frame register or handlers.", nameof(unwindInfo));
        var buffer = new X64CodeBuffer(code.Length);
        int unwindOffset = checked((code.Length + 3) & ~3);
        int tableOffset = checked(unwindOffset + unwindInfo.Length);
        int size = unwindInfo.IsEmpty ? code.Length : checked(tableOffset + 12);
        bool published = false;
        try
        {
            buffer.SetHandle(VirtualAlloc(0, (nuint)size, 0x3000, 4));
            if (buffer.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());
            code.CopyTo(new Span<byte>((void*)buffer.handle, code.Length));
            nint table = 0;
            if (!unwindInfo.IsEmpty)
            {
                unwindInfo.CopyTo(new Span<byte>((void*)(buffer.handle + unwindOffset), unwindInfo.Length));
                table = buffer.handle + tableOffset;
                uint* entry = (uint*)table;
                entry[0] = 0; entry[1] = (uint)code.Length; entry[2] = (uint)unwindOffset;
            }
            if (!VirtualProtect(buffer.handle, (nuint)size, 0x20, out _)
                || !FlushInstructionCache(GetCurrentProcess(), buffer.handle, (nuint)code.Length))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            if (table != 0)
            {
                if (!RtlAddFunctionTable(table, 1, unchecked((ulong)buffer.handle)))
                    throw new InvalidOperationException("Windows rejected the native function's unwind table.");
                buffer._functionTable = table;
            }
            buffer._entry = Marshal.GetDelegateForFunctionPointer<Entry>(buffer.handle);
            published = true;
            return buffer;
        }
        finally { if (!published) buffer.Dispose(); }
    }

    public uint Execute(nint registers, nint state, nint memory = 0) => unchecked((uint)ExecuteLinked(registers, state, memory, 0));

    internal ulong ExecuteLinked(nint registers, nint state, nint memory, nint links)
    {
        if (registers == 0) throw new ArgumentNullException(nameof(registers));
        if (state == 0) throw new ArgumentNullException(nameof(state));
        bool borrowed = false;
        try
        {
            DangerousAddRef(ref borrowed); // Prevent release during an in-flight call.
            return _entry!(registers, state, memory, links);
        }
        finally { if (borrowed) DangerousRelease(); }
    }

    protected override bool ReleaseHandle()
    {
        // Remove OS references before releasing their backing allocation.
        // Existing execution leases keep the allocation alive during callbacks.
        if (_functionTable != 0 && !RtlDeleteFunctionTable(_functionTable)) return false;
        _functionTable = 0;
        return VirtualFree(handle, 0, 0x8000);
    }
    [DllImport("kernel32.dll")] [return: MarshalAs(UnmanagedType.U1)] private static extern bool RtlAddFunctionTable(nint table, uint count, ulong baseAddress);
    [DllImport("kernel32.dll")] [return: MarshalAs(UnmanagedType.U1)] private static extern bool RtlDeleteFunctionTable(nint table);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern nint VirtualAlloc(nint address, nuint size, uint type, uint protection);
    [DllImport("kernel32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool VirtualProtect(nint address, nuint size, uint protection, out uint old);
    [DllImport("kernel32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool VirtualFree(nint address, nuint size, uint type);
    [DllImport("kernel32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool FlushInstructionCache(nint process, nint address, nuint size);
    [DllImport("kernel32.dll")] private static extern nint GetCurrentProcess();
}

