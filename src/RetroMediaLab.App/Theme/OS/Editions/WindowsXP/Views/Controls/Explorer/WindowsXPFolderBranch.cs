using System.Collections;
using System.Windows;
using System.Windows.Media;
using RetroMediaLab.App.ViewModels.Explorer;

namespace RetroMediaLab.App.Theme.OS.Editions.WindowsXP.Views;

/// <summary>Explorer branch lines and expanders; folder images remain supplied by FileEntryIcon.</summary>
internal sealed class WindowsXPFolderBranch : FrameworkElement
{
    public WindowsXPFolderBranch() => SnapsToDevicePixels = true;

    internal const double Indent = 17;
    internal const double BoxSize = 9;
    internal const double RowHeight = 18;
    private const double Gap = 3;
    private const double LineWidth = 1;
    private const double MarkInset = 2;
    private const double PixelCenter = 0.5;

    public static readonly DependencyProperty FolderProperty = DependencyProperty.Register(
        nameof(Folder), typeof(ExplorerFolderItem), typeof(WindowsXPFolderBranch),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty RowsProperty = DependencyProperty.Register(
        nameof(Rows), typeof(IEnumerable), typeof(WindowsXPFolderBranch),
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

    internal bool IsExpanderHit(Point point) => Folder is { Children.Count: > 0 } folder
        && point.X >= folder.Depth * Indent && point.X <= folder.Depth * Indent + BoxSize
        && point.Y >= (Math.Max(RowHeight, ActualHeight) - BoxSize) / 2
        && point.Y <= (Math.Max(RowHeight, ActualHeight) + BoxSize) / 2;

    protected override Size MeasureOverride(Size availableSize) => new((Folder?.Depth ?? 0) * Indent + BoxSize + Gap, RowHeight);

    protected override void OnRender(DrawingContext drawing)
    {
        base.OnRender(drawing);
        if (Folder is not { } folder) return;
        var rows = Rows?.Cast<ExplorerFolderItem>().ToArray() ?? [];
        var index = Array.IndexOf(rows, folder);
        var center = Math.Floor(ActualHeight / 2) + PixelCenter;
        var x = folder.Depth * Indent;
        var pen = new Pen(Brushes.Gray, LineWidth) { DashStyle = DashStyles.Dot };
        for (var depth = 0; depth < folder.Depth; depth++)
        {
            var branch = depth * Indent + BoxSize / 2;
            var next = index < 0 ? null : rows.Skip(index + 1).FirstOrDefault(item => item.Depth <= depth + 1);
            if (depth == folder.Depth - 1)
            {
                drawing.DrawLine(pen, new Point(branch, 0), new Point(branch, center));
                drawing.DrawLine(pen, new Point(branch, center), new Point(x + BoxSize + Gap, center));
            }
            if (next?.Depth == depth + 1)
                drawing.DrawLine(pen, new Point(branch, depth == folder.Depth - 1 ? center : 0), new Point(branch, ActualHeight));
        }
        if (folder.Children.Count == 0) return;
        var border = new Pen(Brushes.Gray, LineWidth);
        var top = Math.Floor(center - BoxSize / 2) + PixelCenter;
        drawing.DrawRectangle(Brushes.White, border, new Rect(x + PixelCenter, top, BoxSize - LineWidth, BoxSize - LineWidth));
        var mark = new Pen(Brushes.Black, LineWidth);
        drawing.DrawLine(mark, new Point(x + MarkInset, center), new Point(x + BoxSize - MarkInset, center));
        if (!folder.IsExpanded)
        {
            var middle = x + BoxSize / 2;
            drawing.DrawLine(mark, new Point(middle, top + MarkInset), new Point(middle, top + BoxSize - MarkInset));
        }
    }
}
