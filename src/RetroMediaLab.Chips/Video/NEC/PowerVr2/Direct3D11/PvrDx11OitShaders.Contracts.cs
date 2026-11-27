using Vortice.Direct3D11;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Direct3D11;

internal sealed partial class PvrDx11OitShaders : PvrDx11CachedShaders
{
    internal enum Pass { Depth, Color, Oit }

    internal void Term()
    {
        try
        {
            SaveCache(CacheFile);
        }
        finally
        {
            foreach (ID3D11PixelShader? shader in _shaders.Values)
                shader?.Dispose();
            _shaders.Clear();
            foreach (ID3D11VertexShader? shader in _vertexShaders.Values)
                shader?.Dispose();
            _vertexShaders.Clear();
            ReleaseShaders(_modVolVertexShaders);
            ReleaseShaders(_modVolShaders);
            ReleaseShaders(_trModVolShaders);
            ReleaseShaders(_finalShaders);
            _clearShader?.Dispose();
            _clearShader = null;
            _finalVertexShader?.Dispose();
            _finalVertexShader = null;
            _device = null;
        }
    }

    private ID3D11Device? _device;
    private readonly Dictionary<uint, ID3D11PixelShader?> _shaders = [];
    private readonly Dictionary<uint, ID3D11VertexShader?> _vertexShaders = [];
    private readonly ID3D11PixelShader?[] _modVolShaders = new ID3D11PixelShader?[2];
    private readonly ID3D11VertexShader?[] _modVolVertexShaders = new ID3D11VertexShader?[4];
    private readonly ID3D11PixelShader?[] _trModVolShaders = new ID3D11PixelShader?[16];
    private readonly ID3D11PixelShader?[] _finalShaders = new ID3D11PixelShader?[2];
    private ID3D11PixelShader? _clearShader;
    private ID3D11VertexShader? _finalVertexShader;
    private PvrDx11ShaderCompiler? _d3dCompile;
    private int _maxLayers;

    private const string CacheFile = "dx11oit_shader_cache.bin";
}
