using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.App.Views.Controls.Common;

namespace RetroMediaLab.App.Theme.OS.Editions.Windows31.Views;

internal partial class ClassificationView : UserControl, IDisposable
{
    private readonly ThemeBindingScope bindings = new();
    private readonly Action close;
    private bool disposed;

    internal ClassificationView(DiskClassificationSelector source, Action close)
    {
        this.close = close;
        try
        {
            InitializeComponent();
            Choice(source.Machine, Machines);
            Choice(source.Format, Formats);
            Choice(source.Protection, Protections);
            bindings.TwoWay(source.Automatic, ToggleButton.IsCheckedProperty, Automatic, ToggleButton.IsCheckedProperty);
            bindings.OneWay(source.Automatic, VisibilityProperty, Automatic, VisibilityProperty);
            bindings.OneWay(source.ProtectionPanel, VisibilityProperty, ProtectionGroup, VisibilityProperty);
        }
        catch { Dispose(); throw; }
    }

    private void Choice(Selector original, Selector target)
    {
        bindings.OneWay(original, ItemsControl.ItemsSourceProperty, target, ItemsControl.ItemsSourceProperty);
        bindings.TwoWay(original, Selector.SelectedItemProperty, target, Selector.SelectedItemProperty);
        bindings.OneWay(original, IsEnabledProperty, target, IsEnabledProperty);
    }

    private void CloseClick(object sender, RoutedEventArgs args) { if (!disposed) close(); }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { bindings.Dispose(); }
        finally
        {
            if (CloseCommand is not null) CloseCommand.Click -= CloseClick;
            if (Machines is not null) Machines.ItemsSource = null;
            if (Formats is not null) Formats.ItemsSource = null;
            if (Protections is not null) Protections.ItemsSource = null;
            Content = null;
        }
    }
}
