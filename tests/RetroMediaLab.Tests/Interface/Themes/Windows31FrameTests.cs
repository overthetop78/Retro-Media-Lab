using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using RetroMediaLab.App.Theme.OS.Editions.Windows31;
using RetroMediaLab.Tests.Application.TestInfrastructure;

namespace RetroMediaLab.Tests.Interface.Themes;

[Collection("WPF")]
public sealed class Windows31FrameTests(StaExecutionScenarios sta)
{
    private static Windows31WindowPlacement Placement() => new(new Rect(0, 0, 1024, 768), new Rect(40, 60, 500, 350), new Size(180, 120));

    [Fact]
    public Task CaptionCommandsUseDesktopIconReductionAndRestoreOriginalBounds() => sta.Run(() =>
    {
        var placement = Placement();
        using var frame = new Windows31Frame(placement, new TextBox());
        frame.Title = "capture";
        Assert.Equal("capture", frame.Title);
        var maximize = Assert.IsType<Button>(frame.FindName("MaximizeCommand"));
        maximize.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        Assert.Equal(Windows31WindowState.Maximized, placement.State);
        Assert.Equal(1024, frame.Width);
        var iconify = Assert.IsType<Button>(frame.FindName("IconifyCommand"));
        iconify.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        Assert.Equal(Windows31WindowState.Iconified, placement.State);
        Assert.Equal(Visibility.Collapsed, frame.Visibility);
        placement.Activate();
        Assert.Equal(Visibility.Visible, frame.Visibility);
        Assert.Equal(Windows31WindowState.Maximized, placement.State);
        maximize.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        Assert.Equal(500, frame.Width);
        Assert.Equal(350, frame.Height);
    });

    [Fact]
    public Task ActivationChangesTheCaptionAndClosingHidesTheFrame() => sta.Run(() =>
    {
        var placement = Placement();
        using var frame = new Windows31Frame(placement, new Grid());
        var caption = Assert.IsType<Grid>(frame.FindName("Caption"));
        Assert.Same(Windows31Metrics.InactiveCaptionBrush, caption.Background);
        placement.Activate();
        Assert.Same(Windows31Metrics.ActiveCaptionBrush, caption.Background);
        placement.Deactivate();
        Assert.Same(Windows31Metrics.InactiveCaptionBrush, caption.Background);
        placement.Close();
        Assert.Equal(Visibility.Collapsed, frame.Visibility);
    });

    [Fact]
    public Task DisposingDetachesBorrowedDocumentAndEveryCommand() => sta.Run(() =>
    {
        var placement = Placement();
        var document = new TextBox { Text = "kept" };
        var frame = new Windows31Frame(placement, document);
        Button? command = null;
        try { command = Assert.IsType<Button>(frame.FindName("IconifyCommand")); }
        finally { frame.Dispose(); }
        Assert.Null(document.Parent);
        Assert.Null(frame.Content);
        command!.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        Assert.Equal(Windows31WindowState.Normal, placement.State);
        placement.Maximize();
        Assert.Equal(500, frame.Width);
        Assert.Equal("kept", document.Text);
        frame.Dispose();
    });

    [Fact]
    public Task PlacementUsesAnOwnedOutlineAndCommitsOnlyWhenAccepted() => sta.Run(() =>
    {
        var placement = Placement();
        var initial = placement.Bounds;
        var desktop = new Canvas { Width = 1024, Height = 768 };
        using var frame = new Windows31Frame(placement, new Grid());
        try
        {
            desktop.Children.Add(frame);
            Assert.True(frame.BeginPlacement(Windows31ResizeEdges.None));
            Assert.Equal(2, desktop.Children.Count);
            frame.PreviewPlacement(new Vector(20, 30));
            Assert.Equal(initial, placement.Bounds);
            frame.EndPlacement(false);
            Assert.Single(desktop.Children);
            Assert.Equal(initial, placement.Bounds);
            Assert.True(frame.BeginPlacement(Windows31ResizeEdges.None));
            frame.PreviewPlacement(new Vector(20, 30));
            frame.EndPlacement(true);
            Assert.Equal(new Rect(60, 90, 500, 350), placement.Bounds);
            Assert.Single(desktop.Children);
            Assert.True(frame.BeginPlacement(Windows31ResizeEdges.Right | Windows31ResizeEdges.Bottom));
            frame.PreviewPlacement(new Vector(100, 50));
            frame.EndPlacement(true);
            Assert.Equal(new Rect(60, 90, 600, 400), placement.Bounds);
        }
        finally { desktop.Children.Clear(); }
    });

    [Fact]
    public Task SystemCommandsFollowWindowStateAndDisposalRemovesOwnedOverlays() => sta.Run(() =>
    {
        var placement = Placement();
        var desktop = new Canvas { Width = 1024, Height = 768 };
        var document = new Grid();
        var frame = new Windows31Frame(placement, document);
        try
        {
            desktop.Children.Add(frame);
            desktop.Measure(new Size(1024, 768));
            desktop.Arrange(new Rect(0, 0, 1024, 768));
            frame.ShowSystemMenu();
            var menu = Assert.IsType<Border>(desktop.Children[1]);
            var rows = Assert.IsType<StackPanel>(menu.Child);
            var commands = rows.Children.OfType<Button>().ToDictionary(item => (Windows31SystemCommand)item.Tag);
            Assert.False(commands[Windows31SystemCommand.Restore].IsEnabled);
            Assert.True(commands[Windows31SystemCommand.Move].IsEnabled);
            commands[Windows31SystemCommand.Maximize].RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            Assert.Single(desktop.Children);
            Assert.Equal(Windows31WindowState.Maximized, placement.State);
            frame.ShowSystemMenu();
            menu = Assert.IsType<Border>(desktop.Children[1]);
            rows = Assert.IsType<StackPanel>(menu.Child);
            commands = rows.Children.OfType<Button>().ToDictionary(item => (Windows31SystemCommand)item.Tag);
            Assert.True(commands[Windows31SystemCommand.Restore].IsEnabled);
            Assert.False(commands[Windows31SystemCommand.Move].IsEnabled);
            Assert.False(commands[Windows31SystemCommand.Size].IsEnabled);
            commands[Windows31SystemCommand.Restore].RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            Assert.Equal(Windows31WindowState.Normal, placement.State);
            frame.ShowSystemMenu();
            Assert.Equal(2, desktop.Children.Count);
            Assert.True(frame.BeginPlacement(Windows31ResizeEdges.None));
            Assert.Equal(2, desktop.Children.Count);
            frame.Dispose();
            Assert.Single(desktop.Children);
            Assert.Null(document.Parent);
        }
        finally
        {
            try { frame.Dispose(); }
            finally { desktop.Children.Clear(); }
        }
    });
}
