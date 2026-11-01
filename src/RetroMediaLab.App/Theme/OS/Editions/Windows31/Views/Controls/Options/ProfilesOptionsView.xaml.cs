using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.App.Views.Controls.Options;

namespace RetroMediaLab.App.Theme.OS.Editions.Windows31.Views;

internal partial class ProfilesOptionsView : UserControl, IDisposable
{
    private readonly OptionsProfilesSection source;
    private readonly ThemeBindingScope bindings = new();
    private bool disposed;

    internal ProfilesOptionsView(OptionsProfilesSection source)
    {
        this.source = source;
        try
        {
            InitializeComponent();
            Choice(source.ReadProfiles, ReadProfiles);
            Choice(source.WriteProfiles, WriteProfiles);
            Choice(source.ConvertProfiles, ConvertProfiles);
            UpdateAvailability();
        }
        catch { Dispose(); throw; }
    }

    private ListBox? ActiveList => Categories?.SelectedContent as ListBox;

    private void Choice(ListBox original, ListBox target)
    {
        bindings.OneWay(original, ItemsControl.ItemsSourceProperty, target, ItemsControl.ItemsSourceProperty);
        bindings.TwoWay(original, Selector.SelectedItemProperty, target, Selector.SelectedItemProperty);
        bindings.OneWay(original, IsEnabledProperty, target, IsEnabledProperty);
    }

    private void SelectionChanged(object sender, SelectionChangedEventArgs args) { if (IsInitialized && !disposed) UpdateAvailability(); }
    private void UpdateAvailability()
    {
        if (RenameAction is null || DeleteAction is null) return;
        RenameAction.IsEnabled = DeleteAction.IsEnabled = ActiveList is { IsEnabled: true, SelectedItem: not null };
    }
    private void RenameClick(object sender, RoutedEventArgs args) { if (!disposed && ActiveList is { } list) source.RequestRename(list); }
    private void DeleteClick(object sender, RoutedEventArgs args) { if (!disposed && ActiveList is { } list) source.RequestDelete(list); }
    private void ProfileKeyDown(object sender, KeyEventArgs args) { if (!disposed) source.RequestKeyDown(sender, args); }
    private void ProfileLeftButtonDown(object sender, MouseButtonEventArgs args) { if (!disposed) source.RequestLeftButtonDown(sender, args); }
    private void ProfileRightButtonDown(object sender, MouseButtonEventArgs args) { if (!disposed) source.RequestRightButtonDown(sender, args); }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { bindings.Dispose(); }
        finally
        {
            if (Categories is not null) Categories.SelectionChanged -= SelectionChanged;
            foreach (var list in new[] { ReadProfiles, WriteProfiles, ConvertProfiles })
                if (list is not null)
                {
                    list.SelectionChanged -= SelectionChanged;
                    list.KeyDown -= ProfileKeyDown;
                    list.PreviewMouseLeftButtonDown -= ProfileLeftButtonDown;
                    list.PreviewMouseRightButtonDown -= ProfileRightButtonDown;
                    list.ItemsSource = null;
                }
            Content = null;
        }
    }
}
