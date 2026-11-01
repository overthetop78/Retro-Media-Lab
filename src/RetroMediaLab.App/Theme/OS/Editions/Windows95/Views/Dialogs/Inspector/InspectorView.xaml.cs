using System.Windows;
using System.Windows.Controls;
using RetroMediaLab.App.Contracts.ViewModels.Visualization;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.App.Views.Controls.Visualization;

namespace RetroMediaLab.App.Theme.OS.Editions.Windows95.Views;

internal partial class InspectorView : UserControl, IDisposable
{
    private static readonly DependencyProperty FirstModelProperty = DependencyProperty.Register(
        "FirstModel", typeof(MediaInspectorModel), typeof(InspectorView), new PropertyMetadata(null, PresentationChanged));
    private static readonly DependencyProperty SecondModelProperty = DependencyProperty.Register(
        "SecondModel", typeof(MediaInspectorModel), typeof(InspectorView), new PropertyMetadata(null, PresentationChanged));
    private static readonly DependencyProperty FirstVisibilityProperty = DependencyProperty.Register(
        "FirstVisibility", typeof(Visibility), typeof(InspectorView), new PropertyMetadata(Visibility.Visible, PresentationChanged));
    private static readonly DependencyProperty SecondVisibilityProperty = DependencyProperty.Register(
        "SecondVisibility", typeof(Visibility), typeof(InspectorView), new PropertyMetadata(Visibility.Visible, PresentationChanged));
    private readonly ThemeBindingScope bindings = new();
    private readonly Action close;
    private bool refreshing;
    private bool disposed;

    internal InspectorView(MediaInspectorPanel first, MediaInspectorPanel second, Action close)
    {
        this.close = close;
        try
        {
            InitializeComponent();
            bindings.OneWay(first, MediaInspectorPanel.SurfaceTitleProperty, FirstSurface, ContentControl.ContentProperty);
            bindings.OneWay(second, MediaInspectorPanel.SurfaceTitleProperty, SecondSurface, ContentControl.ContentProperty);
            bindings.OneWay(first, VisibilityProperty, this, FirstVisibilityProperty);
            bindings.OneWay(second, VisibilityProperty, this, SecondVisibilityProperty);
            bindings.OneWay(first, MediaInspectorPanel.ModelProperty, this, FirstModelProperty);
            bindings.OneWay(second, MediaInspectorPanel.ModelProperty, this, SecondModelProperty);
            RefreshPresentation();
        }
        catch { Dispose(); throw; }
    }

    private static void PresentationChanged(DependencyObject target, DependencyPropertyChangedEventArgs args) =>
        ((InspectorView)target).RefreshPresentation();

    private void SurfaceChanged(object sender, SelectionChangedEventArgs args)
    {
        if (!refreshing && !disposed) RefreshPresentation();
    }

    private void RefreshPresentation()
    {
        if (disposed || SurfaceChoice is null || refreshing) return;
        refreshing = true;
        try
        {
            FirstSurface.Visibility = (Visibility)GetValue(FirstVisibilityProperty);
            SecondSurface.Visibility = (Visibility)GetValue(SecondVisibilityProperty);
            if (SurfaceChoice.SelectedItem is not ComboBoxItem { Visibility: Visibility.Visible })
                SurfaceChoice.SelectedItem = FirstSurface.Visibility == Visibility.Visible ? FirstSurface
                    : SecondSurface.Visibility == Visibility.Visible ? SecondSurface : null;
            var model = ReferenceEquals(SurfaceChoice.SelectedItem, FirstSurface) ? (MediaInspectorModel?)GetValue(FirstModelProperty)
                : ReferenceEquals(SurfaceChoice.SelectedItem, SecondSurface) ? (MediaInspectorModel?)GetValue(SecondModelProperty) : null;
            Title.Text = model?.Title ?? string.Empty;
            SelectedElement.Text = model?.SelectedElement ?? string.Empty;
            SelectedElement.Visibility = string.IsNullOrWhiteSpace(model?.SelectedElement) ? Visibility.Collapsed : Visibility.Visible;
            Sections.ItemsSource = model?.Sections;
            SurfaceChoice.IsEnabled = FirstSurface.Visibility == Visibility.Visible && SecondSurface.Visibility == Visibility.Visible;
        }
        finally { refreshing = false; }
    }

    private void CloseClick(object sender, RoutedEventArgs args) { if (!disposed) close(); }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { bindings.Dispose(); }
        finally
        {
            if (SurfaceChoice is not null) SurfaceChoice.SelectionChanged -= SurfaceChanged;
            if (CloseCommand is not null) CloseCommand.Click -= CloseClick;
            if (Sections is not null) Sections.ItemsSource = null;
            ClearValue(FirstModelProperty);
            ClearValue(SecondModelProperty);
            Content = null;
            Resources.Clear();
        }
    }
}
