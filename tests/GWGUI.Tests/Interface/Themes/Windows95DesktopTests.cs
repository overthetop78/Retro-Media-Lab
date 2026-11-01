using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using GWGUI.App.Theme.OS.Editions.Windows95;
using GWGUI.App.Theme.OS.Editions.Windows95.Views;
using GWGUI.Tests.Application.TestInfrastructure;

namespace GWGUI.Tests.Interface.Themes;

[Collection("WPF")]
public sealed class Windows95DesktopTests(StaExecutionScenarios sta)
{
    [Fact]
    public Task LaunchersUseExistingActionsAndDetachAfterDisposal() => sta.Run(() =>
    {
        var calls = 0;
        var definition = new Windows95LauncherDefinition("read", Windows95ProgramGroup.Media, "Read", null, () => calls++);
        using var desktop = new Windows95Desktop([definition]);
        var layout = Assert.IsType<DockPanel>(desktop.Content);
        var iconArea = Assert.IsType<Border>(layout.Children[1]);
        var iconPanel = Assert.IsType<WrapPanel>(Assert.IsType<ScrollViewer>(iconArea.Child).Content);
        var icon = Assert.IsType<Button>(Assert.Single(iconPanel.Children.Cast<object>()));
        Assert.Equal(definition.Id, icon.Tag);
        icon.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, new StubPresentationSource(), 0, Key.Enter)
        {
            RoutedEvent = Keyboard.KeyDownEvent
        });
        Assert.Equal(1, calls);
        var taskbar = Assert.IsType<DockPanel>(layout.Children[0]);
        var start = Assert.IsType<ToggleButton>(taskbar.Children[0]);
        var menu = Assert.IsType<ContextMenu>(start.ContextMenu);
        Assert.True(menu.ApplyTemplate());
        var programs = Assert.IsType<MenuItem>(Assert.Single(menu.Items.Cast<object>()));
        Assert.True(programs.ApplyTemplate());
        var group = Assert.IsType<MenuItem>(Assert.Single(programs.Items.Cast<object>()));
        var item = Assert.IsType<MenuItem>(Assert.Single(group.Items.Cast<object>()));
        Assert.True(item.ApplyTemplate());
        item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Assert.Equal(2, calls);
        desktop.Dispose();
        icon.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, new StubPresentationSource(), 0, Key.Enter)
        {
            RoutedEvent = Keyboard.KeyDownEvent
        });
        item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Assert.Equal(2, calls);
        Assert.Null(desktop.Content);
        Assert.Empty(iconPanel.Children);
    });

    [Fact]
    public Task DesktopDoesNotReplaceSuppliedMachineTitlesOrIcons() => sta.Run(() =>
    {
        var definition = new Windows95LauncherDefinition("configuration-id", Windows95ProgramGroup.Emulation, "My BBC", null, () => { });
        using var desktop = new Windows95Desktop([definition]);
        var layout = Assert.IsType<DockPanel>(desktop.Content);
        var icons = Assert.IsType<WrapPanel>(Assert.IsType<ScrollViewer>(Assert.IsType<Border>(layout.Children[1]).Child).Content);
        var button = Assert.IsType<Button>(Assert.Single(icons.Children.Cast<object>()));
        Assert.Equal("My BBC", button.ToolTip);
        var caption = Assert.IsType<TextBlock>(Assert.Single(Assert.IsType<StackPanel>(button.Content).Children.Cast<object>()));
        Assert.Equal(definition.Title, caption.Text);
    });

    [Fact]
    public Task ReplacingLaunchersDetachesOldActionsAndKeepsScrollableAccess()
        => sta.Run(() =>
    {
        var called = 0;
        using var desktop = new Windows95Desktop([new("old", Windows95ProgramGroup.Media, "Old", null, () => called++)]);
        var layout = Assert.IsType<DockPanel>(desktop.Content);
        var scrolling = Assert.IsType<ScrollViewer>(Assert.IsType<Border>(layout.Children[1]).Child);
        var icons = Assert.IsType<WrapPanel>(scrolling.Content);
        var old = Assert.IsType<Button>(icons.Children[0]);
        desktop.SetLaunchers(Enumerable.Range(0, 100).Select(index => new Windows95LauncherDefinition(
            index.ToString(), Windows95ProgramGroup.Emulation, index.ToString(), null, () => called++)));
        old.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, new StubPresentationSource(), 0, Key.Enter)
        {
            RoutedEvent = Keyboard.KeyDownEvent
        });
        Assert.Equal(0, called);
        Assert.Equal(100, icons.Children.Count);
        Assert.Equal(ScrollBarVisibility.Auto, scrolling.HorizontalScrollBarVisibility);
        desktop.Measure(new Size(640, 400));
        desktop.Arrange(new Rect(0, 0, 640, 400));
        desktop.UpdateLayout();
        Assert.True(scrolling.ExtentWidth > scrolling.ViewportWidth);
    });

    private sealed class StubPresentationSource : PresentationSource
    {
        public override System.Windows.Media.Visual RootVisual { get; set; } = new System.Windows.Media.DrawingVisual();
        public override bool IsDisposed => false;
        protected override System.Windows.Media.CompositionTarget? GetCompositionTargetCore() => null;
    }
}
