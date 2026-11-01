using System.Windows;
using RetroMediaLab.App.ViewModels;

namespace RetroMediaLab.App.Theme.OS.Editions.Windows31;

/// <summary>Placement of a Windows 3.1 application, including desktop-icon reduction.</summary>
internal sealed class Windows31WindowPlacement : ObservableObject
{
    private Rect desktop;
    private Rect normalBounds;
    private readonly Size minimum;
    private Rect bounds;
    private Windows31WindowState state;
    private Windows31WindowState beforeIcon;
    private bool active;

    internal Windows31WindowPlacement(Rect desktop, Rect initialBounds, Size minimum)
    {
        Validate(desktop, nameof(desktop));
        Validate(initialBounds, nameof(initialBounds));
        if (minimum.IsEmpty || !double.IsFinite(minimum.Width) || !double.IsFinite(minimum.Height)
            || minimum.Width <= 0 || minimum.Height <= 0) throw new ArgumentOutOfRangeException(nameof(minimum));
        this.desktop = desktop;
        this.minimum = minimum;
        bounds = normalBounds = Constrain(initialBounds);
    }

    public Rect Bounds => bounds;
    public Windows31WindowState State => state;
    public bool IsActive => active;
    public bool IsVisible => state is Windows31WindowState.Normal or Windows31WindowState.Maximized;

    internal void Activate()
    {
        if (state == Windows31WindowState.Closed) return;
        if (state == Windows31WindowState.Iconified) Restore();
        Set(ref active, true, nameof(IsActive));
    }

    internal void Deactivate() => Set(ref active, false, nameof(IsActive));

    internal void Iconify()
    {
        if (!IsVisible) return;
        beforeIcon = state;
        ChangeState(Windows31WindowState.Iconified);
        Deactivate();
    }

    internal void Maximize()
    {
        if (state is Windows31WindowState.Closed or Windows31WindowState.Iconified) return;
        if (state == Windows31WindowState.Normal) normalBounds = bounds;
        ChangeState(Windows31WindowState.Maximized);
        SetBounds(desktop);
    }

    internal void Restore()
    {
        if (state is Windows31WindowState.Normal or Windows31WindowState.Closed) return;
        var next = state == Windows31WindowState.Iconified ? beforeIcon : Windows31WindowState.Normal;
        ChangeState(next);
        SetBounds(next == Windows31WindowState.Maximized ? desktop : normalBounds);
    }

    internal void MoveTo(Point location)
    {
        if (state != Windows31WindowState.Normal) return;
        if (!double.IsFinite(location.X) || !double.IsFinite(location.Y)) throw new ArgumentOutOfRangeException(nameof(location));
        SetNormalBounds(Constrain(new Rect(location, bounds.Size)));
    }

    internal void ResizeTo(Size size)
    {
        if (state != Windows31WindowState.Normal) return;
        if (size.IsEmpty || !double.IsFinite(size.Width) || !double.IsFinite(size.Height))
            throw new ArgumentOutOfRangeException(nameof(size));
        SetNormalBounds(Constrain(new Rect(bounds.Location, size)));
    }

    internal Rect PreviewMove(Vector delta)
    {
        ValidateDelta(delta);
        return state == Windows31WindowState.Normal
            ? Constrain(new Rect(bounds.Location + delta, bounds.Size)) : bounds;
    }

    internal Rect PreviewResize(Vector delta, Windows31ResizeEdges edges)
    {
        ValidateDelta(delta);
        if (state != Windows31WindowState.Normal) return bounds;
        var width = Math.Min(minimum.Width, desktop.Width);
        var height = Math.Min(minimum.Height, desktop.Height);
        var left = bounds.Left;
        var top = bounds.Top;
        var right = bounds.Right;
        var bottom = bounds.Bottom;
        if (edges.HasFlag(Windows31ResizeEdges.Left)) left = Math.Clamp(left + delta.X, desktop.Left, right - width);
        if (edges.HasFlag(Windows31ResizeEdges.Right)) right = Math.Clamp(right + delta.X, left + width, desktop.Right);
        if (edges.HasFlag(Windows31ResizeEdges.Top)) top = Math.Clamp(top + delta.Y, desktop.Top, bottom - height);
        if (edges.HasFlag(Windows31ResizeEdges.Bottom)) bottom = Math.Clamp(bottom + delta.Y, top + height, desktop.Bottom);
        return new Rect(new Point(left, top), new Point(right, bottom));
    }

    internal void ApplyBounds(Rect value)
    {
        Validate(value, nameof(value));
        if (state == Windows31WindowState.Normal) SetNormalBounds(Constrain(value));
    }

    private static void ValidateDelta(Vector delta)
    {
        if (!double.IsFinite(delta.X) || !double.IsFinite(delta.Y)) throw new ArgumentOutOfRangeException(nameof(delta));
    }

    internal void SetDesktop(Rect area)
    {
        Validate(area, nameof(area));
        if (state == Windows31WindowState.Closed) return;
        desktop = area;
        normalBounds = Constrain(normalBounds);
        if (IsVisible) SetBounds(state == Windows31WindowState.Maximized ? desktop : normalBounds);
    }

    internal void Close()
    {
        if (state == Windows31WindowState.Closed) return;
        ChangeState(Windows31WindowState.Closed);
        Deactivate();
    }

    private void ChangeState(Windows31WindowState next)
    {
        if (state == next) return;
        Set(ref state, next, nameof(State));
        OnPropertyChanged(nameof(IsVisible));
    }

    private void SetNormalBounds(Rect value)
    {
        normalBounds = value;
        SetBounds(value);
    }

    private void SetBounds(Rect value) => Set(ref bounds, value, nameof(Bounds));

    private Rect Constrain(Rect value)
    {
        var width = Math.Clamp(value.Width, Math.Min(minimum.Width, desktop.Width), desktop.Width);
        var height = Math.Clamp(value.Height, Math.Min(minimum.Height, desktop.Height), desktop.Height);
        var left = Math.Clamp(value.Left, desktop.Left, desktop.Right - width);
        var top = Math.Clamp(value.Top, desktop.Top, desktop.Bottom - height);
        return new Rect(left, top, width, height);
    }

    private static void Validate(Rect value, string name)
    {
        if (value.IsEmpty || !double.IsFinite(value.X) || !double.IsFinite(value.Y)
            || !double.IsFinite(value.Width) || !double.IsFinite(value.Height)
            || value.Width <= 0 || value.Height <= 0) throw new ArgumentOutOfRangeException(name);
    }
}
