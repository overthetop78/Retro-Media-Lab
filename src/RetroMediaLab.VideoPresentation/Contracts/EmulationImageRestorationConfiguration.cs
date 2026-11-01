using RetroMediaLab.VideoPresentation.Constants;
using RetroMediaLab.VideoPresentation.Enums;

namespace RetroMediaLab.VideoPresentation.Contracts;

public sealed record EmulationImageRestorationConfiguration(
    int Dedithering = EmulationVideoProcessingDefaults.Intensity,
    int Denoising = EmulationVideoProcessingDefaults.Intensity,
    int Debanding = EmulationVideoProcessingDefaults.Intensity,
    int DetailRecovery = EmulationVideoProcessingDefaults.Intensity,
    EmulationDeinterlacingMode Deinterlacing = EmulationDeinterlacingMode.Off);
