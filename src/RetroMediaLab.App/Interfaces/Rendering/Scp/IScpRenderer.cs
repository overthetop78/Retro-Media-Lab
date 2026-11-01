using RetroMediaLab.App.Contracts.Rendering.Scp;
using SkiaSharp;

using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Scp;

namespace RetroMediaLab.App.Interfaces.Rendering.Scp;

public interface IScpRenderer
{
    string? DecoderId { get; set; }
    void ClearCache();
    void RevealTrack(int cylinder);
    Task PrepareAsync(ScpImage image, int head, IProgress<ScpTrackPreparation>? progress = null, CancellationToken cancellationToken = default);
    void Render(SKCanvas canvas, ScpRenderRequest request);
}
