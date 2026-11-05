using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Adaptec;
using RetroMediaLab.MediaEngine.Images.Formats.HardDisk.MfmEmu;
using RetroMediaLab.MediaEngine.Images.Models.Blocks;
using RetroMediaLab.MediaEngine.Images.Reading.Blocks;
using RetroMediaLab.MediaEngine.Images.Visualization;
using MediaVolumeOrigins = RetroMediaLab.MediaFileSystems.Constants.MediaVolumeOrigins;
using RetroMediaLab.MediaFileSystems.Contracts;

namespace RetroMediaLab.Tests.Media;

public sealed class MfmEmuLogicalViewTests
{
    private const uint Period = 16_666_667;
    private const uint Rate = 10_000_000;
    private const int TrackBytes = 20_836;
    private const int SectorSize = 1024;
    private const int SectorCount = 9;

    [Fact]
    public async Task VisualizationRegistryPreparesTheLogicalViewWithoutFormatSelectionByItsCaller()
    {
        var representation = await Formatted();
        var source = Document(representation);
        var registry = MediaVisualizationComposition.CreateDefault().Registry;
        var view = registry.PrepareDocument(source);
        Assert.IsType<BlockMediaImageRepresentation>(view.Representation);
        Assert.Same(representation, source.Representation);
        Assert.Same(source.Source, view.Source);
        Assert.Equal(source.FormatId, view.FormatId);
        Assert.Equal(source.Metadata, view.Metadata);
        Assert.Equal(source.FileSystemVolumes.Count, view.FileSystemVolumes.Count);
        Assert.Same(source.FileSystemVolumes[0], view.FileSystemVolumes[0]);
        Assert.Equal(MediaRepresentationKind.Blocks, registry.CreateDescriptor(view).RepresentationKind);
        Assert.Same(view, registry.PrepareDocument(view));
        var data = new byte[1];
        await BlockMediaDataReader.ReadExactlyAsync((BlockMediaImageRepresentation)view.Representation, SectorSize, data);
        Assert.Equal(1, data[0]);
    }

    [Fact]
    public void VisualizationRegistryKeepsTheBlankPhysicalDocumentAndItsFluxDescriptor()
    {
        var source = Document(MfmEmuImageRepresentation.CreateUnformatted(1, 1, TrackBytes, Rate));
        var registry = MediaVisualizationComposition.CreateDefault().Registry;
        Assert.Same(source, registry.PrepareDocument(source));
        Assert.Equal(MediaRepresentationKind.Flux, registry.CreateDescriptor(source).RepresentationKind);
    }

    [Fact]
    public async Task LogicalViewKeepsSourceVolumesAndPhysicalDocumentWhileSharingDecodedReads()
    {
        var representation = await Formatted();
        var source = Document(representation);
        var view = representation.CreateLogicalView(source);
        Assert.NotNull(view);
        Assert.Same(source.Source, view.Source);
        Assert.Equal(source.FormatId, view.FormatId);
        Assert.Equal(source.Volumes[0].Length, view.Volumes[0].Length);
        Assert.Equal(source.Diagnostics, view.Diagnostics);
        Assert.Equal(source.Metadata, view.Metadata);
        Assert.Same(representation, source.Representation);
        var blocks = Assert.IsType<BlockMediaImageRepresentation>(view.Representation);
        Assert.Equal(SectorCount * SectorSize, blocks.Capacity);
        Assert.NotNull(blocks.Geometry);
        var data = new byte[50];
        await BlockMediaDataReader.ReadExactlyAsync(blocks, SectorSize - 20, data);
        Assert.All(data[..20], value => Assert.Equal(0, value));
        Assert.All(data[20..], value => Assert.Equal(1, value));
    }

    [Fact]
    public void BlankPhysicalImageDoesNotBecomeAStoredLogicalDisk()
    {
        var representation = MfmEmuImageRepresentation.CreateUnformatted(1, 1, TrackBytes, Rate);
        Assert.Equal(0L, representation.LogicalLength);
        Assert.Null(representation.CreateLogicalView(Document(representation)));
    }

    [Fact]
    public async Task AProjectionCannotBeAttachedToAnotherPhysicalDocument()
    {
        var representation = await Formatted();
        var another = MfmEmuImageRepresentation.CreateUnformatted(1, 1, TrackBytes, Rate);
        Assert.Throws<ArgumentException>(() => representation.CreateLogicalView(Document(another)));
    }

    private static MediaImageDocument Document(MfmEmuImageRepresentation representation) => new(
        new("virtual/sample.emu", []), HardDiskImageFormatIds.MfmEmu, MediaKind.HardDisk, representation,
        [new MediaVolumeDescriptor(0, SectorSize * SectorCount, MediaVolumeOrigins.DirectVolume)],
        ["preserved"], new Dictionary<string, string> { ["example"] = "preserved" });

    private static async Task<MfmEmuImageRepresentation> Formatted()
    {
        int[] addresses = [0, 2, 4, 6, 8, 1, 3, 5, 7];
        var sectors = addresses.Select(address => new MediaDiskSector(address,
            Enumerable.Repeat((byte)address, SectorSize).ToArray(), 0, 0, address)).ToArray();
        var track = AdaptecMfmTrackCodec.Encode(sectors, false, Period, parameterFlags: [0, 8, 0, 0, 0, 0, 0, 0, 0]);
        uint time = 0;
        var transitions = track.Revolutions[0].FluxIntervalsNanoseconds.Select(interval => time += interval).ToArray();
        return await MfmEmuImageRepresentation.CreateUnformatted(1, 1, TrackBytes, Rate)
            .WithTrackWindowAsync(0, 0, Period, 0, Period, transitions);
    }
}
