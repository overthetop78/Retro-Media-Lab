using RetroMediaLab.MediaFileSystems.Exploration.Interpretation;
using RetroMediaLab.MediaEngine.Contracts.Explorer;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Scp;
using RetroMediaLab.MediaEngine.Images.Reading;

using RetroMediaLab.MediaEngine.Images.Models.Sectors;

namespace RetroMediaLab.MediaEngine.Images.Formats.Floppy.Scp.Inspection;

/// <summary>Classe les inspections SCP dans leur ordre et conserve le premier résultat en cas d'égalité.</summary>
internal static class ScpCandidateRanker
{
    /// <summary>Résultat agrégé du classement automatique.</summary>
    internal sealed record Result(
        SectorImage? BestDecoded,
        SectorImage? BestRecognized,
        ExploredFileSystem? BestFileSystem,
        IReadOnlyList<ExploredFileSystem> Detected,
        IReadOnlyList<SectorImage> DecodedImages,
        IReadOnlyList<ScpCandidateInspection> Rejected);

    /// <summary>Calcule une fois chaque score, déduplique les systèmes et conserve l'ordre des formats et diagnostics.</summary>
    public static Result Rank(IEnumerable<ScpCandidateInspection> inspections, ScpImage source)
    {
        SectorImage? bestDecoded = null;
        SectorImage? bestRecognized = null;
        ExploredFileSystem? bestFileSystem = null;
        var bestDecodedScore = ScpExplorationThresholds.NoRecognizedScore;
        FileSystemEvidence? bestFileSystemEvidence = null;
        var detected = new List<ExploredFileSystem>();
        var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var decodedImages = new List<SectorImage>();
        var formatIdentity = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rejected = new List<ScpCandidateInspection>();
        foreach (var inspection in inspections)
        {
            var image = NormalizeCompatibleGeometry(inspection.Image, source);
            if (image is null || !HasCompatibleGeometry(image, source) && !HasCredibleSparseGeometry(image, inspection, source))
            {
                rejected.Add(inspection);
                continue;
            }

            var decodedScore = DiskImageDecodeScore.Calculate(image);
            if (decodedScore > bestDecodedScore)
            {
                bestDecoded = image;
                bestDecodedScore = decodedScore;
            }

            if (decodedScore >= ScpExplorationThresholds.MinimumDecodedFormatScore
                && formatIdentity.Add(image.FormatId))
            {
                decodedImages.Add(image);
            }

            foreach (var recognized in inspection.Matches)
            {
                if (!FileSystemAlternativePolicy.IsCredible(recognized.Volume))
                {
                    continue;
                }

                var evidence = FileSystemEvidence.From(recognized.Volume, DiskImageDecodeScore.Calculate(recognized.Image));
                if (bestFileSystemEvidence is null || evidence.IsBetterThan(bestFileSystemEvidence.Value))
                {
                    bestRecognized = recognized.Image;
                    bestFileSystem = recognized;
                    bestFileSystemEvidence = evidence;
                }

                if (identities.Add(FileSystemInterpretationIdentity.Create(recognized.FormatId, recognized.Volume)))
                {
                    detected.Add(recognized);
                }
            }
        }
        return new(bestDecoded, bestRecognized, bestFileSystem, detected, decodedImages, rejected);
    }

    private static bool HasCompatibleGeometry(SectorImage image, ScpImage source)
    {
        var sourceHeads = source.Tracks.Select(track => track.Head).Distinct().Count();
        var sourceCylinders = source.Tracks.Select(track => track.Cylinder).Distinct().Count();
        var minimumCylinders = Math.Max(1, (int)Math.Floor(sourceCylinders * 0.9));
        return image.Heads == sourceHeads && image.Cylinders >= minimumCylinders && image.Cylinders <= sourceCylinders;
    }

    private static bool HasCredibleSparseGeometry(SectorImage image, ScpCandidateInspection inspection, ScpImage source)
        => source.Tracks.Count > 0 && image.Heads == source.Tracks.Max(track => track.Head) + 1
            && image.Cylinders >= source.Tracks.Max(track => track.Cylinder) + 1
            && image.Cylinders > source.Tracks.Select(track => track.Cylinder).Distinct().Count()
            && image.AvailableBlocks.Count < image.BlockCount
            && inspection.Matches.Any(match => match.Image.FormatId == image.FormatId && FileSystemAlternativePolicy.IsCredible(match.Volume));

    internal static SectorImage? NormalizeCompatibleGeometry(SectorImage? image, ScpImage source)
    {
        if (image is null) return null;
        var sourceHeads = source.Tracks.Select(track => track.Head).Distinct().Count();
        var sourceCylinders = source.Tracks.Select(track => track.Cylinder).Distinct().Count();
        if (sourceHeads != 2 || sourceCylinders < 80 || image.Heads != 2 || image.BlockSize != 512 || image.SectorsPerTrack != 9)
            return image;

        var formatId = image.FormatId.StartsWith(RetroMediaLab.MediaEngine.Constants.DiskImageFormatIds.AtariStPrefix, StringComparison.OrdinalIgnoreCase)
            ? RetroMediaLab.MediaEngine.Constants.DiskImageFormatIds.AtariSt720
            : image.FormatId.StartsWith(RetroMediaLab.MediaEngine.Constants.DiskImageFormatIds.IbmPrefix, StringComparison.OrdinalIgnoreCase) ? RetroMediaLab.MediaEngine.Constants.DiskImageFormatIds.Ibm720 : null;
        return formatId is null ? image : new SectorImage(formatId, 512, 80, 2, 9, image.AvailableBlocks);
    }

}
