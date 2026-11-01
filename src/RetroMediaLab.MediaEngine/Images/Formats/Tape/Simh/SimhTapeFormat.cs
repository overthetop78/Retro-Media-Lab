using System.Collections.Frozen;
using RetroMediaLab.MediaEngine.Constants;

namespace RetroMediaLab.MediaEngine.Images.Formats.Tape.Simh;

/// <summary>Declares structurally validated SIMH tape records and metadata markers.</summary>
internal static class SimhTapeFormat
{
    public static string FormatId => TapeImageFormatIds.SimhTap;
    public static IReadOnlySet<string> Extensions { get; } =
        new[] { DiskImageFileExtensions.Tap }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
}
