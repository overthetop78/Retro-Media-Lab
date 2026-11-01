using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Images.Models.Sequential;

namespace RetroMediaLab.MediaEngine.Functions;

internal static class SequentialDecodeFunctions
{
    internal static bool CanDecodeTape(MediaImageDocument document, string? machineId,
        IReadOnlySet<string> supportedFormats, IReadOnlySet<string> supportedMachines) =>
        document.MediaKind == MediaKind.Tape
        && supportedFormats.Contains(document.FormatId)
        && (machineId is null || supportedMachines.Contains(machineId))
        && document.Representation is SequentialMediaImageRepresentation;
}
