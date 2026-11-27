using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal static partial class Sh4HostRuntime
{
    private const int Sigtrap = 5;

    private static void DebugBreakUnix()
    {
        RaiseSignal(Sigtrap);
        AbortUnix();
    }

    [DllImport("libc", EntryPoint = "raise", CallingConvention = CallingConvention.Cdecl)]
    private static extern int RaiseSignal(int signal);

    [DllImport("libc", EntryPoint = "abort", CallingConvention = CallingConvention.Cdecl)]
    private static extern void AbortUnix();
}
