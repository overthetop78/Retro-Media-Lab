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
        byte[] Render(int brightness) =>
            pipeline.Process(new() { Adjustments = new(Brightness: brightness, Contrast: -20) },
                frame, new(2, 1), new(2, 1)).Pixels.ToArray();

        Assert.InRange(Render(0)[0], 126, 128);
        Assert.InRange(Render(0)[4], 130, 132);
        Assert.InRange(Render(-20)[4], 0, 2);
        Assert.InRange(Render(20)[0], 248, 251);
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
        Assert.Equal(new EmulationImageAdjustments(-20,20,0,-2,20),result);
        Assert.Equal(-100,original.Brightness);
        Assert.Equal(new EmulationImageAdjustments(),EmulationImageAdjustmentFunctions.Normalize(null));
    }
    public static void Gamma(int gamma, float expected) => Assert.Equal(expected, EmulationImageAdjustmentFunctions.GammaParameter(gamma));

    public static void AdjustmentEndpoints()
    {
        float[] lowContrast = [0f, .125f, .75f, 1f];
        var highContrast = lowContrast.ToArray();
        RetroMediaLab.App.Rendering.Emulation.Processing.VideoContrastParameterFunctions.Apply(lowContrast, -20);
        RetroMediaLab.App.Rendering.Emulation.Processing.VideoContrastParameterFunctions.Apply(highContrast, 20);
        Assert.All(lowContrast, value => Assert.InRange(value, .21f, .23f));
        Assert.Equal([0f, 0f, 1f, 1f], highContrast);

        float[] saturated = [.6f, .4f, .2f];
        RetroMediaLab.App.Rendering.Emulation.Processing.VideoSaturationParameterFunctions.Apply(saturated, -20);
        Assert.All(saturated, component => Assert.Equal(saturated[0], component));
        Assert.Equal(.42808f, saturated[0], 5);
        Assert.Equal(1f, EmulationImageAdjustmentFunctions.SaturationGain(0));
        Assert.Equal(3f, EmulationImageAdjustmentFunctions.SaturationGain(20));
        Assert.Equal("-20", EmulationImageAdjustmentFunctions.FormatSaturation(-20));
        Assert.Equal("+20", EmulationImageAdjustmentFunctions.FormatSaturation(20));
        float[] boosted = [.9f, .4f, .2f, .4f, .4f, .4f];
        RetroMediaLab.App.Rendering.Emulation.Processing.VideoSaturationParameterFunctions.Apply(boosted, 20);
        Assert.Equal(1f, boosted[0]);
        Assert.Equal(0f, boosted[2]);
        Assert.Equal([.4f, .4f, .4f], boosted[3..]);

        float[] edge = [.25f, .25f, .25f, .75f, .75f, .75f];
        var softened = edge.ToArray();
        var sharpened = edge.ToArray();
        RetroMediaLab.App.Rendering.Emulation.Processing.VideoSharpnessParameterFunctions.Apply(softened, 2, 1, -20);
        RetroMediaLab.App.Rendering.Emulation.Processing.VideoSharpnessParameterFunctions.Apply(sharpened, 2, 1, 20);
        Assert.InRange(softened[0], .4f, .45f);
        Assert.Equal(0f, sharpened[0]);
        Assert.InRange(softened[3], .55f, .6f);
        Assert.Equal(1f, sharpened[3]);
    }

    public static void GammaAndSharpnessPixels()
    {
        using var pipeline = new RetroMediaLab.App.Rendering.Emulation.Processing.SoftwareEmulationVideoProcessingPipeline();
        var frame = new RetroMediaLab.Emulation.Contracts.VideoFrame(
            new byte[] { 0, 0, 0, 255, 128, 128, 128, 255, 255, 255, 255, 255 }, 3, 1, 12,
            RetroMediaLab.Emulation.Enums.EmulationPixelFormat.Xrgb8888, 3, 1, TimeSpan.Zero);
        byte[] Render(EmulationImageAdjustments adjustments, int width) =>
            pipeline.Process(new() { Adjustments = adjustments }, frame, new(3, 1), new(width, 1)).Pixels.ToArray();

        var original = Render(new(), 3);
        var light = Render(new(Gamma: 180), 3);
        var reference = Render(new(Gamma: 220), 3);
        var dark = Render(new(Gamma: 240), 3);
        var bt1886 = Render(new(Gamma: EmulationImageAdjustmentFunctions.Bt1886Gamma), 3);
        Assert.Equal(frame.Pixels.ToArray(), original);
        Assert.True(light[4] > reference[4]);
        Assert.True(reference[4] > dark[4]);
        Assert.True(bt1886[0] > dark[0]);
        Assert.True(bt1886[4] > dark[4]);
        Assert.Equal(0, light[0]);
        Assert.Equal(0, dark[0]);
        Assert.Equal(255, light[8]);
        Assert.Equal(255, dark[8]);
        Assert.True(light[4] > 128);
        Assert.True(dark[4] < 128);
        float[] red = [1f, 0f, 0f];
        RetroMediaLab.App.Rendering.Emulation.Processing.VideoGammaParameterFunctions.Apply(red, 240);
        Assert.InRange(red[0], .85f, .90f);
        Assert.Equal([0f, 0f], red[1..]);

        var scaled = Render(new(), 12);
        var softened = Render(new(Sharpness: -20), 12);
        var sharpened = Render(new(Sharpness: 20), 12);
        Assert.Equal(scaled[0], softened[0]);
        Assert.Equal(scaled[44], softened[44]);
        Assert.True(softened[12] > scaled[12]);
        Assert.True(softened[32] < scaled[32]);
        Assert.Equal(scaled[20], sharpened[20]);
        Assert.True(sharpened[16] > scaled[16]);
        Assert.True(sharpened[28] < scaled[28]);
    }
}
