using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.App.ViewModels.Operations;
using RetroMediaLab.App.Views.Controls.Read;
using RetroMediaLab.App.Views.Controls.Common;
using RetroMediaLab.App.Views.Controls.Shell;

namespace RetroMediaLab.App.Theme.OS.Editions.Windows95.Views;

internal partial class AcquisitionView : UserControl, IDisposable
{
    private readonly ThemeBindingScope bindings = new();
    private readonly ReadTabSection source;
    private readonly Action showAdvanced;
    private readonly Action close;
    private bool disposed;

    internal AcquisitionView(ReadTabSection source, ApplicationStatusBar status, Action showAdvanced, Action close)
    {
        this.source = source;
        this.showAdvanced = showAdvanced;
        this.close = close;
        try
        {
            InitializeComponent();
            bindings.TwoWay(source.FolderBlock, PathSection.TextProperty, Folder, TextBox.TextProperty);
            bindings.OneWay(source.FolderBlock.Input, TextBoxBase.IsReadOnlyProperty, Folder, TextBoxBase.IsReadOnlyProperty);
            bindings.TwoWay(source.FileNameBlock.FileNameTextBox, TextBox.TextProperty, FileName, TextBox.TextProperty);
            bindings.Action(source.FolderBlock.BrowseButton, Browse);
            bindings.Action(source.FolderBlock.BrowseButton, BrowseCommand);
            bindings.Action(source.ExecuteActionButton, Execute, followCaption: true);
            bindings.Action(source.ExecuteActionButton, ExecuteCommand, followCaption: true);
            bindings.Action(source.ProfileBlock.SaveButton, SaveProfile);
            bindings.Action(source.ProfileBlock.ResetButton, ResetProfile);
            BindChoice(source.ProfileBlock.ProfileCombo, Profiles);
            BindChoice(source.ImageBlock.FamilyCombo, Family);
            BindChoice(source.ImageBlock.FormatCombo, Format);
            BindChoice(source.ImageBlock.ExtensionCombo, Extension);
            bindings.TwoWay(source.ImageBlock.RawScpRadio, ToggleButton.IsCheckedProperty, Raw, TabItem.IsSelectedProperty);
            bindings.TwoWay(source.ImageBlock.KnownFormatRadio, ToggleButton.IsCheckedProperty, Known, TabItem.IsSelectedProperty);
            bindings.OneWay(source.ImageBlock.RawScpRadio, IsEnabledProperty, Raw, IsEnabledProperty);
            bindings.OneWay(source.ImageBlock.KnownFormatRadio, IsEnabledProperty, Known, IsEnabledProperty);
            var revolutionsEnabled = source.AdvancedBlock.RevsEnabledCheckBox;
            var revolutions = source.AdvancedBlock.RevsValue;
            bindings.TwoWay(revolutionsEnabled, ToggleButton.IsCheckedProperty, RevolutionsEnabled, ToggleButton.IsCheckedProperty);
            bindings.TwoWay(revolutions, TextBox.TextProperty, Revolutions, TextBox.TextProperty);
            bindings.OneWay(revolutionsEnabled, IsEnabledProperty, RevolutionsEnabled, IsEnabledProperty);
            bindings.OneWay(revolutions, IsEnabledProperty, Revolutions, IsEnabledProperty);
            bindings.OneWay(revolutions, ToolTipProperty, Revolutions, ToolTipProperty);
            bindings.OneWay(status.HardwareText, TextBlock.TextProperty, Hardware, TextBlock.TextProperty);
            History.ItemsSource = source.CompletionBlock.Captures;
            var completedPath = nameof(Selector.SelectedItem) + "." + nameof(ReadCaptureViewModel.IsCompleted);
            Explore.SetBinding(IsEnabledProperty, new Binding(completedPath) { Source = History, FallbackValue = false });
            Visualize.SetBinding(IsEnabledProperty, new Binding(completedPath) { Source = History, FallbackValue = false });
            ExploreCommand.SetBinding(IsEnabledProperty, new Binding(completedPath) { Source = History, FallbackValue = false });
            VisualizeCommand.SetBinding(IsEnabledProperty, new Binding(completedPath) { Source = History, FallbackValue = false });
        }
        catch { Dispose(); throw; }
    }

    private void BindChoice(ComboBox original, ComboBox target)
    {
        bindings.OneWay(original, ItemsControl.DisplayMemberPathProperty, target, ItemsControl.DisplayMemberPathProperty);
        bindings.OneWay(original, ItemsControl.ItemsSourceProperty, target, ItemsControl.ItemsSourceProperty);
        bindings.TwoWay(original, Selector.SelectedItemProperty, target, Selector.SelectedItemProperty);
        bindings.OneWay(original, IsEnabledProperty, target, IsEnabledProperty);
    }

    private void CloseClick(object sender, RoutedEventArgs args) { if (!disposed) close(); }
    private void AdvancedClick(object sender, RoutedEventArgs args) { if (!disposed) showAdvanced(); }
    private void ExploreClick(object sender, RoutedEventArgs args)
    {
        if (!disposed && History.SelectedItem is ReadCaptureViewModel capture) source.CompletionBlock.RequestExplore(capture);
    }
    private void VisualizeClick(object sender, RoutedEventArgs args)
    {
        if (!disposed && History.SelectedItem is ReadCaptureViewModel capture) source.CompletionBlock.RequestVisualization(capture);
    }
    private void HistoryDoubleClick(object sender, MouseButtonEventArgs args)
    {
        if (args.ChangedButton == MouseButton.Left) ExploreClick(sender, args);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { bindings.Dispose(); }
        finally
        {
            if (History is not null)
            {
                History.MouseDoubleClick -= HistoryDoubleClick;
                History.ItemsSource = null;
            }
            if (AdvancedCommand is not null) AdvancedCommand.Click -= AdvancedClick;
            if (CloseCommand is not null) CloseCommand.Click -= CloseClick;
            if (ExploreCommand is not null) ExploreCommand.Click -= ExploreClick;
            if (VisualizeCommand is not null) VisualizeCommand.Click -= VisualizeClick;
            if (Explore is not null) Explore.Click -= ExploreClick;
            if (Visualize is not null) Visualize.Click -= VisualizeClick;
            if (Explore is not null) BindingOperations.ClearAllBindings(Explore);
            if (Visualize is not null) BindingOperations.ClearAllBindings(Visualize);
            if (ExploreCommand is not null) BindingOperations.ClearAllBindings(ExploreCommand);
            if (VisualizeCommand is not null) BindingOperations.ClearAllBindings(VisualizeCommand);
            if (Profiles is not null) Profiles.ItemsSource = null;
            if (Family is not null) Family.ItemsSource = null;
            if (Format is not null) Format.ItemsSource = null;
            if (Extension is not null) Extension.ItemsSource = null;
            Content = null;
        }
    }
}
