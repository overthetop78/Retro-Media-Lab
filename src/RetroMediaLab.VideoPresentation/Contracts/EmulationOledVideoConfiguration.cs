using RetroMediaLab.VideoPresentation.Enums;

namespace RetroMediaLab.VideoPresentation.Contracts;

public sealed record EmulationOledVideoConfiguration : EmulationPanelSignalConfiguration
{
    public EmulationOledPanelType PanelType { get; init; }
    public EmulationOledPixelStructure PixelStructure { get; init; } = EmulationOledPixelStructure.RgbVertical;
}
