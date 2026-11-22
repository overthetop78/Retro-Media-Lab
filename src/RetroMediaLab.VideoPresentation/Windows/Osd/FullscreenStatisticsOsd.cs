using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace RetroMediaLab.VideoPresentation.Windows.Osd;

public sealed class FullscreenStatisticsOsd : IDisposable
{
    public sealed record Metric(string Glyph, TextBlock Source, Color Color);

    private const double ScreenInset = 12;
    private readonly Popup _popup;
    private bool _disposed;

    public FullscreenStatisticsOsd(FrameworkElement host, FontFamily iconFont, IReadOnlyList<Metric> metrics)
    {
        var rows = new StackPanel { MinWidth = 124 };
        foreach (var metric in metrics)
        {
            var foreground = new SolidColorBrush(metric.Color);
            foreground.Freeze();
            var row = new DockPanel { Height = 26 };
            var icon = new TextBlock
            {
                Text = metric.Glyph, FontFamily = iconFont, Width = 23,
                Foreground = foreground, VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(icon, Dock.Left);
            row.Children.Add(icon);
            var value = new TextBlock
            {
                Foreground = foreground, FontSize = 14, VerticalAlignment = VerticalAlignment.Center
            };
            value.SetBinding(TextBlock.TextProperty, new Binding(nameof(TextBlock.Text))
            {
                Source = metric.Source, Mode = BindingMode.OneWay
            });
            row.Children.Add(value);
            rows.Children.Add(row);
        }

        _popup = new Popup
        {
            PlacementTarget = host,
            Placement = PlacementMode.Custom,
            CustomPopupPlacementCallback = (size, target, _) =>
                [new(new Point(Math.Max(0, target.Width - size.Width - ScreenInset), ScreenInset), PopupPrimaryAxis.Horizontal)],
            AllowsTransparency = true,
            Focusable = false,
            IsHitTestVisible = false,
            Child = new Border
            {
                Padding = new Thickness(9, 5, 11, 5),
                Background = new SolidColorBrush(Color.FromArgb(226, 33, 37, 43)),
                CornerRadius = new CornerRadius(5),
                IsHitTestVisible = false,
                Child = rows
            }
        };
    }

    public void SetVisible(bool visible)
    {
        if (!_disposed) _popup.IsOpen = visible;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _popup.IsOpen = false; }
        finally
        {
            _popup.Child = null;
            _popup.PlacementTarget = null;
        }
    }
}
