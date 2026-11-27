using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Direct3D11;

internal sealed partial class PvrDx11Context : PvrGraphicsContext
{
    internal static void Create(nint window, nint display = 0) => _ = new PvrDx11Context(window, display);
    internal new static PvrDx11Context Instance => (PvrDx11Context)PvrGraphicsContext.Instance;
    internal ID3D11Device Device => _device!;
    internal ID3D11DeviceContext DeviceContext => _deviceContext!;
    internal ref ID3D11RenderTargetView? RenderTarget => ref _renderTargetView;
    internal PvrDx11Shaders Shaders => _shaders;
    internal PvrDx11Samplers Samplers => _samplers;

    internal override string DriverName => _adapterDescription;
    internal override string DriverVersion => _adapterVersion;
    internal override bool HasPerPixel => _featureLevel >= FeatureLevel.Level_11_0;
    internal bool IsIntel => _vendorId == Intel;
    internal override bool IsAmd => _vendorId == Ati || _vendorId == Amd;
    internal override void SetSwapInterval(int interval) => _gameSwapInterval = interval;
    internal void SetOverlay(bool overlayOnly) => _overlayOnly = overlayOnly;
    internal void SetFrameRendered() => _frameRendered = true;
    internal bool HasShaderCache() => _hasShaderCache;
    internal bool TextureFormatSupported(PvrTextureType type) => _supportedTextureFormats[(int)type];

    private ID3D11Device? _device;
    private ID3D11DeviceContext? _deviceContext;
    private IDXGISwapChain? _swapChain;
    private IDXGISwapChain1? _swapChain1;
    private ID3D11RenderTargetView? _renderTargetView;
    private bool _overlayOnly;
    private readonly PvrDx11Overlay _overlay = new();
    private bool _allowTearing;
    private bool _swapOnVSync;
    private bool _frameRendered;
    private string _adapterDescription = "";
    private string _adapterVersion = "";
    private uint _vendorId;
    private bool _hasShaderCache;
    private readonly PvrDx11Shaders _shaders = new();
    private readonly PvrDx11Samplers _samplers = new();
    private FeatureLevel _featureLevel;
    private readonly bool[] _supportedTextureFormats = new bool[5];
    private readonly PvrWindowsLibrary _compilerLibrary = new();
    private PvrDx11ShaderCompiler? _compiler;
    private int _gameSwapInterval = 1;

    private const uint Intel = 0x8086;
    private const uint Ati = 0x1002;
    private const uint Amd = 0x1022;
}
