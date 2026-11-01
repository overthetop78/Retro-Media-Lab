using RetroMediaLab.App.Functions.Views.Visualization;
using RetroMediaLab.App.Constants.Rendering.Sequential;
using RetroMediaLab.App.Contracts.Rendering.Sequential;
using RetroMediaLab.App.Contracts.Views.Visualization;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Rendering.Sequential;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace RetroMediaLab.App.Views.Controls.Visualization;

public partial class SequentialMediaView : UserControl, IMediaVisualizationView
{
    private readonly SkiaSequentialMediaRenderer _renderer = new();
    private SequentialMediaRenderModel? _model;
    private SequentialMediaSegment? _selectedSegment;
    private int _preparedSegmentCount;
    private float _zoom = 1;
    private int _renderPixelWidth = 1;
    private int _renderPixelHeight = 1;
    private Point? _panDragLast;
    private ScrollViewer? presentationViewport;
    private DispatcherOperation? pendingZoomPlacement;

    internal ScrollViewer ActiveViewport => presentationViewport ?? TimelineScrollViewer;

    public SequentialMediaView()
    {
        InitializeComponent();
        FaceSelector.DisplayMemberPath = "Value";
        TrackSelector.DisplayMemberPath = "Value";
        ChannelSelector.DisplayMemberPath = "Value";
        SizeChanged += (_, _) => UpdateCanvasWidth();
        Unloaded += (_, _) => CancelZoomPlacement();
    }

    public event Action<int, long>? ElementSelected;
    public event Action<SequentialMediaSegment?>? SegmentSelected;

    internal void SetPresentationViewport(ScrollViewer? viewport)
    {
        if (ReferenceEquals(presentationViewport, viewport)) return;
        CancelZoomPlacement();
        if (_panDragLast is not null)
        {
            _panDragLast = null;
            if (Canvas.IsMouseCaptured) Canvas.ReleaseMouseCapture();
        }
        if (presentationViewport is not null)
        {
            presentationViewport.SizeChanged -= TimelineScrollViewer_SizeChanged;
            presentationViewport.PreviewMouseWheel -= TimelineScrollViewer_PreviewMouseWheel;
        }
        presentationViewport = viewport;
        if (presentationViewport is not null)
        {
            presentationViewport.SizeChanged += TimelineScrollViewer_SizeChanged;
            presentationViewport.PreviewMouseWheel += TimelineScrollViewer_PreviewMouseWheel;
        }
        UpdateCanvasWidth();
    }

    private void CancelZoomPlacement()
    {
        pendingZoomPlacement?.Abort();
        pendingZoomPlacement = null;
    }

    public void SetDocument(SequentialMediaRenderModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        _model = model;
        _selectedSegment = null;
        _preparedSegmentCount = 0;
        SetChoices(FaceSelector, model.Segments.Where(item => item.FaceNumber.HasValue).Select(item => item.FaceNumber!.Value), "Visual.DiscFace");
        SetChoices(TrackSelector, model.Segments.Where(item => item.TrackNumber.HasValue).Select(item => item.TrackNumber!.Value), "Visual.TrackNumber");
        SetChoices(ChannelSelector, model.Segments.Where(item => item.ChannelNumber.HasValue).Select(item => item.ChannelNumber!.Value), "Visual.Channel");
        SetFilterVisibility(FaceLabel, FaceSelector, model.Segments.Any(item => item.FaceNumber.HasValue));
        SetFilterVisibility(TrackLabel, TrackSelector, model.Segments.Any(item => item.TrackNumber.HasValue));
        SetFilterVisibility(ChannelLabel, ChannelSelector, model.Segments.Any(item => item.ChannelNumber.HasValue));
        UpdateSelectionLabel();
        UpdateCanvasWidth();
    }

    internal void RevealThrough(int segmentCount)
    {
        _preparedSegmentCount = Math.Clamp(segmentCount, 0, _model?.Segments.Count ?? 0);
        Canvas.InvalidateVisual();
    }

    public void SelectElement(int surface, long position)
    {
        _selectedSegment = _model?.Segments.FirstOrDefault(segment => segment.Lane == surface && segment.Position == position);
        UpdateSelectionLabel();
        Canvas.InvalidateVisual();
    }

    private void Canvas_PaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        _renderPixelWidth = Math.Max(1, e.Info.Width);
        _renderPixelHeight = Math.Max(1, e.Info.Height);
        var filtered = FilteredModel();
        _renderer.Render(e.Surface.Canvas, filtered, _selectedSegment, Math.Min(_preparedSegmentCount, filtered?.Segments.Count ?? 0), SequentialMediaRenderConstants.DefaultWrappedLineCount, e.Info.Width, e.Info.Height, _zoom);
    }

    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => SelectAt(e.GetPosition(Canvas));

    private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        => VisualizationPanFunctions.Begin(Canvas, ActiveViewport, _zoom, ref _panDragLast, e);

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (_panDragLast is not Point last || e.MiddleButton != MouseButtonState.Pressed) return;
        var point = e.GetPosition(ActiveViewport);
        ActiveViewport.ScrollToHorizontalOffset(ActiveViewport.HorizontalOffset - (point.X - last.X));
        ActiveViewport.ScrollToVerticalOffset(ActiveViewport.VerticalOffset - (point.Y - last.Y));
        _panDragLast = point;
    }

    private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        => VisualizationPanFunctions.End(Canvas, ref _panDragLast, e);

    private void SelectAt(Point pointer)
    {
        var filtered = FilteredModel();
        var scaleX = _renderPixelWidth / Math.Max(1, Canvas.ActualWidth);
        var scaleY = _renderPixelHeight / Math.Max(1, Canvas.ActualHeight);
        var segment = _renderer.HitTest(filtered, SequentialMediaRenderConstants.DefaultWrappedLineCount, _renderPixelWidth, _renderPixelHeight, new SKPoint((float)(pointer.X * scaleX), (float)(pointer.Y * scaleY)), _zoom);
        if (segment is null) return;
        _selectedSegment = segment;
        UpdateSelectionLabel();
        Canvas.InvalidateVisual();
        ElementSelected?.Invoke(segment.Lane, segment.Position);
        SegmentSelected?.Invoke(segment);
    }

    private void FilterSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedSegment = null;
        UpdateSelectionLabel();
        Canvas.InvalidateVisual();
        SegmentSelected?.Invoke(null);
    }

    private void ZoomOutButton_Click(object sender, RoutedEventArgs e) => SetZoom(_zoom * .8f);
    private void ZoomInButton_Click(object sender, RoutedEventArgs e) => SetZoom(_zoom * 1.25f);
    private void ResetZoomButton_Click(object sender, RoutedEventArgs e) => SetZoom(1);

    private void TimelineScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!ReferenceEquals(sender, ActiveViewport)) return;
        SetZoom(_zoom * (e.Delta > 0 ? 1.15f : 1 / 1.15f), e.GetPosition(ActiveViewport));
        e.Handled = true;
    }

    private void TimelineScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (ReferenceEquals(sender, ActiveViewport)) UpdateCanvasWidth();
    }

    private void SetZoom(float zoom, Point? anchor = null)
    {
        CancelZoomPlacement();
        var viewport = ActiveViewport;
        var point = anchor ?? new Point(ActiveViewport.ViewportWidth / 2, ActiveViewport.ViewportHeight / 2);
        var oldWidth = Math.Max(1, Canvas.ActualWidth);
        var oldHeight = Math.Max(1, Canvas.ActualHeight);
        var relativeX = (ActiveViewport.HorizontalOffset + point.X) / oldWidth;
        var relativeY = (ActiveViewport.VerticalOffset + point.Y) / oldHeight;
        _zoom = Math.Clamp(zoom, 1, 8);
        ResetZoomButton.Content = $"{_zoom:P0}";
        UpdateCanvasWidth();
        pendingZoomPlacement = Dispatcher.BeginInvoke(() =>
        {
            pendingZoomPlacement = null;
            if (!ReferenceEquals(viewport, ActiveViewport)) return;
            viewport.ScrollToHorizontalOffset(relativeX * Canvas.ActualWidth - point.X);
            viewport.ScrollToVerticalOffset(relativeY * Canvas.ActualHeight - point.Y);
        }, DispatcherPriority.Loaded);
    }

    private void UpdateCanvasWidth()
    {
        var viewportWidth = Math.Max(ActiveViewport.ViewportWidth, ActiveViewport.ActualWidth - 2);
        Canvas.Width = Math.Max(640, viewportWidth) * _zoom;
        var viewportHeight = Math.Max(ActiveViewport.ViewportHeight, ActiveViewport.ActualHeight - 2);
        Canvas.Height = Math.Max(360, viewportHeight) * _zoom;
        Canvas.InvalidateVisual();
    }

    private SequentialMediaRenderModel? FilteredModel()
    {
        if (_model is null) return null;
        var face = SelectedValue(FaceSelector);
        var track = SelectedValue(TrackSelector);
        var channel = SelectedValue(ChannelSelector);
        var segments = _model.Segments.Where(segment => (face is null || segment.FaceNumber == face) && (track is null || segment.TrackNumber == track) && (channel is null || segment.ChannelNumber == channel)).ToArray();
        return _model with { Segments = segments };
    }

    private static int? SelectedValue(ComboBox selector) => selector.SelectedItem is KeyValuePair<int?, string> choice ? choice.Key : null;

    private static void SetChoices(ComboBox selector, IEnumerable<int> values, string resourceKey)
    {
        var choices = new List<KeyValuePair<int?, string>> { new(null, LocExtension.Get("Visual.All")) };
        choices.AddRange(values.Distinct().Order().Select(value => new KeyValuePair<int?, string>(value, LocExtension.Get(resourceKey, value))));
        selector.ItemsSource = choices;
        selector.SelectedIndex = 0;
    }

    private static void SetFilterVisibility(TextBlock label, ComboBox selector, bool visible)
    {
        var visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        label.Visibility = visibility;
        selector.Visibility = visibility;
    }

    private void UpdateSelectionLabel()
    {
        if (_selectedSegment is not null)
        {
            SelectionLabel.Text = $"{LocExtension.Get("Visual.StartLabel")} {_selectedSegment.Start:g}  ·  {LocExtension.Get("Visual.DurationLabel")} {_selectedSegment.Duration:g}";
            return;
        }

        if (_model is null)
        {
            SelectionLabel.Text = string.Empty;
            return;
        }

        var duration = _model.Duration is { } value ? $"  ·  {LocExtension.Get("Visual.DurationLabel")} {value:g}" : string.Empty;
        SelectionLabel.Text = $"{LocExtension.Get("Explorer.Cassette")}{duration}  ·  {LocExtension.Get("Visual.SegmentCountLabel")} {_model.Segments.Count}";
    }
}
