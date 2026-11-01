using System.Windows;
using System.Windows.Media.Imaging;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.App.Interfaces.Rendering.Emulation;

internal interface IEmulationVideoSurface : IDisposable
{
    FrameworkElement View { get; }
    /// <summary>
    /// Gets the final Retro Media Lab processing output before any external frame or bezel.
    /// </summary>
    BitmapSource? Snapshot { get; }
    Task<BitmapSource?> CaptureSnapshotAsync() => Task.FromResult(Snapshot);
    EmulationVideoRenderer Renderer { get; }
    IntPtr InputHandle { get; }
    EmulationVideoProcessingConfiguration VideoProcessing => EmulationVideoProcessingConfigurationFunctions.Normalize(null);
    void SetVideoProcessing(EmulationVideoProcessingConfiguration configuration);
    void Present(VideoFrame frame);
    void ResetHistory() { }
    void SuspendPresentation() { }
}
