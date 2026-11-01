using System.Collections;
using System.Windows;
using System.Windows.Media;
using GWGUI.App.ViewModels.Explorer;

namespace GWGUI.App.Theme.OS.Editions.Windows31;

/// <summary>File Manager tree lines and original folder glyphs, without a separate expander.</summary>
internal sealed class Windows31FolderBranch : FrameworkElement
{
    internal const double Indent = 17;
    internal const double GlyphSize = 16;
    private const double BranchCenter = 8;
    private const double LineWidth = 1;

    public static readonly DependencyProperty FolderProperty = DependencyProperty.Register(
        nameof(Folder), typeof(ExplorerFolderItem), typeof(Windows31FolderBranch),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty RowsProperty = DependencyProperty.Register(
        nameof(Rows), typeof(IEnumerable), typeof(Windows31FolderBranch),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public ExplorerFolderItem? Folder
    {
        get => (ExplorerFolderItem?)GetValue(FolderProperty);
        set => SetValue(FolderProperty, value);
    }

    public IEnumerable? Rows
    {
        get => (IEnumerable?)GetValue(RowsProperty);
        set => SetValue(RowsProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize) =>
        new((Folder?.Depth ?? 0) * Indent + GlyphSize, GlyphSize);

    protected override void OnRender(DrawingContext drawing)
    {
        base.OnRender(drawing);
        if (Folder is not { } folder) return;
        var rows = Rows?.Cast<ExplorerFolderItem>().ToArray() ?? [];
        var index = Array.IndexOf(rows, folder);
        var brush = TryFindResource(Windows31PaletteResources.WindowText) as Brush ?? Brushes.Black;
        var pen = new Pen(brush, LineWidth) { DashStyle = DashStyles.Dot };
        var center = ActualHeight / 2;
        for (var depth = 0; depth < folder.Depth; depth++)
        {
            var x = depth * Indent + BranchCenter;
            var next = index < 0 ? null : rows.Skip(index + 1).FirstOrDefault(item => item.Depth <= depth + 1);
            if (depth == folder.Depth - 1)
            {
                drawing.DrawLine(pen, new Point(x, 0), new Point(x, center));
                drawing.DrawLine(pen, new Point(x, center), new Point(folder.Depth * Indent, center));
            }
            if (next?.Depth == depth + 1)
                drawing.DrawLine(pen, new Point(x, depth == folder.Depth - 1 ? center : 0), new Point(x, ActualHeight));
        }
        drawing.DrawImage(Windows31FileImages.FolderBranch(folder.Children.Count > 0, folder.IsExpanded),
            new Rect(folder.Depth * Indent, center - GlyphSize / 2, GlyphSize, GlyphSize));
    }
}
