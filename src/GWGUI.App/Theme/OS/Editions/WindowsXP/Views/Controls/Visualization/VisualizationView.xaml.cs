using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using GWGUI.App.Localization.Extensions;
using GWGUI.App.Theme.Common;
using GWGUI.App.Views.Controls.Visualization;
using GWGUI.MediaEngine.Enums;

namespace GWGUI.App.Theme.OS.Editions.WindowsXP.Views;

internal partial class VisualizationView : UserControl, IDisposable
{
    private readonly VisualizerTabSection source;
    private readonly ThemeBindingScope bindings = new();
    private readonly Action showInspector;
    private readonly Action showClassification;
    private readonly Action close;
    private VisualizationOverviewView? overview;
    private ThemeElementLease? drawings;
    private ThemeBindingScope? drawingBindings;
    private SequentialMediaView? timeline;
    private FrameworkElement? presentedSource;
    private MediaRepresentationKind? presentedKind;
    private bool disposed;
    private WindowsXPResources? resourceScope;

    internal VisualizationView(VisualizerTabSection source, Action showInspector, Action showClassification, Action close, WindowsXPColorScheme scheme = WindowsXPColorScheme.Blue)
    {
        this.source = source;
        this.showInspector = showInspector;
        this.showClassification = showClassification;
        this.close = close;
        try
        {
            InitializeComponent();
            resourceScope = new WindowsXPResources(this, scheme);
            Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(WindowsXPVisualizationMetrics.StylesUri, UriKind.Relative) });
            OpenIcon.Source = WindowsXPApplicationIcons.ForIcon(WindowsXPIcon.Explorer);
            InspectorIcon.Source = WindowsXPApplicationIcons.ForIcon(WindowsXPIcon.Information);
            bindings.Action(source.Header.OpenButton, OpenCommand);
            bindings.Action(source.Header.OpenButton, OpenToolbar);
            bindings.Action(source.Header.ResetButton, ResetToolbar);
            bindings.TwoWay(source.Header.LinkZoomCheckBox, ToggleButton.IsCheckedProperty, LinkZoomToolbar, ToggleButton.IsCheckedProperty);
            bindings.OneWay(source.Header.LinkZoomCheckBox, VisibilityProperty, LinkZoomToolbar, VisibilityProperty);
            bindings.Action(source.Header.OpenButton, EmptyOpen);
            bindings.Action(source.Header.ResetButton, ResetCommand);
            bindings.TwoWay(source.Header.LinkZoomCheckBox, ToggleButton.IsCheckedProperty,
                LinkZoomCommand, MenuItem.IsCheckedProperty);
            bindings.OneWay(source.Header.LinkZoomCheckBox, VisibilityProperty, LinkZoomCommand, VisibilityProperty);
            bindings.OneWay(source.Header.ClassificationOptionsButton, VisibilityProperty, ClassificationCommand, VisibilityProperty);
            bindings.OneWay(source.Header.FileNameText, TextBlock.TextProperty, FileName, TextBlock.TextProperty);
            bindings.OneWay(source.Header.SummaryText, TextBlock.TextProperty, Summary, TextBlock.TextProperty);
            Choice(DocumentChoices, source.Header.RepresentationCombo, RepresentationResource, bindings);
            Choice(DocumentChoices, source.Header.DecoderCombo, DecoderResource, bindings);
            Choice(DocumentChoices, source.Header.RevolutionCombo, RevolutionResource, bindings, source.Header.RevolutionAvailability);
            bindings.OneWay(source.ProgressLegend.DataItems, ItemsControl.ItemsSourceProperty, Legend, ItemsControl.ItemsSourceProperty);
            overview = new VisualizationOverviewView(source.Overview);
            OverviewHost.Content = overview;
            bindings.OneWay(source.Overview, VisibilityProperty, OverviewHost, VisibilityProperty);
            source.PresentationChanged += PresentationChanged;
            Refresh();
        }
        catch { Dispose(); throw; }
    }

    private void PresentationChanged(object? sender, EventArgs args) => Refresh();

    private void Refresh()
    {
        if (disposed) return;
        var hasDocument = source.CurrentDocument is not null;
        InspectorCommand.IsEnabled = hasDocument;
        InspectorToolbar.IsEnabled = hasDocument;
        EmptyState.Visibility = !hasDocument && !source.RecognitionProgressVisible ? Visibility.Visible : Visibility.Collapsed;
        Recognition.Visibility = source.RecognitionProgressVisible ? Visibility.Visible : Visibility.Collapsed;
        RecognitionStage.Text = source.RecognitionStage;
        RecognitionDetail.Text = source.RecognitionDetail;
        RecognitionProgress.IsIndeterminate = source.RecognitionIsIndeterminate;
        RecognitionProgress.Value = source.RecognitionValue;
        var kind = hasDocument ? source.ActiveRepresentationKind : null;
        var current = kind is { } value ? source.RegisteredRepresentationViews.GetValueOrDefault(value) : null;
        if (kind == presentedKind && ReferenceEquals(current, presentedSource)) return;
        ClearSurfaces();
        if (kind is null || current is null) return;
        try
        {
            var surfaces = MediaVisualizationElements.Surfaces(source, kind.Value);
            drawings = new ThemeElementLease(surfaces.Select(surface => surface.Document));
            drawingBindings = new ThemeBindingScope();
            DrawingArea.ColumnDefinitions.Clear();
            foreach (var surface in surfaces)
            {
                var panel = CreateSurface(surface, drawings[surface.Document.Id], drawingBindings);
                var column = new ColumnDefinition();
                column.SetBinding(ColumnDefinition.WidthProperty, new Binding(nameof(Visibility))
                { Source = panel, Converter = SurfaceColumnConverter.Instance });
                DrawingArea.ColumnDefinitions.Add(column);
                Grid.SetColumn(panel, DrawingArea.ColumnDefinitions.Count - 1);
                DrawingArea.Children.Add(panel);
            }
            if (current is BlockMediaView blocks) ConfigureBlockMode(blocks);
            presentedSource = current;
            presentedKind = kind;
        }
        catch { ClearSurfaces(); throw; }
    }

    private FrameworkElement CreateSurface(MediaVisualizationElements.Surface surface, ThemeElementHost host, ThemeBindingScope scope)
    {
        var panel = new DockPanel { Margin = new Thickness(2) };
        if (surface.Source is not BlockMediaView && surface.Document.VisibilitySource is { } available)
            scope.OneWay(available, VisibilityProperty, panel, VisibilityProperty);
        var commands = new DockPanel { Margin = new Thickness(0, 0, 0, 4) };
        DockPanel.SetDock(commands, Dock.Top);
        panel.Children.Add(commands);
        var zoom = new StackPanel { Orientation = Orientation.Horizontal };
        DockPanel.SetDock(zoom, Dock.Right);
        commands.Children.Add(zoom);
        AddZoom(zoom, surface.ZoomOut, ZoomOutCaption, scope);
        AddZoom(zoom, surface.ResetZoom, null, scope);
        AddZoom(zoom, surface.ZoomIn, ZoomInCaption, scope);
        var selection = new TextBlock { Margin = new Thickness(4, 3, 4, 3), TextTrimming = TextTrimming.CharacterEllipsis };
        scope.OneWay(surface.Selection, TextBlock.TextProperty, selection, TextBlock.TextProperty);
        DockPanel.SetDock(selection, Dock.Bottom);
        panel.Children.Add(selection);
        if (surface.Caption is { } originalCaption)
        {
            var caption = new TextBlock { Margin = new Thickness(4, 3, 4, 3) };
            scope.OneWay(originalCaption, TextBlock.TextProperty, caption, TextBlock.TextProperty);
            commands.Children.Add(caption);
        }
        var filters = new WrapPanel();
        DockPanel.SetDock(filters, Dock.Top);
        panel.Children.Add(filters);
        switch (surface.Source)
        {
            case OpticalMediaView optical:
                Choice(filters, optical.FaceSelector, SideResource, scope);
                Choice(filters, optical.LayerSelector, LayerResource, scope);
                Choice(filters, optical.SessionSelector, SessionResource, scope);
                break;
            case SequentialMediaView sequential:
                Choice(filters, sequential.FaceSelector, SideResource, scope);
                Choice(filters, sequential.TrackSelector, TrackResource, scope);
                Choice(filters, sequential.ChannelSelector, ChannelResource, scope);
                break;
        }
        var border = new Border { BorderThickness = new Thickness(1), ClipToBounds = true };
        border.SetResourceReference(Border.BackgroundProperty, WindowsXPVisualizationMetrics.WindowBrush);
        border.SetResourceReference(Border.BorderBrushProperty, WindowsXPVisualizationMetrics.FrameBrush);
        panel.Children.Add(border);
        if (surface.Timeline is { } sequentialSource)
        {
            var viewport = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = host };
            border.Child = viewport;
            timeline = sequentialSource;
            sequentialSource.SetPresentationViewport(viewport);
        }
        else border.Child = host;
        return panel;
    }

    private void ConfigureBlockMode(BlockMediaView blocks)
    {
        var mode = new ComboBox { Margin = new Thickness(4), MinWidth = ChoiceWidth };
        mode.Items.Add(new ComboBoxItem { Content = LocExtension.Get(LogicalMapResource) });
        var geometry = new ComboBoxItem { Content = LocExtension.Get(GeometryResource) };
        mode.Items.Add(geometry);
        blockMode = mode;
        var scope = drawingBindings!;
        scope.OneWay(blocks.GeometryTab, VisibilityProperty, geometry, VisibilityProperty);
        scope.OneWay(blocks.GeometryTab, VisibilityProperty, this, GeometryAvailabilityProperty);
        scope.TwoWay(blocks.ViewSelector, Selector.SelectedIndexProperty, mode, Selector.SelectedIndexProperty);
        if (mode.SelectedIndex < 0) mode.SelectedIndex = 0;
        DocumentChoices.Children.Add(mode);
        blockSurfaceChoice = Choice(DocumentChoices, blocks.SurfaceSelector, SurfaceResource, scope);
        mode.SelectionChanged += BlockModeChanged;
        UpdateBlockMode();
    }

    private static readonly DependencyProperty GeometryAvailabilityProperty = DependencyProperty.Register(
        nameof(GeometryAvailability), typeof(Visibility), typeof(VisualizationView),
        new PropertyMetadata(Visibility.Collapsed, (owner, _) => ((VisualizationView)owner).UpdateBlockMode()));
    private Visibility GeometryAvailability => (Visibility)GetValue(GeometryAvailabilityProperty);
    private FrameworkElement? blockSurfaceChoice;

    private void BlockModeChanged(object sender, SelectionChangedEventArgs args) => UpdateBlockMode();

    private void UpdateBlockMode()
    {
        if (blockMode is null) return;
        if (blockMode.SelectedIndex == 1 && GeometryAvailability != Visibility.Visible)
            blockMode.SelectedIndex = 0;
        for (var index = 0; index < DrawingArea.Children.Count; index++)
        {
            DrawingArea.Children[index].Visibility = index == blockMode.SelectedIndex ? Visibility.Visible : Visibility.Collapsed;
            Grid.SetColumn(DrawingArea.Children[index], 0);
            Grid.SetColumnSpan(DrawingArea.Children[index], DrawingArea.ColumnDefinitions.Count);
        }
        if (blockSurfaceChoice is not null)
            blockSurfaceChoice.Visibility = blockMode.SelectedIndex == 1 ? Visibility.Visible : Visibility.Collapsed;
    }

    private ComboBox? blockMode;
    private const int PersistentChoiceCount = 3;

    private static void AddZoom(Panel panel, Button original, string? caption, ThemeBindingScope scope)
    {
        var button = new Button { Content = caption, Margin = new Thickness(1), MinWidth = ZoomWidth };
        scope.Action(original, button, followCaption: caption is null);
        panel.Children.Add(button);
    }

    private static FrameworkElement Choice(Panel panel, ComboBox original, string labelResource, ThemeBindingScope scope, FrameworkElement? available = null)
    {
        var availabilityStyle = new Style(typeof(StackPanel));
        availabilityStyle.Setters.Add(new Setter(VisibilityProperty, Visibility.Collapsed));
        var visible = new Trigger { Property = TagProperty, Value = Visibility.Visible };
        visible.Setters.Add(new Setter(VisibilityProperty, Visibility.Visible));
        availabilityStyle.Triggers.Add(visible);
        var group = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(2), Style = availabilityStyle };
        var choice = new ComboBox { MinWidth = ChoiceWidth, MaxWidth = ChoiceMaximumWidth };
        group.Children.Add(new Label { Content = LocExtension.Get(labelResource), Target = choice });
        group.Children.Add(choice);
        panel.Children.Add(group);
        scope.OneWay(original, ItemsControl.DisplayMemberPathProperty, choice, ItemsControl.DisplayMemberPathProperty);
        scope.OneWay(original, ItemsControl.ItemsSourceProperty, choice, ItemsControl.ItemsSourceProperty);
        scope.TwoWay(original, Selector.SelectedItemProperty, choice, Selector.SelectedItemProperty);
        scope.OneWay(original, IsEnabledProperty, choice, IsEnabledProperty);
        scope.OneWay(available ?? original, VisibilityProperty, group, TagProperty);
        return group;
    }

    private void ClearSurfaces()
    {
        try { drawingBindings?.Dispose(); }
        finally
        {
            drawingBindings = null;
            try { timeline?.SetPresentationViewport(null); }
            finally
            {
                timeline = null;
                if (blockMode is not null) blockMode.SelectionChanged -= BlockModeChanged;
                blockMode = null;
                blockSurfaceChoice = null;
                while (DocumentChoices is not null && DocumentChoices.Children.Count > PersistentChoiceCount)
                    DocumentChoices.Children.RemoveAt(DocumentChoices.Children.Count - 1);
                DrawingArea?.Children.Clear();
                if (DrawingArea is not null)
                {
                    foreach (var column in DrawingArea.ColumnDefinitions) BindingOperations.ClearAllBindings(column);
                    DrawingArea.ColumnDefinitions.Clear();
                }
                try { drawings?.Dispose(); }
                finally { drawings = null; presentedSource = null; presentedKind = null; }
            }
        }
    }

    private void InspectorClick(object sender, RoutedEventArgs args) { if (!disposed && InspectorCommand.IsEnabled) showInspector(); }
    private void ClassificationClick(object sender, RoutedEventArgs args) { if (!disposed) showClassification(); }
    private void CloseClick(object sender, RoutedEventArgs args) { if (!disposed) close(); }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        source.PresentationChanged -= PresentationChanged;
        try { ClearSurfaces(); }
        finally
        {
            try { bindings.Dispose(); }
            finally
            {
                try { overview?.Dispose(); }
                finally
                {
                    overview = null;
                    if (OverviewHost is not null) OverviewHost.Content = null;
                    if (Legend is not null) Legend.ItemsSource = null;
                    if (InspectorCommand is not null) InspectorCommand.Click -= InspectorClick;
                    if (InspectorToolbar is not null) InspectorToolbar.Click -= InspectorClick;
                    if (ClassificationCommand is not null) ClassificationCommand.Click -= ClassificationClick;
                    if (CloseCommand is not null) CloseCommand.Click -= CloseClick;
                    if (DocumentChoices is not null) DocumentChoices.Children.Clear();
                    try { resourceScope?.Dispose(); }
                    finally { resourceScope = null; Resources.MergedDictionaries.Clear(); Content = null; }
                }
            }
        }
    }

    private sealed class SurfaceColumnConverter : IValueConverter
    {
        internal static SurfaceColumnConverter Instance { get; } = new();
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is Visibility.Visible ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    private const double ChoiceWidth = 100;
    private const double ChoiceMaximumWidth = 230;
    private const double ZoomWidth = 28;
    private const string ZoomOutCaption = "−";
    private const string ZoomInCaption = "+";
    private const string RepresentationResource = "Visual.Representation";
    private const string DecoderResource = "Visual.Decoder";
    private const string RevolutionResource = "Visual.RevolutionLabel";
    private const string SideResource = "Visual.SideLabel";
    private const string LayerResource = "Visual.LayerLabel";
    private const string SessionResource = "Visual.SessionLabel";
    private const string TrackResource = "Visual.TrackLabel";
    private const string ChannelResource = "Visual.ChannelLabel";
    private const string SurfaceResource = "Visual.SurfaceLabel";
    private const string LogicalMapResource = "Visual.LogicalMapTitle";
    private const string GeometryResource = "Visual.ChsGeometryTitle";
}
