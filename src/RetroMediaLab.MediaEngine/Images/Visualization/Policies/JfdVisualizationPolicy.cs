using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Images.Models.Sectors;
using RetroMediaLab.MediaEngine.Images.Writing.Encoding;

namespace RetroMediaLab.MediaEngine.Images.Visualization.Policies;

/// <summary>Uses the recorded JFD density for ordinary sector tracks, without guessing special layouts.</summary>
internal sealed class JfdVisualizationPolicy : SectorImageVisualizationPolicy
{
    public override bool CanHandle(SectorImage image) => image.FormatId == DiskImageFormatIds.Jfd
        && image.AvailableBlocks.Count > 0 && image.AvailableBlocks.Select(block => block.FormatCode).Distinct().Count() == 1
        && image.AvailableBlocks.All(block => block.Tag is { Count: 4 } tag && tag[3] == 255);
    public override string EncoderId(SectorImage image) => image.AvailableBlocks.First().FormatCode == 1 ? FluxCodecIds.IsoFm : FluxCodecIds.IsoMfm;
    public override uint BitCellTicks(SectorImage image, int cylinder) => 160u / image.AvailableBlocks.First().FormatCode!.Value;
}
