using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using RetroMediaLab.App.Constants.Input.Windows;

namespace RetroMediaLab.App.Theme.Common;

/// <summary>Coordinates native airspace with internal WPF windows without owning their renderers.</summary>
internal sealed class ThemeNativeWorkspaceHost : IDisposable
{
    private sealed record Surface(HwndHost View, FrameworkElement Layer, ThemeNativeWindowRegion Region);
    private static readonly DependencyProperty OccludesInteriorProperty = DependencyProperty.RegisterAttached(
        "OccludesInterior", typeof(bool), typeof(ThemeNativeWorkspaceHost), new PropertyMetadata(true));
    private readonly Canvas workspace;
    private readonly Dictionary<HwndHost, Surface> surfaces = [];
    private bool updating;
    private bool disposed;
    private DispatcherOperation? pendingUpdate;

    internal ThemeNativeWorkspaceHost(Canvas workspace)
    {
        workspace.VerifyAccess();
        this.workspace = workspace;
        try
        {
            workspace.LayoutUpdated += LayoutUpdated;
            workspace.Unloaded += WorkspaceUnloaded;
            ComponentDispatcher.ThreadFilterMessage += FilterNativeMessage;
        }
        catch { Dispose(); throw; }
    }

    internal event Action<FrameworkElement>? NativePointerPressed;

    internal static void SetOccludesInterior(DependencyObject element, bool value) => element.SetValue(OccludesInteriorProperty, value);

    private void LayoutUpdated(object? sender, EventArgs args)
    {
        if (disposed || pendingUpdate is not null) return;
        pendingUpdate = workspace.Dispatcher.BeginInvoke(DispatcherPriority.Render, () =>
        {
            pendingUpdate = null;
            if (!disposed) Update();
        });
    }

    internal void Update()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        workspace.VerifyAccess();
        if (updating || PresentationSource.FromVisual(workspace) is null) return;
        updating = true;
        try
        {
            var layers = workspace.Children.OfType<FrameworkElement>().Where(item => item.IsVisible)
                .OrderBy(Panel.GetZIndex).ToArray();
            var encountered = new HashSet<HwndHost>();
            for (var index = 0; index < layers.Length; index++)
            {
                foreach (var view in NativeSurfaces(layers[index]))
                {
                    if (!view.IsVisible || view.Handle == nint.Zero) continue;
                    encountered.Add(view);
                    if (surfaces.TryGetValue(view, out var previous) && (!previous.Region.IsCurrent || previous.Layer != layers[index]))
                    {
                        surfaces.Remove(view);
                        previous.Region.Dispose();
                    }
                    if (!surfaces.TryGetValue(view, out var surface))
                    {
                        surface = new Surface(view, layers[index], new ThemeNativeWindowRegion(view));
                        surfaces.Add(view, surface);
                    }
                    UpdateSurface(surface, layers.AsSpan(index + 1));
                }
            }
            foreach (var stale in surfaces.Values.Where(item => !encountered.Contains(item.View)).ToArray()) Release(stale);
        }
        finally { updating = false; }
    }

    private void UpdateSurface(Surface surface, ReadOnlySpan<FrameworkElement> above)
    {
        var window = surface.Region.WindowBounds();
        if (window.IsEmpty) return;
        var visible = ScreenBounds(surface.View);
        visible.Intersect(window);
        for (DependencyObject? current = surface.View; current is Visual; current = VisualTreeHelper.GetParent(current))
        {
            if (current is FrameworkElement { ClipToBounds: true } clipped) visible.Intersect(ScreenBounds(clipped));
            if (current is UIElement { Clip: { } geometry } clippedElement)
            {
                var screenClip = ScreenBounds(clippedElement, geometry.Bounds);
                visible.Intersect(screenClip);
            }
            if (ReferenceEquals(current, workspace)) break;
        }
        visible.Intersect(ScreenBounds(workspace));
        var exclusions = new List<Rect>();
        foreach (var layer in above)
        {
            foreach (var rectangle in Occlusion(layer))
            {
                var overlap = Rect.Intersect(window, rectangle);
                if (!overlap.IsEmpty) exclusions.Add(Relative(overlap, window.Location));
            }
        }
        surface.Region.Update(Relative(visible, window.Location), exclusions);
    }

    private static IEnumerable<Rect> Occlusion(FrameworkElement layer)
    {
        if ((bool)layer.GetValue(OccludesInteriorProperty)) { yield return ScreenBounds(layer); yield break; }
        if (layer is not Border border) yield break;
        var size = border.RenderSize;
        var thickness = border.BorderThickness;
        if (thickness.Top > 0) yield return ScreenBounds(layer, new Rect(0, 0, size.Width, thickness.Top));
        if (thickness.Bottom > 0) yield return ScreenBounds(layer, new Rect(0, Math.Max(0, size.Height - thickness.Bottom), size.Width, thickness.Bottom));
        if (thickness.Left > 0) yield return ScreenBounds(layer, new Rect(0, 0, thickness.Left, size.Height));
        if (thickness.Right > 0) yield return ScreenBounds(layer, new Rect(Math.Max(0, size.Width - thickness.Right), 0, thickness.Right, size.Height));
    }

    private static Rect Relative(Rect rectangle, Point origin)
    {
        if (rectangle.IsEmpty) return Rect.Empty;
        rectangle.Offset(-origin.X, -origin.Y);
        return rectangle;
    }

    private static Rect ScreenBounds(FrameworkElement element) => ScreenBounds(element, new Rect(element.RenderSize));

    private static Rect ScreenBounds(Visual element, Rect rectangle)
    {
        if (rectangle.IsEmpty || PresentationSource.FromVisual(element) is null) return Rect.Empty;
        var points = new[] { rectangle.TopLeft, rectangle.TopRight, rectangle.BottomLeft, rectangle.BottomRight }
            .Select(element.PointToScreen).ToArray();
        return new Rect(new Point(points.Min(point => point.X), points.Min(point => point.Y)),
            new Point(points.Max(point => point.X), points.Max(point => point.Y)));
    }

    private static IEnumerable<HwndHost> NativeSurfaces(DependencyObject root)
    {
        if (root is HwndHost host) { yield return host; yield break; }
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
            foreach (var descendant in NativeSurfaces(VisualTreeHelper.GetChild(root, index))) yield return descendant;
    }

    private void FilterNativeMessage(ref MSG message, ref bool handled)
    {
        if (disposed || handled || message.message is not (WindowsInputMessages.LeftButtonDown or WindowsInputMessages.RightButtonDown
            or WindowsInputMessages.MiddleButtonDown or WindowsInputMessages.XButtonDown)) return;
        foreach (var surface in surfaces.Values.ToArray())
        {
            if (!surface.Region.IsCurrent || !surface.View.IsVisible) continue;
            if (surface.Region.Handle != message.hwnd && !IsChild(surface.Region.Handle, message.hwnd)) continue;
            NativePointerPressed?.Invoke(surface.Layer);
            Update();
            break; // Observation only: leave handled and the original native message untouched.
        }
    }

    private void Release(Surface surface)
    {
        surfaces.Remove(surface.View);
        surface.Region.Dispose();
    }

    internal void ReleaseLayer(FrameworkElement layer)
    {
        workspace.VerifyAccess();
        ReleaseAll(surfaces.Values.Where(surface => ReferenceEquals(surface.Layer, layer)).ToArray());
    }

    private void WorkspaceUnloaded(object sender, RoutedEventArgs args)
    {
        if (disposed) return;
        try { CancelPendingUpdate(); }
        finally { ReleaseAll(); }
    }

    private void CancelPendingUpdate()
    {
        var pending = pendingUpdate;
        pendingUpdate = null;
        pending?.Abort();
    }

    private void ReleaseAll() => ReleaseAll(surfaces.Values.ToArray());

    private void ReleaseAll(IEnumerable<Surface> selected)
    {
        Exception? failure = null;
        foreach (var surface in selected)
            try { Release(surface); }
            catch (Exception error) { failure ??= error; }
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }

    public void Dispose()
    {
        if (disposed) return;
        workspace.VerifyAccess();
        disposed = true;
        try
        {
            workspace.LayoutUpdated -= LayoutUpdated;
            workspace.Unloaded -= WorkspaceUnloaded;
            ComponentDispatcher.ThreadFilterMessage -= FilterNativeMessage;
            try { CancelPendingUpdate(); }
            finally { ReleaseAll(); }
        }
        finally { NativePointerPressed = null; }
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsChild(nint parent, nint child);
}
