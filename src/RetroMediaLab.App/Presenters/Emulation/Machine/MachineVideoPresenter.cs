using RetroMediaLab.App.Constants.Machine;
using RetroMediaLab.App.Factories.Rendering.Emulation;
using RetroMediaLab.App.Functions.Rendering.Emulation;
using RetroMediaLab.App.Interfaces.Rendering.Emulation;
using RetroMediaLab.App.Theme.Common;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.App.Presenters.Emulation.Machine;

internal sealed class MachineVideoPresenter : IDisposable
{
    private EmulationMachinePresentation _view;
    private readonly Func<EmulationVideoRenderer, IEmulationVideoSurface> _createSurface;
    private FrameworkElement _displayHost;
    private IEmulatedMachine _machine;
    private IEmulationVideoSurface _surface;
    private EmulationVideoProcessingConfiguration _videoProcessing;
    private int _framePending;
    private int _uiFramePending;
    private int _framesInWindow;
    private readonly TimeProvider _measurementClock;
    private long _frameWindowStarted;
    private long _lastUiFrameNotification;
    private double _measuredFramesPerSecond;
    private readonly object _frameRateGate = new();
    private TimeSpan? _emulatedWindowStarted;
    private TimeSpan _previousEmulatedTime;
    private long _previousFrameSequence;
    private EmulationExecutionRate? _executionRate;
    private readonly object _surfaceGate = new();
    private readonly object _fieldGate = new();
    private readonly EmulationVideoFieldComposer _fields = new();
    private readonly object _gpuFrameGate = new();
    private readonly AutoResetEvent _gpuFrameAvailable = new(false);
    private readonly CancellationTokenSource _gpuWorkerCancellation = new();
    private readonly Task _gpuWorker;
    private VideoFrame? _pendingGpuFrame;
    private long? _pendingGpuRefresh;
    private long _pendingGpuGeneration;
    private VideoFrame? _latestCompletedFrame;
    private volatile bool _disposed;
    private long _shaderLoadGeneration;
    private volatile bool _shaderLoading;
    private volatile bool _shaderPrepared;
    private volatile bool _presentationEnabled;
    private long _presentationGeneration;
    private long _historyGeneration = -1;
    private double _layoutImageAspect;
    private Window? _hostWindow;
    private readonly BlackFrameCadence _blackFrameCadence = new();
    private IntPtr _blackFrameWindow;
    private CancellationTokenSource? _blackFrameCancellation;
    private Task? _blackFrameWorker;
    private int _hostRefreshPending;
    private volatile bool _blackFrameCadenceActive;
    private long _blackFrameGeneration;
    private long _actualHostRefresh;
    private long _visibleSubmissionRefresh;
    private VideoFrame? _lastVisibleSource;
    private bool _blackOnScreen;

    internal MachineVideoPresenter(EmulationMachinePresentation view, IEmulatedMachine machine,
        EmulationVideoRenderer renderer,
        EmulationVideoProcessingConfiguration? videoProcessing = null,
        Func<EmulationVideoRenderer, IEmulationVideoSurface>? createSurface = null,
        TimeProvider? measurementClock = null)
    {
        _measurementClock = measurementClock ?? TimeProvider.System;
        _frameWindowStarted = _measurementClock.GetTimestamp();
        _view = view;
        _createSurface = createSurface ?? EmulationVideoSurfaceFactory.Create;
        _displayHost = view.DisplayHost;
        _machine = machine;
        _videoProcessing = EmulationVideoProcessingConfigurationFunctions.Normalize(videoProcessing);
        var initialized = false;
        try
        {
            _surface = CreateSurface(renderer);
            _gpuWorker = Task.Factory.StartNew(ProcessGpuFrames, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            _view.SetVideoView(_surface.View);
            _shaderPrepared = _surface.Renderer == EmulationVideoRenderer.Wpf;
            _displayHost.SizeChanged += DisplayHostSizeChanged;
            _machine.Video.FrameReady += VideoFrameReady;
            _view.VideoHost.IsVisibleChanged += VideoHostVisibilityChanged;
            _view.VideoHost.Loaded += VideoHostLoaded;
            _view.VideoHost.Unloaded += VideoHostUnloaded;
            UpdatePresentationVisibility();
            FitScreen();
            initialized = true;
        }
        finally { if (!initialized) Dispose(); }
    }

    internal FrameworkElement InputView => _surface.View;
    internal IntPtr InputHandle => _surface.InputHandle;
    internal EmulationVideoRenderer Renderer => _surface.Renderer;
    internal EmulationVideoProcessingConfiguration VideoProcessing => _videoProcessing;
    internal System.Windows.Media.Imaging.BitmapSource? Snapshot
    {
        get { lock (_surfaceGate) return _surface.Snapshot; }
    }

    internal Task<System.Windows.Media.Imaging.BitmapSource?> CaptureSnapshotAsync()
    {
        lock (_surfaceGate)
        {
            if (_presentationEnabled) return _surface.CaptureSnapshotAsync();
            var (frame, _) = PresentationFrame();
            return EmulationVideoSnapshotFunctions.CreateAsync(frame, _videoProcessing, new EmulationVideoProcessingSize(
                Math.Max(1, (int)_view.Screen.ActualWidth), Math.Max(1, (int)_view.Screen.ActualHeight)));
        }
    }
    internal double MeasuredFramesPerSecond => _measuredFramesPerSecond;
    internal EmulationExecutionRate? ExecutionRate { get { lock (_frameRateGate) return _executionRate; } }
    internal bool IsShaderLoading => _shaderLoading;
    internal event EventHandler<VideoFrame>? FramePresented;
    internal event EventHandler? SurfaceChanged;
    internal event Action<bool>? ShaderLoadingChanged;

    internal void SetPresentation(EmulationMachinePresentation presentation)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (ReferenceEquals(_view, presentation)) return;
        var previous = _view;
        var visibility = previous.VideoHost.Visibility;
        Unsubscribe(previous);
        previous.DetachVideoView();
        try
        {
            presentation.SetVideoView(_surface.View);
            _view = presentation;
            presentation.VideoHost.Visibility = visibility;
            presentation.SetShaderLoading(_shaderLoading);
            Subscribe(presentation);
            SetDisplayHost(presentation.DisplayHost);
            UpdatePresentationVisibility();
            RequestCurrentFrame();
        }
        catch
        {
            Unsubscribe(presentation);
            presentation.DetachVideoView();
            _view = previous;
            previous.SetVideoView(_surface.View);
            Subscribe(previous);
            SetDisplayHost(previous.DisplayHost);
            UpdatePresentationVisibility();
            throw;
        }

        void Subscribe(EmulationMachinePresentation view)
        {
            view.VideoHost.IsVisibleChanged += VideoHostVisibilityChanged;
            view.VideoHost.Loaded += VideoHostLoaded;
            view.VideoHost.Unloaded += VideoHostUnloaded;
        }
        void Unsubscribe(EmulationMachinePresentation view)
        {
            view.VideoHost.IsVisibleChanged -= VideoHostVisibilityChanged;
            view.VideoHost.Loaded -= VideoHostLoaded;
            view.VideoHost.Unloaded -= VideoHostUnloaded;
        }
    }

    internal void SetMachine(IEmulatedMachine machine)
    {
        if (ReferenceEquals(_machine, machine)) return;
        _machine.Video.FrameReady -= VideoFrameReady;
        lock (_fieldGate) _fields.Reset();
        _machine = machine;
        _machine.Video.FrameReady += VideoFrameReady;
        ResetBlackFrameCadence();
        ResetFrameRate();
        Interlocked.Increment(ref _presentationGeneration);
        RequestCurrentFrame();
    }

    internal void SetRenderer(EmulationVideoRenderer renderer)
    {
        if (_surface.Renderer == renderer) return;
        var replacement = CreateSurface(renderer);
        Interlocked.Increment(ref _presentationGeneration);
        IEmulationVideoSurface previous;
        lock (_surfaceGate)
        {
            previous = _surface;
            _surface = replacement;
            _lastVisibleSource = null;
            _blackOnScreen = false;
            ResetBlackFrameCadence();
        }
        try { _view.SetVideoView(replacement.View); }
        finally { previous.Dispose(); }
        SurfaceChanged?.Invoke(this, EventArgs.Empty);
        _shaderPrepared = replacement.Renderer == EmulationVideoRenderer.Wpf;
        if (_shaderPrepared) SetShaderLoading(false);
        else if (_shaderLoading) Interlocked.Increment(ref _shaderLoadGeneration);
        FitScreenToLatestFrame();
        RequestCurrentFrame();
    }

    internal void SetVideoProcessing(EmulationVideoProcessingConfiguration configuration)
    {
        var normalized = EmulationVideoProcessingConfigurationFunctions.Normalize(configuration);
        if (_videoProcessing == normalized) return;
        var rebuildsShader = _surface.Renderer != EmulationVideoRenderer.Wpf
            && (EmulationVideoProcessingConfigurationFunctions.SamplingForDisplay(_videoProcessing)
                != EmulationVideoProcessingConfigurationFunctions.SamplingForDisplay(normalized)
                || _videoProcessing.DisplayTechnology != normalized.DisplayTechnology);
        _videoProcessing = normalized;
        lock (_surfaceGate)
        {
            _surface.SetVideoProcessing(normalized);
            if (rebuildsShader)
            {
                Interlocked.Increment(ref _shaderLoadGeneration);
                _shaderPrepared = false;
            }
        }
        if (_presentationEnabled) FitScreenToLatestFrame();
        UpdateBlackFrameClock();
        RequestCurrentFrame();
    }

    internal void SetVisible(bool visible) => _view.VideoHost.Visibility = visible ? Visibility.Visible : Visibility.Hidden;

    internal void SetDisplayHost(FrameworkElement displayHost)
    {
        if (ReferenceEquals(_displayHost, displayHost)) return;
        _displayHost.SizeChanged -= DisplayHostSizeChanged;
        _displayHost = displayHost;
        _displayHost.SizeChanged += DisplayHostSizeChanged;
        UpdatePresentationVisibility();
        FitScreen();
    }

    internal void FitScreen()
    {
        if (FitScreenToLatestFrame()) RequestCurrentFrame();
    }

    private bool FitScreenToLatestFrame()
    {
        var frame = _machine.Video.LatestFrame;
        var imageAspect = frame is { Width: > 0, Height: > 0 }
            ? FrameAspectRatio(frame) : MachinePresentationConstants.DefaultAspectRatio;
        return FitScreen(imageAspect);
    }

    private bool FitScreen(double imageAspect, bool updateLayout = false)
    {
        var (width, height) = EmulationVideoProcessingConfigurationFunctions.AspectRatioDimensions(
            EmulationVideoProcessingConfigurationFunctions.EffectiveAspectRatio(_videoProcessing));
        var screenAspect = height > 0 ? (double)width / height : imageAspect;
        var (screen, image) = EmulationVideoLayoutFunctions.FitDisplay(
            _displayHost.ActualWidth, _displayHost.ActualHeight, screenAspect, imageAspect);
        var resized = false;
        if (!image.IsEmpty)
        {
            SetSize(_view.Screen, screen);
            var surface = _surface.View;
            surface.HorizontalAlignment = HorizontalAlignment.Center;
            surface.VerticalAlignment = VerticalAlignment.Center;
            resized = SetSize(surface, image);
            if (resized && updateLayout) _view.Screen.UpdateLayout();
        }
        Volatile.Write(ref _layoutImageAspect, imageAspect);
        return resized;

        static bool SetSize(FrameworkElement element, Size size)
        {
            var widthChanged = double.IsNaN(element.Width) || Math.Abs(element.Width - size.Width) >= 0.5d;
            var heightChanged = double.IsNaN(element.Height) || Math.Abs(element.Height - size.Height) >= 0.5d;
            if (widthChanged) element.Width = size.Width;
            if (heightChanged) element.Height = size.Height;
            return widthChanged || heightChanged;
        }
    }

    internal void ResetFrameRate()
    {
        lock (_frameRateGate)
        {
            _framesInWindow = MachinePresentationConstants.InactiveFramePending;
            _frameWindowStarted = _measurementClock.GetTimestamp();
            _measuredFramesPerSecond = MachinePresentationConstants.EmptyMeasurement;
            _emulatedWindowStarted = null;
            _previousEmulatedTime = TimeSpan.Zero;
            _previousFrameSequence = 0;
            _executionRate = null;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _presentationEnabled = false;
        try
        {
            EmulationVideoCleanupFunctions.RunAll(
                UpdateBlackFrameClock,
                () => _machine.Video.FrameReady -= VideoFrameReady,
                () => _displayHost.SizeChanged -= DisplayHostSizeChanged,
                () => _view.VideoHost.IsVisibleChanged -= VideoHostVisibilityChanged,
                () => _view.VideoHost.Loaded -= VideoHostLoaded,
                () => _view.VideoHost.Unloaded -= VideoHostUnloaded,
                () => { if (_hostWindow is not null) _hostWindow.StateChanged -= HostWindowStateChanged; });
        }
        finally
        {
            _hostWindow = null;
            lock (_fieldGate) _fields.Reset();
            lock (_gpuFrameGate)
            {
                _pendingGpuFrame = null;
                _latestCompletedFrame = null;
            }
            try
            {
                EmulationVideoCleanupFunctions.RunAll(
                    _gpuWorkerCancellation.Cancel,
                    () => _gpuFrameAvailable.Set(),
                    () => { try { _gpuWorker?.GetAwaiter().GetResult(); } catch (OperationCanceledException) { } });
            }
            finally
            {
                EmulationVideoCleanupFunctions.RunAll(
                    () => { lock (_surfaceGate) EmulationVideoCleanupFunctions.RunAll(() => _surface?.SuspendPresentation(), () => _surface?.Dispose()); },
                    _gpuWorkerCancellation.Dispose,
                    _gpuFrameAvailable.Dispose);
            }
        }
    }
    private void DisplayHostSizeChanged(object sender, SizeChangedEventArgs args) => FitScreen();

    private void VideoHostVisibilityChanged(object sender, DependencyPropertyChangedEventArgs args) => UpdatePresentationVisibility();
    private void VideoHostLoaded(object sender, RoutedEventArgs args) => UpdatePresentationVisibility();
    private void VideoHostUnloaded(object sender, RoutedEventArgs args) => SetPresentationEnabled(false);
    private void HostWindowStateChanged(object? sender, EventArgs args) => UpdatePresentationVisibility();

    private void UpdatePresentationVisibility()
    {
        if (_disposed) return;
        var window = Window.GetWindow(_view.VideoHost);
        if (!ReferenceEquals(window, _hostWindow))
        {
            if (_hostWindow is not null) _hostWindow.StateChanged -= HostWindowStateChanged;
            _hostWindow = window;
            ResetBlackFrameCadence();
            if (window is not null) window.StateChanged += HostWindowStateChanged;
        }
        SetPresentationEnabled(_view.VideoHost.IsLoaded && _view.VideoHost.IsVisible && window?.WindowState != WindowState.Minimized);
        UpdateBlackFrameClock();
    }

    private void SetPresentationEnabled(bool enabled)
    {
        lock (_gpuFrameGate)
        {
            if (_presentationEnabled == enabled) return;
            Interlocked.Increment(ref _presentationGeneration);
            _presentationEnabled = enabled;
            if (!enabled) _pendingGpuFrame = null;
        }
        if (!enabled)
        {
            _surface.SuspendPresentation();
        }
        else RequestCurrentFrame();
        UpdateBlackFrameClock();
    }

    private void UpdateBlackFrameClock()
    {
        var window = !_disposed && _presentationEnabled && _videoProcessing.BlackFrameInsertion && _hostWindow is { } host
            ? new WindowInteropHelper(host).Handle : IntPtr.Zero;
        if (window == _blackFrameWindow) return;
        if (_blackFrameCancellation is { } previous)
        {
            try
            {
                previous.Cancel();
                _blackFrameWorker?.GetAwaiter().GetResult();
            }
            finally
            {
                previous.Dispose();
                _blackFrameCancellation = null;
                _blackFrameWorker = null;
                _blackFrameWindow = IntPtr.Zero;
                ResetBlackFrameCadence();
            }
        }

        if (window == IntPtr.Zero) return;
        var cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        _blackFrameWindow = window;
        _blackFrameCancellation = cancellation;
        _blackFrameWorker = Task.Factory.StartNew(() =>
            NativeDisplayRefreshRateFunctions.RunClock(window, token, (refresh, hostHz) =>
            {
                Volatile.Write(ref _actualHostRefresh, refresh);
                if (_blackFrameCadenceActive && _surface.Renderer != EmulationVideoRenderer.Wpf
                    && _machine.State == EmulationMachineState.Running
                    && BlackFrameCadence.IsAvailable(hostHz, _machine.Video.FramesPerSecond)
                    && _machine.Video.LatestFrame is { } gameFrame)
                {
                    QueueFrame(gameFrame, refresh);
                    return;
                }
                if (Interlocked.Exchange(ref _hostRefreshPending, 1) != 0) return;
                _view.Dispatcher.BeginInvoke(() =>
                {
                    Interlocked.Exchange(ref _hostRefreshPending, 0);
                    if (!token.IsCancellationRequested) PresentAtHostRefresh(refresh, hostHz);
                }, DispatcherPriority.Render);
            }), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    private void ResetBlackFrameCadence()
    {
        _blackFrameCadenceActive = false;
        Interlocked.Increment(ref _blackFrameGeneration);
    }

    private void PresentAtHostRefresh(long refresh, double hostHz)
    {
        if (_disposed || !_presentationEnabled) return;
        var emulatedHz = _machine.State == EmulationMachineState.Running ? _machine.Video.FramesPerSecond : 0;
        if (!BlackFrameCadence.IsAvailable(hostHz, emulatedHz))
        {
            if (!_blackFrameCadenceActive) return;
            ResetBlackFrameCadence();
            RequestCurrentFrame();
            return;
        }

        _blackFrameCadenceActive = true;
        if (_machine.Video.LatestFrame is not { } frame) return;
        if (_surface.Renderer == EmulationVideoRenderer.Wpf) PresentOnUi(frame, refresh);
        else QueueFrame(frame, refresh);
    }

    private void RequestCurrentFrame()
    {
        if (_machine.Video.LatestFrame is not { } frame) return;
        var generation = Volatile.Read(ref _presentationGeneration);
        _view.Dispatcher.BeginInvoke(() =>
        {
            if (!_disposed && _presentationEnabled && generation == Volatile.Read(ref _presentationGeneration)
                && ReferenceEquals(frame, _machine.Video.LatestFrame))
                QueueFrame(frame);
        }, DispatcherPriority.Loaded);
    }

    private void VideoFrameReady(object? sender, VideoFrame frame)
    {
        UpdateFrameRate(frame);
        lock (_fieldGate)
        {
            if (_disposed) return;
            _fields.Receive(frame, _machine.Video.FramesPerSecond);
        }
        if (!_presentationEnabled)
        {
            if (!_disposed) NotifyFrameCompleted(frame);
            return;
        }
        if (FrameAspectRatio(frame) == Volatile.Read(ref _layoutImageAspect))
        {
            if (!_blackFrameCadenceActive) QueueFrame(frame);
            return;
        }

        var generation = Volatile.Read(ref _presentationGeneration);
        _view.Dispatcher.BeginInvoke(() =>
        {
            if (_disposed || !_presentationEnabled || generation != Volatile.Read(ref _presentationGeneration)) return;
            var latest = _machine.Video.LatestFrame ?? frame;
            var aspect = FrameAspectRatio(latest);
            if (aspect == Volatile.Read(ref _layoutImageAspect)) return;
            FitScreen(aspect, updateLayout: true);
            if (!_blackFrameCadenceActive) QueueFrame(latest);
        }, DispatcherPriority.Render);
    }

    private (VideoFrame? Output, VideoFrame? Source) PresentationFrame()
    {
        lock (_fieldGate)
        {
            if (_disposed) return (null, null);
            if (_fields.Latest is null && _machine.Video.LatestFrame is { } frame)
                _fields.Receive(frame, _machine.Video.FramesPerSecond);
            return (_fields.Compose(EmulationVideoProcessingConfigurationFunctions.DeinterlacingForDisplay(_videoProcessing)), _fields.Latest);
        }
    }

    private VideoFrame PresentFrame(VideoFrame fallback)
    {
        var (output, source) = PresentationFrame();
        if (output is not null && source is not null && !ReferenceEquals(output, source))
            _surface.PresentTransient(WithVideoSignal(output, source), WithVideoSignal(source, source));
        else
        {
            var displayed = output ?? fallback;
            _surface.Present(WithVideoSignal(displayed, displayed));
        }
        return source ?? fallback;
    }

    private VideoFrame WithVideoSignal(VideoFrame frame, VideoFrame source)
    {
        if (_videoProcessing.DisplayTechnology != EmulationVideoDisplayTechnology.Crt) return frame;

        return frame with
        {
            SignalStandard = source.SignalStandard ?? _machine.Runtime.Region,
            SignalFieldRate = source.SignalFieldRate > 0 ? source.SignalFieldRate : _machine.Video.FramesPerSecond,
            SignalFieldHeight = source.SignalFieldHeight > 0 ? source.SignalFieldHeight : source.Height
        };
    }

    private VideoFrame? PresentForRefresh(VideoFrame frame, long? refresh, long generation)
    {
        if (refresh is long slot && _blackFrameCadenceActive && _machine.State == EmulationMachineState.Running)
        {
            frame = _machine.Video.LatestFrame ?? frame;
            var newImage = !ReferenceEquals(frame, _lastVisibleSource);
            var blank = _blackFrameCadence.ShouldBlank(slot, Volatile.Read(ref _blackFrameGeneration), newImage);
            if (!newImage)
            {
                if (blank && !_blackOnScreen && slot > _visibleSubmissionRefresh
                    && slot == Volatile.Read(ref _actualHostRefresh) && _surface.PresentBlack())
                {
                    _blackOnScreen = true;
                    return null;
                }
                if (!_blackOnScreen || _surface.RestoreLastFrame())
                {
                    _blackOnScreen = false;
                    return null;
                }
            }
        }

        PrepareHistory(generation);
        _lastVisibleSource = PresentFrame(frame);
        _visibleSubmissionRefresh = Volatile.Read(ref _actualHostRefresh);
        _blackOnScreen = false;
        return _lastVisibleSource;
    }

    private void QueueFrame(VideoFrame frame, long? refresh = null)
    {
        if (_disposed || !_presentationEnabled) return;
        if (_surface.Renderer == EmulationVideoRenderer.Wpf)
        {
            if (Interlocked.Exchange(ref _framePending, MachinePresentationConstants.ActiveFramePending)
                != MachinePresentationConstants.InactiveFramePending) return;
            _view.Dispatcher.BeginInvoke(() =>
            {
                try { PresentOnUi(frame, refresh); }
                finally { Interlocked.Exchange(ref _framePending, MachinePresentationConstants.InactiveFramePending); }
            });
            return;
        }
        if (!_shaderPrepared) SetShaderLoading(true);
        lock (_gpuFrameGate)
        {
            if (_disposed || !_presentationEnabled) return;
            if (refresh.HasValue && _pendingGpuFrame is not null && !_pendingGpuRefresh.HasValue) return;
            _pendingGpuFrame = frame;
            _pendingGpuRefresh = refresh;
            _pendingGpuGeneration = Volatile.Read(ref _presentationGeneration);
            _gpuFrameAvailable.Set();
        }
    }

    private void ProcessGpuFrames()
    {
        while (true)
        {
            _gpuFrameAvailable.WaitOne();
            if (_gpuWorkerCancellation.IsCancellationRequested) return;
            while (true)
            {
                VideoFrame? frame;
                long? refresh;
                long presentationGeneration;
                lock (_gpuFrameGate)
                {
                    frame = _pendingGpuFrame;
                    refresh = _pendingGpuRefresh;
                    presentationGeneration = _pendingGpuGeneration;
                    _pendingGpuFrame = null;
                }
                if (frame is null) break;
                Exception? error = null;
                long generation = 0;
                try
                {
                    lock (_surfaceGate)
                    {
                        if (_disposed || !_presentationEnabled
                            || presentationGeneration != Volatile.Read(ref _presentationGeneration)) continue;
                        if (PresentForRefresh(frame, refresh, presentationGeneration) is not { } presented) continue;
                        frame = presented;
                        generation = Volatile.Read(ref _shaderLoadGeneration);
                        _shaderPrepared = true;
                    }
                }
                catch (Exception exception) { error = exception; }
                if (error is not null)
                {
                    _view.Dispatcher.BeginInvoke(() =>
                    {
                        if (presentationGeneration == Volatile.Read(ref _presentationGeneration))
                            FallbackAfterGpuFailure(frame);
                    });
                    lock (_gpuFrameGate) _pendingGpuFrame = null;
                    break;
                }
                if (presentationGeneration != Volatile.Read(ref _presentationGeneration)) continue;
                CompleteShaderLoading(generation);
                NotifyFrameCompleted(frame);
            }
        }
    }

    private void PresentOnUi(VideoFrame frame, long? refresh)
    {
        if (_disposed || !_presentationEnabled) return;
        lock (_surfaceGate)
        {
            if (PresentForRefresh(frame, refresh, Volatile.Read(ref _presentationGeneration)) is not { } presented) return;
            frame = presented;
        }
        CompleteShaderLoading(Volatile.Read(ref _shaderLoadGeneration));
        NotifyFrameCompleted(frame);
    }

    private void PrepareHistory(long generation)
    {
        if (_historyGeneration == generation) return;
        _surface.ResetHistory();
        _historyGeneration = generation;
    }

    private void SetShaderLoading(bool loading)
    {
        if (_shaderLoading == loading) return;
        _shaderLoading = loading;
        if (_view.Dispatcher.CheckAccess()) ShaderLoadingChanged?.Invoke(loading);
        else _view.Dispatcher.BeginInvoke(() =>
        {
            if (!_disposed && _shaderLoading == loading) ShaderLoadingChanged?.Invoke(loading);
        });
    }

    private void CompleteShaderLoading(long generation)
    {
        if (!_shaderLoading || generation != Volatile.Read(ref _shaderLoadGeneration)) return;
        _view.Dispatcher.BeginInvoke(() =>
        {
            if (generation == Volatile.Read(ref _shaderLoadGeneration)) SetShaderLoading(false);
        });
    }

    private void FallbackAfterGpuFailure(VideoFrame frame)
    {
        if (_disposed) return;
        SetRenderer(EmulationVideoRenderer.Wpf);
    }

    private void FrameCompleted(VideoFrame frame)
    {
        if (_disposed) return;
        FramePresented?.Invoke(this, frame);
    }

    internal static double FrameAspectRatio(VideoFrame frame) => frame.AspectRatio > 0 && float.IsFinite(frame.AspectRatio) ? frame.AspectRatio : frame.Width / (double)frame.ImageHeight;
    private void NotifyFrameCompleted(VideoFrame frame)
    {
        Interlocked.Exchange(ref _latestCompletedFrame, frame);
        var now = Stopwatch.GetTimestamp();
        var previous = Interlocked.Read(ref _lastUiFrameNotification);
        if (previous != 0 && Stopwatch.GetElapsedTime(previous, now)
                < TimeSpan.FromMilliseconds(MachinePresentationConstants.UiFrameNotificationMilliseconds))
            return;
        if (_view.Dispatcher.CheckAccess())
        {
            Interlocked.Exchange(ref _lastUiFrameNotification, now);
            FrameCompleted(Interlocked.Exchange(ref _latestCompletedFrame, null) ?? frame);
            return;
        }
        if (Interlocked.CompareExchange(ref _uiFramePending, 1, 0) != 0) return;
        Interlocked.Exchange(ref _lastUiFrameNotification, now);
        _view.Dispatcher.BeginInvoke(() =>
        {
            try
            {
                var latest = Interlocked.Exchange(ref _latestCompletedFrame, null);
                if (latest is not null) FrameCompleted(latest);
            }
            finally { Interlocked.Exchange(ref _uiFramePending, 0); }
        });
    }

    private void UpdateFrameRate(VideoFrame frame)
    {
        lock (_frameRateGate)
        {
            if (_disposed) return;
            var now = _measurementClock.GetTimestamp();
            // A new sequence or emulated-time origin starts a new measurement, never a negative rate.
            if (_emulatedWindowStarted is null || frame.Sequence <= _previousFrameSequence || frame.Timestamp < _previousEmulatedTime)
            {
                _frameWindowStarted = now;
                _emulatedWindowStarted = frame.Timestamp;
                _framesInWindow = MachinePresentationConstants.InactiveFramePending;
                _measuredFramesPerSecond = MachinePresentationConstants.EmptyMeasurement;
                _executionRate = null;
            }
            else _framesInWindow++;
            _previousFrameSequence = frame.Sequence;
            _previousEmulatedTime = frame.Timestamp;
            var elapsed = _measurementClock.GetElapsedTime(_frameWindowStarted, now);
            if (elapsed < TimeSpan.FromSeconds(MachinePresentationConstants.FrameRateWindowSeconds)) return;
            _measuredFramesPerSecond = _framesInWindow / elapsed.TotalSeconds;
            _executionRate = EmulationSdk.MachineExecution.MeasureExecutionRate(frame.Timestamp - _emulatedWindowStarted.Value,
                elapsed, _machine.Runtime.ProcessorClockHz);
            _framesInWindow = MachinePresentationConstants.InactiveFramePending;
            _frameWindowStarted = now;
            _emulatedWindowStarted = frame.Timestamp;
        }
    }

    private IEmulationVideoSurface CreateSurface(EmulationVideoRenderer renderer)
    {
        IEmulationVideoSurface surface;
        try { surface = _createSurface(renderer); }
        catch when (renderer != EmulationVideoRenderer.Wpf)
        {
            surface = _createSurface(EmulationVideoRenderer.Wpf);
        }
        var configured = false;
        try
        {
            surface.SetVideoProcessing(_videoProcessing);
            configured = true;
            return surface;
        }
        finally { if (!configured) surface.Dispose(); }
    }
}
