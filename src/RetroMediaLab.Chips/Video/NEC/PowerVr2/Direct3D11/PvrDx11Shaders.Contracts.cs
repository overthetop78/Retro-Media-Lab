using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.D3DCompiler;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Direct3D11;

internal delegate SharpGen.Runtime.Result PvrDx11ShaderCompiler(
    IntPtr sourceData,
    nuint sourceSize,
    string? sourceName,
    ShaderMacro[]? defines,
    Include include,
    string entryPoint,
    string target,
    ShaderFlags flags1,
    EffectFlags flags2,
    out Blob? code,
    out Blob? errorMessages);

internal partial class PvrDx11CachedShaders
{
    protected void EnableCache(bool enable) => _enabled = enable;

    private readonly record struct ShaderBlob(uint Size, byte[] Blob);

    private readonly Dictionary<ulong, ShaderBlob> _shaderCache = [];
    private bool _enabled = true;
}

internal sealed partial class PvrDx11Shaders : PvrDx11CachedShaders
{
    private ID3D11Device? _device;
    private readonly Dictionary<uint, ID3D11PixelShader?> _shaders = [];
    private readonly ID3D11VertexShader?[] _vertexShaders = new ID3D11VertexShader?[8];
    private ID3D11PixelShader? _modVolShader;
    private readonly ID3D11VertexShader?[] _modVolVertexShaders = new ID3D11VertexShader?[4];
    private ID3D11PixelShader? _quadPixelShader;
    private ID3D11VertexShader? _quadVertexShader;
    private ID3D11VertexShader? _quadRotateVertexShader;
    private PvrDx11ShaderCompiler? _d3dCompile;

    private const string CacheFile = "dx11_shader_cache.bin";
}
