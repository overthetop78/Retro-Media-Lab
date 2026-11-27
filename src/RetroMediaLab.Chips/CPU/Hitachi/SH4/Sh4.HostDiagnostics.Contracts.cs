using System.Runtime.CompilerServices;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal static partial class Sh4HostRuntime
{
    internal static void Verify(
        bool condition,
        [CallerArgumentExpression(nameof(condition))] string expression = "",
        [CallerMemberName] string function = "",
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
        if (!condition)
        {
            FatalError($"Verify Failed  : {expression}\n in {function} -> {file} : {line}");
            DebugBreak();
        }
    }

    internal static void Die(
        string reason,
        [CallerMemberName] string function = "",
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
        FatalError($"Fatal error : {reason}\n in {function} -> {file} : {line}");
        DebugBreak();
    }

    private static void DebugBreak()
    {
        if (OperatingSystem.IsWindows())
            DebugBreakWindows();
        else
#if SH4_APPLE && !SH4_UNIX && !LIBRETRO && !SH4_SWITCH && !SH4_HAIKU
            Sh4PosixHostRuntime.DebugBreak();
#else
            DebugBreakUnix();
#endif
    }
}
