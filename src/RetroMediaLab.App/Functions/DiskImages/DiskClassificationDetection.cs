using RetroMediaLab.MediaEngine.Images.Formats;

namespace RetroMediaLab.App.Functions.DiskImages;

internal static class DiskClassificationDetection
{
    internal static (IReadOnlySet<string> FormatIds, IReadOnlyDictionary<string, string> FormatByMachine, DiskFormat? PreferredFormat)
        Resolve(DiskClassificationCatalog catalog, string? preferredId, IEnumerable<string> detectedIds)
    {
        var resolved = detectedIds.Select(catalog.ResolveFormat).OfType<DiskFormat>()
            .DistinctBy(format => format.Id, StringComparer.OrdinalIgnoreCase).ToArray();
        var formatIds = resolved.Select(format => format.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var formatByMachine = resolved.GroupBy(format => format.Family, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Id, StringComparer.OrdinalIgnoreCase);
        return (formatIds, formatByMachine, catalog.ResolveFormat(preferredId) ?? resolved.FirstOrDefault());
    }
}
