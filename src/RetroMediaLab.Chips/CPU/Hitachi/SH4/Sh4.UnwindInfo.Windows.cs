using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal unsafe partial class Sh4UnwindInfo
{
    private enum WindowsUnwindOperation
    {
        PushNonvolatile = 0,
        AllocateLarge = 1,
        AllocateSmall = 2,
        SetFrameRegister = 3,
        SaveNonvolatile = 4,
        SaveNonvolatileFar = 5,
        SaveXmm128 = 8,
        SaveXmm128Far = 9,
        PushMachineFrame = 10
    }

    private partial void StartWindows(nuint address)
    {
        _startAddress = address;
        _codes.Clear();
    }

    private partial void PushRegisterWindows(uint offset, int register)
    {
        _codes.Add(unchecked((ushort)(offset | ((uint)WindowsUnwindOperation.PushNonvolatile << 8)
            | (uint)(register << 12))));
    }

    private partial void AllocateStackWindows(uint offset, int size)
    {
        Sh4HostRuntime.Verify(size <= 128, "size <= 128");
        Sh4HostRuntime.Verify((size & 7) == 0, "(size & 7) == 0");

        _codes.Add(unchecked((ushort)(offset | ((uint)WindowsUnwindOperation.AllocateSmall << 8)
            | (uint)((size / 8 - 1) << 12))));
    }

    private partial void EndPrologWindows(uint offset)
    {
        _codes.Add(0);
        _codes.Add(0);
        _codes.Reverse();
        _codes[0] = unchecked((ushort)(1 | (offset << 8)));
        _codes[1] = unchecked((ushort)((byte)_codes.Count - 2));
        if ((_codes.Count & 1) != 0)
            _codes.Add(0);
    }

    private partial nuint EndWindows(uint offset, nint writableToExecutableOffset)
    {
        nuint endAddress = _startAddress + offset;
        if ((endAddress & 3) != 0)
            offset += 4 - (uint)(endAddress & 3);

        nuint unwindInfo = _startAddress + offset;
        CollectionsMarshal.AsSpan(_codes).CopyTo(new Span<ushort>((void*)unwindInfo, _codes.Count));
        var table = (WindowsRuntimeFunction*)(unwindInfo + (nuint)_codes.Count * sizeof(ushort));
        table->BeginAddress = 0;
        table->EndAddress = (uint)(endAddress - _startAddress);
#if SH4_MINGW64
        table->UnwindData = (uint)(unwindInfo - _startAddress);
#else
        table->UnwindInfoAddress = (uint)(unwindInfo - _startAddress);
#endif

        bool result = RtlAddFunctionTable(table, 1, _startAddress);
        _tables.Add((nuint)table);
        Debug.WriteLine($"RtlAddFunctionTable 0x{_startAddress:x} sz {table->EndAddress} rc {(result ? 1 : 0)} tables: {_tables.Count}");

        return unwindInfo + (nuint)_codes.Count * sizeof(ushort) + (nuint)sizeof(WindowsRuntimeFunction) - endAddress;
    }

    private partial void ClearWindows()
    {
        Debug.WriteLine("UnwindInfo::clear");
        foreach (nuint table in _tables)
            RtlDeleteFunctionTable((WindowsRuntimeFunction*)table);
        _tables.Clear();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowsRuntimeFunction
    {
        internal uint BeginAddress;
        internal uint EndAddress;
#if SH4_MINGW64
        internal uint UnwindData;
#else
        internal uint UnwindInfoAddress;
#endif
    }

    [DllImport("kernel32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static extern bool RtlAddFunctionTable(WindowsRuntimeFunction* table, uint count, ulong baseAddress);

    [DllImport("kernel32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static extern bool RtlDeleteFunctionTable(WindowsRuntimeFunction* table);

}
