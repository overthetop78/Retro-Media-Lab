using System;
using System.Runtime.InteropServices;
using Vortice.Direct3D9;
using Vortice.Mathematics;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Direct3D9;

internal sealed partial class PvrDx9Context
{
    private const uint D3dSdkVersion = 32;
    private const int DeviceLost = unchecked((int)0x88760868u);
    private const int DeviceNotReset = unchecked((int)0x88760869u);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate nint Direct3DCreate9(uint sdkVersion);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetClientRect(nint window, out RawRect rectangle);

    private PvrDx9Context(nint window, nint display) : base(window, display)
    {
        bool initialized = false;
        try
        {
            _d3d9Library.Load("D3D9.DLL");
            initialized = Init();
            if (!initialized)
                throw new PvrRendererException("DX9 initialization failed");
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
                _d3d9Library.Dispose();
            }
            finally
            {
                base.Dispose();
            }
        }
    }

    private bool Init(bool keepCurrentWindow = false)
    {
        PvrRuntime.Log.Notice("DX9 Context initializing");
#if USE_SDL
        if (!keepCurrentWindow && !PvrPlatform.RecreateWindow(0))
        {
            Term();
            return false;
        }
#endif

        Direct3DCreate9? create = _d3d9Library.GetFunction<Direct3DCreate9>("Direct3DCreate9");
        if (create == null)
        {
            PvrRuntime.Log.Error("Cannot load D3D9.DLL");
            Term();
            return false;
        }

        _d3d?.Dispose();
        nint pointer = create(D3dSdkVersion);
        _d3d = pointer == 0 ? null : new IDirect3D9(pointer);
        if (_d3d == null)
        {
            PvrRuntime.Log.Error("Direct3DCreate9 failed");
            Term();
            return false;
        }
        _presentParameters = default;
        _presentParameters.DeviceWindowHandle = Window;
        _presentParameters.Windowed = true;
        _presentParameters.SwapEffect = SwapEffect.Discard;
        _presentParameters.BackBufferFormat = Format.Unknown;
        _presentParameters.EnableAutoDepthStencil = false;
        _swapOnVSync = !PvrConfiguration.FastForwardMode && PvrConfiguration.VSync;
        if (_swapOnVSync)
            _presentParameters.PresentationInterval = PresentInterval.One;
        else
            _presentParameters.PresentationInterval = PresentInterval.Immediate;

        var result = _d3d.CreateDevice(0, DeviceType.Hardware, Window,
            CreateFlags.HardwareVertexProcessing, ref _presentParameters, out _device);
        if (result.Failure)
        {
            PvrRuntime.Log.Error("DirectX9 device creation failed: {0:x}", result.Code);
            Term();
            return false;
        }
        ImGuiDriver = new PvrDx9Driver(_device!);
        _overlay.Init(_device!);

        _d3d.GetAdapterIdentifier(0, 0, out AdapterIdentifier adapter);
        _driverName = adapter.Description;
        long version = adapter.DriverVersion;
        int high = (int)(version >> 32);
        uint low = (uint)version;
        _driverVersion = FormattableString.Invariant($"{high >> 16}.{(ushort)high}.{low >> 16}.{(ushort)low}");
        _amd = adapter.VendorId == 0x1002 || adapter.VendorId == 0x1022;
        _deviceReady = true;
        return true;
    }

    private void Term()
    {
        PvrRuntime.Log.Notice("DX9 Context terminating");
        try
        {
            _overlay.Term();
        }
        finally
        {
            try
            {
                ImGuiDriver?.Dispose();
                ImGuiDriver = null;
            }
            finally
            {
                try
                {
                    _device?.Dispose();
                    _device = null;
                }
                finally
                {
                    _d3d?.Dispose();
                    _d3d = null;
                    _deviceReady = false;
                }
            }
        }
    }

    internal void Present()
    {
        if (!_frameRendered)
            return;
        if (_device == null)
        {
            if (Init(true))
            {
                PvrRenderer.Backend = new PvrDx9Renderer();
                PvrRenderer.Init();
            }
            return;
        }

        var result = _device.Present(null, null, 0, 0);
        if (result.Code == DeviceLost)
        {
            _deviceReady = false;
            result = _device.TestCooperativeLevel();
            if (result.Code == DeviceNotReset)
                ResetDevice();
        }
        else if (result.Failure)
            PvrRuntime.Log.Warn("Present failed {0:x}", result.Code);
        else
        {
            _frameRendered = false;
            if (_swapOnVSync != (!PvrConfiguration.FastForwardMode && PvrConfiguration.VSync))
            {
                PvrRuntime.Log.Debug("Switch vsync {0}", !_swapOnVSync);
                PvrRenderer.Term();
                Term();
                if (Init(true))
                {
                    PvrRenderer.Backend = new PvrDx9Renderer();
                    PvrRenderer.Init();
                }
                else
                    _deviceReady = false;
            }
        }
    }

    internal void EndImGuiFrame()
    {
        if (_deviceReady)
        {
            PvrRuntime.Verify(_device != null);
            _device!.SetRenderState(RenderState.ZEnable, 0);
            _device.SetRenderState(RenderState.AlphaBlendEnable, 0);
            _device.SetRenderState(RenderState.ScissorTestEnable, 0);
            if (!_overlayOnly)
            {
                _device.Clear(0, null, ClearFlags.Target, 0xff000000u, 1f, 0);
                PvrRenderer.Backend?.RenderLastFrame();
            }
            if (_device.BeginScene().Success)
            {
                try
                {
                    if (_overlayOnly)
                        _overlay.Draw((uint)PvrRuntime.Display.Width, (uint)PvrRuntime.Display.Height, PvrConfiguration.FloatVmus, true);
                    PvrImGuiDx9.RenderDrawData(PvrImGui.GetDrawData());
                }
                finally
                {
                    _device.EndScene();
                }
            }
        }
        _frameRendered = true;
    }

    internal override void Resize()
    {
        if (_device == null)
            return;
        GetClientRect(Window, out RawRect rect);
        _presentParameters.BackBufferWidth = (uint)(PvrRuntime.Display.Width = rect.Right);
        _presentParameters.BackBufferHeight = (uint)(PvrRuntime.Display.Height = rect.Bottom);
        if (PvrRuntime.Display.Width == 0 || PvrRuntime.Display.Height == 0)
        {
            PvrRuntime.Display.Width = Math.Max(PvrRuntime.Display.Width, 1);
            PvrRuntime.Display.Height = Math.Max(PvrRuntime.Display.Height, 1);
            return;
        }
        ResetDevice();
    }

    private void ResetDevice()
    {
        var renderer = PvrRenderer.Backend as PvrDx9Renderer;
        renderer?.PreReset();
        _overlay.Term();
        PvrImGuiDx9.InvalidateDeviceObjects();
        var result = _device!.Reset(ref _presentParameters);
        if (result.Failure)
        {
            PvrRuntime.Log.Error("DX9 device reset failed: {0:x}", result.Code);
            _deviceReady = false;
            return;
        }
        _deviceReady = true;
        PvrImGuiDx9.CreateDeviceObjects();
        _overlay.Init(_device);
        renderer?.PostReset();
    }
}
