using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.App.Views.Controls.Options;

namespace RetroMediaLab.App.Theme.OS.Editions.Windows31.Views;

internal partial class UpdatesView : UserControl, IDisposable
{
    private readonly OptionsUpdatesSection source;
    private readonly Action close;
    private readonly ThemeBindingScope bindings = new();
    private bool disposed;

    internal UpdatesView(OptionsUpdatesSection source, Button footer, TextBlock hint, Action close)
    {
        this.source = source;
        this.close = close;
        try
        {
            InitializeComponent();
            bindings.OneWay(source.NavigationControl, ItemsControl.ItemsSourceProperty, Navigation, ItemsControl.ItemsSourceProperty);
            bindings.TwoWay(source.NavigationControl, Selector.SelectedItemProperty, Navigation, Selector.SelectedItemProperty);
            foreach (var pair in new (FrameworkElement Original, FrameworkElement Copy)[]
            {
                (source.PresentationApplicationPage, ApplicationPage), (source.PresentationDirectoryPage, DirectoryPage),
                (source.PresentationModulePage, ModulePage), (source.PresentationAdvancedPage, AdvancedPage)
            }) bindings.OneWay(pair.Original, VisibilityProperty, pair.Copy, VisibilityProperty);
            foreach (var pair in new (Button Original, Button Copy)[]
            {
                (source.SearchButton, Search), (source.InstallButton, Install), (source.CancelButton, Cancel),
                (source.SearchModulesButton, SearchModules), (source.InstallModulesButton, InstallModules), (source.CancelModulesButton, CancelModules),
                (source.SearchAvailableModulesButton, SearchAvailable), (source.InstallModuleFileButton, InstallFile),
                (source.InstallModuleUrlButton, InstallUrl), (footer, FooterCommand)
            }) bindings.Action(pair.Original, pair.Copy, followCaption: true);
            foreach (var pair in new (TextBlock Original, TextBlock Copy)[]
            {
                (source.Status, Status), (source.ModuleStatus, ModuleStatus), (source.AvailableModuleStatus, AvailableStatus),
                (source.ManualInstallationStatus, ManualStatus), (hint, FooterHint)
            })
            {
                bindings.OneWay(pair.Original, TextBlock.TextProperty, pair.Copy, TextBlock.TextProperty);
                bindings.OneWay(pair.Original, VisibilityProperty, pair.Copy, VisibilityProperty);
            }
            foreach (var pair in new (ProgressBar Original, ProgressBar Copy)[]
            {
                (source.Progress, Progress), (source.ModulesProgress, ModulesProgress),
                (source.AvailableModulesProgress, AvailableProgress), (source.ManualInstallationProgress, ManualProgress)
            })
            {
                bindings.OneWay(pair.Original, VisibilityProperty, pair.Copy, VisibilityProperty);
                bindings.OneWay(pair.Original, ProgressBar.ValueProperty, pair.Copy, ProgressBar.ValueProperty);
                bindings.OneWay(pair.Original, ProgressBar.IsIndeterminateProperty, pair.Copy, ProgressBar.IsIndeterminateProperty);
                bindings.OneWay(pair.Original, ProgressBar.MinimumProperty, pair.Copy, ProgressBar.MinimumProperty);
                bindings.OneWay(pair.Original, ProgressBar.MaximumProperty, pair.Copy, ProgressBar.MaximumProperty);
            }
            foreach (var pair in new (ItemsControl Original, ItemsControl Copy)[]
            {
                (source.Results, Results), (source.ModuleResults, ModuleResults), (source.AvailableModuleResults, AvailableResults)
            }) bindings.OneWay(pair.Original, ItemsControl.ItemsSourceProperty, pair.Copy, ItemsControl.ItemsSourceProperty);
            bindings.TwoWay(source.ModuleCatalogUrl, TextBox.TextProperty, CatalogUrl, TextBox.TextProperty);
            bindings.OneWay(source.ModuleCatalogUrl, IsEnabledProperty, CatalogUrl, IsEnabledProperty);
            PreviewKeyDown += OnKeyDown;
        }
        catch { Dispose(); throw; }
    }

    private void VersionChanged(object sender, SelectionChangedEventArgs args)
    {
        if (!disposed) source.PresentationVersionChanged(sender, args);
    }
    private void InstallAvailable(object sender, RoutedEventArgs args)
    {
        if (!disposed) source.PresentationInstallAvailable(sender, args);
    }
    private void OnKeyDown(object sender, KeyEventArgs args)
    {
        if (args.Key != Key.Escape) return;
        args.Handled = true;
        close();
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { bindings.Dispose(); }
        finally
        {
            PreviewKeyDown -= OnKeyDown;
            Navigation.ItemsSource = null;
            Results.ItemsSource = null;
            ModuleResults.ItemsSource = null;
            AvailableResults.ItemsSource = null;
            Content = null;
        }
    }
}
