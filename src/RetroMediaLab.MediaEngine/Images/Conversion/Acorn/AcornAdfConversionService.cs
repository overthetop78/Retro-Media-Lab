using RetroMediaLab.MediaEngine.Images.Reading.Reconstruction.Iso;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Adf;

using RetroMediaLab.MediaEngine.Images.Models.Sectors;

namespace RetroMediaLab.MediaEngine.Images.Conversion.Acorn;

/// <summary>Convertit les images sectorielles Acorn selon le profil physique cible.</summary>
public sealed class AcornAdfConversionService(IsoScpSectorImageReader scpReader, AdfReader reader, AcornAdfWriter writer)
{
    /// <summary>Annonce ADF pour les géométries Acorn et ADS/ADM/ADL pour les géométries BBC correspondantes.</summary>
    public static bool CanCreate(string formatId, string extension) => AcornAdfGeometry.All.Any(geometry => geometry.FormatId.Equals(formatId, StringComparison.OrdinalIgnoreCase))
        && (formatId.Equals(DiskImageFormatIds.AcornDosPlus800, StringComparison.OrdinalIgnoreCase)
            ? extension.ToLowerInvariant() is ".img" or ".800"
            : extension.Equals(DiskImageFileExtensions.Adf, StringComparison.OrdinalIgnoreCase)
                || formatId.Equals(DiskImageFormatIds.AcornAdfs640, StringComparison.OrdinalIgnoreCase) && extension.ToLowerInvariant() is DiskImageFileExtensions.Adl or DiskImageFileExtensions.AcornAdfs640
                || formatId.Equals(DiskImageFormatIds.AcornAdfs160, StringComparison.OrdinalIgnoreCase) && extension.Equals(DiskImageFileExtensions.Ads, StringComparison.OrdinalIgnoreCase)
                || formatId.Equals(DiskImageFormatIds.AcornAdfs320, StringComparison.OrdinalIgnoreCase) && extension.Equals(DiskImageFileExtensions.Adm, StringComparison.OrdinalIgnoreCase));

    /// <summary>Reconstruit ou relit la source puis écrit l'ADF Acorn.</summary>
    public async Task ConvertAsync(string sourcePath, string outputPath, string formatId, CancellationToken cancellationToken = default)
    {
        var image = await MediaConversionService.ReadSectorSourceAsync(sourcePath, () => scpReader.ReadAsync(sourcePath, formatId, cancellationToken), () => reader.ReadAsync(sourcePath, cancellationToken)).ConfigureAwait(false);
        await writer.WriteAsync(image, outputPath, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Écrit une image sectorielle Acorn déjà reconstruite.</summary>
    public Task ConvertAsync(SectorImage image, string outputPath, CancellationToken cancellationToken = default) => writer.WriteAsync(image, outputPath, cancellationToken);
}
