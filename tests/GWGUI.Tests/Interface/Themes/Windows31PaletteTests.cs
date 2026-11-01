using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GWGUI.App.Localization.Extensions;
using GWGUI.App.Theme.OS.Editions.Windows31;
using GWGUI.Tests.Application.TestInfrastructure;

namespace GWGUI.Tests.Interface.Themes;

[Collection("WPF")]
public sealed class Windows31PaletteTests(StaExecutionScenarios sta)
{
    [Fact]
    public Task ProgramIconsUseEveryPaletteWithoutLocalDefaultColorsMaskingTheChoice() => sta.Run(() =>
    {
        using var desktop = new Windows31Desktop([
            new(Windows31ApplicationId.Read, Windows31ProgramGroup.Media, "Tab.Read",
                () => new Windows31Document(new Grid()), new Size(600, 400))]);
        Arrange(desktop);
        var manager = Descendants(desktop).OfType<Windows31ProgramManager>().Single();
        var icon = Descendants(manager).OfType<ListBoxItem>().Single(item => Equals(item.Tag, Windows31ApplicationId.Read));
        var list = ItemsControl.ItemsControlFromItemContainer(icon);
        var caption = Assert.IsType<StackPanel>(icon.Content).Children.OfType<TextBlock>().Single();
        foreach (var palette in Windows31PaletteCatalog.All)
        {
            Windows31PaletteResources.Apply(desktop, palette.Id);
            Assert.Equal(palette.Id, Windows31PaletteResources.GetScheme(manager));
            icon.IsSelected = false;
            Assert.Equal(ColorOf(palette[Windows31ColorRole.WindowText]), ColorOf(icon.Foreground));
            icon.IsSelected = true;
            Assert.Equal(ColorOf(palette[Windows31ColorRole.Selection]), ColorOf(icon.Background));
            Assert.Equal(ColorOf(palette[Windows31ColorRole.SelectionText]), ColorOf(caption.Foreground));
            Assert.Same(list, ItemsControl.ItemsControlFromItemContainer(icon));
        }
    });

    [Fact]
    public Task EveryOriginalSchemeHasAllSystemColorsAndItsInvariantName() => sta.Run(() =>
    {
        Assert.Equal(Enum.GetValues<Windows31ColorScheme>().Length, Windows31PaletteCatalog.All.Count);
        Assert.Equal(23, Windows31PaletteCatalog.All.Count);
        foreach (var palette in Windows31PaletteCatalog.All)
        {
            Assert.Equal(Enum.GetValues<Windows31ColorRole>().Length, palette.ColorRefs.Count);
            Assert.NotEqual(palette.NameResourceKey, LocExtension.Get(palette.NameResourceKey));
            foreach (var role in Enum.GetValues<Windows31ColorRole>())
            {
                var brush = Assert.IsType<SolidColorBrush>(palette[role]);
                Assert.True(brush.IsFrozen);
                var raw = palette.ColorRefs[role];
                Assert.Equal(Color.FromRgb((byte)(raw & 0xff), (byte)((raw >> 8) & 0xff), (byte)((raw >> 16) & 0xff)), brush.Color);
            }
        }
        var hotdog = Windows31PaletteCatalog.Get(Windows31ColorScheme.HotdogStand);
        Assert.Equal(Colors.Yellow, ColorOf(hotdog[Windows31ColorRole.Desktop]));
        Assert.Equal(Colors.Red, ColorOf(hotdog[Windows31ColorRole.Window]));
        var leather = Windows31PaletteCatalog.Get(Windows31ColorScheme.BlackLeatherJacket);
        Assert.Equal(0x10E0E0E0u, leather.ColorRefs[Windows31ColorRole.ScrollBar]);
        Assert.Equal(Color.FromRgb(224, 224, 224), ColorOf(leather[Windows31ColorRole.ScrollBar]));
    });

    [Fact]
    public Task AllSchemesUpdateExistingDesktopWindowsControlsAndMenusWithoutReplacingDocuments() => sta.Run(() =>
    {
        var body = new StackPanel();
        var button = new Button { Content = "action" };
        var input = new TextBox { Text = "value" };
        var list = new ListBox { Height = 80 };
        list.Items.Add("item");
        list.SelectedIndex = 0;
        body.Children.Add(button);
        body.Children.Add(input);
        body.Children.Add(list);
        using var desktop = new Windows31Desktop([
            new Windows31ApplicationDefinition(Windows31ApplicationId.Write, Windows31ProgramGroup.Media,
                "Tab.Write", () => new Windows31Document(body), new Size(600, 400))]);
        try
        {
            var placement = desktop.Open(Windows31ApplicationId.Write);
            Arrange(desktop);
            var frame = Descendants(desktop).OfType<Windows31Frame>().Single(candidate => ReferenceEquals(
                Assert.IsType<ContentControl>(candidate.FindName("DocumentHost")).Content, body));
            var row = Assert.IsType<ListBoxItem>(list.ItemContainerGenerator.ContainerFromIndex(0));
            frame.ShowSystemMenu();
            var canvas = Assert.IsType<Canvas>(frame.Parent);
            var menu = Assert.Single(canvas.Children.OfType<Border>());
            foreach (var palette in Windows31PaletteCatalog.All)
            {
                Windows31PaletteResources.Apply(desktop, palette.Id);
                Assert.Equal(palette.Id, Windows31PaletteResources.GetScheme(frame));
                Assert.Equal(ColorOf(palette[Windows31ColorRole.Desktop]), ColorOf(desktop.Background));
                Assert.Equal(ColorOf(palette[Windows31ColorRole.ActiveBorder]), ColorOf(Assert.IsType<Border>(frame.FindName("FrameBorder")).Background));
                Assert.Equal(ColorOf(palette[Windows31ColorRole.ActiveCaption]), ColorOf(Assert.IsType<Grid>(frame.FindName("Caption")).Background));
                Assert.Equal(ColorOf(palette[Windows31ColorRole.CaptionText]), ColorOf(Assert.IsType<TextBlock>(frame.FindName("CaptionText")).Foreground));
                Assert.Equal(ColorOf(palette[Windows31ColorRole.ButtonFace]), ColorOf(button.Background));
                Assert.Equal(ColorOf(palette[Windows31ColorRole.ButtonText]), ColorOf(button.Foreground));
                Assert.Equal(ColorOf(palette[Windows31ColorRole.Window]), ColorOf(input.Background));
                Assert.Equal(ColorOf(palette[Windows31ColorRole.WindowText]), ColorOf(input.Foreground));
                Assert.Equal(ColorOf(palette[Windows31ColorRole.Selection]), ColorOf(row.Background));
                Assert.Equal(ColorOf(palette[Windows31ColorRole.SelectionText]), ColorOf(row.Foreground));
                Assert.Equal(ColorOf(palette[Windows31ColorRole.Menu]), ColorOf(menu.Background));
                placement.Deactivate();
                Assert.Equal(ColorOf(palette[Windows31ColorRole.InactiveBorder]), ColorOf(Assert.IsType<Border>(frame.FindName("FrameBorder")).Background));
                Assert.Equal(ColorOf(palette[Windows31ColorRole.InactiveCaption]), ColorOf(Assert.IsType<Grid>(frame.FindName("Caption")).Background));
                placement.Activate();
                frame.ShowSystemMenu();
                menu = Assert.Single(canvas.Children.OfType<Border>());
                Assert.Equal("value", input.Text);
                Assert.Same(body, Assert.IsType<ContentControl>(frame.FindName("DocumentHost")).Content);
            }
            Windows31PaletteResources.Apply(desktop, Windows31ColorScheme.WindowsDefault);
            Assert.Equal(Windows31ColorScheme.WindowsDefault, Windows31PaletteResources.GetScheme(frame));
            Assert.Equal(Windows31WindowState.Normal, placement.State);
        }
        finally { desktop.Dispose(); body.Children.Clear(); }
    });

    private static Color ColorOf(Brush brush) => Assert.IsType<SolidColorBrush>(brush).Color;
    private static void Arrange(FrameworkElement view)
    {
        view.Measure(new Size(1280, 800));
        view.Arrange(new Rect(0, 0, 1280, 800));
        view.UpdateLayout();
    }
    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }
}
