using RetroMediaLab.App.Constants.Controls.Visual;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RetroMediaLab.App.Contracts.Emulation.Configurations;
using RetroMediaLab.App.Constants.Emulation.Errors;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Presenters.Common;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.App.Views.Controls.Emulation.Options;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.App.Theme.OS.Editions.WindowsXP.Views;

internal partial class EmulationPreferencesView : UserControl, IDisposable
{
    private readonly EmulationPreferencesSection source;
    private readonly Action close;
    private readonly ThemeBindingScope bindings = new();
    private InputBindingsView? shortcuts;
    private NetworkView? network;
    private bool updating;
    private bool busy;
    private bool disposed;
    private WindowsXPResources? resourceScope;
    private const string DeviceSymbolFontResource = "WindowsXPDeviceSymbols";
    private const string AllMachinesResource = "Visual.All";

    internal EmulationPreferencesView(EmulationPreferencesSection source, Action close, WindowsXPColorScheme scheme = WindowsXPColorScheme.Blue)
    {
        this.source = source;
        this.close = close;
        try
        {
            InitializeComponent();
            resourceScope = new WindowsXPResources(this, scheme);
            Resources[DeviceSymbolFontResource] = ControlVisualConstants.IconFont;
            bindings.TwoWay(source.StorageFolder, TextBox.TextProperty, Storage, TextBox.TextProperty);
            bindings.TwoWay(source.CaptureFolder, TextBox.TextProperty, Capture, TextBox.TextProperty);
            bindings.TwoWay(source.StateFolder, TextBox.TextProperty, State, TextBox.TextProperty);
            shortcuts = new InputBindingsView(source.ShortcutEditor, scheme);
            Shortcuts.Content = shortcuts;
            network = new NetworkView(scheme);
            Network.Content = network;
            source.PresentationChanged += SourceChanged;
            Loaded += ViewLoaded;
            RefreshConfigurations();
        }
        catch { Dispose(); throw; }
    }

    private async void ViewLoaded(object sender, RoutedEventArgs args) => await RunAsync(source.EnsureConfigurationsAsync);
    private void SourceChanged(object? sender, EventArgs args) { if (!disposed) RefreshConfigurations(); }
    private void RefreshConfigurations()
    {
        updating = true;
        try
        {
            var selected = Configurations.SelectedItem as EmulationConfigurationTableRow;
            Brands.Items.Clear();
            foreach (var module in source.ConfigurationRows.Select(row => row.Module).DistinctBy(module => module.Id))
            {
                var choice = new ComboBoxItem { Tag = module };
                choice.SetBinding(ContentControl.ContentProperty, LocExtension.CreateBinding(module, module.DisplayResourceKey));
                Brands.Items.Add(choice);
                if (module.Id == source.SelectedModule?.Id) Brands.SelectedItem = choice;
            }
            Machines.Items.Clear();
            var all = new ComboBoxItem();
            all.SetBinding(ContentControl.ContentProperty, LocExtension.CreateBinding(AllMachinesResource));
            Machines.Items.Add(all);
            Machines.SelectedItem = all;
            if (source.SelectedModule is { } current)
                foreach (var machine in current.Machines.Where(machine => source.ConfigurationRows.Any(row => row.Module.Id == current.Id && row.Configuration.MachineId == machine.Id)))
                {
                    var choice = new ComboBoxItem { Tag = machine.Id };
                    choice.SetBinding(ContentControl.ContentProperty, LocExtension.CreateBinding(current, machine.DisplayResourceKey));
                    Machines.Items.Add(choice);
                    if (machine.Id == source.SelectedMachineId) Machines.SelectedItem = choice;
                }
            Configurations.ItemsSource = source.ConfigurationRows.Where(row => row.Module.Id == source.SelectedModule?.Id
                && (source.SelectedMachineId is null || row.Configuration.MachineId == source.SelectedMachineId)).ToArray();
            Configurations.SelectedItem = Configurations.Items.OfType<EmulationConfigurationTableRow>().FirstOrDefault(row => selected is not null && row.Module.Id == selected.Module.Id && row.Configuration.Id == selected.Configuration.Id);
            if (Configurations.SelectedItem is null && Configurations.Items.Count > 0) Configurations.SelectedIndex = 0;
            UpdateActions();
        }
        finally { updating = false; }
    }

    private void BrandChanged(object sender, SelectionChangedEventArgs args)
    {
        if (!disposed && !updating && Brands.SelectedItem is ComboBoxItem { Tag: IEmulationModule module }) source.SelectModule(module);
    }
    private void MachineChanged(object sender, SelectionChangedEventArgs args)
    {
        if (!disposed && !updating) source.SelectMachine((Machines.SelectedItem as ComboBoxItem)?.Tag as string);
    }
    private void ConfigurationChanged(object sender, SelectionChangedEventArgs args) { if (IsInitialized && !disposed) UpdateActions(); }
    private void UpdateActions() => EditAction.IsEnabled = DeleteAction.IsEnabled = !busy && Configurations.SelectedItem is EmulationConfigurationTableRow;
    private async void FolderFinished(object sender, KeyboardFocusChangedEventArgs args) => await RunAsync(source.RequestSaveFoldersAsync);
    private async void BrowseClick(object sender, RoutedEventArgs args)
    {
        if (sender is not Button { Tag: string name }) return;
        var folder = name == nameof(Storage) ? source.StorageFolder : name == nameof(Capture) ? source.CaptureFolder : source.StateFolder;
        await RunAsync(() => source.RequestBrowseFolderAsync(folder));
    }
    private async void EditClick(object sender, RoutedEventArgs args)
    {
        if (Configurations.SelectedItem is EmulationConfigurationTableRow row) await RunAsync(() => source.RequestEditConfigurationAsync(row));
    }
    private async void DeleteClick(object sender, RoutedEventArgs args)
    {
        if (Configurations.SelectedItem is EmulationConfigurationTableRow row) await RunAsync(() => source.RequestDeleteConfigurationAsync(row));
    }
    private async void ConfigurationDoubleClick(object sender, MouseButtonEventArgs args)
    {
        if (args.ChangedButton == MouseButton.Left && ItemsControl.ContainerFromElement(Configurations, args.OriginalSource as DependencyObject) is ListBoxItem
            && Configurations.SelectedItem is EmulationConfigurationTableRow row)
            await RunAsync(() => source.RequestEditConfigurationAsync(row));
    }
    private void CloseClick(object sender, RoutedEventArgs args) { if (!disposed) close(); }
    private async Task RunAsync(Func<Task> action)
    {
        if (disposed || busy) return;
        busy = true;
        UpdateActions();
        try { await action(); }
        catch (Exception error)
        {
            if (!disposed) ControlErrorPresenter.ShowEmulation(this, error, ControlErrorContexts.EmulationConfigurationManagement, LocExtension.Get("Options.EmulationTitle"));
        }
        finally
        {
            busy = false;
            if (!disposed) UpdateActions();
        }
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        source.PresentationChanged -= SourceChanged;
        Loaded -= ViewLoaded;
        try { shortcuts?.Dispose(); }
        finally
        {
            try { network?.Dispose(); }
            finally
            {
                try { bindings.Dispose(); }
                finally
                {
                    Shortcuts.Content = null;
                    Network.Content = null;
                    Configurations.ItemsSource = null;
                    Brands.Items.Clear();
                    Machines.Items.Clear();
                    Content = null;
                    try { resourceScope?.Dispose(); }
                    finally { resourceScope = null; Resources.Clear(); }
                }
            }
        }
    }
}
