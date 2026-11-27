using System.Numerics;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal static unsafe partial class PvrGles
{
    internal const uint MaxTextureMaxAnisotropy = 0x84ff;
    internal const uint PrimitiveRestartFixedIndex = 0x8d69;
    internal const uint Rgba8 = 0x8058;
    internal const uint R8 = 0x8229;
    internal const uint VertexPosArray = 0;
    internal const uint VertexColBaseArray = 1;
    internal const uint VertexColOffsArray = 2;
    internal const uint VertexUvArray = 3;
    internal const uint VertexColBase1Array = 4;
    internal const uint VertexColOffs1Array = 5;
    internal const uint VertexUv1Array = 6;
    internal const uint VertexNormArray = 7;

    internal static readonly PvrGlContext Gl = new();
    internal static readonly PvrGlShaderUniforms ShaderUniforms = new();
    internal static readonly PvrGlTextureCache TexCache = new();

    internal static void Check()
    {
        if (PvrConfiguration.OpenGlChecks)
            PvrRuntime.Verify(PvrGl.GetError() == PvrGl.NoError);
    }
}

internal sealed class PvrGlPipelineShader : PvrGlNaomi2Shader
{
    internal int DepthScale;
    internal int ClipTest;
    internal int AlphaTestValue;
    internal int FogColorRam;
    internal int FogColorVertex;
    internal int FogDensity;
    internal int TrilinearAlpha;
    internal int FogClampMin;
    internal int FogClampMax;
    internal int NdcMat;
    internal int PaletteIndex;
    internal int DitherDivisor;
    internal int TexSize;

    internal bool AlphaTest;
    internal bool InsideClipping;
    internal bool Texture;
    internal bool UseAlpha;
    internal bool IgnoreTexA;
    internal uint ShadInstr;
    internal bool Offset;
    internal uint FogCtrl;
    internal bool Gouraud;
    internal bool BumpMap;
    internal bool FogClamping;
    internal bool Trilinear;
    internal int Palette;
    internal bool Naomi2;
    internal bool DivPosZ;
    internal bool Dithering;
}

internal sealed unsafe class PvrGlBuffer : IDisposable
{
    private readonly uint _type;
    private readonly uint _usage;
    private nint _size;
    private uint _name;

    internal PvrGlBuffer(uint type, uint usage = PvrGl.StreamDraw)
    {
        _type = type;
        _usage = usage;
        fixed (uint* name = &_name)
            PvrGl.GenBuffers(1, name);
    }

    public void Dispose()
    {
        fixed (uint* name = &_name)
            PvrGl.DeleteBuffers(1, name);
    }

    internal void Bind() => PvrGl.BindBuffer(_type, _name);

    internal uint GetName() => _name;

    internal void Update(void* data, nint size)
    {
        Bind();
        if (size > _size)
        {
            PvrGl.BufferData(_type, size, data, _usage);
            _size = size;
        }
        else
        {
            PvrGl.BufferSubData(_type, 0, size, data);
        }
    }
}

internal sealed unsafe partial class PvrGlFramebuffer : IDisposable
{
    internal void Bind(uint type = PvrGl.Framebuffer) => PvrGl.BindFramebuffer(type, _framebuffer);
    internal int GetWidth() => _width;
    internal int GetHeight() => _height;
    internal uint GetTexture() => _texture;

    internal uint DetachTexture()
    {
        uint texture = _texture;
        _texture = 0;
        return texture;
    }

    internal uint GetFramebuffer() => _framebuffer;

    private readonly int _width;
    private readonly int _height;
    private uint _texture;
    private uint _framebuffer;
    private uint _colorBuffer;
    private uint _depthBuffer;
}

internal abstract unsafe class PvrGlVertexArray
{
    internal void Bind(PvrGlBuffer buffer, PvrGlBuffer? indexBuffer = null)
    {
        if (_vertexArray == 0)
        {
            if (PvrGles.Gl.Major >= 3)
            {
                fixed (uint* vao = &_vertexArray)
                    PvrGl.GenVertexArrays(1, vao);
                PvrGl.BindVertexArray(_vertexArray);
            }
            buffer.Bind();
            if (indexBuffer != null)
                indexBuffer.Bind();
            else
                PvrGl.BindBuffer(PvrGl.ElementArrayBuffer, 0);
            DefineVtxAttribs();
        }
        else
        {
            BindVertexArray(_vertexArray);
            buffer.Bind();
            if (indexBuffer != null)
                indexBuffer.Bind();
            else
                PvrGl.BindBuffer(PvrGl.ElementArrayBuffer, 0);
        }
    }

    internal static void Unbind() => BindVertexArray(0);

    internal void Term()
    {
        if (PvrGles.Gl.Major >= 3)
            fixed (uint* vao = &_vertexArray)
                PvrGl.DeleteVertexArrays(1, vao);
        _vertexArray = 0;
    }

    protected abstract void DefineVtxAttribs();

    private static void BindVertexArray(uint vao)
    {
        if (PvrGles.Gl.Major >= 3)
            PvrGl.BindVertexArray(vao);
    }

    private uint _vertexArray;
}

internal sealed unsafe partial class PvrGlMainVertexArray : PvrGlVertexArray { }
internal sealed unsafe partial class PvrGlModvolVertexArray : PvrGlVertexArray { }

internal sealed class PvrGlContext
{
    internal struct ModifierVolumeShader
    {
        internal uint Program;
        internal int DepthScale;
        internal int ShaderColor;
        internal int NdcMat;
    }

    internal struct Naomi2ModifierVolumeShader
    {
        internal uint Program;
        internal int DepthScale;
        internal int ShaderColor;
        internal int NdcMat;
        internal int MvMat;
        internal int ProjMat;
    }

    internal sealed class VertexBuffers
    {
        internal readonly PvrGlMainVertexArray MainVao = new();
        internal readonly PvrGlModvolVertexArray ModvolVao = new();
        internal PvrGlBuffer? Geometry;
        internal PvrGlBuffer? Modvols;
        internal PvrGlBuffer? Indices;
    }

    internal struct RenderToTexture { internal PvrGlFramebuffer? Framebuffer; }

    internal struct OutputFramebuffer
    {
        internal PvrGlFramebuffer? Framebuffer;
        internal float AspectRatio;
        internal uint OriginalFbo;
        internal float ShiftX;
        internal float ShiftY;
    }

    internal struct DreamcastFramebuffer
    {
        internal uint Texture;
        internal int Width;
        internal int Height;
    }

    internal struct FramebufferScaling { internal PvrGlFramebuffer? Framebuffer; }

    internal struct SecondOutputFramebuffer
    {
        internal PvrGlFramebuffer? Framebuffer;
        internal bool Ready;
    }

    internal struct VideoRouting { internal PvrGlFramebuffer? Framebuffer; }

    internal ModifierVolumeShader ModvolShader;
    internal Naomi2ModifierVolumeShader N2ModvolShader;
    internal readonly Dictionary<uint, PvrGlPipelineShader> Shaders = [];
    internal readonly VertexBuffers Vbo = new();
    internal RenderToTexture Rtt;
    internal OutputFramebuffer Ofbo;
    internal DreamcastFramebuffer Dcfb;
    internal FramebufferScaling FbScaling;
    internal SecondOutputFramebuffer Ofbo2;
    internal VideoRouting Routing;
    internal PvrGlQuadDrawer? QuadDrawer;
    internal string Version = null!;
    internal string GlslVersionHeader = null!;
    internal int Major;
    internal int Minor;
    internal bool IsGles;
    internal uint SingleChannelFormat;
    internal uint IndexType;
    internal bool PackedDepthStencilSupported;
    internal bool Depth24Supported;
    internal bool HighpFloatSupported;
    internal float MaxAnisotropy;
    internal bool MesaNouveau;
    internal bool Mali;
    internal bool BorderClampSupported;
    internal bool PrimRestartSupported;
    internal bool PrimRestartFixedSupported;
    internal bool BogusBlitFramebuffer;
    internal RenderContext? RendContext;
    internal readonly PvrTransformMatrix Matrices = new();

    internal int GetIndexSize() => IndexType == PvrGl.UnsignedInt ? sizeof(uint) : sizeof(ushort);
}

internal enum PvrGlModifierVolumeMode { Xor, Or, Inclusion, Exclusion, ModeCount }

internal struct PvrGlBaseClipping
{
    internal bool Enabled;
    internal int X;
    internal int Y;
    internal int Width;
    internal int Height;
}

internal sealed unsafe class PvrGlShaderUniforms
{
    internal float PtAlpha;
    internal readonly float[] DepthCoefs = new float[4];
    internal float FogDensity;
    internal readonly float[] FogColorRam = new float[3];
    internal readonly float[] FogColorVertex = new float[3];
    internal readonly float[] FogClampMin = new float[4];
    internal readonly float[] FogClampMax = new float[4];
    internal Matrix4x4 NdcMat;
    internal PvrGlBaseClipping BaseClipping;
    internal bool Dithering;
    internal readonly float[] DitherDivisor = new float[4];

    internal void Set(PvrGlPipelineShader shader)
    {
        if (shader.AlphaTestValue != -1)
            PvrGl.Uniform1f(shader.AlphaTestValue, PtAlpha);
        if (shader.DepthScale != -1)
            fixed (float* values = DepthCoefs)
                PvrGl.Uniform4fv(shader.DepthScale, 1, values);
        if (shader.FogDensity != -1)
            PvrGl.Uniform1f(shader.FogDensity, FogDensity);
        if (shader.FogColorRam != -1)
            fixed (float* values = FogColorRam)
                PvrGl.Uniform3fv(shader.FogColorRam, 1, values);
        if (shader.FogColorVertex != -1)
            fixed (float* values = FogColorVertex)
                PvrGl.Uniform3fv(shader.FogColorVertex, 1, values);
        if (shader.FogClampMin != -1)
            fixed (float* values = FogClampMin)
                PvrGl.Uniform4fv(shader.FogClampMin, 1, values);
        if (shader.FogClampMax != -1)
            fixed (float* values = FogClampMax)
                PvrGl.Uniform4fv(shader.FogClampMax, 1, values);
        if (shader.NdcMat != -1)
            fixed (Matrix4x4* matrix = &NdcMat)
                PvrGl.UniformMatrix4fv(shader.NdcMat, 1, 0, (float*)matrix);
        if (shader.DitherDivisor != -1)
            fixed (float* values = DitherDivisor)
                PvrGl.Uniform4fv(shader.DitherDivisor, 1, values);
    }
}

internal sealed unsafe partial class PvrGlTextureCacheData : PvrBaseTextureCacheData
{
    internal PvrGlTextureCacheData(TSP tsp, TCW tcw, int area) : base(tsp, tcw, area)
    {
    }

    internal PvrGlTextureCacheData(PvrGlTextureCacheData other) : base(other)
    {
        (TextureId, other.TextureId) = (other.TextureId, TextureId);
        (Storage, other.Storage) = (other.Storage, Storage);
    }

    internal uint TextureId;
    internal int[] Storage = new int[4];

    internal override string GetId() => TextureId.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private delegate void UploadFlavor(PvrGlTextureCacheData texture, int width, int height,
        byte* buffer, bool mipmapped, bool mipmapsIncluded);
    private static UploadFlavor? _uploadToGpu;
}

internal sealed unsafe class PvrGlTextureCache : PvrBaseTextureCache<PvrGlTextureCacheData>
{
    private readonly List<uint> _texturesToDelete = [];

    protected override PvrGlTextureCacheData CreateTexture(TSP tsp, TCW tcw, int area) => new(tsp, tcw, area);

    internal void Cleanup()
    {
        if (_texturesToDelete.Count != 0)
        {
            fixed (uint* ids = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_texturesToDelete))
                PvrGlCache.Instance.DeleteTextures(_texturesToDelete.Count, ids);
            _texturesToDelete.Clear();
        }
        CollectCleanup();
    }

    internal void DeleteLater(uint textureId) => _texturesToDelete.Add(textureId);
}

internal unsafe partial class PvrOpenGlRenderer : PvrRenderBackend
{
    internal override bool RenderLastFrame()
    {
        if (ClearLastFrame)
            return false;
        SaveCurrentFramebuffer();
        try
        {
            return RenderLastFrameCore();
        }
        finally
        {
            RestoreCurrentFramebuffer();
        }
    }

    internal override bool Present()
    {
        if (!FrameRendered || ClearLastFrame)
            return false;
        PvrGlGraphicsContext.Instance.ImGuiDriver.SetFrameRendered();
        FrameRendered = false;
        return true;
    }

    protected virtual uint GetFogTextureSlot() => PvrGl.Texture1;
    protected virtual uint GetPaletteTextureSlot() => PvrGl.Texture2;

    protected static void SaveCurrentFramebuffer()
    {
        PvrGles.Gl.Ofbo.OriginalFbo = 0;
        fixed (uint* original = &PvrGles.Gl.Ofbo.OriginalFbo)
            PvrGl.GetIntegerv(PvrGl.FramebufferBinding, (int*)original);
    }

    protected static void RestoreCurrentFramebuffer() =>
        PvrGl.BindFramebuffer(PvrGl.Framebuffer, PvrGles.Gl.Ofbo.OriginalFbo);

    protected bool FrameRendered;
    protected int Width = 640;
    protected int Height = 480;
}

internal class PvrOpenGlSource
{
    private readonly PvrShaderSource _source;

    internal PvrOpenGlSource() : this(PvrGles.Gl.GlslVersionHeader)
    {
        AddConstant("TARGET_GL", PvrGles.Gl.Version);
        AddSource(PvrGles.ShaderCompatSource);
    }

    protected PvrOpenGlSource(string header)
    {
        _source = new PvrShaderSource(header);
    }

    internal PvrOpenGlSource AddConstant(string name, string value = "0")
    {
        _source.AddConstant(name, value);
        return this;
    }

    internal PvrOpenGlSource AddConstant(string name, int value)
    {
        _source.AddConstant(name, value);
        return this;
    }

    internal PvrOpenGlSource AddSource(string source)
    {
        _source.AddSource(source);
        return this;
    }

    internal PvrOpenGlSource SetConstant(string name, int value)
    {
        _source.SetConstant(name, value);
        return this;
    }

    internal string Generate() => _source.Generate();
}
