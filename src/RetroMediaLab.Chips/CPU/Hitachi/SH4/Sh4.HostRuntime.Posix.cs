#if SH4_UNIX || SH4_APPLE || SH4_ANDROID || SH4_SWITCH || SH4_HAIKU
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal static unsafe class Sh4PosixHostRuntime
{
#if !SH4_SWITCH
    private static Sh4PosixSignalAction _nextSegvHandler;
#endif
#if SH4_APPLE
    private static Sh4PosixSignalAction _nextBusHandler;
#endif
#if !SH4_APPLE
    private static readonly byte[] ThreadNameBuffer = new byte[17];
#endif

    [UnmanagedCallersOnly]
    internal static void FaultHandler(int signal, Sh4PosixSignalInfo* info, void* signalContext)
    {
        nuint address = info->Address;
        if (Sh4MemoryWatch.WriteAccess(address))
            return;
        if (Sh4BlockManager.HandleRamWriteAccess(address))
            return;
        if (Sh4TextureCache.VramLockedWrite(address))
            return;

#if !SH4_DYNAREC_NONE
        if (Sh4RecompilerDriver.Current.AddressSpace.BlockManagerLockedWrite(address))
            return;

        Sh4HostContext context = default;
        Sh4PosixHostContext.FromSegfault(ref context, signalContext);
        if (Sh4NativeBackend.Current!.Rewrite(ref context, address))
        {
            Sh4PosixHostContext.ToSegfault(ref context, signalContext);
            return;
        }

        Trace.TraceError($"SIGSEGV @ 0x{context.ProgramCounter:x} invalid access to 0x{address:x}");
#endif

#if SH4_SWITCH
        Sh4SwitchHost.QueryMemory(out Sh4SwitchMemoryInfo memory, out _, Sh4SwitchHost.StartAddress);
        Trace.TraceError($".text base: 0x{memory.Address:x} -> offset: {context.ProgramCounter - memory.Address:x}");
        Sh4HostRuntime.Die("segfault");
#else
        if (_nextSegvHandler.SignalAction != null)
            _nextSegvHandler.SignalAction(signal, info, signalContext);
        else
            Sh4HostRuntime.Die("segfault");
#endif
    }

    internal static void InstallFaultHandler()
    {
#if !SH4_SWITCH
        Sh4PosixSignalAction action = default;
        action.Handler = Sh4PosixSignals.Ignore;
        Sh4PosixSignals.Sigaction(Sh4PosixSignals.Sigpipe, &action, null);

        action.SignalAction = &FaultHandler;
        Sh4PosixSignals.Sigemptyset(&action.Mask);
        action.Flags = Sh4PosixSignals.SaSiginfo;
        fixed (Sh4PosixSignalAction* next = &_nextSegvHandler)
            Sh4PosixSignals.Sigaction(Sh4PosixSignals.Sigsegv, &action, next);
#endif
#if SH4_APPLE
        fixed (Sh4PosixSignalAction* next = &_nextBusHandler)
            Sh4PosixSignals.Sigaction(Sh4PosixSignals.Sigbus, &action, next);
#endif
    }

    internal static void UninstallFaultHandler()
    {
#if !SH4_SWITCH
        fixed (Sh4PosixSignalAction* next = &_nextSegvHandler)
            Sh4PosixSignals.Sigaction(Sh4PosixSignals.Sigsegv, next, null);
#endif
#if SH4_APPLE
        fixed (Sh4PosixSignalAction* next = &_nextBusHandler)
            Sh4PosixSignals.Sigaction(Sh4PosixSignals.Sigbus, next, null);
#endif
    }

#if SH4_APPLE && !SH4_UNIX && !LIBRETRO && !SH4_SWITCH && !SH4_HAIKU
    internal static void DebugBreak()
    {
        Sh4HostRuntime.Trap();
    }
#endif

    private static void EnableRunFast()
    {
#if !SH4_ARMCC
        if (RuntimeInformation.ProcessArchitecture == Architecture.Arm)
        {
            const uint mask = 0x04086060;
            const uint value = 0x03000000;
            uint fpscr = Sh4VirtualMemory.ReadPosixFpscr();
            fpscr &= mask;
            fpscr |= value;
            Sh4VirtualMemory.WritePosixFpscr(fpscr);
            Debug.WriteLine("ARM VFP-Run Fast (NFP) enabled !");
        }
#endif
    }

    private static void LinuxFixPersonality()
    {
        if (OperatingSystem.IsLinux() && !OperatingSystem.IsAndroid()
            && RuntimeInformation.ProcessArchitecture is Architecture.X86 or Architecture.Arm)
        {
            Debug.WriteLine($"Personality: {Personality(uint.MaxValue):X8}");
            Personality(unchecked((uint)Personality(uint.MaxValue)) & ~0x00400000u);
            Debug.WriteLine($"Updated personality: {Personality(uint.MaxValue):X8}");
        }
    }

#if SH4_UNIX && !LIBRETRO && !SH4_ANDROID
    [UnmanagedCallersOnly]
    private static void SigintHandler(int signal)
    {
        Sh4DreamcastRuntime.Exit();
    }
#endif

    internal static void Setup()
    {
        LinuxFixPersonality();
        EnableRunFast();
        InstallFaultHandler();
#if SH4_UNIX && !LIBRETRO && !SH4_ANDROID
        Sh4PosixSignals.Signal(Sh4PosixSignals.Sigint, &SigintHandler);
#endif
        Debug.WriteLine($"Linux paging: sysconf {Sysconf(Sh4PosixHostAbi.ScPageSize)} PAGE_SIZE {Sh4PosixHostAbi.PageSize} PAGE_MASK {Sh4PosixHostAbi.PageMask:X}");
        Sh4HostRuntime.Verify(Sh4PosixHostAbi.PageMask == Sysconf(Sh4PosixHostAbi.ScPageSize) - 1,
            "PAGE_MASK==(sysconf(_SC_PAGESIZE)-1)");
    }

#if !SH4_APPLE
    internal static void SetThreadName(byte* name)
    {
        if (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid())
        {
            fixed (byte* buffer = ThreadNameBuffer)
            {
                if (Strlen(name) > 16)
                {
                    new ReadOnlySpan<byte>(name, 16).CopyTo(new Span<byte>(buffer, 16));
                    name = buffer;
                }
                PthreadSetName(PthreadSelf(), name);
            }
        }
    }

    [DllImport("libc", EntryPoint = "strlen")]
    private static extern nuint Strlen(byte* value);

    [DllImport("libc", EntryPoint = "pthread_self")]
    private static extern nuint PthreadSelf();

    [DllImport("libc", EntryPoint = "pthread_setname_np")]
    private static extern int PthreadSetName(nuint thread, byte* name);
#endif

    [DllImport("libc", EntryPoint = "personality")]
    private static extern int Personality(nuint value);

    [DllImport("libc", EntryPoint = "sysconf")]
    private static extern nint Sysconf(int name);
}
#endif
