using RetroMediaLab.App.Constants.Rendering.Emulation;
using RetroMediaLab.App.Factories.Rendering.Emulation;
using RetroMediaLab.App.Functions.Rendering.Emulation;
using RetroMediaLab.App.Interfaces.Rendering.Emulation;
using RetroMediaLab.App.Rendering.Emulation.Processing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.App.Rendering.Emulation.Surfaces;

internal sealed class OpenGlVideoSurface : HwndHost, IEmulationVideoSurface
{
    private const int ProgramCacheCapacity = 8;

    private readonly object _contextGate = new();
    private readonly Dictionary<(EmulationVideoSampling, EmulationVideoDisplayTechnology), OpenGlVideoProcessingProgram> _programs = new();
    private readonly Queue<(EmulationVideoSampling, EmulationVideoDisplayTechnology)> _programOrder = new();
    private IntPtr _hwnd;
    private IntPtr _dc;
    private IntPtr _context;
    private WriteableBitmap? _snapshot;
    private VideoFrame? _snapshotSourceFrame;
    private EmulationVideoProcessingSize _snapshotOutputSize;
    private EmulationVideoProcessingConfiguration _snapshotConfiguration = EmulationVideoProcessingConfigurationFunctions.Normalize(null);
    private readonly IEmulationVideoProcessingPipeline _videoProcessingPipeline;
    private bool _rasterVector;
    private readonly SoftwareEmulationVideoProcessingPipeline _vectorPipeline = new();
    private readonly SoftwareEmulationVideoProcessingPipeline _snapshotPipeline = new();
    private bool _disposed;
    private OpenGlVideoProcessingProgram? _program;
    private EmulationVideoSampling _programSampling;
    private EmulationVideoDisplayTechnology _programDisplayTechnology;
    private uint _texture;
    private uint _historyTexture;
    private uint _retainedTexture;
    private int _retainedWidth;
    private int _retainedHeight;
    private ActiveTextureDelegate? _activeTexture;
    private bool _hasHistory;
    private readonly EPaperRefreshClock _ePaperClock = new();
    private byte[]? _deditheringWorkspace;
    private float? _gpuPanelDecoder;
    private readonly FilterDisplayConnection _displayConnection = new();
    private int _historyWidth;
    private int _historyHeight;
    private TimeSpan _historyTimestamp;
    private long _historySequence;
    private EmulationVideoProcessingConfiguration _videoProcessing = EmulationVideoProcessingConfigurationFunctions.Normalize(null);
    public FrameworkElement View => this;
    public BitmapSource? Snapshot => EnsureSnapshot();
    public EmulationVideoRenderer Renderer => EmulationVideoRenderer.OpenGL;
    public IntPtr InputHandle => _hwnd;
    public EmulationVideoProcessingConfiguration VideoProcessing => _videoProcessing;
    public Task<BitmapSource?> CaptureSnapshotAsync() => EmulationVideoSnapshotFunctions.CreateAsync(_snapshotSourceFrame, _snapshotConfiguration, _snapshotOutputSize);

    internal OpenGlVideoSurface()
    {
        Focusable = true;
        _videoProcessingPipeline = EmulationVideoProcessingPipelineFactory.Create(EmulationVideoRenderer.OpenGL);
    }

    public void SetVideoProcessing(EmulationVideoProcessingConfiguration configuration)
    {
        var displayTechnologyChanged = _videoProcessing.DisplayTechnology != configuration.DisplayTechnology;
        _videoProcessing = EmulationVideoProcessingConfigurationFunctions.ForRendering(configuration);
        if (displayTechnologyChanged) ResetHistory();
        _snapshot = null;
    }

    protected override HandleRef BuildWindowCore(HandleRef hwndParent)
    {
        lock (_contextGate) return BuildVideoWindow(hwndParent);
    }

    private HandleRef BuildVideoWindow(HandleRef hwndParent)
    {
        var initialized = false;
        try
        {
            _hwnd = NativeVideoWindowFunctions.Create(hwndParent.Handle, (int)ActualWidth, (int)ActualHeight);
            if (_hwnd == IntPtr.Zero) throw new InvalidOperationException("Unable to create the OpenGL video window.");
            _dc = GetDC(_hwnd);
            var descriptor = new PixelFormatDescriptor
            {
                Size = (ushort)Marshal.SizeOf<PixelFormatDescriptor>(), Version = 1,
                Flags = OpenGlVideoConstants.PixelFormatDrawToWindow |
                    OpenGlVideoConstants.PixelFormatSupportOpenGl |
                    OpenGlVideoConstants.PixelFormatDoubleBuffer,
                PixelType = OpenGlVideoConstants.PixelTypeRgba,
                ColorBits = 32,
                DepthBits = 0,
                LayerType = OpenGlVideoConstants.MainPlane
            };
            var format = ChoosePixelFormat(_dc, ref descriptor);
            if (format == 0 || !SetPixelFormat(_dc, format, ref descriptor))
                throw new InvalidOperationException("OpenGL could not select a compatible pixel format.");
            _context = WglCreateContext(_dc);
            if (_context == IntPtr.Zero) throw new InvalidOperationException("OpenGL could not create a rendering context.");
            WglMakeCurrent(_dc, _context);
            _activeTexture = LoadActiveTexture();
            _activeTexture(OpenGlVideoConstants.Texture0);
            GlGenTextures(1, out _texture);
            GlBindTexture(OpenGlVideoConstants.Texture2D, _texture);
            GlTexParameteri(OpenGlVideoConstants.Texture2D, OpenGlVideoConstants.TextureWrapS, OpenGlVideoConstants.Clamp);
            GlTexParameteri(OpenGlVideoConstants.Texture2D, OpenGlVideoConstants.TextureWrapT, OpenGlVideoConstants.Clamp);
            _activeTexture(OpenGlVideoConstants.Texture1);
            GlGenTextures(1, out _historyTexture);
            GlBindTexture(OpenGlVideoConstants.Texture2D, _historyTexture);
            GlTexParameteri(OpenGlVideoConstants.Texture2D, OpenGlVideoConstants.TextureWrapS, OpenGlVideoConstants.Clamp);
            GlTexParameteri(OpenGlVideoConstants.Texture2D, OpenGlVideoConstants.TextureWrapT, OpenGlVideoConstants.Clamp);
            _activeTexture(OpenGlVideoConstants.Texture0);
            WglMakeCurrent(IntPtr.Zero, IntPtr.Zero);
            initialized = true;
            return new HandleRef(this, _hwnd);
        
        }
        finally
        {
            if (!initialized && _hwnd != IntPtr.Zero) DestroyWindowCore(new HandleRef(this, _hwnd));
        }
    }
    protected override void DestroyWindowCore(HandleRef hwnd)
    {
        lock (_contextGate) DestroyVideoWindow(hwnd);
    }

    private void DestroyVideoWindow(HandleRef hwnd)
    {
        try
        {
            if (_context != IntPtr.Zero)
            {
                WglMakeCurrent(_dc, _context);
                try
                {
                    EmulationVideoCleanupFunctions.RunAll(
                        () => EmulationVideoCleanupFunctions.DisposeAll(_programs.Values),
                        () => { if (_texture != 0) GlDeleteTextures(1, ref _texture); },
                        () => { if (_historyTexture != 0) GlDeleteTextures(1, ref _historyTexture); },
                        ReleaseRetainedTexture,
                        ResetHistory);
                }
                finally
                {
                    try { WglMakeCurrent(IntPtr.Zero, IntPtr.Zero); }
                    finally { WglDeleteContext(_context); }
                }
            }
        }
        finally
        {
            _program = null;
            _programs.Clear();
            _programOrder.Clear();
            _texture = 0;
            _historyTexture = 0;
            _retainedTexture = 0;
            _activeTexture = null;
            _context = IntPtr.Zero;
            try { if (_dc != IntPtr.Zero) ReleaseDC(hwnd.Handle, _dc); }
            finally
            {
                _dc = IntPtr.Zero;
                try { NativeVideoWindowFunctions.Destroy(hwnd.Handle); }
                finally { _hwnd = IntPtr.Zero; }
            }
        }
    }

    public void Present(VideoFrame frame)
    {
        lock (_contextGate) PresentCore(frame);
    }

    public bool PresentBlack()
    {
        lock (_contextGate)
        {
            if (!HasRetainedOutput() || !WglMakeCurrent(_dc, _context)) return false;
            try
            {
                GlClearColor(0, 0, 0, 1);
                GlClear(OpenGlVideoConstants.ColorBufferBit);
                return SwapBuffers(_dc);
            }
            finally { WglMakeCurrent(IntPtr.Zero, IntPtr.Zero); }
        }
    }

    public bool RestoreLastFrame()
    {
        lock (_contextGate)
        {
            if (!HasRetainedOutput() || !WglMakeCurrent(_dc, _context)) return false;
            try
            {
                _activeTexture!(OpenGlVideoConstants.Texture0);
                GlBindTexture(OpenGlVideoConstants.Texture2D, _retainedTexture);
                GlViewport(0, 0, _retainedWidth, _retainedHeight);
                GlEnable(OpenGlVideoConstants.Texture2D);
                GlBegin(OpenGlVideoConstants.Quads);
                GlTexCoord2f(0, 0); GlVertex2f(-1, -1);
                GlTexCoord2f(0, 1); GlVertex2f(-1, 1);
                GlTexCoord2f(1, 1); GlVertex2f(1, 1);
                GlTexCoord2f(1, 0); GlVertex2f(1, -1);
                GlEnd();
                return SwapBuffers(_dc);
            }
            finally { WglMakeCurrent(IntPtr.Zero, IntPtr.Zero); }
        }
    }

    private bool HasRetainedOutput()
    {
        if (_context == IntPtr.Zero || _retainedTexture == 0) return false;
        var size = NativeVideoWindowFunctions.GetClientSize(_hwnd);
        return size.Width == _retainedWidth && size.Height == _retainedHeight;
    }

    private void RetainOutput(int width, int height)
    {
        var allocate = _retainedTexture == 0 || width != _retainedWidth || height != _retainedHeight;
        if (_retainedTexture == 0) GlGenTextures(1, out _retainedTexture);
        GlBindTexture(OpenGlVideoConstants.Texture2D, _retainedTexture);
        if (allocate)
        {
            GlTexParameteri(OpenGlVideoConstants.Texture2D, OpenGlVideoConstants.TextureMinFilter, OpenGlVideoConstants.Nearest);
            GlTexParameteri(OpenGlVideoConstants.Texture2D, OpenGlVideoConstants.TextureMagFilter, OpenGlVideoConstants.Nearest);
            GlCopyTexImage2D(OpenGlVideoConstants.Texture2D, 0, OpenGlVideoConstants.Rgba, 0, 0, width, height, 0);
            _retainedWidth = width;
            _retainedHeight = height;
        }
        else GlCopyTexSubImage2D(OpenGlVideoConstants.Texture2D, 0, 0, 0, 0, 0, width, height);
    }

    private void ReleaseRetainedTexture()
    {
        if (_retainedTexture == 0) return;
        GlDeleteTextures(1, ref _retainedTexture);
        _retainedTexture = 0;
    }

    private void PresentCore(VideoFrame frame)
    {
        if (_context == IntPtr.Zero || _hwnd == IntPtr.Zero) return;
        var clientSize = NativeVideoWindowFunctions.GetClientSize(_hwnd);
        var outputSize = new EmulationVideoProcessingSize(Math.Max(1, clientSize.Width), Math.Max(1, clientSize.Height));
        var configuration = _videoProcessing;
        var vector = configuration.DisplayTechnology == EmulationVideoDisplayTechnology.Vector;
        var nativeVector = vector && frame.VectorSegments.HasValue;
        var rasterVector = vector && !nativeVector;
        var processed = nativeVector
            ? _vectorPipeline.Process(configuration, frame, new(frame.Width, frame.Height), outputSize)
            : frame;
        var gpuConfiguration = nativeVector
            ? new EmulationVideoProcessingConfiguration { BlackFrameInsertion = configuration.BlackFrameInsertion }
            : rasterVector ? configuration with { Sampling = EmulationVideoSampling.Bilinear, SignalSimulation = new() }
            : configuration;
        if (_rasterVector != rasterVector)
        {
            _hasHistory = false;
            _rasterVector = rasterVector;
        }
        byte[] pixels;
        var pixelOffset = 0;
        if (processed.PixelFormat == EmulationPixelFormat.Xrgb8888
            && processed.Pitch == checked(processed.Width * 4)
            && MemoryMarshal.TryGetArray(processed.Pixels, out var sourceSegment)
            && sourceSegment.Array is not null)
        {
            pixels = sourceSegment.Array;
            pixelOffset = sourceSegment.Offset;
        }
        else pixels = EmulationVideoPixelFunctions.ToBgra32(processed);
        var prepared = EmulationImageRestorationFunctions.ApplyDedithering(
            pixels.AsSpan(pixelOffset, checked(processed.Width * processed.Height * 4)), processed.Width, processed.Height,
            gpuConfiguration.Restoration.Dedithering, ref _deditheringWorkspace);
        if (prepared is not null)
        {
            pixels = prepared;
            pixelOffset = 0;
        }
        var panelSignal = PanelSignalVideoShader.ForPresentation(gpuConfiguration, frame);
        var panelDecoder = panelSignal?.Tuning.Y;
        if (_gpuPanelDecoder != panelDecoder)
        {
            _hasHistory = false;
            _gpuPanelDecoder = panelDecoder;
        }
        var connected = _displayConnection.Apply(
            pixels.AsSpan(pixelOffset, checked(processed.Width * processed.Height * 4)), processed,
            gpuConfiguration, panelSignal.HasValue);
        if (connected is not null)
        {
            pixels = connected;
            pixelOffset = 0;
        }
        if (!WglMakeCurrent(_dc, _context))
            throw new InvalidOperationException("OpenGL could not activate its rendering context.");
        try
        {
            EnsureProgram(gpuConfiguration);
            var paper = gpuConfiguration.DisplayTechnology == EmulationVideoDisplayTechnology.EPaper;
            var refresh = paper ? _ePaperClock.Advance(gpuConfiguration.EPaper, processed.Width, processed.Height, frame.Timestamp) : default;
            if (!paper)
                _ePaperClock.Reset();
            var fixedPixel = gpuConfiguration.DisplayTechnology == EmulationVideoDisplayTechnology.FixedPixel;
            var temporalDisplay = gpuConfiguration.DisplayTechnology != EmulationVideoDisplayTechnology.Normal
                || gpuConfiguration.SignalSimulation.StandardIntensity > 0;
            var hasHistory = !paper && temporalDisplay && _hasHistory && _historyWidth == processed.Width && _historyHeight == processed.Height && (fixedPixel ? frame.Timestamp >= _historyTimestamp : frame.Sequence >= _historySequence);
            var elapsedMilliseconds = hasHistory ? (frame.Timestamp - _historyTimestamp).TotalMilliseconds : 0;
            var filter = gpuConfiguration.Sampling is EmulationVideoSampling.Bilinear
                or EmulationVideoSampling.SharpBilinear
                or EmulationVideoSampling.Hq2x
                or EmulationVideoSampling.Hq3x
                or EmulationVideoSampling.Hq4x
                or EmulationVideoSampling.SuperTwoXSai
                or EmulationVideoSampling.SuperEagle
                ? OpenGlVideoConstants.Linear : OpenGlVideoConstants.Nearest;
            if (hasHistory || refresh.HasPrevious || refresh.CaptureHistory)
            {
                _activeTexture!(OpenGlVideoConstants.Texture1);
                GlBindTexture(OpenGlVideoConstants.Texture2D, _historyTexture);
                if (refresh.CaptureHistory) UploadFrame(pixels, pixelOffset, processed.Width, processed.Height, false);
                GlTexParameteri(OpenGlVideoConstants.Texture2D, OpenGlVideoConstants.TextureMinFilter, filter);
                GlTexParameteri(OpenGlVideoConstants.Texture2D, OpenGlVideoConstants.TextureMagFilter, filter);
            }
            _activeTexture!(OpenGlVideoConstants.Texture0);
            GlBindTexture(OpenGlVideoConstants.Texture2D, _texture);
            GlTexParameteri(OpenGlVideoConstants.Texture2D, OpenGlVideoConstants.TextureMinFilter, filter);
            GlTexParameteri(OpenGlVideoConstants.Texture2D, OpenGlVideoConstants.TextureMagFilter, filter);
            UploadFrame(pixels, pixelOffset, processed.Width, processed.Height, rasterVector);
            GlViewport(0, 0, clientSize.Width, clientSize.Height);
            GlClearColor(0, 0, 0, 1); GlClear(OpenGlVideoConstants.ColorBufferBit);
            _program!.Use(gpuConfiguration, processed.Width, processed.Height,
                Math.Max(1, clientSize.Width), Math.Max(1, clientSize.Height),
                hasHistory, elapsedMilliseconds, frame.Sequence,
                panelSignal ?? CrtSignal.From(gpuConfiguration.Crt, frame, EmulationVideoProcessingConfigurationFunctions.EffectiveAspectRatio(gpuConfiguration)),
                gpuConfiguration.DisplayTechnology == EmulationVideoDisplayTechnology.Plasma ? FilterPlasmaAutomaticBrightnessLimiter.MeasureBgra(pixels.AsSpan(pixelOffset, processed.Width * processed.Height * 4)) : 0f,
                panelSignal.HasValue, frame.SegmentLevels, refresh);
            GlEnable(OpenGlVideoConstants.Texture2D);
            GlBegin(OpenGlVideoConstants.Quads);
            GlTexCoord2f(0, 1); GlVertex2f(-1, -1);
            GlTexCoord2f(0, 0); GlVertex2f(-1, 1);
            GlTexCoord2f(1, 0); GlVertex2f(1, 1);
            GlTexCoord2f(1, 1); GlVertex2f(1, -1);
            GlEnd();
            _program.Stop();
            if (gpuConfiguration.BlackFrameInsertion) RetainOutput(clientSize.Width, clientSize.Height);
            else ReleaseRetainedTexture();
            SwapBuffers(_dc);
            if (temporalDisplay && !paper)
            {
                (_texture, _historyTexture) = (_historyTexture, _texture);
                _hasHistory = true;
                _historyWidth = processed.Width;
                _historyHeight = processed.Height;
                _historyTimestamp = frame.Timestamp;
                _historySequence = frame.Sequence;
            }
            else if (!nativeVector && !paper) ResetHistory();
            _snapshotSourceFrame = processed;
            _snapshotOutputSize = outputSize;
            _snapshotConfiguration = gpuConfiguration;
            _snapshot = null;
        }
        finally { WglMakeCurrent(IntPtr.Zero, IntPtr.Zero); }
    }

    private static void UploadFrame(byte[] pixels, int offset, int width, int height, bool srgb)
    {
        var handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        try
        {
            var format = srgb ? OpenGlVideoConstants.Srgb8Alpha8 : OpenGlVideoConstants.Rgba;
            GlTexImage2D(OpenGlVideoConstants.Texture2D, 0, (int)format, width, height, 0,
                OpenGlVideoConstants.Bgra, OpenGlVideoConstants.UnsignedByte, IntPtr.Add(handle.AddrOfPinnedObject(), offset));
        }
        finally { handle.Free(); }
    }

    public new void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { base.Dispose(); }
        finally
        {
            _snapshot = null;
            _snapshotSourceFrame = null;
            EmulationVideoCleanupFunctions.RunAll(_videoProcessingPipeline.Dispose, _snapshotPipeline.Dispose, _vectorPipeline.Dispose);
        }
    }

    public void ResetHistory()
    {
        _displayConnection.ResetHistory();
        _ePaperClock.Reset();
        _hasHistory = false;
        _historyWidth = 0;
        _historyHeight = 0;
        _historyTimestamp = TimeSpan.Zero;
        _historySequence = 0;
        _snapshotPipeline.ResetTemporalHistory();
        _vectorPipeline.ResetTemporalHistory();
    }

    private void EnsureProgram(EmulationVideoProcessingConfiguration configuration)
    {
        if (_program is not null && _programSampling == configuration.Sampling
            && _programDisplayTechnology == configuration.DisplayTechnology) return;
        var key = (configuration.Sampling, configuration.DisplayTechnology);
        if (!_programs.TryGetValue(key, out var program))
        {
            program = new OpenGlVideoProcessingProgram(configuration.Sampling, configuration.DisplayTechnology);
            _programs.Add(key, program);
            _programOrder.Enqueue(key);
            if (_programs.Count > ProgramCacheCapacity)
            {
                var oldest = _programOrder.Dequeue();
                _programs.Remove(oldest, out var retired);
                retired!.Dispose();
            }
        }

        _program = program;
        _programSampling = configuration.Sampling;
        _programDisplayTechnology = configuration.DisplayTechnology;
        ResetHistory();
    }

    private BitmapSource? EnsureSnapshot()
        => EmulationVideoSnapshotFunctions.Ensure(ref _snapshot, _snapshotSourceFrame, _snapshotPipeline, _snapshotConfiguration, _snapshotOutputSize);

    private static ActiveTextureDelegate LoadActiveTexture()
    {
        var address = WglGetProcAddress("glActiveTexture");
        if (address == IntPtr.Zero || address == new IntPtr(1) || address == new IntPtr(2)
            || address == new IntPtr(3) || address == new IntPtr(-1))
            address = WglGetProcAddress("glActiveTextureARB");
        if (address == IntPtr.Zero || address == new IntPtr(1) || address == new IntPtr(2)
            || address == new IntPtr(3) || address == new IntPtr(-1))
            throw new InvalidOperationException("OpenGL multitexturing is unavailable.");
        return Marshal.GetDelegateForFunctionPointer<ActiveTextureDelegate>(address);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PixelFormatDescriptor
    {
        public ushort Size, Version; public uint Flags; public byte PixelType, ColorBits, RedBits, RedShift,
            GreenBits, GreenShift, BlueBits, BlueShift, AlphaBits, AlphaShift, AccumBits, AccumRedBits,
            AccumGreenBits, AccumBlueBits, AccumAlphaBits, DepthBits, StencilBits, AuxBuffers, LayerType, Reserved;
        public uint LayerMask, VisibleMask, DamageMask;
    }
    [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hwnd, IntPtr dc);
    [DllImport("gdi32.dll")] private static extern int ChoosePixelFormat(IntPtr dc, ref PixelFormatDescriptor descriptor);
    [DllImport("gdi32.dll")] private static extern bool SetPixelFormat(IntPtr dc, int format, ref PixelFormatDescriptor descriptor);
    [DllImport("gdi32.dll")] private static extern bool SwapBuffers(IntPtr dc);
    [DllImport("opengl32.dll", EntryPoint = "wglCreateContext")] private static extern IntPtr WglCreateContext(IntPtr dc);
    [DllImport("opengl32.dll", EntryPoint = "wglMakeCurrent")] private static extern bool WglMakeCurrent(IntPtr dc, IntPtr context);
    [DllImport("opengl32.dll", EntryPoint = "wglDeleteContext")] private static extern bool WglDeleteContext(IntPtr context);
    [DllImport("opengl32.dll", EntryPoint = "wglGetProcAddress", CharSet = CharSet.Ansi)]
    private static extern IntPtr WglGetProcAddress(string name);
    [DllImport("opengl32.dll", EntryPoint = "glViewport")] private static extern void GlViewport(int x, int y, int width, int height);
    [DllImport("opengl32.dll", EntryPoint = "glClearColor")] private static extern void GlClearColor(float red, float green, float blue, float alpha);
    [DllImport("opengl32.dll", EntryPoint = "glClear")] private static extern void GlClear(uint mask);
    [DllImport("opengl32.dll", EntryPoint = "glGenTextures")] private static extern void GlGenTextures(int count, out uint texture);
    [DllImport("opengl32.dll", EntryPoint = "glDeleteTextures")] private static extern void GlDeleteTextures(int count, ref uint texture);
    [DllImport("opengl32.dll", EntryPoint = "glBindTexture")] private static extern void GlBindTexture(uint target, uint texture);
    [DllImport("opengl32.dll", EntryPoint = "glTexParameteri")] private static extern void GlTexParameteri(uint target, uint name, uint value);
    [DllImport("opengl32.dll", EntryPoint = "glTexImage2D")] private static extern void GlTexImage2D(uint target, int level, int internalFormat, int width, int height, int border, uint format, uint type, IntPtr pixels);
    [DllImport("opengl32.dll", EntryPoint = "glCopyTexImage2D")] private static extern void GlCopyTexImage2D(uint target, int level, uint internalFormat, int x, int y, int width, int height, int border);
    [DllImport("opengl32.dll", EntryPoint = "glCopyTexSubImage2D")] private static extern void GlCopyTexSubImage2D(uint target, int level, int xOffset, int yOffset, int x, int y, int width, int height);
    [DllImport("opengl32.dll", EntryPoint = "glEnable")] private static extern void GlEnable(uint capability);
    [DllImport("opengl32.dll", EntryPoint = "glBegin")] private static extern void GlBegin(uint mode);
    [DllImport("opengl32.dll", EntryPoint = "glEnd")] private static extern void GlEnd();
    [DllImport("opengl32.dll", EntryPoint = "glTexCoord2f")] private static extern void GlTexCoord2f(float x, float y);
    [DllImport("opengl32.dll", EntryPoint = "glVertex2f")] private static extern void GlVertex2f(float x, float y);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate void ActiveTextureDelegate(uint texture);
}
