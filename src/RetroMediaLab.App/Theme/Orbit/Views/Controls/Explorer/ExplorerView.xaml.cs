using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.App.ViewModels.Explorer;
using RetroMediaLab.App.Views.Controls.Explorer;

namespace RetroMediaLab.App.Theme.Orbit.Views;

internal partial class ExplorerView : UserControl, IDisposable
{
    private readonly ThemeBindingScope bindings = new();
    private readonly ExplorerSection source;
    private readonly Action information;
    private readonly Action classification;
    private readonly Action warnings;
    private readonly Action close;
    private bool disposed;
    private OrbitResources? resourceScope;

    internal ExplorerView(ExplorerSection source, Action information, Action classification, Action warnings, Action close, bool? dark = null)
    {
        this.source = source;
        this.information = information;
        this.classification = classification;
        this.warnings = warnings;
        this.close = close;
        try
        {
            resourceScope = new OrbitResources(this, dark);
            InitializeComponent();
            bindings.Action(source.OpenButton, OpenToolbar);
            bindings.Action(source.ReadDiskButton, ReadToolbar);
            bindings.OneWay(source.ReadDiskButton, ContentControl.ContentProperty, ReadCaption, TextBlock.TextProperty);
            bindings.Action(source.OpenButton, EmptyOpen);
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
            bindings.TwoWay(source.AutomaticDetection, ToggleButton.IsCheckedProperty, AutomaticCommand, ToggleButton.IsCheckedProperty);
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
    {
        InformationToolbar.IsEnabled = source.DetailsPanel.Presentation.Rows.Count > 0;
    }

    private void FolderBranchClick(object sender, MouseButtonEventArgs args)
    {
        if (disposed || sender is not OrbitFolderBranch { Folder: { } folder } branch
            || !branch.IsExpanderHit(args.GetPosition(branch))) return;
        source.RequestToggleFolder(folder);
        args.Handled = true;
    }

    private void FolderKeyDown(object sender, KeyEventArgs args)
    {
        if (disposed || Folders.SelectedItem is not ExplorerFolderItem folder) return;
        if (args.Key == Key.Right)
        {
            if (folder.Children.Count == 0) return;
            if (!folder.IsExpanded) source.RequestToggleFolder(folder);
            else Folders.SelectedItem = folder.Children[0];
        }
        else if (args.Key == Key.Left)
        {
            if (folder.IsExpanded && folder.Children.Count > 0) source.RequestToggleFolder(folder);
            else if (Folders.Items.Cast<ExplorerFolderItem>().TakeWhile(item => !ReferenceEquals(item, folder))
                .LastOrDefault(item => item.Depth < folder.Depth) is { } parent) Folders.SelectedItem = parent;
        }
        else return;
        args.Handled = true;
    }

    private void FileDoubleClick(object sender, MouseButtonEventArgs args)
    {
        if (args.ChangedButton == MouseButton.Left) OpenSelected();
    }
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
            if (Folders is not null) Folders.PreviewKeyDown -= FolderKeyDown;
            if (InformationToolbar is not null) InformationToolbar.Click -= InformationClick;
            if (ClassificationCommand is not null) ClassificationCommand.Click -= ClassificationClick;
            if (WarningsCommand is not null) WarningsCommand.Click -= WarningsClick;
            if (CloseCommand is not null) CloseCommand.Click -= CloseClick;
            foreach (var selector in new Selector?[] { Folders, Files, Sessions, Volumes, Formats, Tracks })
                if (selector is not null) selector.ItemsSource = null;
            try { resourceScope?.Dispose(); }
            finally { resourceScope = null; Content = null; }
        }
    }
}
