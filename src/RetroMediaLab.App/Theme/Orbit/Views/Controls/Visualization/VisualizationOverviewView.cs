using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using RetroMediaLab.App.ViewModels.Visualization;
using RetroMediaLab.App.Views.Controls.Visualization;

namespace RetroMediaLab.App.Theme.Orbit.Views;

internal sealed class VisualizationOverviewView : ScrollViewer, IDisposable
{
    private readonly VisualizerTrackOverview source;
    private readonly StackPanel rows = new();
    private readonly List<ItemsControl> lists = [];
    private bool disposed;

    internal VisualizationOverviewView(VisualizerTrackOverview source)
    {
        this.source = source;
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        SetResourceReference(StyleProperty, typeof(ScrollViewer));
        Content = rows;
        try
        {
            source.RowsChanged += RowsChanged;
            Rebuild();
        }
        catch { Dispose(); throw; }
    }

    private void RowsChanged(object? sender, EventArgs args) => Rebuild();

    private void Rebuild()
    {
        ClearRows();
        foreach (var strip in source.Rows.Values)
        {
            var label = new TextBlock { Margin = new Thickness(4) };
            label.SetBinding(TextBlock.TextProperty, new Binding(nameof(TextBlock.Text)) { Source = strip.ProgressLabel });
            rows.Children.Add(label);
            var list = new ItemsControl { ItemsSource = strip.Segments, Margin = new Thickness(4, 0, 4, 6) };
            var panel = new FrameworkElementFactory(typeof(WrapPanel));
            list.ItemsPanel = new ItemsPanelTemplate(panel);
            var button = new FrameworkElementFactory(typeof(Button));
            button.SetResourceReference(FrameworkElement.StyleProperty, OrbitVisualizationMetrics.OverviewButtonStyle);
            button.SetValue(FrameworkElement.MinWidthProperty, CellWidth);
            button.SetValue(FrameworkElement.MarginProperty, new Thickness(1));
            button.SetValue(Control.PaddingProperty, new Thickness(4, 2, 4, 2));
            button.SetBinding(ButtonBase.CommandParameterProperty, new Binding());
            button.SetBinding(ContentControl.ContentProperty, new Binding(nameof(TrackSegment.Position)) { ConverterCulture = CultureInfo.CurrentCulture });
            button.SetBinding(Control.BackgroundProperty, new Binding(nameof(TrackSegment.Brush)));
            button.SetBinding(Control.BorderThicknessProperty, new Binding(nameof(TrackSegment.IsSelected)) { Converter = SelectedBorderConverter.Instance });
            list.ItemTemplate = new DataTemplate { VisualTree = button };
            list.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(SegmentClicked));
            lists.Add(list);
            rows.Children.Add(list);
        }
    }

    private void SegmentClicked(object sender, RoutedEventArgs args)
    {
        if (disposed || args.Source is not Button { CommandParameter: TrackSegment segment }) return;
        source.RequestSelection(segment.Surface, segment.Position);
        args.Handled = true;
    }

    private void ClearRows()
    {
        foreach (var list in lists)
        {
            list.RemoveHandler(ButtonBase.ClickEvent, new RoutedEventHandler(SegmentClicked));
            list.ItemsSource = null;
            list.ItemTemplate = null;
        }
        foreach (var label in rows.Children.OfType<TextBlock>()) BindingOperations.ClearAllBindings(label);
        lists.Clear();
        rows.Children.Clear();
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { source.RowsChanged -= RowsChanged; }
        finally
        {
            try { ClearRows(); }
            finally { Content = null; }
        }
    }

    private const double CellWidth = 30;

    private sealed class SelectedBorderConverter : IValueConverter
    {
        internal static SelectedBorderConverter Instance { get; } = new();
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => new Thickness(value is true ? 2 : 1);
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
