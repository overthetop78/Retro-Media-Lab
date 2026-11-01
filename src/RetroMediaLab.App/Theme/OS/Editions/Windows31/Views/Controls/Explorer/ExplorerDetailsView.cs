using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Views.Controls.Explorer;

namespace RetroMediaLab.App.Theme.OS.Editions.Windows31.Views;

internal sealed class ExplorerDetailsView : UserControl, IDisposable
{
    private readonly ExplorerDetailsPanel source;
    private readonly StackPanel rows = new() { Margin = new Thickness(10) };
    private readonly List<TextBlock> labels = [];
    private bool disposed;

    internal ExplorerDetailsView(ExplorerDetailsPanel source)
    {
        this.source = source;
        try
        {
            Content = new ScrollViewer { Content = rows, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            source.PresentationChanged += PresentationChanged;
            Render();
        }
        catch { Dispose(); throw; }
    }

    private void PresentationChanged(object? sender, EventArgs args) => Render();

    private void Render()
    {
        if (disposed) return;
        ClearRows();
        var presentation = source.Presentation;
        rows.Children.Add(new TextBlock { Text = presentation.Title, FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 12) });
        foreach (var item in presentation.Rows)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 7) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            var label = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 10, 0) };
            label.SetBinding(TextBlock.TextProperty, LocExtension.CreateBinding(item.Key));
            labels.Add(label);
            var value = new TextBlock { Text = item.Value, TextWrapping = TextWrapping.Wrap };
            Grid.SetColumn(value, 1);
            grid.Children.Add(label);
            grid.Children.Add(value);
            rows.Children.Add(grid);
        }
    }

    private void ClearRows()
    {
        foreach (var label in labels) BindingOperations.ClearAllBindings(label);
        labels.Clear();
        rows.Children.Clear();
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { source.PresentationChanged -= PresentationChanged; }
        finally
        {
            ClearRows();
            if (Content is ScrollViewer viewer) viewer.Content = null;
            Content = null;
        }
    }
}
