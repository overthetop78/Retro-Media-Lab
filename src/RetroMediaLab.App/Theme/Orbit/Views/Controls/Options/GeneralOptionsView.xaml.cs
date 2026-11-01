using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.App.Views.Controls.Options;

namespace RetroMediaLab.App.Theme.Orbit.Views;

internal partial class GeneralOptionsView : UserControl, IDisposable
{
    private readonly OptionsGeneralSection source;
    private readonly ThemeBindingScope bindings = new();
    private bool disposed;
    private OrbitResources? resourceScope;

    internal GeneralOptionsView(OptionsGeneralSection source, bool? dark = null)
    {
        this.source = source;
        try
        {
            resourceScope = new OrbitResources(this, dark);
            InitializeComponent();
            Text(source.ImagesFolder, ImagesFolder);
            Text(source.TagPattern, TagPattern);
            Choice(source.Languages, Languages);
            Choice(source.TagPresets, TagPresets);
            bindings.TwoWay(source.UseTags, ToggleButton.IsCheckedProperty, UseTags, ToggleButton.IsCheckedProperty);
            Availability(source.UseTags, UseTags);
            bindings.OneWay(source.RecentTagPatternsList, ItemsControl.ItemsSourceProperty, RecentPatterns, ItemsControl.ItemsSourceProperty);
            bindings.TwoWay(source.RecentTagPatternsList, Selector.SelectedItemProperty, RecentPatterns, Selector.SelectedItemProperty);
            Availability(source.RecentTagPatternsList, RecentPatterns);
            bindings.OneWay(source.TagVariables, ItemsControl.ItemsSourceProperty, Variables, ItemsControl.ItemsSourceProperty);
            bindings.OneWay(source.TagPreview, TextBlock.TextProperty, Preview, TextBlock.TextProperty);
        }
        catch { Dispose(); throw; }
    }

    private void Text(TextBox original, TextBox target)
    {
        bindings.TwoWay(original, TextBox.TextProperty, target, TextBox.TextProperty);
        bindings.OneWay(original, TextBoxBase.IsReadOnlyProperty, target, TextBoxBase.IsReadOnlyProperty);
        Availability(original, target);
    }

    private void Choice(ComboBox original, ComboBox target)
    {
        bindings.OneWay(original, ItemsControl.ItemsSourceProperty, target, ItemsControl.ItemsSourceProperty);
        bindings.OneWay(original, ItemsControl.DisplayMemberPathProperty, target, ItemsControl.DisplayMemberPathProperty);
        bindings.TwoWay(original, Selector.SelectedItemProperty, target, Selector.SelectedItemProperty);
        Availability(original, target);
    }

    private void Availability(FrameworkElement original, FrameworkElement target)
    {
        bindings.OneWay(original, IsEnabledProperty, target, IsEnabledProperty);
        bindings.OneWay(original, VisibilityProperty, target, VisibilityProperty);
        bindings.OneWay(original, ToolTipProperty, target, ToolTipProperty);
    }

    private void BrowseClick(object sender, RoutedEventArgs args) { if (!disposed) source.RequestBrowseImagesFolder(); }
    private void NextExampleClick(object sender, RoutedEventArgs args) { if (!disposed) source.RequestNextTagExample(); }
    private void RecentDoubleClick(object sender, MouseButtonEventArgs args)
    {
        if (!disposed && ItemsControl.ContainerFromElement(RecentPatterns, args.OriginalSource as DependencyObject) is ListBoxItem)
            source.RequestRecentTagPattern(args);
    }
    private void FolderEditingFinished(object sender, KeyboardFocusChangedEventArgs args) { if (!disposed) source.RequestImagesFolderEditingFinished(args); }
    private void PatternEditingFinished(object sender, KeyboardFocusChangedEventArgs args) { if (!disposed) source.RequestTagPatternEditingFinished(args); }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { bindings.Dispose(); }
        finally
        {
            if (RecentPatterns is not null) RecentPatterns.MouseDoubleClick -= RecentDoubleClick;
            if (ImagesFolder is not null) ImagesFolder.LostKeyboardFocus -= FolderEditingFinished;
            if (TagPattern is not null) TagPattern.LostKeyboardFocus -= PatternEditingFinished;
            Content = null;
            try { resourceScope?.Dispose(); }
            finally { resourceScope = null; Resources.Clear(); }
        }
    }
}
