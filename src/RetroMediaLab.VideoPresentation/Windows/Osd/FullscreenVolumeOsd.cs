using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace RetroMediaLab.VideoPresentation.Windows.Osd;

public sealed class FullscreenVolumeOsd : IDisposable
{
    private readonly Border _panel;
    private readonly Popup _popup;
    private readonly TextBlock _value = new()
    {
        Foreground = Brushes.LightSkyBlue,
        FontSize = 16,
        VerticalAlignment = VerticalAlignment.Center,
        FontWeight = FontWeights.SemiBold
    };
    private readonly ProgressBar _level = new()
    {
        Maximum = 100,
        Height = 5,
        Margin = new Thickness(0, 10, 0, 0),
        Foreground = Brushes.LightSkyBlue,
        Background = new SolidColorBrush(Color.FromRgb(38, 56, 78)),
        BorderThickness = new Thickness(0)
    };
    private readonly DispatcherTimer _hideTimer = new(DispatcherPriority.Background)
    {
        Interval = TimeSpan.FromSeconds(2)
    };
    private bool _disposed;

    public FullscreenVolumeOsd(FrameworkElement host, FontFamily iconFont, string glyph, string label)
    {
        var header = new DockPanel();
        var icon = new TextBlock
        {
            Text = glyph,
            FontFamily = iconFont,
            FontSize = 19,
            Foreground = Brushes.LightSkyBlue,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 12, 0)
        };
        header.Children.Add(icon);
        DockPanel.SetDock(_value, Dock.Right);
        header.Children.Add(_value);
        header.Children.Add(new TextBlock
        {
            Text = label,
            Foreground = Brushes.White,
            FontSize = 17,
            VerticalAlignment = VerticalAlignment.Center
        });
        _panel = new Border
        {
            Width = 288,
            Padding = new Thickness(16, 12, 16, 14),
            Background = new SolidColorBrush(Color.FromRgb(15, 29, 49)),
            BorderBrush = Brushes.LightSkyBlue,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Child = new StackPanel { Children = { header, _level } }
        };
        _popup = new Popup
        {
            PlacementTarget = host,
            Placement = PlacementMode.Custom,
            CustomPopupPlacementCallback = (size, target, _) =>
                [new(new Point(Math.Max(0, (target.Width - size.Width) / 2),
                    Math.Max(0, target.Height - size.Height - 25)), PopupPrimaryAxis.Horizontal)],
            AllowsTransparency = true,
            Focusable = false,
            IsHitTestVisible = false,
            Child = _panel
        };
        _hideTimer.Tick += Hide;
    }

    public void Show(int percent)
    {
        if (_disposed) return;
        _value.Text = (percent / 100d).ToString("P0");
        _level.Value = percent;
        _popup.IsOpen = true;
        _hideTimer.Stop();
        _hideTimer.Start();
    }

    public void SetTransparency(int percent)
    {
        if (!_disposed) FullscreenOsdWindow.ApplyTransparency(_panel, percent);
    }

    private void Hide(object? sender, EventArgs args)
    {
        _hideTimer.Stop();
        _popup.IsOpen = false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { Hide(this, EventArgs.Empty); }
        finally
        {
            _hideTimer.Tick -= Hide;
            _popup.Child = null;
            _popup.PlacementTarget = null;
        }
    }
}
