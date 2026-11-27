using System.Diagnostics;
using System.Text;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal static partial class Sh4HostRuntime
{
    internal static void FatalError(string text)
    {
        Span<byte> buffer = stackalloc byte[2048];
        Encoding.UTF8.GetEncoder().Convert(text.AsSpan(), buffer[..^1], true,
            out _, out int written, out _);
        buffer[written] = 0;

        ReadOnlySpan<byte> formatted = buffer[..written];
        int terminator = formatted.IndexOf((byte)0);
        if (terminator >= 0)
            formatted = formatted[..terminator];

        string message = Encoding.UTF8.GetString(formatted);
        Trace.TraceError(message);
        Sh4HostNotifications.Notify("Fatal Error", 20000, message);
    }
}
