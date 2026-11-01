using System.Windows;
using System.Windows.Controls;
using GWGUI.App.Contracts.Theming;
using GWGUI.App.Views.Controls.Visualization;
using GWGUI.MediaEngine.Enums;
using SkiaSharp.Views.WPF;

namespace GWGUI.App.Theme.Common;

/// <summary>Existing drawing leaves and actions; never a presentation container to copy.</summary>
internal static class MediaVisualizationElements
{
    internal sealed record Surface(ThemeElementSlot Document, FrameworkElement Source,
        Button ZoomOut, Button ResetZoom, Button ZoomIn, TextBlock Selection,
        TextBlock? Caption = null, SequentialMediaView? Timeline = null);

    internal static IReadOnlyList<Surface> Surfaces(VisualizerTabSection source, MediaRepresentationKind kind)
    {
        if (!source.RegisteredRepresentationViews.TryGetValue(kind, out var view)) return [];
        return kind switch
        {
            MediaRepresentationKind.Flux => [Flux(source.FirstSide, nameof(source.FirstSide)), Flux(source.SecondSide, nameof(source.SecondSide))],
            MediaRepresentationKind.Sectors when view is SectorMediaView sectors =>
            [
                Create(sectors.Canvas0, sectors, sectors.Side0Panel, sectors.ZoomOut0Button,
                    sectors.ResetZoom0Button, sectors.ZoomIn0Button, sectors.Selection0Label, sectors.Surface0Label),
                Create(sectors.Canvas1, sectors, sectors.Side1Panel, sectors.ZoomOut1Button,
                    sectors.ResetZoom1Button, sectors.ZoomIn1Button, sectors.Selection1Label, sectors.Surface1Label)
            ],
            MediaRepresentationKind.Blocks when view is BlockMediaView blocks =>
            [
                Create(blocks.LogicalCanvas, blocks, null, blocks.ZoomOutButton,
                    blocks.ResetZoomButton, blocks.ZoomInButton, blocks.SelectionLabel),
                Create(blocks.GeometryCanvas, blocks, blocks.GeometryTab, blocks.ZoomOutButton,
                    blocks.ResetZoomButton, blocks.ZoomInButton, blocks.SelectionLabel)
            ],
            MediaRepresentationKind.OpticalTracks when view is OpticalMediaView optical =>
            [Create(optical.Canvas, optical, null, optical.ZoomOutButton,
                optical.ResetZoomButton, optical.ZoomInButton, optical.SelectionLabel)],
            MediaRepresentationKind.Sequential when view is SequentialMediaView sequential =>
            [Create(sequential.Canvas, sequential, null, sequential.ZoomOutButton,
                sequential.ResetZoomButton, sequential.ZoomInButton, sequential.SelectionLabel) with { Timeline = sequential }],
            _ => []
        };
    }

    private static Surface Flux(ScpDiskView view, string id) => new(
        new ThemeElementSlot(id, view.Canvas, ThemeElementRole.Document, view), view,
        view.ZoomOutButton, view.ResetZoomButton, view.ZoomInButton, view.SelectionLabel, view.SurfaceLabel);

    private static Surface Create(SKElement canvas, FrameworkElement source, FrameworkElement? availability,
        Button zoomOut, Button resetZoom, Button zoomIn, TextBlock selection, TextBlock? caption = null)
        => new(new ThemeElementSlot(canvas.Name, canvas, ThemeElementRole.Document, availability),
            source, zoomOut, resetZoom, zoomIn, selection, caption);
}
