using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Win32.SafeHandles;

namespace RetroMediaLab.App.Theme.Common;

/// <summary>Temporarily clips a borrowed native surface, owning regions but never its window.</summary>
internal sealed class ThemeNativeWindowRegion : IDisposable
{
    private const int RegionError = 0;
    private const int RegionIntersection = 1;
    private const int RegionDifference = 4;
    private const int RegionCopy = 5;
    private readonly HwndHost owner;
    private readonly nint window;
    private readonly Region original;
    private readonly bool hasOriginal;
    private Rect? previousVisible;
    private Rect[] previousExclusions = [];
    private bool changed;
    private bool disposed;

    internal ThemeNativeWindowRegion(HwndHost owner)
    {
        owner.VerifyAccess();
        this.owner = owner;
        window = owner.Handle;
        if (window == nint.Zero) throw new ArgumentException(nameof(owner));
        original = Region.Create(Rect.Empty);
        try { hasOriginal = GetWindowRgn(window, original.DangerousGetHandle()) != RegionError; }
        catch { original.Dispose(); throw; }
    }

    internal nint Handle => window;
    internal bool IsCurrent => !disposed && owner.Handle == window;

    internal Rect WindowBounds()
    {
        if (!IsCurrent) return Rect.Empty;
        if (!GetWindowRect(window, out var bounds)) throw new Win32Exception(Marshal.GetLastWin32Error());
        return bounds.Right > bounds.Left && bounds.Bottom > bounds.Top
            ? new Rect(bounds.Left, bounds.Top, bounds.Right - bounds.Left, bounds.Bottom - bounds.Top) : Rect.Empty;
    }

    internal void Update(Rect visible, IReadOnlyList<Rect> exclusions)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        owner.VerifyAccess();
        if (!IsCurrent) return;
        if (previousVisible == visible && previousExclusions.SequenceEqual(exclusions)) return;
        using var region = Region.Create(visible);
        if (hasOriginal) Combine(region, original, RegionIntersection);
        foreach (var exclusion in exclusions)
        {
            using var removed = Region.Create(exclusion);
            Combine(region, removed, RegionDifference);
        }
        Apply(region);
        previousVisible = visible;
        previousExclusions = exclusions.ToArray();
        changed = true;
    }

    private static void Combine(Region destination, Region other, int mode)
    {
        if (CombineRgn(destination.DangerousGetHandle(), destination.DangerousGetHandle(), other.DangerousGetHandle(), mode) == RegionError)
            throw new Win32Exception(Marshal.GetLastWin32Error());
    }

    private void Apply(Region region)
    {
        if (SetWindowRgn(window, region.DangerousGetHandle(), true) == RegionError)
            throw new Win32Exception(Marshal.GetLastWin32Error());
        region.SetHandleAsInvalid(); // Successful SetWindowRgn transfers this HRGN to Windows.
    }

    public void Dispose()
    {
        if (disposed) return;
        owner.VerifyAccess();
        try
        {
            if (!changed || !IsCurrent) return;
            if (hasOriginal)
            {
                using var restored = Region.Create(Rect.Empty);
                if (CombineRgn(restored.DangerousGetHandle(), original.DangerousGetHandle(), nint.Zero, RegionCopy) == RegionError)
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                Apply(restored);
            }
            else if (SetWindowRgn(window, nint.Zero, true) == RegionError)
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        finally
        {
            disposed = true;
            previousExclusions = [];
            original.Dispose();
        }
    }

    private sealed class Region : SafeHandleZeroOrMinusOneIsInvalid
    {
        private Region(nint handle) : base(true) => SetHandle(handle);

        internal static Region Create(Rect rectangle)
        {
            var handle = rectangle.IsEmpty ? CreateRectRgn(0, 0, 0, 0)
                : CreateRectRgn(checked((int)Math.Floor(rectangle.Left)), checked((int)Math.Floor(rectangle.Top)),
                    checked((int)Math.Ceiling(rectangle.Right)), checked((int)Math.Ceiling(rectangle.Bottom)));
            if (handle == nint.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
            return new Region(handle);
        }

        protected override bool ReleaseHandle() => DeleteObject(handle);
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct NativeRect(int Left, int Top, int Right, int Bottom);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(nint window, out NativeRect rectangle);

    [DllImport("user32.dll")]
    private static extern int GetWindowRgn(nint window, nint region);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowRgn(nint window, nint region, [MarshalAs(UnmanagedType.Bool)] bool redraw);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern nint CreateRectRgn(int left, int top, int right, int bottom);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern int CombineRgn(nint destination, nint source, nint other, int mode);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(nint region);
}
