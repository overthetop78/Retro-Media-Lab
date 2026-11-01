using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.App.Theme.OS.Editions.Windows31.Views;
using RetroMediaLab.App.Views.Controls.Visualization;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Images.Visualization;
using RetroMediaLab.MediaEngine.Interfaces;
using RetroMediaLab.Tests.Application.TestInfrastructure;

namespace RetroMediaLab.Tests.Interface.Themes;

[Collection("WPF")]
public sealed class Windows31VisualizationViewTests(StaExecutionScenarios sta)
{
    [Fact]
    public Task MenusKeepExistingCommandsWhileClosingDetachesTheNewPresentation()
        => sta.Run(() =>
        {
            var source = new VisualizerTabSection();
            var opens = 0;
            var resets = 0;
            var closes = 0;
            var inspections = 0;
            var classifications = 0;
            RoutedEventHandler open = (_, _) => opens++;
            RoutedEventHandler reset = (_, _) => resets++;
            source.OpenRequested += open;
            source.Header.ResetButton.Click += reset;
            try
            {
                var view = new VisualizationView(source, () => inspections++, () => classifications++, () => closes++);
                var openCommand = Find<MenuItem>(view, "OpenCommand");
                var closeCommand = Find<MenuItem>(view, "CloseCommand");
                try
                {
                    openCommand.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                    Find<Button>(view, "EmptyOpen").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    Find<MenuItem>(view, "ResetCommand").RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                    Find<MenuItem>(view, "ClassificationCommand").RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                    closeCommand.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                    Assert.Equal(2, opens);
                    Assert.Equal(1, resets);
                    Assert.Equal(1, closes);
                    Assert.Equal(1, classifications);
                    Assert.False(Find<MenuItem>(view, "InspectorCommand").IsEnabled);
                    var linked = Find<MenuItem>(view, "LinkZoomCommand");
                    linked.IsChecked = false;
                    Assert.False(source.Header.LinkZoomCheckBox.IsChecked);
                    source.SetRecognitionProgress(true, "stage", "detail", 50, indeterminate: true);
                    Assert.True(Find<ProgressBar>(view, "RecognitionProgress").IsIndeterminate);
                    Assert.Equal("detail", Find<TextBlock>(view, "RecognitionDetail").Text);
                    Assert.Equal(Visibility.Collapsed, Find<StackPanel>(view, "EmptyState").Visibility);
                    Assert.Equal(0, inspections);
                }
                finally { view.Dispose(); }
                openCommand.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                closeCommand.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                Assert.Equal(2, opens);
                Assert.Equal(1, closes);
                source.Header.OpenButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert.Equal(3, opens);
            }
            finally
            {
                source.OpenRequested -= open;
                source.Header.ResetButton.Click -= reset;
            }
        });

    [Fact]
    public Task SwitchingAllFiveRepresentationsRestoresEachBorrowedCanvasAndTheTimelineViewport()
        => sta.Run(() =>
        {
            var source = new VisualizerTabSection();
            source.RegisterRepresentationView(MediaRepresentationKind.Sectors, new SectorMediaView());
            source.RegisterRepresentationView(MediaRepresentationKind.Blocks, new BlockMediaView());
            source.RegisterRepresentationView(MediaRepresentationKind.OpticalTracks, new OpticalMediaView());
            var timeline = new SequentialMediaView();
            var originalViewport = timeline.ActiveViewport;
            source.RegisterRepresentationView(MediaRepresentationKind.Sequential, timeline);
            var records = new List<(FrameworkElement Canvas, DependencyObject Parent)>();
            var inspections = 0;
            using var view = new VisualizationView(source, () => inspections++, () => { }, () => { });
            foreach (var kind in new[] { MediaRepresentationKind.Flux, MediaRepresentationKind.Sectors,
                MediaRepresentationKind.Blocks, MediaRepresentationKind.OpticalTracks, MediaRepresentationKind.Sequential })
            {
                var surfaces = MediaVisualizationElements.Surfaces(source, kind);
                var parents = surfaces.Select(surface => surface.Document.Element.Parent).ToArray();
                Show(source, kind);
                Assert.Equal(surfaces.Count, Find<Grid>(view, "DrawingArea").Children.Count);
                Assert.Equal(Visibility.Collapsed, Find<StackPanel>(view, "EmptyState").Visibility);
                Assert.True(Find<MenuItem>(view, "InspectorCommand").IsEnabled);
                foreach (var old in records) Assert.Same(old.Parent, old.Canvas.Parent);
                records.Clear();
                for (var index = 0; index < surfaces.Count; index++)
                {
                    Assert.IsType<ThemeElementHost>(surfaces[index].Document.Element.Parent);
                    records.Add((surfaces[index].Document.Element, parents[index]));
                }
                if (kind == MediaRepresentationKind.Sequential) Assert.NotSame(originalViewport, timeline.ActiveViewport);
                else Assert.Same(originalViewport, timeline.ActiveViewport);
            }
            Find<MenuItem>(view, "InspectorCommand").RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Assert.Equal(1, inspections);
            source.ClearDocumentForLoading();
            Assert.Empty(Find<Grid>(view, "DrawingArea").Children);
            Assert.Same(originalViewport, timeline.ActiveViewport);
            foreach (var old in records) Assert.Same(old.Parent, old.Canvas.Parent);
        });

    [Fact]
    public Task AHiddenSurfaceGivesItsWidthToTheRemainingSurfaceAndCanReturn()
        => sta.Run(() =>
        {
            var source = new VisualizerTabSection();
            var sectors = new SectorMediaView();
            source.RegisterRepresentationView(MediaRepresentationKind.Sectors, sectors);
            Show(source, MediaRepresentationKind.Sectors);
            using var view = new VisualizationView(source, () => { }, () => { }, () => { });
            var area = Find<Grid>(view, "DrawingArea");
            sectors.Side1Panel.Visibility = Visibility.Collapsed;
            Assert.Equal(new GridLength(0), area.ColumnDefinitions[1].Width);
            Assert.Equal(new GridLength(1, GridUnitType.Star), area.ColumnDefinitions[0].Width);
            sectors.Side0Panel.Visibility = Visibility.Collapsed;
            sectors.Side1Panel.Visibility = Visibility.Visible;
            Assert.Equal(new GridLength(0), area.ColumnDefinitions[0].Width);
            Assert.Equal(new GridLength(1, GridUnitType.Star), area.ColumnDefinitions[1].Width);
            sectors.Side0Panel.Visibility = Visibility.Visible;
            Assert.Equal(new GridLength(1, GridUnitType.Star), area.ColumnDefinitions[0].Width);
        });

    [Fact]
    public Task BlockModesShareTheOriginalSelectionAndNeverDisplayBothCanvasesAtOnce()
        => sta.Run(() =>
        {
            var source = new VisualizerTabSection();
            var blocks = new BlockMediaView();
            source.RegisterRepresentationView(MediaRepresentationKind.Blocks, blocks);
            Show(source, MediaRepresentationKind.Blocks);
            using var view = new VisualizationView(source, () => { }, () => { }, () => { });
            var drawings = Find<Grid>(view, "DrawingArea");
            Assert.Equal(Visibility.Visible, drawings.Children[0].Visibility);
            Assert.Equal(Visibility.Collapsed, drawings.Children[1].Visibility);
            var choices = Find<WrapPanel>(view, "DocumentChoices");
            var mode = Assert.IsType<ComboBox>(choices.Children[3]);
            mode.SelectedIndex = 1;
            Assert.Equal(1, blocks.ViewSelector.SelectedIndex);
            Assert.Equal(Visibility.Collapsed, drawings.Children[0].Visibility);
            Assert.Equal(Visibility.Visible, drawings.Children[1].Visibility);
            blocks.ViewSelector.SelectedIndex = 0;
            Assert.Equal(0, mode.SelectedIndex);
            Assert.Equal(Visibility.Visible, drawings.Children[0].Visibility);
            blocks.GeometryTab.Visibility = Visibility.Collapsed;
            blocks.GeometryTab.Visibility = Visibility.Visible;
            Assert.Equal(Visibility.Collapsed, drawings.Children[1].Visibility);
            mode.SelectedIndex = 1;
            blocks.GeometryTab.Visibility = Visibility.Collapsed;
            Assert.Equal(0, mode.SelectedIndex);
            Assert.Equal(Visibility.Visible, drawings.Children[0].Visibility);
            Assert.Equal(Visibility.Collapsed, drawings.Children[1].Visibility);
        });

    private static T Find<T>(FrameworkElement view, string name) => Assert.IsType<T>(view.FindName(name));

    private static void Show(VisualizerTabSection source, MediaRepresentationKind kind)
    {
        var document = new MediaImageDocument(new MediaSourceDescriptor("sample", []), "ibm.160",
            MediaKind.Floppy, new Representation(kind), [], [], new Dictionary<string, string>());
        Assert.True(source.ShowDocument(document, new MediaVisualizationDescriptor(kind, [0, 1],
            MediaVisualizationProgressUnit.Track, MediaVisualizationDirection.Ascending,
            [new MediaVisualizationElement(0, 0), new MediaVisualizationElement(1, 1)])));
    }

    private sealed class Representation(MediaRepresentationKind kind) : IMediaImageRepresentation
    {
        public MediaRepresentationKind RepresentationKind => kind;
        public long? LogicalLength => null;
        public bool SupportsRandomAccess => true;
        public bool SupportsSequentialAccess => true;
    }
}
