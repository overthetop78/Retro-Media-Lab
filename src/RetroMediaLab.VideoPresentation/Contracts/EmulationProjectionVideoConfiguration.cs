using RetroMediaLab.VideoPresentation.Enums;

namespace RetroMediaLab.VideoPresentation.Contracts;

public sealed record EmulationProjectionVideoConfiguration : EmulationPanelSignalConfiguration
{
    public const int MaximumAmbientLight = 20;

    public EmulationProjectionTechnology Technology { get; init; } = EmulationProjectionTechnology.ThreeLcd;
    public EmulationProjectionSurface Surface { get; init; }
    public int AmbientLight { get; init; }
}
