using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.App.ViewModels.Input;
using RetroMediaLab.App.Views.Controls.Emulation.Input;

namespace RetroMediaLab.App.Theme.OS.Editions.WindowsXP.Views;

internal partial class InputBindingsView : UserControl, IDisposable
{
    private readonly InputBindingEditor source;
    private readonly ThemeBindingScope bindings = new();
    private IDisposable? capture;
    private bool disposed;
    private WindowsXPResources? resourceScope;

    internal InputBindingsView(InputBindingEditor source, WindowsXPColorScheme scheme = WindowsXPColorScheme.Blue)
    {
        this.source = source;
        try
        {
            InitializeComponent();
            resourceScope = new WindowsXPResources(this, scheme);
            Rows.ItemsSource = source.Rows;
            bindings.OneWay(source.PresentationTargetHeader, TextBlock.TextProperty, TargetHeader, TextBlock.TextProperty);
            bindings.OneWay(source, VisibilityProperty, this, VisibilityProperty);
            bindings.OneWay(source, IsEnabledProperty, this, IsEnabledProperty);
            bindings.TwoWay(source.PresentationSearch, TextBox.TextProperty, Search, TextBox.TextProperty);
            bindings.TwoWay(source.PresentationRows, Selector.SelectedItemProperty, Rows, Selector.SelectedItemProperty);
            capture = source.AttachPresentation(this, () => new TextBlock { Text = LocExtension.Get("Emulation.Input.Press") });
        }
        catch { Dispose(); throw; }
    }

    internal void RequestCapture(string commandId)
    {
        if (!disposed) source.SelectAndStartCapture(commandId, AssignAction);
    }

    private void Assign(object sender, RoutedEventArgs args)
    {
        if (!disposed && Rows.SelectedItem is InputBindingRow row) RequestCapture(row.Id);
    }
    private void Clear(object sender, RoutedEventArgs args)
    {
        if (!disposed && Rows.SelectedItem is InputBindingRow row) source.RequestClear(row);
    }
    private void RestoreDefaults(object sender, RoutedEventArgs args) { if (!disposed) source.RequestRestoreDefaults(); }
    private void ClearConflicts(object sender, RoutedEventArgs args) { if (!disposed) source.RequestClearConflicts(); }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { capture?.Dispose(); }
        finally
        {
            capture = null;
            try { bindings.Dispose(); }
            finally
            {
                if (Rows is not null) Rows.ItemsSource = null;
                try { resourceScope?.Dispose(); }
                finally { resourceScope = null; Content = null; Resources.Clear(); }
            }
        }
    }
}
