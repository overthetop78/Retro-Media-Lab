namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal static partial class VeldridVideoProcessingShaders
{
    internal static string Fragment(RetroMediaLab.VideoPresentation.Enums.EmulationVideoSampling sampling,
        RetroMediaLab.VideoPresentation.Enums.EmulationVideoDisplayTechnology displayTechnology =
            RetroMediaLab.VideoPresentation.Enums.EmulationVideoDisplayTechnology.Normal)
    {
        var (dependencies, function) = sampling switch
        {
            RetroMediaLab.VideoPresentation.Enums.EmulationVideoSampling.Nearest =>
                (FilterNormal.VeldridShader, "nearestSample"),
            RetroMediaLab.VideoPresentation.Enums.EmulationVideoSampling.Bilinear =>
                (FilterBilinear.VeldridShader, "linearSampleCompact"),
            RetroMediaLab.VideoPresentation.Enums.EmulationVideoSampling.SharpBilinear =>
                (FilterBilinear.VeldridShader + FilterSharpBilinear.VeldridShader,
                    "sharpBilinearSampleCompact"),
            RetroMediaLab.VideoPresentation.Enums.EmulationVideoSampling.Bicubic =>
                (FilterNormal.VeldridShader + FilterBicubic.VeldridShader,
                    "bicubicSampleCompact"),
            RetroMediaLab.VideoPresentation.Enums.EmulationVideoSampling.Xbr =>
                (FilterNormal.VeldridShader + FilterXbr.VeldridShader, "xbrCompactSample"),
            RetroMediaLab.VideoPresentation.Enums.EmulationVideoSampling.Xbrz =>
                (FilterNormal.VeldridShader + FilterXbr.VeldridShader
                    + FilterXbrz.VeldridShader, "xbrzCompactSample"),
            RetroMediaLab.VideoPresentation.Enums.EmulationVideoSampling.Hqx =>
                (FilterNormal.VeldridShader + FilterBilinear.VeldridShader
                    + FilterXbr.VeldridShader + FilterHqx.VeldridShader, "hqxCompactSample"),
            RetroMediaLab.VideoPresentation.Enums.EmulationVideoSampling.Hq2x =>
                (FilterNormal.VeldridShader + FilterBilinear.VeldridShader
                    + FilterXbr.VeldridShader + FilterHqx.VeldridShader
                    + FilterHq2x.VeldridShader, "hq2xSampleCompact"),
            RetroMediaLab.VideoPresentation.Enums.EmulationVideoSampling.Hq3x =>
                (FilterNormal.VeldridShader + FilterBilinear.VeldridShader
                    + FilterXbr.VeldridShader + FilterHqx.VeldridShader
                    + FilterHq3x.VeldridShader, "hq3xSampleCompact"),
            RetroMediaLab.VideoPresentation.Enums.EmulationVideoSampling.Hq4x =>
                (FilterNormal.VeldridShader + FilterBilinear.VeldridShader
                    + FilterXbr.VeldridShader + FilterHqx.VeldridShader
                    + FilterHq4x.VeldridShader, "hq4xSampleCompact"),
            RetroMediaLab.VideoPresentation.Enums.EmulationVideoSampling.TwoXSai =>
                (FilterNormal.VeldridShader + FilterXbr.VeldridShader
                    + FilterTwoXSai.VeldridShader, "twoXSaiSampleCompact"),
            RetroMediaLab.VideoPresentation.Enums.EmulationVideoSampling.SuperTwoXSai =>
                (FilterNormal.VeldridShader + FilterBilinear.VeldridShader
                    + FilterXbr.VeldridShader + FilterTwoXSai.VeldridShader
                    + FilterSuperTwoXSai.VeldridShader, "superTwoXSaiSampleCompact"),
            RetroMediaLab.VideoPresentation.Enums.EmulationVideoSampling.SuperEagle =>
                (FilterNormal.VeldridShader + FilterBilinear.VeldridShader
                    + FilterXbr.VeldridShader + FilterTwoXSai.VeldridShader
                    + FilterSuperEagle.VeldridShader, "superEagleSampleCompact"),
            RetroMediaLab.VideoPresentation.Enums.EmulationVideoSampling.EpxScale2x =>
                (FilterNormal.VeldridShader + FilterXbr.VeldridShader
                    + FilterEpxScale2x.VeldridShader, "epxScale2xSampleCompact"),
            RetroMediaLab.VideoPresentation.Enums.EmulationVideoSampling.Jinc2 =>
                (FilterNormal.VeldridShader + FilterJinc2.VeldridShader,
                    "jinc2SampleCompact"),
            RetroMediaLab.VideoPresentation.Enums.EmulationVideoSampling.Lanczos =>
                (FilterNormal.VeldridShader + FilterLanczos.VeldridShader,
                    "lanczosSampleCompact"),
            RetroMediaLab.VideoPresentation.Enums.EmulationVideoSampling.ScaleFx =>
                (FilterNormal.VeldridShader + FilterXbr.VeldridShader
                    + FilterScaleFx.VeldridShader, "scaleFxCompactSample"),
            RetroMediaLab.VideoPresentation.Enums.EmulationVideoSampling.ScaleNx =>
                (FilterNormal.VeldridShader + FilterXbr.VeldridShader
                    + FilterScaleNx.VeldridShader, "scaleNxCompactSample"),
            RetroMediaLab.VideoPresentation.Enums.EmulationVideoSampling.Sabr =>
                (FilterNormal.VeldridShader + FilterBilinear.VeldridShader
                    + FilterXbr.VeldridShader + FilterSabr.VeldridShader, "sabrCompactSample"),
            _ => throw new ArgumentOutOfRangeException(nameof(sampling), sampling, null)
        };
        return FragmentHeader + $"#define DISPLAY_TECHNOLOGY {(int)displayTechnology}\n" + dependencies
            + $"vec3 sourceColor(vec2 uv){{return {function}(uv);}}\n"
            + VideoBrightnessParameterFunctions.Shader + VideoContrastParameterFunctions.Shader
            + VideoGammaParameterFunctions.Shader + VideoSaturationParameterFunctions.Shader
            + VideoSharpnessParameterFunctions.Shader + FragmentBody;
    }
}
