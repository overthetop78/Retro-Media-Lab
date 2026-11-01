using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.App.Views.Controls.Emulation.Input;

namespace RetroMediaLab.App.Theme.Orbit.Views;

internal sealed class ControllerOutputsView : StackPanel, IDisposable
{
    private readonly EmulationControllerOutputEditor source;
    private readonly ThemeBindingScope availability = new();
    private ThemeBindingScope rows = new();
    private readonly List<Action> detach = [];
    private bool disposed;
    private const double LabelWidth = 140;

    internal ControllerOutputsView(EmulationControllerOutputEditor source)
    {
        this.source = source;
        try
        {
            availability.OneWay(source, VisibilityProperty, this, VisibilityProperty);
            availability.OneWay(source, IsEnabledProperty, this, IsEnabledProperty);
            source.PresentationChanged += SourceChanged;
            Rebuild();
        }
        catch { Dispose(); throw; }
    }

    private void SourceChanged(object? sender, EventArgs args) { if (!disposed) Rebuild(); }
    private void Rebuild()
    {
        ClearRows();
        rows = new ThemeBindingScope();
        foreach (var endpoint in source.PresentationRows)
        {
            var row = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(LabelWidth) });
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.Children.Add(new TextBlock { Text = endpoint.Label, TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) });
            var selector = new ComboBox();
            rows.OneWay(endpoint.Selector, ItemsControl.DisplayMemberPathProperty, selector, ItemsControl.DisplayMemberPathProperty);
            rows.OneWay(endpoint.Selector, Selector.SelectedValuePathProperty, selector, Selector.SelectedValuePathProperty);
            rows.OneWay(endpoint.Selector, ItemsControl.ItemsSourceProperty, selector, ItemsControl.ItemsSourceProperty);
            rows.TwoWay(endpoint.Selector, Selector.SelectedItemProperty, selector, Selector.SelectedItemProperty);
            rows.OneWay(endpoint.Selector, IsEnabledProperty, selector, IsEnabledProperty);
            EventHandler refresh = (_, _) => endpoint.Refresh();
            selector.DropDownOpened += refresh;
            detach.Add(() => selector.DropDownOpened -= refresh);
            detach.Add(() => selector.ItemsSource = null);
            Grid.SetColumn(selector, 1);
            row.Children.Add(selector);
            Children.Add(row);
        }
    }

    private void ClearRows()
    {
        try { rows.Dispose(); }
        finally
        {
            try { foreach (var action in detach) action(); }
            finally { detach.Clear(); Children.Clear(); }
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        source.PresentationChanged -= SourceChanged;
        try { availability.Dispose(); }
        finally { ClearRows(); }
    }
}
