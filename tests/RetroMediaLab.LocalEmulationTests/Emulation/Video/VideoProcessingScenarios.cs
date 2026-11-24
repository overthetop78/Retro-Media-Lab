using RetroMediaLab.VideoPresentation.Functions;
using RetroMediaLab.VideoPresentation.Enums;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.App.Functions.Rendering.Emulation;
namespace RetroMediaLab.Tests.Emulation.Video;
internal static class VideoProcessingScenarios
{
    internal sealed class VideoSource : RetroMediaLab.Emulation.Interfaces.IEmulationVideo
    {
        private readonly RetroMediaLab.Emulation.Interfaces.IEmulationRuntime _runtime;
        internal long? ProcessorClockHz { get; set; }
        internal VideoSource() => _runtime = RetroMediaLab.Tests.Application.TestInfrastructure.ControlledDependencies.Simulate<RetroMediaLab.Emulation.Interfaces.IEmulationRuntime>(
            (method, _) => method.Name switch
            {
                "get_ProcessorClockHz" => ProcessorClockHz,
                "get_Region" => EmulationVideoStandard.Pal,
                _ => throw new InvalidOperationException(method.Name)
            });
        public VideoFrame? LatestFrame { get; private set; }
        public double FramesPerSecond => 50;
        public event EventHandler<VideoFrame>? FrameReady;
        public int Subscribers => FrameReady?.GetInvocationList().Length ?? 0;
        public void Send(VideoFrame frame) { LatestFrame=frame; FrameReady?.Invoke(this,frame); }
        public RetroMediaLab.Emulation.Interfaces.IEmulatedMachine Machine => RetroMediaLab.Tests.Application.TestInfrastructure.ControlledDependencies.Simulate<RetroMediaLab.Emulation.Interfaces.IEmulatedMachine>((method,_) => method.Name switch { "get_Video" => this, "get_Runtime" => _runtime, _ => throw new InvalidOperationException(method.Name) });
    }
    private sealed class Surface(EmulationVideoRenderer renderer) : RetroMediaLab.App.Interfaces.Rendering.Emulation.IEmulationVideoSurface
    {
        public System.Windows.FrameworkElement View { get; } = new System.Windows.Controls.Border();
        public System.Windows.Media.Imaging.BitmapSource? Snapshot => null;
        public EmulationVideoRenderer Renderer => renderer;
        public IntPtr InputHandle => IntPtr.Zero;
        public List<RetroMediaLab.VideoPresentation.Contracts.EmulationVideoProcessingConfiguration> Settings = [];
        public List<VideoFrame> Frames = [];
        public bool Fail, Disposed;
        public void SetVideoProcessing(RetroMediaLab.VideoPresentation.Contracts.EmulationVideoProcessingConfiguration configuration) => Settings.Add(configuration);
        public void Present(VideoFrame frame) { if(Fail) throw new IOException("synthetic renderer failure"); Frames.Add(frame); }
        public void Dispose() => Disposed=true;
    }
    public static async Task FieldPresentation()
    {
        var source = new VideoSource();
        var replacement = new VideoSource();
        var view = new RetroMediaLab.App.Views.Controls.Emulation.Machine.MachineView();
        var surface = new Surface(EmulationVideoRenderer.Wpf);
        using var presenter = new RetroMediaLab.App.Presenters.Emulation.Machine.MachineVideoPresenter(
            view, source.Machine, EmulationVideoRenderer.Wpf,
            videoProcessing: new() { Restoration = new(Deinterlacing: EmulationDeinterlacingMode.Weave) },
            createSurface: _ => surface);
        var host = new System.Windows.Window
        {
            Content = view, Width = 320, Height = 240, ShowActivated = false, ShowInTaskbar = false, Opacity = 0
        };
        try
        {
            host.Show();
            await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            var even = new VideoFrame(new byte[] { 11, 12, 13, 255 }, 1, 1, 4,
                EmulationPixelFormat.Xrgb8888, 1, 1, TimeSpan.FromMilliseconds(20))
                { ScanMode = EmulationVideoScanMode.InterlacedEvenField };
            var odd = even with
            {
                Pixels = new byte[] { 21, 22, 23, 255 }, Sequence = 2,
                Timestamp = TimeSpan.FromMilliseconds(40), ScanMode = EmulationVideoScanMode.InterlacedOddField
            };
            source.Send(even);
            source.Send(odd);
            await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.ContextIdle);
            var composed = Assert.Single(surface.Frames);
            Assert.Equal(2, composed.Height);
            Assert.Equal(even.Pixels.ToArray().Concat(odd.Pixels.ToArray()), composed.Pixels.ToArray());
            presenter.SetMachine(replacement.Machine);
            replacement.Send(odd with { Sequence = 1, Timestamp = TimeSpan.FromMilliseconds(20) });
            await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.ContextIdle);
            Assert.Equal(odd.Pixels.ToArray().Concat(odd.Pixels.ToArray()), surface.Frames[^1].Pixels.ToArray());

            foreach (var screen in new[] { EmulationVideoScreenType.LedBacklitLcd, EmulationVideoScreenType.Oled, EmulationVideoScreenType.Plasma })
            {
                presenter.SetVideoProcessing(EmulationVideoProcessingConfigurationFunctions.WithScreenType(
                    new() { Restoration = new(Deinterlacing: EmulationDeinterlacingMode.Weave) }, screen));
                // Switching the screen type redraws the last frame even without a new frame from the core.
                await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.ContextIdle);
                var displayed = surface.Frames[^1];
                Assert.Equal(EmulationVideoStandard.Pal, displayed.SignalStandard);
                Assert.Equal(50d, displayed.SignalFieldRate);
                Assert.Equal(odd.Height, displayed.SignalFieldHeight);
            }

            replacement.Send(odd with
            {
                Sequence = 2, Timestamp = TimeSpan.FromMilliseconds(40),
                SignalStandard = EmulationVideoStandard.Pal, SignalFieldRate = 60
            });
            await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.ContextIdle);
            Assert.Equal(EmulationVideoStandard.Pal, surface.Frames[^1].SignalStandard);
            Assert.Equal(60d, surface.Frames[^1].SignalFieldRate);
        }
        finally
        {
            try { presenter.Dispose(); }
            finally { host.Close(); host.Content = null; }
            await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }
    }

    public static void NativeFieldDeinterlacing()
    {
        var composer = new EmulationVideoFieldComposer();
        var progressive = Field(1, true, (_, _) => 44) with { ScanMode = EmulationVideoScanMode.Progressive };
        composer.Receive(progressive, 50);
        foreach (var mode in Enum.GetValues<EmulationDeinterlacingMode>())
            Assert.Same(progressive, composer.Compose(mode));

        var diagonal = Field(1, true, (x, y) => x == 2 + y * 2 ? (byte)240 : (byte)0);
        composer.Receive(diagonal, 50);
        Assert.Same(diagonal, composer.Compose(EmulationDeinterlacingMode.Off));
        var bob = composer.Compose(EmulationDeinterlacingMode.Bob)!;
        Assert.Equal((byte)240, Pixel(bob, 3, 1));
        Assert.Equal(diagonal.Pixels.Span[..diagonal.Pitch].ToArray(), bob.Pixels.Span[..bob.Pitch].ToArray());
        Assert.Equal(EmulationVideoScanMode.Progressive, bob.ScanMode);

        var dark = Field(1, true, (_, _) => 0);
        var bright = Field(2, false, (_, _) => 240);
        composer.Reset();
        composer.Receive(dark, 50);
        composer.Receive(bright, 50);
        var woven = composer.Compose(EmulationDeinterlacingMode.Weave)!;
        for (var y = 0; y < woven.Height; y++)
        {
            var original = (y & 1) == 0 ? dark : bright;
            Assert.Equal(original.Pixels.Span.Slice(y / 2 * original.Pitch, original.Pitch).ToArray(),
                woven.Pixels.Span.Slice(y * woven.Pitch, woven.Pitch).ToArray());
        }
        var blended = composer.Compose(EmulationDeinterlacingMode.Blend)!;
        Assert.Equal((byte)120, Pixel(blended, 3, 1));
        Assert.Equal((byte)120, Pixel(blended, 3, 2));
        composer.Reset();
        composer.Receive(Field(1, true, (_, row) => row == 0 ? (byte)0 : (byte)240));
        composer.Receive(Field(2, false, (_, row) => row == 0 ? (byte)0 : (byte)240));
        blended = composer.Compose(EmulationDeinterlacingMode.Blend)!;
        Assert.Equal((byte)0, Pixel(blended, 3, 1));
        Assert.Equal((byte)240, Pixel(blended, 3, 2));

        composer.Reset();
        composer.Receive(dark);
        composer.Receive(bright);
        composer.Receive(dark with { Sequence = 3, Timestamp = TimeSpan.FromMilliseconds(60) });
        var adaptive = composer.Compose(EmulationDeinterlacingMode.Adaptive)!;
        Assert.Equal((byte)240, Pixel(adaptive, 2, 1));
        composer.Receive(bright with { Sequence = 4, Timestamp = TimeSpan.FromMilliseconds(80) });
        composer.Receive(Field(5, true, (x, _) => x < 4 ? (byte)240 : (byte)0));
        adaptive = composer.Compose(EmulationDeinterlacingMode.Adaptive)!;
        Assert.Equal((byte)240, Pixel(adaptive, 2, 1));
        Assert.Equal((byte)240, Pixel(adaptive, 6, 1)); // The unchanged tile keeps its actual odd line.
        composer.Receive(Field(6, false, (x, _) => x < 4 ? (byte)0 : (byte)240));
        adaptive = composer.Compose(EmulationDeinterlacingMode.Adaptive)!;
        Assert.Equal((byte)0, Pixel(adaptive, 2, 0));

        composer.Reset();
        composer.Receive(Field(1, true, (x, _) => x is 1 or 2 ? (byte)240 : (byte)0));
        composer.Receive(Field(2, false, (x, _) => x is 2 or 3 ? (byte)240 : (byte)0));
        composer.Receive(Field(3, true, (x, _) => x is 3 or 4 ? (byte)240 : (byte)0));
        var movingBar = composer.Compose(EmulationDeinterlacingMode.Adaptive)!;
        Assert.Equal((byte)0, Pixel(movingBar, 2, 1));
        Assert.Equal((byte)240, Pixel(movingBar, 3, 1));

        composer.Reset();
        composer.Receive(dark);
        composer.Receive(Field(2, false, (x, row) => row == 1 && x is 1 or 5 ? (byte)235 : (byte)0));
        composer.Receive(dark with { Sequence = 3, Timestamp = TimeSpan.FromMilliseconds(60) });
        var thinText = composer.Compose(EmulationDeinterlacingMode.Adaptive)!;
        Assert.Equal((byte)235, Pixel(thinText, 1, 3));
        Assert.Equal((byte)0, Pixel(thinText, 1, 2));
        Assert.Equal((byte)0, Pixel(thinText, 2, 3));
        composer.Receive(Field(4, false, (_, _) => 215));

        var afterGap = Field(5, true, (_, _) => 62) with { Timestamp = TimeSpan.FromSeconds(1) };
        composer.Receive(afterGap, 50);
        Assert.Equal((byte)62, Pixel(composer.Compose(EmulationDeinterlacingMode.Weave)!, 3, 1));
        var resized = Field(6, false, (_, _) => 77, width: 10) with { Timestamp = TimeSpan.FromMilliseconds(1020) };
        composer.Receive(resized, 50);
        Assert.Equal((byte)77, Pixel(composer.Compose(EmulationDeinterlacingMode.Weave)!, 3, 0));
        composer.Receive(progressive);
        Assert.Same(progressive, composer.Compose(EmulationDeinterlacingMode.Adaptive));

        var migrated = EmulationVideoProcessingConfigurationFunctions.Normalize(new()
        {
            Restoration = new(Deinterlacing: EmulationDeinterlacingMode.BobOddLines)
        });
        Assert.Equal(EmulationDeinterlacingMode.Bob, migrated.Restoration.Deinterlacing);
        var crt = EmulationVideoProcessingConfigurationFunctions.WithScreenType(migrated, EmulationVideoScreenType.Crt);
        Assert.Equal(EmulationDeinterlacingMode.Off, EmulationVideoProcessingConfigurationFunctions.DeinterlacingForDisplay(crt));

        static byte Pixel(VideoFrame frame, int x, int y) => frame.Pixels.Span[y * frame.Pitch + x * 4];

        static VideoFrame Field(int sequence, bool even, Func<int, int, byte> color, int width = 8)
        {
            const int height = 3;
            var pitch = width * 4 + 4;
            var data = new byte[pitch * height];
            for (var y = 0; y < height; y++)
            {
                data[y * pitch + width * 4] = (byte)(even ? 17 : 231);
                for (var x = 0; x < width; x++)
                {
                    var offset = y * pitch + x * 4;
                    data.AsSpan(offset, 3).Fill(color(x, y));
                    data[offset + 3] = 255;
                }
            }
            return new VideoFrame(data, width, height, pitch, EmulationPixelFormat.Xrgb8888, 4f / 3f,
                sequence, TimeSpan.FromMilliseconds(sequence * 20))
            {
                ScanMode = even ? EmulationVideoScanMode.InterlacedEvenField : EmulationVideoScanMode.InterlacedOddField
            };
        }
    }

    public static async Task Presentation(int failure)
    {
        var source=new VideoSource(); var replacement=new VideoSource(); var created=new List<Surface>();
        var view=new RetroMediaLab.App.Views.Controls.Emulation.Machine.MachineView();
        var processing=new RetroMediaLab.VideoPresentation.Contracts.EmulationVideoProcessingConfiguration { Adjustments=new(Brightness:2) };
        RetroMediaLab.App.Interfaces.Rendering.Emulation.IEmulationVideoSurface Create(EmulationVideoRenderer renderer)
        {
            if(failure==1 && renderer!=EmulationVideoRenderer.Wpf) throw new IOException("synthetic factory failure");
            var surface=new Surface(renderer) { Fail=failure==2 && renderer!=EmulationVideoRenderer.Wpf }; created.Add(surface); return surface;
        }
        using var presenter=new RetroMediaLab.App.Presenters.Emulation.Machine.MachineVideoPresenter(view,source.Machine,EmulationVideoRenderer.Direct3D11,processing,Create);
        var host = new System.Windows.Window { Content = view, Width = 320, Height = 240,
            ShowActivated = false, ShowInTaskbar = false, Opacity = 0 };
        try
        {
        host.Show();
        await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        Assert.Equal(1,source.Subscribers); Assert.Equal(processing,Assert.Single(created[0].Settings));
        var completed=new TaskCompletionSource<VideoFrame>(TaskCreationOptions.RunContinuationsAsynchronously);
        presenter.FramePresented+=(_,frame)=>completed.TrySetResult(frame);
        var frame=new VideoFrame(new byte[]{1,2,3,255},1,1,4,EmulationPixelFormat.Xrgb8888,1,7,TimeSpan.Zero);
        source.Send(frame); Assert.Same(frame,await completed.Task);
        await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.ContextIdle);
        Assert.False(presenter.IsShaderLoading); Assert.Equal(failure==0?EmulationVideoRenderer.Direct3D11:EmulationVideoRenderer.Wpf,presenter.Renderer);
        Assert.Same(frame,Assert.Single(created[^1].Frames));
        if(failure==2) Assert.True(created[0].Disposed);
        var changed=processing with { Adjustments=new(Contrast:3) }; presenter.SetVideoProcessing(changed);
        Assert.Equal(changed,created[^1].Settings[^1]); Assert.Null(await presenter.CaptureSnapshotAsync());
        presenter.SetMachine(replacement.Machine); Assert.Equal(0,source.Subscribers); Assert.Equal(1,replacement.Subscribers);
        source.Send(frame with { Sequence=8 }); Assert.Single(created[^1].Frames);
        presenter.Dispose(); Assert.Equal(0,replacement.Subscribers); Assert.True(created[^1].Disposed);
        replacement.Send(frame); Assert.Single(created[^1].Frames);
        }
        finally { host.Close(); }
    }
    public static void PipelineBrightness(int setting,byte dark,byte light)
    {
        using var pipeline = new RetroMediaLab.App.Rendering.Emulation.Processing.SoftwareEmulationVideoProcessingPipeline();
        byte[] pixels = [0,0,0,255,255,255,255,255,0,165,232,255];
        var frame = new VideoFrame(pixels,3,1,12,EmulationPixelFormat.Xrgb8888,3,17,TimeSpan.FromSeconds(1));
        var configuration = new RetroMediaLab.VideoPresentation.Contracts.EmulationVideoProcessingConfiguration { Adjustments = new(Brightness:setting) };
        var result = pipeline.Process(configuration,frame,new(3,1),new(3,1));
        var output = result.Pixels.Span;
        Assert.Equal(new byte[]{dark,dark,dark,255,light,light,light,255},output[..8].ToArray());
        Assert.All(output[8..11].ToArray(), component => Assert.InRange(component, dark, light));
        if (setting == 0) Assert.Equal(pixels, output.ToArray());
        Assert.Equal(new byte[]{0,0,0,255,255,255,255,255,0,165,232,255},pixels);
        Assert.Equal(frame.Sequence,result.Sequence); Assert.Equal(frame.Timestamp,result.Timestamp); Assert.Equal(frame.AspectRatio,result.AspectRatio);
        Assert.Same(frame,pipeline.Process(new(),frame,new(3,1),new(3,1)));
        Assert.Throws<ArgumentException>(()=>pipeline.Process(new(),frame,new(1,1),new(3,1)));
        Assert.Throws<ArgumentOutOfRangeException>(()=>pipeline.Process(new(),frame,new(3,1),new(0,1)));
    }
    public static void Resize()
    {
        using var pipeline = new RetroMediaLab.App.Rendering.Emulation.Processing.SoftwareEmulationVideoProcessingPipeline();
        byte[] pixels=[0,0,255,255,0,255,0,255,255,0,0,255,255,255,255,255];
        var frame=new VideoFrame(pixels,2,2,8,EmulationPixelFormat.Xrgb8888,1,7,TimeSpan.Zero);
        var result=pipeline.Process(new(),frame,new(2,2),new(4,4));
        Assert.Equal(4,result.Width); Assert.Equal(4,result.Height); Assert.Equal(16,result.Pitch);
        var red=new byte[]{0,0,255,255};var green=new byte[]{0,255,0,255};var blue=new byte[]{255,0,0,255};var white=new byte[]{255,255,255,255};
        var expected=new[]{red,red,green,green,red,red,green,green,blue,blue,white,white,blue,blue,white,white}.SelectMany(x=>x);
        Assert.Equal(expected,result.Pixels.ToArray()); Assert.Equal(7,result.Sequence);
    }
    public static void RendererRouting(EmulationVideoRenderer renderer)
    {
        using var pipeline=RetroMediaLab.App.Factories.Rendering.Emulation.EmulationVideoProcessingPipelineFactory.Create(renderer);
        Assert.Equal(renderer,pipeline.Renderer);
        Assert.IsType<RetroMediaLab.App.Rendering.Emulation.Processing.PassthroughEmulationVideoProcessingPipeline>(pipeline);
        var frame=new VideoFrame(new byte[]{0,0,255,255},1,1,4,EmulationPixelFormat.Xrgb8888,1,1,TimeSpan.Zero);
        Assert.Same(frame,pipeline.Process(new(),frame,new(1,1),new(2,2)));
        Assert.Throws<ArgumentException>(()=>pipeline.Process(new(),frame,new(2,2),new(2,2)));
        Assert.Throws<ArgumentOutOfRangeException>(()=>pipeline.Process(new(),frame,new(1,1),new(0,0)));
    }
    public static void Pixels(EmulationPixelFormat format)
    {
        byte[] source = format switch {
            EmulationPixelFormat.Xrgb8888 => [0,0,255,0, 0,255,0,0, 91,92,93,94, 255,0,0,0, 255,255,255,0, 81,82,83,84],
            EmulationPixelFormat.Rgb565 => [0,248,224,7,91,92, 31,0,255,255,81,82],
            _ => [0,124,224,3,91,92, 31,0,255,127,81,82]
        };
        var before = source.ToArray();
        var frame = new VideoFrame(source, 2, 2, format == EmulationPixelFormat.Xrgb8888 ? 12 : 6, format, 1, 7, TimeSpan.Zero);
        Assert.Equal(new byte[] { 0,0,255,255, 0,255,0,255, 255,0,0,255, 255,255,255,255 }, EmulationVideoPixelFunctions.ToBgra32(frame));
        Assert.Equal(before, source); Assert.Equal(7, frame.Sequence);
        Assert.Throws<ArgumentOutOfRangeException>(() => EmulationVideoPixelFunctions.ToBgra32(frame with { Pixels = source.AsMemory(0, 1) }));
    }
    public static void Palettes()
    {
        Assert.Equal(EmulationMonochromePalette.White,EmulationMonochromePaletteFunctions.FromArgb(0xffffffff));
        Assert.Equal(EmulationMonochromePalette.Amber,EmulationMonochromePaletteFunctions.FromArgb(0xffff7509));
        Assert.Equal(EmulationMonochromePalette.Blue,EmulationMonochromePaletteFunctions.FromArgb(0x006bbdff));
    }
}
