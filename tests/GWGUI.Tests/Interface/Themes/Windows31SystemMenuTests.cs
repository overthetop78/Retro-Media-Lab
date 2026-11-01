using System.Windows;
using System.Windows.Controls;
using GWGUI.App.Theme.OS.Editions.Windows31;
using GWGUI.Tests.Application.TestInfrastructure;

namespace GWGUI.Tests.Interface.Themes;

[Collection("WPF")]
public sealed class Windows31SystemMenuTests(StaExecutionScenarios sta)
{
    [Fact]
    public Task MenuIsConstrainedInsideTheDesktopAndIgnoresUnavailableCommands() => sta.Run(() =>
    {
        var desktop = new Canvas();
        desktop.Measure(new Size(400, 300));
        desktop.Arrange(new Rect(0, 0, 400, 300));
        var invoked = new List<Windows31SystemCommand>();
        var dismissed = 0;
        using var menu = new Windows31SystemMenu(desktop, new Point(390, 290),
            command => command != Windows31SystemCommand.Restore, invoked.Add, () => dismissed++);
        Assert.Single(desktop.Children);
        Assert.True(Canvas.GetLeft(menu.Surface) + menu.Surface.DesiredSize.Width <= 400);
        Assert.True(Canvas.GetTop(menu.Surface) + menu.Surface.DesiredSize.Height <= 300);
        menu.Invoke(Windows31SystemCommand.Restore);
        Assert.Empty(invoked);
        Assert.Equal(0, dismissed);
        menu.Invoke(Windows31SystemCommand.Move);
        Assert.Equal([Windows31SystemCommand.Move], invoked);
        Assert.Equal(1, dismissed);
        Assert.Empty(desktop.Children);
        menu.Invoke(Windows31SystemCommand.Close);
        Assert.Single(invoked);
    });

    [Fact]
    public Task MenuDetachesItsSurfaceBeforeACommandThrows() => sta.Run(() =>
    {
        var desktop = new Canvas();
        using var menu = new Windows31SystemMenu(desktop, default, _ => true,
            _ => throw new InvalidOperationException("command"), () => { });
        Assert.Throws<InvalidOperationException>(() => menu.Invoke(Windows31SystemCommand.Close));
        Assert.Empty(desktop.Children);
        Assert.False(menu.CanExecute(Windows31SystemCommand.Close));
        desktop.Children.Add(new Grid());
        menu.Dispose();
        Assert.Single(desktop.Children);
        desktop.Children.Clear();
    });
}
