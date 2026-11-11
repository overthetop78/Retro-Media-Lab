using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Images.Models.Sectors;
using RetroMediaLab.MediaEngine.Images.Reading.Decoding;
using RetroMediaLab.MediaEngine.Images.Reading.Reconstruction.Iso;
using RetroMediaLab.MediaFileSystems.FileSystems.Acorn.Adfs;
using RetroMediaLab.MediaEngine.Images.Formats;
using RetroMediaLab.MediaEngine;
using RetroMediaLab.MediaEngine.Enums;

namespace RetroMediaLab.MediaAudit.Acorn;

public sealed class AcornFluxRecognitionTests
{
    [Theory]
    [InlineData(DiskImageFormatIds.AcornAdfs640, DiskImageFormatIds.CpcEmuDsk)]
    [InlineData(DiskImageFormatIds.AcornAdfs800, DiskImageFormatIds.CpcEmuDsk)]
    [InlineData(DiskImageFormatIds.AcornAdfs1600, DiskImageFormatIds.CpcEmuDsk)]
    [InlineData(DiskImageFormatIds.AcornAdfs640, DiskImageFormatIds.UcsdIbmMfm)]
    [InlineData(DiskImageFormatIds.AcornAdfs800, DiskImageFormatIds.UcsdIbmMfm)]
    [InlineData(DiskImageFormatIds.AcornAdfs1600, DiskImageFormatIds.UcsdIbmMfm)]
    public async Task GenericSectorContainersKeepTheGeometryDecodedFromFlux(string format, string container)
    {
        var engine = MediaEngineComposition.CreateDefault();
        var source = await AcornDosPlus800Tests.AdfWithFile(format);
        var original = ((SectorMediaImageRepresentation)source.Representation).Image;
        var encode = engine.Conversion.Registry.Resolve(source, DiskImageFormatIds.RawScp, MediaRepresentationKind.Flux)!;
        var flux = await encode.ConvertAsync(source, DiskImageFormatIds.RawScp, MediaRepresentationKind.Flux, new Dictionary<string, string>());
        var decode = engine.Conversion.Registry.Resolve(flux.Document, container, MediaRepresentationKind.Sectors)!;
        var converted = await decode.ConvertAsync(flux.Document, container, MediaRepresentationKind.Sectors, new Dictionary<string, string>());
        var image = Assert.IsType<SectorMediaImageRepresentation>(converted.Document.Representation).Image;
        Assert.Equal((original.Cylinders, original.Heads, original.SectorsPerTrack, original.BlockSize),
            (image.Cylinders, image.Heads, image.SectorsPerTrack, image.BlockSize));
        Assert.Equal(original.AvailableBlocks.SelectMany(block => block.Data), image.AvailableBlocks.OrderBy(block => block.LogicalBlock).SelectMany(block => block.Data));
        var explored = await engine.Explorer.ExploreAsync(converted.Document);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, Assert.Single(Assert.Single(explored.Volumes).FileSystem!.Entries).Content);
    }

    [Theory]
    [InlineData(DiskImageFormatIds.AcornAdfs160)]
    [InlineData(DiskImageFormatIds.AcornAdfs320)]
    [InlineData(DiskImageFormatIds.AcornAdfs640)]
    [InlineData(DiskImageFormatIds.AcornAdfs800)]
    [InlineData(DiskImageFormatIds.AcornAdfs1600)]
    public async Task AutomaticIsoRecognitionUsesAdfsStructureAndPhysicalSides(string format)
    {
        var source = await AcornDosPlus800Tests.AdfWithFile(format);
        var original = ((SectorMediaImageRepresentation)source.Representation).Image;
        var physical = Candidates(original, false);
        var addressed = physical.Where(pair => pair.Key.Head == pair.Value[0].Sector.Head).ToDictionary(pair => pair.Key, pair => pair.Value);
        var policy = new AutomaticIsoScpSectorImagePolicy();
        var image = policy.Build(null, new(addressed, physical));
        Assert.Equal(format, image.FormatId);
        Assert.NotNull(new DiskClassificationCatalog(new BuiltInImageFormatCatalog().Formats).ResolveFormat(format));
        Assert.Equal(original.AvailableBlocks.Count, image.AvailableBlocks.Count);
        Assert.Equal(original.AvailableBlocks.SelectMany(block => block.Data), image.AvailableBlocks.OrderBy(block => block.LogicalBlock).SelectMany(block => block.Data));
        Assert.True(new AcornAdfsFileSystemReader().CanRead(image));
        var empty = Candidates(original, true);
        var unidentified = policy.Build(null, new(empty, empty));
        Assert.False(unidentified.FormatId.StartsWith(DiskImageFormatIds.AcornAdfsPrefix, StringComparison.OrdinalIgnoreCase));
    }

    private static Dictionary<SectorAddress, List<IsoSectorCandidate>> Candidates(SectorImage image, bool clear)
        => image.AvailableBlocks.ToDictionary(block => block.Address, block => new List<IsoSectorCandidate>
        {
            new(new DecodedSector((byte)block.Address.Cylinder, (byte)(block.RecordedAddress?.Head ?? block.Address.Head),
                block.Address.Number, (byte)(image.BlockSize == 256 ? 1 : 3), image.BlockSize, true,
                block.Address.Number * image.BlockSize * 16, Data: clear ? new byte[image.BlockSize] : block.Data), 0)
        });
}
