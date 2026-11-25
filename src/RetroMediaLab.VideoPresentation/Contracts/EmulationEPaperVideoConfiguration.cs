using RetroMediaLab.VideoPresentation.Enums;

namespace RetroMediaLab.VideoPresentation.Contracts;

public sealed record EmulationEPaperVideoConfiguration(
    EmulationEPaperColorMode ColorMode = EmulationEPaperColorMode.Monochrome,
    EmulationLcdDefinition Definition = EmulationLcdDefinition.Native);
