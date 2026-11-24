using RetroMediaLab.VideoPresentation.Enums;

namespace RetroMediaLab.VideoPresentation.Contracts;

public sealed record EmulationDotMatrixVideoConfiguration(
    EmulationDotMatrixPalette Palette = EmulationDotMatrixPalette.Green,
    EmulationDotMatrixShape Shape = EmulationDotMatrixShape.Round,
    EmulationLedMatrixDefinition Definition = EmulationLedMatrixDefinition.Native,
    EmulationDotMatrixDisplayTechnology Technology = EmulationDotMatrixDisplayTechnology.Reflective);
