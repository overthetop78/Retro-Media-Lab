using RetroMediaLab.VideoPresentation.Enums;

namespace RetroMediaLab.VideoPresentation.Contracts;

public sealed record EmulationVectorVideoConfiguration(
    EmulationCrtColorMode PhosphorColor = EmulationCrtColorMode.Color,
    int BeamWidth = 0,
    int Curvature = 0)
{
    public const int MinimumBeamWidth = -20;
    public const int MaximumBeamWidth = 20;
}
