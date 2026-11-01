using RetroMediaLab.VideoPresentation.Constants;
using RetroMediaLab.VideoPresentation.Enums;

namespace RetroMediaLab.VideoPresentation.Contracts;

public sealed record EmulationFixedPixelVideoConfiguration(
    EmulationFixedPixelTechnology Technology = EmulationFixedPixelTechnology.Lcd,
    EmulationSubpixelLayout Subpixels = EmulationSubpixelLayout.Rgb,
    int GridIntensity = EmulationVideoProcessingDefaults.Intensity,
    int PixelGap = EmulationVideoProcessingDefaults.Intensity,
    int ResponseTimeMilliseconds = EmulationVideoProcessingDefaults.DurationMilliseconds,
    int PersistenceIntensity = EmulationVideoProcessingDefaults.Intensity,
    int? BacklightIntensity = null,
    int? BlackDepth = null,
    EmulationMonochromePalette MonochromePalette = EmulationMonochromePalette.Green,
    int BacklightBleedIntensity = 25);
