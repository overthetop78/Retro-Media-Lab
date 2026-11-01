using System.Collections.Frozen;
using RetroMediaLab.MediaFileSystems.Constants;

namespace RetroMediaLab.MediaFileSystems.FileSystems.Atari.KFile;

/// <summary>Formats d'images pouvant contenir une disquette Atari K-file.</summary>
internal static class AtariKFileFormatCatalog
{
    public static IReadOnlySet<string> FormatIds { get; } = new[]
    {
        MediaImageFormatIds.Atari90,
        MediaImageFormatIds.Atari130,
        MediaImageFormatIds.Atari140,
        MediaImageFormatIds.Atari180,
        MediaImageFormatIds.AtariAtx,
        MediaImageFormatIds.AtariXfd90,
        MediaImageFormatIds.AtariXfd130,
        MediaImageFormatIds.AtariXfd140,
        MediaImageFormatIds.AtariXfd180
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
}
