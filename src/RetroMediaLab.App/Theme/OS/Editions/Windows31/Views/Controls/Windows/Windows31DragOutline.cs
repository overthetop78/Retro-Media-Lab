using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RetroMediaLab.App.Theme.Common;

namespace RetroMediaLab.App.Theme.OS.Editions.Windows31;

/// <summary>Owns only the preview contour; the desktop and document remain borrowed.</summary>
internal sealed class Windows31DragOutline : IDisposable
{
    private readonly Canvas desktop;
    private readonly Border contour = new();
    private bool disposed;

    internal Windows31DragOutline(Canvas desktop, Rect bounds)
    {
        this.desktop = desktop;
        try
        {
            contour.IsHitTestVisible = false;
            ThemeNativeWorkspaceHost.SetOccludesInterior(contour, false);
            contour.BorderBrush = Pattern();
            contour.BorderThickness = new Thickness(Windows31Metrics.OutlineThickness);
            contour.UseLayoutRounding = true;
            contour.SnapsToDevicePixels = true;
            Panel.SetZIndex(contour, int.MaxValue);
            Update(bounds);
            desktop.Children.Add(contour);
        }
        catch { Dispose(); throw; }
    }

    internal Rect Bounds { get; private set; }

    internal void Update(Rect bounds)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        Bounds = bounds;
        Canvas.SetLeft(contour, bounds.Left);
        Canvas.SetTop(contour, bounds.Top);
        contour.Width = bounds.Width;
        contour.Height = bounds.Height;
    }

    private static Brush Pattern()
    {
        var size = Windows31Metrics.OutlinePatternSize;
        var half = size / 2;
        var drawing = new DrawingGroup();
        drawing.Children.Add(new GeometryDrawing(Windows31Metrics.HighlightBrush, null, new RectangleGeometry(new Rect(0, 0, size, size))));
        drawing.Children.Add(new GeometryDrawing(Windows31Metrics.EdgeBrush, null, new RectangleGeometry(new Rect(0, 0, half, half))));
        drawing.Children.Add(new GeometryDrawing(Windows31Metrics.EdgeBrush, null, new RectangleGeometry(new Rect(half, half, half, half))));
        var brush = new DrawingBrush(drawing)
        {
            Viewport = new Rect(0, 0, size, size),
            ViewportUnits = BrushMappingMode.Absolute,
            TileMode = TileMode.Tile,
            Stretch = Stretch.None
        };
        brush.Freeze();
        return brush;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { contour.BorderBrush = null; }
        finally { desktop.Children.Remove(contour); }
    }
}
