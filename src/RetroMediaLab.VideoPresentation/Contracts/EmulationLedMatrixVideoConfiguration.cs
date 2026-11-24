using RetroMediaLab.VideoPresentation.Enums;

namespace RetroMediaLab.VideoPresentation.Contracts;

public sealed record EmulationLedMatrixVideoConfiguration(
    EmulationLedMatrixColor Color = EmulationLedMatrixColor.Rgb,
    EmulationLedMatrixDefinition Definition = EmulationLedMatrixDefinition.Native,
    EmulationLedMatrixShape Shape = EmulationLedMatrixShape.Round);
