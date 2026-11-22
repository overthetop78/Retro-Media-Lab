using RetroMediaLab.App.Constants.DiskImages;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.App.Constants.Controls.Visual;
using RetroMediaLab.App.Functions.Storage;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.ViewModels.Explorer;
using RetroMediaLab.MediaEngine.Contracts.Explorer;
using RetroMediaLab.MediaEngine.Images.Formats;
using RetroMediaLab.MediaEngine.Images.Models.Optical;
using RetroMediaLab.MediaEngine.Images.Reading.Recognition;

namespace RetroMediaLab.App.Presenters.Explorer;

internal static class ExplorerMediaChoicesPresenter
{
    private static readonly IReadOnlyList<DiskFormat> BuiltInFormats = new BuiltInImageFormatCatalog().Formats;

    internal static DiskFormat? FormatForIdentity(IReadOnlyList<DiskFormat> formats, string? formatId)
    {
        if (string.IsNullOrWhiteSpace(formatId)) return null;
        return formats.FirstOrDefault(format => string.Equals(format.Id, formatId, StringComparison.OrdinalIgnoreCase)) ?? BuiltInFormats.FirstOrDefault(format => string.Equals(format.Id, formatId, StringComparison.OrdinalIgnoreCase));
    }

    internal static ExplorerOpticalSessionChoice[]? Sessions(ExploredMediaImage? document)
    {
        if (document?.Document.Representation is not OpticalMediaImageRepresentation { Tracks: { } tracks }) return null;
        return tracks.Select(track => track.SessionNumber).Distinct().Order().Select(number => new ExplorerOpticalSessionChoice(number, $"{LocExtension.Get("Explorer.Session")} {number}")).ToArray();
    }

    internal static (ExplorerMediaVolumeChoice[] Choices, ExploredMediaVolume? Selected) Volumes(
        ExploredMediaImage document, int? session, ExploredMediaVolume? preferredVolume)
    {
        var volumes = document.Volumes.Where(volume => session is null || volume.Descriptor.SessionNumber is null || volume.Descriptor.SessionNumber == session).ToArray();
        var choices = volumes.Select((volume, index) => new ExplorerMediaVolumeChoice(volume, VolumeChoiceName(volume, index))).ToArray();
        var selectedVolume = preferredVolume is not null && volumes.Contains(preferredVolume) ? preferredVolume : volumes.FirstOrDefault(volume => volume.FileSystem is not null) ?? volumes.FirstOrDefault();
        return (choices, selectedVolume);
    }

    internal static ExplorerOpticalTrackChoice[] AudioTracks(ExploredMediaImage document, int? session)
    {
        return (document.Document.Representation as OpticalMediaImageRepresentation)?.Tracks?
            .Where(track => track.IsAudio && (session is null || track.SessionNumber == session))
            .Select(track => new ExplorerOpticalTrackChoice(track, $"{LocExtension.Get("Explorer.Track")} {track.TrackNumber} \u00b7 {LocExtension.Get("Explorer.Audio")}"))
            .ToArray() ?? [];
    }

    private static string VolumeChoiceName(ExploredMediaVolume volume, int index)
    {
        var name = volume.FileSystem?.Name ?? volume.Descriptor.Name;
        var prefix = string.IsNullOrWhiteSpace(name) ? $"{LocExtension.Get("Explorer.Volume")} {index + 1}" : name;
        var partition = volume.Descriptor.PartitionNumber is { } number ? $"{ControlVisualConstants.DetailSeparator}#{number}" : string.Empty;
        var track = volume.Descriptor.TrackNumber is { } trackNumber ? $" \u00b7 {LocExtension.Get("Explorer.Track")} {trackNumber}" : string.Empty;
        return $"{prefix}{partition}{track} \u00b7 {StorageSizeFormatter.FormatBytes(volume.Descriptor.Length)}";
    }

    internal static IReadOnlyList<string> ReportedFormats(ExploredDiskImage document)
    {
        return document.FormatsDetectes.Select(format => format.FormatId).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    internal static string DetectedFormatsSummary(IEnumerable<string> detectedFormatIds, IReadOnlyList<DiskFormat> formats)
    {
        if (detectedFormatIds.Contains(TapeImageFormatIds.AtariCas, StringComparer.OrdinalIgnoreCase))
            return LocExtension.Get("Explorer.DetectedFormats", $"{LocExtension.Get("System.atari-8bit")} (Atari CAS)");

        var catalog = new DiskClassificationCatalog(formats);
        var recognized = detectedFormatIds.Select(id => catalog.ResolveFormat(id)).Where(format => format is not null).Cast<DiskFormat>().DistinctBy(format => format.Id, StringComparer.OrdinalIgnoreCase)
            .Select(format => $"{format.Family} ({format.DisplayName})")
            .ToArray();
        var value = recognized.Length == 0 ? ControlVisualConstants.EmptyValue : string.Join(ControlVisualConstants.DetailSeparator, recognized);
        return LocExtension.Get("Explorer.DetectedFormats", value);
    }

    internal static ExplorerFormatChoice[] DetectedFormats(IEnumerable<string> detectedFormatIds, IReadOnlyList<DiskFormat> formats)
    {
        var catalog = new DiskClassificationCatalog(formats);
        return detectedFormatIds.Select(id => catalog.ResolveFormat(id)).Where(format => format is not null).Cast<DiskFormat>().DistinctBy(format => format.Id, StringComparer.OrdinalIgnoreCase)
            .Select(format => new ExplorerFormatChoice(format.Id, $"{format.Family}{ControlVisualConstants.DetailSeparator}{format.DisplayName}"))
            .ToArray();
    }

    internal static string CurrentSystem(ExploredDiskImage document, IReadOnlyList<DiskFormat> formats, string? selectedMachine)
    {
        var format = new DiskClassificationCatalog(formats).ResolveFormat(document.PrimaryFormatId);
        return format?.Family ?? selectedMachine ?? ExplorerMetadataPresenter.Systems(document.Metadata);
    }

    internal static string CurrentSystem(ExploredMediaImage document, IReadOnlyList<DiskFormat> formats, string? selectedMachine)
    {
        if (document.Document.FormatId.Equals(TapeImageFormatIds.AtariCas, StringComparison.OrdinalIgnoreCase)
            && document.Document.Metadata.TryGetValue(ExplorerMediaMetadataConstants.SystemId, out var tapeSystemId)
            && tapeSystemId == DiskSystemIds.Atari8Bit)
            return LocExtension.Get("System.atari-8bit");
        if (selectedMachine is not null) return selectedMachine;
        var format = new DiskClassificationCatalog(formats).ResolveFormat(document.Document.FormatId);
        return format?.Family
            ?? (document.Document.Metadata.TryGetValue(ExplorerMediaMetadataConstants.SystemId, out var systemId) && systemId == DiskSystemIds.Atari8Bit ? LocExtension.Get("System.atari-8bit") : null)
            ?? document.Document.MediaKind.ToString();
    }

    internal static string TapeDescription(ExploredMediaImage document, IReadOnlyList<DiskFormat> formats, string? selectedMachine)
    {
        var tape = LocExtension.Get("Explorer.Tape");
        var system = CurrentSystem(document, formats, selectedMachine);
        if (!string.IsNullOrWhiteSpace(system)
            && !system.Equals(document.Document.MediaKind.ToString(), StringComparison.OrdinalIgnoreCase))
            return $"{tape} {system}";
        var format = FormatForIdentity(formats, document.Document.FormatId);
        return format is null ? tape : $"{tape} {format.DisplayName}";
    }
}
