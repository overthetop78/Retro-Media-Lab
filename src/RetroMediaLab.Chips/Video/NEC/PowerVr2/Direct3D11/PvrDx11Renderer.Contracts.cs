using Vortice.Direct3D11;
using Vortice.Mathematics;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Direct3D11;

internal unsafe partial class PvrDx11Renderer : PvrRenderBackend
{
    internal override bool Present()
    {
        if (!FrameRendered || ClearLastFrame)
            return false;
        FrameRendered = false;
        PvrDx11Context.Instance.ImGuiDriver!.SetFrameRendered();
        return true;
    }

    protected struct VertexConstants
    {
        internal fixed float TransMatrix[16];
        internal fixed float LeftPlane[4];
        internal fixed float TopPlane[4];
        internal fixed float RightPlane[4];
        internal fixed float BottomPlane[4];
    }

    protected struct PixelConstants
    {
        internal fixed float ColorClampMin[4];
        internal fixed float ColorClampMax[4];
        internal fixed float FogColVert[4];
        internal fixed float FogColRam[4];
        internal fixed float DitherDivisor[4];
        internal float FogDensity;
        internal float ShadowScale;
        internal float AlphaTestValue;
    }

    protected struct PixelPolyConstants
    {
        internal fixed float ClipTest[4];
        internal float PaletteIndex;
        internal float TrilinearAlpha;
    }

    protected virtual void SetRTTSize(int width, int height)
    {
    }

    protected ID3D11Device Device = null!;
    protected ID3D11DeviceContext DeviceContext = null!;
    protected ID3D11Texture2D? DepthTex;
    protected ID3D11DepthStencilView? DepthTexView;
    protected ID3D11InputLayout? MainInputLayout;
    protected ID3D11InputLayout? ModVolInputLayout;
    protected ID3D11Buffer? PxlPolyConstants;
    protected ID3D11Buffer? VertexBuffer;
    protected ID3D11Buffer? IndexBuffer;
    protected ID3D11Buffer? ModvolBuffer;
    protected ID3D11RenderTargetView? FbRenderTarget;
    protected ID3D11RenderTargetView? RttRenderTarget;
    protected ID3D11Texture2D? FbTex;
    protected ID3D11ShaderResourceView? FbTextureView;

    protected readonly PvrDx11BlendStates BlendStates = new();
    protected readonly PvrDx11DepthStencilStates DepthStencilStates = new();
    protected PvrDx11Samplers Samplers = null!;
    protected readonly PvrTransformMatrix Matrices = new(true);
    protected RawRect ScissorRect;
    protected uint Width;
    protected uint Height;
    protected bool FrameRendered;
    protected bool FrameRenderedOnce;
    protected readonly PvrDx11Naomi2Helper N2Helper = new();
    protected float AspectRatio = 4f / 3f;
    protected bool Dithering;
    protected RenderContext RendContext = null!;

    private uint _vertexBufferSize;
    private uint _modvolBufferSize;
    private uint _indexBufferSize;

    private ID3D11Texture2D? _dcfbTexture;
    private ID3D11ShaderResourceView? _dcfbTextureView;
    private ID3D11Texture2D? _paletteTexture;
    private ID3D11ShaderResourceView? _paletteTextureView;
    private ID3D11Texture2D? _fogTexture;
    private ID3D11ShaderResourceView? _fogTextureView;
    private ID3D11Texture2D? _rttTexture;
    private ID3D11Texture2D? _rttDepthTex;
    private ID3D11DepthStencilView? _rttDepthTexView;
    private ID3D11Texture2D? _whiteTexture;
    private ID3D11ShaderResourceView? _whiteTextureView;
    private ID3D11Texture2D? _fbScaledTexture;
    private ID3D11ShaderResourceView? _fbScaledTextureView;
    private ID3D11RenderTargetView? _fbScaledRenderTarget;
    private ID3D11Texture2D? _vrStagingTexture;
    private ID3D11ShaderResourceView? _vrStagingTextureSRV;
    private ID3D11Texture2D? _vrScaledTexture;
    private ID3D11RenderTargetView? _vrScaledRenderTarget;

    private ID3D11RasterizerState? _rasterCullNone;
    private ID3D11RasterizerState? _rasterCullFront;
    private ID3D11RasterizerState? _rasterCullBack;

    private readonly PvrDx11TextureCache _texCache = new();
    private PvrDx11Shaders _shaders = null!;
    private PvrDx11Quad? _quad;
    private ID3D11Buffer? _vtxConstants;
    private ID3D11Buffer? _pxlConstants;
    private bool _scissorEnable;
}
