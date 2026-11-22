using System.Windows;
using System.Windows.Controls.Primitives;

namespace RetroMediaLab.VideoPresentation.Windows.Osd;

public sealed class FullscreenOsdIndicator : IDisposable
{
    private readonly Popup _popup;
    private bool _disposed;

    public FullscreenOsdIndicator(FrameworkElement host, string label)
    {
        var badge = FullscreenOsdWindow.CreateShaderLoadingBadge(label);
        badge.Visibility = Visibility.Visible;
        _popup = new Popup
        {
            PlacementTarget = host,
            Placement = PlacementMode.Center,
            AllowsTransparency = true,
            Focusable = false,
            IsHitTestVisible = false,
            Child = badge
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
