using RetroMediaLab.VideoPresentation.Enums;

namespace RetroMediaLab.VideoPresentation.Contracts;

public sealed record EmulationVideoProcessingConfiguration
{
    public EmulationVideoDisplayTechnology DisplayTechnology { get; init; }
        = EmulationVideoDisplayTechnology.Normal;
    public EmulationVideoAspectRatio AspectRatio { get; init; }
    public EmulationVideoSampling Sampling { get; init; } = EmulationVideoSampling.Nearest;
    public EmulationImageAdjustments Adjustments { get; init; } = new();
    public IReadOnlyDictionary<EmulationVideoScreenType, EmulationImageAdjustments>? ScreenAdjustments { get; init; }
    public EmulationImageRestorationConfiguration Restoration { get; init; } = new();
    public EmulationSignalSimulationConfiguration SignalSimulation { get; init; } = new();
    public EmulationCrtVideoConfiguration Crt { get; init; } = new();
    public EmulationLcdVideoConfiguration Lcd { get; init; } = new();
    public EmulationLcdVideoConfiguration Led { get; init; } = new() { BacklightIntensity = 80 };
    public EmulationOledVideoConfiguration Oled { get; init; } = new();
    public EmulationFixedPixelVideoConfiguration FixedPixel { get; init; } = new();
    public EmulationFixedPixelVideoConfiguration? LedBacklitLcdProfile { get; init; }
    public EmulationFixedPixelVideoConfiguration? OledProfile { get; init; }
    public EmulationPlasmaVideoConfiguration Plasma { get; init; } = new();
    public EmulationVectorVideoConfiguration Vector { get; init; } = new();
    public EmulationVfdVideoConfiguration Vfd { get; init; } = new();
    public EmulationLedMatrixVideoConfiguration LedMatrix { get; init; } = new();
    public EmulationDotMatrixVideoConfiguration DotMatrix { get; init; } = new();
    public EmulationSegmentDisplayVideoConfiguration SegmentDisplay { get; init; } = new();
    public EmulationEPaperVideoConfiguration EPaper { get; init; } = new();
    public EmulationProjectionVideoConfiguration Projection { get; init; } = new();
}
