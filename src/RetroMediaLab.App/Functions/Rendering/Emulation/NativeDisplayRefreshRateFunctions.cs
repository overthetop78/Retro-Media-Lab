using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RetroMediaLab.App.Functions.Rendering.Emulation;

internal static class NativeDisplayRefreshRateFunctions
{
    private const int CurrentDisplaySettings = -1;
    private const uint MonitorDefaultToNearest = 2;

    internal static void RunClock(IntPtr window, CancellationToken cancellation, Action<long, double> present)
    {
        var clock = Stopwatch.StartNew();
        while (!cancellation.IsCancellationRequested)
        {
            var (deviceName, refreshRate) = GetDisplayMode(window);
            if (deviceName is not null && refreshRate > 0)
                RunMonitor(window, deviceName, refreshRate, clock, cancellation, present);
            if (cancellation.IsCancellationRequested) return;
            present(0, 0);
            cancellation.WaitHandle.WaitOne(TimeSpan.FromMilliseconds(250));
        }
    }

    private static void RunMonitor(IntPtr window, string deviceName, double refreshRate, Stopwatch clock,
        CancellationToken cancellation, Action<long, double> present)
    {
        var dc = CreateDC("DISPLAY", deviceName, null, IntPtr.Zero);
        if (dc == IntPtr.Zero) return;
        try
        {
            var adapter = new OpenAdapter { DeviceContext = dc };
            if (D3DKMTOpenAdapterFromHdc(ref adapter) != 0) return;
            try
            {
                var blank = new VerticalBlank { Adapter = adapter.Adapter, SourceId = adapter.SourceId };
                var checkedAt = clock.Elapsed;
                long refresh = 0;
                while (!cancellation.IsCancellationRequested && D3DKMTWaitForVerticalBlankEvent(in blank) == 0)
                {
                    if (cancellation.IsCancellationRequested) return;
                    var now = clock.Elapsed;
                    if (now - checkedAt >= TimeSpan.FromSeconds(1))
                    {
                        var current = GetDisplayMode(window);
                        if (current.DeviceName != deviceName || current.RefreshRate <= 0) return;
                        refreshRate = current.RefreshRate;
                        checkedAt = now;
                    }
                    present(++refresh, refreshRate);
                }
            }
            finally
            {
                var close = new CloseAdapter { Adapter = adapter.Adapter };
                D3DKMTCloseAdapter(in close);
            }
        }
        finally { DeleteDC(dc); }
    }

    private static (string? DeviceName, double RefreshRate) GetDisplayMode(IntPtr window)
    {
        if (window == IntPtr.Zero) return default;
        var monitor = MonitorFromWindow(window, MonitorDefaultToNearest);
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor, ref info)) return default;

        var mode = new DisplayMode { Size = (ushort)Marshal.SizeOf<DisplayMode>() };
        return EnumDisplaySettings(info.DeviceName, CurrentDisplaySettings, ref mode) && mode.DisplayFrequency > 1
            ? (info.DeviceName, mode.DisplayFrequency) : default;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);

    [DllImport("user32.dll", EntryPoint = "GetMonitorInfoW", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

    [DllImport("user32.dll", EntryPoint = "EnumDisplaySettingsW", CharSet = CharSet.Unicode, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplaySettings(string deviceName, int modeIndex, ref DisplayMode mode);

    [DllImport("gdi32.dll", EntryPoint = "CreateDCW", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern IntPtr CreateDC(string driverName, string deviceName, string? output, IntPtr mode);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteDC(IntPtr deviceContext);

    [DllImport("gdi32.dll")]
    private static extern int D3DKMTOpenAdapterFromHdc(ref OpenAdapter adapter);

    [DllImport("gdi32.dll")]
    private static extern int D3DKMTWaitForVerticalBlankEvent(in VerticalBlank blank);

    [DllImport("gdi32.dll")]
    private static extern int D3DKMTCloseAdapter(in CloseAdapter adapter);

    [StructLayout(LayoutKind.Sequential)]
    private struct OpenAdapter
    {
        internal IntPtr DeviceContext;
        internal uint Adapter;
        internal uint AdapterLuidLow;
        internal int AdapterLuidHigh;
        internal uint SourceId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct VerticalBlank
    {
        internal uint Adapter;
        internal uint Device;
        internal uint SourceId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CloseAdapter
    {
        internal uint Adapter;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct Rectangle(int Left, int Top, int Right, int Bottom);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MonitorInfo
    {
        internal int Size;
        internal Rectangle Monitor;
        internal Rectangle WorkArea;
        internal uint Flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        internal string DeviceName;
    }

    // DEVMODEW's printer and display members share a union. Only these members are used.
    [StructLayout(LayoutKind.Explicit, Size = 220)]
    private struct DisplayMode
    {
        [FieldOffset(68)] internal ushort Size;
        [FieldOffset(184)] internal uint DisplayFrequency;
    }
}
