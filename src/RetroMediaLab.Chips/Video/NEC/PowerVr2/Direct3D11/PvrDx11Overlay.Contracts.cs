using Vortice.Direct3D11;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Direct3D11;

internal sealed unsafe partial class PvrDx11Overlay
{
    internal void Init(ID3D11Device device, ID3D11DeviceContext deviceContext, PvrDx11Shaders shaders, PvrDx11Samplers samplers)
    {
        _device = device;
        _deviceContext = deviceContext;
        _samplers = samplers;
        _quad.Init(device, deviceContext, shaders);
        Array.Clear(_vmuLastChanged);
    }

    internal void Term()
    {
        try
        {
            _blendStates.Term();
            _depthStencilStates.Term();
            _xhairTextureView?.Dispose();
            _xhairTextureView = null;
            _xhairTexture?.Dispose();
            _xhairTexture = null;
            for (int i = 0; i < _vmuTextureViews.Length; i++)
            {
                _vmuTextureViews[i]?.Dispose();
                _vmuTextureViews[i] = null;
            }
            for (int i = 0; i < _vmuTextures.Length; i++)
            {
                _vmuTextures[i]?.Dispose();
                _vmuTextures[i] = null;
            }
        }
        finally
        {
            _quad.Dispose();
            _deviceContext = null!;
            _device = null!;
        }
    }

    private ID3D11Device _device = null!;
    private ID3D11DeviceContext _deviceContext = null!;
    private ID3D11Texture2D? _xhairTexture;
    private ID3D11ShaderResourceView? _xhairTextureView;
    private readonly ID3D11Texture2D?[] _vmuTextures = new ID3D11Texture2D?[8];
    private readonly ID3D11ShaderResourceView?[] _vmuTextureViews = new ID3D11ShaderResourceView?[8];
    private readonly ulong[] _vmuLastChanged = new ulong[8];
    private readonly PvrDx11Quad _quad = new();
    private PvrDx11Samplers _samplers = null!;
    private readonly PvrDx11BlendStates _blendStates = new();
    private readonly PvrDx11DepthStencilStates _depthStencilStates = new();
}
