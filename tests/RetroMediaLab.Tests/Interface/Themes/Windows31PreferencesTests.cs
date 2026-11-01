using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using RetroMediaLab.App.Theme.OS.Editions.Windows31;
using RetroMediaLab.App.Theme.OS.Editions.Windows31.Views;
using RetroMediaLab.App.Views.Controls.Options;
using RetroMediaLab.Tests.Application.TestInfrastructure;
using RetroMediaLab.Tests.Interface.DeviceSettingsViews;

namespace RetroMediaLab.Tests.Interface.Themes;

[Collection("WPF")]
public sealed class Windows31PreferencesTests(StaExecutionScenarios sta)
{
    [Fact]
    public Task AllPagesKeepTheirFunctionalSourcesAndDetachTogether() => sta.Run(() =>
    {
        var sources = new FrameworkElement[]
        {
            new OptionsGeneralSection(), new OptionsThemesSection(), new OptionsLogsSection(),
            new OptionsHardwareSection(), new OptionsEnginesSection(), new OptionsProfilesSection(),
            DeviceListScenarios.View(new DeviceListScenarios.Source())
        };
        var navigation = new TabControl();
        foreach (var source in sources) navigation.Items.Add(new TabItem { Content = source });
        navigation.SelectedIndex = 0;
        var closed = 0;
        using var view = new PreferencesView(navigation, () => closed++);
        var expected = new[] { typeof(GeneralOptionsView), typeof(ThemeOptionsView), typeof(LogsOptionsView),
            typeof(HardwareOptionsView), typeof(EnginesOptionsView), typeof(ProfilesOptionsView), typeof(ControllersOptionsView) };
        for (var index = 0; index < expected.Length; index++)
        {
            navigation.SelectedIndex = index;
            Assert.Equal(expected[index], view.Children.OfType<ContentControl>().Single().Content.GetType());
            Assert.Same(sources[index], ((TabItem)navigation.Items[index]).Content);
        }
        var menu = view.Children.OfType<Menu>().Single();
        var choices = ((MenuItem)menu.Items[0]).Items.OfType<MenuItem>().ToArray();
        choices[0].RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Assert.Equal(0, navigation.SelectedIndex);
        Assert.True(choices[0].IsChecked);
        view.Dispose();
        Assert.Empty(view.Children);
        Assert.Null(((OptionsControllersSection)sources[^1]).PresentationVisibility);
        navigation.SelectedIndex = 2;
        Assert.Empty(view.Children);
        Assert.Equal(0, closed);
    });

    [Fact]
    public Task SystemPreferencesCommandUsesTheExistingRequest() => sta.Run(() =>
    {
        var sources = new Windows31WorkspaceTests.Sources();
        var menu = sources.Root.Children.OfType<RetroMediaLab.App.Views.Controls.Shell.MainMenu>().Single();
        var requests = 0;
        menu.PreferencesRequested += (_, _) => requests++;
        using var workspace = new Windows31Workspace(sources.Root, sources.Tabs);
        workspace.Desktop.Open(Windows31ApplicationId.Preferences);
        Assert.Equal(1, requests);
        Assert.Empty(workspace.Desktop.OpenApplications);
    });

    [Fact]
    public Task RootModalBlocksOtherFramesAndWaitsForItsFunctionalClose() => sta.Run(() =>
    {
        using var desktop = new Windows31Desktop([
            new(Windows31ApplicationId.Read, Windows31ProgramGroup.Media, "Tab.Read", () => new Windows31Document(new Grid()), new Size(500, 350))]);
        desktop.Open(Windows31ApplicationId.Read);
        var closes = 0;
        var placement = desktop.ShowDialog(Windows31ApplicationId.ProgramManager, Windows31DialogKind.Preferences,
            Windows31DialogMode.Modal, "Options.PreferencesTitle", new Size(600, 450),
            _ => new Windows31Document(new Grid()), () => closes++);
        var frames = Descendants(desktop).OfType<Windows31Frame>().ToArray();
        var dialog = frames.Single(frame => frame.Title == RetroMediaLab.App.Localization.Extensions.LocExtension.Get("Options.PreferencesTitle"));
        Assert.True(dialog.IsEnabled);
        Assert.All(frames.Where(frame => !ReferenceEquals(frame, dialog)), frame => Assert.False(frame.IsEnabled));
        ((Button)dialog.FindName("SystemBox")).RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
            { RoutedEvent = Control.MouseDoubleClickEvent });
        Assert.Equal(1, closes);
        Assert.NotEqual(Windows31WindowState.Closed, placement.State);
        desktop.CloseDialog(Windows31ApplicationId.ProgramManager, Windows31DialogKind.Preferences);
        Assert.Equal(Windows31WindowState.Closed, placement.State);
        Assert.All(frames.Where(frame => !ReferenceEquals(frame, dialog)), frame => Assert.True(frame.IsEnabled));
        Assert.Null(dialog.Content);
    });

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
