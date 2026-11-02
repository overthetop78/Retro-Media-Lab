using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using RetroMediaLab.App.Functions.Machine;
using RetroMediaLab.App.Interfaces.Rendering.Emulation;
using RetroMediaLab.App.Presenters.Emulation.Machine;
using RetroMediaLab.App.Views.Controls.Emulation.Machine;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Tests.Application.TestInfrastructure;
using RetroMediaLab.Tests.Emulation.Video;
using RetroMediaLab.VideoPresentation.Contracts;
using RetroMediaLab.VideoPresentation.Enums;
using System.Collections.Concurrent;
using System.Windows.Threading;

namespace RetroMediaLab.Tests.Interface.EmulationViews;

[Collection("WPF")]
public sealed class MachineExecutionRateViewTests(StaExecutionScenarios sta)
{
    [Fact]
    public Task BlockedGpuAndDispatcherDoNotBlockFrameDeliveryOrFrequencyMeasurement() => sta.RunAsync(async () =>
    {
        var clock = new Clock();
        var source = new VideoProcessingScenarios.VideoSource { ProcessorClockHz = 2_000_000 };
        using var view = new MachineView();
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var surface = new BlockingSurface(entered, release);
        using var presenter = new MachineVideoPresenter(view, source.Machine, EmulationVideoRenderer.Direct3D11,
            createSurface: _ => surface, measurementClock: clock);
        var window = new Window { Content = view, Width = 320, Height = 240, ShowActivated = false, ShowInTaskbar = false, Opacity = 0 };
        Task? producer = null;
        try
        {
            window.Show();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            source.Send(Frame(1, 0));
            Assert.True(await Task.Run(() => entered.Wait(TimeSpan.FromSeconds(5))));
            producer = Task.Run(() =>
            {
                for (var i = 1; i <= 50; i++)
                {
                    clock.Advance(TimeSpan.FromMilliseconds(20));
                    source.Send(Frame(i + 1, i / 50d));
                }
            });
            // Deliberately stop pumping WPF while Present is blocked on the GPU worker.
            // A dependency on either thread makes this bounded wait fail.
            Assert.True(producer.Wait(TimeSpan.FromSeconds(5)));
            Assert.Equal(50, presenter.MeasuredFramesPerSecond);
            Assert.Equal(2, presenter.ExecutionRate!.Value.EffectiveMegahertz);
            Assert.Empty(surface.Completed);
            release.Set();
            for (var attempts = 0; attempts < 100 && !surface.Completed.Contains(51); attempts++) await Task.Delay(10);
            Assert.Contains(51, surface.Completed); // Latest frame replaces the backlog.
        }
        finally
        {
            release.Set();
            try { if (producer is not null) await producer; }
            finally
            {
                presenter.Dispose();
                window.Content = null;
                window.Close();
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            }
        }
    });

    [Fact]
    public Task MeasuresElapsedEmulatedTimeAndRestartsAfterPauseOrTimeReset() => sta.Run(() =>
    {
        var clock = new Clock();
        var source = new VideoProcessingScenarios.VideoSource { ProcessorClockHz = 2_000_000 };
        using var view = new MachineView();
        using var presenter = new MachineVideoPresenter(view, source.Machine, EmulationVideoRenderer.Wpf,
            createSurface: _ => new Surface(), measurementClock: clock);
        source.Send(Frame(1, 0));
        clock.Advance(TimeSpan.FromSeconds(1));
        source.Send(Frame(2, 32)); // One presented frame can contain 32 seconds of emulated work.
        Assert.Equal(64, presenter.ExecutionRate!.Value.EffectiveMegahertz!.Value);
        presenter.ResetFrameRate(); // Pause/resume must not include the inactive wall-clock interval.
        clock.Advance(TimeSpan.FromHours(1));
        source.Send(Frame(3, 32));
        Assert.Null(presenter.ExecutionRate);
        clock.Advance(TimeSpan.FromSeconds(1));
        source.Send(Frame(4, 33));
        Assert.Equal(2, presenter.ExecutionRate!.Value.EffectiveMegahertz!.Value);
        source.Send(Frame(1, 0)); // Restarted core sequence and timestamp.
        Assert.Null(presenter.ExecutionRate);
    });

    [Fact]
    public Task FrequencyStatusUsesTheSharedResourceAndWrapsWithinItsWidth() => sta.Run(() =>
    {
        var text = MachinePresentationFunctions.Status(Frame(1, 0), 50.08, 49.9, new(1, 2));
        Assert.Contains("MHz", text);
        Assert.DoesNotContain("[Emulation.", text);
        var block = new TextBlock { Text = text, MaxWidth = 360, TextWrapping = TextWrapping.Wrap };
        try
        {
            block.Measure(new Size(360, double.PositiveInfinity));
            Assert.InRange(block.DesiredSize.Width, 1, 360);
            Assert.True(block.DesiredSize.Height > 0);
        }
        finally { block.Text = string.Empty; }
    });

    private static VideoFrame Frame(long sequence, double seconds) => new(new byte[4], 1, 1, 4, EmulationPixelFormat.Xrgb8888, 1, sequence, TimeSpan.FromSeconds(seconds));
    private sealed class Clock : TimeProvider
    {
        private long _ticks;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => _ticks;
        internal void Advance(TimeSpan duration) => _ticks += duration.Ticks;
    }
    private sealed class Surface : IEmulationVideoSurface
    {
        public FrameworkElement View { get; } = new Border();
        public BitmapSource? Snapshot => null;
        public EmulationVideoRenderer Renderer => EmulationVideoRenderer.Wpf;
        public IntPtr InputHandle => IntPtr.Zero;
        public void SetVideoProcessing(EmulationVideoProcessingConfiguration configuration) { }
        public void Present(VideoFrame frame) { }
        public void Dispose() { }
    }

    private sealed class BlockingSurface(ManualResetEventSlim entered, ManualResetEventSlim release) : IEmulationVideoSurface
    {
        internal ConcurrentQueue<long> Completed { get; } = new();
        public FrameworkElement View { get; } = new Border();
        public BitmapSource? Snapshot => null;
        public EmulationVideoRenderer Renderer => EmulationVideoRenderer.Direct3D11;
        public IntPtr InputHandle => IntPtr.Zero;
        public void SetVideoProcessing(EmulationVideoProcessingConfiguration configuration) { }
        public void Present(VideoFrame frame)
        {
            entered.Set();
            if (!release.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException();
            Completed.Enqueue(frame.Sequence);
        }
        public void Dispose() { }
    }
}
