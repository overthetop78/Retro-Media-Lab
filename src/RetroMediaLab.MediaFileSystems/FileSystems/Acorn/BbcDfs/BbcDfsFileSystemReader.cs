using System.Collections.Frozen;
using RetroMediaLab.MediaFileSystems.Constants;
using RetroMediaLab.MediaFileSystems.Interfaces;

namespace RetroMediaLab.MediaFileSystems.FileSystems.Acorn.BbcDfs;
/// <summary>Lit les catalogues BBC DFS simple et double face.</summary>
public sealed partial class BbcDfsFileSystemReader : IFileSystemReader
{
    /// <summary>Identifiant technique du lecteur.</summary>
    public string Id => Definitions.FileSystemIds.AcornDfs;
    /// <summary>Formats BBC DFS pris en charge.</summary>
    public IReadOnlySet<string> CatalogFormatIds { get; } = new[]
    {
        MediaImageFormatIds.AcornAtomDos,
        MediaImageFormatIds.Jfd,
        MediaImageFormatIds.AcornDfsSingleSided,
        MediaImageFormatIds.AcornDfsSingleSided80,
        MediaImageFormatIds.AcornDfsDoubleSided,
        MediaImageFormatIds.AcornDfsDoubleSided80
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public bool CanRead(IMediaSectorImage image)
    {
        if (IsDoubleSided(image)) return Enumerable.Range(0, image.Heads).Any(head => CanRead(new SideImage(image, head)));
        if (image.BlockSize
            != BbcDfsFileSystemLayout.SectorSize 
            || !image.TryGetBlock(BbcDfsFileSystemLayout.NamesSector,
            out var names) 
            || !image.TryGetBlock(BbcDfsFileSystemLayout.MetadataSector,
            out var metadata) 
            || names.Data.Count 
            != BbcDfsFileSystemLayout.SectorSize 
            || metadata.Data.Count 
            != BbcDfsFileSystemLayout.SectorSize)
            return false;
        var count = metadata.Data[BbcDfsFileSystemLayout.EntryCountOffset];
        if (!CatalogFormatIds.Contains(image.FormatId)
            && !HasStructuredCatalogue(image, names.Data.ToArray(), metadata.Data.ToArray())) return false;
        return count 
            % BbcDfsFileSystemLayout.EntryPartSize 
            == 0 
            && count 
            <= BbcDfsFileSystemLayout.MaximumEntryCount 
            * BbcDfsFileSystemLayout.EntryPartSize 
            && ((metadata.Data[BbcDfsFileSystemLayout.TotalSectorsHighOffset] 
            & BbcDfsFileSystemLayout.TotalSectorsHighMask) 
            << BitConstants.BitsPerByte 
            | metadata.Data[BbcDfsFileSystemLayout.TotalSectorsLowOffset]) 
            <= image.BlockCount;
    }

    private static bool HasStructuredCatalogue(IMediaSectorImage image, ReadOnlySpan<byte> names, ReadOnlySpan<byte> metadata)
    {
        var total = ((metadata[BbcDfsFileSystemLayout.TotalSectorsHighOffset] & BbcDfsFileSystemLayout.TotalSectorsHighMask) << 8)
            | metadata[BbcDfsFileSystemLayout.TotalSectorsLowOffset];
        var count = metadata[BbcDfsFileSystemLayout.EntryCountOffset];
        if (image.SectorsPerTrack != BbcDfsFileSystemLayout.SectorsPerTrack || total is not (400 or 800)
            || total > image.BlockCount || count % BbcDfsFileSystemLayout.EntryPartSize != 0
            || count > BbcDfsFileSystemLayout.MaximumEntryCount * BbcDfsFileSystemLayout.EntryPartSize) return false;
        for (var offset = BbcDfsFileSystemLayout.FirstEntryOffset; offset < count + BbcDfsFileSystemLayout.FirstEntryOffset; offset += BbcDfsFileSystemLayout.EntryPartSize)
        {
            for (var character = 0; character < BbcDfsFileSystemLayout.EntryPartSize; character++)
                if ((names[offset + character] & BbcDfsFileSystemLayout.CharacterMask) is < 32 or > 126) return false;
            var start = BbcDfsEntryDecoder.StartSector(metadata, offset);
            var length = BbcDfsEntryDecoder.Length(metadata, offset);
            if (start < 2 || start + (length + BbcDfsFileSystemLayout.SectorSize - 1) / BbcDfsFileSystemLayout.SectorSize > total) return false;
        }
        return true;
    }

    public FileSystemVolume Read(IMediaSectorImage image)
    {
        if (IsDoubleSided(image)) return ReadSides(image);
        if (!CanRead(image))
        {
            var count = image.TryGetBlock(BbcDfsFileSystemLayout.MetadataSector, out var block) && block.Data.Count > BbcDfsFileSystemLayout.EntryCountOffset ? block.Data[BbcDfsFileSystemLayout.EntryCountOffset] / BbcDfsFileSystemLayout.EntryPartSize : -1;
            throw BbcDfsFileSystemExceptions.InvalidCatalog(count, image.BlockCount);
        }

        image.TryGetBlock(BbcDfsFileSystemLayout.NamesSector, out var namesBlock);
        var names = namesBlock.Data.ToArray().AsSpan();
        image.TryGetBlock(BbcDfsFileSystemLayout.MetadataSector, out var metadataBlock);
        var metadata = metadataBlock.Data.ToArray().AsSpan();
        var title = BbcDfsNameCodec.Decode(names[..BbcDfsFileSystemLayout.TitleFirstLength]) + BbcDfsNameCodec.Decode(metadata[..BbcDfsFileSystemLayout.TitleSecondLength]);
        var totalSectors = (metadata[BbcDfsFileSystemLayout.TotalSectorsHighOffset] & BbcDfsFileSystemLayout.TotalSectorsHighMask) << BitConstants.BitsPerByte | metadata[BbcDfsFileSystemLayout.TotalSectorsLowOffset];
        if (totalSectors == 0 && image.FormatId.Equals(MediaImageFormatIds.AcornAtomDos, StringComparison.OrdinalIgnoreCase))
            totalSectors = image.BlockCount;
        var fileCount = metadata[BbcDfsFileSystemLayout.EntryCountOffset] / BbcDfsFileSystemLayout.EntryPartSize;
        var entries = new List<FileSystemEntry>(fileCount);
        var warnings = new List<string>();
        var usedSectors = 2;
        for (var index = 0; index < fileCount; index++)
        {
            var nameOffset = BbcDfsFileSystemLayout.FirstEntryOffset + index * BbcDfsFileSystemLayout.EntryPartSize;
            var metaOffset = nameOffset;
            var leaf = BbcDfsNameCodec.Decode(names.Slice(nameOffset, BbcDfsFileSystemLayout.LeafNameLength));
            var directoryByte = names[nameOffset + BbcDfsFileSystemLayout.DirectoryOffset];
            var directory = (char)(directoryByte & BbcDfsFileSystemLayout.CharacterMask);
            var atomDefaultQualifier = image.FormatId.Equals(MediaImageFormatIds.AcornAtomDos, StringComparison.OrdinalIgnoreCase) && directory == ' ';
            var name = directory == BbcDfsFileSystemLayout.RootDirectory || atomDefaultQualifier ? leaf : $"{directory}.{leaf}";
            var length = BbcDfsEntryDecoder.Length(metadata, metaOffset);
            var start = BbcDfsEntryDecoder.StartSector(metadata, metaOffset);
            var load = BbcDfsEntryDecoder.Load(metadata, metaOffset);
            var execute = BbcDfsEntryDecoder.Execute(metadata, metaOffset);
            var sectorCount = (length + BbcDfsFileSystemLayout.SectorSize - 1) / BbcDfsFileSystemLayout.SectorSize;
            usedSectors += sectorCount;
            var content = new byte[length];
            var copied = 0;
            var valid = true;
            for (var sector = 0; sector < sectorCount; sector++)
            {
                var targetSector = start + sector;
                if (targetSector >= totalSectors || !image.TryGetBlock(targetSector, out var block))
                {
                    warnings.Add(BbcDfsFileSystemExceptions.FileOutsideImage(name, start, length));
                    valid = false;
                    continue;
                }

                var targetOffset = sector * BbcDfsFileSystemLayout.SectorSize;
                var count = Math.Min(BbcDfsFileSystemLayout.SectorSize, length - targetOffset);
                if (block.Data.Count < count)
                {
                    valid = false;
                    continue;
                }

                for (var byteIndex = 0; byteIndex < count; byteIndex++)
                    content[targetOffset + byteIndex] = block.Data[byteIndex];
                copied += count;
            }

            var comment = BbcDfsNameCodec.Description(load, execute);
            entries.Add(new(name, FileSystemEntryKind.File, length, null, comment, (uint)((directoryByte & BbcDfsFileSystemLayout.LockedBit) != 0 ? 1 : 0), start, valid, [], content));
        }

        var capacity = (long)totalSectors * BbcDfsFileSystemLayout.SectorSize;
        return new(
            title.Trim(),
            Definitions.FileSystemIds.AcornDfs,
            capacity,
            Math.Max(0, totalSectors - usedSectors) * (long)BbcDfsFileSystemLayout.SectorSize,
            null,
            null,
            entries.OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase).ToArray(),
            warnings
        );
    }
}
