using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using RetroMediaLab.App.Localization.Extensions;

namespace RetroMediaLab.App.Theme.OS.Editions.WindowsXP.Views;

/// <summary>Frames a borrowed presentation; its caller retains responsibility for its lifetime.</summary>
internal sealed class WindowsXPPropertyWindow : UserControl, IDisposable
{
    private readonly Action close;
    private readonly DockPanel panel = new();
    private readonly Button command = new()
    {
        MinWidth = CommandWidth,
        IsDefault = true,
        IsCancel = true,
        HorizontalAlignment = HorizontalAlignment.Right,
        Margin = FooterMargin
    };
    private WindowsXPResources? resourceScope;
    private bool disposed;
    private const double CommandWidth = 85;
    private const string CloseResource = "Common.Close";
    private static readonly Thickness FooterMargin = new(12);

    internal WindowsXPPropertyWindow(FrameworkElement document, Action close, WindowsXPColorScheme scheme = WindowsXPColorScheme.Blue)
    {
        this.close = close;
        try
        {
            resourceScope = new WindowsXPResources(this, scheme);
            SetResourceReference(BackgroundProperty, WindowsXPResources.FaceResource);
            command.SetBinding(ContentControl.ContentProperty, LocExtension.CreateBinding(CloseResource));
            command.Click += CloseClick;
            DockPanel.SetDock(command, Dock.Bottom);
            panel.Children.Add(command);
            panel.Children.Add(document);
            Content = panel;
        }
        catch { Dispose(); throw; }
    }

    private void CloseClick(object sender, RoutedEventArgs args) { if (!disposed) close(); }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try
        {
            command.Click -= CloseClick;
            BindingOperations.ClearAllBindings(command);
            panel.Children.Clear();
            Content = null;
        }
        finally
        {
            try { resourceScope?.Dispose(); }
            finally { resourceScope = null; Resources.Clear(); }
        }
    }
}
