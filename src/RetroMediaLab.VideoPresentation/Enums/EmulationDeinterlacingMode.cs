namespace RetroMediaLab.VideoPresentation.Enums;

public enum EmulationDeinterlacingMode
{
    Off,
    // Retained for video profiles saved before field parity was automatic.
    BobEvenLines,
    BobOddLines,
    Blend,
    Bob,
    Weave,
    Adaptive
}
