using System.Buffers;
using RetroMediaLab.App.Functions.Rendering.Emulation;
using RetroMediaLab.App.Interfaces.Rendering.Emulation;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal sealed record CpuVideoFrameProcessingResult(VideoFrame SourceFrame, VideoFrame? ProcessedFrame, byte[]? Bgra32Pixels, long ConfigurationVersion, Exception? Error);

internal sealed class SoftwareVideoFrameProcessingWorker : IDisposable
{
    private readonly object _gate = new();
    private readonly IEmulationVideoProcessingPipeline _pipeline;
    private WorkItem? _pending;
    private Task? _processingTask;
    private bool _running;
    private bool _disposed;
    private bool _resetHistory;

    internal void ResetHistory()
    {
        lock (_gate) _resetHistory = true;
    }

    internal void Suspend()
    {
        lock (_gate)
        {
            _pending?.ReleasePixels();
            _pending = null;
            _resetHistory = true;
        }
    }

    internal SoftwareVideoFrameProcessingWorker(IEmulationVideoProcessingPipeline pipeline) => _pipeline = pipeline;

    internal void Submit(EmulationVideoProcessingConfiguration configuration, VideoFrame frame, EmulationVideoProcessingSize outputSize, long configurationVersion, Action<CpuVideoFrameProcessingResult> completed, VideoFrame? transientSource = null)
    {
        lock (_gate)
        {
            if (_disposed) return;
            _pending?.ReleasePixels();
            _pending = null;
            _pending = new WorkItem(configuration, frame, outputSize, configurationVersion, completed, transientSource);
            if (_running) return;
            _running = true;
            _processingTask = Task.Run(ProcessPending);
        }
    }

    public void Dispose()
    {
        Task? processingTask;
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            _pending?.ReleasePixels();
            _pending = null;
            processingTask = _processingTask;
        }
        try { processingTask?.GetAwaiter().GetResult(); }
        finally { _pipeline.Dispose(); }
    }

    private void ProcessPending()
    {
        while (true)
        {
            WorkItem? work;
            bool resetHistory;
            lock (_gate)
            {
                if (_disposed || _pending is null)
                {
                    _running = false;
                    return;
                }
                work = _pending;
                _pending = null;
                resetHistory = _resetHistory;
                _resetHistory = false;
            }

            CpuVideoFrameProcessingResult result;
            try
            {
                if (resetHistory) _pipeline.ResetTemporalHistory();
                var processed = _pipeline.Process(work.Configuration, work.Frame, new EmulationVideoProcessingSize(work.Frame.Width, work.Frame.Height), work.OutputSize);
                var pixels = EmulationVideoPixelFunctions.ToBgra32(processed);
                if (work.IsTransient)
                    processed = processed with { Pixels = pixels, Pitch = checked(processed.Width * 4), PixelFormat = EmulationPixelFormat.Xrgb8888 };
                result = new(work.Source, processed, pixels, work.ConfigurationVersion, null);
            }
            catch (Exception error)
            {
                result = new(work.Source, null, null, work.ConfigurationVersion, error);
            }
            finally { work.ReleasePixels(); }

            lock (_gate)
            {
                if (_disposed) continue;
                if (_pending is not null) continue;
                _running = false;
            }
            work.Completed(result);
            return;
        }
    }

    private sealed class WorkItem
    {
        private readonly byte[]? _rented;

        internal WorkItem(EmulationVideoProcessingConfiguration configuration, VideoFrame frame,
            EmulationVideoProcessingSize outputSize, long version, Action<CpuVideoFrameProcessingResult> completed,
            VideoFrame? transientSource)
        {
            Configuration = configuration;
            Source = transientSource ?? frame;
            OutputSize = outputSize;
            ConfigurationVersion = version;
            Completed = completed;
            if (transientSource is not null)
            {
                _rented = ArrayPool<byte>.Shared.Rent(frame.Pixels.Length);
                frame.Pixels.Span.CopyTo(_rented);
                frame = frame with { Pixels = _rented.AsMemory(0, frame.Pixels.Length) };
            }
            Frame = frame;
        }

        internal bool IsTransient => _rented is not null;
        internal EmulationVideoProcessingConfiguration Configuration { get; }
        internal VideoFrame Frame { get; }
        internal VideoFrame Source { get; }
        internal EmulationVideoProcessingSize OutputSize { get; }
        internal long ConfigurationVersion { get; }
        internal Action<CpuVideoFrameProcessingResult> Completed { get; }

        internal void ReleasePixels()
        {
            if (_rented is not null) ArrayPool<byte>.Shared.Return(_rented);
        }
    }
}
