using System.Buffers.Binary;
using System.IO;
using System.Text;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.CpcDsk;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.ImageDisk;
using RetroMediaLab.MediaEngine.Images.Reading.Decoding;
using RetroMediaLab.MediaEngine.Images.Writing.Encoding;
using RetroMediaLab.MediaEngine.Images.Models.Sectors;
using RetroMediaLab.MediaEngine.Interfaces;
using RetroMediaLab.MediaEngine.Functions;
using RetroMediaLab.MediaEngine.Contracts;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuFloppyMetadataTests
{
    [Theory]
    [InlineData(0, 0, true, true, false)]
    [InlineData(0x20, 0, false, true, false)]
    [InlineData(0x20, 0x60, true, false, true)]
    [InlineData(0, 0x40, true, true, true)]
    public async Task CpcContainerSeparatesIdAndDataCrcAndPreservesDeletedMarksOnReconstruction(
        byte status1, byte status2, bool headerValid, bool dataValid, bool deleted)
    {
        var bytes = new byte[1024];
        CpcDskFormat.StandardHeaderBytes.CopyTo(bytes);
        bytes[CpcDskLayout.CylinderCountOffset] = 1;
        bytes[CpcDskLayout.HeadCountOffset] = 1;
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(CpcDskLayout.StandardTrackSizeOffset), 768);
        var track = bytes.AsMemory(CpcDskLayout.DiskInformationBlockSize);
        CpcDskFormat.TrackHeaderBytes.CopyTo(track.Span);
        track.Span[CpcDskLayout.TrackSectorSizeCodeOffset] = 2;
        track.Span[CpcDskLayout.TrackSectorCountOffset] = 1;
        var descriptor = track[CpcDskLayout.SectorDescriptorTableOffset..];
        descriptor.Span[CpcDskLayout.SectorIdOffset] = 1;
        descriptor.Span[CpcDskLayout.SectorSizeCodeOffset] = 2;
        descriptor.Span[CpcDskLayout.SectorStatus1Offset] = status1;
        descriptor.Span[CpcDskLayout.SectorStatus2Offset] = status2;
        var image = await new CpcDskReader().ReadAsync(bytes);
        var sector = Assert.Single(image.AvailableBlocks);
        Assert.Equal(headerValid, sector.HeaderIntegrityValid);
        Assert.Equal(dataValid, sector.DataIntegrityValid);
        Assert.Equal(deleted, sector.DeletedDataMark);
        var rebuilt = CpcDskImageBuilder.Build(image, CpcDskContainerKind.Standard);
        var stored = Assert.Single(Assert.Single(rebuilt.Tracks).Sectors);
        Assert.Equal(status1, stored.Status1);
        Assert.Equal(status2, stored.Status2);
        var output = new MemoryWriter();
        await new CpcDskWriter(output).WriteAsync(rebuilt, "memory.dsk");
        var reopened = Assert.Single((await new CpcDskReader().ReadAsync(output.Bytes)).AvailableBlocks);
        Assert.Equal(headerValid, reopened.HeaderIntegrityValid);
        Assert.Equal(dataValid, reopened.DataIntegrityValid);
        Assert.Equal(deleted, reopened.DeletedDataMark);
    }

    [Theory]
    [InlineData(ImdSectorRecordType.Normal, true, false)]
    [InlineData(ImdSectorRecordType.Deleted, true, true)]
    [InlineData(ImdSectorRecordType.DeletedWithError, false, true)]
    [InlineData(ImdSectorRecordType.CompressedWithError, false, false)]
    public void ImageDiskDoesNotInventHeaderCrcFromADataError(ImdSectorRecordType type, bool valid, bool deleted)
    {
        var compressed = type == ImdSectorRecordType.CompressedWithError;
        var bytes = Encoding.ASCII.GetBytes("IMD 1.18 test").Concat(new byte[]
            { 0x1a, 5, 0, 0, 1, 2, 1, (byte)type }).Concat(new byte[compressed ? 1 : 512]).ToArray();
        var image = ImdReader.ReadDetailed(bytes).SectorImage;
        var sector = Assert.Single(image.AvailableBlocks);
        Assert.Null(sector.HeaderIntegrityValid);
        Assert.Equal(valid, sector.DataIntegrityValid);
        Assert.Equal(deleted, sector.DeletedDataMark);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void IsoFluxDecodersExposeTheMarkAndSeparateBothRealCrcChecks(bool fm, bool deleted)
    {
        var data = Enumerable.Range(0, 512).Select(value => (byte)value).ToArray();
        var request = new TrackEncodeRequest(0, 0, [new(1, data, Deleted: deleted)]);
        var encoded = fm ? new IsoFmTrackEncoder().Encode(request) : new IsoMfmTrackEncoder().Encode(request);
        var decoded = fm ? new IsoFmDecoder().Decode(encoded.Revolution) : new IsoMfmDecoder().Decode(encoded.Revolution);
        var sector = Assert.Single(decoded.Sectors);
        Assert.True(sector.HeaderIntegrityValid);
        Assert.True(sector.DataIntegrityValid);
        Assert.Equal(deleted, sector.DeletedDataMark);
        Assert.Equal(data, sector.Data);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ImageDiskMetadataUsesTheUpdatedMarkCrcAndPayloadInsteadOfTheOriginalRecordType(bool deleted)
    {
        var bytes = Encoding.ASCII.GetBytes("IMD 1.18 test").Concat(new byte[]
            { 0x1a, 5, 0, 0, 1, 2, 1, (byte)ImdSectorRecordType.CompressedDeletedWithError, 0 }).ToArray();
        var original = ImdReader.ReadDetailed(bytes);
        var payload = Enumerable.Range(0, 512).Select(value => (byte)value).ToArray();
        var block = Assert.Single(original.SectorImage.AvailableBlocks) with
        {
            Data = payload, IntegrityValid = true, DataIntegrityValid = true, DeletedDataMark = deleted
        };
        var updated = new SectorImage(original.SectorImage.FormatId, 512, 1, 1, 1, [block]);
        var restored = Assert.IsType<ImdImage>(ImdMetadataFunctions.Restore(updated, ImdMetadataFunctions.Create(original)));
        var record = Assert.Single(Assert.Single(restored.Tracks).Sectors);
        Assert.Equal(deleted ? ImdSectorRecordType.Deleted : ImdSectorRecordType.Normal, record.RecordType);
        var output = new MemoryWriter();
        await new ImdWriter(output).WriteAsync(restored, "memory.imd");
        var reopened = Assert.Single(ImdReader.ReadDetailed(output.Bytes).SectorImage.AvailableBlocks);
        Assert.Equal(payload, reopened.Data);
        Assert.Equal(deleted, reopened.DeletedDataMark);
        Assert.True(reopened.DataIntegrityValid);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ContainersKeepPhysicalTrackSeparateFromRepeatedOutOfOrderRecordedIdentifiers(bool imd)
    {
        var numbers = new[] { 5, 1, 5 };
        var blocks = numbers.Select((number, index) => new SectorBlock(index, new(0, 0, number),
            Enumerable.Repeat((byte)index, 512).ToArray())
        {
            RecordedAddress = new(9, 1, number)
        }).ToArray();
        var original = new SectorImage(CpcDskFormat.FormatId, 512, 1, 1, numbers.Length, blocks);
        var output = new MemoryWriter();
        SectorImage reopened;
        if (imd)
        {
            var sectors = blocks.Select(block => new ImdSector(9, 1, (byte)block.Address.Number, 512,
                ImdSectorRecordType.Normal, block.Data)).ToArray();
            var image = new ImdImage(ImdFormat.DefaultComment, [new(ImdMode.Mfm250Kbps, 0, 0, sectors)], original);
            await new ImdWriter(output).WriteAsync(image, "memory.imd");
            var parsed = ImdReader.ReadDetailed(output.Bytes);
            var restored = Assert.IsType<ImdImage>(ImdMetadataFunctions.Restore(parsed.SectorImage, ImdMetadataFunctions.Create(parsed)));
            await new ImdWriter(output).WriteAsync(restored, "memory.imd");
            reopened = ImdReader.ReadDetailed(output.Bytes).SectorImage;
        }
        else
        {
            await new CpcDskWriter(output).WriteAsync(CpcDskImageBuilder.Build(original, CpcDskContainerKind.Standard), "memory.dsk");
            var parsed = await new CpcDskReader().ReadAsync(output.Bytes);
            await new CpcDskWriter(output).WriteAsync(CpcDskImageBuilder.Build(parsed, CpcDskContainerKind.Standard), "memory.dsk");
            reopened = await new CpcDskReader().ReadAsync(output.Bytes);
        }
        Assert.Equal(1, reopened.Cylinders);
        Assert.Equal(1, reopened.Heads);
        Assert.Equal(numbers.Order(), reopened.AvailableBlocks.OrderBy(block => block.LogicalBlock).Select(block => block.Address.Number));
        var records = reopened.AvailableBlocks.OrderBy(block => block.RecordedTrackOrder).ToArray();
        Assert.Equal(numbers, records.Select(block => block.Address.Number));
        Assert.Equal(numbers, records.Select(block => block.RecordedAddress!.Number));
        foreach (var block in records)
        {
            Assert.Equal(0, block.Address.Cylinder);
            Assert.Equal(0, block.Address.Head);
            Assert.Equal(9, block.RecordedAddress!.Cylinder);
            Assert.Equal(1, block.RecordedAddress.Head);
            Assert.All(block.Data, value => Assert.Equal((byte)block.RecordedTrackOrder!.Value, value));
        }
    }

    [Fact]
    public async Task ExtendedDskKeepsTheRecordedSizeCodeWhenItDisagreesWithTheStoredPayloadLength()
    {
        var data = Enumerable.Range(0, 512).Select(value => (byte)value).ToArray();
        var block = new SectorBlock(0, new(0, 0, 1), data) { RecordedSizeCode = 3 };
        var image = new SectorImage(CpcDskFormat.FormatId, 512, 1, 1, 1, [block]);
        var output = new MemoryWriter();
        for (var cycle = 0; cycle < 2; cycle++)
        {
            await new CpcDskWriter(output).WriteAsync(CpcDskImageBuilder.Build(image, CpcDskContainerKind.Extended), "memory.edsk");
            image = await new CpcDskReader().ReadAsync(output.Bytes);
            var stored = Assert.Single(image.AvailableBlocks);
            Assert.Equal((byte)3, stored.RecordedSizeCode);
            Assert.Equal(data, stored.Data);
        }
    }

    [Fact]
    public async Task FormattedTrackWithADifferentSectorCountAndRecordedHeadSurvivesExtendedDskPublication()
    {
        var source = new SectorImage(CpcDskFormat.FormatId, 512, 2, 1, 3,
            Enumerable.Range(0, 6).Select(index => new SectorBlock(index, new(index / 3, 0, index % 3),
                Enumerable.Repeat((byte)(0x11 + index), 512).ToArray())));
        var payload = Enumerable.Repeat((byte)0xa5, 512).ToArray();
        MediaDiskSector[] fields =
        [
            new(0, payload, 9, 9, 7, true) { RecordedSizeCode = 2 },
            new(1, Enumerable.Repeat((byte)0xb6, 512).ToArray(), 0, 0, 1, true) { RecordedSizeCode = 2 }
        ];
        var formatted = SectorTrackFunctions.Replace(source, 0, 0, fields, retainGeometry: false);
        payload[0] = 0;
        var output = new MemoryWriter();
        await new CpcDskWriter(output).WriteAsync(CpcDskImageBuilder.Build(formatted, CpcDskContainerKind.Extended), "memory.dsk");
        var reopened = await new CpcDskReader().ReadAsync(output.Bytes);
        var track = reopened.AvailableBlocks.Where(block => block.Address.Cylinder == 0)
            .OrderBy(block => block.RecordedTrackOrder).ToArray();
        Assert.Equal(new[] { 7, 1 }, track.Select(block => block.Address.Number));
        Assert.Equal(new SectorAddress(9, 9, 7), track[0].RecordedAddress);
        Assert.All(track[0].Data, value => Assert.Equal(0xa5, value));
        Assert.All(track[1].Data, value => Assert.Equal(0xb6, value));
        Assert.Equal(5, reopened.BlockCount);
        foreach (var original in source.AvailableBlocks.Where(block => block.Address.Cylinder == 1))
            Assert.Equal(original.Data, reopened.AvailableBlocks.Single(block => block.Address == original.Address).Data);
    }

    private sealed class MemoryWriter : IAtomicImageFileWriter
    {
        internal byte[] Bytes { get; private set; } = [];

        public async Task WriteAsync(string path, Func<Stream, CancellationToken, Task> write, CancellationToken cancellationToken = default)
        {
            using var stream = new MemoryStream();
            await write(stream, cancellationToken);
            Bytes = stream.ToArray();
        }
    }
}
