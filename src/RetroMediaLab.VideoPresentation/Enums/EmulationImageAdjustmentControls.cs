namespace RetroMediaLab.VideoPresentation.Enums;

[Flags]
public enum EmulationImageAdjustmentControls
{
    None = 0,
    Brightness = 1,
    Contrast = 2,
    Gamma = 4,
    Saturation = 8,
    Sharpness = 16,
    All = Brightness | Contrast | Gamma | Saturation | Sharpness
}
