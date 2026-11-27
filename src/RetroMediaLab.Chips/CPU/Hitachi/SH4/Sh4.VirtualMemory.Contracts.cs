using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal static partial class Sh4VirtualMemory
{
    private const nuint CodeCacheAlignment = 4096;

    internal static nuint DeclareCodeCache(nuint size)
    {
        if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() || OperatingSystem.IsIOS()
            || OperatingSystem.IsTvOS() || OperatingSystem.IsMacCatalyst())
        {
            return 0;
        }

        if (OperatingSystem.IsAndroid() || OperatingSystem.IsOSPlatform("OPENBSD")
            || OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD() || OperatingSystem.IsOSPlatform("NETBSD")
            || OperatingSystem.IsOSPlatform("SWITCH") || OperatingSystem.IsOSPlatform("HAIKU"))
        {
            nuint address = ReservePosixRegion(0, size);
            if (address == 0)
                throw new OutOfMemoryException("The SH-4 code cache address space could not be reserved.");

            return address;
        }

        throw new PlatformNotSupportedException("Unknown platform for dynarec code cache declaration.");
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Mapping
    {
        internal ulong StartAddress;
        internal ulong EndAddress;
        internal ulong MemoryOffset;
        internal ulong MemorySize;

        [MarshalAs(UnmanagedType.U1)]
        internal bool AllowWrites;
    }

    internal static bool Initialize(ref nuint virtualMemoryBase, ref nuint controlBlock, nuint ramSize) =>
        OperatingSystem.IsWindows()
            ? InitializeWindows(ref virtualMemoryBase, ref controlBlock, ramSize)
            : InitializePosix(ref virtualMemoryBase, ref controlBlock, ramSize);

    internal static void ResetMemory(nuint address, uint size)
    {
        if (OperatingSystem.IsWindows())
            ResetMemoryWindows(address, size);
        else
            ResetMemoryPosix(address, size);
    }

    internal static void AllocateOnDemandPage(nuint address, uint size)
    {
        if (OperatingSystem.IsWindows())
            AllocateOnDemandPageWindows(address, size);
        else
            AllocateOnDemandPagePosix(address, size);
    }

    internal static void CreateMappings(ReadOnlySpan<Mapping> mappings)
    {
        if (OperatingSystem.IsWindows())
            CreateMappingsWindows(mappings);
        else
            CreateMappingsPosix(mappings);
    }

    internal static void Destroy()
    {
        if (OperatingSystem.IsWindows())
            DestroyWindows();
        else
            DestroyPosix();
    }

    internal static bool PrepareJitBlock(nuint address, nuint size, out nuint readWriteExecuteAddress) =>
        OperatingSystem.IsWindows()
            ? PrepareJitBlockWindows(address, size, out readWriteExecuteAddress)
            : PrepareJitBlockPosix(address, size, out readWriteExecuteAddress);

    internal static bool PrepareJitBlock(nuint address, nuint size, out nuint writableAddress,
        out nint executableOffset) =>
        OperatingSystem.IsWindows()
            ? PrepareJitBlockWindows(address, size, out writableAddress, out executableOffset)
            : PrepareJitBlockPosix(address, size, out writableAddress, out executableOffset);

    internal static void FlushCache(nuint instructionStart, nuint instructionEnd, nuint dataStart, nuint dataEnd)
    {
        if (OperatingSystem.IsWindows())
            FlushCacheWindows(instructionStart, instructionEnd, dataStart, dataEnd);
        else
            FlushCachePosix(instructionStart, instructionEnd, dataStart, dataEnd);
    }

    internal static void SetJitExecutable(nuint address, nuint size, bool enabled)
    {
        if (OperatingSystem.IsWindows())
            SetJitExecutableWindows(address, size, enabled);
        else
            SetJitExecutablePosix(address, size, enabled);
    }

    internal static void ReleaseJitBlock(nuint address, nuint size)
    {
        if (OperatingSystem.IsWindows())
            ReleaseJitBlockWindows(address, size);
        else
            ReleaseJitBlockPosix(address, size);
    }

    internal static void ReleaseJitBlock(nuint writableAddress, nuint executableAddress, nuint size)
    {
        if (OperatingSystem.IsWindows())
            ReleaseJitBlockWindows(writableAddress, executableAddress, size);
        else
            ReleaseJitBlockPosix(writableAddress, executableAddress, size);
    }

    internal static nuint Untag(nuint address)
    {
        if (OperatingSystem.IsAndroid() && RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            return (nuint)((ulong)address & 0x00ffffffffffffffUL);
        return address;
    }

    internal static bool LockRegion(nuint address, nuint length) =>
        OperatingSystem.IsWindows() ? LockRegionWindows(address, length) : LockRegionPosix(address, length);

    internal static bool UnlockRegion(nuint address, nuint length) =>
        OperatingSystem.IsWindows() ? UnlockRegionWindows(address, length) : UnlockRegionPosix(address, length);

    internal static bool SetRegionExecutable(nuint address, nuint length) =>
        SetRegionExecutablePosix(address, length);
}
