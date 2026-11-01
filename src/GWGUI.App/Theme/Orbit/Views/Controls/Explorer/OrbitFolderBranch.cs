using System.Windows;
using System.Windows.Media;
using GWGUI.App.ViewModels.Explorer;

namespace GWGUI.App.Theme.Orbit.Views;

/// <summary>Indentation and a chevron only for folders which can expand.</summary>
internal sealed class OrbitFolderBranch : FrameworkElement
{
    private const double Indent = 16;
    private const double ChevronWidth = 12;
    private const double RowHeight = 24;
    private const double StrokeWidth = 1.5;
    private const double ChevronInset = 3;
    public OrbitFolderBranch() => SnapsToDevicePixels = true;

    public static readonly DependencyProperty FolderProperty = DependencyProperty.Register(
        nameof(Folder), typeof(ExplorerFolderItem), typeof(OrbitFolderBranch),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));
    public ExplorerFolderItem? Folder
    {
        get => (ExplorerFolderItem?)GetValue(FolderProperty);
        set => SetValue(FolderProperty, value);
    }

    internal bool IsExpanderHit(Point point) => Folder is { Children.Count: > 0 } folder
        && point.X >= folder.Depth * Indent && point.X <= folder.Depth * Indent + ChevronWidth
        && point.Y >= 0 && point.Y <= ActualHeight;

    protected override Size MeasureOverride(Size availableSize) => new((Folder?.Depth ?? 0) * Indent + ChevronWidth, RowHeight);

    protected override void OnRender(DrawingContext drawing)
    {
        base.OnRender(drawing);
        if (Folder is not { Children.Count: > 0 } folder) return;
        var pen = new Pen((Brush)FindResource(OrbitMetrics.MutedBrush), StrokeWidth);
        var left = folder.Depth * Indent + ChevronInset;
        var middle = ActualHeight / 2;
        if (folder.IsExpanded)
        {
            drawing.DrawLine(pen, new(left, middle - ChevronInset), new(left + ChevronInset, middle));
            drawing.DrawLine(pen, new(left + ChevronInset, middle), new(left + ChevronInset * 2, middle - ChevronInset));
        }
        else
        {
            drawing.DrawLine(pen, new(left, middle - ChevronInset), new(left + ChevronInset, middle));
            drawing.DrawLine(pen, new(left + ChevronInset, middle), new(left, middle + ChevronInset));
        }
    }
}
