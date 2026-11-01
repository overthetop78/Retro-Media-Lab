using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GWGUI.App.Theme.Common;
using GWGUI.App.Views.Controls.Options;

namespace GWGUI.App.Theme.Orbit.Views;

internal partial class LogsOptionsView : UserControl, IDisposable
{
    private readonly OptionsLogsSection source;
    private readonly ThemeBindingScope bindings = new();
    private bool disposed;
    private OrbitResources? resourceScope;

    internal LogsOptionsView(OptionsLogsSection source, bool? dark = null)
    {
        this.source = source;
        try
        {
            resourceScope = new OrbitResources(this, dark);
            InitializeComponent();
            bindings.OneWay(source.OptionsList, ItemsControl.ItemsSourceProperty, Rows, ItemsControl.ItemsSourceProperty);
            bindings.OneWay(source.DirectoryText, TextBlock.TextProperty, Directory, TextBlock.TextProperty);
        }
        catch { Dispose(); throw; }
    }

    private void RowChanged(object sender, RoutedEventArgs args) { if (!disposed) source.RequestRowChanged(sender, args); }
    private void MaximumSizeEditingFinished(object sender, KeyboardFocusChangedEventArgs args) { if (!disposed) source.RequestMaximumSizeEditingFinished(sender, args); }
    private void NumericTextInput(object sender, TextCompositionEventArgs args) { if (!disposed) source.RequestNumericTextInput(sender, args); }
    private void OpenFolderClick(object sender, RoutedEventArgs args) { if (!disposed) source.RequestOpenLogsFolder(); }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { bindings.Dispose(); }
        finally
        {
            if (Rows is not null) Rows.ItemsSource = null;
            Content = null;
            try { resourceScope?.Dispose(); }
            finally { resourceScope = null; Resources.Clear(); }
        }
    }
}
