using System.Windows;
using RetroMediaLab.App.Theme.OS.Editions.Windows31;

namespace RetroMediaLab.Tests.Interface.Themes;

public sealed class Windows31WindowPlacementTests
{
    private static readonly Rect Desktop = new(0, 0, 1024, 768);
    private static readonly Rect Initial = new(40, 60, 500, 350);
    private static readonly Size Minimum = new(180, 120);

    [Fact]
    public void DesktopIconRestoresTheSameApplicationPlacementAndActivation()
    {
        var window = new Windows31WindowPlacement(Desktop, Initial, Minimum);
        window.Activate();
        window.Iconify();
        Assert.Equal(Windows31WindowState.Iconified, window.State);
        Assert.False(window.IsVisible);
        Assert.False(window.IsActive);
        window.Activate();
        Assert.Equal(Windows31WindowState.Normal, window.State);
        Assert.Equal(Initial, window.Bounds);
        Assert.True(window.IsActive);
        Assert.True(window.IsVisible);
    }

    [Fact]
    public void IconifiedMaximizedApplicationUsesUpdatedDesktopThenRestoresItsNormalBounds()
    {
        var window = new Windows31WindowPlacement(Desktop, Initial, Minimum);
        window.Maximize();
        window.Iconify();
        var smaller = new Rect(0, 0, 800, 600);
        window.SetDesktop(smaller);
        window.Activate();
        Assert.Equal(Windows31WindowState.Maximized, window.State);
        Assert.Equal(smaller, window.Bounds);
        window.Restore();
        Assert.Equal(Initial, window.Bounds);
    }

    [Fact]
    public void NormalBoundsStayReachableAndRespectAvailableDesktopAndMinimumSize()
    {
        var window = new Windows31WindowPlacement(Desktop, Initial, Minimum);
        window.MoveTo(new Point(10000, -100));
        Assert.Equal(Desktop.Right, window.Bounds.Right);
        Assert.Equal(Desktop.Top, window.Bounds.Top);
        window.ResizeTo(new Size(1, 1));
        Assert.Equal(Minimum, window.Bounds.Size);
        var tiny = new Rect(0, 0, 100, 80);
        window.SetDesktop(tiny);
        Assert.Equal(tiny, window.Bounds);
    }

    [Fact]
    public void ClosedApplicationCannotBeRestoredOrReactivatedAndStopsChangingBounds()
    {
        var window = new Windows31WindowPlacement(Desktop, Initial, Minimum);
        window.Activate();
        window.Maximize();
        window.Close();
        var closedBounds = window.Bounds;
        window.Activate();
        window.Restore();
        window.Maximize();
        window.Iconify();
        window.MoveTo(new Point(10, 10));
        window.ResizeTo(new Size(300, 300));
        window.SetDesktop(new Rect(0, 0, 640, 480));
        Assert.Equal(Windows31WindowState.Closed, window.State);
        Assert.Equal(closedBounds, window.Bounds);
        Assert.False(window.IsVisible);
        Assert.False(window.IsActive);
    }

    [Fact]
    public void InvalidCoordinatesCannotPoisonSubsequentLayout()
    {
        var window = new Windows31WindowPlacement(Desktop, Initial, Minimum);
        Assert.Throws<ArgumentOutOfRangeException>(() => window.MoveTo(new Point(double.NaN, 0)));
        Assert.Throws<ArgumentOutOfRangeException>(() => window.ResizeTo(new Size(double.PositiveInfinity, 100)));
        Assert.Throws<ArgumentOutOfRangeException>(() => window.SetDesktop(Rect.Empty));
        Assert.Equal(Initial, window.Bounds);
    }

    [Fact]
    public void ResizePreviewKeepsOppositeEdgesFixedAndDoesNotChangeLiveBounds()
    {
        var window = new Windows31WindowPlacement(Desktop, Initial, Minimum);
        var smaller = window.PreviewResize(new Vector(10000, 10000), Windows31ResizeEdges.Left | Windows31ResizeEdges.Top);
        Assert.Equal(Initial.Right, smaller.Right);
        Assert.Equal(Initial.Bottom, smaller.Bottom);
        Assert.Equal(Minimum, smaller.Size);
        var larger = window.PreviewResize(new Vector(-10000, -10000), Windows31ResizeEdges.Left | Windows31ResizeEdges.Top);
        Assert.Equal(Desktop.Left, larger.Left);
        Assert.Equal(Desktop.Top, larger.Top);
        Assert.Equal(Initial.BottomRight, larger.BottomRight);
        Assert.Equal(Initial, window.Bounds);
        window.ApplyBounds(smaller);
        window.Maximize();
        window.Restore();
        Assert.Equal(smaller, window.Bounds);
    }

    [Fact]
    public void PlacementPreviewClampsToTheDesktopAndRejectsNonFiniteCoordinates()
    {
        var window = new Windows31WindowPlacement(Desktop, Initial, Minimum);
        var moved = window.PreviewMove(new Vector(10000, -10000));
        Assert.Equal(Desktop.Right, moved.Right);
        Assert.Equal(Desktop.Top, moved.Top);
        Assert.Equal(Initial.Size, moved.Size);
        Assert.Equal(Initial, window.Bounds);
        Assert.Throws<ArgumentOutOfRangeException>(() => window.PreviewMove(new Vector(double.NaN, 0)));
        Assert.Throws<ArgumentOutOfRangeException>(() => window.PreviewResize(new Vector(0, double.PositiveInfinity), Windows31ResizeEdges.Bottom));
        window.Maximize();
        Assert.Equal(Desktop, window.PreviewMove(new Vector(20, 20)));
        Assert.Equal(Desktop, window.PreviewResize(new Vector(-20, -20), Windows31ResizeEdges.Right));
    }
}
