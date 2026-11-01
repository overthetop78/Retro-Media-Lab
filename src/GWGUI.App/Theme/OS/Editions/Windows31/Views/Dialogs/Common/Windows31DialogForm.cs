using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using GWGUI.App.Localization.Extensions;

namespace GWGUI.App.Theme.OS.Editions.Windows31;

/// <summary>Owns the transferred options document and its close command.</summary>
internal sealed class Windows31DialogForm : UserControl, IDisposable
{
    private readonly Windows31Document document;
    private readonly Action close;
    private readonly DockPanel panel = new();
    private readonly ContentControl body = new() { HorizontalContentAlignment = HorizontalAlignment.Stretch,
        VerticalContentAlignment = VerticalAlignment.Stretch };
    internal Button CloseButton { get; } = new() { MinWidth = CloseWidth,
        HorizontalAlignment = HorizontalAlignment.Right, IsDefault = true, Margin = FooterMargin };
    private bool disposed;

    internal Windows31DialogForm(Windows31Document document, Action close)
    {
        this.document = document;
        this.close = close;
        try
        {
            DockPanel.SetDock(CloseButton, Dock.Bottom);
            CloseButton.SetBinding(ContentControl.ContentProperty, LocExtension.CreateBinding(CloseResource));
            CloseButton.Click += CloseClick;
            panel.Children.Add(CloseButton);
            body.Content = document.View;
            panel.Children.Add(body);
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
            CloseButton.Click -= CloseClick;
            BindingOperations.ClearAllBindings(CloseButton);
        }
        finally
        {
            try { body.Content = null; }
            finally
            {
                try { document.Dispose(); }
                finally { panel.Children.Clear(); Content = null; }
            }
        }
    }

    private const double CloseWidth = 90;
    private const string CloseResource = "Common.Close";
    private static readonly Thickness FooterMargin = new(12, 6, 12, 10);
}
