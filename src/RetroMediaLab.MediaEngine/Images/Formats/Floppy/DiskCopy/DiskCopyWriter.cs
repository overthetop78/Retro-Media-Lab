using System.Buffers.Binary;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Raw;
using RetroMediaLab.MediaEngine.Images.Models.Sectors;

namespace RetroMediaLab.MediaEngine.Images.Formats.Floppy.DiskCopy;
/// <summary>Écrit les conteneurs Apple DiskCopy 4.2 avec données, tags et checksums.</summary>
public sealed class DiskCopyWriter(RetroMediaLab.MediaEngine.Interfaces.IAtomicImageFileWriter? fileSystem = null)
{
    private readonly RetroMediaLab.MediaEngine.Interfaces.IAtomicImageFileWriter files = fileSystem ?? new RetroMediaLab.MediaEngine.Interfaces.AtomicImageFileWriter();
    internal static bool HasSupportedDataLayout(SectorImage image) => DataLayoutError(image) is null;

    private static string? DataLayoutError(SectorImage image)
    {
        // SectorImage guarantees unique logical indices within BlockCount.
        if (image.AvailableBlocks.Count != image.BlockCount)
            return "DiskCopy exige une image sectorielle complète et ordonnée.";
        long dataLength = 0;
        var tagged = false;
        var allTagsValid = true;
        foreach (var block in image.AvailableBlocks)
        {
            dataLength += block.Data.Count;
            tagged |= block.Tag is not null;
            allTagsValid &= block.Tag?.Count == DiskCopyLayout.TagSizePerBlock;
        }
        if (dataLength != (long)image.BlockCount * DiskCopyLayout.DataBlockSize)
            return "DiskCopy exige des blocs de 512 octets.";
        if (tagged && !allTagsValid)
            return "Tous les tags DiskCopy doivent contenir 12 octets.";
        return null;
    }
    /// <summary>Écrit une image avec les métadonnées d'en-tête indiquées.</summary>
    public async Task WriteAsync(SectorImage image, string path, DiskCopyImage? source = null, CancellationToken cancellationToken = default)
    {
        var metadata = source ?? CreateMetadata(image, Path.GetFileNameWithoutExtension(path));
        var bytes = Build(image, metadata);
        await files.WriteAsync(path, (stream, token) => stream.WriteAsync(bytes, token).AsTask(), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sérialise l'en-tête, les blocs et leurs tags facultatifs.</summary>
    internal static byte[] Build(SectorImage image, DiskCopyImage metadata)
    {
        if (DataLayoutError(image) is { } error) throw new InvalidDataException(error);
        var blocks = image.AvailableBlocks.OrderBy(block => block.LogicalBlock).ToArray();
        var data = blocks.SelectMany(block => block.Data).ToArray();
        var tagged = blocks.Any(block => block.Tag is not null);
        var tags = tagged ? blocks.SelectMany(block => block.Tag!).ToArray() : [];
        var container = new byte[checked(DiskCopyLayout.HeaderSize + data.Length + tags.Length)];
        WriteName(container, metadata.NameBytes);
        BinaryPrimitives.WriteUInt32BigEndian(container.AsSpan(DiskCopyLayout.DataLengthOffset), checked((uint)data.Length));
        BinaryPrimitives.WriteUInt32BigEndian(container.AsSpan(DiskCopyLayout.TagLengthOffset), checked((uint)tags.Length));
        BinaryPrimitives.WriteUInt32BigEndian(container.AsSpan(DiskCopyLayout.DataChecksumOffset), DiskCopyReader.CalculateChecksum(data));
        var tagChecksum = tags.Length == 0 ? DiskCopyFormat.MissingChecksum : DiskCopyReader.CalculateChecksum(tags.AsSpan(DiskCopyLayout.TagChecksumExcludedPrefixSize));
        BinaryPrimitives.WriteUInt32BigEndian(container.AsSpan(DiskCopyLayout.TagChecksumOffset), tagChecksum);
        container[DiskCopyLayout.DiskFormatOffset] = metadata.DiskFormat;
        container[DiskCopyLayout.FormatByteOffset] = metadata.FormatByte;
        BinaryPrimitives.WriteUInt16BigEndian(container.AsSpan(DiskCopyLayout.PrivateWordOffset), DiskCopyFormat.PrivateWord);
        data.CopyTo(container, DiskCopyLayout.HeaderSize);
        tags.CopyTo(container, DiskCopyLayout.HeaderSize + data.Length);
        return container;
    }

    /// <summary>Crée des métadonnées DiskCopy cohérentes pour une image sans en-tête source.</summary>
    private static DiskCopyImage CreateMetadata(SectorImage image, string name)
    {
        var diskFormat = image.Capacity switch
        {
            MacintoshGcrGeometry.Capacity400K => DiskCopyFormat.DiskFormat400K,
            MacintoshGcrGeometry.Capacity800K => DiskCopyFormat.DiskFormat800K,
            MacintoshMfmGeometry.Capacity => DiskCopyFormat.DiskFormat1440K,
            _ => throw new InvalidDataException($"La capacité DiskCopy {image.Capacity} n'est pas prise en charge.")
        };
        var formatByte = image.FormatId.StartsWith(DiskImageFormatIds.AppleLisaPrefix, StringComparison.OrdinalIgnoreCase) 
            ? DiskCopyFormat.FormatByteLisa 
            : image.FormatId.Contains("mfs", StringComparison.OrdinalIgnoreCase) ? DiskCopyFormat.FormatByteMacintoshMfs : DiskCopyFormat.FormatByteMacintoshHfs;
        return new(image, System.Text.Encoding.ASCII.GetBytes(name), diskFormat, formatByte);
    }

    /// <summary>Écrit le nom Pascal DiskCopy sans dépasser son champ de 64 octets.</summary>
    private static void WriteName(Span<byte> container, IReadOnlyList<byte> nameBytes)
    {
        var length = Math.Min(nameBytes.Count, DiskCopyLayout.MaximumNameLength);
        container[DiskCopyLayout.NameLengthOffset] = checked((byte)length);
        nameBytes.Take(length).ToArray().CopyTo(container[DiskCopyLayout.NameOffset..]);
    }
}
