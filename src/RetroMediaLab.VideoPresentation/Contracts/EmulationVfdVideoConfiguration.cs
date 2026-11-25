using RetroMediaLab.VideoPresentation.Enums;

namespace RetroMediaLab.VideoPresentation.Contracts;

public sealed record EmulationVfdVideoConfiguration(
    EmulationVfdColor Color = EmulationVfdColor.BlueGreen,
    EmulationLedMatrixDefinition Definition = EmulationLedMatrixDefinition.Native,
    EmulationDotMatrixShape Shape = EmulationDotMatrixShape.Round);
