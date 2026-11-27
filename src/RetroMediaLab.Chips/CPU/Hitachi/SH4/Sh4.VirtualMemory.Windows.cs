using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal static unsafe partial class Sh4VirtualMemory
{
    private const uint WindowsPageNoAccess = 0x01;
    private const uint WindowsPageReadOnly = 0x02;
    private const uint WindowsPageReadWrite = 0x04;
    private const uint WindowsPageExecuteRead = 0x20;
    private const uint WindowsPageExecuteReadWrite = 0x40;
    private const uint WindowsCommit = 0x1000;
    private const uint WindowsReserve = 0x2000;
    private const uint WindowsDecommit = 0x4000;
    private const uint WindowsRelease = 0x8000;
    private const uint WindowsTopDown = 0x100000;
    private const uint WindowsFileMapWrite = 0x02;
    private const uint WindowsFileMapRead = 0x04;
    private const uint WindowsFileMapExecute = 0x20;

    private static bool LockRegionWindows(nuint start, nuint length)
    {
        if (!WindowsVirtualProtect(start, length, WindowsPageReadOnly, out _))
        {
            Trace.TraceError($"VirtualProtect(0x{start:x}, {(uint)length:x}, RO) failed: {Marshal.GetLastPInvokeError()}");
            Sh4HostRuntime.Die("VirtualProtect(ro) failed");
        }
        return true;
    }

    private static bool UnlockRegionWindows(nuint start, nuint length)
    {
        if (!WindowsVirtualProtect(start, length, WindowsPageReadWrite, out _))
        {
            Trace.TraceError($"VirtualProtect(0x{start:x}, {(uint)length:x}, RW) failed: {Marshal.GetLastPInvokeError()}");
            Sh4HostRuntime.Die("VirtualProtect(rw) failed");
        }
        return true;
    }

    private static nuint ReserveWindowsRegion(nuint start, nuint length)
    {
        uint allocationType = WindowsReserve;
        if (start == 0)
            allocationType |= WindowsTopDown;
        return WindowsVirtualAlloc(start, length, allocationType, WindowsPageNoAccess);
    }

    private static bool ReleaseWindowsRegion(nuint start, nuint length)
    {
        return WindowsVirtualFree(start, 0, WindowsRelease);
    }

    private static nint _windowsMemoryHandle = -1;
    private static nint _windowsJitMemoryHandle = -1;
    private static nuint _windowsBaseAllocation;
    private static nuint _windowsRamBase;
    private static readonly List<nuint> WindowsUnmappedRegions = [];
    private static readonly List<nuint> WindowsMappedRegions = [];

#if TARGET_UWP
    private static delegate* unmanaged[Stdcall]<nint, uint, uint, uint, nuint, nuint, nuint> _windowsMapViewOfFileEx;
#endif

    private static bool InitializeWindows(ref nuint virtualMemoryBase, ref nuint controlBlock, nuint ramSize)
    {
#if TARGET_UWP
        if (_windowsMapViewOfFileEx == null)
        {
            nint kernel32 = WindowsGetModuleHandle("Kernel32.dll");
            _windowsMapViewOfFileEx = (delegate* unmanaged[Stdcall]<nint, uint, uint, uint, nuint, nuint, nuint>)
                WindowsGetProcAddress(kernel32, "MapViewOfFileEx");
            if (_windowsMapViewOfFileEx == null)
                return false;
        }
#endif
        WindowsUnmappedRegions.EnsureCapacity(32);
        WindowsMappedRegions.EnsureCapacity(32);

        _windowsMemoryHandle = WindowsCreateFileMapping(-1, 0, WindowsPageReadWrite, 0, (uint)ramSize, 0);

        nuint controlBlockSize = Sh4ContextLayout.ControlBlockSize;
        uint memorySize = (uint)(512 * 1024 * 1024 + controlBlockSize + 8 * 1024 * 1024);
        _windowsBaseAllocation = ReserveWindowsRegion(0, memorySize);
        controlBlock = _windowsBaseAllocation;
        virtualMemoryBase = _windowsBaseAllocation + controlBlockSize;
        _windowsRamBase = virtualMemoryBase;

        WindowsVirtualFree(_windowsBaseAllocation, 0, WindowsRelease);
        nuint basePointer = WindowsVirtualAlloc(_windowsBaseAllocation, controlBlockSize, WindowsReserve, WindowsPageNoAccess);
        Sh4HostRuntime.Verify(basePointer == _windowsBaseAllocation, "base_ptr == base_alloc");

        nuint fpcbSize = (nuint)Sh4ContextLayout.FpcbSize * (nuint)IntPtr.Size;
        nuint contextPointer = WindowsVirtualAlloc(controlBlock + fpcbSize, controlBlockSize - fpcbSize,
            WindowsCommit, WindowsPageReadWrite);
        Sh4HostRuntime.Verify(contextPointer == controlBlock + fpcbSize,
            "cntx_ptr == (u8*)p_sh4rcb + sizeof(p_sh4rcb->fpcb)");

        nuint address = WindowsVirtualAlloc(virtualMemoryBase, memorySize - controlBlockSize, WindowsReserve, WindowsPageNoAccess);
        Sh4HostRuntime.Verify(address == virtualMemoryBase, "ptr == *vmem_base_addr");
        WindowsUnmappedRegions.Add(address);
        return true;
    }

    private static void DestroyWindows()
    {
        WindowsVirtualFree(_windowsBaseAllocation, 0, WindowsRelease);
        WindowsCloseHandle(_windowsMemoryHandle);
    }

    private static void ResetMemoryWindows(nuint address, uint size)
    {
        WindowsVirtualFree(address, size, WindowsDecommit);
    }

    private static void AllocateOnDemandPageWindows(nuint address, uint size)
    {
        nuint pointer = WindowsVirtualAlloc(address, size, WindowsCommit, WindowsPageReadWrite);
        Sh4HostRuntime.Verify(pointer != 0, "p != nullptr");
    }

    private static void CreateMappingsWindows(ReadOnlySpan<Mapping> mappings)
    {
        foreach (nuint address in WindowsMappedRegions)
            WindowsUnmapViewOfFile(address);
        WindowsMappedRegions.Clear();
        foreach (nuint address in WindowsUnmappedRegions)
            ReleaseWindowsRegion(address, 0);
        WindowsUnmappedRegions.Clear();

        for (int i = 0; i < mappings.Length; i++)
        {
            ref readonly Mapping mapping = ref mappings[i];
            nuint addressRangeSize = (nuint)(mapping.EndAddress - mapping.StartAddress);
            uint protection = mapping.AllowWrites ? WindowsFileMapRead | WindowsFileMapWrite : WindowsFileMapRead;

            if (mapping.MemorySize == 0)
            {
                nuint address = _windowsRamBase + (nuint)mapping.StartAddress;
                nuint pointer = WindowsVirtualAlloc(address, addressRangeSize, WindowsReserve, WindowsPageNoAccess);
                Sh4HostRuntime.Verify(pointer == address, "ptr == &addrspace::ram_base[vmem_maps[i].start_address]");
                WindowsUnmappedRegions.Add(pointer);
            }
            else
            {
                uint mirrors = (uint)((ulong)addressRangeSize / mapping.MemorySize);
                Sh4HostRuntime.Verify((ulong)addressRangeSize % mapping.MemorySize == 0 && mirrors >= 1,
                    "(address_range_size % vmem_maps[i].memsize) == 0 && num_mirrors >= 1");

                for (uint j = 0; j < mirrors; j++)
                {
                    nuint offset = (nuint)(mapping.StartAddress + j * mapping.MemorySize);
                    nuint address = _windowsRamBase + offset;
#if TARGET_UWP
                    nuint pointer = _windowsMapViewOfFileEx(_windowsMemoryHandle, protection, 0,
                        (uint)mapping.MemoryOffset, (nuint)mapping.MemorySize, address);
#else
                    nuint pointer = WindowsMapViewOfFileEx(_windowsMemoryHandle, protection, 0,
                        (uint)mapping.MemoryOffset, (nuint)mapping.MemorySize, address);
#endif
                    Sh4HostRuntime.Verify(pointer == address, "ptr == &addrspace::ram_base[offset]");
                    WindowsMappedRegions.Add(pointer);
                }
            }
        }
    }

    private static nuint PrepareWindowsJitBlock(nuint size, delegate* managed<nuint, nuint, nuint> mapper)
    {
        nuint baseAddress = (nuint)(delegate* managed<ref nuint, ref nuint, nuint, bool>)&InitializeWindows
            & ~(nuint)0xfffff;
        for (nuint i = 0; i < 1800 * 1024 * 1024; i += 1024 * 1024)
        {
            nuint above = unchecked(baseAddress + i);
            nuint below = unchecked(baseAddress - i);

            if (above != 0 && above > baseAddress)
            {
                nuint pointer = mapper(above, size);
                if (pointer != 0)
                    return pointer;
            }
            if (below != 0 && below < baseAddress)
            {
                nuint pointer = mapper(below, size);
                if (pointer != 0)
                    return pointer;
            }
        }

        return 0;
    }

    private static nuint AllocateWindowsJitMemory(nuint address, nuint size)
    {
#if TARGET_UWP
        return WindowsVirtualAlloc(address, size, WindowsReserve | WindowsCommit, WindowsPageReadWrite);
#else
        return WindowsVirtualAlloc(address, size, WindowsReserve | WindowsCommit, WindowsPageExecuteReadWrite);
#endif
    }

    private static bool PrepareJitBlockWindows(nuint address, nuint size, out nuint readWriteExecuteAddress)
    {
        nuint pointer = PrepareWindowsJitBlock(size, &AllocateWindowsJitMemory);
        if (pointer == 0)
        {
            Unsafe.SkipInit(out readWriteExecuteAddress);
            return false;
        }

        readWriteExecuteAddress = pointer;
        nuint initializeAddress = (nuint)(delegate* managed<ref nuint, ref nuint, nuint, bool>)&InitializeWindows;
        Trace.TraceInformation($"Found code area at 0x{readWriteExecuteAddress:x}, not too far away from 0x{initializeAddress:x}");
        return true;
    }

    private static void ReleaseJitBlockWindows(nuint address, nuint size)
    {
        WindowsVirtualFree(address, 0, WindowsRelease);
    }

    private static nuint MapWindowsJitFile(nuint address, nuint size)
    {
        nuint pointer = WindowsVirtualAlloc(address, size, WindowsReserve, WindowsPageNoAccess);
        if (pointer == 0)
            return 0;
        WindowsVirtualFree(pointer, 0, WindowsRelease);
        if (pointer != address)
            return 0;

#if TARGET_UWP
        return WindowsMapViewOfFileFromApp(_windowsJitMemoryHandle, WindowsFileMapRead | WindowsFileMapExecute, 0, size);
#else
        return WindowsMapViewOfFileEx(_windowsJitMemoryHandle, WindowsFileMapRead | WindowsFileMapExecute, 0, 0, size, address);
#endif
    }

    private static bool PrepareJitBlockWindows(nuint address, nuint size, out nuint writableAddress, out nint executableOffset)
    {
        _windowsJitMemoryHandle = WindowsCreateFileMapping(-1, 0, WindowsPageExecuteReadWrite, 0, (uint)size, 0);
        nuint executable = PrepareWindowsJitBlock(size, &MapWindowsJitFile);
        if (executable == 0)
        {
            Unsafe.SkipInit(out writableAddress);
            Unsafe.SkipInit(out executableOffset);
            return false;
        }

#if TARGET_UWP
        nuint writable = WindowsMapViewOfFileFromApp(_windowsJitMemoryHandle, WindowsFileMapRead | WindowsFileMapWrite, 0, size);
#else
        nuint writable = WindowsMapViewOfFileEx(_windowsJitMemoryHandle, WindowsFileMapRead | WindowsFileMapWrite, 0, 0, size, 0);
#endif
        writableAddress = writable;
        executableOffset = unchecked((nint)(executable - writable));
        Trace.TraceInformation($"Info: Using NO_RWX mode, rx ptr: 0x{executable:x}, rw ptr: 0x{writable:x}, offset: {unchecked((uint)executableOffset)}");
        return writable != 0;
    }

    private static void ReleaseJitBlockWindows(nuint writableAddress, nuint executableAddress, nuint size)
    {
        WindowsUnmapViewOfFile(writableAddress);
        WindowsUnmapViewOfFile(executableAddress);
        WindowsCloseHandle(_windowsJitMemoryHandle);
    }

    private static void SetJitExecutableWindows(nuint address, nuint size, bool enabled)
    {
#if TARGET_UWP
        if (!WindowsVirtualProtect(address, size, enabled ? WindowsPageExecuteRead : WindowsPageReadWrite, out _))
        {
            Trace.TraceError($"VirtualProtect(0x{address:x}, {(uint)size:x}, {(enabled ? "RX" : "RW")}) failed: {Marshal.GetLastPInvokeError()}");
            Sh4HostRuntime.Die("VirtualProtect(rx/rw) failed");
        }
#endif
    }

    private static void FlushWindowsArmCache(nuint start, nuint end)
    {
        if (start == end)
            return;
        WindowsFlushInstructionCache(WindowsGetCurrentProcess(), start, end - start);
    }

    private static void FlushCacheWindows(nuint instructionStart, nuint instructionEnd, nuint dataStart, nuint dataEnd)
    {
        if (RuntimeInformation.ProcessArchitecture is not (Architecture.Arm64 or Architecture.Arm))
            return;

        FlushWindowsArmCache(dataStart, dataEnd);
        if (instructionStart != dataStart)
            FlushWindowsArmCache(instructionStart, instructionEnd);
    }

    [DllImport("kernel32.dll", EntryPoint = "VirtualProtect", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WindowsVirtualProtect(nuint address, nuint size, uint protection, out uint oldProtection);

    [DllImport("kernel32.dll", EntryPoint = "VirtualAlloc", SetLastError = true)]
    private static extern nuint WindowsVirtualAlloc(nuint address, nuint size, uint type, uint protection);

    [DllImport("kernel32.dll", EntryPoint = "VirtualFree", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WindowsVirtualFree(nuint address, nuint size, uint type);

    [DllImport("kernel32.dll", EntryPoint = "CreateFileMappingW", SetLastError = true)]
    private static extern nint WindowsCreateFileMapping(nint file, nuint attributes, uint protection,
        uint sizeHigh, uint sizeLow, nuint name);

    [DllImport("kernel32.dll", EntryPoint = "CloseHandle", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WindowsCloseHandle(nint handle);

    [DllImport("kernel32.dll", EntryPoint = "UnmapViewOfFile", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WindowsUnmapViewOfFile(nuint address);

    [DllImport("kernel32.dll", EntryPoint = "MapViewOfFileEx", SetLastError = true)]
    private static extern nuint WindowsMapViewOfFileEx(nint mapping, uint access, uint offsetHigh, uint offsetLow,
        nuint size, nuint address);

#if TARGET_UWP
    [DllImport("kernel32.dll", EntryPoint = "GetModuleHandleW", CharSet = CharSet.Unicode)]
    private static extern nint WindowsGetModuleHandle(string name);

    [DllImport("kernel32.dll", EntryPoint = "GetProcAddress", CharSet = CharSet.Ansi)]
    private static extern nuint WindowsGetProcAddress(nint module, string name);

    [DllImport("kernel32.dll", EntryPoint = "MapViewOfFileFromApp")]
    private static extern nuint WindowsMapViewOfFileFromApp(nint mapping, uint access, ulong offset, nuint size);
#endif

    [DllImport("kernel32.dll", EntryPoint = "GetCurrentProcess")]
    private static extern nint WindowsGetCurrentProcess();

    [DllImport("kernel32.dll", EntryPoint = "FlushInstructionCache")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WindowsFlushInstructionCache(nint process, nuint address, nuint size);
}
