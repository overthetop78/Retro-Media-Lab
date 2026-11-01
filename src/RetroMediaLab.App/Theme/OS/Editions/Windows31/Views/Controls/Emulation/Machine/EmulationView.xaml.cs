using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using RetroMediaLab.App.Contracts.Emulation.Configurations;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Views.Controls.Emulation.Machine;

namespace RetroMediaLab.App.Theme.OS.Editions.Windows31.Views;

internal partial class EmulationView : UserControl, IDisposable
{
    private readonly EmulationSection source;
    private readonly Action close;
    private readonly Action<Exception> showError;
    private bool disposed;
    private bool busy;

    internal EmulationView(EmulationSection source, Action close, Action<Exception> showError)
    {
        this.source = source;
        this.close = close;
        this.showError = showError;
        try
        {
            InitializeComponent();
            source.PresentationChanged += PresentationChanged;
            RefreshItems();
        }
        catch { Dispose(); throw; }
    }

    private void PresentationChanged(object? sender, EventArgs args) { if (!disposed) RefreshItems(); }

    private void RefreshItems()
    {
        var selected = (Configurations.SelectedItem as ListBoxItem)?.Tag as EmulationConfigurationListItem;
        Configurations.Items.Clear();
        foreach (var configuration in source.Configurations)
        {
            var item = new ListBoxItem { Tag = configuration, HorizontalContentAlignment = HorizontalAlignment.Stretch };
            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(BrandWidth) });
            row.ColumnDefinitions.Add(new ColumnDefinition());
            var brand = new TextBlock { TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(0, 3, 8, 3) };
            brand.SetBinding(TextBlock.TextProperty, LocExtension.CreateBinding(configuration.Module, configuration.Module.DisplayResourceKey));
            var machine = new TextBlock { Text = configuration.DisplayName, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 3, 0, 3) };
            foreach (var text in new[] { brand, machine })
                text.SetBinding(TextBlock.ForegroundProperty, new Binding(nameof(Control.Foreground)) { Source = item });
            Grid.SetColumn(machine, MachineColumn);
            row.Children.Add(brand);
            row.Children.Add(machine);
            item.Content = row;
            Configurations.Items.Add(item);
            if (selected?.Module.Id == configuration.Module.Id && selected.Configuration.Id == configuration.Configuration.Id)
                Configurations.SelectedItem = item;
        }
        if (Configurations.SelectedIndex < 0 && Configurations.Items.Count > 0) Configurations.SelectedIndex = FirstConfiguration;
        UpdateAvailability();
    }

    private void UpdateAvailability()
    {
        var canOpen = !busy && Configurations.SelectedItem is ListBoxItem;
        OpenCommand.IsEnabled = canOpen;
        OpenButton.IsEnabled = canOpen;
        RefreshCommand.IsEnabled = !busy;
    }

    private void SelectionChanged(object sender, SelectionChangedEventArgs args) { if (IsInitialized && !disposed) UpdateAvailability(); }
    private async void OpenClick(object sender, RoutedEventArgs args) => await OpenSelectedAsync();
    private async void RefreshClick(object sender, RoutedEventArgs args) => await RunAsync(source.ReloadConfigurationsAsync);
    private void CloseClick(object sender, RoutedEventArgs args) { if (!disposed) close(); }

    private async Task OpenSelectedAsync()
    {
        if (Configurations.SelectedItem is ListBoxItem { Tag: EmulationConfigurationListItem selected })
            await RunAsync(() => source.RequestOpenAsync(selected));
    }

    private async void ConfigurationDoubleClick(object sender, MouseButtonEventArgs args)
    {
        if (ItemsControl.ContainerFromElement(Configurations, args.OriginalSource as DependencyObject) is not ListBoxItem) return;
        args.Handled = true;
        await OpenSelectedAsync();
    }

    private async void ConfigurationKeyDown(object sender, KeyEventArgs args)
    {
        if (args.Key != Key.Enter) return;
        args.Handled = true;
        await OpenSelectedAsync();
    }

    private async Task RunAsync(Func<Task> action)
    {
        if (disposed || busy) return;
        busy = true;
        UpdateAvailability();
        try { await action(); }
        catch (Exception error) { if (!disposed) showError(error); }
        finally
        {
            busy = false;
            if (!disposed) UpdateAvailability();
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { source.PresentationChanged -= PresentationChanged; }
        finally
        {
            if (Configurations is not null)
            {
                Configurations.SelectionChanged -= SelectionChanged;
                Configurations.MouseDoubleClick -= ConfigurationDoubleClick;
                Configurations.KeyDown -= ConfigurationKeyDown;
                Configurations.Items.Clear();
            }
            if (OpenCommand is not null) OpenCommand.Click -= OpenClick;
            if (OpenButton is not null) OpenButton.Click -= OpenClick;
            if (RefreshCommand is not null) RefreshCommand.Click -= RefreshClick;
            if (CloseCommand is not null) CloseCommand.Click -= CloseClick;
            Content = null;
        }
    }

    private const double BrandWidth = 160;
    private const int MachineColumn = 1;
    private const int FirstConfiguration = 0;
}
