using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using GWGUI.App.Localization.Extensions;

namespace GWGUI.App.Theme.OS.Editions.Windows95.Views;

internal sealed class Windows95PropertyWindow : UserControl, IDisposable
{
    private readonly FrameworkElement document;
    private readonly Action close;
    private readonly Button command = new() { MinWidth = CommandWidth, IsDefault = true, IsCancel = true,
        HorizontalAlignment = HorizontalAlignment.Right, Margin = FooterMargin };
    private readonly DockPanel panel = new();
    private bool disposed;
    private const double CommandWidth = 80;
    private const string CloseResource = "Common.Close";
    private const string ControlsUri = "/gwgui.app;component/Theme/OS/Editions/Windows95/Styles/Windows95Controls.xaml";
    private static readonly Thickness FooterMargin = new(8, 8, 8, 8);

    internal Windows95PropertyWindow(FrameworkElement document, Action close)
    {
        this.document = document;
        this.close = close;
        try
        {
            Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(ControlsUri, UriKind.Relative) });
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
            try { (document as IDisposable)?.Dispose(); }
            finally { Resources.MergedDictionaries.Clear(); }
        }
    }
}
