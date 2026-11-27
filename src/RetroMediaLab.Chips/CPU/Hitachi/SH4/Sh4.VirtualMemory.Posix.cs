using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal static unsafe partial class Sh4VirtualMemory
{
    private const int PosixNone = 0;
    private const int PosixRead = 1;
    private const int PosixWrite = 2;
    private const int PosixExec = 4;
    private const int PosixShared = 1;
    private const int PosixPrivate = 2;
    private const int PosixFixed = 0x10;
    private const int PosixJit = 0x800;
    private const int PosixReadWrite = 2;
    private const int PosixDontNeed = 4;
    private static readonly nuint PosixFailed = unchecked((nuint)(-1));
    private static bool PosixApple => OperatingSystem.IsMacOS() || OperatingSystem.IsIOS()
        || OperatingSystem.IsTvOS() || OperatingSystem.IsMacCatalyst();
    private static bool PosixBsd => PosixApple || OperatingSystem.IsFreeBSD()
        || OperatingSystem.IsOSPlatform("OPENBSD") || OperatingSystem.IsOSPlatform("NETBSD");
    private static int PosixAnonymous => PosixBsd ? 0x1000 : 0x20;
    private static int PosixNoSync => OperatingSystem.IsFreeBSD() ? 0x0800 : 0;
    private static int PosixCreate => PosixBsd ? 0x0200 : 0x0040;
    private static int PosixExclusive => PosixBsd ? 0x0800 : 0x0080;
    private static int PosixTruncate => PosixBsd ? 0x0400 : 0x0200;
    private static nuint PosixPageMask => (nuint)(OperatingSystem.IsIOS() || OperatingSystem.IsTvOS() ? 16384 : 4096) - 1;

    private static int CreateAshmemRegion(string name, nuint size)
    {
        int fd = -1;
        if (NativeLibrary.TryLoad("libandroid.so", out nint library))
        {
            try
            {
                if (NativeLibrary.TryGetExport(library, "ASharedMemory_create", out nint entry))
                {
                    nint nativeName = Marshal.StringToCoTaskMemUTF8(name);
                    try
                    {
                        var create = (delegate* unmanaged<byte*, nuint, int>)entry;
                        fd = create((byte*)nativeName, size);
                        if (fd < 0)
                            Trace.TraceWarning($"ASharedMemory_create failed: errno {PosixErrno()}");
                    }
                    finally
                    {
                        Marshal.FreeCoTaskMem(nativeName);
                    }
                }
            }
            finally
            {
                NativeLibrary.Free(library);
            }
        }

        if (fd < 0)
        {
            fd = PosixOpen("/dev/ashmem", PosixReadWrite, 0);
            nuint setSize = (nuint)(0x40007703 | (IntPtr.Size << 16));
            if (fd >= 0 && PosixIoctl(fd, setSize, size) < 0)
            {
                PosixClose(fd);
                fd = -1;
            }
        }
        return fd;
    }

    private static bool LockRegionPosix(nuint start, nuint length)
    {
        nuint inPage = start & PosixPageMask;
        if (PosixProtect(start - inPage, length + inPage, PosixRead) != 0)
            Sh4HostRuntime.Die("mprotect failed...");
        return true;
    }

    private static bool UnlockRegionPosix(nuint start, nuint length)
    {
        nuint inPage = start & PosixPageMask;
        if (PosixProtect(start - inPage, length + inPage, PosixRead | PosixWrite) != 0)
            Sh4HostRuntime.Die("mprotect  failed...");
        return true;
    }

    private static bool SetRegionExecutablePosix(nuint start, nuint length)
    {
        nuint inPage = start & PosixPageMask;
        int protection = PosixRead | PosixExec;
        if (!OperatingSystem.IsIOS() && !OperatingSystem.IsTvOS())
            protection |= PosixWrite;

        if (PosixProtect(start - inPage, length + inPage, protection) != 0)
        {
            Trace.TraceWarning($"region_set_exec: mprotect failed. errno {Marshal.GetLastPInvokeError()}");
            return false;
        }
        return true;
    }

    private static nuint ReservePosixRegion(nuint start, nuint length)
    {
        nuint pointer = PosixMap(start, length, PosixNone, PosixPrivate | PosixAnonymous, -1, 0);
        if (pointer == PosixFailed)
        {
            PosixPerror("mmap");
            return 0;
        }
        return pointer;
    }

    private static bool ReleasePosixRegion(nuint start, nuint length)
    {
        return PosixUnmap(start, length) == 0;
    }

    private static nuint MapPosixRegionFile(int file, nuint destination, nuint length, nuint offset, bool readWrite)
    {
        int flags = PosixShared | PosixNoSync | (destination != 0 ? PosixFixed : 0);
        nuint pointer = PosixMap(destination, length, PosixRead | (readWrite ? PosixWrite : 0), flags, file, (long)offset);
        if (pointer == PosixFailed)
        {
            PosixPerror("mmap");
            return 0;
        }
        return pointer;
    }

    private static int AllocatePosixSharedFileMemory(uint size)
    {
        int fd = -1;
        if (OperatingSystem.IsAndroid())
        {
            fd = CreateAshmemRegion("RAM", size);
        }
        else
        {
            if (!PosixApple)
            {
                fd = PosixShmOpen("/dcnzorz_mem", PosixCreate | PosixExclusive | PosixReadWrite, 0x180);
                PosixShmUnlink("/dcnzorz_mem");
            }
            if (fd < 0)
            {
                string path = Sh4Platform.GetWritableDataPath("dcnzorz_mem");
                fd = PosixOpen(path, PosixCreate | PosixReadWrite | PosixTruncate, 0x1ff);
                PosixUnlink(path);
            }
            if (fd >= 0 && PosixSetLength(fd, size) != 0)
            {
                PosixClose(fd);
                fd = -1;
            }
        }

        if (fd < 0)
            Trace.TraceWarning($"Virtual memory file allocation failed: errno {Marshal.GetLastPInvokeError()}");
        return fd;
    }

    private static int _posixVirtualMemoryFile = -1;
    private static nuint _posixReservedBase;
    private static nuint _posixReservedSize;
    private static nuint _posixRamBase;

    private static bool InitializePosix(ref nuint virtualMemoryBase, ref nuint controlBlock, nuint ramSize)
    {
        _posixVirtualMemoryFile = AllocatePosixSharedFileMemory((uint)ramSize);
        if (_posixVirtualMemoryFile < 0)
            return false;

        nuint controlBlockSize = Sh4ContextLayout.ControlBlockSize;
        _posixReservedSize = 512 * 1024 * 1024 + controlBlockSize + 8 * 1024 * 1024 + 0x10000;
        _posixReservedBase = ReservePosixRegion(0, _posixReservedSize);
        if (_posixReservedBase == 0)
        {
            PosixClose(_posixVirtualMemoryFile);
            return false;
        }

        nuint pointer = (_posixReservedBase + 0x10000 - 1) & ~(nuint)0xffff;
        controlBlock = pointer;
        virtualMemoryBase = pointer + controlBlockSize;
        _posixRamBase = virtualMemoryBase;
        nuint fpcbSize = (nuint)Sh4ContextLayout.FpcbSize * (nuint)IntPtr.Size;
        nuint controlBlockBase = pointer + fpcbSize;
        UnlockRegionPosix(controlBlockBase, controlBlockSize - fpcbSize);
        return true;
    }

    private static void DestroyPosix()
    {
        if (_posixReservedBase != 0)
        {
            ReleasePosixRegion(_posixReservedBase, _posixReservedSize);
            _posixReservedBase = 0;
        }
        if (_posixVirtualMemoryFile >= 0)
        {
            PosixClose(_posixVirtualMemoryFile);
            _posixVirtualMemoryFile = -1;
        }
        if (_posixArmCacheCode != 0)
        {
            PosixUnmap(_posixArmCacheCode, CodeCacheAlignment);
            _posixArmCacheCode = 0;
        }
    }

    private static void ResetMemoryPosix(nuint address, uint size)
    {
        PosixProtect(address, size, PosixNone);
        PosixAdvise(address, size, PosixDontNeed);
        if (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid())
            PosixAdvise(address, size, 9);
        else if (PosixBsd)
            PosixAdvise(address, size, 5);
    }

    private static void AllocateOnDemandPagePosix(nuint address, uint size)
    {
        bool unlocked = UnlockRegionPosix(address, size);
        Sh4HostRuntime.Verify(unlocked);
    }

    private static void CreateMappingsPosix(ReadOnlySpan<Mapping> mappings)
    {
        for (int i = 0; i < mappings.Length; i++)
        {
            ref readonly Mapping mapping = ref mappings[i];
            if (mapping.MemorySize == 0)
                continue;

            ulong addressRangeSize = mapping.EndAddress - mapping.StartAddress;
            uint mirrors = (uint)(addressRangeSize / mapping.MemorySize);
            Sh4HostRuntime.Verify(addressRangeSize % mapping.MemorySize == 0 && mirrors >= 1);
            for (uint j = 0; j < mirrors; j++)
            {
                ulong offset = mapping.StartAddress + j * mapping.MemorySize;
                nuint pointer = MapPosixRegionFile(_posixVirtualMemoryFile, _posixRamBase + (nuint)offset,
                    (nuint)mapping.MemorySize, (nuint)mapping.MemoryOffset, mapping.AllowWrites);
                Sh4HostRuntime.Verify(pointer != 0);
            }
        }
    }

    private static bool PrepareJitBlockPosix(nuint address, nuint size, out nuint readWriteExecuteAddress)
    {
        Unsafe.SkipInit(out readWriteExecuteAddress);
        if (address != 0 && SetRegionExecutablePosix(address, size))
        {
            readWriteExecuteAddress = address;
            return true;
        }

        const int protection = PosixRead | PosixWrite | PosixExec;
        nuint pointer;
        if (!OperatingSystem.IsMacOS())
        {
            pointer = PosixFailed;
            if (address != 0)
            {
                PosixUnmap(address, size);
                pointer = PosixMap(address, size, protection, PosixFixed | PosixPrivate | PosixAnonymous, 0, 0);
            }
            if (pointer == PosixFailed)
                pointer = PosixMap(0, size, protection, PosixPrivate | PosixAnonymous, -1, 0);
        }
        else
        {
            pointer = PosixMap(0, size, protection, PosixPrivate | PosixAnonymous | PosixJit, -1, 0);
        }
        if (pointer == PosixFailed)
            return false;
        readWriteExecuteAddress = pointer;
        return true;
    }

    private static void ReleaseJitBlockPosix(nuint address, nuint size)
    {
        PosixUnmap(address, size);
    }

    private static bool PrepareJitBlockPosix(nuint address, nuint size, out nuint writableAddress, out nint executableOffset)
    {
        Unsafe.SkipInit(out writableAddress);
        Unsafe.SkipInit(out executableOffset);
        int fd = AllocatePosixSharedFileMemory((uint)size);
        if (fd < 0)
            return false;

        nuint executable;
        nuint writable;
        try
        {
            PosixUnmap(address, size);
            executable = PosixMap(address, size, PosixRead | PosixExec, PosixShared | PosixNoSync | PosixFixed, fd, 0);
            if (executable != address)
                return false;

            writable = PosixMap(0, size, PosixRead | PosixWrite, PosixShared | PosixNoSync, fd, 0);
            writableAddress = writable;
            executableOffset = unchecked((nint)(executable - writable));
        }
        finally
        {
            PosixClose(fd);
        }

        Trace.TraceInformation($"Info: Using NO_RWX mode, rx ptr: 0x{executable:x}, rw ptr: 0x{writable:x}, offset: {executableOffset}");
        return writable != PosixFailed;
    }

    private static void ReleaseJitBlockPosix(nuint writableAddress, nuint executableAddress, nuint size)
    {
        PosixUnmap(executableAddress, size);
    }

    private static void SetJitExecutablePosix(nuint address, nuint size, bool enabled)
    {
    }

    private static nuint _posixInstructionCacheLine = 0xffff;
    private static nuint _posixDataCacheLine = 0xffff;

    private static void FlushPosixArm64Cache(nuint start, nuint end)
    {
        if (start == end)
            return;

        if (PosixApple)
        {
            DarwinCacheControl(1, start, end - start);
        }
        else
        {
            ulong ctrEl0 = Sh4Arm64CacheInstructions.ReadCtrEl0();
            nuint instructionSize = (nuint)(4u << (int)(ctrEl0 & 0xf));
            nuint dataSize = (nuint)(4u << (int)((ctrEl0 >> 16) & 0xf));

            _posixInstructionCacheLine = instructionSize = _posixInstructionCacheLine < instructionSize
                ? _posixInstructionCacheLine : instructionSize;
            _posixDataCacheLine = dataSize = _posixDataCacheLine < dataSize ? _posixDataCacheLine : dataSize;

            for (nuint address = start & ~(dataSize - 1); address < end; address += dataSize)
                Sh4Arm64CacheInstructions.DcCivac(address);
            Sh4Arm64CacheInstructions.DsbIsh();

            for (nuint address = start & ~(instructionSize - 1); address < end; address += instructionSize)
                Sh4Arm64CacheInstructions.IcIvau(address);

            Sh4Arm64CacheInstructions.DsbIsh();
            Sh4Arm64CacheInstructions.Isb();
        }
    }

    private static void FlushPosixArmCache(nuint start, nuint end)
    {
        if (PosixApple)
        {
            DarwinDataCacheFlush(start, end - start + 1);
            DarwinInstructionCacheInvalidate(start, end - start + 1);
        }
#if SH4_ARMCC
        else
        {
            Sh4ArmCacheInstructions.CacheFlushSystemCall((uint)start, (uint)end, 0);
        }
#else
        else if (OperatingSystem.IsAndroid())
        {
            Sh4ArmCacheInstructions.CacheFlushSystemCall((uint)start, (uint)(end + 4), 0);
        }
        else
        {
            PosixClearCache(start, end);
        }
#endif
    }

    private static void FlushCachePosix(nuint instructionStart, nuint instructionEnd, nuint dataStart, nuint dataEnd)
    {
        if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
        {
            FlushPosixArm64Cache(dataStart, dataEnd);
            if (instructionStart != dataStart)
                FlushPosixArm64Cache(instructionStart, instructionEnd);
        }
        else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm)
        {
            FlushPosixArmCache(instructionStart, instructionEnd);
        }
    }

    private static nuint _posixArmCacheCode;

    private static nuint PreparePosixArmCacheCode(ReadOnlySpan<uint> instructions)
    {
        if (_posixArmCacheCode != 0)
            return _posixArmCacheCode;

        nuint address = PosixMap(0, CodeCacheAlignment, PosixRead | PosixWrite, PosixPrivate | PosixAnonymous, -1, 0);
        Sh4HostRuntime.Verify(address != PosixFailed);
        try
        {
            instructions.CopyTo(new Span<uint>((void*)address, instructions.Length));
            Sh4HostRuntime.Verify(PosixProtect(address, CodeCacheAlignment, PosixRead | PosixExec) == 0);
            _posixArmCacheCode = address;
            return address;
        }
        finally
        {
            if (_posixArmCacheCode != address)
                PosixUnmap(address, CodeCacheAlignment);
        }
    }

    private static class Sh4Arm64CacheInstructions
    {
        private static ReadOnlySpan<uint> Instructions =>
        [
            0xd53b0020, 0xd65f03c0,
            0xd50b7e20, 0xd65f03c0,
            0xd5033b9f, 0xd65f03c0,
            0xd50b7520, 0xd65f03c0,
            0xd5033fdf, 0xd65f03c0
        ];

        private static nuint Code => PreparePosixArmCacheCode(Instructions);

        internal static ulong ReadCtrEl0() => ((delegate* unmanaged<ulong>)Code)();

        internal static void DcCivac(nuint address) => ((delegate* unmanaged<nuint, void>)(Code + 8))(address);

        internal static void DsbIsh() => ((delegate* unmanaged<void>)(Code + 16))();

        internal static void IcIvau(nuint address) => ((delegate* unmanaged<nuint, void>)(Code + 24))(address);

        internal static void Isb() => ((delegate* unmanaged<void>)(Code + 32))();
    }

    private static class Sh4ArmCacheInstructions
    {
        private static ReadOnlySpan<uint> Instructions =>
        [
#if SH4_ARM_THUMB && !SH4_ARMCC
#if SH4_ARM_OABI
            0x4718a300, 0xef9f0002, 0xe28f3001, 0xe12fff13, 0x00004770,
#else
            0x4718a300, 0xe52d7004, 0xe3a0780f, 0xe2877002, 0xef000000,
            0xe49d7004, 0xe28f3001, 0xe12fff13, 0x00004770,
#endif
#elif SH4_ARM_OABI && !SH4_ARMCC
            0xef9f0002, 0xe12fff1e,
#else
            0xe52d7004, 0xe3a0780f, 0xe2877002, 0xef000000, 0xe49d7004, 0xe12fff1e,
#endif
        ];

        private static nuint Code => PreparePosixArmCacheCode(Instructions);

        internal static void CacheFlushSystemCall(uint start, uint end, uint flags)
        {
#if SH4_ARM_THUMB && !SH4_ARMCC
            ((delegate* unmanaged<uint, uint, uint, void>)(Code | 1))(start, end, flags);
#else
            ((delegate* unmanaged<uint, uint, uint, void>)Code)(start, end, flags);
#endif
        }
    }

    [DllImport("libc", EntryPoint = "mmap", SetLastError = true)]
    private static extern nuint PosixMap(nuint start, nuint length, int protection, int flags, int fd, long offset);

    [DllImport("libc", EntryPoint = "munmap", SetLastError = true)]
    private static extern int PosixUnmap(nuint address, nuint length);

    [DllImport("libc", EntryPoint = "mprotect", SetLastError = true)]
    private static extern int PosixProtect(nuint address, nuint length, int protection);

    [DllImport("libc", EntryPoint = "madvise", SetLastError = true)]
    private static extern int PosixAdvise(nuint address, nuint length, int advice);

    [DllImport("libc", EntryPoint = "open", SetLastError = true)]
    private static extern int PosixOpen([MarshalAs(UnmanagedType.LPUTF8Str)] string path, int flags, uint mode);

    [DllImport("libc", EntryPoint = "close", SetLastError = true)]
    private static extern int PosixClose(int fd);

    [DllImport("libc", EntryPoint = "ioctl", SetLastError = true)]
    private static extern int PosixIoctl(int fd, nuint request, nuint value);

    [DllImport("libc", EntryPoint = "shm_open", SetLastError = true)]
    private static extern int PosixShmOpen([MarshalAs(UnmanagedType.LPUTF8Str)] string name, int flags, uint mode);

    [DllImport("libc", EntryPoint = "shm_unlink", SetLastError = true)]
    private static extern int PosixShmUnlink([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport("libc", EntryPoint = "unlink", SetLastError = true)]
    private static extern int PosixUnlink([MarshalAs(UnmanagedType.LPUTF8Str)] string path);

    [DllImport("libc", EntryPoint = "ftruncate", SetLastError = true)]
    private static extern int PosixSetLength(int fd, long size);

    [DllImport("libc", EntryPoint = "perror")]
    private static extern void PosixPerror([MarshalAs(UnmanagedType.LPUTF8Str)] string message);

    [DllImport("libc", EntryPoint = "__errno")]
    private static extern int* PosixErrnoAddress();

    private static int PosixErrno() => *PosixErrnoAddress();

    [DllImport("libgcc_s.so.1", EntryPoint = "__clear_cache")]
    private static extern void PosixClearCache(nuint start, nuint end);

    [DllImport("/usr/lib/libSystem.B.dylib", EntryPoint = "sys_cache_control")]
    private static extern int DarwinCacheControl(int operation, nuint start, nuint length);

    [DllImport("/usr/lib/libSystem.B.dylib", EntryPoint = "sys_dcache_flush")]
    private static extern void DarwinDataCacheFlush(nuint start, nuint length);

    [DllImport("/usr/lib/libSystem.B.dylib", EntryPoint = "sys_icache_invalidate")]
    private static extern void DarwinInstructionCacheInvalidate(nuint start, nuint length);
}
