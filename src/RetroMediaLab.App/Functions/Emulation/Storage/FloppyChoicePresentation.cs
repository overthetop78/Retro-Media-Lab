using System.Globalization;
using System.IO;
using RetroMediaLab.App.Constants.Emulation;
using RetroMediaLab.App.Functions.Storage;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.Emulation.Contracts;

namespace RetroMediaLab.App.Functions.Emulation.Storage;

/// <summary>Libellés localisés issus de la géométrie, sans interpréter les noms techniques du catalogue.</summary>
internal static class FloppyChoicePresentation
{
    internal static (IReadOnlyDictionary<string, FloppyCreationFormatChoice[]> Groups, FloppyCreationFormatChoice Initial)
        CreationFormats(FloppyDriveModelChoice model)
    {
        var groups = model.CreationFormats.GroupBy(format => (format.DisplayName, format.Tracks, format.Sides, format.DoubleDensity, format.Capacity))
            .ToDictionary(group => group.First().FormatId, group => group.ToArray(), StringComparer.Ordinal);
        var initial = groups.Values.Select(group => group[0]).FirstOrDefault(format =>
            format.Tracks == model.Tracks && format.Sides == model.Sides && format.DoubleDensity == model.DoubleDensity)
            ?? model.CreationFormats.OrderByDescending(format => format.Capacity).First();
        return (groups, initial);
    }

    internal static string Model(FloppyDriveModelChoice model, string? moduleId)
    {
        if (model.Sides > 0 && model.Tracks > 0)
        {
            var diameter = model.DiameterInches.ToString("0.##", CultureInfo.CurrentCulture);
            return $"{diameter}″ — {Geometry(model.Sides, model.Tracks, model.DoubleDensity)} — {StorageSizeFormatter.FormatBytes(model.BlankImageSize)}";
        }
        return string.IsNullOrWhiteSpace(model.DisplayResourceKey) ? model.InvariantDisplayValue ?? model.Value
            : moduleId is null ? LocExtension.Get(model.DisplayResourceKey) : LocExtension.GetForModule(moduleId, model.DisplayResourceKey);
    }

    internal static string DiskFormat(FloppyCreationFormatChoice format) => format.Sides > 0 && format.Tracks > 0
        ? $"{format.DisplayName} — {Geometry(format.Sides, format.Tracks, format.DoubleDensity)} — {StorageSizeFormatter.FormatBytes(format.Capacity)}"
        : format.DisplayName;

    internal static FloppyCreationFormatChoice ImageFormat(FloppyCreationFormatChoice format) => format with
    {
        DisplayName = ExtensionLabel(format.Extension)
    };

    internal static FloppyCreationFormatChoice? SelectedImageFormat(
        FloppyCreationFormatChoice[] formats, FloppyCreationFormatChoice? previous) =>
        formats.FirstOrDefault(format => format.Extension == previous?.Extension)
        ?? (formats.Length == 1 ? formats[0] : null);

    internal static (string Filter, string FileName) DestinationDialog(
        IEnumerable<FloppyCreationFormatChoice> formats, FloppyCreationFormatChoice? selected, string currentPath)
    {
        var extensions = (selected is null ? formats.Select(format => format.Extension) : [selected.Extension])
            .Distinct(StringComparer.OrdinalIgnoreCase);
        var filter = string.Join("|", extensions.Select(extension => $"{ExtensionLabel(extension)}|*{extension}"));
        var fileName = string.IsNullOrWhiteSpace(currentPath)
            ? MediaCreationConstants.BlankFileName + selected?.Extension
            : Path.GetFileName(currentPath);
        return (filter, fileName);
    }

    internal static string DestinationForFormat(string path, FloppyCreationFormatChoice? format) =>
        format is null || string.IsNullOrWhiteSpace(path) ? path : Path.ChangeExtension(path, format.Extension);

    private static string ExtensionLabel(string extension) => $"{extension.TrimStart('.').ToUpperInvariant()} (*{extension})";

    private static string Geometry(int sides, int tracks, bool doubleDensity) => string.Join(", ",
        LocExtension.Get(sides == 1 ? "Emulation.Storage.Floppy.OneSide" : "Emulation.Storage.Floppy.TwoSides"),
        LocExtension.Get("Emulation.Storage.Floppy.Tracks", tracks),
        LocExtension.Get(doubleDensity ? "Emulation.Storage.Floppy.DoubleDensity" : "Emulation.Storage.Floppy.SingleDensity"));
}
