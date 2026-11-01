using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.App.Views.Windows.Logs;

namespace RetroMediaLab.App.Theme.OS.Editions.WindowsXP.Views;

internal partial class LogHistoryView : UserControl, IDisposable
{
    private readonly LogHistoryWindow source;
    private readonly ThemeBindingScope bindings = new();
    private bool disposed;
    private WindowsXPResources? resourceScope;

    internal LogHistoryView(LogHistoryWindow source, WindowsXPColorScheme scheme = WindowsXPColorScheme.Blue)
    {
        this.source = source;
        try
        {
            InitializeComponent();
            resourceScope = new WindowsXPResources(this, scheme);
            bindings.OneWay(source.PresentationFiles, ItemsControl.ItemsSourceProperty, Files, ItemsControl.ItemsSourceProperty);
            bindings.TwoWay(source.PresentationFiles, Selector.SelectedItemProperty, Files, Selector.SelectedItemProperty);
            bindings.OneWay(source.PresentationContent, TextBox.TextProperty, Output, TextBox.TextProperty);
            bindings.Action(source.PresentationExport, ExportCommand);
            PreviewKeyDown += OnKeyDown;
        }
        catch { Dispose(); throw; }
    }

    private void CloseClick(object sender, RoutedEventArgs args) => source.RequestPresentationClose();
    private void OnKeyDown(object sender, KeyEventArgs args)
    {
        if (args.Key != Key.Escape) return;
        args.Handled = true;
        source.RequestPresentationClose();
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { bindings.Dispose(); }
        finally
        {
            PreviewKeyDown -= OnKeyDown;
            Files.ItemsSource = null;
            try { resourceScope?.Dispose(); }
            finally { resourceScope = null; Content = null; }
            Resources.Clear();
        }
    }
}
