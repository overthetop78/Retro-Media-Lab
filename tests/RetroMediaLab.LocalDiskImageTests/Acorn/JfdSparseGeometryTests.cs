using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Jfd;
using RetroMediaLab.MediaEngine.Images.Models.Sectors;
using RetroMediaLab.MediaFileSystems.FileSystems.Acorn.Adfs;
using System.Buffers.Binary;
using System.IO;
using RetroMediaLab.MediaEngine;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Contracts.Explorer;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.CpcDsk;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.TeleDisk;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Scp;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Scp.Inspection;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Scp.Decoding.Sectors;
using RetroMediaLab.MediaEngine.Images.Models.Flux;
using RetroMediaLab.MediaEngine.Images.Reading;
using RetroMediaLab.MediaEngine.Interfaces;

namespace RetroMediaLab.MediaAudit.Acorn;

public sealed class JfdSparseGeometryTests
{
    [Fact]
    public async Task EmptySlotsDoNotPermitTruncatingTheDeclaredAdfsVolume()
    {
        var document = await AcornDosPlus800Tests.AdfWithFile(DiskImageFormatIds.AcornAdfs320);
        var bytes = JfdCodec.Create(((SectorMediaImageRepresentation)document.Representation).Image);
        var table = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(24)));
        var end = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(28)));
        for (var offset = table + 80 * 4; offset < end; offset += 4)
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), uint.MaxValue);
        var partial = JfdReader.ReadBytes(new("memory.jfd", []), bytes);
        Assert.Null(MediaEngineComposition.CreateDefault().Conversion.Registry.Resolve(partial,
            DiskImageFormatIds.AcornAdfs160, MediaRepresentationKind.Sectors));
    }

    [Theory]
    [InlineData(DiskImageFormatIds.AcornAdfs320, true)]
    [InlineData(DiskImageFormatIds.AcornAdfs800, true)]
    [InlineData(DiskImageFormatIds.AcornAdfs320, false)]
    [InlineData(DiskImageFormatIds.AcornAdfs800, false)]
    public async Task MissingTracksKeepTheirExtentAndSectorPositionsAcrossContainers(string format, bool trailing)
    {
        var document = await AcornDosPlus800Tests.AdfWithFile(format);
        var complete = ((SectorMediaImageRepresentation)document.Representation).Image;
        var present = complete.AvailableBlocks.Where(block => trailing ? block.Address.Cylinder == 0 : block.Address.Cylinder != 1).OrderBy(block => block.LogicalBlock).ToArray();
        var bytes = JfdCodec.Create(complete);
        var table = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(24)));
        var tableEnd = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(28)));
        for (var offset = table + 8; offset < (trailing ? tableEnd : table + 16); offset += 4)
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), uint.MaxValue);
        var decoded = JfdCodec.Decode(bytes);
        Assert.Equal(complete.Cylinders, decoded.Sectors.Cylinders);
        Assert.Equal(complete.Capacity, decoded.Sectors.Capacity);
        Assert.Equal(present.Length, decoded.Sectors.AvailableBlocks.Count);
        Assert.Equal(present.SelectMany(block => block.Data), decoded.Sectors.AvailableBlocks.OrderBy(block => block.LogicalBlock).SelectMany(block => block.Data));
        Assert.Equal(bytes, JfdCodec.Rewrite(decoded, decoded.Sectors));
        Assert.True(new AcornAdfsFileSystemReader().CanRead(decoded.Sectors));

        var captured = new Capture();
        await new CpcDskWriter(captured).WriteAsync(CpcDskImageBuilder.Build(decoded.Sectors, CpcDskContainerKind.Extended), "memory.edsk");
        Check(await new CpcDskReader().ReadAsync(captured.Bytes.AsMemory()));
        await new Td0Writer(captured).WriteAsync(decoded.Sectors, "memory.td0");
        Check(Td0Reader.ReadDetailed(captured.Bytes).SectorImage);

        var engine = MediaEngineComposition.CreateDefault();
        var source = JfdReader.ReadBytes(new("memory.jfd", []), bytes);
        var encode = engine.Conversion.Registry.Resolve(source, DiskImageFormatIds.RawScp, MediaRepresentationKind.Flux)!;
        var generated = await encode.ConvertAsync(source, DiskImageFormatIds.RawScp, MediaRepresentationKind.Flux, new Dictionary<string, string>());
        var scp = ProtectedTrackScpImageAdapter.Create(((FluxMediaImageRepresentation)generated.Document.Representation).Image, generated.Document.Metadata);
        var explored = await engine.Explorer.ExploreAsync(source);
        var match = new ExploredFileSystem("acorn-adfs", decoded.Sectors, Assert.Single(explored.Volumes).FileSystem!);
        var ranked = ScpCandidateRanker.Rank([new ScpCandidateInspection("sparse", decoded.Sectors, [match], null)], scp);
        Assert.NotNull(ranked.BestFileSystem);

        void Check(SectorImage roundtrip)
        {
            Assert.Equal(decoded.Sectors.Capacity, roundtrip.Capacity);
            Assert.Equal(decoded.Sectors.BlockCount, roundtrip.BlockCount);
            Assert.Equal(present.Length, roundtrip.AvailableBlocks.Count);
            foreach (var block in decoded.Sectors.AvailableBlocks)
            {
                Assert.True(roundtrip.TryGetBlock(block.LogicalBlock, out var returned));
                Assert.Equal(block.Data, returned.Data);
            }
        }
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
}
