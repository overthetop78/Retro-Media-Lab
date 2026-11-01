using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RetroMediaLab.App.Theme.OS.Editions.Windows31;

internal partial class Windows31Frame
{
    private const Windows31ResizeEdges TopLeft = Windows31ResizeEdges.Left | Windows31ResizeEdges.Top;
    private const Windows31ResizeEdges TopRight = Windows31ResizeEdges.Right | Windows31ResizeEdges.Top;
    private const Windows31ResizeEdges BottomLeft = Windows31ResizeEdges.Left | Windows31ResizeEdges.Bottom;
    private const Windows31ResizeEdges BottomRight = Windows31ResizeEdges.Right | Windows31ResizeEdges.Bottom;
    private Windows31SystemMenu? systemMenu;
    private Windows31DragOutline? outline;
    private Windows31ResizeEdges movingEdges;
    private Point pointerOrigin;
    private Vector keyboardDelta;
    private bool pointerPlacement;
    private IInputElement? previousPlacementFocus;

    private void SystemClick(object sender, RoutedEventArgs args)
    {
        if (systemMenu is null) ShowSystemMenu();
        else CloseSystemMenu();
        args.Handled = true;
    }

    internal void ShowSystemMenu()
    {
        if (disposed || !placement.IsVisible || Parent is not Canvas desktop) return;
        EndPlacement(false);
        CloseSystemMenu();
        placement.Activate();
        systemMenu = new Windows31SystemMenu(desktop,
            SystemBox.TranslatePoint(new Point(0, SystemBox.ActualHeight), desktop),
            CanExecuteSystemCommand, ExecuteSystemCommand, CloseSystemMenu);
    }

    private bool CanExecuteSystemCommand(Windows31SystemCommand command) => !disposed && (command switch
    {
        Windows31SystemCommand.Restore => !isDialog && placement.State == Windows31WindowState.Maximized,
        Windows31SystemCommand.Move => placement.State == Windows31WindowState.Normal,
        Windows31SystemCommand.Size => AllowResize && placement.State == Windows31WindowState.Normal,
        Windows31SystemCommand.Minimize => !isDialog && placement.IsVisible,
        Windows31SystemCommand.Maximize => !isDialog && placement.State == Windows31WindowState.Normal,
        Windows31SystemCommand.Close => AllowClose && placement.IsVisible,
        _ => false
    });

    private void ExecuteSystemCommand(Windows31SystemCommand command)
    {
        if (!CanExecuteSystemCommand(command)) return;
        switch (command)
        {
            case Windows31SystemCommand.Restore: placement.Restore(); break;
            case Windows31SystemCommand.Move: BeginPlacement(Windows31ResizeEdges.None); break;
            case Windows31SystemCommand.Size: BeginPlacement(Windows31ResizeEdges.Right | Windows31ResizeEdges.Bottom); break;
            case Windows31SystemCommand.Minimize: placement.Iconify(); break;
            case Windows31SystemCommand.Maximize: placement.Maximize(); break;
            case Windows31SystemCommand.Close: RequestClose(); break;
        }
    }

    private void CloseSystemMenu()
    {
        var menu = systemMenu;
        systemMenu = null;
        menu?.Dispose();
    }

    internal bool BeginPlacement(Windows31ResizeEdges edges)
    {
        if (disposed || placement.State != Windows31WindowState.Normal || Parent is not Canvas desktop
            || edges != Windows31ResizeEdges.None && !AllowResize) return false;
        EndPlacement(false);
        CloseSystemMenu();
        previousPlacementFocus = Keyboard.FocusedElement;
        try
        {
            movingEdges = edges;
            keyboardDelta = default;
            pointerPlacement = false;
            outline = new Windows31DragOutline(desktop, placement.Bounds);
            Focus();
            return true;
        }
        catch { EndPlacement(false); throw; }
    }

    private bool BeginPointerPlacement(Windows31ResizeEdges edges, Point origin)
    {
        if (!BeginPlacement(edges)) return false;
        try
        {
            pointerPlacement = true;
            pointerOrigin = origin;
            if (CaptureMouse()) return true;
            EndPlacement(false);
            return false;
        }
        catch { EndPlacement(false); throw; }
    }

    internal void PreviewPlacement(Vector delta)
    {
        if (outline is null) return;
        outline.Update(movingEdges == Windows31ResizeEdges.None ? placement.PreviewMove(delta) : placement.PreviewResize(delta, movingEdges));
    }

    internal void EndPlacement(bool commit)
    {
        var current = outline;
        if (current is null) return;
        outline = null;
        var focus = previousPlacementFocus;
        previousPlacementFocus = null;
        try
        {
            if (pointerPlacement && IsMouseCaptured) ReleaseMouseCapture();
        }
        finally
        {
            pointerPlacement = false;
            try { current.Dispose(); }
            finally
            {
                if (commit) placement.ApplyBounds(current.Bounds);
                if (focus is UIElement { IsVisible: true, IsEnabled: true } element) element.Focus();
            }
        }
    }

    private bool HandlePlacementKey(KeyEventArgs args)
    {
        if (outline is null) return false;
        if (args.Key == Key.Escape) EndPlacement(false);
        else if (args.Key == Key.Enter) EndPlacement(true);
        else if (args.Key is Key.Left or Key.Right or Key.Up or Key.Down)
        {
            var step = Keyboard.Modifiers.HasFlag(ModifierKeys.Control) ? 1 : Windows31Metrics.KeyboardPlacementStep;
            keyboardDelta += args.Key switch
            {
                Key.Left => new Vector(-step, 0),
                Key.Right => new Vector(step, 0),
                Key.Up => new Vector(0, -step),
                _ => new Vector(0, step)
            };
            PreviewPlacement(keyboardDelta);
        }
        else return false;
        args.Handled = true;
        return true;
    }

    private void FrameMouseMove(object sender, MouseEventArgs args)
    {
        if (outline is not null && Parent is Canvas desktop)
        {
            if (pointerPlacement) PreviewPlacement(args.GetPosition(desktop) - pointerOrigin);
            return;
        }
        Cursor = ResizeCursor(ResizeEdgesAt(args.GetPosition(this)));
    }

    private void FrameMouseUp(object sender, MouseButtonEventArgs args)
    {
        if (outline is null || !pointerPlacement) return;
        try
        {
            if (Parent is Canvas desktop) PreviewPlacement(args.GetPosition(desktop) - pointerOrigin);
            EndPlacement(true);
            args.Handled = true;
        }
        catch { EndPlacement(false); throw; }
    }

    private void FrameLostCapture(object sender, MouseEventArgs args)
    {
        if (pointerPlacement && !IsMouseCaptured) EndPlacement(false);
    }

    private void FrameMouseLeave(object sender, MouseEventArgs args)
    {
        if (outline is null) ClearValue(CursorProperty);
    }

    private Windows31ResizeEdges ResizeEdgesAt(Point location)
    {
        if (!AllowResize || placement.State != Windows31WindowState.Normal) return Windows31ResizeEdges.None;
        var border = Windows31Metrics.ResizeBorder;
        var corner = Windows31Metrics.ResizeCorner;
        var edges = Windows31ResizeEdges.None;
        if (location.X < border) edges |= Windows31ResizeEdges.Left;
        else if (location.X >= ActualWidth - border) edges |= Windows31ResizeEdges.Right;
        if (location.Y < border) edges |= Windows31ResizeEdges.Top;
        else if (location.Y >= ActualHeight - border) edges |= Windows31ResizeEdges.Bottom;
        if (edges != Windows31ResizeEdges.None)
        {
            if (location.X < corner) edges |= Windows31ResizeEdges.Left;
            else if (location.X >= ActualWidth - corner) edges |= Windows31ResizeEdges.Right;
            if (location.Y < corner) edges |= Windows31ResizeEdges.Top;
            else if (location.Y >= ActualHeight - corner) edges |= Windows31ResizeEdges.Bottom;
        }
        return edges;
    }

    private static Cursor? ResizeCursor(Windows31ResizeEdges edges) => edges switch
    {
        Windows31ResizeEdges.Left or Windows31ResizeEdges.Right => Cursors.SizeWE,
        Windows31ResizeEdges.Top or Windows31ResizeEdges.Bottom => Cursors.SizeNS,
        TopLeft or BottomRight => Cursors.SizeNWSE,
        TopRight or BottomLeft => Cursors.SizeNESW,
        _ => null
    };
}
