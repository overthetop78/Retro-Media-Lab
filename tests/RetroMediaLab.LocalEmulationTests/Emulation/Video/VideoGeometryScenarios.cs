using RetroMediaLab.VideoPresentation.Contracts;
using RetroMediaLab.VideoPresentation.Functions;
using RetroMediaLab.App.Functions.Rendering.Emulation;
namespace RetroMediaLab.Tests.Emulation.Video;
internal static class VideoGeometryScenarios
{
    public static void ProcessingLimits()
    {
        var input=new EmulationVideoProcessingConfiguration
        {
            DisplayTechnology=(RetroMediaLab.VideoPresentation.Enums.EmulationVideoDisplayTechnology)999,
            Sampling=(RetroMediaLab.VideoPresentation.Enums.EmulationVideoSampling)999,
            Crt=new(HorizontalCurvature:-500,VerticalCurvature:500,BeamWidth:-10,MaskIntensity:200),
            FixedPixel=new(ResponseTimeMilliseconds:5000,GridIntensity:-1,PersistenceIntensity:101,MonochromePalette:RetroMediaLab.VideoPresentation.Enums.EmulationMonochromePalette.White,BacklightIntensity:101,BlackDepth:-1),
            Restoration=new(Dedithering:-1,Denoising:101),Temporal=new(GeneralPersistence:101,MotionBlur:-1)
        };
        var output=EmulationVideoProcessingConfigurationFunctions.Normalize(input);
        Assert.Equal(RetroMediaLab.VideoPresentation.Enums.EmulationVideoDisplayTechnology.Normal,output.DisplayTechnology);
        Assert.Equal(RetroMediaLab.VideoPresentation.Enums.EmulationVideoSampling.Nearest,output.Sampling);
        Assert.Equal(-100,output.Crt.HorizontalCurvature); Assert.Equal(100,output.Crt.VerticalCurvature); Assert.Equal(0,output.Crt.BeamWidth); Assert.Equal(100,output.Crt.MaskIntensity);
        Assert.Equal(1000,output.FixedPixel.ResponseTimeMilliseconds); Assert.Equal(0,output.FixedPixel.GridIntensity); Assert.Equal(100,output.FixedPixel.PersistenceIntensity);
        Assert.Equal(RetroMediaLab.VideoPresentation.Enums.EmulationMonochromePalette.White,output.FixedPixel.MonochromePalette);
        Assert.Equal(100,output.FixedPixel.BacklightIntensity); Assert.Equal(0,output.FixedPixel.BlackDepth); Assert.Equal(0,output.Restoration.Dedithering); Assert.Equal(100,output.Restoration.Denoising);
        Assert.Equal(100,output.Temporal.GeneralPersistence); Assert.Equal(0,output.Temporal.MotionBlur);
        Assert.Equal(5000,input.FixedPixel.ResponseTimeMilliseconds); Assert.Equal(output,EmulationVideoProcessingConfigurationFunctions.Normalize(output));
    }
    public static void ProcessingOrder()
    {
        using var pipeline=new RetroMediaLab.App.Rendering.Emulation.Processing.SoftwareEmulationVideoProcessingPipeline();
        var frame=new RetroMediaLab.Emulation.Contracts.VideoFrame(new byte[]{0,0,0,255,255,255,255,255},2,1,8,RetroMediaLab.Emulation.Enums.EmulationPixelFormat.Xrgb8888,2,1,TimeSpan.Zero);
        var processed=pipeline.Process(new(){Adjustments=new(Brightness:5,Contrast:10)},frame,new(2,1),new(2,1));
        // Black becomes .25 linear, then .46 after contrast, which rounds to 181 in sRGB.
        Assert.Equal(new byte[]{181,181,181,255,255,255,255,255},processed.Pixels.ToArray());
    }
    public static void Fit(double width, double height, double aspect, double expectedWidth, double expectedHeight)
    {
        var result = EmulationVideoLayoutFunctions.Fit(width, height, aspect);
        Assert.Equal(expectedWidth, result.Width, 8); Assert.Equal(expectedHeight, result.Height, 8);
        Assert.True(result.Width <= width && result.Height <= height);
    }
    public static void InvalidFit(double width, double height, double aspect) => Assert.True(EmulationVideoLayoutFunctions.Fit(width, height, aspect).IsEmpty);
    public static void FrameAspect()
    {
        var pixels = new byte[640 * 240 * 4];
        var reported = new RetroMediaLab.Emulation.Contracts.VideoFrame(pixels, 640, 240, 640 * 4, RetroMediaLab.Emulation.Enums.EmulationPixelFormat.Xrgb8888, 4f / 3f, 1, TimeSpan.Zero);
        Assert.Equal(4d / 3d, RetroMediaLab.App.Presenters.Emulation.Machine.MachineVideoPresenter.FrameAspectRatio(reported), 6);
        Assert.Equal(640d / 240d, RetroMediaLab.App.Presenters.Emulation.Machine.MachineVideoPresenter.FrameAspectRatio(reported with { AspectRatio = 0 }), 6);
    }
    public static void Adjustments()
    {
        var original=new EmulationImageAdjustments(-100,100,3,-2,20);
        var result=EmulationImageAdjustmentFunctions.Normalize(original);
        Assert.Equal(new EmulationImageAdjustments(-10,10,3,-2,10),result);
        Assert.Equal(-100,original.Brightness);
        Assert.Equal(new EmulationImageAdjustments(),EmulationImageAdjustmentFunctions.Normalize(null));
    }
    public static void Gamma(int gamma,double expected)=>Assert.Equal(expected,EmulationImageAdjustmentFunctions.GammaExponent(gamma),10);
}
