
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Raw;

using RetroMediaLab.MediaEngine.Images.Models.Sectors;

namespace RetroMediaLab.MediaEngine.Images.Formats.Floppy.Adf;

/// <summary>Écrit les géométries sectorielles Acorn dans leur ordre logique, sans rembourrage ambigu.</summary>
public sealed class AcornAdfWriter(LinearSectorImageWriter writer)
{
    /// <summary>Crée un Writer Acorn ADF utilisant le Writer linéaire commun.</summary>
    public AcornAdfWriter() : this(new LinearSectorImageWriter()) { }

    internal static bool CanWrite(SectorImage image)
        => AcornAdfGeometry.All.FirstOrDefault(geometry => geometry.FormatId.Equals(image.FormatId, StringComparison.OrdinalIgnoreCase)) is { } geometry
            && LinearSectorImageWriter.MatchesGeometry(image, geometry)
            && LinearSectorImageWriter.HasCompletePayload(image, geometry.BlockSize);

    /// <summary>Écrit les blocs validés selon la géométrie annoncée par l'image.</summary>
    public Task WriteAsync(SectorImage image, string path, CancellationToken cancellationToken = default) => writer.WriteAsync(image, path, AcornAdfGeometry.All.Single(geometry => geometry.FormatId.Equals(image.FormatId, StringComparison.OrdinalIgnoreCase)), cancellationToken);
}
