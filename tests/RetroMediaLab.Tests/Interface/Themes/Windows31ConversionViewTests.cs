using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using RetroMediaLab.App.Contracts.ViewModels.Conversion;
using RetroMediaLab.App.Controllers.MainWindow;
using RetroMediaLab.App.Presenters.Conversion;
using RetroMediaLab.App.Theme.OS.Editions.Windows31.Views;
using RetroMediaLab.App.ViewModels.Main;
using RetroMediaLab.App.Views.Controls.Conversion;
using RetroMediaLab.App.Views.Controls.Shell;
using RetroMediaLab.Infrastructure.Settings;
using RetroMediaLab.Infrastructure.Settings.Engines;
using RetroMediaLab.MediaEngine.Images.Formats;
using RetroMediaLab.Tests.Application.TestInfrastructure;

namespace RetroMediaLab.Tests.Interface.Themes;

[Collection("WPF")]
public sealed class Windows31ConversionViewTests(StaExecutionScenarios sta)
{
    private const string FormatId = "ibm.160";

    [Fact]
    public Task ConversionMenusAndInputsKeepTheExistingEndpointsAndReleaseThemOnClose() => sta.RunAsync(async () =>
    {
        var model = new MainWindowViewModel("hardware", "ready");
        var source = new ConversionTabSection { DataContext = model };
        var status = new ApplicationStatusBar { DataContext = model };
        await Dispatcher.Yield(DispatcherPriority.DataBind);
        var calls = 0;
        source.ExecuteRequested += (_, _) => calls++;
        source.MigrationRequested += (_, _) => calls++;
        var view = new ConversionView(source, status, () => calls++, () => calls++);
        var path = Assert.IsType<TextBox>(view.FindName("SourcePath"));
        var originalParent = source.SourceBlock.Input.Parent;
        var execute = Assert.IsType<MenuItem>(view.FindName("ExecuteCommand"));
        var advanced = Assert.IsType<MenuItem>(view.FindName("AdvancedCommand"));
        try
        {
            Assert.NotSame(path, source.SourceBlock.Input);
            path.Text = @"virtual\source.scp";
            Assert.Equal(path.Text, model.Conversion.SourcePath);
            Assert.IsType<TextBox>(view.FindName("OutputName")).Text = "converted";
            Assert.Equal("converted", model.Conversion.OutputName);
            Assert.IsType<CheckBox>(view.FindName("Tags")).IsChecked = true;
            Assert.True(model.Conversion.AddTags);
            execute.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Assert.IsType<MenuItem>(view.FindName("MigrationCommand")).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            advanced.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Assert.Equal(3, calls);
            source.ExecuteActionButton.IsEnabled = false;
            Assert.False(execute.IsEnabled);
            execute.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Assert.Equal(3, calls);
            Assert.Same(originalParent, source.SourceBlock.Input.Parent);
        }
        finally { view.Dispose(); }
        source.SourceBlock.Text = "after";
        Assert.Equal(@"virtual\source.scp", path.Text);
        advanced.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Assert.Equal(3, calls);
        Assert.Null(view.Content);
    });

    [Fact]
    public Task BaseAndNewFormatControlsUseTheSameControllerAndPreserveExplicitExtensions() => sta.Run(() =>
    {
        var model = new MainWindowViewModel("hardware", "ready");
        var source = new ConversionTabSection { DataContext = model };
        var status = new ApplicationStatusBar { DataContext = model };
        var catalog = new BuiltInImageFormatCatalog(key => key);
        var settings = new AppSettings();
        settings.Engines.Conversion = OperationEngine.GreaseweazleHostTools;
        var controller = Controller(source, model, catalog, settings);
        source.FormatsBlock.ValueChanged += (sender, _) => controller.SelectionChanged(sender);
        controller.BuildFormats(".scp");
        source.FormatsBlock.MachineSelector.SelectedItem = catalog.Formats.Single(format => format.Id == FormatId).Family;
        source.Measure(new Size(1000, 700));
        source.Arrange(new Rect(0, 0, 1000, 700));
        source.UpdateLayout();
        var original = Descendants(source.FormatsBlock).OfType<ConversionFormatControl>().Single(item => item.Format.Id == FormatId);
        Assert.IsType<CheckBox>(original.FindName("FormatCheck")).IsChecked = true;
        Assert.Contains(FormatId, model.Conversion.SelectedFormats);
        using var view = new ConversionView(source, status, () => { }, () => { });
        var formats = Assert.IsType<ListBox>(view.FindName("Formats"));
        var row = formats.Items.Cast<ConversionFormatRow>().Single(item => item.Presentation.Format.Id == FormatId);
        formats.SelectedItem = row;
        Assert.IsType<CheckBox>(row.Content).IsChecked = false;
        Assert.DoesNotContain(FormatId, model.Conversion.SelectedFormats);
        row = formats.Items.Cast<ConversionFormatRow>().Single(item => item.Presentation.Format.Id == FormatId);
        Assert.IsType<CheckBox>(row.Content).IsChecked = true;
        Assert.Contains(FormatId, model.Conversion.SelectedFormats);
        var extensions = Assert.IsType<WrapPanel>(view.FindName("Extensions"));
        var extension = extensions.Children.OfType<CheckBox>().First();
        var selectedExtension = Assert.IsType<string>(extension.Tag);
        extension.IsChecked = true;
        Assert.Contains(selectedExtension, model.Conversion.ExplicitExtensions[FormatId]);
        Assert.NotEmpty(Assert.IsType<ListBox>(view.FindName("SelectedOutputs")).Items);
    });

    [Fact]
    public Task UnknownOrIncompatibleFormatsCannotBeSelectedThroughThePresentationBridge() => sta.Run(() =>
    {
        var source = new ConversionTabSection();
        var catalog = new BuiltInImageFormatCatalog(key => key);
        var format = catalog.Formats.Single(item => item.Id == FormatId);
        var presentation = new ConversionFormatPresentation(format, false, false,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase), RetroMediaLab.App.Enums.ViewModels.Conversion.ConversionFormatGroup.Common, false);
        source.FormatsBlock.SetItems([presentation]);
        var received = new List<ConversionFormatPresentation>();
        source.FormatsBlock.ValueChanged += (sender, _) => received.Add(Assert.IsType<ConversionFormatPresentation>(sender));
        source.FormatsBlock.RequestSelection("unknown", true, []);
        source.FormatsBlock.RequestSelection(FormatId, true, []);
        Assert.Empty(received);
        source.FormatsBlock.SetItems([presentation with { IsCompatible = true }]);
        source.FormatsBlock.RequestSelection(FormatId, true, [format.Extensions[0].Extension, ".unknown"]);
        var selected = Assert.Single(received);
        Assert.True(selected.IsSelected);
        Assert.Equal(format.Extensions[0].Extension, Assert.Single(selected.ExplicitExtensions));
    });

    [Fact]
    public Task ConversionOptionsRetainModelNotificationsAndDetachWithoutMovingBaseControls() => sta.RunAsync(async () =>
    {
        var model = new MainWindowViewModel("hardware", "ready");
        var source = new ConversionAdvancedSection { DataContext = model };
        await Dispatcher.Yield(DispatcherPriority.DataBind);
        var events = 0;
        source.InputChanged += (_, _) => events++;
        var original = Assert.IsType<TextBox>(source.FindName(nameof(ConversionOptionField.OutputTracksValue)));
        var parent = original.Parent;
        var view = new ConversionOptionsView(source);
        TextBox? output = null;
        try
        {
            foreach (var field in Enum.GetValues<ConversionOptionField>()) Assert.NotSame(source.FindName(field.ToString()), view.Field(field));
            output = Assert.IsType<TextBox>(view.Field(ConversionOptionField.OutputTracksValue));
            output.Text = "c=1-3:h=0";
            Assert.Equal("c=1-3:h=0", model.Conversion.OutputTracks.Value);
            Assert.IsType<CheckBox>(view.Field(ConversionOptionField.Reverse)).IsChecked = true;
            Assert.True(model.Conversion.Reverse.Enabled);
            Assert.True(events > 0);
            Assert.Same(parent, original.Parent);
        }
        finally { view.Dispose(); }
        original.Text = "after";
        Assert.Equal("c=1-3:h=0", output!.Text);
        Assert.Null(view.Content);
    });

    private static ConversionTabController Controller(ConversionTabSection source, MainWindowViewModel model, IImageFormatCatalog catalog, AppSettings settings) =>
        new(owner: null!, view: source, viewModel: model, profileController: null!, formatPresenter: new ConversionFormatPresenter(),
            formatCatalog: () => catalog, formatDetector: null!, settings: () => settings, commandBuilder: null!, runner: null!,
            mediaReader: null!, mediaConversion: null!, sequentialMediaConversion: null!, selectSource: null!, businessDialogs: null!,
            dialogs: null!, diskDefinitionsController: null!, operation: null!, consoleLog: null!, diskImageWorkspace: null!,
            commandPreview: new TextBox(), logOutput: new RichTextBox(), selectedMainTab: () => -1, selectMainTab: null!,
            loadImage: null!, confirmAndRequestStop: null!, appendAnalysisFailure: null!, updateProfileStatus: null!, dispatcher: Dispatcher.CurrentDispatcher);

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        yield return root;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
            foreach (var child in Descendants(VisualTreeHelper.GetChild(root, index))) yield return child;
    }
}
