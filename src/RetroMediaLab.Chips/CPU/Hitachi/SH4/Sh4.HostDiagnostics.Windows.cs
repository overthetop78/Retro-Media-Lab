using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal static partial class Sh4HostRuntime
{
    private static void DebugBreakWindows()
    {
        NativeWindowsDebugBreak();
        AbortWindows();
    }

    internal static string GetThreadName()
    {
        try
        {
            if (GetThreadDescription(GetCurrentThread(), out nint name) < 0)
                return "?";

            try
            {
                return Marshal.PtrToStringUni(name) ?? "?";
            }
            finally
            {
                LocalFree(name);
            }
        }
        catch (EntryPointNotFoundException)
        {
            return "?";
        }
    }

    [DllImport("kernel32.dll", EntryPoint = "DebugBreak", ExactSpelling = true)]
    private static extern void NativeWindowsDebugBreak();

    [DllImport("ucrtbase.dll", EntryPoint = "abort", CallingConvention = CallingConvention.Cdecl)]
    private static extern void AbortWindows();

    [DllImport("kernelbase.dll", ExactSpelling = true)]
    private static extern int GetThreadDescription(nint thread, out nint name);

    [DllImport("kernel32.dll", ExactSpelling = true)]
    private static extern nint GetCurrentThread();

    [DllImport("kernel32.dll", ExactSpelling = true)]
    private static extern nint LocalFree(nint address);
}
