using System.IO;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Images.Formats;
using RetroMediaLab.MediaEngine.Images.Formats.Detection;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Adf;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Raw;
using RetroMediaLab.MediaEngine.Images.Writing.Encoding;
using RetroMediaLab.MediaEngine.Interfaces;
using RetroMediaLab.MediaEngine.Interfaces.Reading;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Images.Creation;
using RetroMediaLab.MediaEngine.Images.Reading.Recognition;
using RetroMediaLab.MediaEngine.Images.Conversion.Acorn;
using System.Buffers.Binary;
using RetroMediaLab.MediaEngine;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Images.Reading;
using RetroMediaLab.MediaEngine.Images.Models.Sectors;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Jfd;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.DiskCopy;
using RetroMediaLab.MediaEngine.Images.Reading.Reconstruction.Sectors.Apple;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.CpcDsk;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.TeleDisk;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.St;

namespace RetroMediaLab.MediaAudit.Acorn;

public sealed class AcornDosPlus800Tests
{
    [Theory]
    [InlineData(0)]
    [InlineData(17)]
    public async Task PartialAdfKeepsItsAvailableBytesInCpcAndOnlyOffersRepresentableTd0(int tail)
    {
        var source = await AdfWithFile(DiskImageFormatIds.AcornAdfs800);
        var bytes = ((SectorMediaImageRepresentation)source.Representation).Image.AvailableBlocks
            .OrderBy(block => block.LogicalBlock).SelectMany(block => block.Data).Take(8192 + tail).ToArray();
        var image = await new AdfReader((_, _) => Task.FromResult(bytes)).ReadAsync("partial.adf");
        var written = new MemoryWriter();
        await new CpcDskWriter(written).WriteAsync(CpcDskImageBuilder.Build(image, CpcDskContainerKind.Extended), "partial.edsk");
        var decoded = await new CpcDskReader().ReadAsync(written.Bytes.AsMemory());
        Assert.Equal(bytes, decoded.AvailableBlocks.OrderBy(block => block.LogicalBlock).SelectMany(block => block.Data));
        var destinations = MediaEngineComposition.CreateDefault().ConversionService.GetAvailableDestinations(
            MediaImageDocumentFactory.CreateFloppySector(new("partial.adf", []), image));
        Assert.Contains(destinations, item => item.WriterId == MediaImageWriterIds.AmstradDsk && item.Extension == ".edsk");
        Assert.Equal(tail == 0, destinations.Any(item => item.WriterId == MediaImageWriterIds.Ucsd && item.Extension == ".td0"));
        if (tail == 0) await new Td0Writer(new MemoryWriter()).WriteAsync(image, "partial.td0");
    }

    [Theory]
    [InlineData(DiskImageFormatIds.AcornAdfs160)]
    [InlineData(DiskImageFormatIds.AcornAdfs320)]
    [InlineData(DiskImageFormatIds.AcornAdfs640)]
    [InlineData(DiskImageFormatIds.AcornAdfs800)]
    [InlineData(DiskImageFormatIds.AcornAdfs1600)]
    public async Task PartialAdfDoesNotOfferWritersThatRequireEveryBlock(string format)
    {
        var original = await AdfWithFile(format);
        var bytes = ((SectorMediaImageRepresentation)original.Representation).Image.AvailableBlocks
            .OrderBy(block => block.LogicalBlock).SelectMany(block => block.Data)
            .Take(format == DiskImageFormatIds.AcornAdfs1600 ? 1_000_000 : 8192).ToArray();
        var image = await new AdfReader((_, _) => Task.FromResult(bytes)).ReadAsync("partial.adf");
        Assert.Equal(format, image.FormatId);
        Assert.True(image.AvailableBlocks.Count < image.BlockCount);
        var document = MediaImageDocumentFactory.CreateFloppySector(new("partial.adf", []), image);
        var destinations = MediaEngineComposition.CreateDefault().ConversionService.GetAvailableDestinations(document);
        Assert.DoesNotContain(destinations, item => item.WriterId is MediaImageWriterIds.AcornAdf or MediaImageWriterIds.IbmRaw or "defined-img");
    }

    [Theory]
    [InlineData(DiskImageFormatIds.AcornAdfs160, DiskImageFormatIds.Ibm160)]
    [InlineData(DiskImageFormatIds.AcornAdfs320, DiskImageFormatIds.Ibm320)]
    [InlineData(DiskImageFormatIds.AcornAdfs640, "atarist.640")]
    [InlineData(DiskImageFormatIds.AcornAdfs800, DiskImageFormatIds.Ibm800)]
    public async Task RawSectorInterpretationsCanReturnToTheAdfIdentifiedByTheirStructure(string format, string rawFormat)
    {
        var source = await AdfWithFile(format);
        var expected = ((SectorMediaImageRepresentation)source.Representation).Image.AvailableBlocks
            .OrderBy(block => block.LogicalBlock).SelectMany(block => block.Data).ToArray();
        var raw = rawFormat.StartsWith(DiskImageFormatIds.AtariStPrefix, StringComparison.OrdinalIgnoreCase)
            ? await new AtariStReader((_, _) => Task.FromResult(expected)).ReadAsync("source.img")
            : await new IbmRawImageReader((_, _) => Task.FromResult(expected)).ReadAsync("source.img");
        Assert.Equal(rawFormat, raw.FormatId);
        var document = MediaImageDocumentFactory.CreateFloppySector(new("source.img", []), raw);
        var engine = MediaEngineComposition.CreateDefault();
        Assert.Contains(engine.ConversionService.GetAvailableDestinations(document), item => item.FormatId == format && item.Extension == ".adf");
        var converter = engine.Conversion.Registry.Resolve(document, format, MediaRepresentationKind.Sectors);
        Assert.NotNull(converter);
        var result = await converter.ConvertAsync(document, format, MediaRepresentationKind.Sectors, new Dictionary<string, string>());
        Assert.NotEmpty(result.Losses);
        var written = new MemoryWriter();
        await new AcornAdfWriter(new LinearSectorImageWriter(written)).WriteAsync(((SectorMediaImageRepresentation)result.Document.Representation).Image, "returned.adf");
        Assert.Equal(expected, written.Bytes);
        var explored = await engine.Explorer.ExploreAsync(result.Document);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, Assert.Single(Assert.Single(explored.Volumes).FileSystem!.Entries).Content);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public async Task RawAdfReturnRejectsDamagedOrIncompleteImages(int damage)
    {
        var source = await AdfWithFile(DiskImageFormatIds.AcornAdfs160);
        var bytes = ((SectorMediaImageRepresentation)source.Representation).Image.AvailableBlocks
            .OrderBy(block => block.LogicalBlock).SelectMany(block => block.Data).ToArray();
        if (damage == 0) bytes[255] ^= 1;
        if (damage == 1) bytes[513] = 0;
        var shape = BlankSectorImageFactory.Create(DiskImageFormatIds.Ibm160);
        var blocks = shape.AvailableBlocks.Select(block => block with { Data = bytes.AsSpan(block.LogicalBlock * 512, 512).ToArray() })
            .Select(block => block.LogicalBlock != 0 ? block : damage switch
            {
                3 => block with { IntegrityValid = false },
                4 => block with { Tag = new byte[] { 1 } },
                5 => block with { RecordedSizeCode = 2 },
                _ => block
            }).Where(block => damage != 2 || block.LogicalBlock != 0).ToArray();
        var raw = new SectorImage(shape.FormatId, 512, shape.Cylinders, shape.Heads, shape.SectorsPerTrack, blocks);
        var document = MediaImageDocumentFactory.CreateFloppySector(new("damaged.img", []), raw);
        Assert.Null(MediaEngineComposition.CreateDefault().Conversion.Registry.Resolve(document, DiskImageFormatIds.AcornAdfs160, MediaRepresentationKind.Sectors));
    }

    [Theory]
    [InlineData(DiskImageFormatIds.Ibm160, DiskImageFormatIds.AcornAdfs160)]
    [InlineData(DiskImageFormatIds.Ibm320, DiskImageFormatIds.AcornAdfs320)]
    [InlineData("atarist.640", DiskImageFormatIds.AcornAdfs640)]
    [InlineData(DiskImageFormatIds.Ibm800, DiskImageFormatIds.AcornAdfs800)]
    public async Task RawCapacityAloneCannotOfferAcornAdf(string rawFormat, string targetFormat)
    {
        var bytes = new byte[AcornAdfGeometry.All.Single(item => item.FormatId == targetFormat).Capacity];
        var image = rawFormat.StartsWith(DiskImageFormatIds.AtariStPrefix, StringComparison.OrdinalIgnoreCase)
            ? await new AtariStReader((_, _) => Task.FromResult(bytes)).ReadAsync("native.img")
            : await new IbmRawImageReader((_, _) => Task.FromResult(bytes)).ReadAsync("native.img");
        var source = MediaImageDocumentFactory.CreateFloppySector(new("native.img", []), image);
        Assert.Null(MediaEngineComposition.CreateDefault().Conversion.Registry.Resolve(source, targetFormat, MediaRepresentationKind.Sectors));
    }

    [Theory]
    [InlineData(DiskImageFormatIds.AcornAdfs160)]
    [InlineData(DiskImageFormatIds.AcornAdfs320)]
    [InlineData(DiskImageFormatIds.AcornAdfs640)]
    [InlineData(DiskImageFormatIds.AcornAdfs800)]
    [InlineData(DiskImageFormatIds.AcornAdfs1600)]
    public async Task Td0CanReturnToAdfWithPhysicalAndRecordedAddressesAndRatePreserved(string format)
    {
        var source = await AdfWithFile(format);
        var image = ((SectorMediaImageRepresentation)source.Representation).Image;
        var output = new MemoryWriter();
        await new Td0Writer(output).WriteAsync(image, "memory.td0");
        var decoded = Td0Reader.ReadDetailed(output.Bytes);
        Assert.Equal(format == DiskImageFormatIds.AcornAdfs1600 ? 2 : 0, decoded.Header.DataRate);
        var rewritten = new MemoryWriter();
        await new Td0Writer(rewritten).WriteAsync(decoded.SectorImage, "rewritten.td0");
        Assert.Equal(output.Bytes, rewritten.Bytes);
        foreach (var original in image.AvailableBlocks)
        {
            Assert.True(decoded.SectorImage.TryGetBlock(original.LogicalBlock, out var block));
            Assert.Equal(original.Address, block.Address);
            Assert.Equal(original.RecordedAddress ?? original.Address, block.RecordedAddress ?? block.Address);
            Assert.Equal(original.Data, block.Data);
        }
        var document = MediaImageDocumentFactory.CreateFloppySector(new("memory.td0", []), decoded.SectorImage);
        var engine = MediaEngineComposition.CreateDefault();
        var nativeExploration = await engine.Explorer.ExploreAsync(document);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, Assert.Single(Assert.Single(nativeExploration.Volumes).FileSystem!.Entries).Content);
        Assert.Contains(engine.ConversionService.GetAvailableDestinations(document), item => item.FormatId == format && item.Extension == ".adf");
        var converter = engine.Conversion.Registry.Resolve(document, format, MediaRepresentationKind.Sectors);
        Assert.NotNull(converter);
        var result = await converter.ConvertAsync(document, format, MediaRepresentationKind.Sectors, new Dictionary<string, string>());
        Assert.NotEmpty(result.Losses);
        var adf = new MemoryWriter();
        await new AcornAdfWriter(new LinearSectorImageWriter(adf)).WriteAsync(((SectorMediaImageRepresentation)result.Document.Representation).Image, "returned.adf");
        Assert.Equal(image.AvailableBlocks.OrderBy(block => block.LogicalBlock).SelectMany(block => block.Data), adf.Bytes);
        var explored = await engine.Explorer.ExploreAsync(result.Document);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, Assert.Single(Assert.Single(explored.Volumes).FileSystem!.Entries).Content);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(16)]
    [InlineData(32)]
    [InlineData(64)]
    public async Task Td0CannotHideNativeSectorConditionsInAdf(byte flags)
    {
        var source = await AdfWithFile(DiskImageFormatIds.AcornAdfs800);
        var encoded = new MemoryWriter();
        await new Td0Writer(encoded).WriteAsync(((SectorMediaImageRepresentation)source.Representation).Image, "memory.td0");
        var native = Td0Reader.ReadDetailed(encoded.Bytes);
        var tracks = native.Tracks.ToArray();
        var sectors = tracks[0].Sectors.ToArray();
        sectors[0] = sectors[0] with { Flags = flags, Data = (flags & 0x30) != 0 ? null : sectors[0].Data };
        tracks[0] = tracks[0] with { Sectors = sectors };
        await new Td0Writer(encoded).WriteAsync(native with { Tracks = tracks }, "changed.td0");
        var document = MediaImageDocumentFactory.CreateFloppySector(new("changed.td0", []), Td0Reader.ReadDetailed(encoded.Bytes).SectorImage);
        Assert.Null(MediaEngineComposition.CreateDefault().Conversion.Registry.Resolve(document, DiskImageFormatIds.AcornAdfs800, MediaRepresentationKind.Sectors));
    }

    [Theory]
    [InlineData(DiskImageFormatIds.AcornAdfs160)]
    [InlineData(DiskImageFormatIds.AcornAdfs320)]
    [InlineData(DiskImageFormatIds.AcornAdfs640)]
    [InlineData(DiskImageFormatIds.AcornAdfs800)]
    [InlineData(DiskImageFormatIds.AcornAdfs1600)]
    public async Task CpcDskContainersCanReturnToAdfWithoutChangingSectorsOrTheFile(string format)
    {
        var original = await AdfWithFile(format);
        var sectors = ((SectorMediaImageRepresentation)original.Representation).Image;
        var engine = MediaEngineComposition.CreateDefault();
        foreach (var kind in new[] { CpcDskContainerKind.Standard, CpcDskContainerKind.Extended })
        {
            var output = new MemoryWriter();
            await new CpcDskWriter(output).WriteAsync(CpcDskImageBuilder.Build(sectors, kind), "memory.dsk");
            var decoded = await new CpcDskReader().ReadAsync(output.Bytes.AsMemory());
            var document = MediaImageDocumentFactory.CreateFloppySector(new("memory.dsk", []), decoded);
            var nativeExploration = await engine.Explorer.ExploreAsync(document);
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, Assert.Single(Assert.Single(nativeExploration.Volumes).FileSystem!.Entries).Content);
            Assert.Contains(engine.ConversionService.GetAvailableDestinations(document), item => item.FormatId == format && item.Extension == ".adf");
            var converter = engine.Conversion.Registry.Resolve(document, format, MediaRepresentationKind.Sectors);
            Assert.NotNull(converter);
            var result = await converter.ConvertAsync(document, format, MediaRepresentationKind.Sectors, new Dictionary<string, string>());
            Assert.NotEmpty(result.Losses);
            var restored = Assert.IsType<SectorMediaImageRepresentation>(result.Document.Representation).Image;
            var adf = new MemoryWriter();
            await new AcornAdfWriter(new LinearSectorImageWriter(adf)).WriteAsync(restored, "returned.adf");
            Assert.Equal(sectors.AvailableBlocks.OrderBy(block => block.LogicalBlock).SelectMany(block => block.Data), adf.Bytes);
            foreach (var block in sectors.AvailableBlocks)
            {
                Assert.True(restored.TryGetBlock(block.LogicalBlock, out var returned));
                Assert.Equal(block.Address, returned.Address);
                Assert.Equal(block.RecordedAddress ?? block.Address, returned.RecordedAddress ?? returned.Address);
                Assert.Equal(block.Data, returned.Data);
            }
            var explored = await engine.Explorer.ExploreAsync(result.Document);
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, Assert.Single(Assert.Single(explored.Volumes).FileSystem!.Entries).Content);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task CpcDskRetainsNativeStatusesAndExtendedDataInsteadOfHidingThemInAdf(int variant)
    {
        var source = await AdfWithFile(DiskImageFormatIds.AcornAdfs800);
        var container = CpcDskImageBuilder.Build(((SectorMediaImageRepresentation)source.Representation).Image, CpcDskContainerKind.Extended);
        var tracks = container.Tracks.ToArray();
        var sectors = tracks[0].Sectors.ToArray();
        sectors[0] = variant switch
        {
            0 => sectors[0] with { Status1 = 0x04 },
            1 => sectors[0] with { Status2 = 0x10 },
            _ => sectors[0] with { Data = sectors[0].Data.Concat(sectors[0].Data).ToArray() }
        };
        tracks[0] = tracks[0] with { Sectors = sectors };
        var original = new MemoryWriter();
        await new CpcDskWriter(original).WriteAsync(container with { Tracks = tracks }, "native.edsk");
        var image = await new CpcDskReader().ReadAsync(original.Bytes.AsMemory());
        var document = MediaImageDocumentFactory.CreateFloppySector(new("native.edsk", []), image);
        Assert.Null(MediaEngineComposition.CreateDefault().Conversion.Registry.Resolve(document, DiskImageFormatIds.AcornAdfs800, MediaRepresentationKind.Sectors));
        var roundtrip = new MemoryWriter();
        await new CpcDskWriter(roundtrip).WriteAsync(CpcDskImageBuilder.Build(image, CpcDskContainerKind.Extended), "roundtrip.edsk");
        Assert.Equal(original.Bytes, roundtrip.Bytes);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public async Task CpcDskCannotHideMissingOrIncompatibleSectorsInAdf(int damage)
    {
        var source = await AdfWithFile(DiskImageFormatIds.AcornAdfs800);
        var output = new MemoryWriter();
        await new CpcDskWriter(output).WriteAsync(CpcDskImageBuilder.Build(((SectorMediaImageRepresentation)source.Representation).Image, CpcDskContainerKind.Extended), "memory.edsk");
        var image = await new CpcDskReader().ReadAsync(output.Bytes.AsMemory());
        var blocks = image.AvailableBlocks.Select(block => block.LogicalBlock != 0 ? block : damage switch
        {
            0 => block with { IntegrityValid = false },
            1 => block with { HeaderIntegrityValid = false },
            2 => block with { DataIntegrityValid = false },
            3 => block with { DeletedDataMark = true },
            4 => block with { RecordedAddress = block.Address with { Head = 1 } },
            5 => block with { RecordedSizeCode = 1 },
            6 => block with { FormatCode = 1 },
            _ => block
        }).Where(block => damage != 7 || block.LogicalBlock != 0).ToArray();
        var document = MediaImageDocumentFactory.CreateFloppySector(new("damaged.edsk", []),
            new SectorImage(image.FormatId, image.BlockSize, image.Cylinders, image.Heads, image.SectorsPerTrack, blocks));
        Assert.Null(MediaEngineComposition.CreateDefault().Conversion.Registry.Resolve(document, DiskImageFormatIds.AcornAdfs800, MediaRepresentationKind.Sectors));
    }

    [Theory]
    [InlineData(DiskImageFormatIds.AcornAdfs160)]
    [InlineData(DiskImageFormatIds.AcornAdfs320)]
    [InlineData(DiskImageFormatIds.AcornAdfs640)]
    [InlineData(DiskImageFormatIds.AcornAdfs800)]
    [InlineData(DiskImageFormatIds.AcornAdfs1600)]
    public async Task AdfDoesNotOfferIncompatibleMacintoshOrDiskCopyContainers(string format)
    {
        var destinations = MediaEngineComposition.CreateDefault().ConversionService.GetAvailableDestinations(await AdfWithFile(format));
        Assert.DoesNotContain(destinations, item => item.WriterId is MediaImageWriterIds.Macintosh or MediaImageWriterIds.LisaDiskCopy);
    }

    [Theory]
    [InlineData(DiskImageFormatIds.Mac400, 1)]
    [InlineData(DiskImageFormatIds.Mac800, 2)]
    [InlineData(DiskImageFormatIds.Mac1440, 0)]
    public async Task MacintoshNativeContainersRemainWritable(string format, int heads)
    {
        var image = heads == 0
            ? BlankSectorImageFactory.Create(DiskImageFormatIds.Ibm1440).WithFormatId(format)
            : MacintoshGcrSectorImageBuilder.Create(new byte[MacintoshGcrGeometry.SingleSidedBlockCount * heads * MacintoshGcrGeometry.BlockSize],
                format, MacintoshGcrGeometry.ForHeads(heads));
        // Macintosh MFM uses zero-based sector addresses, unlike the IBM fixture.
        if (heads == 0)
            image = new SectorImage(format, image.BlockSize, image.Cylinders, image.Heads, image.SectorsPerTrack,
                image.AvailableBlocks.Select(block => block with { Address = block.Address with { Number = block.Address.Number - 1 } }));
        var source = MediaImageDocumentFactory.CreateFloppySector(new("native.img", []), image);
        var destinations = MediaEngineComposition.CreateDefault().ConversionService.GetAvailableDestinations(source);
        foreach (var extension in new[] { ".img", ".dc42" })
            Assert.Contains(destinations, item => item.WriterId == MediaImageWriterIds.Macintosh && item.FormatId == format && item.Extension == extension);
        var raw = new MemoryWriter();
        await new MacintoshRawImageWriter(raw).WriteAsync(image, "memory.img");
        Assert.Equal(image.AvailableBlocks.OrderBy(block => block.LogicalBlock).SelectMany(block => block.Data), raw.Bytes);
        var diskCopy = new MemoryWriter();
        await new DiskCopyWriter(diskCopy).WriteAsync(image, "memory.dc42");
        Assert.Equal(raw.Bytes, diskCopy.Bytes[DiskCopyLayout.HeaderSize..]);
    }

    [Theory]
    [InlineData(DiskImageFormatIds.AcornAdfs160)]
    [InlineData(DiskImageFormatIds.AcornAdfs320)]
    [InlineData(DiskImageFormatIds.AcornAdfs640)]
    [InlineData(DiskImageFormatIds.AcornAdfs800)]
    [InlineData(DiskImageFormatIds.AcornAdfs1600)]
    public async Task AdfDoesNotOfferIncompatibleAppleAtariOrCoherentContainers(string format)
    {
        var destinations = MediaEngineComposition.CreateDefault().ConversionService.GetAvailableDestinations(await AdfWithFile(format));
        Assert.DoesNotContain(destinations, item => item.WriterId is MediaImageWriterIds.AppleSector or MediaImageWriterIds.AppleNibble
            or MediaImageWriterIds.AtariSt or MediaImageWriterIds.AtariAtr or MediaImageWriterIds.CoherentRaw);
    }

    [Fact]
    public void AtariStStillAcceptsAnIbmSourceWithMatchingCapacityAndSectorSize()
    {
        var source = MediaImageDocumentFactory.CreateFloppySector(new("native.img", []), BlankSectorImageFactory.Create(DiskImageFormatIds.Ibm720));
        var destinations = MediaEngineComposition.CreateDefault().ConversionService.GetAvailableDestinations(source);
        Assert.Contains(destinations, item => item.WriterId == MediaImageWriterIds.AtariSt && item.FormatId == "atarist.720" && item.Extension == ".st");
    }

    [Theory]
    [InlineData(DiskImageFormatIds.AcornAdfs160)]
    [InlineData(DiskImageFormatIds.AcornAdfs320)]
    [InlineData(DiskImageFormatIds.AcornAdfs640)]
    [InlineData(DiskImageFormatIds.AcornAdfs800)]
    [InlineData(DiskImageFormatIds.AcornAdfs1600)]
    public async Task AdfDoesNotOfferIncompatibleFixedSectorContainers(string format)
    {
        var destinations = MediaEngineComposition.CreateDefault().ConversionService.GetAvailableDestinations(await AdfWithFile(format));
        Assert.DoesNotContain(destinations, item => item.WriterId is MediaImageWriterIds.AcornAtomDsk or MediaImageWriterIds.Apridisk
            or MediaImageWriterIds.BbcDfs or MediaImageWriterIds.MsxRaw or MediaImageWriterIds.CommodoreD81 or MediaImageWriterIds.DecRx02);
        Assert.DoesNotContain(destinations, item => item.WriterId == MediaImageWriterIds.Ucsd && item.Extension == ".img");
        Assert.Contains(destinations, item => item.WriterId == MediaImageWriterIds.Ucsd && item.Extension == ".td0");
    }

    [Theory]
    [InlineData(DiskImageFormatIds.AcornAtomDos, MediaImageWriterIds.AcornAtomDsk, ".dsk")]
    [InlineData(DiskImageFormatIds.AcornDfsSingleSided, MediaImageWriterIds.BbcDfs, ".ssd")]
    [InlineData(DiskImageFormatIds.AcornDfsDoubleSided80, MediaImageWriterIds.BbcDfs, ".dsd")]
    [InlineData(DiskImageFormatIds.Commodore1581, MediaImageWriterIds.CommodoreD81, ".d81")]
    public void FixedSectorContainersKeepTheirNativeDestinations(string format, string writer, string extension)
    {
        var source = MediaImageDocumentFactory.CreateFloppySector(new("native" + extension, []), BlankSectorImageFactory.Create(format));
        var destinations = MediaEngineComposition.CreateDefault().ConversionService.GetAvailableDestinations(source);
        Assert.Contains(destinations, item => item.FormatId == format && item.WriterId == writer && item.Extension == extension);
    }

    [Theory]
    [InlineData(DiskImageFormatIds.AcornAdfs160)]
    [InlineData(DiskImageFormatIds.AcornAdfs320)]
    [InlineData(DiskImageFormatIds.AcornAdfs640)]
    [InlineData(DiskImageFormatIds.AcornAdfs800)]
    [InlineData(DiskImageFormatIds.AcornAdfs1600)]
    public async Task AdfOffersGenericIbmRawButNotIncompatibleFixedIbmGeometries(string format)
    {
        var destinations = MediaEngineComposition.CreateDefault().ConversionService.GetAvailableDestinations(await AdfWithFile(format));
        var ibm = destinations.Where(item => item.WriterId == MediaImageWriterIds.IbmRaw).ToArray();
        Assert.Equal(2, ibm.Length);
        Assert.All(ibm, item => Assert.Equal(DiskImageFormatIds.IbmScan, item.FormatId));
        Assert.Contains(ibm, item => item.Extension == ".ima");
        Assert.Contains(ibm, item => item.Extension == ".img");
    }

    [Fact]
    public void IbmRawStillAcceptsMatchingGeometryFromAnotherFamily()
    {
        var image = BlankSectorImageFactory.Create(DiskImageFormatIds.Ibm720).WithFormatId(DiskImageFormatIds.AmigaDos);
        var document = MediaImageDocumentFactory.CreateFloppySector(new("foreign.adf", []), image);
        var destinations = MediaEngineComposition.CreateDefault().ConversionService.GetAvailableDestinations(document);
        Assert.Contains(destinations, item => item.WriterId == MediaImageWriterIds.IbmRaw && item.FormatId == DiskImageFormatIds.Ibm720 && item.Extension == ".img");
    }

    [Theory]
    [InlineData(DiskImageFormatIds.AcornAdfs160)]
    [InlineData(DiskImageFormatIds.AcornAdfs320)]
    [InlineData(DiskImageFormatIds.AcornAdfs640)]
    [InlineData(DiskImageFormatIds.AcornAdfs800)]
    [InlineData(DiskImageFormatIds.AcornAdfs1600)]
    public async Task AdfDestinationsRespectEpsonAndCommodoreSourceFormatRequirements(string format)
    {
        var destinations = MediaEngineComposition.CreateDefault().ConversionService.GetAvailableDestinations(await AdfWithFile(format));
        Assert.DoesNotContain(destinations, item => item.WriterId is MediaImageWriterIds.EpsonQx10 or MediaImageWriterIds.CommodoreDos);
    }

    [Fact]
    public async Task AdfDKeepsTheOtherNineWritableContainerDestinations()
    {
        var destinations = MediaEngineComposition.CreateDefault().ConversionService.GetAvailableDestinations(await AdfWithFile(DiskImageFormatIds.AcornAdfs800));
        foreach (var format in new[] { "amstrad.cpc", "amstrad.pcw", "cpcemu.dsk" })
            foreach (var extension in new[] { ".dsk", ".edsk" })
                Assert.Contains(destinations, item => item.FormatId == format && item.Extension == extension);
        foreach (var extension in new[] { ".ima", ".img" })
            Assert.Contains(destinations, item => item.FormatId == "ibm.scan" && item.Extension == extension);
        Assert.Contains(destinations, item => item.FormatId == "ucsd.ibm.mfm" && item.Extension == ".td0");
    }

    [Theory]
    [InlineData(DiskImageFormatIds.AcornAdfs160)]
    [InlineData(DiskImageFormatIds.AcornAdfs320)]
    [InlineData(DiskImageFormatIds.AcornAdfs640)]
    [InlineData(DiskImageFormatIds.AcornAdfs800)]
    [InlineData(DiskImageFormatIds.AcornAdfs1600)]
    public async Task AcornAdfDoesNotOfferTheAmigaAdfWriter(string format)
    {
        var source = await AdfWithFile(format);
        var destinations = MediaEngineComposition.CreateDefault().ConversionService.GetAvailableDestinations(source);
        Assert.DoesNotContain(destinations, item => item.WriterId == MediaImageWriterIds.AmigaAdf);
        Assert.Contains(destinations, item => item.WriterId == MediaImageWriterIds.AcornAdf && item.FormatId == format && item.Extension == ".adf");
    }

    [Theory]
    [InlineData(DiskImageFormatIds.AcornAdfs160)]
    [InlineData(DiskImageFormatIds.AcornAdfs320)]
    [InlineData(DiskImageFormatIds.AcornAdfs800)]
    [InlineData(DiskImageFormatIds.AcornAdfs1600)]
    public async Task JfdCanReturnToTheMatchingAdfWithoutChangingItsSectors(string format)
    {
        var original = await AdfWithFile(format);
        var sectors = ((SectorMediaImageRepresentation)original.Representation).Image;
        var decoded = JfdCodec.Decode(JfdCodec.Create(sectors));
        var document = MediaImageDocument.CreateUnexplored(new("native.jfd", []), DiskImageFormatIds.Jfd, MediaKind.Floppy,
            new SectorMediaImageRepresentation(decoded.Sectors), decoded.Diagnostics, new Dictionary<string, string>());
        var engine = MediaEngineComposition.CreateDefault();
        Assert.Contains(engine.ConversionService.GetAvailableDestinations(document), item => item.FormatId == format && item.Extension == ".adf");
        Assert.All(engine.ConversionService.GetAvailableDestinations(document).Where(item => item.FormatId == format && item.WriterId == MediaImageWriterIds.AcornAdf),
            item => Assert.True(AcornAdfConversionService.CanCreate(item.FormatId, item.Extension)));
        var converter = engine.Conversion.Registry.Resolve(document, format, MediaRepresentationKind.Sectors);
        Assert.NotNull(converter);
        var converted = await converter.ConvertAsync(document, format, MediaRepresentationKind.Sectors, new Dictionary<string, string>());
        Assert.NotEmpty(converted.Losses);
        var image = Assert.IsType<SectorMediaImageRepresentation>(converted.Document.Representation).Image;
        Assert.Equal(sectors.AvailableBlocks.OrderBy(block => block.LogicalBlock).SelectMany(block => block.Data),
            image.AvailableBlocks.OrderBy(block => block.LogicalBlock).SelectMany(block => block.Data));
        var explored = await engine.Explorer.ExploreAsync(converted.Document);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, Assert.Single(Assert.Single(explored.Volumes).FileSystem!.Entries).Content);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task JfdCannotHideMissingOrBadSectorsInAnAdf(int damage)
    {
        var original = await AdfWithFile(DiskImageFormatIds.AcornAdfs800);
        var image = JfdCodec.Decode(JfdCodec.Create(((SectorMediaImageRepresentation)original.Representation).Image)).Sectors;
        var blocks = image.AvailableBlocks.Select(block => block.LogicalBlock != 0 ? block : damage switch
        {
            2 => block with { Tag = block.Tag!.Concat(new byte[] { 1 }).ToArray() },
            3 => block with { FormatCode = 1 },
            _ => block with { IntegrityValid = false }
        }).ToArray();
        if (damage == 1) blocks = image.AvailableBlocks.Where(block => block.LogicalBlock != 0).ToArray();
        var document = MediaImageDocument.CreateUnexplored(new("damaged.jfd", []), DiskImageFormatIds.Jfd, MediaKind.Floppy,
            new SectorMediaImageRepresentation(new SectorImage(DiskImageFormatIds.Jfd, image.BlockSize, image.Cylinders, image.Heads, image.SectorsPerTrack, blocks)), [], new Dictionary<string, string>());
        Assert.Null(MediaEngineComposition.CreateDefault().Conversion.Registry.Resolve(document, DiskImageFormatIds.AcornAdfs800, MediaRepresentationKind.Sectors));
    }

    [Theory]
    [InlineData(DiskImageFormatIds.AcornAdfs160)]
    [InlineData(DiskImageFormatIds.AcornAdfs320)]
    [InlineData(DiskImageFormatIds.AcornAdfs640)]
    [InlineData(DiskImageFormatIds.AcornAdfs800)]
    [InlineData(DiskImageFormatIds.AcornAdfs1600)]
    public async Task CommonAdfFluxConversionsPreserveEverySectorAndTheFile(string format)
    {
        var source = await AdfWithFile(format);
        var engine = MediaEngineComposition.CreateDefault();
        var destinations = engine.ConversionService.GetAvailableDestinations(source);
        foreach (var target in new[] { DiskImageFormatIds.RawScp, DiskImageFormatIds.RawHfe, DiskImageFormatIds.RawHxcStream })
            Assert.Contains(destinations, item => item.FormatId == target);
        if (format == DiskImageFormatIds.AcornAdfs640)
            Assert.DoesNotContain(destinations, item => item.FormatId == DiskImageFormatIds.Jfd);
        else Assert.Contains(destinations, item => item.FormatId == DiskImageFormatIds.Jfd);
        var converter = engine.Conversion.Registry.Resolve(source, DiskImageFormatIds.RawScp, MediaRepresentationKind.Flux);
        Assert.NotNull(converter);
        var flux = await converter.ConvertAsync(source, DiskImageFormatIds.RawScp, MediaRepresentationKind.Flux, new Dictionary<string, string>());
        var reverse = engine.Conversion.Registry.Resolve(flux.Document, format, MediaRepresentationKind.Sectors);
        Assert.NotNull(reverse);
        var result = await reverse.ConvertAsync(flux.Document, format, MediaRepresentationKind.Sectors, new Dictionary<string, string>());
        var original = ((SectorMediaImageRepresentation)source.Representation).Image;
        var restored = Assert.IsType<SectorMediaImageRepresentation>(result.Document.Representation).Image;
        Assert.Equal(original.BlockCount, restored.AvailableBlocks.Count);
        foreach (var block in original.AvailableBlocks)
        {
            Assert.True(restored.TryGetBlock(block.LogicalBlock, out var found));
            Assert.Equal(block.Address, found.Address);
            Assert.Equal(block.RecordedAddress ?? block.Address, found.RecordedAddress ?? found.Address);
            Assert.Equal(block.Data, found.Data);
        }
        var explored = await engine.Explorer.ExploreAsync(result.Document);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, Assert.Single(Assert.Single(explored.Volumes).FileSystem!.Entries).Content);
    }

    internal static async Task<MediaImageDocument> AdfWithFile(string format)
    {
        byte[] bytes;
        if (format == DiskImageFormatIds.AcornAdfs1600) bytes = AcornStorageImageTests.CreateFFormatBytes(true);
        else
        {
            var blank = BlankSectorImageFactory.Create(format);
            bytes = blank.AvailableBlocks.OrderBy(block => block.LogicalBlock).SelectMany(block => block.Data).ToArray();
            var root = blank.BlockSize == 256 ? 512 : 1024;
            "ONE"u8.CopyTo(bytes.AsSpan(root + 5));
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(root + 23), 4);
            bytes[root + 27] = (byte)(7 * blank.BlockSize / 256);
            new byte[] { 1, 2, 3, 4 }.CopyTo(bytes, 7 * blank.BlockSize);
            var freeStart = 8 * blank.BlockSize / 256;
            var freeLength = bytes.Length / 256 - freeStart;
            bytes[0] = (byte)freeStart;
            bytes[256] = (byte)freeLength; bytes[257] = (byte)(freeLength >> 8); bytes[258] = (byte)(freeLength >> 16);
            for (var sector = 0; sector < 2; sector++)
            {
                var sum = 0;
                for (var offset = 254; offset >= 0; offset--) sum = (sum & 255) + (sum >> 8) + bytes[sector * 256 + offset];
                bytes[sector * 256 + 255] = (byte)sum;
            }
            if (blank.BlockSize == 1024) AcornStorageImageTests.UpdateNickDirectoryCheck(bytes.AsSpan(root, 2048), 1);
        }
        bytes[^1] = 0x81;
        var image = await new AdfReader((_, _) => Task.FromResult(bytes)).ReadAsync("fixture.adf");
        return MediaImageDocumentFactory.CreateFloppySector(new("fixture.adf", []), image);
    }

    [Fact]
    public async Task PartialAdfCannotPublishAnInventedCompleteImage()
    {
        var blank = BlankSectorImageFactory.Create(DiskImageFormatIds.AcornAdfs800);
        var bytes = blank.AvailableBlocks.OrderBy(block => block.LogicalBlock).SelectMany(block => block.Data).Take(8192).ToArray();
        var partial = await new AdfReader((_, _) => Task.FromResult(bytes)).ReadAsync("partial.adf");
        var output = new MemoryWriter();
        await Assert.ThrowsAsync<InvalidDataException>(() => new AcornAdfWriter(new LinearSectorImageWriter(output)).WriteAsync(partial, "target.adf"));
        Assert.Empty(output.Bytes);
    }

    [Theory]
    [InlineData(DiskImageFormatIds.AcornAdfs160)]
    [InlineData(DiskImageFormatIds.AcornAdfs320)]
    [InlineData(DiskImageFormatIds.AcornAdfs640)]
    [InlineData(DiskImageFormatIds.AcornAdfs800)]
    public async Task CreatedAdfsImageCanBeWrittenAndReopenedWithoutChangingItsBytes(string format)
    {
        var blank = BlankSectorImageFactory.Create(format);
        Assert.True(AcornAdfConversionService.CanCreate(format, ".adf"));
        Assert.NotEmpty(RetroMediaLab.MediaEngine.Images.Writing.MediaWritingComposition.CreateDefault()
            .Registry.FindTargetCandidates(format, ".adf"));
        var output = new MemoryWriter();
        var writer = new AcornAdfWriter(new LinearSectorImageWriter(output));
        await writer.WriteAsync(blank, "memory.adf");
        var original = output.Bytes;
        Assert.Equal(blank.Capacity, original.Length);
        var reopened = await new AdfReader((_, _) => Task.FromResult(original)).ReadAsync("memory.adf");
        Assert.Equal(format, reopened.FormatId);
        Assert.Equal(blank.Cylinders, reopened.Cylinders);
        Assert.Equal(blank.Heads, reopened.Heads);
        Assert.Equal(blank.BlockSize, reopened.BlockSize);
        await writer.WriteAsync(reopened, "roundtrip.adf");
        Assert.Equal(original, output.Bytes);
        var document = RetroMediaLab.MediaEngine.Images.Reading.MediaImageDocumentFactory
            .CreateFloppySector(new("memory.adf", []), reopened);
        var explored = await RetroMediaLab.MediaEngine.MediaEngineComposition.CreateDefault().Explorer.ExploreAsync(document);
        var volume = Assert.Single(explored.Volumes).FileSystem!;
        Assert.NotNull(volume);
        Assert.Empty(volume.Entries);
        Assert.Empty(volume.Warnings);
        Assert.Equal(blank.Capacity, volume.Capacity);
        var reservedBytes = format == DiskImageFormatIds.AcornAdfs800
            ? RetroMediaLab.MediaFileSystems.FileSystems.Acorn.Adfs.AcornAdfsLayout.DFormatHeaderSize
            : RetroMediaLab.MediaFileSystems.FileSystems.Acorn.Adfs.AcornAdfsFormatter.HeaderSize;
        Assert.Equal(blank.Capacity - reservedBytes, volume.FreeBytes);
    }

    [Theory]
    [InlineData(".640")]
    [InlineData(".640", "ACORN.ADFS.640")]
    public async Task Historical640ExtensionUsesTheExistingAdfsGeometry(string extension, string format = "acorn.adfs.640")
    {
        const int capacity = 655360;
        var catalog = new BuiltInImageFormatCatalog();
        Assert.Contains(catalog.Formats.Single(item => item.Id == DiskImageFormatIds.AcornAdfs640).Extensions,
            item => item.Extension == extension);
        var detector = new ImageFormatDetector(catalog, _ => throw new InvalidOperationException("No header needed"));
        Assert.Equal(DiskImageFormatIds.AcornAdfs640, detector.Detect("disk" + extension, capacity).Format!.Id);
        Assert.Null(detector.Detect("disk" + extension, 819200).Format);
        Assert.True(AcornAdfConversionService.CanCreate(format, extension));
        Assert.NotEmpty(RetroMediaLab.MediaEngine.Images.Writing.MediaWritingComposition.CreateDefault().Registry.FindTargetCandidates(format, extension));
        IMediaImageReader contract = new AdfReader();
        Assert.True(await contract.CanReadAsync(new(new MediaSourceDescriptor("disk" + extension, [], capacity, format)), CancellationToken.None));
        Assert.False(await contract.CanReadAsync(new(new MediaSourceDescriptor("disk" + extension, [], 163840, format)), CancellationToken.None));
        var data = new byte[capacity];
        new Random(640).NextBytes(data);
        var image = await new AdfReader((_, _) => Task.FromResult(data)).ReadAsync("disk" + extension);
        Assert.Equal(DiskImageFormatIds.AcornAdfs640, image.FormatId);
        var writer = new MemoryWriter();
        await new AcornAdfWriter(new LinearSectorImageWriter(writer)).WriteAsync(image, "disk" + extension);
        Assert.Equal(data, writer.Bytes);
        Assert.True(new SectorImageTrackEncoder().CanEncode(image));
    }

    [Theory]
    [InlineData("acorn.dosplus.800", ".img", true)]
    [InlineData("ACORN.DOSPLUS.800", ".IMG", true)]
    [InlineData("acorn.dosplus.800", ".800", true)]
    [InlineData("ACORN.DOSPLUS.800", ".800", true)]
    [InlineData("acorn.dosplus.800", ".adf", false)]
    [InlineData("ACORN.DOSPLUS.800", ".ADF", false)]
    public void OutputSelectionKeepsThePhysicalFormatRegardlessOfCase(string format, string extension, bool expected)
        => Assert.Equal(expected, AcornAdfConversionService.CanCreate(format, extension));

    [Theory]
    [InlineData("acorn.dosplus.800", 819200, true)]
    [InlineData("ACORN.DOSPLUS.800", 819200, true)]
    [InlineData("acorn.dosplus.800", 655360, false)]
    [InlineData("ACORN.DOSPLUS.800", 655360, false)]
    public async Task ChecksTheExplicitGeometryRegardlessOfCase(string format, int size, bool expected)
    {
        IMediaImageReader reader = new AdfReader();
        var context = new MediaRecognitionContext(new MediaSourceDescriptor("memory", [], size, format));
        Assert.Equal(expected, await reader.CanReadAsync(context, CancellationToken.None));
    }

    [Fact]
    public async Task EmptyCreatedMediaRemainsIdentifiableAfterReopeningItsRawBytes()
    {
        var blank = BlankSectorImageFactory.Create(DiskImageFormatIds.AcornDosPlus800);
        var output = new MemoryWriter();
        await new AcornAdfWriter(new LinearSectorImageWriter(output)).WriteAsync(blank, "memory");
        Assert.Equal(new byte[] { 0xfd, 0xff, 0xff }, output.Bytes[..3]);
        Assert.All(output.Bytes[3..], value => Assert.Equal(0, value));
        var reopened = await new AdfReader((_, _) => Task.FromResult(output.Bytes)).ReadAsync("memory");
        Assert.Equal(DiskImageFormatIds.AcornDosPlus800, reopened.FormatId);
        Assert.True(reopened.TryGetBlock(0, out var first));
        Assert.Equal(1, first.Address.Number);
        var document = MediaImageDocumentFactory.CreateFloppySector(new("detected.adf", []), reopened);
        var destinations = MediaEngineComposition.CreateDefault().ConversionService.GetAvailableDestinations(document);
        Assert.Contains(destinations, item => item.WriterId == MediaImageWriterIds.AcornAdf && item.FormatId == DiskImageFormatIds.AcornDosPlus800 && item.Extension == ".img");
        Assert.Contains(destinations, item => item.WriterId == MediaImageWriterIds.AcornAdf && item.FormatId == DiskImageFormatIds.AcornDosPlus800 && item.Extension == ".800");
        Assert.DoesNotContain(destinations, item => item.Extension == ".adf");
    }

    [Fact]
    public async Task ProducesThePhysicalMfmTrackUsedByTheFloppyController()
    {
        var data = new byte[AcornAdfGeometry.Capacity];
        data[0] = 0xfd; data[1] = data[2] = 0xff;
        var image = await new AdfReader((_, _) => Task.FromResult(data)).ReadAsync("memory");
        var encoder = new SectorImageTrackEncoder();
        Assert.True(encoder.CanEncode(image));
        var track = encoder.EncodePhysicalTrack(image, 0, 0, 200_000_000);
        Assert.Equal("iso.mfm", track.EncoderId);
        Assert.Equal(100_000, track.Bits.Count);
        Assert.Equal(200_000_000u, track.Revolution.IndexTimeTicks);
        Assert.NotEmpty(track.Revolution.FluxIntervals);
    }

    [Fact]
    public async Task WritesTheOriginalPayloadWithoutAddingASectorOrBootBlock()
    {
        var data = new byte[AcornAdfGeometry.Capacity];
        new Random(512).NextBytes(data);
        data[0] = 0xfd; data[1] = data[2] = 0xff;
        var image = await new AdfReader((_, _) => Task.FromResult(data)).ReadAsync("memory");
        var output = new MemoryWriter();
        await new AcornAdfWriter(new LinearSectorImageWriter(output)).WriteAsync(image, "memory");
        Assert.Equal(data, output.Bytes);
    }

    private sealed class MemoryWriter : IAtomicImageFileWriter
    {
        internal byte[] Bytes = [];
        public async Task WriteAsync(string path, Func<Stream, CancellationToken, Task> write, CancellationToken cancellationToken = default)
        {
            using var stream = new MemoryStream();
            await write(stream, cancellationToken);
            Bytes = stream.ToArray();
        }
    }

    [Theory]
    [InlineData(".img")]
    [InlineData(".800")]
    [InlineData(".adf")]
    public void DetectsTheDosPlusFatBeforeCapacityBasedFormats(string extension)
    {
        var detector = new ImageFormatDetector(new BuiltInImageFormatCatalog(),
            _ => new MemoryStream([0xfd, 0xff, 0xff], writable: false));
        Assert.Equal(DiskImageFormatIds.AcornDosPlus800,
            detector.Detect("memory" + extension, AcornAdfGeometry.Capacity).Format!.Id);
    }

    [Theory]
    [InlineData(".img", "ibm.800")]
    [InlineData(".adf", "acorn.adfs.800")]
    public void KeepsOther800KFormatsWhenTheDosPlusHeaderIsAbsent(string extension, string expected)
    {
        var detector = new ImageFormatDetector(new BuiltInImageFormatCatalog(),
            _ => new MemoryStream([0, 0, 0], writable: false));
        Assert.Equal(expected, detector.Detect("memory" + extension, AcornAdfGeometry.Capacity).Format!.Id);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public async Task PreservesPayloadAndUsesThePhysicalNumberingOfEachFormat(bool dosPlus, int firstSector)
    {
        var data = new byte[AcornAdfGeometry.Capacity];
        if (dosPlus) { data[0] = 0xfd; data[1] = data[2] = 0xff; }
        for (int block = 1; block < 800; block++) data[block * 1024] = (byte)block;
        var image = await new AdfReader((_, _) => Task.FromResult(data)).ReadAsync("memory");
        Assert.Equal(dosPlus ? DiskImageFormatIds.AcornDosPlus800 : DiskImageFormatIds.AcornAdfs800, image.FormatId);
        Assert.Equal(1024, image.BlockSize);
        Assert.Equal(5, image.SectorsPerTrack);
        foreach (var (block, cylinder, head, sector) in new[]
        {
            (0, 0, 0, firstSector), (4, 0, 0, firstSector + 4),
            (5, 0, 1, firstSector), (10, 1, 0, firstSector), (799, 79, 1, firstSector + 4)
        })
        {
            Assert.True(image.TryGetBlock(block, out var found));
            Assert.Equal(cylinder, found.Address.Cylinder);
            Assert.Equal(head, found.Address.Head);
            Assert.Equal(sector, found.Address.Number);
            Assert.Equal(data.AsSpan(block * 1024, 1024).ToArray(), found.Data.ToArray());
        }
    }
}
