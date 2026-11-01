using RetroMediaLab.MediaAnalysis.Constants;
using RetroMediaLab.MediaAnalysis.Contracts;
using RetroMediaLab.MediaAnalysis.Enums;

namespace RetroMediaLab.MediaAnalysis.Dictionaries.ContentRecognition;

internal static class AtariCommonMediaContentRecognitionTable
{
    private const MediaFileSystemFamily Family = MediaFileSystemFamily.Atari;

    // Famille | Extension | Signatures | Catégorie | Encodage | Exécution | Aperçu
    public static IReadOnlyList<MediaContentRecognitionRule> Rows { get; } =
    [
        new(Family, MediaContentRecognitionPriority.Standard, FileTypeExtensions.Bas, [], MediaContentCategory.BasicProgram, MediaTextEncoding.Unknown, MediaExecutionKind.None, MediaPreviewKind.BasicListing),
    ];
}
