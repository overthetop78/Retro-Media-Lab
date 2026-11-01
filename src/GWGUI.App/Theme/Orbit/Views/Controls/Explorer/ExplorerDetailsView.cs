using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using GWGUI.App.Localization.Extensions;
using GWGUI.App.Views.Controls.Explorer;

namespace GWGUI.App.Theme.Orbit.Views;

internal sealed class ExplorerDetailsView : UserControl, IDisposable
{
    private const double MinimumWidth = 380;
    private const double MaximumHeight = 560;
    private const double RowSpacing = 4;
    private readonly ExplorerDetailsPanel source;
    private readonly StackPanel pages;
    private readonly List<TextBlock> captions = [];
    private bool disposed;
    private OrbitResources? resourceScope;

    internal ExplorerDetailsView(ExplorerDetailsPanel source, bool? dark = null)
    {
        this.source = source;
        try
        {
            resourceScope = new OrbitResources(this, dark);
            pages = OrbitOptionPages.Initialize(this, MinimumWidth, MaximumHeight);
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
        pages.Children.Clear();
        var page = OrbitOptionPages.Page(pages, OrbitOptionPages.GeneralResource);
        var presentation = source.Presentation;
        OrbitOptionPages.Row(page, new TextBlock
        {
            Text = presentation.Title,
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, RowSpacing)
        });
        foreach (var item in presentation.Rows)
        {
            var caption = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, RowSpacing, RowSpacing, 0) };
            caption.SetBinding(TextBlock.TextProperty, LocExtension.CreateBinding(item.Key));
            captions.Add(caption);
            var value = new TextBlock { Text = item.Value, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, RowSpacing, 0, 0) };
            OrbitOptionPages.Row(page, caption, value);
        }
    }

    private void ClearRows()
    {
        foreach (var caption in captions) BindingOperations.ClearAllBindings(caption);
        captions.Clear();
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { source.PresentationChanged -= PresentationChanged; }
        finally
        {
            ClearRows();
            pages?.Children.Clear();
            Content = null;
            try { resourceScope?.Dispose(); }
            finally { resourceScope = null; }
        }
    }
}
