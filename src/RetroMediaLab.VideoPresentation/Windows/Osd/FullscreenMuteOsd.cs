using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace RetroMediaLab.VideoPresentation.Windows.Osd;

public sealed class FullscreenMuteOsd : IDisposable
{
    private const double Inset = 44;
    private readonly Popup _popup;
    private bool _disposed;

    public FullscreenMuteOsd(FrameworkElement host, FontFamily iconFont, string glyph, string label)
    {
        var icon = new TextBlock
        {
            Text = glyph,
            FontFamily = iconFont,
            FontSize = 44,
            Foreground = Brushes.White,
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 5,
                ShadowDepth = 1,
                Opacity = .8
            }
        };
        AutomationProperties.SetName(icon, label);
        _popup = new Popup
        {
            PlacementTarget = host,
            Placement = PlacementMode.Custom,
            CustomPopupPlacementCallback = (size, target, _) =>
                [new(new Point(Math.Max(0, target.Width - size.Width - Inset),
                    Math.Max(0, target.Height - size.Height - Inset)), PopupPrimaryAxis.Horizontal)],
            AllowsTransparency = true,
            Focusable = false,
            IsHitTestVisible = false,
            Child = icon
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
