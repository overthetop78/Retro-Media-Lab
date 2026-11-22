using System.Globalization;
using RetroMediaLab.VideoPresentation.Constants;
using RetroMediaLab.VideoPresentation.Contracts;

namespace RetroMediaLab.VideoPresentation.Functions;

public static class EmulationImageAdjustmentFunctions
{
    public const int OriginalGamma = 0;
    public const int Bt1886Gamma = 1886;

    public static IReadOnlyList<int> GammaCurves { get; } = [OriginalGamma, 180, 190, 220, 240, Bt1886Gamma];

    public static EmulationImageAdjustments Normalize(EmulationImageAdjustments? adjustments)
    {
        adjustments ??= new EmulationImageAdjustments();
        return new EmulationImageAdjustments(Clamp(adjustments.Brightness), Clamp(adjustments.Contrast), NormalizeGamma(adjustments.Gamma), Clamp(adjustments.Saturation), Clamp(adjustments.Sharpness));
    }

    public static float BrightnessAmount(int setting)
    {
        var progress = Progress(setting);
        if (progress < 0f)
        {
            var remainingLight = 1f + progress;
            return (remainingLight * remainingLight - 1f) * 0.9998f;
        }

        return progress * progress * progress * 0.935f;
    }

    public static float ContrastGain(int setting)
    {
        var progress = Progress(setting);
        return MathF.Pow(2f, progress * (progress < 0f ? 6f : 2.125f));
    }
    public static int NormalizeGamma(int setting) => GammaCurves.Contains(setting) ? setting : OriginalGamma;

    // A negative parameter selects the BT.1886 transfer function in the renderers.
    public static float GammaParameter(int setting) => NormalizeGamma(setting) switch
    {
        OriginalGamma => 0f,
        Bt1886Gamma => -1f,
        _ => setting / 100f
    };

    public static float SaturationGain(int setting)
    {
        var progress = Progress(setting);
        return 1f + (progress > 0f ? 2f * progress : progress);
    }

    public static float SharpnessAmount(int setting)
    {
        var progress = Progress(setting);
        return progress > 0f ? 2f * progress : progress;
    }

    public static string FormatSigned(int setting) => Clamp(setting).ToString("+0;-0;0", CultureInfo.CurrentCulture);
    public static string FormatGamma(int setting, string originalLabel) => NormalizeGamma(setting) switch
    {
        OriginalGamma => originalLabel,
        Bt1886Gamma => "BT.1886",
        _ => (setting / 100d).ToString("F1", CultureInfo.CurrentCulture)
    };
    public static string FormatSaturation(int setting) => FormatSigned(setting);

    private static float Progress(int setting) => (float)Clamp(setting) / EmulationVideoProcessingLimits.AdjustmentMaximum;

    private static int Clamp(int value) => Math.Clamp(value, EmulationVideoProcessingLimits.AdjustmentMinimum, EmulationVideoProcessingLimits.AdjustmentMaximum);
}
