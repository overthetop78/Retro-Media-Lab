using RetroMediaLab.MediaFileSystems.Formats.Commodore;
using RetroMediaLab.MediaEngine.Images.Reading.Reconstruction;
using RetroMediaLab.MediaEngine.Constants;

using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Raw;

using RetroMediaLab.MediaEngine.Images.Models.Sectors;

namespace RetroMediaLab.MediaEngine.Images.Formats.Floppy.D81;

/// <summary>Écrit les 3 200 blocs logiques d'une image Commodore 1581 dans un conteneur D81.</summary>
public sealed class D81Writer(LinearSectorImageWriter writer)
{
    private static readonly RegularSectorGeometry Geometry = new(DiskImageFormatIds.Commodore1581, Commodore1581Geometry.LogicalBlockSize, Commodore1581Geometry.LogicalCylinderCount, Commodore1581Geometry.LogicalHeadCount, Commodore1581Geometry.LogicalBlocksPerTrack);

    internal static bool CanWrite(SectorImage image) => LinearSectorImageWriter.MatchesGeometry(image, Geometry);

    /// <summary>Valide la géométrie logique D81 puis écrit chaque bloc sans remplissage implicite.</summary>
    public Task WriteAsync(SectorImage image, string path, CancellationToken cancellationToken = default)
    {
        return writer.WriteAsync(image, path, Geometry, cancellationToken);
    }
}
