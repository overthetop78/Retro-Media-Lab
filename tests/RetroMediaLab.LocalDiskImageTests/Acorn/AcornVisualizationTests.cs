using RetroMediaLab.App.Presenters.Visualization;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Images.Models.Sectors;
using RetroMediaLab.App.Enums.Rendering.Sectors;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Images.Creation;
using RetroMediaLab.App.Rendering.Sectors;
using SkiaSharp;

namespace RetroMediaLab.MediaAudit.Acorn;

public sealed class AcornVisualizationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AdfSectorViewShowsStoredSectorsAndKeepsFilePathsSeparate(bool hasFile)
    {
        var blank = BlankSectorImageFactory.Create(DiskImageFormatIds.AcornAdfs160);
        var blocks = blank.AvailableBlocks.Select(block => block with { Data = block.Data.ToArray() }).ToArray();
        if (hasFile) ((byte[])blocks.Single(block => block.LogicalBlock == 7).Data)[0] = 0x42;
        var image = new SectorImage(blank.FormatId, blank.BlockSize, blank.Cylinders, blank.Heads,
            blank.SectorsPerTrack, blocks);
        var paths = new Dictionary<int, string>();
        if (hasFile) paths.Add(7, "$.ONE");
        var presenter = new SectorMediaInspectorPresenter((key, _) => key);
        var model = presenter.BuildRenderModel(image, paths);
        var sectors = Assert.Single(model.Surfaces).Tracks.SelectMany(track => track.Sectors).ToArray();
        Assert.Equal(image.BlockCount, sectors.Length);
        Assert.All(sectors, sector => Assert.Equal(SectorMediaElementState.WithData, sector.State));
        Assert.Equal(hasFile ? "$.ONE" : null, sectors.Single(sector => sector.LogicalBlock == 7).FileSystemPath);
        Assert.Null(sectors.Single(sector => sector.LogicalBlock == 8).FileSystemPath);
        if (hasFile)
            Assert.Contains(presenter.BuildInspectorModel(0, sectors.Single(sector => sector.LogicalBlock == 7))
                .Sections.SelectMany(section => section.Entries), entry => entry.Value == "$.ONE");

        SKBitmap? bitmap = null;
        SKCanvas? canvas = null;
        try
        {
            bitmap = new SKBitmap(800, 800);
            canvas = new SKCanvas(bitmap);
            var renderer = new SkiaSectorMediaRenderer();
            renderer.Render(canvas, model, 0, MediaVisualizationDirection.Ascending, null, 800, 800);
            Assert.NotEqual(bitmap.GetPixel(0, 0), bitmap.GetPixel(700, 400));
            Assert.NotNull(renderer.HitTest(model, 0, MediaVisualizationDirection.Ascending, 800, 800, new(700, 400)));
        }
        finally
        {
            canvas?.Dispose();
            bitmap?.Dispose();
        }
    }

}
