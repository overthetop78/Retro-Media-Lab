using RetroMediaLab.VideoPresentation.Enums;

namespace RetroMediaLab.VideoPresentation.Contracts;

public sealed record EmulationLcdVideoConfiguration : EmulationPanelSignalConfiguration
{
    public EmulationLcdPixelStructure PixelStructure { get; init; }
    public EmulationLcdPixelOrientation Orientation { get; init; }
    public EmulationLcdPanelType PanelType { get; init; } = EmulationLcdPanelType.Ips;
    public int BacklightIntensity { get; init; } = 65;
    public EmulationLedBacklightType BacklightType { get; init; }

    public bool HasPixelOrientation() => PixelStructure is EmulationLcdPixelStructure.Rgb or EmulationLcdPixelStructure.Bgr;
}
