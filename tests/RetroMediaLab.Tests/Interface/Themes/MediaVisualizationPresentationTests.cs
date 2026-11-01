using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using RetroMediaLab.App.Theme.Common;
using SkiaSharp.Views.WPF;
using RetroMediaLab.App.Contracts.Rendering.Sequential;
using SequentialSegmentKind = RetroMediaLab.App.Enums.Rendering.Sequential.SequentialSegmentKind;
using RetroMediaLab.App.Views.Controls.Visualization;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.Tests.Application.TestInfrastructure;

namespace RetroMediaLab.Tests.Interface.Themes;

[Collection("WPF")]
public sealed class MediaVisualizationPresentationTests(StaExecutionScenarios sta)
{
    [Fact]
    public Task PresentationNotificationFollowsRegistrationLoadingAndRecognition()
        => sta.Run(() =>
        {
            var source = new VisualizerTabSection();
            var notifications = 0;
            EventHandler changed = (_, _) => notifications++;
            source.PresentationChanged += changed;
            var view = new Border();
            try
            {
                source.RegisterRepresentationView(MediaRepresentationKind.Sectors, view);
                Assert.Equal(1, notifications);
                Assert.Same(view, source.RegisteredRepresentationViews[MediaRepresentationKind.Sectors]);
                source.SetRecognitionProgress(true, "stage", "detail", 25);
                Assert.Equal(2, notifications);
                Assert.True(source.RecognitionProgressVisible);
                Assert.Equal("detail", source.RecognitionDetail);
                source.ClearDocumentForLoading();
                Assert.Equal(3, notifications);
                Assert.Null(source.ActiveRepresentationKind);
                Assert.Equal(Visibility.Collapsed, view.Visibility);
                Assert.Null(source.CurrentDocument);
            }
            finally
            {
                source.PresentationChanged -= changed;
                Assert.IsType<Grid>(view.Parent).Children.Remove(view);
            }
        });

    [Fact]
    public Task OverviewNotifiesAfterItsRowsAndSequentialAddressMappingAreReady()
        => sta.Run(() =>
        {
            var source = new VisualizerTrackOverview();
            var first = new SequentialMediaSegment(100, 3, TimeSpan.Zero, TimeSpan.Zero, SequentialSegmentKind.Signal);
            var second = new SequentialMediaSegment(900, 7, TimeSpan.Zero, TimeSpan.Zero, SequentialSegmentKind.DecodedBlock);
            var received = new List<(int, long)>();
            var changes = 0;
            void Selected(int surface, long position) => received.Add((surface, position));
            EventHandler changed = (_, _) =>
            {
                changes++;
                Assert.Equal(2, source.Rows.Count);
                source.RequestSelection(1, 0);
            };
            source.ElementSelected += Selected;
            source.RowsChanged += changed;
            try
            {
                source.ConfigureSequential(new SequentialMediaRenderModel(1000, null, [first, second]));
                Assert.Equal(1, changes);
                Assert.Equal((7, 900L), Assert.Single(received));
                Assert.Single(source.Rows[0].Segments);
                Assert.Single(source.Rows[1].Segments);
                source.RowsChanged -= changed;
                source.Configure(new Dictionary<int, IReadOnlyList<int>> { [0] = [2, 4] });
                source.RequestSelection(0, 4);
                Assert.Equal((0, 4L), received[^1]);
                Assert.True(source.Rows[0].Segments.Single(item => item.Position == 4).IsSelected);
            }
            finally
            {
                source.ElementSelected -= Selected;
                source.RowsChanged -= changed;
            }
        });

    [Fact]
    public Task AllFiveRepresentationsBorrowOnlyTheirDrawingLeavesAndRestoreTheirOriginalParents()
        => sta.Run(() =>
        {
            var source = new VisualizerTabSection();
            source.RegisterRepresentationView(MediaRepresentationKind.Sectors, new SectorMediaView());
            source.RegisterRepresentationView(MediaRepresentationKind.Blocks, new BlockMediaView());
            source.RegisterRepresentationView(MediaRepresentationKind.OpticalTracks, new OpticalMediaView());
            source.RegisterRepresentationView(MediaRepresentationKind.Sequential, new SequentialMediaView());
            foreach (var kind in new[] { MediaRepresentationKind.Flux, MediaRepresentationKind.Sectors,
                MediaRepresentationKind.Blocks, MediaRepresentationKind.OpticalTracks, MediaRepresentationKind.Sequential })
            {
                var surfaces = MediaVisualizationElements.Surfaces(source, kind);
                Assert.NotEmpty(surfaces);
                var parents = surfaces.Select(surface => surface.Document.Element.Parent).ToArray();
                using (var lease = new ThemeElementLease(surfaces.Select(surface => surface.Document)))
                {
                    foreach (var surface in surfaces)
                    {
                        Assert.IsType<SKElement>(surface.Document.Element);
                        Assert.Same(surface.Document.Element, lease[surface.Document.Id].Content);
                        Assert.Same(surface.Source, FindSource(surface.ZoomOut));
                        Assert.Same(surface.Source, FindSource(surface.ResetZoom));
                        Assert.Same(surface.Source, FindSource(surface.ZoomIn));
                        if (kind == MediaRepresentationKind.Sequential)
                            Assert.Same(surface.Source, surface.Timeline);
                    }
                }
                for (var index = 0; index < surfaces.Count; index++)
                    Assert.Same(parents[index], surfaces[index].Document.Element.Parent);
            }
        });

    private static UserControl? FindSource(FrameworkElement element)
    {
        for (var current = element.Parent as FrameworkElement; current is not null; current = current.Parent as FrameworkElement)
            if (current is UserControl control) return control;
        return null;
    }

    [Fact]
    public Task ChangingOrUnloadingTheBorrowedViewportCancelsDeferredZoomPlacement()
        => sta.Run(() =>
        {
            var source = new SequentialMediaView();
            var original = source.ActiveViewport;
            var first = new ScrollViewer();
            var second = new ScrollViewer();
            var zoom = Assert.IsType<Button>(source.FindName("ResetZoomButton"));
            var posted = new List<DispatcherOperation>();
            void Posted(object? sender, DispatcherHookEventArgs args)
            {
                if (args.Operation.Priority == DispatcherPriority.Loaded) posted.Add(args.Operation);
            }
            var hooks = Dispatcher.CurrentDispatcher.Hooks;
            hooks.OperationPosted += Posted;
            try
            {
                source.SetPresentationViewport(first);
                Assert.Same(first, source.ActiveViewport);
                zoom.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var firstPlacement = Assert.Single(posted);
                Assert.Equal(DispatcherOperationStatus.Pending, firstPlacement.Status);
                source.SetPresentationViewport(second);
                Assert.Equal(DispatcherOperationStatus.Aborted, firstPlacement.Status);
                Assert.Same(second, source.ActiveViewport);
                posted.Clear();
                zoom.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var secondPlacement = Assert.Single(posted);
                source.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
                Assert.Equal(DispatcherOperationStatus.Aborted, secondPlacement.Status);
                source.SetPresentationViewport(null);
                Assert.Same(original, source.ActiveViewport);
                Assert.Null(first.Parent);
                Assert.Null(second.Parent);
            }
            finally
            {
                try { source.SetPresentationViewport(null); }
                finally { hooks.OperationPosted -= Posted; }
            }
        });
}
