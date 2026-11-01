using System.IO;
using RetroMediaLab.MediaFileSystems.Constants;
using RetroMediaLab.MediaFileSystems.Definitions;
using RetroMediaLab.MediaFileSystems.Functions;
using RetroMediaLab.MediaFileSystems.Interfaces;

namespace RetroMediaLab.MediaFileSystems.FileSystems.Atari.KFile;

/// <summary>Reconnaît les disquettes Atari K-file amorcées par KBoot, sans inventer de nom de fichier.</summary>
public sealed class AtariKFileFileSystemReader : IFileSystemReader
{
    public string Id => FileSystemIds.AtariKFile;

    public IReadOnlySet<string> CatalogFormatIds => AtariKFileFormatCatalog.FormatIds;

    public bool CanRead(IMediaSectorImage image) => CatalogFormatIds.Contains(image.FormatId) && AtariKFileFunctions.IsKBootFile(image);

    public FileSystemVolume Read(IMediaSectorImage image)
    {
        if (!CanRead(image))
            throw new InvalidDataException(AtariKFileConstants.InvalidImageMessage);

        return new FileSystemVolume(
            string.Empty,
            FileSystemIds.AtariKFile,
            image.Capacity,
            0,
            null,
            null,
            [],
            [],
            freeSpaceKnown: false,
            attributes:
            [
                AtariKFileConstants.BootableAttribute,
                AtariKFileConstants.SingleProgramAttribute,
                AtariKFileConstants.KBootAttribute
            ],
            bootable: true,
            fileSystemDisplayName: FileSystemDisplayNames.AtariKFile);
    }
}
