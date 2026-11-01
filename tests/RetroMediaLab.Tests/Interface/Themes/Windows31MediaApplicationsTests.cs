using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using RetroMediaLab.App.Contracts.ViewModels.Visualization;
using RetroMediaLab.App.Theme.OS.Editions.Windows31;
using RetroMediaLab.App.Theme.OS.Editions.Windows31.Views;
using RetroMediaLab.App.ViewModels.Main;
using RetroMediaLab.App.Views.Controls.Conversion;
using RetroMediaLab.App.Views.Controls.Read;
using RetroMediaLab.App.Views.Controls.Shell;
using RetroMediaLab.App.Views.Controls.Visualization;
using RetroMediaLab.App.Views.Controls.Write;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Images.Visualization;
using RetroMediaLab.MediaEngine.Interfaces;
using RetroMediaLab.Tests.Application.TestInfrastructure;

namespace RetroMediaLab.Tests.Interface.Themes;

[Collection("WPF")]
public sealed class Windows31MediaApplicationsTests(StaExecutionScenarios sta)
{
    [Fact]
    public Task ProgramApplicationsOpenTheirOwnFormsAndTheSameOperationalOptions() => sta.RunAsync(async () =>
    {
        var sources = new Sources();
        await Dispatcher.Yield(DispatcherPriority.DataBind);
        using var desktop = sources.Desktop();
        desktop.Open(Windows31ApplicationId.Read);
        desktop.Open(Windows31ApplicationId.Write);
        desktop.Open(Windows31ApplicationId.Conversion);
        desktop.Open(Windows31ApplicationId.Visualization);
        Arrange(desktop);
        Assert.Equal(4, desktop.OpenApplications.Count);
        var read = Only<AcquisitionView>(desktop);
        var write = Only<WriteView>(desktop);
        var conversion = Only<ConversionView>(desktop);
        Assert.NotSame(sources.Read, read);
        Assert.NotSame(sources.Write, write);
        Find<MenuItem>(read, "AdvancedCommand").RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Arrange(desktop);
        Assert.NotNull(Only<AcquisitionOptionsView>(desktop));
        desktop.CloseDialog(Windows31ApplicationId.Read, Windows31DialogKind.AcquisitionOptions);
        Assert.Empty(Descendants(desktop).OfType<AcquisitionOptionsView>());
        Find<MenuItem>(write, "AdvancedCommand").RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Arrange(desktop);
        var writeOptions = Only<WriteOptionsView>(desktop);
        Assert.IsType<CheckBox>(writeOptions.Field(WriteOptionField.NoVerify)).IsChecked = true;
        Assert.True(sources.Write.AdvancedBlock.NoVerifyCheckBox.IsChecked);
        var optionsForm = Only<Windows31DialogForm>(desktop);
        optionsForm.CloseButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Empty(Descendants(desktop).OfType<Windows31DialogForm>());
        Assert.Null(optionsForm.CloseButton.Parent);
        Assert.Equal(4, desktop.OpenApplications.Count);
        Find<MenuItem>(conversion, "AdvancedCommand").RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Arrange(desktop);
        var conversionOptions = Only<ConversionOptionsView>(desktop);
        var reverse = Assert.IsType<CheckBox>(conversionOptions.Field(ConversionOptionField.Reverse));
        reverse.IsChecked = true;
        Assert.True(Find<CheckBox>(sources.Conversion.AdvancedBlock, nameof(ConversionOptionField.Reverse)).IsChecked);
        desktop.CloseApplication(Windows31ApplicationId.Conversion);
        Assert.Empty(Descendants(desktop).OfType<ConversionOptionsView>());
        Assert.Empty(Descendants(desktop).OfType<ConversionView>());
        Assert.True(reverse.IsChecked);
    });

    [Fact]
    public Task InspectorAndClassificationMenusOpenInternalDialogsAndKeepTheLiveMediaModel() => sta.Run(() =>
    {
        var sources = new Sources();
        var canvas = sources.Visualization.FirstSide.Canvas;
        var originalParent = canvas.Parent;
        sources.Visualization.ShowDocument(new MediaImageDocument(new MediaSourceDescriptor("sample.scp", []), "raw.scp",
            MediaKind.Floppy, new Representation(), [], [], new Dictionary<string, string>()),
            new MediaVisualizationDescriptor(MediaRepresentationKind.Flux, [0], MediaVisualizationProgressUnit.Track,
                MediaVisualizationDirection.Ascending, [new MediaVisualizationElement(0, 0)]));
        sources.Visualization.SetInspectorModel(new MediaInspectorModel("first", null, []));
        using var desktop = sources.Desktop();
        desktop.Open(Windows31ApplicationId.Visualization);
        Arrange(desktop);
        var view = Only<VisualizationView>(desktop);
        Find<MenuItem>(view, "InspectorCommand").RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Arrange(desktop);
        var inspector = Only<InspectorView>(desktop);
        Assert.Equal("first", Find<TextBlock>(inspector, "Title").Text);
        sources.Visualization.SetInspectorModel(new MediaInspectorModel("second", null, []));
        Assert.Equal("second", Find<TextBlock>(inspector, "Title").Text);
        Find<MenuItem>(view, "ClassificationCommand").RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Arrange(desktop);
        var classification = Only<ClassificationView>(desktop);
        Assert.False(view.IsEnabled);
        Assert.False(inspector.IsEnabled);
        Find<Button>(classification, "CloseCommand").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.True(view.IsEnabled);
        Assert.True(inspector.IsEnabled);
        Find<Button>(inspector, "CloseCommand").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Empty(Descendants(desktop).OfType<InspectorView>());
        Find<MenuItem>(view, "CloseCommand").RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Assert.Empty(desktop.OpenApplications);
        Assert.Same(originalParent, canvas.Parent);
    });

    [Fact]
    public Task ClosingAnApplicationDetachesTheNewMenuAndNeverReopensAClosedApplication() => sta.RunAsync(async () =>
    {
        var sources = new Sources();
        await Dispatcher.Yield(DispatcherPriority.DataBind);
        using var desktop = sources.Desktop();
        desktop.Open(Windows31ApplicationId.Write);
        Arrange(desktop);
        var write = Only<WriteView>(desktop);
        var menu = Find<MenuItem>(write, "WriteCommand");
        var execute = 0;
        RoutedEventHandler requested = (_, _) => execute++;
        sources.Write.ExecuteRequested += requested;
        try
        {
            Find<TextBox>(write, "SourcePath").Text = "archive.scp";
            Assert.Equal("archive.scp", sources.Model.Write.SourcePath);
            menu.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Assert.Equal(1, execute);
            Find<MenuItem>(write, "CloseCommand").RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            menu.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            desktop.CloseApplication(Windows31ApplicationId.Write);
            Assert.Equal(1, execute);
            Assert.Empty(desktop.OpenApplications);
            sources.Write.ExecuteActionButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Equal(2, execute);
        }
        finally { sources.Write.ExecuteRequested -= requested; }
    });

    private sealed class Sources
    {
        internal MainWindowViewModel Model { get; } = new("hardware", "ready");
        internal ReadTabSection Read { get; }
        internal WriteTabSection Write { get; }
        internal ConversionTabSection Conversion { get; }
        internal VisualizerTabSection Visualization { get; } = new();
        private ApplicationStatusBar Status { get; }
        internal Sources()
        {
            Read = new() { DataContext = Model };
            Write = new() { DataContext = Model };
            Conversion = new() { DataContext = Model };
            Status = new() { DataContext = Model };
        }
        internal Windows31Desktop Desktop() => new(desktop => Windows31MediaApplications.Create(desktop,
            Read, Write, Conversion, Visualization, Status));
    }

    private static T Find<T>(FrameworkElement root, string name) => Assert.IsType<T>(root.FindName(name));
    private static T Only<T>(DependencyObject root) => Assert.Single(Descendants(root).OfType<T>());
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
    private sealed class Representation : IMediaImageRepresentation
    {
        public MediaRepresentationKind RepresentationKind => MediaRepresentationKind.Flux;
        public long? LogicalLength => null;
        public bool SupportsRandomAccess => false;
        public bool SupportsSequentialAccess => true;
    }
}
