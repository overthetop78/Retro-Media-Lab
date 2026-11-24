using RetroMediaLab.VideoPresentation.Enums;

namespace RetroMediaLab.VideoPresentation.Contracts;

public sealed record EmulationPlasmaVideoConfiguration : EmulationPanelSignalConfiguration
{
    public EmulationPlasmaCellStructure CellStructure { get; init; }
}
