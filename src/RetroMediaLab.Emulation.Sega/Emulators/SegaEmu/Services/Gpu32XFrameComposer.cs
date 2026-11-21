using System.Diagnostics;
using System.Runtime.InteropServices;
using RetroMediaLab.Emulation.Interfaces;
using RetroMediaLab.Emulation.Interop;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Services;

/// <summary>Experimental OpenGL path for the final 32X-over-Mega-Drive pixel composition.</summary>
/// <remarks>The VDP rasters the Mega Drive image on the CPU. This class borrows the session framebuffer
/// and render context, stages 32X priority/color metadata, composites it in a fragment shader, and reads
/// the result back because the managed video contract consumes CPU pixels.</remarks>
internal sealed class Gpu32XFrameComposer : IDisposable
{
    private const uint Texture2D = 0x0DE1, Texture0 = 0x84C0, Texture1 = 0x84C1;
    private const uint TextureMinFilter = 0x2801, TextureMagFilter = 0x2800, TextureWrapS = 0x2802, TextureWrapT = 0x2803;
    private const int Nearest = 0x2600, ClampToEdge = 0x812F;
    private const int Rgba8 = 0x8058, R32ui = 0x8236;
    private const uint Rgba = 0x1908, Bgra = 0x80E1, RedInteger = 0x8D94, UnsignedByte = 0x1401, UnsignedInt = 0x1405;
    private const uint Framebuffer = 0x8D40, ColorAttachment0 = 0x8CE0;
    private const uint PackAlignment = 0x0D05, PackRowLength = 0x0D02, UnpackAlignment = 0x0CF5, UnpackRowLength = 0x0CF2;
    private const uint VertexShader = 0x8B31, FragmentShader = 0x8B30, CompileStatus = 0x8B81, LinkStatus = 0x8B82;
    private const uint Triangles = 0x0004, Blend = 0x0BE2, DepthTest = 0x0B71, ScissorTest = 0x0C11;

    private const string VertexSource = """
        #version 130
        void main() {
            vec2 p;
            if (gl_VertexID == 0) p = vec2(-1.0, -1.0);
            else if (gl_VertexID == 1) p = vec2(3.0, -1.0);
            else p = vec2(-1.0, 3.0);
            gl_Position = vec4(p, 0.0, 1.0);
        }
        """;

    private const string FragmentSource = """
        #version 130
        uniform sampler2D uMegaDrive;
        uniform usampler2D uLayer32X;
        uniform int uHeight;
        out vec4 outputColor;
        void main() {
            int x = int(gl_FragCoord.x);
            int y = uHeight - 1 - int(gl_FragCoord.y);
            ivec2 p = ivec2(x, y);
            uint layer = texelFetch(uLayer32X, p, 0).r;
            int sourceX = (layer & 0x80100000u) != 0u ? int((layer >> 22) & 511u) : x;
            vec4 megaDrive = texelFetch(uMegaDrive, ivec2(sourceX, y), 0);
            if ((layer & 0x80000000u) == 0u) {
                outputColor = megaDrive;
                return;
            }
            uint color = layer & 0xffffu;
            uint index = (layer >> 16) & 15u;
            bool displayEnabled = (layer & (1u << 20)) != 0u;
            uint priorityMode = (layer >> 21) & 1u;
            bool replace = !displayEnabled || index == 0u || (((color >> 15) & 1u) != priorityMode);
            if (!replace) {
                outputColor = megaDrive;
                return;
            }
            uint r5 = color & 31u;
            uint g5 = (color >> 5) & 31u;
            uint b5 = (color >> 10) & 31u;
            uint r = (r5 << 3) | (r5 >> 2);
            uint g = (g5 << 3) | (g5 >> 2);
            uint b = (b5 << 3) | (b5 >> 2);
            outputColor = vec4(float(r), float(g), float(b), 255.0) / 255.0;
        }
        """;

    private readonly IEmulationHardwareRenderContext _context;
    private readonly byte[] _framebuffer;
    private readonly int _width, _height, _pitch;
    private readonly uint[] _layerPixels;
    private readonly int[] _rowGeneration, _touchedRows;
    private readonly byte[] _rowScratch;
    private readonly GCHandle _framebufferPin, _layerPin;
    private readonly ExternalCoreApi.HardwareGetProcAddress _getProcAddress;
    private readonly ExternalCoreApi.HardwareGetCurrentFramebuffer _getCurrentFramebuffer;
    private readonly GenObjects _genTextures;
    private readonly DeleteObjects _deleteTextures;
    private readonly BindTextureFunction _bindTexture;
    private readonly TexImage2DFunction _texImage2D;
    private readonly TexSubImage2DFunction _texSubImage2D;
    private readonly TexParameterFunction _texParameter;
    private readonly PixelStoreFunction _pixelStore;
    private readonly ActiveTextureFunction _activeTexture;
    private readonly GenerateVertexArraysFunction _genVertexArrays;
    private readonly DeleteVertexArraysFunction _deleteVertexArrays;
    private readonly BindVertexArrayFunction _bindVertexArray;
    private readonly BindFramebufferFunction _bindFramebuffer;
    private readonly ViewportFunction _viewport;
    private readonly DisableFunction _disable;
    private readonly DrawArraysFunction _drawArrays;
    private readonly ReadBufferFunction _readBuffer;
    private readonly ReadPixelsFunction _readPixels;
    private readonly FinishFunction _finish;
    private readonly CreateShaderFunction _createShader;
    private readonly ShaderSourceFunction _shaderSource;
    private readonly CompileShaderFunction _compileShader;
    private readonly GetShaderFunction _getShader;
    private readonly GetShaderInfoLogFunction _getShaderInfoLog;
    private readonly DeleteShaderFunction _deleteShader;
    private readonly CreateProgramFunction _createProgram;
    private readonly AttachShaderFunction _attachShader;
    private readonly LinkProgramFunction _linkProgram;
    private readonly GetProgramFunction _getProgram;
    private readonly GetProgramInfoLogFunction _getProgramInfoLog;
    private readonly DeleteProgramFunction _deleteProgram;
    private readonly UseProgramFunction _useProgram;
    private readonly GetUniformLocationFunction _getUniformLocation;
    private readonly Uniform1iFunction _uniform1i;
    private readonly uint _framebufferId;
    private uint _megaDriveTexture, _layerTexture, _vertexArray, _program;
    private int _generation = 1, _touchedRowCount, _minTouchedRow, _maxTouchedRow, _maxOutputWidth, _maxSourceWidth, _uniformMegaDrive, _uniformLayer, _uniformHeight;
    private bool _disposed;

    public long TotalComposedFrames { get; private set; }
    public long TotalComposedLines { get; private set; }
    public long UnsupportedLines { get; private set; }
    public double TotalGpuMilliseconds { get; private set; }
    public long LastFramePixelTransfers { get; private set; }
    public long TotalPixelTransfers { get; private set; }
    public bool LastFrameUsedGpu { get; private set; }
    public int LastFrameComposedLines { get; private set; }

    public Gpu32XFrameComposer(IEmulationHardwareRenderContext context, byte[] framebuffer, int width, int height, int pitch)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(framebuffer);
        if (width <= 0 || height <= 0 || pitch != checked(width * 4) || framebuffer.Length < checked(pitch * height))
            throw new ArgumentOutOfRangeException(nameof(framebuffer), "La voie GPU 32X requiert un framebuffer BGRA32 contigu.");
        _context = context;
        _framebuffer = framebuffer;
        _width = width;
        _height = height;
        _pitch = pitch;
        _layerPixels = new uint[checked(width * height)];
        _rowGeneration = new int[height];
        _touchedRows = new int[height];
        _rowScratch = new byte[pitch];

        if (!context.MakeCurrentForCore()) throw new InvalidOperationException("Le contexte OpenGL n’a pas pu être activé.");
        int openGl = RetroMediaLab.Emulation.Constants.ExternalCoreApiConstants.OpenGlContextType;
        int openGlCore = RetroMediaLab.Emulation.Constants.ExternalCoreApiConstants.OpenGlCoreContextType;
        if ((context.ContextType != openGl && context.ContextType != openGlCore) || context.VersionMajor < 3)
            throw new PlatformNotSupportedException("La composition GPU 32X requiert un contexte OpenGL Core 3.0.");
        _getProcAddress = Marshal.GetDelegateForFunctionPointer<ExternalCoreApi.HardwareGetProcAddress>(context.ProcAddressCallback);
        _getCurrentFramebuffer = Marshal.GetDelegateForFunctionPointer<ExternalCoreApi.HardwareGetCurrentFramebuffer>(context.CurrentFramebufferCallback);

        _genTextures = Load<GenObjects>("glGenTextures");
        _deleteTextures = Load<DeleteObjects>("glDeleteTextures");
        _bindTexture = Load<BindTextureFunction>("glBindTexture");
        _texImage2D = Load<TexImage2DFunction>("glTexImage2D");
        _texSubImage2D = Load<TexSubImage2DFunction>("glTexSubImage2D");
        _texParameter = Load<TexParameterFunction>("glTexParameteri");
        _pixelStore = Load<PixelStoreFunction>("glPixelStorei");
        _activeTexture = Load<ActiveTextureFunction>("glActiveTexture");
        _genVertexArrays = Load<GenerateVertexArraysFunction>("glGenVertexArrays");
        _deleteVertexArrays = Load<DeleteVertexArraysFunction>("glDeleteVertexArrays");
        _bindVertexArray = Load<BindVertexArrayFunction>("glBindVertexArray");
        _bindFramebuffer = Load<BindFramebufferFunction>("glBindFramebuffer");
        _viewport = Load<ViewportFunction>("glViewport");
        _disable = Load<DisableFunction>("glDisable");
        _drawArrays = Load<DrawArraysFunction>("glDrawArrays");
        _readBuffer = Load<ReadBufferFunction>("glReadBuffer");
        _readPixels = Load<ReadPixelsFunction>("glReadPixels");
        _finish = Load<FinishFunction>("glFinish");
        _createShader = Load<CreateShaderFunction>("glCreateShader");
        _shaderSource = Load<ShaderSourceFunction>("glShaderSource");
        _compileShader = Load<CompileShaderFunction>("glCompileShader");
        _getShader = Load<GetShaderFunction>("glGetShaderiv");
        _getShaderInfoLog = Load<GetShaderInfoLogFunction>("glGetShaderInfoLog");
        _deleteShader = Load<DeleteShaderFunction>("glDeleteShader");
        _createProgram = Load<CreateProgramFunction>("glCreateProgram");
        _attachShader = Load<AttachShaderFunction>("glAttachShader");
        _linkProgram = Load<LinkProgramFunction>("glLinkProgram");
        _getProgram = Load<GetProgramFunction>("glGetProgramiv");
        _getProgramInfoLog = Load<GetProgramInfoLogFunction>("glGetProgramInfoLog");
        _deleteProgram = Load<DeleteProgramFunction>("glDeleteProgram");
        _useProgram = Load<UseProgramFunction>("glUseProgram");
        _getUniformLocation = Load<GetUniformLocationFunction>("glGetUniformLocation");
        _uniform1i = Load<Uniform1iFunction>("glUniform1i");

        bool initialized = false;
        try
        {
            _framebufferPin = GCHandle.Alloc(framebuffer, GCHandleType.Pinned);
            _layerPin = GCHandle.Alloc(_layerPixels, GCHandleType.Pinned);
            context.EnsureFramebuffer(width, height);
            _framebufferId = unchecked((uint)_getCurrentFramebuffer());
            if (_framebufferId == 0) throw new InvalidOperationException("Le framebuffer OpenGL de la session est absent.");
            InitializeOpenGl();
            initialized = true;
        }
        finally
        {
            if (!initialized) Dispose();
        }
    }

    public void BeginFrame()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        for (int i = 0; i < _touchedRowCount; i++) Array.Clear(_layerPixels, _touchedRows[i] * _width, _width);
        _touchedRowCount = 0;
        _minTouchedRow = _height;
        _maxTouchedRow = -1;
        _maxOutputWidth = 0;
        _maxSourceWidth = 0;
        LastFrameUsedGpu = false;
        LastFrameComposedLines = 0;
        LastFramePixelTransfers = 0;
        unchecked
        {
            if (++_generation == 0)
            {
                Array.Clear(_rowGeneration);
                _generation = 1;
            }
        }
    }

    public bool TryCaptureLine(int rowIndex, int depth, int outputWidth, int nativeWidth,
        int viewportWidth, int viewportX, bool displayEnabled, int priorityMode,
        ReadOnlySpan<ushort> colors, ReadOnlySpan<byte> indices)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(Gpu32XFrameComposer));
        if (depth != 32 || (uint)rowIndex >= (uint)_height || outputWidth <= 0 || outputWidth > _width
            || nativeWidth <= 0 || nativeWidth > Math.Min(_width, 512) || viewportWidth <= 0 || colors.Length < 320 || indices.Length < viewportWidth)
        {
            UnsupportedLines++;
            return false;
        }

        int offset = rowIndex * _width;
        if (_rowGeneration[rowIndex] != _generation)
        {
            _rowGeneration[rowIndex] = _generation;
            _touchedRows[_touchedRowCount++] = rowIndex;
            Array.Clear(_layerPixels, offset, _width);
            LastFrameComposedLines++;
        }
        _minTouchedRow = Math.Min(_minTouchedRow, rowIndex);
        _maxTouchedRow = Math.Max(_maxTouchedRow, rowIndex);
        _maxOutputWidth = Math.Max(_maxOutputWidth, outputWidth);
        _maxSourceWidth = Math.Max(_maxSourceWidth, Math.Max(nativeWidth, outputWidth));

        uint rowFlags = 0x80000000u | (displayEnabled ? 1u << 20 : 0) | ((uint)(priorityMode & 1) << 21);
        for (int x = 0; x < outputWidth; x++)
        {
            // Match the backward CPU composition loop, including border pixels.
            uint sourceColumn = (uint)Math.Clamp(x * nativeWidth / outputWidth, 0, nativeWidth - 1) << 22;
            // Bit 20 marks a mapped border pixel when the 32X-valid bit is clear;
            // untouched pixels remain zero and keep their original source column.
            _layerPixels[offset + x] = sourceColumn | (1u << 20);
            int active = x * (viewportWidth + 2 * viewportX) / outputWidth - viewportX;
            if ((uint)active >= (uint)viewportWidth) continue;
            ushort color = colors[active * 320 / viewportWidth];
            uint index = (uint)indices[active] & 15u;
            _layerPixels[offset + x] = sourceColumn | rowFlags | ((uint)color) | (index << 16);
        }
        return true;
    }

    public void FinishFrame()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (LastFrameComposedLines == 0) return;
        if (!_context.MakeCurrentForCore()) throw new InvalidOperationException("Le contexte OpenGL n’a pas pu être réactivé.");
        Stopwatch stopwatch = Stopwatch.StartNew();
        int regionHeight = _maxTouchedRow - _minTouchedRow + 1;
        int regionWidth = _maxOutputWidth;
        int framebufferOffset = checked(_minTouchedRow * _pitch);
        int layerOffset = checked(_minTouchedRow * _width);

        _pixelStore(UnpackAlignment, 1);
        _pixelStore(UnpackRowLength, _width);
        try
        {
            _activeTexture(Texture0);
            _bindTexture(Texture2D, _megaDriveTexture);
            _texSubImage2D(Texture2D, 0, 0, 0, _maxSourceWidth, regionHeight, Bgra, UnsignedByte,
                IntPtr.Add(_framebufferPin.AddrOfPinnedObject(), framebufferOffset));
            _activeTexture(Texture1);
            _bindTexture(Texture2D, _layerTexture);
            _texSubImage2D(Texture2D, 0, 0, 0, regionWidth, regionHeight, RedInteger, UnsignedInt,
                IntPtr.Add(_layerPin.AddrOfPinnedObject(), checked(layerOffset * sizeof(uint))));
        }
        finally { _pixelStore(UnpackRowLength, 0); }

        _bindFramebuffer(Framebuffer, _framebufferId);
        _viewport(0, 0, regionWidth, regionHeight);
        _disable(Blend);
        _disable(DepthTest);
        _disable(ScissorTest);
        _useProgram(_program);
        _activeTexture(Texture0);
        _bindTexture(Texture2D, _megaDriveTexture);
        _activeTexture(Texture1);
        _bindTexture(Texture2D, _layerTexture);
        _bindVertexArray(_vertexArray);
        _uniform1i(_uniformHeight, regionHeight);
        _drawArrays(Triangles, 0, 3);
        _pixelStore(PackAlignment, 1);
        _pixelStore(PackRowLength, _width);
        _readBuffer(ColorAttachment0);
        try
        {
            _readPixels(0, 0, regionWidth, regionHeight, Bgra, UnsignedByte,
                IntPtr.Add(_framebufferPin.AddrOfPinnedObject(), framebufferOffset));
        }
        finally { _pixelStore(PackRowLength, 0); }
        // Client-memory readback is complete when glReadPixels returns; waiting
        // for the entire GL queue again only stalls this frame a second time.
        FlipRows(_minTouchedRow, regionHeight, regionWidth);

        LastFramePixelTransfers = checked(((long)_maxSourceWidth + 2L * regionWidth) * regionHeight);
        TotalPixelTransfers += LastFramePixelTransfers;

        stopwatch.Stop();
        TotalGpuMilliseconds += stopwatch.Elapsed.TotalMilliseconds;
        TotalComposedFrames++;
        TotalComposedLines += LastFrameComposedLines;
        LastFrameUsedGpu = true;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Exception? failure = null;
        void Release(Action action)
        {
            try { action(); }
            catch (Exception error) { failure ??= error; }
        }

        try
        {
            if (_context.MakeCurrentForCore())
            {
                Release(() => _finish());
                if (_program != 0) Release(() => _deleteProgram(_program));
                if (_vertexArray != 0) Release(() => _deleteVertexArrays(1, ref _vertexArray));
                if (_megaDriveTexture != 0 || _layerTexture != 0)
                {
                    Release(() => _deleteTextures(1, ref _megaDriveTexture));
                    Release(() => _deleteTextures(1, ref _layerTexture));
                }
            }
            else failure = new InvalidOperationException("Le contexte OpenGL n’a pas pu être activé pour libérer la composition GPU.");
        }
        finally
        {
            if (_framebufferPin.IsAllocated) _framebufferPin.Free();
            if (_layerPin.IsAllocated) _layerPin.Free();
            GC.KeepAlive(_getProcAddress);
            GC.KeepAlive(_getCurrentFramebuffer);
        }
        if (failure is not null) throw failure;
    }

    private void InitializeOpenGl()
    {
        _pixelStore(UnpackAlignment, 1);
        _genTextures(1, out _megaDriveTexture);
        _bindTexture(Texture2D, _megaDriveTexture);
        ConfigureTexture();
        _texImage2D(Texture2D, 0, Rgba8, _width, _height, 0, Bgra, UnsignedByte, nint.Zero);

        _genTextures(1, out _layerTexture);
        _bindTexture(Texture2D, _layerTexture);
        ConfigureTexture();
        _texImage2D(Texture2D, 0, R32ui, _width, _height, 0, RedInteger, UnsignedInt, nint.Zero);

        uint vertex = 0, fragment = 0;
        try
        {
            vertex = CompileShader(VertexShader, VertexSource);
            fragment = CompileShader(FragmentShader, FragmentSource);
            _program = _createProgram();
            if (_program == 0) throw new InvalidOperationException("glCreateProgram a échoué.");
            _attachShader(_program, vertex);
            _attachShader(_program, fragment);
            _linkProgram(_program);
            _getProgram(_program, LinkStatus, out int linked);
            if (linked == 0) throw new InvalidOperationException("Échec de liaison du shader GPU 32X : " + ProgramLog(_program));
        }
        finally
        {
            if (vertex != 0) _deleteShader(vertex);
            if (fragment != 0) _deleteShader(fragment);
        }

        _genVertexArrays(1, out _vertexArray);
        if (_vertexArray == 0) throw new InvalidOperationException("glGenVertexArrays a échoué.");
        _bindVertexArray(_vertexArray);
        _uniformMegaDrive = UniformLocation("uMegaDrive");
        _uniformLayer = UniformLocation("uLayer32X");
        _uniformHeight = UniformLocation("uHeight");
        if (_uniformMegaDrive < 0 || _uniformLayer < 0 || _uniformHeight < 0)
            throw new InvalidOperationException("Une uniform du shader GPU 32X est absente.");
        _useProgram(_program);
        _uniform1i(_uniformMegaDrive, 0);
        _uniform1i(_uniformLayer, 1);
        _uniform1i(_uniformHeight, _height);
        _finish();
    }

    private void ConfigureTexture()
    {
        _texParameter(Texture2D, TextureMinFilter, Nearest);
        _texParameter(Texture2D, TextureMagFilter, Nearest);
        _texParameter(Texture2D, TextureWrapS, ClampToEdge);
        _texParameter(Texture2D, TextureWrapT, ClampToEdge);
    }

    private uint CompileShader(uint type, string source)
    {
        uint shader = _createShader(type);
        if (shader == 0) throw new InvalidOperationException("glCreateShader a échoué.");
        nint text = Marshal.StringToHGlobalAnsi(source);
        nint pointer = Marshal.AllocHGlobal(IntPtr.Size);
        try
        {
            Marshal.WriteIntPtr(pointer, text);
            _shaderSource(shader, 1, pointer, nint.Zero);
            _compileShader(shader);
            _getShader(shader, CompileStatus, out int compiled);
            if (compiled == 0) throw new InvalidOperationException("Échec de compilation du shader GPU 32X : " + ShaderLog(shader));
            return shader;
        }
        catch
        {
            _deleteShader(shader);
            throw;
        }
        finally
        {
            Marshal.FreeHGlobal(pointer);
            Marshal.FreeHGlobal(text);
        }
    }

    private int UniformLocation(string name)
    {
        nint text = Marshal.StringToHGlobalAnsi(name);
        try { return _getUniformLocation(_program, text); }
        finally { Marshal.FreeHGlobal(text); }
    }

    private string ShaderLog(uint shader)
    {
        nint buffer = Marshal.AllocHGlobal(8192);
        try
        {
            _getShaderInfoLog(shader, 8192, out int length, buffer);
            return length > 0 ? Marshal.PtrToStringAnsi(buffer, length) ?? string.Empty : string.Empty;
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    private string ProgramLog(uint program)
    {
        nint buffer = Marshal.AllocHGlobal(8192);
        try
        {
            _getProgramInfoLog(program, 8192, out int length, buffer);
            return length > 0 ? Marshal.PtrToStringAnsi(buffer, length) ?? string.Empty : string.Empty;
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    private T Load<T>(string name) where T : Delegate
    {
        nint text = Marshal.StringToHGlobalAnsi(name);
        try
        {
            nint address = _getProcAddress(text);
            if (address == nint.Zero) throw new PlatformNotSupportedException("OpenGL ne fournit pas " + name + ".");
            return Marshal.GetDelegateForFunctionPointer<T>(address);
        }
        finally { Marshal.FreeHGlobal(text); }
    }

    private void FlipRows(int firstRow, int rowCount, int width)
    {
        int rowBytes = checked(width * sizeof(uint));
        for (int top = 0, bottom = rowCount - 1; top < bottom; top++, bottom--)
        {
            Span<byte> first = _framebuffer.AsSpan((firstRow + top) * _pitch, rowBytes);
            Span<byte> last = _framebuffer.AsSpan((firstRow + bottom) * _pitch, rowBytes);
            first.CopyTo(_rowScratch);
            last.CopyTo(first);
            _rowScratch.AsSpan(0, rowBytes).CopyTo(last);
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void GenObjects(int count, out uint handle);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void DeleteObjects(int count, ref uint handle);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void BindTextureFunction(uint target, uint texture);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void TexImage2DFunction(uint target, int level, int internalFormat, int width, int height, int border, uint format, uint type, nint pixels);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void TexSubImage2DFunction(uint target, int level, int x, int y, int width, int height, uint format, uint type, nint pixels);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void TexParameterFunction(uint target, uint name, int value);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void PixelStoreFunction(uint name, int value);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void ActiveTextureFunction(uint texture);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void GenerateVertexArraysFunction(int count, out uint handle);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void DeleteVertexArraysFunction(int count, ref uint handle);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void BindVertexArrayFunction(uint handle);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void BindFramebufferFunction(uint target, uint handle);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void ViewportFunction(int x, int y, int width, int height);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void DisableFunction(uint capability);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void DrawArraysFunction(uint mode, int first, int count);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void ReadBufferFunction(uint source);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void ReadPixelsFunction(int x, int y, int width, int height, uint format, uint type, nint pixels);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void FinishFunction();
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate uint CreateShaderFunction(uint type);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void ShaderSourceFunction(uint shader, int count, nint strings, nint lengths);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void CompileShaderFunction(uint shader);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void GetShaderFunction(uint shader, uint parameter, out int value);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void GetShaderInfoLogFunction(uint shader, int capacity, out int length, nint log);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void DeleteShaderFunction(uint shader);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate uint CreateProgramFunction();
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void AttachShaderFunction(uint program, uint shader);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void LinkProgramFunction(uint program);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void GetProgramFunction(uint program, uint parameter, out int value);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void GetProgramInfoLogFunction(uint program, int capacity, out int length, nint log);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void DeleteProgramFunction(uint program);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void UseProgramFunction(uint program);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate int GetUniformLocationFunction(uint program, nint name);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void Uniform1iFunction(int location, int value);
}
