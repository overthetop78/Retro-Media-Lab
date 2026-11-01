using System.Windows;
using System.Windows.Controls;
using GWGUI.App.Theme.Common;
using GWGUI.App.Views.Controls.Emulation.Storage;

namespace GWGUI.App.Theme.OS.Editions.WindowsXP.Views;

internal partial class ModuleStorageView : UserControl, IDisposable
{
    private readonly EmulationStorageDeviceList source;
    private readonly ThemeBindingScope bindings = new();
    private bool disposed;
    private WindowsXPResources? resourceScope;

    internal ModuleStorageView(EmulationStorageDeviceList source, WindowsXPColorScheme scheme = WindowsXPColorScheme.Blue)
    {
        this.source = source;
        try
        {
            InitializeComponent();
            resourceScope = new WindowsXPResources(this, scheme);
            bindings.OneWay(source, IsEnabledProperty, this, IsEnabledProperty);
            bindings.OneWay(source, VisibilityProperty, this, VisibilityProperty);
            bindings.Action(source.PresentationAdd, Add);
            source.PresentationChanged += SourceChanged;
            Rebuild();
        }
        catch { Dispose(); throw; }
    }
    private void SourceChanged(object? sender, EventArgs args) { if (!disposed) Rebuild(); }
    private void Rebuild()
    {
        var selected = (Devices.SelectedItem as EmulationStorageDeviceList.PresentationRow)?.Device.Slot;
        Devices.ItemsSource = source.PresentationRows;
        Devices.SelectedItem = source.PresentationRows.FirstOrDefault(row => row.Device.Slot == selected) ?? source.PresentationRows.FirstOrDefault();
        UpdateActions();
    }
    private void SelectionChanged(object sender, SelectionChangedEventArgs args) { if (!disposed) UpdateActions(); }
    private void UpdateActions()
    {
        var device = (Devices.SelectedItem as EmulationStorageDeviceList.PresentationRow)?.Device;
        Configure.IsEnabled = device?.CanConfigure == true;
        Remove.IsEnabled = device?.CanRemove == true;
    }
    private void ConfigureClicked(object sender, RoutedEventArgs args)
    {
        if (!disposed && Devices.SelectedItem is EmulationStorageDeviceList.PresentationRow row) source.RequestPresentationConfigure(row.Device);
    }
    private void RemoveClicked(object sender, RoutedEventArgs args)
    {
        if (!disposed && Devices.SelectedItem is EmulationStorageDeviceList.PresentationRow row) source.RequestPresentationRemove(row.Device);
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        source.PresentationChanged -= SourceChanged;
        try { bindings.Dispose(); }
        finally
        {
            if (Devices is not null) Devices.SelectionChanged -= SelectionChanged;
            if (Configure is not null) Configure.Click -= ConfigureClicked;
            if (Remove is not null) Remove.Click -= RemoveClicked;
            if (Devices is not null) Devices.ItemsSource = null;
            Content = null;
            try { resourceScope?.Dispose(); }
            finally { resourceScope = null; Resources.Clear(); }
        }
    }
}
