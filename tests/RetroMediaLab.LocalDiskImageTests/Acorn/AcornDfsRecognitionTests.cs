using System.IO;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Images.Creation;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.BbcDfs;
using RetroMediaLab.MediaEngine;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Images.Models.Sectors;
using RetroMediaLab.MediaEngine.Images.Reading;
using RetroMediaLab.MediaEngine.Images.Reading.Decoding;
using RetroMediaLab.MediaEngine.Images.Reading.Reconstruction.Iso;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.TeleDisk;
using RetroMediaLab.MediaEngine.Interfaces;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.CpcDsk;
using RetroMediaLab.MediaFileSystems.FileSystems.Acorn.BbcDfs;

namespace RetroMediaLab.MediaAudit.Acorn;

public sealed class AcornDfsRecognitionTests
{
    [Theory]
    [InlineData(DiskImageFormatIds.AcornDfsSingleSided)]
    [InlineData(DiskImageFormatIds.AcornDfsSingleSided80)]
    [InlineData(DiskImageFormatIds.AcornDfsDoubleSided)]
    [InlineData(DiskImageFormatIds.AcornDfsDoubleSided80)]
    public async Task DfsCataloguesAreExploredInsideSectorContainersAndRawAliases(string format)
    {
        var template = BlankSectorImageFactory.Create(format);
        var names = template.AvailableBlocks.First(block => block.LogicalBlock == 0).Data.ToArray();
        var metadata = template.AvailableBlocks.First(block => block.LogicalBlock == 1).Data.ToArray();
        "ONE    $"u8.CopyTo(names.AsSpan(8));
        metadata[5] = 8; metadata[12] = 4; metadata[15] = 2;
        var payload = new byte[256];
        new byte[] { 1, 2, 3, 4 }.CopyTo(payload, 0);
        var blocks = template.AvailableBlocks.Select(block => block.LogicalBlock switch
        {
            0 => block with { Data = names }, 1 => block with { Data = metadata },
            2 => block with { Data = payload }, _ => block
        }).ToArray();
        var image = new SectorImage(format, 256, template.Cylinders, template.Heads, template.SectorsPerTrack, blocks);
        var captured = new Capture();
        await new CpcDskWriter(captured).WriteAsync(CpcDskImageBuilder.Build(image, CpcDskContainerKind.Extended), "memory.edsk");
        Check(await new CpcDskReader().ReadAsync(captured.Bytes.AsMemory()));
        await new Td0Writer(captured).WriteAsync(image, "memory.td0");
        Check(Td0Reader.ReadDetailed(captured.Bytes).SectorImage);
        var raw = blocks.OrderBy(block => block.LogicalBlock).SelectMany(block => block.Data).ToArray();
        var reader = new BbcDfsReader((_, _) => Task.FromResult(raw));
        Assert.Equal(format, (await reader.ReadAsync("memory.ima")).FormatId);
        Assert.Equal(format, (await reader.ReadAsync("memory.img")).FormatId);
        var empty = new SectorImage(DiskImageFormatIds.CpcEmuDsk, 256, template.Cylinders, template.Heads, template.SectorsPerTrack,
            blocks.Select(block => block with { Data = new byte[256] }));
        Assert.False(new BbcDfsFileSystemReader().CanRead(empty));

        void Check(SectorImage decoded)
        {
            var fileSystem = new BbcDfsFileSystemReader();
            Assert.True(fileSystem.CanRead(decoded));
            var entries = fileSystem.Read(decoded).Entries;
            var file = decoded.Heads == 1 ? Assert.Single(entries) : Assert.Single(entries.First().Children);
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, file.Content);
        }
    }

    [Theory]
    [InlineData(DiskImageFormatIds.AcornDfsSingleSided)]
    [InlineData(DiskImageFormatIds.AcornDfsDoubleSided80)]
    [InlineData(DiskImageFormatIds.AcornAtomDos)]
    public async Task TeleDiskKeepsDfsFmEncodingAndRecordedHeadZero(string format)
    {
        var source = BlankSectorImageFactory.Create(format);
        var captured = new Capture();
        await new Td0Writer(captured).WriteAsync(source, "memory.td0");
        var decoded = Td0Reader.ReadDetailed(captured.Bytes);
        Assert.Equal(0x80, decoded.Header.DataRate);
        Assert.Equal(source.AvailableBlocks.SelectMany(block => block.Data), decoded.SectorImage.AvailableBlocks.SelectMany(block => block.Data));
        Assert.All(decoded.SectorImage.AvailableBlocks, block => Assert.Equal(0, block.RecordedAddress!.Head));
        var rewritten = new Capture();
        await new Td0Writer(rewritten).WriteAsync(decoded.SectorImage, "rewritten.td0");
        Assert.Equal(captured.Bytes, rewritten.Bytes);
    }

    private sealed class Capture : IAtomicImageFileWriter
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
    [InlineData(DiskImageFormatIds.AcornDfsSingleSided)]
    [InlineData(DiskImageFormatIds.AcornDfsSingleSided80)]
    [InlineData(DiskImageFormatIds.AcornDfsDoubleSided)]
    [InlineData(DiskImageFormatIds.AcornDfsDoubleSided80)]
    public async Task RawReturnRequiresDfsCataloguesAndPreservesEveryByte(string format)
    {
        var original = BlankSectorImageFactory.Create(format);
        var bytes = original.AvailableBlocks.SelectMany(block => block.Data).ToArray();
        var raw = Raw(bytes);
        var registry = MediaEngineComposition.CreateDefault().Conversion.Registry;
        var converter = registry.Resolve(raw, format, MediaRepresentationKind.Sectors);
        Assert.NotNull(converter);
        var converted = await converter.ConvertAsync(raw, format, MediaRepresentationKind.Sectors, new Dictionary<string, string>());
        Assert.NotEmpty(converted.Losses);
        var returned = Assert.IsType<SectorMediaImageRepresentation>(converted.Document.Representation).Image;
        Assert.Equal(bytes, returned.AvailableBlocks.SelectMany(block => block.Data));
        Assert.All(returned.AvailableBlocks, block => Assert.Null(block.IntegrityValid));
        bytes[256 + 6] = 0;
        Assert.Null(registry.Resolve(Raw(bytes), format, MediaRepresentationKind.Sectors));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void RawReturnNeverDropsErrorsOrNativeMetadata(int damage)
    {
        var original = BlankSectorImageFactory.Create(DiskImageFormatIds.AcornDfsDoubleSided80);
        var raw = Raw(original.AvailableBlocks.SelectMany(block => block.Data).ToArray());
        var image = ((SectorMediaImageRepresentation)raw.Representation).Image;
        var blocks = image.AvailableBlocks.Select(block => block.LogicalBlock != 0 ? block : damage switch
        {
            0 => block with { IntegrityValid = false },
            1 => block with { Tag = new byte[] { 1 } },
            _ => block with { RecordedAddress = block.Address }
        });
        var damaged = new SectorImage(image.FormatId, 512, image.Cylinders, image.Heads, image.SectorsPerTrack, blocks);
        var document = MediaImageDocumentFactory.CreateFloppySector(raw.Source, damaged);
        Assert.Null(MediaEngineComposition.CreateDefault().Conversion.Registry.Resolve(document, original.FormatId, MediaRepresentationKind.Sectors));
    }

    [Fact]
    public void FluxReconstructionRetainsBothPhysicalSidesWithRecordedHeadZero()
    {
        var original = BlankSectorImageFactory.Create(DiskImageFormatIds.AcornDfsDoubleSided80);
        var physical = new Dictionary<SectorAddress, List<IsoSectorCandidate>>();
        foreach (var block in original.AvailableBlocks)
        {
            var data = block.Data.ToArray();
            data[^1] = (byte)(0x60 + block.Address.Head);
            physical[block.Address] = [new(new DecodedSector((byte)block.Address.Cylinder, 0, block.Address.Number,
                1, 256, true, block.Address.Number * 2048, Data: data), 0)];
        }
        var addressed = physical.Where(pair => pair.Key.Head == 0).ToDictionary(pair => pair.Key, pair => pair.Value);
        var rebuilt = new BbcIsoScpSectorImagePolicy().Build(null, new(addressed, physical));
        Assert.Equal(original.FormatId, rebuilt.FormatId);
        Assert.Equal(1600, rebuilt.AvailableBlocks.Count);
        Assert.All(rebuilt.AvailableBlocks, block =>
        {
            Assert.Equal(0, block.RecordedAddress!.Head);
            Assert.Equal((byte)(0x60 + block.Address.Head), block.Data[^1]);
        });
    }

    private static MediaImageDocument Raw(byte[] bytes)
    {
        var blocks = Enumerable.Range(0, bytes.Length / 512).Select(index => new SectorBlock(index, new(0, 0, index), bytes.AsSpan(index * 512, 512).ToArray(), IntegrityValid: null));
        var image = new SectorImage(DiskImageFormatIds.IbmScan, 512, 1, 1, bytes.Length / 512, blocks);
        return MediaImageDocumentFactory.CreateFloppySector(new("raw.img", []), image);
    }

    [Theory]
    [InlineData(DiskImageFormatIds.AcornDfsSingleSided, ".ssd")]
    [InlineData(DiskImageFormatIds.AcornDfsSingleSided80, ".ssd")]
    [InlineData(DiskImageFormatIds.AcornDfsDoubleSided, ".dsd")]
    [InlineData(DiskImageFormatIds.AcornDfsDoubleSided80, ".dsd")]
    public async Task PartialImagesUseDeclaredTracksAndKeepEveryAvailableByte(string format, string extension)
    {
        var complete = BlankSectorImageFactory.Create(format);
        var bytes = complete.AvailableBlocks.SelectMany(block => block.Data).Take(8193).ToArray();
        bytes[^1] = 0x59;
        var image = await new BbcDfsReader((_, _) => Task.FromResult(bytes)).ReadAsync("partial" + extension);
        Assert.Equal(format, image.FormatId);
        Assert.Equal(complete.Capacity, image.Capacity);
        Assert.Equal(bytes, image.AvailableBlocks.OrderBy(block => block.LogicalBlock).SelectMany(block => block.Data));
        Assert.Single(image.AvailableBlocks.Last().Data);
        Assert.Null(image.AvailableBlocks.Last().IntegrityValid);
        Assert.False(BbcDfsImageWriter.CanWrite(image, format));
        Assert.True(BbcDfsImageWriter.CanWrite(complete, format));
        var captured = new Capture();
        await new CpcDskWriter(captured).WriteAsync(CpcDskImageBuilder.Build(image, CpcDskContainerKind.Extended), "partial.edsk");
        var decoded = await new CpcDskReader().ReadAsync(captured.Bytes.AsMemory());
        Assert.Equal(bytes, decoded.AvailableBlocks.OrderBy(block => block.LogicalBlock).SelectMany(block => block.Data));
    }

    [Fact]
    public async Task DsdWithAdlExtensionRequiresBothDfsCatalogues()
    {
        var image = BlankSectorImageFactory.Create(DiskImageFormatIds.AcornDfsDoubleSided80);
        var bytes = image.AvailableBlocks.OrderBy(block => block.LogicalBlock).SelectMany(block => block.Data).ToArray();
        Assert.True(BbcDfsReader.HasAdlDfsCatalogues(bytes));
        var decoded = await new BbcDfsReader((_, _) => Task.FromResult(bytes)).ReadAsync("misnamed.adl");
        Assert.Equal(image.FormatId, decoded.FormatId);
        Assert.Equal(bytes, decoded.AvailableBlocks.SelectMany(block => block.Data));
        bytes[BbcDfsGeometry.TrackSize + BbcDfsGeometry.SectorSize + 6] = 0;
        Assert.False(BbcDfsReader.HasAdlDfsCatalogues(bytes));
    }

    [Fact]
    public void AdfsAndUnstructuredDataAreNeverDfsByFilenameAlone()
    {
        var adfs = BlankSectorImageFactory.Create(DiskImageFormatIds.AcornAdfs640);
        var bytes = adfs.AvailableBlocks.SelectMany(block => block.Data).ToArray();
        Assert.False(BbcDfsReader.HasAdlDfsCatalogues(bytes));
        Assert.False(BbcDfsReader.HasAdlDfsCatalogues(new byte[BbcDfsGeometry.Dsd80.Capacity]));
    }
}
