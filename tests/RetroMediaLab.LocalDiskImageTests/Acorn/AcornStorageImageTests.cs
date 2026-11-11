using System.Buffers.Binary;
using System.IO;
using System.Numerics;
using RetroMediaLab.MediaEngine.Images.Formats;
using RetroMediaLab.MediaEngine.Images.Formats.Detection;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Adf;
using RetroMediaLab.MediaEngine;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Images.Models.Blocks;
using RetroMediaLab.MediaEngine.Images.Models.Sectors;
using RetroMediaLab.MediaEngine.Images.Reading.Sources;
using RetroMediaLab.MediaEngine.Images.Visualization.Providers;
using RetroMediaLab.App.Contracts.Rendering.Blocks;
using RetroMediaLab.App.Presenters.Visualization;
using RetroMediaLab.App.Rendering.Blocks;
using SkiaSharp;
using AdfsLayout = RetroMediaLab.MediaFileSystems.FileSystems.Acorn.Adfs.AcornAdfsLayout;

namespace RetroMediaLab.MediaAudit.Acorn;

/// <summary>Contrôles autonomes des répertoires BBC, identiques dans une disquette et un Winchester.</summary>
public sealed class AcornStorageImageTests
{
    [Theory]
    [InlineData(DiskImageFormatIds.AcornAdfs160, false)]
    [InlineData(DiskImageFormatIds.AcornAdfs160, true)]
    [InlineData(DiskImageFormatIds.AcornAdfs320, false)]
    [InlineData(DiskImageFormatIds.AcornAdfs320, true)]
    [InlineData(DiskImageFormatIds.AcornAdfs640, false)]
    [InlineData(DiskImageFormatIds.AcornAdfs640, true)]
    [InlineData(DiskImageFormatIds.AcornAdfs800, false)]
    [InlineData(DiskImageFormatIds.AcornAdfs800, true)]
    public async Task PartialAdfPreservesAvailableBytesAndReportsUnavailableFileData(string format, bool missingFile)
    {
        var blank = RetroMediaLab.MediaEngine.Images.Creation.BlankSectorImageFactory.Create(format);
        var bytes = blank.AvailableBlocks.OrderBy(block => block.LogicalBlock).SelectMany(block => block.Data).ToArray();
        const int storedLength = 4096 + 17;
        var fileBlock = 4096 / blank.BlockSize + (missingFile ? 2 : 0);
        var root = blank.BlockSize == 256 ? 512 : 1024;
        "ONE"u8.CopyTo(bytes.AsSpan(root + 5));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(root + 23), 4);
        var fileUnit = fileBlock * blank.BlockSize / 256;
        if (blank.BlockSize == 256 && blank.Heads == 2)
            fileUnit = fileBlock / blank.SectorsPerTrack % 2 * blank.Cylinders * blank.SectorsPerTrack
                + fileBlock / (2 * blank.SectorsPerTrack) * blank.SectorsPerTrack + fileBlock % blank.SectorsPerTrack;
        bytes[root + 27] = (byte)fileUnit; bytes[root + 28] = (byte)(fileUnit >> 8);
        byte[] payload = [0, 0xe5, 0xff, 0x42];
        payload.CopyTo(bytes, fileBlock * blank.BlockSize);
        var firstFree = fileUnit + blank.BlockSize / 256;
        var freeUnits = bytes.Length / 256 - firstFree;
        bytes[0] = (byte)firstFree; bytes[1] = (byte)(firstFree >> 8);
        bytes[256] = (byte)freeUnits; bytes[257] = (byte)(freeUnits >> 8); bytes[258] = (byte)(freeUnits >> 16);
        for (var sector = 0; sector < 2; sector++)
        {
            var sum = 0;
            for (var offset = 254; offset >= 0; offset--) sum = (sum & 255) + (sum >> 8) + bytes[sector * 256 + offset];
            bytes[sector * 256 + 255] = (byte)sum;
        }
        if (blank.BlockSize == 1024) UpdateNickDirectoryCheck(bytes.AsSpan(root, 2048), 1);
        var partial = bytes[..storedLength];
        var detector = new ImageFormatDetector(new BuiltInImageFormatCatalog(), _ => new MemoryStream(partial, writable: false));
        Assert.Equal(format, detector.Detect("partial.adf", partial.Length).Format!.Id);
        var image = await new AdfReader((_, _) => Task.FromResult(partial)).ReadAsync("partial.adf");
        Assert.Equal(blank.Capacity, image.Capacity);
        Assert.Equal(storedLength, image.AvailableBlocks.Sum(block => block.Data.Count));
        var last = image.AvailableBlocks.MaxBy(block => block.LogicalBlock)!;
        Assert.Equal(17, last.Data.Count);
        Assert.Null(last.IntegrityValid);
        Assert.False(image.TryGetBlock(last.LogicalBlock + 1, out _));
        var elements = new SectorMediaInspectorPresenter((key, _) => key).BuildRenderModel(image)
            .Surfaces.SelectMany(surface => surface.Tracks).SelectMany(track => track.Sectors).ToArray();
        Assert.Equal(RetroMediaLab.App.Enums.Rendering.Sectors.SectorMediaElementState.Degraded,
            elements.Single(element => element.LogicalBlock == last.LogicalBlock).State);
        Assert.Equal(RetroMediaLab.App.Enums.Rendering.Sectors.SectorMediaElementState.WithoutData,
            elements.Single(element => element.LogicalBlock == last.LogicalBlock + 1).State);
        var document = RetroMediaLab.MediaEngine.Images.Reading.MediaImageDocumentFactory.CreateFloppySector(new("partial.adf", []), image);
        var result = await MediaEngineComposition.CreateDefault().Explorer.ExploreAsync(document);
        var volume = Assert.Single(result.Volumes).FileSystem!;
        Assert.NotNull(volume);
        var file = Assert.Single(volume.Entries);
        Assert.Equal("ONE", file.Name);
        Assert.Equal(!missingFile, file.MetadataValid);
        if (missingFile) { Assert.Null(file.Content); Assert.NotEmpty(volume.Warnings); }
        else { Assert.Equal(payload, file.Content); Assert.Empty(volume.Warnings); }
    }

    [Fact]
    public async Task PartialAdfDoesNotInferGeometryFromAnInvalidMap()
    {
        var blank = RetroMediaLab.MediaEngine.Images.Creation.BlankSectorImageFactory.Create(DiskImageFormatIds.AcornAdfs160);
        var bytes = blank.AvailableBlocks.OrderBy(block => block.LogicalBlock).SelectMany(block => block.Data).Take(4096).ToArray();
        bytes[255] ^= 1;
        await Assert.ThrowsAsync<InvalidDataException>(() => new AdfReader((_, _) => Task.FromResult(bytes)).ReadAsync("partial.adf"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FFormatFindsItsCentralMapAndUsesTheMapRecord(bool hasFile)
    {
        var bytes = CreateFFormatBytes(hasFile);
        var image = await new AdfReader((_, _) => Task.FromResult(bytes)).ReadAsync("memory.adf");
        Assert.Equal(DiskImageFormatIds.AcornAdfs1600, image.FormatId);
        var document = RetroMediaLab.MediaEngine.Images.Reading.MediaImageDocumentFactory.CreateFloppySector(new("memory.adf", []), image);
        var result = await MediaEngineComposition.CreateDefault().Explorer.ExploreAsync(document);
        var volume = Assert.Single(result.Volumes).FileSystem!;
        Assert.NotNull(volume);
        Assert.Equal("MAP", volume.Name);
        Assert.Empty(volume.Warnings);
        Assert.Equal(bytes.Length - (hasFile ? 11 : 10) * 1024, volume.FreeBytes);
        if (hasFile)
        {
            var file = Assert.Single(volume.Entries);
            Assert.Equal("ONE", file.Name);
            Assert.True(file.MetadataValid);
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, file.Content);
        }
        else Assert.Empty(volume.Entries);
    }

    [Fact]
    public async Task FFormatDoesNotLocateAMapFromAnInvalidBootChecksum()
    {
        var bytes = CreateFFormatBytes(true);
        bytes[0xdff] ^= 1;
        var image = await new AdfReader((_, _) => Task.FromResult(bytes)).ReadAsync("memory.adf");
        var document = RetroMediaLab.MediaEngine.Images.Reading.MediaImageDocumentFactory.CreateFloppySector(new("memory.adf", []), image);
        var result = await MediaEngineComposition.CreateDefault().Explorer.ExploreAsync(document);
        Assert.DoesNotContain(result.Volumes, volume => volume.FileSystem is not null);
    }

    [Theory]
    [InlineData(0, false, true)]
    [InlineData(2, false, true)]
    [InlineData(2, true, false)]
    public async Task FFormatUsesOnlyAnIntactMapCopy(int damagedZone, bool damageBothCopies, bool recognized)
    {
        var bytes = CreateFFormatBytes(true);
        bytes.AsSpan((794 + damagedZone) * 1024, 1024).Clear();
        if (damageBothCopies) bytes.AsSpan((798 + damagedZone) * 1024, 1024).Clear();
        var image = await new AdfReader((_, _) => Task.FromResult(bytes)).ReadAsync("memory.adf");
        var document = RetroMediaLab.MediaEngine.Images.Reading.MediaImageDocumentFactory.CreateFloppySector(new("memory.adf", []), image);
        var result = await MediaEngineComposition.CreateDefault().Explorer.ExploreAsync(document);
        var volume = Assert.Single(result.Volumes).FileSystem;
        Assert.Equal(recognized, volume is not null);
        if (recognized) Assert.Equal(new byte[] { 1, 2, 3, 4 }, Assert.Single(volume!.Entries).Content);
    }

    [Fact]
    public async Task FFormatRejectsBadMapChecksumsEvenWhenTheDirectoryRemainsReadable()
    {
        var bytes = CreateFFormatBytes(true);
        foreach (var copy in new[] { 794, 798 }) bytes[(copy + 2) * 1024 + 900] ^= 1;
        var image = await new AdfReader((_, _) => Task.FromResult(bytes)).ReadAsync("memory.adf");
        var document = RetroMediaLab.MediaEngine.Images.Reading.MediaImageDocumentFactory.CreateFloppySector(new("memory.adf", []), image);
        var result = await MediaEngineComposition.CreateDefault().Explorer.ExploreAsync(document);
        Assert.DoesNotContain(result.Volumes, volume => volume.FileSystem is not null);
    }

    // PRM FileCore: four F-format zones, 64 bytes per map bit, map at block 794.
    // Object 2 contains two copies of the map followed by the root and its shared file.
    internal static byte[] CreateFFormatBytes(bool hasFile)
    {
        var bytes = new byte[1638400];
        bytes.AsSpan(0, 3072).Fill(0xa5);
        var record = new byte[60];
        record[0] = 10; record[1] = 10; record[2] = 2; record[3] = 4;
        record[4] = 15; record[5] = 6; record[9] = 4;
        BinaryPrimitives.WriteUInt16LittleEndian(record.AsSpan(10), 1600);
        BinaryPrimitives.WriteUInt32LittleEndian(record.AsSpan(12), 0x209);
        BinaryPrimitives.WriteUInt32LittleEndian(record.AsSpan(16), (uint)bytes.Length);
        "MAP"u8.CopyTo(record.AsSpan(22));
        var map = new byte[4096];
        record.CopyTo(map, 4);
        static void Bits(Span<byte> data, int start, int count, uint value)
        {
            for (var bit = 0; bit < count; bit++)
                if ((value & (1u << bit)) != 0) data[(start + bit) / 8] |= (byte)(1 << ((start + bit) % 8));
        }
        for (var zone = 0; zone < 4; zone++)
        {
            var data = map.AsSpan(zone * 1024, 1024);
            var start = zone == 0 ? 512 : 32;
            var end = zone == 3 ? 6336 : 6624;
            var freeStart = start;
            if (zone == 2)
            {
                var allocated = (hasFile ? 11 : 10) * 16;
                Bits(data, start, 15, 2);
                Bits(data, start + allocated - 1, 1, 1);
                freeStart += allocated;
            }
            Bits(data, 8, 15, (uint)(freeStart - 8));
            Bits(data, 23, 1, 1); // Terminating bit of the header's free fragment.
            Bits(data, end - 1, 1, 1); // Last free fragment has link zero.
            data[3] = zone == 0 ? (byte)0xff : (byte)0;
            uint a = 0, b = 0, c = 0, d = 0;
            for (var offset = 1020; offset > 0; offset -= 4)
            {
                a += data[offset] + (d >> 8); d &= 255;
                b += data[offset + 1] + (a >> 8); a &= 255;
                c += data[offset + 2] + (b >> 8); b &= 255;
                d += data[offset + 3] + (c >> 8); c &= 255;
            }
            a += d >> 8; b += data[1] + (a >> 8); c += data[2] + (b >> 8); d += data[3] + (c >> 8);
            data[0] = (byte)(a ^ b ^ c ^ d);
        }
        map.CopyTo(bytes, 794 * 1024);
        map.CopyTo(bytes, 798 * 1024);
        record.CopyTo(bytes, 0xdc0);
        "BOOT"u8.CopyTo(bytes.AsSpan(0xdc0 + 22));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0xdc0 + 12), 0x201);
        var sum = 0;
        for (var offset = 0xdfe; offset >= 0xc00; offset--)
            sum = (sum & 255) + (sum >> 8) + bytes[offset];
        bytes[0xdff] = (byte)sum;

        var header = RetroMediaLab.MediaFileSystems.FileSystems.Acorn.Adfs.AcornAdfsFormatter.CreateDFormatHeader();
        var directory = bytes.AsSpan(802 * 1024, 2048);
        header.AsSpan(AdfsLayout.DFormatRootOffset, 2048).CopyTo(directory);
        directory[AdfsLayout.ParentAddressOffset] = 9;
        directory[AdfsLayout.ParentAddressOffset + 1] = 2;
        if (hasFile)
        {
            "ONE"u8.CopyTo(directory[5..]);
            BinaryPrimitives.WriteUInt32LittleEndian(directory[23..], 4);
            directory[27] = 11; directory[28] = 2;
            new byte[] { 1, 2, 3, 4 }.CopyTo(bytes, 804 * 1024);
        }
        UpdateNickDirectoryCheck(directory, hasFile ? 1 : 0);
        return bytes;
    }

    internal static void UpdateNickDirectoryCheck(Span<byte> directory, int entryCount)
    {
        uint check = 0;
        var used = 5 + entryCount * 26;
        var position = 0;
        while (position + 4 <= used) { check = BitOperations.RotateRight(check, 13) ^ BinaryPrimitives.ReadUInt32LittleEndian(directory[position..]); position += 4; }
        while (position < used) check = BitOperations.RotateRight(check, 13) ^ directory[position++];
        position = AdfsLayout.TailOffset;
        while (position % 4 != 0) check = BitOperations.RotateRight(check, 13) ^ directory[position++];
        while (position < 2044) { check = BitOperations.RotateRight(check, 13) ^ BinaryPrimitives.ReadUInt32LittleEndian(directory[position..]); position += 4; }
        directory[2047] = (byte)(check ^ check >> 8 ^ check >> 16 ^ check >> 24);
    }

    [Theory]
    [InlineData(DiskImageFormatIds.AcornAdfs160, false)]
    [InlineData(DiskImageFormatIds.AcornAdfs160, true)]
    [InlineData(DiskImageFormatIds.AcornAdfs320, false)]
    [InlineData(DiskImageFormatIds.AcornAdfs320, true)]
    [InlineData(DiskImageFormatIds.AcornAdfs640, false)]
    [InlineData(DiskImageFormatIds.AcornAdfs640, true)]
    public async Task BbcAdfRecognitionAndReopeningPreserveFilesAndFreeSpace(string format, bool hasFile)
    {
        var blank = RetroMediaLab.MediaEngine.Images.Creation.BlankSectorImageFactory.Create(format);
        var bytes = blank.AvailableBlocks.OrderBy(block => block.LogicalBlock)
            .SelectMany(block => block.Data.ToArray()).ToArray();
        byte[] payload = [0, 0xe5, 0xff, 0x42];
        if (hasFile)
        {
            // One allocated sector follows the seven system sectors of an S/M/L volume.
            var entryOffset = 2 * 256 + 5;
            "ONE"u8.CopyTo(bytes.AsSpan(entryOffset));
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(entryOffset + 18), (uint)payload.Length);
            bytes[entryOffset + 22] = 7;
            payload.CopyTo(bytes, 7 * 256);
            bytes[0] = 8;
            var freeSectors = bytes.Length / 256 - 8;
            bytes[256] = (byte)freeSectors;
            bytes[257] = (byte)(freeSectors >> 8);
            bytes[258] = (byte)(freeSectors >> 16);
            for (var sector = 0; sector < 2; sector++)
            {
                var sum = 0;
                for (var offset = 254; offset >= 0; offset--)
                    sum = (sum & 255) + (sum >> 8) + bytes[sector * 256 + offset];
                bytes[sector * 256 + 255] = (byte)sum;
            }
        }

        var detector = new ImageFormatDetector(new BuiltInImageFormatCatalog(),
            _ => new MemoryStream(bytes, writable: false));
        Assert.Equal(format, detector.Detect("memory.adf", bytes.Length).Format!.Id);
        var image = await new AdfReader((_, _) => Task.FromResult(bytes)).ReadAsync("memory.adf");
        Assert.Equal(format, image.FormatId);
        var document = MediaImageDocument.CreateUnexplored(new("memory.adf", []), image.FormatId,
            MediaKind.Floppy, new SectorMediaImageRepresentation(image), [], new Dictionary<string, string>());
        var explored = await MediaEngineComposition.CreateDefault().Explorer.ExploreAsync(document);
        var volume = Assert.Single(explored.Volumes).FileSystem!;
        Assert.NotNull(volume);
        Assert.Empty(volume.Warnings);
        Assert.Equal(bytes.Length, volume.Capacity);
        Assert.Equal(bytes.Length - (hasFile ? 8 : 7) * 256, volume.FreeBytes);
        if (hasFile)
        {
            var file = Assert.Single(volume.Entries);
            Assert.Equal("ONE", file.Name);
            Assert.True(file.MetadataValid);
            Assert.Equal(payload.Length, file.Size);
            Assert.Equal(payload, file.Content);
        }
        else Assert.Empty(volume.Entries);
    }

    [Theory]
    [InlineData(DiskImageFormatIds.AcornAdfs160)]
    [InlineData(DiskImageFormatIds.AcornAdfs320)]
    [InlineData(DiskImageFormatIds.AcornAdfs640)]
    public async Task BlankBbcAdfsHasAUsableMapAndEmptyRoot(string format)
    {
        var image = RetroMediaLab.MediaEngine.Images.Creation.BlankSectorImageFactory.Create(format);
        var document = MediaImageDocument.CreateUnexplored(new("blank.adf", []), format, MediaKind.Floppy, new SectorMediaImageRepresentation(image), [], new Dictionary<string, string>());
        var explored = await MediaEngineComposition.CreateDefault().Explorer.ExploreAsync(document);
        var volume = Assert.Single(explored.Volumes).FileSystem!;
        Assert.NotNull(volume); Assert.Empty(volume.Entries); Assert.Empty(volume.Warnings);
        Assert.Equal(image.Capacity - 1792, volume.FreeBytes);
        for (var index = 0; index < 2; index++)
        {
            Assert.True(image.TryGetBlock(index, out var block)); var sum = 0;
            for (var offset = 254; offset >= 0; offset--) sum = (sum & 255) + (sum >> 8) + block.Data[offset];
            Assert.Equal(sum & 255, block.Data[255]);
        }
    }

    [Theory]
    [InlineData(15)]
    [InlineData(1279)]
    public async Task AdfsLFileAddressesCrossTracksAndSidesInLogicalOrder(int start)
    {
        var blank = RetroMediaLab.MediaEngine.Images.Creation.BlankSectorImageFactory.Create(DiskImageFormatIds.AcornAdfs640);
        var blocks = blank.AvailableBlocks.ToDictionary(block => block.LogicalBlock, block => block.Data.ToArray());
        var directory = blocks[2]; "CROSS"u8.CopyTo(directory.AsSpan(5));
        BinaryPrimitives.WriteUInt32LittleEndian(directory.AsSpan(23), 512);
        directory[27] = (byte)start; directory[28] = (byte)(start >> 8);
        for (var offset = 0; offset < 2; offset++)
        {
            var logical = start + offset; var side = logical / 1280; var track = logical % 1280 / 16;
            blocks[(track * 2 + side) * 16 + logical % 16].AsSpan().Fill((byte)(0x61 + offset));
        }
        var image = new SectorImage(blank.FormatId, 256, 80, 2, 16, blank.AvailableBlocks.Select(block => block with { Data = blocks[block.LogicalBlock] }));
        var document = MediaImageDocument.CreateUnexplored(new("cross.adl", []), image.FormatId, MediaKind.Floppy, new SectorMediaImageRepresentation(image), [], new Dictionary<string, string>());
        var explored = await MediaEngineComposition.CreateDefault().Explorer.ExploreAsync(document);
        var file = Assert.Single(Assert.Single(explored.Volumes).FileSystem!.Entries);
        Assert.Equal("CROSS", file.Name); Assert.True(file.MetadataValid);
        Assert.Equal(Enumerable.Repeat((byte)0x61, 256).Concat(Enumerable.Repeat((byte)0x62, 256)), file.Content);
    }

    [Theory]
    [InlineData(DiskImageFormatIds.AcornDfsDoubleSided)]
    [InlineData(DiskImageFormatIds.AcornDfsDoubleSided80)]
    [InlineData(DiskImageFormatIds.Jfd)]
    public async Task DoubleSidedDfsKeepsIdenticallyNamedFilesOnTheirOwnSide(string format)
    {
        var blank = RetroMediaLab.MediaEngine.Images.Creation.BlankSectorImageFactory.Create(
            format == DiskImageFormatIds.Jfd ? DiskImageFormatIds.AcornDfsDoubleSided : format);
        var blocks = blank.AvailableBlocks.ToDictionary(block => block.LogicalBlock, block => block.Data.ToArray());
        for (var head = 0; head < 2; head++)
        {
            "TEST   $"u8.CopyTo(blocks[head * 10].AsSpan(8));
            var metadata = blocks[head * 10 + 1]; metadata[5] = 8; metadata[12] = 1; metadata[15] = 12;
            blocks[20 + head * 10 + 2][0] = (byte)(65 + head);
        }
        var image = new SectorImage(format, 256, blank.Cylinders, 2, 10, blank.AvailableBlocks.Select(block => block with { Data = blocks[block.LogicalBlock] }));
        var document = MediaImageDocument.CreateUnexplored(new("memory.dsd", []), format, MediaKind.Floppy, new SectorMediaImageRepresentation(image), [], new Dictionary<string, string>());
        var result = await MediaEngineComposition.CreateDefault().Explorer.ExploreAsync(document);
        var volume = Assert.Single(result.Volumes).FileSystem!;
        Assert.Equal(blank.Capacity, volume.Capacity);
        Assert.Empty(volume.Warnings);
        for (var head = 0; head < 2; head++)
        {
            var file = Assert.Single(volume.Entries[head].Children);
            Assert.Equal("TEST", file.Name); Assert.Equal(new[] { (byte)(65 + head) }, file.Content);
            Assert.True(file.MetadataValid);
        }
    }

    [Theory]
    [InlineData(DiskImageFormatIds.AcornDfsSingleSided, 40, 1)]
    [InlineData(DiskImageFormatIds.AcornDfsSingleSided80, 80, 1)]
    [InlineData(DiskImageFormatIds.AcornDfsDoubleSided, 40, 2)]
    [InlineData(DiskImageFormatIds.AcornDfsDoubleSided80, 80, 2)]
    public async Task BlankDfsHasAnEmptyCatalogueOnEachPhysicalSide(string format, int cylinders, int heads)
    {
        var image = RetroMediaLab.MediaEngine.Images.Creation.BlankSectorImageFactory.Create(format);
        Assert.Equal(cylinders, image.Cylinders); Assert.Equal(heads, image.Heads);
        Assert.Equal(cylinders * heads * 10 * 256, image.Capacity);
        for (var head = 0; head < heads; head++)
        {
            Assert.True(image.TryGetBlock(head * 10 + 1, out var catalogue));
            Assert.Equal(0, catalogue.Data[5]);
            Assert.Equal(cylinders * 10, (catalogue.Data[6] & 3) * 256 + catalogue.Data[7]);
        }
        var document = MediaImageDocument.CreateUnexplored(new("memory" + (heads == 1 ? ".ssd" : ".dsd"), []), format, MediaKind.Floppy, new SectorMediaImageRepresentation(image), [], new Dictionary<string, string>());
        var result = await MediaEngineComposition.CreateDefault().Explorer.ExploreAsync(document);
        var volume = Assert.Single(result.Volumes).FileSystem!;
        Assert.NotNull(volume);
        if (heads == 1) Assert.Empty(volume.Entries);
        else
        {
            Assert.Equal(new[] { "0", "2" }, volume.Entries.Select(entry => entry.Name));
            Assert.All(volume.Entries, entry => { Assert.True(entry.MetadataValid); Assert.Empty(entry.Children); });
        }
    }

    [Theory]
    [InlineData(DiskImageFormatIds.AcornAdfs160, 40, 1, 160 * 1024)]
    [InlineData(DiskImageFormatIds.AcornAdfs320, 80, 1, 320 * 1024)]
    [InlineData(DiskImageFormatIds.AcornAdfs640, 80, 2, 640 * 1024)]
    public void BbcAdfsGeometriesRetainTheirSectorSizeAndPhysicalSides(string format, int cylinders, int heads, int capacity)
    {
        var geometry = Assert.Single(RetroMediaLab.MediaEngine.Images.Formats.Floppy.Adf.AcornAdfGeometry.BbcGeometries, candidate => candidate.FormatId == format);
        Assert.Equal(cylinders, geometry.Cylinders); Assert.Equal(heads, geometry.Heads); Assert.Equal(capacity, geometry.Capacity);
        Assert.Equal(256, geometry.BlockSize); Assert.Equal(16, geometry.SectorsPerTrack);
    }

    [Fact]
    public async Task WinchesterScanCoversEveryTrackAndSurfaceWithLbaPositions()
    {
        var bytes = Image();
        var document = MediaImageDocument.CreateUnexplored(new("memory.dat", []), HardDiskImageFormatIds.AcornWinchester, MediaKind.HardDisk,
            new BlockMediaImageRepresentation(bytes.Length, 256, [new(0, bytes.Length, MediaDataRangeKind.Stored, new MemoryRandomAccessData(bytes))], new(2, 4, 33, 256)), [], new Dictionary<string, string>());
        var descriptor = new BlockMediaVisualizationProvider().CreateDescriptor(document);
        Assert.Equal(new[] { 0, 1, 2, 3 }, descriptor.Surfaces);
        Assert.Equal(8, descriptor.Elements.Count);
        Assert.Equal(Enumerable.Range(0, 8).Select(index => (long)index * 33), descriptor.Elements.Select(element => element.Position));
        Assert.All(descriptor.Elements, element => Assert.Equal(33, element.Length));
        foreach (var element in descriptor.Elements) Assert.True(await BlockMediaVisualizationProvider.ReadElementAsync(document, element));
        var model = new BlockMediaInspectorPresenter((key, _) => key).BuildRenderModel(document);
        Assert.Equal(8, model.Ranges.Count);
        Assert.Equal(264, model.Ranges.Sum(range => range.Length));
        var renderer = new SkiaHardDiskGeometryRenderer();
        Assert.Equal(3, renderer.HitTest(model, 800, 800, new(700, 600)));
        Assert.Equal(231, renderer.HitTestPosition(model, 800, 800, new(700, 600)));
        Assert.Null(renderer.HitTest(model, 800, 800, new(600, 600)));
    }

    [Fact]
    public async Task UnavailableTrackIsNotReportedAsReadable()
    {
        const int trackBytes = 33 * 256;
        var document = MediaImageDocument.CreateUnexplored(new("memory.dat", []), HardDiskImageFormatIds.AcornWinchester, MediaKind.HardDisk,
            new BlockMediaImageRepresentation(trackBytes * 2, 256, [new(0, trackBytes, MediaDataRangeKind.Zero), new(trackBytes, trackBytes, MediaDataRangeKind.Unavailable)], new(1, 2, 33, 256)), [], new Dictionary<string, string>());
        var descriptor = new BlockMediaVisualizationProvider().CreateDescriptor(document);
        Assert.True(await BlockMediaVisualizationProvider.ReadElementAsync(document, descriptor.Elements[0]));
        Assert.False(await BlockMediaVisualizationProvider.ReadElementAsync(document, descriptor.Elements[1]));
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => BlockMediaVisualizationProvider.ReadElementAsync(document, descriptor.Elements[0], cancellation.Token));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BbcAdfsExploresDirectoriesAndFilesWith256ByteSectors(bool hardDisk)
    {
        var bytes = Image();
        var engine = MediaEngineComposition.CreateDefault();
        RetroMediaLab.MediaEngine.Interfaces.IMediaImageRepresentation representation = hardDisk
            ? new BlockMediaImageRepresentation(bytes.Length, 256, [new(0, bytes.Length, MediaDataRangeKind.Stored, new MemoryRandomAccessData(bytes))], new(2, 4, 33, 256))
            : new SectorMediaImageRepresentation(new SectorImage("acorn.adfs.bbc", 256, 2, 4, 33, Enumerable.Range(0, bytes.Length / 256).Select(index => new SectorBlock(index, new(index / 132, index / 33 % 4, index % 33), bytes.AsSpan(index * 256, 256).ToArray()))));
        var document = MediaImageDocument.CreateUnexplored(new("memory.dat", []), hardDisk ? HardDiskImageFormatIds.AcornWinchester : "acorn.adfs.bbc", hardDisk ? MediaKind.HardDisk : MediaKind.Floppy, representation, [], new Dictionary<string, string>());
        var result = await engine.Explorer.ExploreAsync(document);
        var volume = Assert.Single(result.Volumes).FileSystem!;
        Assert.NotNull(volume);
        var file = Assert.Single(volume.Entries, entry => entry.Name == "HELLO");
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, file.Content!.ToArray());
        var folder = Assert.Single(volume.Entries, entry => entry.Name == "DIRX");
        Assert.Equal(new byte[] { 5, 6, 7 }, Assert.Single(folder.Children).Content!.ToArray());
    }

    internal static byte[] Image()
    {
        var bytes = new byte[2 * 4 * 33 * 256];
        void Directory(int sector, string name)
        {
            var offset = sector * 256;
            bytes[offset] = bytes[offset + 1274] = 1;
            "Hugo"u8.CopyTo(bytes.AsSpan(offset + 1)); "Hugo"u8.CopyTo(bytes.AsSpan(offset + 1275));
            System.Text.Encoding.ASCII.GetBytes(name).CopyTo(bytes, offset + 1228);
        }
        void Entry(int sector, int number, string name, int address, int length, bool folder = false)
        {
            var offset = sector * 256 + 5 + number * 26;
            System.Text.Encoding.ASCII.GetBytes(name).CopyTo(bytes, offset);
            bytes[offset] |= 128; bytes[offset + 1] |= 128;
            if (folder) bytes[offset + 3] |= 128;
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset + 18), (uint)length);
            bytes[offset + 22] = (byte)address;
            bytes[offset + 25] = 8; // Numéro de séquence BBC : ce n'est pas le bit répertoire FileCore.
        }
        Directory(2, "$"); Directory(7, "DIRX");
        Entry(2, 0, "HELLO", 12, 4); Entry(2, 1, "DIRX", 7, 1280, true); Entry(7, 0, "DEEP", 13, 3);
        new byte[] { 1, 2, 3, 4 }.CopyTo(bytes, 12 * 256); new byte[] { 5, 6, 7 }.CopyTo(bytes, 13 * 256);
        return bytes;
    }
}
