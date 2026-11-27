using System.Diagnostics;
using System.Globalization;
using SharpGen.Runtime;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Direct3D11;

internal sealed partial class PvrDx11Context
{
    private PvrDx11Context(nint window, nint display) : base(window, display)
    {
        bool initialized = false;
        try
        {
            initialized = Init();
            if (!initialized)
                throw new PvrRendererException("DX11 initialization failed");
        }
        finally
        {
            if (!initialized)
                Dispose();
        }
    }

    public override void Dispose()
    {
        try
        {
            Term();
        }
        finally
        {
            try
            {
                _compilerLibrary.Dispose();
            }
            finally
            {
                base.Dispose();
            }
        }
    }

    private bool Init(bool keepCurrentWindow = false)
    {
        Trace.TraceInformation("DX11 Context initializing");
#if USE_SDL
        if (!keepCurrentWindow && !PvrPlatform.RecreateWindow(0))
            return false;
#endif

        _allowTearing = false;
        IDXGIAdapter? adapter = null;
        Result result = DXGI.CreateDXGIFactory1(out IDXGIFactory1? factory);
        try
        {
            if (result.Success)
            {
                using IDXGIFactory6? factory6 = factory!.QueryInterfaceOrNull<IDXGIFactory6>();
                if (factory6 is not null)
                {
                    factory6.EnumAdapterByGpuPreference(0, GpuPreference.HighPerformance, out adapter);
                    if (factory6.CheckFeatureSupport(Vortice.DXGI.Feature.PresentAllowTearing, out uint tearing).Success && tearing != 0)
                        _allowTearing = true;
                }
            }
        }
        finally
        {
            factory?.Dispose();
        }

        FeatureLevel[] features =
        [
            FeatureLevel.Level_11_1, FeatureLevel.Level_11_0, FeatureLevel.Level_10_1, FeatureLevel.Level_10_0
        ];

        try
        {
            result = D3D11.D3D11CreateDevice(adapter, adapter is null ? DriverType.Hardware : DriverType.Unknown,
                DeviceCreationFlags.BgraSupport, features, out _device, out _featureLevel, out _deviceContext);
        }
        finally
        {
            adapter?.Dispose();
        }
        if (result.Failure)
        {
            Trace.TraceWarning($"D3D11 device creation failed: {result.Code:x}");
            return false;
        }

        using IDXGIDevice2 dxgiDevice = Device.QueryInterface<IDXGIDevice2>();
        using IDXGIAdapter selectedAdapter = dxgiDevice.GetAdapter();
        AdapterDescription adapterDescription = selectedAdapter.Description;
        _adapterDescription = adapterDescription.Description;
        _adapterVersion = adapterDescription.Revision.ToString(CultureInfo.InvariantCulture);
        _vendorId = adapterDescription.VendorId;
        using IDXGIFactory1 parentFactory = selectedAdapter.GetParent<IDXGIFactory1>();
        using IDXGIFactory2? factory2 = parentFactory.QueryInterfaceOrNull<IDXGIFactory2>();

        if (factory2 is not null)
        {
            SwapChainDescription1 description = default;
            description.Format = Format.B8G8R8A8_UNorm;
            description.SwapEffect = SwapEffect.FlipSequential;
            description.BufferUsage = Usage.RenderTargetOutput;
            description.BufferCount = 2;
            description.SampleDescription.Count = 1;
            description.AlphaMode = AlphaMode.Ignore;
            if (_allowTearing)
                description.Flags = SwapChainFlags.AllowTearing;

            result = factory2.CreateSwapChainForHwnd(Device, Window, description, null, null, out _swapChain1);
            if (result.Success)
                _swapChain = _swapChain1!.QueryInterface<IDXGISwapChain>();
        }
        else
        {
            _swapChain1?.Dispose();
            _swapChain1 = null;

            SwapChainDescription description = default;
            description.BufferCount = 2;
            description.BufferDescription.Format = Format.R8G8B8A8_UNorm;
            description.BufferDescription.RefreshRate.Numerator = 60;
            description.BufferDescription.RefreshRate.Denominator = 1;
            description.OutputWindow = Window;
            description.Windowed = true;
            description.SwapEffect = SwapEffect.FlipSequential;
            description.BufferCount = 2;
            description.BufferUsage = Usage.RenderTargetOutput;
            description.SampleDescription.Count = 1;
            description.SampleDescription.Quality = 0;
            if (_allowTearing)
                description.Flags = SwapChainFlags.AllowTearing;

            result = parentFactory.CreateSwapChain(Device, description, out _swapChain);
        }

        if (result.Failure)
        {
            Trace.TraceWarning($"D3D11 swap chain creation failed: {result.Code:x}");
            _device?.Dispose();
            _device = null;
            return false;
        }

        parentFactory.MakeWindowAssociation(Window, WindowAssociationFlags.IgnoreWindowChanges);
        if (Device.CheckFeatureSupport(Vortice.Direct3D11.Feature.ShaderCache, out FeatureDataShaderCache cache).Success)
        {
            _hasShaderCache = (cache.SupportFlags & ShaderCacheSupportFlags.AutomaticDiskCache) != 0;
            if (!_hasShaderCache)
                Trace.TraceInformation("No system-provided shader cache");
        }

        ImGuiDriver = new PvrDx11Driver(Device, DeviceContext);
        Resize();
        _shaders.Init(Device, PvrDx11Native.D3DCompile);
        _overlay.Init(Device, DeviceContext, _shaders, _samplers);
        bool success = CheckTextureSupport();
        if (!success)
            Term();
        return success;
    }

    private void Term()
    {
        Trace.TraceInformation("DX11 Context terminating");
        try
        {
            _overlay.Term();
        }
        finally
        {
            try
            {
                _samplers.Term();
            }
            finally
            {
                try
                {
                    _shaders.Term();
                }
                finally
                {
                    try
                    {
                        ImGuiDriver?.Dispose();
                    }
                    finally
                    {
                        ImGuiDriver = null;
                        _renderTargetView?.Dispose();
                        _renderTargetView = null;
                        _swapChain1?.Dispose();
                        _swapChain1 = null;
                        _swapChain?.Dispose();
                        _swapChain = null;
                        try
                        {
                            if (_deviceContext is not null)
                            {
                                _deviceContext.ClearState();
                                _deviceContext.Flush();
                            }
                        }
                        finally
                        {
                            _deviceContext?.Dispose();
                            _deviceContext = null;
                            _device?.Dispose();
                            _device = null;
                            _compiler = null;
                        }
                    }
                }
            }
        }
    }

    internal void Present()
    {
        if (!_frameRendered)
            return;
        _frameRendered = false;
        bool swapOnVSync = !PvrConfiguration.FastForwardMode && PvrConfiguration.VSync;
        Result result;
        if (_swapChain is null)
            result = Vortice.DXGI.ResultCode.DeviceReset;
        else if (swapOnVSync)
        {
            int swapInterval = Math.Clamp((int)(PvrRuntime.Display.RefreshRate / 60f * _gameSwapInterval), 1, 4);
            result = _swapChain.Present((uint)swapInterval, PresentFlags.None);
        }
        else
        {
            result = _swapChain.Present(0, _allowTearing ? PresentFlags.AllowTearing : PresentFlags.DoNotWait);
        }

        if (result == Vortice.DXGI.ResultCode.DeviceRemoved || result == Vortice.DXGI.ResultCode.DeviceReset)
        {
            Trace.TraceWarning("Present failed: device removed/reset");
            HandleDeviceLost();
        }
        else if (result != Vortice.DXGI.ResultCode.WasStillDrawing && result.Failure)
        {
            Trace.TraceWarning($"Present failed {result.Code:x}");
        }
    }

    internal void EndImGuiFrame()
    {
        if (_device is not null && _deviceContext is not null && _renderTargetView is not null)
        {
            if (_overlayOnly)
                _overlay.Draw(PvrRuntime.Display.Width, PvrRuntime.Display.Height, PvrConfiguration.FloatVmus, true);
            else
            {
                DeviceContext.OMSetRenderTargets(_renderTargetView, null);
                DeviceContext.ClearRenderTargetView(_renderTargetView, new Color4(0f, 0f, 0f, 1f));
                if (PvrRenderer.Backend is not null)
                    PvrRenderer.Backend.RenderLastFrame();
            }
            PvrImGuiDx11.RenderDrawData(PvrImGui.GetDrawData());
        }
        _frameRendered = true;
    }

    internal override void Resize()
    {
        if (_device is null)
            return;
        if (_swapChain is not null)
        {
            DeviceContext.OMSetRenderTargets((ID3D11RenderTargetView?)null, null);
            _renderTargetView?.Dispose();
            _renderTargetView = null;
            Result result = _swapChain.ResizeBuffers(0, 0, 0, Format.Unknown,
                SwapChainFlags.AllowModeSwitch | (_allowTearing ? SwapChainFlags.AllowTearing : 0));
            if (result == Vortice.DXGI.ResultCode.DeviceRemoved || result == Vortice.DXGI.ResultCode.DeviceReset)
            {
                HandleDeviceLost();
                return;
            }

            if (result.Failure)
            {
                Trace.TraceWarning($"ResizeBuffers failed: {result.Code:x}");
                return;
            }

            result = _swapChain.GetBuffer(0, out ID3D11Texture2D? backBuffer);
            if (result.Failure)
            {
                Trace.TraceWarning($"swapChain->GetBuffer() failed: {result.Code:x}");
                return;
            }

            using (backBuffer)
            {
                result = Device.CreateRenderTargetView(backBuffer!, null, out _renderTargetView);
                if (result.Failure)
                {
                    Trace.TraceWarning($"CreateRenderTargetView failed: {result.Code:x}");
                    return;
                }
                DeviceContext.OMSetRenderTargets(_renderTargetView, null);
            }

            if (_swapChain1 is not null)
            {
                SwapChainDescription1 description = _swapChain1.Description1;
                PvrRuntime.Display.Width = (int)description.Width;
                PvrRuntime.Display.Height = (int)description.Height;
                Trace.TraceInformation($"Swapchain resized: {description.Width} x {description.Height}");
            }
            else
            {
                SwapChainDescription description = _swapChain.Description;
                PvrRuntime.Display.Width = (int)description.BufferDescription.Width;
                PvrRuntime.Display.Height = (int)description.BufferDescription.Height;
                Trace.TraceInformation($"Swapchain resized: {description.BufferDescription.Width} x {description.BufferDescription.Height}");
            }

            PvrRuntime.Display.Width = Math.Max(PvrRuntime.Display.Width, 1);
            PvrRuntime.Display.Height = Math.Max(PvrRuntime.Display.Height, 1);
        }
    }

    private void HandleDeviceLost()
    {
        if (_device is not null)
        {
            Result result = Device.DeviceRemovedReason;
            Trace.TraceWarning($"Device removed reason: {result.Code:x}");
        }
        PvrRenderer.Term();
        Term();
        if (Init(true))
        {
            PvrRenderer.Init();
        }
        else
        {
            PvrRenderer.Backend = new PvrNoRenderer();
            PvrRenderer.Backend.Init();
        }
    }

    internal PvrDx11ShaderCompiler GetCompiler()
    {
        if (_compiler is null)
        {
            if (!_compilerLibrary.Load("d3dcompiler_47.dll"))
            {
                if (!_compilerLibrary.Load("d3dcompiler_46.dll"))
                {
                    Trace.TraceWarning("Neither d3dcompiler_47.dll or d3dcompiler_46.dll can be loaded");
                    return PvrDx11Native.D3DCompile;
                }
            }

            _compiler = _compilerLibrary.GetFunction<PvrDx11ShaderCompiler>("D3DCompile");
            if (_compiler is null)
                _compiler = PvrDx11Native.D3DCompile;
        }
        return _compiler;
    }

    private bool CheckTextureSupport()
    {
        Format[] formats =
        [
            Format.B5G5R5A1_UNorm, Format.B4G4R4A4_UNorm, Format.B5G6R5_UNorm, Format.B8G8R8A8_UNorm, Format.A8_UNorm
        ];
        string[] names = ["B5G5R5A1", "B4G4R4A4", "B5G6R5", "B8G8R8A8", "A8"];
        PvrTextureType[] types =
        [
            PvrTextureType._5551, PvrTextureType._4444, PvrTextureType._565, PvrTextureType._8888, PvrTextureType._8
        ];

        for (int i = 0; i < formats.Length; i++)
        {
            _supportedTextureFormats[(int)types[i]] = false;
            Device.CheckFormatSupport(formats[i], out FormatSupport support);
            const FormatSupport required = FormatSupport.Texture2D | FormatSupport.ShaderSample;
            if ((support & required) != required)
            {
                if (formats[i] == Format.B8G8R8A8_UNorm)
                {
                    Trace.TraceError($"Fatal: Format {names[i]} not supported");
                    return false;
                }
                Trace.TraceWarning($"Format {names[i]} not supported");
            }
            else
            {
                const FormatSupport generateMipmaps = FormatSupport.MipAutogen | FormatSupport.RenderTarget;
                if ((support & FormatSupport.Mip) == 0)
                    Trace.TraceWarning($"Format {names[i]} doesn't support mipmaps");
                else if ((support & generateMipmaps) != generateMipmaps)
                    Trace.TraceWarning($"Format {names[i]} doesn't support mipmap autogen");
                else
                    _supportedTextureFormats[(int)types[i]] = true;
            }
        }
        return true;
    }
}
