using RetroMediaLab.MediaFileSystems.FileSystems.Fat12;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Images.Reading.Reconstruction;

using RetroMediaLab.MediaEngine.Images.Models.Sectors;

namespace RetroMediaLab.MediaEngine.Images.Formats.Floppy.Raw;

/// <summary>Écrit les conteneurs sectoriels IBM IMA et IMG dans l'ordre logique CHS.</summary>
public sealed class IbmRawImageWriter(LinearSectorImageWriter writer)
{
    /// <summary>Crée un Writer IBM utilisant le Writer linéaire commun.</summary>
    public IbmRawImageWriter() : this(new LinearSectorImageWriter()) { }

    internal static bool CanWrite(SectorImage image, string formatId)
        => LinearSectorImageWriter.HasCompletePayload(image, image.BlockSize)
            && (formatId.Equals(DiskImageFormatIds.IbmScan, StringComparison.OrdinalIgnoreCase)
                ? image.BlockCount == (long)image.Cylinders * image.Heads * image.SectorsPerTrack
                : IbmPcGeometryCatalog.TryFromFormatId(formatId, out var geometry) && MatchesGeometry(image, geometry));

    private static bool MatchesGeometry(SectorImage image, IbmPcGeometry geometry)
        => image.BlockSize == FatBootSectorLayout.SectorSize
            && image.Cylinders == geometry.Cylinders && image.Heads == geometry.Heads
            && image.SectorsPerTrack == geometry.SectorsPerTrack
            && image.BlockCount == geometry.Cylinders * geometry.Heads * geometry.SectorsPerTrack;

    /// <summary>Écrit le profil IBM explicitement demandé après validation de tous les blocs.</summary>
    public Task WriteAsync(SectorImage image, string path, string formatId, CancellationToken cancellationToken = default)
    {
        if (formatId.Equals(DiskImageFormatIds.IbmScan, StringComparison.OrdinalIgnoreCase))
            return writer.WriteAsync(image.WithFormatId(formatId), path,
                new RegularSectorGeometry(formatId, image.BlockSize, image.Cylinders, image.Heads, image.SectorsPerTrack), cancellationToken);
        if (!IbmPcGeometryCatalog.TryFromFormatId(formatId, out var geometry)) throw IbmRawImageWriterExceptions.UnsupportedFormat(formatId);
        var expected = new RegularSectorGeometry(formatId, FatBootSectorLayout.SectorSize, geometry.Cylinders, geometry.Heads, geometry.SectorsPerTrack);
        var source = image.FormatId.Equals(formatId, StringComparison.OrdinalIgnoreCase) ? image : image.WithFormatId(formatId);
        if (!MatchesGeometry(image, geometry)) throw IbmRawImageWriterExceptions.GeometryMismatch(image.FormatId, formatId);
        return writer.WriteAsync(source, path, expected, cancellationToken);
    }
}
