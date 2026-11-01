using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RetroMediaLab.App.Theme.Orbit.Views;

internal partial class OrbitCommandPalette : UserControl, IDisposable
{
    private readonly OrbitCommandCatalog catalog;
    private OrbitResources? resources;
    private bool disposed;
    internal event EventHandler? CloseRequested;
    internal event EventHandler? CommandExecuted;

    internal OrbitCommandPalette(OrbitCommandCatalog catalog, bool? dark = null)
    {
        this.catalog = catalog;
        try
        {
            // XAML references named Orbit styles while it is loaded.
            resources = new OrbitResources(this, dark);
            InitializeComponent();
            catalog.Changed += CatalogChanged;
            PreviewKeyDown += PreviewKeys;
            Results.SelectionChanged += SelectionChanged;
            Refresh();
        }
        catch { Dispose(); throw; }
    }

    private void CatalogChanged(object? sender, EventArgs args) { if (!disposed) Refresh(); }
    private void SearchChanged(object sender, TextChangedEventArgs args) { if (IsInitialized && !disposed) Refresh(); }
    private void Refresh()
    {
        var previous = (Results.SelectedItem as OrbitCommand)?.Source;
        var search = Search.Text.Trim();
        var commands = catalog.Commands.Where(command => search.Length == 0
            || command.Title.Contains(search, StringComparison.CurrentCultureIgnoreCase)
            || command.Path.Contains(search, StringComparison.CurrentCultureIgnoreCase)).ToArray();
        Results.ItemsSource = commands;
        Results.SelectedItem = commands.FirstOrDefault(command => ReferenceEquals(command.Source, previous))
            ?? commands.FirstOrDefault(command => command.IsEnabled);
        Empty.Visibility = commands.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        SelectionChanged(null, null!);
    }
    private void SelectionChanged(object? sender, SelectionChangedEventArgs args) => Execute.IsEnabled = Results.SelectedItem is OrbitCommand { IsEnabled: true };
    private void ExecuteSelected()
    {
        if (!disposed && Results.SelectedItem is OrbitCommand command && command.Execute() && !disposed)
            CommandExecuted?.Invoke(this, EventArgs.Empty);
    }
    private void ExecuteClick(object sender, RoutedEventArgs args) => ExecuteSelected();
    private void ResultDoubleClick(object sender, MouseButtonEventArgs args) { if (args.ChangedButton == MouseButton.Left) ExecuteSelected(); }
    private void ResultKeyDown(object sender, KeyEventArgs args) { if (args.Key == Key.Enter) { args.Handled = true; ExecuteSelected(); } }
    private void CloseClick(object sender, RoutedEventArgs args) => CloseRequested?.Invoke(this, EventArgs.Empty);
    private void PreviewKeys(object sender, KeyEventArgs args)
    {
        if (args.Key == Key.Escape) { args.Handled = true; CloseRequested?.Invoke(this, EventArgs.Empty); }
        else if (args.Key == Key.Down && ReferenceEquals(args.OriginalSource, Search))
        {
            args.Handled = true;
            Results.Focus();
            if (Results.SelectedItem is { } selected) (Results.ItemContainerGenerator.ContainerFromItem(selected) as ListBoxItem)?.Focus();
        }
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { catalog.Changed -= CatalogChanged; }
        finally
        {
            PreviewKeyDown -= PreviewKeys;
            if (Search is not null) Search.TextChanged -= SearchChanged;
            if (Results is not null)
            {
                Results.SelectionChanged -= SelectionChanged;
                Results.KeyDown -= ResultKeyDown;
                Results.MouseDoubleClick -= ResultDoubleClick;
                Results.ItemsSource = null;
            }
            if (CloseCommand is not null) CloseCommand.Click -= CloseClick;
            if (Execute is not null) Execute.Click -= ExecuteClick;
            CloseRequested = null;
            CommandExecuted = null;
            Content = null;
            resources?.Dispose();
            resources = null;
        }
    }
}
