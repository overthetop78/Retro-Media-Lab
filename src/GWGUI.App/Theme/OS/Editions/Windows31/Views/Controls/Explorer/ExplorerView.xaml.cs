using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using GWGUI.App.Theme.Common;
using GWGUI.App.ViewModels.Explorer;
using GWGUI.App.Views.Controls.Explorer;

namespace GWGUI.App.Theme.OS.Editions.Windows31.Views;

internal partial class ExplorerView : UserControl, IDisposable
{
    private readonly ThemeBindingScope bindings = new();
    private readonly ExplorerSection source;
    private readonly Action information;
    private readonly Action classification;
    private readonly Action warnings;
    private readonly Action close;
    private bool disposed;

    internal ExplorerView(ExplorerSection source, Action information, Action classification, Action warnings, Action close)
    {
        this.source = source;
        this.information = information;
        this.classification = classification;
        this.warnings = warnings;
        this.close = close;
        try
        {
            InitializeComponent();
            bindings.Action(source.OpenButton, OpenCommand);
            bindings.Action(source.OpenButton, EmptyOpen);
            bindings.Action(source.ReadDiskButton, ReadCommand, followCaption: true);
            Choice(source.SessionSelector, Sessions);
            Choice(source.VolumeSelector, Volumes);
            Choice(source.DetectedFormatSelector, Formats);
            Choice(source.FolderList, Folders);
            Choice(source.ContentsList, Files);
            Choice(source.AudioTrackList, Tracks);
            FollowVisibility(source.SessionSelector, SessionLabel);
            FollowVisibility(source.VolumeSelector, VolumeLabel);
            FollowVisibility(source.OpticalTracksSection, OpticalTracks);
            FollowVisibility(source.ExplorerEmptyState, EmptyState);
            FollowVisibility(source.LoadingOverlay, Recognition);
            FollowVisibility(source.WarningsButton, WarningsCommand);
            Text(source.PathText, Path, TextBox.TextProperty);
            Text(source.VolumeNameText, VolumeName);
            Text(source.CapacityLabel, CapacityCaption);
            Text(source.CapacityText, Capacity);
            Text(source.FreeLabel, FreeCaption);
            Text(source.FreeText, Free);
            Text(source.EntryCountLabel, EntriesCaption);
            Text(source.EntryCountText, Entries);
            Text(source.LoadingCard.StageText, RecognitionStage);
            Text(source.LoadingCard.DetailText, RecognitionDetail);
            bindings.OneWay(source.LoadingCard.Progress, RangeBase.ValueProperty, RecognitionProgress, RangeBase.ValueProperty);
            bindings.OneWay(source.LoadingCard.Progress, ProgressBar.IsIndeterminateProperty, RecognitionProgress, ProgressBar.IsIndeterminateProperty);
            bindings.TwoWay(source.AutomaticDetection, ToggleButton.IsCheckedProperty, AutomaticCommand, MenuItem.IsCheckedProperty);
            source.DetailsPanel.PresentationChanged += DetailsChanged;
            DetailsChanged(this, EventArgs.Empty);
        }
        catch { Dispose(); throw; }
    }

    private void Choice(Selector original, Selector target)
    {
        bindings.OneWay(original, ItemsControl.ItemsSourceProperty, target, ItemsControl.ItemsSourceProperty);
        bindings.TwoWay(original, Selector.SelectedItemProperty, target, Selector.SelectedItemProperty);
        bindings.OneWay(original, IsEnabledProperty, target, IsEnabledProperty);
        FollowVisibility(original, target);
    }

    private void FollowVisibility(UIElement original, UIElement target)
        => bindings.OneWay(original, VisibilityProperty, target, VisibilityProperty);

    private void Text(DependencyObject original, DependencyObject target, DependencyProperty? targetProperty = null)
        => bindings.OneWay(original, original is TextBox ? TextBox.TextProperty : TextBlock.TextProperty,
            target, targetProperty ?? TextBlock.TextProperty);

    private void DetailsChanged(object? sender, EventArgs args)
        => InformationCommand.IsEnabled = source.DetailsPanel.Presentation.Rows.Count > 0;

    private void FolderBranchClick(object sender, MouseButtonEventArgs args)
    {
        if (disposed || sender is not Windows31FolderBranch { Folder: { } folder } branch
            || folder.Children.Count == 0 || args.GetPosition(branch).X < folder.Depth * Windows31FolderBranch.Indent) return;
        source.RequestToggleFolder(folder);
        args.Handled = true;
    }

    private void FileDoubleClick(object sender, MouseButtonEventArgs args) => OpenSelected();
    private void FileKeyDown(object sender, KeyEventArgs args)
    {
        if (args.Key != Key.Enter) return;
        OpenSelected();
        args.Handled = true;
    }
    private void OpenSelected()
    {
        if (!disposed && Files.SelectedItem is ExplorerContentItem item) source.RequestOpenEntry(item);
    }
    private void InformationClick(object sender, RoutedEventArgs args) { if (!disposed) information(); }
    private void ClassificationClick(object sender, RoutedEventArgs args) { if (!disposed) classification(); }
    private void WarningsClick(object sender, RoutedEventArgs args) { if (!disposed) warnings(); }
    private void CloseClick(object sender, RoutedEventArgs args) { if (!disposed) close(); }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try
        {
            source.DetailsPanel.PresentationChanged -= DetailsChanged;
            bindings.Dispose();
        }
        finally
        {
            if (Files is not null)
            {
                Files.MouseDoubleClick -= FileDoubleClick;
                Files.KeyDown -= FileKeyDown;
            }
            foreach (var selector in new Selector?[] { Folders, Files, Sessions, Volumes, Formats, Tracks })
                if (selector is not null) selector.ItemsSource = null;
            Content = null;
        }
    }
}
