using MediaSectorAddress = RetroMediaLab.MediaFileSystems.Contracts.MediaSectorAddress;
using MediaSectorGeometry = RetroMediaLab.MediaFileSystems.Contracts.MediaSectorGeometry;
using MediaSectorWritePlan = RetroMediaLab.MediaFileSystems.Contracts.MediaSectorWritePlan;
using RetroMediaLab.MediaFileSystems.FileSystems.Apple.ProDos;
using RetroMediaLab.MediaFileSystems.Migration;

namespace RetroMediaLab.MediaFileSystems.FileSystems.Apple.Sos;

/// <summary>Crée un volume Apple III SOS en conservant son profil distinct du conteneur ProDOS.</summary>
public sealed class SosVolumeWriter
{
    /// <summary>Crée la structure de volume commune ProDOS/SOS puis ajoute le marqueur d'amorçage SOS.</summary>
    public MediaSectorWritePlan Create(MigrationPlan plan, MediaSectorGeometry proDosGeometry, IReadOnlyList<MediaSectorAddress> addresses, string sosFormatId)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(proDosGeometry);
        ArgumentNullException.ThrowIfNull(addresses);
        ArgumentException.ThrowIfNullOrWhiteSpace(sosFormatId);
        var baseImage = new ProDosVolumeWriter().Create(plan, proDosGeometry, addresses);
        var blocks = baseImage.AvailableBlocks.Select(block => block with { Data = block.Data.ToArray() }).OrderBy(block => block.LogicalBlock).ToArray();
        var boot = blocks.Single(block => block.LogicalBlock == 0).Data.ToArray();
        SosBootFormat.Marker.CopyTo(boot.AsSpan(SosBootFormat.MarkerOffset));
        blocks[0] = blocks[0] with { Data = boot };
        return new(sosFormatId, baseImage.BlockSize, baseImage.Cylinders, baseImage.Heads, baseImage.SectorsPerTrack, blocks);
    }
}
