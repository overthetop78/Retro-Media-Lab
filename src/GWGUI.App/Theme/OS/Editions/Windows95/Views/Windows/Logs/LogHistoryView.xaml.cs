using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using GWGUI.App.Theme.Common;
using GWGUI.App.Views.Windows.Logs;

namespace GWGUI.App.Theme.OS.Editions.Windows95.Views;

internal partial class LogHistoryView : UserControl, IDisposable
{
    private readonly LogHistoryWindow source;
    private readonly ThemeBindingScope bindings = new();
    private bool disposed;

    internal LogHistoryView(LogHistoryWindow source)
    {
        this.source = source;
        try
        {
            InitializeComponent();
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
            Content = null;
            Resources.Clear();
        }
    }
}
