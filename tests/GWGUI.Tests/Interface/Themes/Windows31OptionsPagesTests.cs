using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using GWGUI.App.Localization.Extensions;
using GWGUI.App.Options.Controllers;
using GWGUI.App.Options.States;
using GWGUI.App.Services.Input.GameInput;
using GWGUI.App.Theme.OS.Editions.Windows31.Views;
using GWGUI.App.ViewModels.Options;
using GWGUI.App.Views.Controls.Options;
using GWGUI.Infrastructure.Settings;
using GWGUI.Infrastructure.Settings.Engines;
using GWGUI.Tests.Application.TestInfrastructure;
using GWGUI.Tests.Interface.DeviceSettingsViews;

namespace GWGUI.Tests.Interface.Themes;

[Collection("WPF")]
public sealed class Windows31OptionsPagesTests(StaExecutionScenarios sta)
{
    [Fact]
    public Task LogRowsUseExistingDeferredValidationAndPersistence() => sta.Run(() =>
    {
        var source = new OptionsLogsSection();
        var settings = new AppSettings();
        var saves = 0;
        var controller = new LoggingOptionsController(source, settings, () => false,
            () => { saves++; return Task.CompletedTask; }, key => LocExtension.Get(key), error => throw error, "unused");
        using var view = new LogsOptionsView(source);
        Arrange(view);
        var box = Descendants(view).OfType<TextBox>().First();
        var row = Assert.IsType<LogOptionRow>(box.DataContext);
        box.Text = "64";
        box.RaiseEvent(FocusLost(box, view));
        Assert.Equal(64, row.Settings.MaximumKilobytes);
        Assert.True(saves > 0);
        box.Text = "-7";
        box.RaiseEvent(FocusLost(box, view));
        Assert.Equal("64", box.Text);
        Assert.Equal(64, row.Settings.MaximumKilobytes);
        var enabled = Descendants(view).OfType<CheckBox>().First(item => ReferenceEquals(item.DataContext, row));
        enabled.IsChecked = true;
        enabled.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.True(row.Settings.Enabled);
        view.Dispose();
        Assert.Null(view.Content);
    });

    [Fact]
    public Task HardwareFieldsAndActionsKeepTheirRowsAndDownloadState() => sta.Run(() =>
    {
        var source = new OptionsHardwareSection();
        var row = new HardwareRow("drive", "COM3", "virtual", "Reader", "5.25", "DD", "300 RPM", true, true, "configured");
        source.Drives.ItemsSource = new[] { row };
        var scans = 0;
        var additions = 0;
        var saves = new List<object>();
        RoutedEventHandler scan = (_, _) => scans++;
        RoutedEventHandler add = (_, _) => additions++;
        RoutedEventHandler save = (sender, _) => saves.Add(((FrameworkElement)sender).DataContext);
        source.ScanRequested += scan;
        source.AddDriveRequested += add;
        source.SaveDriveRequested += save;
        using var view = new HardwareOptionsView(source);
        try
        {
            Arrange(view);
            Field<Button>(view, "ScanAction").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Field<Button>(view, "AddAction").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Equal((1, 1), (scans, additions));
            var list = Field<ListBox>(view, "Drives");
            list.SelectedIndex = 0;
            Assert.Same(row, source.Drives.SelectedItem);
            var choices = Descendants(list).OfType<ComboBox>().ToArray();
            choices[0].SelectedItem = "3.5";
            Assert.Equal("3.5", row.Size);
            Descendants(list).OfType<Button>().First().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Same(row, Assert.Single(saves));
            Field<TextBox>(view, "ToolPath").Text = "tool.exe";
            Assert.Equal("tool.exe", source.GwPath.Text);
            source.DownloadProgress.Visibility = Visibility.Visible;
            source.DownloadProgress.Value = 42;
            Assert.Equal(42, Field<ProgressBar>(view, "Progress").Value);
            source.HostToolsState.Text = "Downloading";
            Assert.Equal("Downloading", Field<TextBlock>(view, "State").Text);
            view.Dispose();
            Field<Button>(view, "ScanAction").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Equal(1, scans);
        }
        finally
        {
            source.ScanRequested -= scan;
            source.AddDriveRequested -= add;
            source.SaveDriveRequested -= save;
        }
    });

    [Fact]
    public Task EngineSelectionUsesExistingControllerAndDoesNotMoveItsItems() => sta.Run(() =>
    {
        var source = new OptionsEnginesSection();
        var settings = new AppSettings();
        var saves = 0;
        var controller = new EngineOptionsController(source, settings.Engines, () => false,
            () => { saves++; return Task.CompletedTask; });
        using var view = new EnginesOptionsView(source);
        var own = Field<ComboBox>(view, "PhysicalRead");
        Assert.NotSame(source.PhysicalRead.Items[0], own.Items[0]);
        var next = own.SelectedIndex == 0 ? 1 : 0;
        own.SelectedIndex = next;
        controller.ApplyTo(settings);
        Assert.Equal(next == 0 ? OperationEngine.Internal : OperationEngine.GreaseweazleHostTools, settings.Engines.PhysicalRead);
        Assert.True(saves > 0);
        ((ComboBoxItem)source.PhysicalRead.Items[0]).IsEnabled = false;
        Assert.False(((ComboBoxItem)own.Items[0]).IsEnabled);
        view.Dispose();
        Assert.Equal(2, source.PhysicalRead.Items.Count);
    });

    [Fact]
    public Task ProfileActionsUseTheActiveCategoryEvenWhenAnotherCategoryRetainsASelection() => sta.Run(() =>
    {
        var source = new OptionsProfilesSection();
        var state = new ProfileOptionsState([]);
        state.Read.Add(new("read", "Read", "Read profile", false));
        state.Write.Add(new("write", "Write", "Write profile", false));
        var saves = 0;
        // All owner-dependent prompts are supplied; no native window or file is created.
        var controller = new ProfileOptionsController(null!, source, state,
            () => { saves++; return Task.CompletedTask; }, (key, _) => key,
            _ => "Renamed", _ => true, () => throw new InvalidOperationException());
        using var view = new ProfilesOptionsView(source);
        Field<ListBox>(view, "ReadProfiles").SelectedIndex = 0;
        Field<TabControl>(view, "Categories").SelectedIndex = 1;
        Field<ListBox>(view, "WriteProfiles").SelectedIndex = 0;
        Field<Button>(view, "RenameAction").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Equal("Renamed", state.Write.Single().Name);
        Assert.Equal("Read profile", state.Read.Single().Name);
        Field<ListBox>(view, "WriteProfiles").SelectedIndex = 0;
        Field<Button>(view, "DeleteAction").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Empty(state.Write);
        Assert.Single(state.Read);
        Assert.Equal(2, saves);
        view.Dispose();
        Assert.Single(source.ReadProfiles.Items);
    });

    [Fact]
    public Task ControllerPresentationCopiesLiveDrawingAndRestoresItsMonitoringOwner() => sta.RunAsync(async () =>
    {
        var input = new DeviceListScenarios.Source();
        var profiles = new DeviceListScenarios.Profiles();
        var source = DeviceListScenarios.View(input, profiles);
        await source.RefreshDevicesAsync(false);
        using var view = new ControllersOptionsView(source);
        Assert.False(source.PresentationVisibility);
        var drawing = Field<ControllerVisualizer>(view, "Visualizer");
        Assert.NotSame(source.Visualizer, drawing);
        source.Visualizer.Model = ControllerVisualModel.XboxOne;
        Assert.Equal(source.Visualizer.Model, drawing.Model);
        Field<ComboBox>(view, "DeviceSelector").SelectedIndex = 1;
        Assert.Equal(1, source.DeviceSelector.SelectedIndex);
        await source.RefreshLiveStateAsync();
        Assert.Equal(source.Visualizer.State, drawing.State);
        Field<Slider>(view, "StickDeadZoneSlider").Value = 12;
        Field<Slider>(view, "StickDeadZoneSlider").RaiseEvent(FocusLost(view, source));
        Assert.Contains("two", profiles.Saved);
        source.AnalogDeadZonePanel.Visibility = Visibility.Collapsed;
        Assert.Equal(Visibility.Collapsed, Field<TabItem>(view, "AnalogTab").Visibility);
        Assert.Equal(source.ModelSelectorPanel.Visibility, Field<Grid>(view, "ModelSelectorPanel").Visibility);
        var before = drawing.Model;
        view.Dispose();
        Assert.Null(source.PresentationVisibility);
        source.Visualizer.Model = before == ControllerVisualModel.XboxOne ? ControllerVisualModel.GenericGamepad : ControllerVisualModel.XboxOne;
        Assert.Equal(before, drawing.Model);
        Assert.Equal(1, input.Started);
    });

    private static KeyboardFocusChangedEventArgs FocusLost(IInputElement oldFocus, IInputElement newFocus) =>
        new(Keyboard.PrimaryDevice, Environment.TickCount, oldFocus, newFocus) { RoutedEvent = Keyboard.LostKeyboardFocusEvent };
    private static T Field<T>(FrameworkElement view, string name) => Assert.IsType<T>(view.FindName(name));
    private static void Arrange(FrameworkElement view)
    {
        view.Measure(new Size(850, 500));
        view.Arrange(new Rect(0, 0, 850, 500));
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
