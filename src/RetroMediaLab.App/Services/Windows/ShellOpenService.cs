using System.Diagnostics;

namespace RetroMediaLab.App.Services.Windows;

internal static class ShellOpenService
{
    internal static void Open(string target)
    {
        Process? request = null;
        try { request = Process.Start(new ProcessStartInfo(target) { UseShellExecute = true }); }
        finally { request?.Dispose(); }
    }
}
