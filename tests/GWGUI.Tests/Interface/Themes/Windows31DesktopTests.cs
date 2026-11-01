using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GWGUI.App.Theme.OS.Editions.Windows31;
using GWGUI.Tests.Application.TestInfrastructure;

namespace GWGUI.Tests.Interface.Themes;

[Collection("WPF")]
public sealed class Windows31DesktopTests(StaExecutionScenarios sta)
{

    private sealed class Lifetime(Action dispose) : IDisposable { public void Dispose() => dispose(); }

    [Fact]
    public Task ReturningToProgramManagerRestoresTheChosenGroupAndItsCaptionState() => sta.Run(() =>
    {
        using var desktop = new Windows31Desktop([
            Definition(Windows31ApplicationId.Read, () => new Windows31Document(new Grid())),
            new(Windows31ApplicationId.Emulation, Windows31ProgramGroup.Emulation, "Tab.Emulation",
                () => new Windows31Document(new Grid()), new Size(600, 400))]);
        Arrange(desktop);
        var manager = Descendants(desktop).OfType<Windows31ProgramManager>().Single();
        var groups = Assert.IsType<ListBox>(manager.FindName("GroupIcons"));
        groups.SelectedItem = groups.Items.Cast<ListBoxItem>().Single(item => Equals(item.Tag, Windows31ProgramGroup.Emulation));
        groups.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left) { RoutedEvent = Control.MouseDoubleClickEvent });
        Arrange(desktop);
        var selected = Descendants(manager).OfType<Windows31Frame>().Single(frame => frame.Title == GWGUI.App.Localization.Extensions.LocExtension.Get("Tab.Emulation"));
        var palette = Windows31PaletteCatalog.Get(Windows31ColorScheme.WindowsDefault);
        Assert.Equal(palette[Windows31ColorRole.ActiveCaption], Assert.IsType<Grid>(selected.FindName("Caption")).Background);
        desktop.Open(Windows31ApplicationId.Read);
        Assert.All(Descendants(manager).OfType<Windows31Frame>(), frame =>
            Assert.Equal(palette[Windows31ColorRole.InactiveCaption], Assert.IsType<Grid>(frame.FindName("Caption")).Background));
        desktop.Open(Windows31ApplicationId.ProgramManager);
        Assert.Equal(palette[Windows31ColorRole.ActiveCaption], Assert.IsType<Grid>(selected.FindName("Caption")).Background);
        Assert.All(Descendants(manager).OfType<Windows31Frame>().Where(frame => !ReferenceEquals(frame, selected)), frame =>
            Assert.Equal(palette[Windows31ColorRole.InactiveCaption], Assert.IsType<Grid>(frame.FindName("Caption")).Background));
    });

    [Fact]
    public Task InternalWindowsReuseInstancesRestoreIconsAndReleaseOnlyTransferredLifetimes() => sta.Run(() =>
    {
        var created = 0;
        var released = 0;
        var borrowed = new TextBox { Text = "borrowed" };
        var definitions = new[]
        {
            Definition(Windows31ApplicationId.Read, () => { created++; return new Windows31Document(new TextBox(), new Lifetime(() => released++)); }),
            Definition(Windows31ApplicationId.Write, () => new Windows31Document(borrowed))
        };
        using var desktop = new Windows31Desktop(definitions);
        Arrange(desktop);
        var read = desktop.Open(Windows31ApplicationId.Read);
        Assert.Same(read, desktop.Open(Windows31ApplicationId.Read));
        Assert.Equal(1, created);
        read.Iconify();
        Assert.False(read.IsVisible);
        desktop.Open(Windows31ApplicationId.Read);
        Assert.True(read.IsVisible);
        read.Maximize();
        Assert.Equal(new Rect(0, 0, 1280, 800), read.Bounds);
        Assert.Single(desktop.OpenApplications);
        desktop.Open(Windows31ApplicationId.Write);
        read.Close();
        Assert.Equal(1, released);
        Assert.Single(desktop.OpenApplications);
        desktop.Dispose();
        Assert.Null(borrowed.Parent);
        Assert.Equal("borrowed", borrowed.Text);
        Assert.Equal(1, released);
    });

    [Fact]
    public Task DoubleClickLauncherOpensAnInternalApplicationAndManagerCanReturnFromItsIcon() => sta.Run(() =>
    {
        using var desktop = new Windows31Desktop(new[] { Definition(Windows31ApplicationId.Read, () => new Windows31Document(new Grid())) });
        Arrange(desktop);
        var manager = Descendants(desktop).OfType<Windows31ProgramManager>().Single();
        var item = Descendants(manager).OfType<ListBoxItem>().First(entry => entry.Tag is Windows31ApplicationId.Read);
        var list = Assert.IsType<ListBox>(ItemsControl.ItemsControlFromItemContainer(item));
        list.SelectedItem = item;
        list.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left) { RoutedEvent = Control.MouseDoubleClickEvent });
        Assert.Single(desktop.OpenApplications);
        desktop.ProgramManagerPlacement.Iconify();
        desktop.Open(Windows31ApplicationId.ProgramManager);
        Assert.True(desktop.ProgramManagerPlacement.IsVisible);
        Assert.True(desktop.ProgramManagerPlacement.IsActive);
        var managerFrame = Descendants(desktop).OfType<Windows31Frame>().First(frame => frame.Title.Contains("program", StringComparison.OrdinalIgnoreCase));
        var requests = 0;
        desktop.CloseRequested += (_, _) => requests++;
        Assert.IsType<Button>(managerFrame.FindName("SystemBox")).RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left) { RoutedEvent = Control.MouseDoubleClickEvent });
        Assert.Equal(1, requests);
        Assert.NotEqual(Windows31WindowState.Closed, desktop.ProgramManagerPlacement.State);
    });

    [Fact]
    public Task FailureDuringDisposalStillReleasesEveryOtherPresentationAndVisual() => sta.Run(() =>
    {
        var released = 0;
        var desktop = new Windows31Desktop(new[]
        {
            Definition(Windows31ApplicationId.Read, () => new Windows31Document(new Grid(), new Lifetime(() => throw new InvalidOperationException("release failed")))),
            Definition(Windows31ApplicationId.Write, () => new Windows31Document(new Grid(), new Lifetime(() => released++)))
        });
        try
        {
            Arrange(desktop);
            desktop.Open(Windows31ApplicationId.Read);
            desktop.Open(Windows31ApplicationId.Write);
            Assert.Throws<InvalidOperationException>(desktop.Dispose);
            Assert.Equal(1, released);
            Assert.Empty(desktop.Children);
            Assert.Empty(desktop.OpenApplications);
        }
        finally { desktop.Dispose(); }
    });

    private static Windows31ApplicationDefinition Definition(Windows31ApplicationId id, Func<Windows31Document> create) =>
        new(id, Windows31ProgramGroup.Media, id switch
        {
            Windows31ApplicationId.Read => "Tab.Read", Windows31ApplicationId.Write => "Tab.Write",
            Windows31ApplicationId.Conversion => "Tab.Convert", Windows31ApplicationId.Visualization => "Tab.Visualize",
            Windows31ApplicationId.Explorer => "Tab.Explorer", Windows31ApplicationId.Tools => "Tab.Tools",
            Windows31ApplicationId.Emulation => "Tab.Emulation", Windows31ApplicationId.Preferences => "Menu.Preferences",
            _ => throw new ArgumentOutOfRangeException(nameof(id))
        }, create, new Size(600, 400));

    private static void Arrange(FrameworkElement element)
    {
        element.Measure(new Size(1280, 800));
        element.Arrange(new Rect(0, 0, 1280, 800));
        element.UpdateLayout();
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }
}
