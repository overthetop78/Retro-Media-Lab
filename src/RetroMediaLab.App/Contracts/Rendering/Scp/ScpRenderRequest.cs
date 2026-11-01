using RetroMediaLab.App.Enums.Rendering.Scp;
using RetroMediaLab.MediaEngine.Enums;
using SkiaSharp;

using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Scp;

namespace RetroMediaLab.App.Contracts.Rendering.Scp;

public sealed record ScpRenderRequest(
    ScpImage? Image,
    int Head,
    ScpTrack? SelectedTrack,
    int? SelectedRevolutionIndex,
    int Width,
    int Height,
    SKPoint Center,
    float Zoom,
    string EmptySideText,
    string SideText,
    DiskMediaCategory MediaCategory = DiskMediaCategory.Unknown);
