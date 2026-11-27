using Vortice.Direct3D9;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Direct3D9;

internal sealed partial class PvrDx9Context : PvrGraphicsContext
{
    internal static void Create(nint window, nint display) => _ = new PvrDx9Context(window, display);
    internal new static PvrDx9Context Instance => (PvrDx9Context)PvrGraphicsContext.Instance;

    internal IDirect3D9 D3d => _d3d!;
    internal IDirect3DDevice9 Device => _device!;
    internal void SetOverlay(bool overlayOnly) => _overlayOnly = overlayOnly;
    internal override string DriverName => _driverName;
    internal override string DriverVersion => _driverVersion;
    internal override bool IsAmd => _amd;
    internal void SetFrameRendered() => _frameRendered = true;
    internal bool IsReady => _deviceReady;

    private readonly PvrWindowsLibrary _d3d9Library = new();
    private IDirect3D9? _d3d;
    private IDirect3DDevice9? _device;
    private PresentParameters _presentParameters;
    private bool _overlayOnly;
    private readonly PvrDx9Overlay _overlay = new();
    private bool _swapOnVSync;
    private bool _frameRendered;
    private string _driverName = "";
    private string _driverVersion = "";
    private bool _amd;
    private bool _deviceReady;
}
