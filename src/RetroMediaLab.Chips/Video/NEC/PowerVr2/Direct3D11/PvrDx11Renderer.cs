using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;
using static RetroMediaLab.Chips.Video.NEC.PowerVr2.PowerVr2Ta;
using static RetroMediaLab.Chips.Video.NEC.PowerVr2.PvrRegisters;
using static RetroMediaLab.Chips.Video.NEC.PowerVr2.Direct3D11.PvrDx11DepthStencilStates.ModifierVolumeMode;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Direct3D11;

internal unsafe partial class PvrDx11Renderer
{
    private static readonly InputElementDescription[] MainLayout =
    [
        new("POSITION", 0, Format.R32G32B32_Float, (uint)Marshal.OffsetOf<TaVertex>(nameof(TaVertex.X)), 0, InputClassification.PerVertexData, 0),
        new("COLOR", 0, Format.B8G8R8A8_UNorm, (uint)Marshal.OffsetOf<TaVertex>(nameof(TaVertex.Col)), 0, InputClassification.PerVertexData, 0),
        new("COLOR", 1, Format.B8G8R8A8_UNorm, (uint)Marshal.OffsetOf<TaVertex>(nameof(TaVertex.Spc)), 0, InputClassification.PerVertexData, 0),
        new("TEXCOORD", 0, Format.R32G32_Float, (uint)Marshal.OffsetOf<TaVertex>(nameof(TaVertex.U)), 0, InputClassification.PerVertexData, 0),
        new("NORMAL", 0, Format.R32G32B32_Float, (uint)Marshal.OffsetOf<TaVertex>(nameof(TaVertex.Nx)), 0, InputClassification.PerVertexData, 0)
    ];

    private static readonly InputElementDescription[] ModVolLayout =
    [
        new("POSITION", 0, Format.R32G32B32_Float, (uint)Marshal.OffsetOf<ModTriangle>(nameof(ModTriangle.X0)), 0, InputClassification.PerVertexData, 0)
    ];

    internal override bool Init()
    {
        PvrRuntime.Log.Notice("DX11 renderer initializing");
        Device = PvrDx11Context.Instance.Device;
        DeviceContext = PvrDx11Context.Instance.DeviceContext;
        if (Device == null || DeviceContext == null)
        {
            PvrRuntime.Log.Warn("Null device or device context. Aborting");
            return false;
        }

        _shaders = PvrDx11Context.Instance.Shaders;
        Samplers = PvrDx11Context.Instance.Samplers;
        bool success = _shaders.GetVertexShader(true, true) != null;
        using (Blob? blob = _shaders.GetVertexShaderBlob())
            success = success && Device.CreateInputLayout(MainLayout, new ReadOnlySpan<byte>((void*)blob!.BufferPointer, (int)blob.BufferSize), out MainInputLayout).Success;
        using (Blob? blob = _shaders.GetMVVertexShaderBlob())
            success = success && Device.CreateInputLayout(ModVolLayout, new ReadOnlySpan<byte>((void*)blob!.BufferPointer, (int)blob.BufferSize), out ModVolInputLayout).Success;

        {
            BufferDescription desc = default;
            desc.ByteWidth = (uint)((((sizeof(VertexConstants) - 1) >> 4) + 1) << 4);
            desc.Usage = ResourceUsage.Dynamic;
            desc.BindFlags = BindFlags.ConstantBuffer;
            desc.CPUAccessFlags = CpuAccessFlags.Write;
            success = success && Device.CreateBuffer(desc, null, out _vtxConstants).Success;

            desc.ByteWidth = (uint)((((sizeof(PixelConstants) - 1) >> 4) + 1) << 4);
            success = success && Device.CreateBuffer(desc, null, out _pxlConstants).Success;

            desc.ByteWidth = (uint)((((sizeof(PixelPolyConstants) - 1) >> 4) + 1) << 4);
            success = success && Device.CreateBuffer(desc, null, out PxlPolyConstants).Success;
        }

        {
            RasterizerDescription desc = default;
            desc.FillMode = FillMode.Solid;
            desc.CullMode = CullMode.None;
            desc.FrontCounterClockwise = true;
            desc.ScissorEnable = true;
            desc.DepthClipEnable = false;
            Device.CreateRasterizerState(desc, out _rasterCullNone);
            desc.CullMode = CullMode.Front;
            Device.CreateRasterizerState(desc, out _rasterCullFront);
            desc.CullMode = CullMode.Back;
            Device.CreateRasterizerState(desc, out _rasterCullBack);
        }

        {
            Texture2DDescription desc = default;
            desc.Width = 32;
            desc.Height = 32;
            desc.ArraySize = 1;
            desc.SampleDescription.Count = 1;
            desc.Usage = ResourceUsage.Default;
            desc.BindFlags = BindFlags.ShaderResource;
            desc.Format = Format.B8G8R8A8_UNorm;
            desc.MipLevels = 1;
            Device.CreateTexture2D(desc, null, out _paletteTexture);

            ShaderResourceViewDescription viewDesc = default;
            viewDesc.Format = desc.Format;
            viewDesc.ViewDimension = ShaderResourceViewDimension.Texture2D;
            viewDesc.Texture2D.MipLevels = 1;
            Device.CreateShaderResourceView(_paletteTexture, viewDesc, out _paletteTextureView);
        }

        {
            Texture2DDescription desc = default;
            desc.Width = 128;
            desc.Height = 2;
            desc.ArraySize = 1;
            desc.SampleDescription.Count = 1;
            desc.Usage = ResourceUsage.Default;
            desc.BindFlags = BindFlags.ShaderResource;
            desc.Format = Format.A8_UNorm;
            desc.MipLevels = 1;
            Device.CreateTexture2D(desc, null, out _fogTexture);

            ShaderResourceViewDescription viewDesc = default;
            viewDesc.Format = desc.Format;
            viewDesc.ViewDimension = ShaderResourceViewDimension.Texture2D;
            viewDesc.Texture2D.MipLevels = 1;
            Device.CreateShaderResourceView(_fogTexture, viewDesc, out _fogTextureView);
        }

        {
            Texture2DDescription desc = default;
            desc.Width = 8;
            desc.Height = 8;
            desc.ArraySize = 1;
            desc.SampleDescription.Count = 1;
            desc.Usage = ResourceUsage.Default;
            desc.BindFlags = BindFlags.ShaderResource;
            desc.Format = Format.B8G8R8A8_UNorm;
            desc.MipLevels = 1;
            Device.CreateTexture2D(desc, null, out _whiteTexture);

            ShaderResourceViewDescription viewDesc = default;
            viewDesc.Format = desc.Format;
            viewDesc.ViewDimension = ShaderResourceViewDimension.Texture2D;
            viewDesc.Texture2D.MipLevels = 1;
            Device.CreateShaderResourceView(_whiteTexture, viewDesc, out _whiteTextureView);

            uint* texData = stackalloc uint[8 * 8];
            new Span<uint>(texData, 8 * 8).Fill(uint.MaxValue);
            DeviceContext.UpdateSubresource(_whiteTexture, 0, null, (nint)texData, 8 * sizeof(uint), 8 * sizeof(uint) * 8);
        }

        _quad = new PvrDx11Quad();
        _quad.Init(Device, DeviceContext, _shaders);
        N2Helper.Init(Device, DeviceContext);

        UpdateFogTable = true;

        if (!success)
        {
            PvrRuntime.Log.Warn("DirectX 11 renderer initialization failed");
            Term();
        }
        FrameRendered = false;

        return success;
    }

    internal override void Term()
    {
        try
        {
            PvrRuntime.Log.Notice("DX11 renderer terminating");
#if VIDEO_ROUTING
            PvrVideoRouting.TerminateDirectX();
#endif
            N2Helper.Term();
        }
        finally
        {
            Reset(ref _vtxConstants);
            Reset(ref _pxlConstants);
            Reset(ref FbTex);
            Reset(ref FbTextureView);
            Reset(ref FbRenderTarget);
            Reset(ref _fbScaledRenderTarget);
            Reset(ref _fbScaledTextureView);
            Reset(ref _fbScaledTexture);
            Reset(ref _quad);
            DeviceContext = null!;
            Device = null!;
            Reset(ref _vrStagingTexture);
            Reset(ref _vrStagingTextureSRV);
            Reset(ref _vrScaledTexture);
            Reset(ref _vrScaledRenderTarget);
            Reset(ref DepthTex);
            Reset(ref DepthTexView);
            Reset(ref MainInputLayout);
            Reset(ref ModVolInputLayout);
            Reset(ref PxlPolyConstants);
            Reset(ref VertexBuffer);
            Reset(ref IndexBuffer);
            Reset(ref ModvolBuffer);
            Reset(ref RttRenderTarget);
            Reset(ref _dcfbTexture);
            Reset(ref _dcfbTextureView);
            Reset(ref _paletteTexture);
            Reset(ref _paletteTextureView);
            Reset(ref _fogTexture);
            Reset(ref _fogTextureView);
            Reset(ref _rttTexture);
            Reset(ref _rttDepthTex);
            Reset(ref _rttDepthTexView);
            Reset(ref _whiteTexture);
            Reset(ref _whiteTextureView);
            Reset(ref _rasterCullNone);
            Reset(ref _rasterCullFront);
            Reset(ref _rasterCullBack);
            BlendStates.Term();
            DepthStencilStates.Term();
            _texCache.Dispose();
        }
    }

    protected void CreateDepthTexAndView(ref ID3D11Texture2D? texture, ref ID3D11DepthStencilView? view,
        int width, int height, Format format = Format.D24_UNorm_S8_UInt, BindFlags bindFlags = 0)
    {
        Reset(ref view);
        Reset(ref texture);
        Texture2DDescription desc = default;
        desc.Width = (uint)width;
        desc.Height = (uint)height;
        desc.MipLevels = 1;
        desc.ArraySize = 1;
        desc.Format = format;
        desc.SampleDescription.Count = 1;
        desc.Usage = ResourceUsage.Default;
        desc.BindFlags = BindFlags.DepthStencil | bindFlags;
        var result = Device.CreateTexture2D(desc, null, out texture);
        if (result.Failure)
            PvrRuntime.Log.Warn("Depth/stencil creation failed");

        DepthStencilViewDescription viewDesc = default;
        viewDesc.Format = format == Format.R32G8X24_Typeless ? Format.D32_Float_S8X24_UInt : Format.D24_UNorm_S8_UInt;
        viewDesc.ViewDimension = DepthStencilViewDimension.Texture2D;
        result = Device.CreateDepthStencilView(texture, viewDesc, out view);
        if (result.Failure)
            PvrRuntime.Log.Warn("Depth/stencil view creation failed");
    }

    protected void CreateTexAndRenderTarget(ref ID3D11Texture2D? texture,
        ref ID3D11RenderTargetView? renderTarget, int width, int height)
    {
        Reset(ref texture);
        Reset(ref renderTarget);
        Texture2DDescription desc = default;
        desc.Width = (uint)width;
        desc.Height = (uint)height;
        desc.ArraySize = 1;
        desc.SampleDescription.Count = 1;
        desc.Usage = ResourceUsage.Default;
        desc.BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget;
        desc.Format = Format.B8G8R8A8_UNorm;
        desc.MipLevels = 1;

        var result = Device.CreateTexture2D(desc, null, out texture);
        if (result.Failure)
        {
            PvrRuntime.Log.Warn("Framebuffer texture creation failed");
            return;
        }

        result = Device.CreateRenderTargetView(texture, null, out renderTarget);
        if (result.Failure)
        {
            PvrRuntime.Log.Warn("Framebuffer render target creation failed");
            return;
        }
        DeviceContext.ClearRenderTargetView(renderTarget, new Color4(0f, 0f, 0f, 0f));
    }

    protected virtual void Resize(int w, int h)
    {
        if (Width == (uint)w && Height == (uint)h)
            return;
        Width = (uint)w;
        Height = (uint)h;

        {
            Reset(ref FbTextureView);
            CreateTexAndRenderTarget(ref FbTex, ref FbRenderTarget, (int)Width, (int)Height);

            ShaderResourceViewDescription viewDesc = default;
            viewDesc.Format = Format.B8G8R8A8_UNorm;
            viewDesc.ViewDimension = ShaderResourceViewDimension.Texture2D;
            viewDesc.Texture2D.MipLevels = 1;
            Device.CreateShaderResourceView(FbTex, viewDesc, out FbTextureView);
        }

        CreateDepthTexAndView(ref DepthTex, ref DepthTexView, (int)Width, (int)Height);

        FrameRendered = false;
        FrameRenderedOnce = false;
    }

    protected bool EnsureBufferSize(ref ID3D11Buffer? buffer, BindFlags bind, ref uint currentSize, uint minSize)
    {
        if (minSize <= currentSize && buffer != null)
            return true;
        if (currentSize == 0)
            currentSize = minSize;
        else
            while (currentSize < minSize)
                currentSize *= 2;
        Reset(ref buffer);
        BufferDescription desc = default;
        desc.ByteWidth = currentSize;
        desc.Usage = ResourceUsage.Dynamic;
        desc.BindFlags = bind;
        desc.CPUAccessFlags = CpuAccessFlags.Write;
        return Device.CreateBuffer(desc, null, out buffer).Success;
    }

    internal override PvrBaseTextureCacheData? GetTexture(TSP tsp, TCW tcw, int area = 0)
    {
        PvrDx11Texture? texture = _texCache.GetTextureCacheData(tsp, tcw, area);
        if (texture.NeedsUpdate())
        {
            if (!texture.Update())
                texture = null;
        }
        else if (texture.IsCustomTextureAvailable())
        {
            _texCache.DeleteLater(texture.Texture);
            Reset(ref texture.Texture);
            texture.LoadCustomTexture();
        }
        return texture;
    }

    internal override void Process(TaContext context)
    {
        RendContext = context.Rend;
        if (ResetTextureCache)
        {
            _texCache.Clear();
            ResetTextureCache = false;
        }
        _texCache.Cleanup();
        if (!context.Rend.IsRtt && context.Rend.SwapInterval > 0)
            PvrDx11Context.Instance.SetSwapInterval(context.Rend.SwapInterval);
        PowerVr2Ta.Parse(context, true);
    }

    protected void ResetContextState()
    {
        DeviceContext.OMSetRenderTargetsAndUnorderedAccessViews(0, null, null, 0, 0, null, null);
        DeviceContext.PSSetShader(null);
        DeviceContext.GSSetShader(null);
        DeviceContext.HSSetShader(null);
        DeviceContext.DSSetShader(null);
        DeviceContext.CSSetShader(null);
        DeviceContext.VSSetShader(null);
        ID3D11ShaderResourceView?[] nullView = new ID3D11ShaderResourceView?[128];
        DeviceContext.CSSetShaderResources(0, nullView);
        DeviceContext.DSSetShaderResources(0, nullView);
        DeviceContext.GSSetShaderResources(0, nullView);
        DeviceContext.HSSetShaderResources(0, nullView);
        DeviceContext.PSSetShaderResources(0, nullView);
        DeviceContext.VSSetShaderResources(0, nullView);
        DeviceContext.SetPredication(null, false);
        DeviceContext.SOSetTargets(0, null, null);
    }

    protected void ConfigVertexShader()
    {
        Matrices.CalcMatrices(RendContext, (int)RendContext.FramebufferWidth, (int)RendContext.FramebufferHeight);
        SetBaseScissor();

        if (RendContext.IsRtt)
            PrepareRttRenderTarget(RendContext.FbWSof1 & PvrMemory.VramMask);
        else
            DeviceContext.RSSetViewport(new Viewport(0, 0, RendContext.FramebufferWidth, RendContext.FramebufferHeight, 0f, 1f));

        VertexConstants constants = default;
        Matrix4x4 matrix = Matrices.GetNormalMatrix();
        Buffer.MemoryCopy(&matrix, constants.TransMatrix, 16 * sizeof(float), 16 * sizeof(float));
        constants.LeftPlane[0] = 1;
        constants.LeftPlane[3] = 1;
        constants.RightPlane[0] = -1;
        constants.RightPlane[3] = 1;
        constants.TopPlane[1] = 1;
        constants.TopPlane[3] = 1;
        constants.BottomPlane[1] = -1;
        constants.BottomPlane[3] = 1;
        UploadMapped(_vtxConstants!, &constants, sizeof(VertexConstants));
        DeviceContext.VSSetConstantBuffer(0, _vtxConstants);
    }

    protected void UploadGeometryBuffers()
    {
        PvrSorter.SetFirstProvokingVertex(RendContext);
        uint size = (uint)(RendContext.Verts.Count * sizeof(TaVertex));
        bool result = EnsureBufferSize(ref VertexBuffer, BindFlags.VertexBuffer, ref _vertexBufferSize, size);
        PvrRuntime.Verify(result);
        fixed (TaVertex* vertices = CollectionsMarshal.AsSpan(RendContext.Verts))
            UploadMapped(VertexBuffer!, vertices, size);

        size = (uint)(RendContext.Idx.Count * sizeof(uint));
        result = EnsureBufferSize(ref IndexBuffer, BindFlags.IndexBuffer, ref _indexBufferSize, size);
        PvrRuntime.Verify(result);
        fixed (uint* indices = CollectionsMarshal.AsSpan(RendContext.Idx))
            UploadMapped(IndexBuffer!, indices, size);

        if (PvrConfiguration.ModifierVolumes && RendContext.ModTrig.Count != 0)
        {
            size = (uint)(RendContext.ModTrig.Count * sizeof(ModTriangle));
            result = EnsureBufferSize(ref ModvolBuffer, BindFlags.VertexBuffer, ref _modvolBufferSize, size);
            PvrRuntime.Verify(result);
            fixed (ModTriangle* modifiers = CollectionsMarshal.AsSpan(RendContext.ModTrig))
                UploadMapped(ModvolBuffer!, modifiers, size);
        }
        DeviceContext.IASetVertexBuffer(0, VertexBuffer, (uint)sizeof(TaVertex), 0);
        DeviceContext.IASetIndexBuffer(IndexBuffer, Format.R32_UInt, 0);
    }

    protected void SetupPixelShaderConstants()
    {
        Unsafe.SkipInit(out PixelConstants pixelConstants);
        FOG_COL_VERT.GetRGBColor(new Span<float>(pixelConstants.FogColVert, 4));
        FOG_COL_RAM.GetRGBColor(new Span<float>(pixelConstants.FogColRam, 4));
        pixelConstants.FogDensity = FOG_DENSITY.Get() * PvrConfiguration.ExtraDepthScale;
        pixelConstants.ShadowScale = FPU_SHAD_SCALE.scale_factor / 256f;
        RendContext.FogClampMin.GetRGBAColor(new Span<float>(pixelConstants.ColorClampMin, 4));
        RendContext.FogClampMax.GetRGBAColor(new Span<float>(pixelConstants.ColorClampMax, 4));
        pixelConstants.AlphaTestValue = (PT_ALPHA_REF & 0xff) / 255f;
        Dithering = PvrConfiguration.EmulateFramebuffer && RendContext.FbWCtrl.fb_dither != 0
            && RendContext.FbWCtrl.fb_packmode <= 3;
        if (Dithering)
        {
            switch (RendContext.FbWCtrl.fb_packmode)
            {
                case 0:
                case 3:
                    pixelConstants.DitherDivisor[0] = pixelConstants.DitherDivisor[1] = pixelConstants.DitherDivisor[2] = 2f;
                    break;
                case 1:
                    pixelConstants.DitherDivisor[0] = pixelConstants.DitherDivisor[2] = 2f;
                    pixelConstants.DitherDivisor[1] = 4f;
                    break;
                case 2:
                    pixelConstants.DitherDivisor[0] = pixelConstants.DitherDivisor[1] = pixelConstants.DitherDivisor[2] = 1f;
                    break;
                default:
                    break;
            }
            pixelConstants.DitherDivisor[3] = 1f;
        }
        UploadMapped(_pxlConstants!, &pixelConstants, sizeof(PixelConstants));
        DeviceContext.PSSetConstantBuffers(0, [_pxlConstants, PxlPolyConstants]);
    }

    internal override bool Render()
    {
        ResetContextState();
        bool isRtt = RendContext.IsRtt;
        if (!isRtt)
        {
            Resize((int)RendContext.FramebufferWidth, (int)RendContext.FramebufferHeight);
            DeviceContext.OMSetRenderTargets(FbRenderTarget, DepthTexView);
            DeviceContext.ClearDepthStencilView(DepthTexView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 0f, 0);
            if (RendContext.ClearFramebuffer)
            {
                Span<float> colors = stackalloc float[4];
                VO_BORDER_COL.GetRGBColor(colors);
                colors[3] = 1f;
                DeviceContext.ClearRenderTargetView(FbRenderTarget, new Color4(colors[0], colors[1], colors[2], colors[3]));
            }
        }

        ConfigVertexShader();
        DeviceContext.IASetInputLayout(MainInputLayout);
        N2Helper.ResetCache();
        UploadGeometryBuffers();
        UpdateFogTexture();
        UpdatePaletteTexture();
        SetupPixelShaderConstants();
        DrawStrips();

        if (isRtt)
            ReadRttRenderTarget(RendContext.FbWSof1 & PvrMemory.VramMask);
        else if (PvrConfiguration.EmulateFramebuffer)
            WriteFramebufferToVRAM();
        else
        {
            AspectRatio = PvrTransformMatrix.GetOutputFramebufferAspectRatio();
            DeviceContext.OMSetRenderTargets(PvrDx11Context.Instance.RenderTarget, null);
            DisplayFramebuffer();
            DrawOSD();
            RenderVideoRouting();
            PvrDx11Context.Instance.SetFrameRendered();
            FrameRendered = true;
            FrameRenderedOnce = true;
            ClearLastFrame = false;
        }
        return !isRtt;
    }

    protected void DisplayFramebuffer()
    {
        DeviceContext.RSSetViewport(new Viewport(0, 0, PvrRuntime.Display.Width, PvrRuntime.Display.Height, 0f, 1f));
        DeviceContext.RSSetScissorRect(new RawRect(0, 0, PvrRuntime.Display.Width, PvrRuntime.Display.Height));
        Span<float> colors = stackalloc float[4];
        VO_BORDER_COL.GetRGBColor(colors);
        colors[3] = 1f;
        DeviceContext.ClearRenderTargetView(PvrDx11Context.Instance.RenderTarget, new Color4(colors[0], colors[1], colors[2], colors[3]));

        PvrTransformMatrix.GetVideoShift(out float shiftX, out float shiftY);
        shiftX *= 2f / Width;
        shiftY *= -2f / Height;

        int outWidth = PvrRuntime.Display.Width;
        int outHeight = PvrRuntime.Display.Height;
        float renderAR = AspectRatio;
        if (PvrConfiguration.Rotate90)
        {
            (outWidth, outHeight) = (outHeight, outWidth);
            (shiftX, shiftY) = (shiftY, shiftX);
            renderAR = 1f / renderAR;
        }
        int dy = 0;
        int dx = 0;
        PvrTransformMatrix.GetWindowboxDimensions(outWidth, outHeight, renderAR, ref dx, ref dy, false);

        float x = dx;
        float y = dy;
        float w = outWidth - 2 * dx;
        float h = outHeight - 2 * dy;
        x = x * 2f / outWidth - 1f;
        w *= 2f / outWidth;
        y = y * 2f / outHeight - 1f;
        h *= 2f / outHeight;
        x += shiftX;
        y += shiftY;
        DeviceContext.OMSetBlendState(BlendStates.GetState(false, 0, 0, false, false), null, uint.MaxValue);
        _quad!.Draw(FbTextureView, Samplers.GetSampler(PvrConfiguration.LinearInterpolation), null, x, y, w, h, PvrConfiguration.Rotate90);
    }

    protected void SetCullMode(int mode)
    {
        ID3D11RasterizerState? rasterizer;
        switch (mode)
        {
            case 0:
            case 1:
            default:
                rasterizer = _rasterCullNone;
                break;
            case 2:
                rasterizer = _rasterCullFront;
                break;
            case 3:
                rasterizer = _rasterCullBack;
                break;
        }
        DeviceContext.RSSetState(rasterizer);
    }

    protected TileClipping SetTileClip(uint tileclip, ref PvrRect clipRect)
    {
        TileClipping clipMode = Matrices.GetTileClip(tileclip, ref clipRect);
        if (clipMode == TileClipping.Outside)
        {
            var rect = new RawRect(clipRect.Origin.X, clipRect.Origin.Y, clipRect.Origin.X + clipRect.Size.X, clipRect.Origin.Y + clipRect.Size.Y);
            DeviceContext.RSSetScissorRect(rect);
        }
        else
        {
            DeviceContext.RSSetScissorRect(ScissorRect);
        }
        return clipMode;
    }

    private void SetRenderState(in PolyParam polygon, uint type, bool sortingEnabled)
    {
        Unsafe.SkipInit(out PixelPolyConstants constants);
        if (polygon.Pcw.Texture != 0 && polygon.Tsp.FilterMode > 1
            && type != ListType_Punch_Through && polygon.Tcw.MipMapped == 1)
        {
            constants.TrilinearAlpha = 0.25f * (polygon.Tsp.MipMapD & 3);
            if (polygon.Tsp.FilterMode == 2)
                constants.TrilinearAlpha = 1f - constants.TrilinearAlpha;
        }
        else
            constants.TrilinearAlpha = 1f;

        bool colorClamp = polygon.Tsp.ColorClamp != 0
            && (RendContext.FogClampMin.full != 0 || RendContext.FogClampMax.full != uint.MaxValue);
        uint fogCtrl = PvrConfiguration.Fog ? polygon.Tsp.FogCtrl : 2u;
        PvrRect clipRect = default;
        TileClipping clipMode = SetTileClip(polygon.TileClip, ref clipRect);
        var texture = (PvrDx11Texture?)polygon.Texture;
        int gpuPalette = texture == null || !texture.GpuPalette ? 0 : (int)(polygon.Tsp.FilterMode + 1);
        if (gpuPalette != 0)
        {
            if (PvrConfiguration.TextureFiltering == 1)
                gpuPalette = 1;
            else if (PvrConfiguration.TextureFiltering == 2)
                gpuPalette = 2;
        }

        DeviceContext.VSSetShader(_shaders.GetVertexShader(polygon.Pcw.Gouraud != 0, polygon.IsNaomi2()));
        DeviceContext.PSSetShader(_shaders.GetShader(
            polygon.Pcw.Texture != 0,
            polygon.Tsp.UseAlpha != 0,
            polygon.Tsp.IgnoreTexA != 0 || polygon.Tcw.PixelFmt == (uint)PixelFormat.Pixel565,
            polygon.Tsp.ShadInstr,
            polygon.Pcw.Offset != 0,
            fogCtrl,
            polygon.Tcw.PixelFmt == (uint)PixelFormat.PixelBumpMap,
            colorClamp,
            constants.TrilinearAlpha != 1f,
            gpuPalette,
            polygon.Pcw.Gouraud != 0,
            type == ListType_Punch_Through,
            clipMode == TileClipping.Inside,
            Dithering));

        if (gpuPalette != 0)
        {
            if (polygon.Tcw.PixelFmt == (uint)PixelFormat.PixelPal4)
                constants.PaletteIndex = polygon.Tcw.PalSelect << 4;
            else
                constants.PaletteIndex = (polygon.Tcw.PalSelect >> 4) << 8;
        }

        if (clipMode == TileClipping.Inside)
        {
            constants.ClipTest[0] = clipRect.Origin.X;
            constants.ClipTest[1] = clipRect.Origin.Y;
            constants.ClipTest[2] = clipRect.BottomRight().X;
            constants.ClipTest[3] = clipRect.BottomRight().Y;
        }
        if (constants.TrilinearAlpha != 1f || gpuPalette != 0 || clipMode == TileClipping.Inside)
            UploadMapped(PxlPolyConstants!, &constants, sizeof(PixelPolyConstants));

        if (texture != null)
        {
            DeviceContext.PSSetShaderResource(0, texture.TextureView);
            bool linearFiltering;
            if (gpuPalette != 0)
                linearFiltering = false;
            else if (PvrConfiguration.TextureFiltering == 0)
                linearFiltering = polygon.Tsp.FilterMode != 0;
            else if (PvrConfiguration.TextureFiltering == 1)
                linearFiltering = false;
            else
                linearFiltering = true;
            ID3D11SamplerState? sampler = Samplers.GetSampler(linearFiltering, polygon.Tsp.ClampU != 0,
                polygon.Tsp.ClampV != 0, polygon.Tsp.FlipU != 0, polygon.Tsp.FlipV != 0, type == ListType_Punch_Through);
            DeviceContext.PSSetSampler(0, sampler);
        }
        DeviceContext.OMSetBlendState(BlendStates.GetState(true, (int)polygon.Tsp.SrcInstr, (int)polygon.Tsp.DstInstr), null, uint.MaxValue);
        SetCullMode((int)polygon.Isp.CullMode);
        int zfunc;
        if (type == ListType_Punch_Through || (type == ListType_Translucent && sortingEnabled))
            zfunc = 6;
        else
            zfunc = (int)polygon.Isp.DepthMode;

        bool zwriteEnable;
        if (sortingEnabled)
            zwriteEnable = false;
        else
        {
            if (type == ListType_Punch_Through)
                zwriteEnable = true;
            else
                zwriteEnable = polygon.Isp.ZWriteDis == 0;
        }
        uint stencil = polygon.Pcw.Shadow != 0 ? 0x80u : 0;
        DeviceContext.OMSetDepthStencilState(DepthStencilStates.GetState(true, zwriteEnable, zfunc, PvrConfiguration.ModifierVolumes), stencil);
        if (polygon.IsNaomi2())
            N2Helper.SetConstants(in polygon, 0, RendContext);
    }

    private void DrawList(IReadOnlyList<PolyParam> polygons, int first, int count, uint type, bool sortingEnabled)
    {
        if (count == 0)
            return;
        DeviceContext.IASetPrimitiveTopology(PrimitiveTopology.TriangleStrip);
        int index = first;
        while (count-- > 0)
        {
            PolyParam polygon = polygons[index];
            if (polygon.Count > 2)
            {
                if ((type == ListType_Opaque || (type == ListType_Translucent && !sortingEnabled)) && polygon.Isp.DepthMode == 0)
                {
                    index++;
                    continue;
                }
                SetRenderState(in polygon, type, sortingEnabled);
                DeviceContext.DrawIndexed(polygon.Count, polygon.First, 0);
            }
            index++;
        }
    }

    private void DrawSorted(int first, int count, bool multipass)
    {
        if (count == 0)
            return;
        DeviceContext.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        int end = first + count;
        for (int p = first; p < end; p++)
        {
            SortedTriangle triangle = RendContext.SortedTriangles[p];
            PolyParam polygon = RendContext.GlobalParamTr[(int)triangle.PolyIndex];
            SetRenderState(in polygon, ListType_Translucent, true);
            DeviceContext.DrawIndexed(triangle.Count, triangle.First, 0);
        }
        if (multipass && PvrConfiguration.TranslucentPolygonDepthMask)
        {
            DeviceContext.OMSetBlendState(BlendStates.GetState(false, 0, 0, true), null, uint.MaxValue);
            DeviceContext.VSSetShader(_shaders.GetVertexShader(true, PvrConfiguration.IsNaomi2));
            DeviceContext.PSSetShader(_shaders.GetShader(
                false, false, false, 0, false, 2, false, false, false, 0, true, false, false, false));
            DeviceContext.OMSetDepthStencilState(DepthStencilStates.GetState(true, true, 6, false), 0);
            DeviceContext.RSSetScissorRect(ScissorRect);

            for (int p = first; p < end; p++)
            {
                SortedTriangle triangle = RendContext.SortedTriangles[p];
                PolyParam polygon = RendContext.GlobalParamTr[(int)triangle.PolyIndex];
                if (polygon.Isp.ZWriteDis == 0)
                {
                    SetCullMode((int)polygon.Isp.CullMode);
                    DeviceContext.DrawIndexed(triangle.Count, triangle.First, 0);
                }
            }
        }
    }

    private void DrawModVols(int first, int count)
    {
        if (count == 0 || RendContext.ModTrig.Count == 0 || !PvrConfiguration.ModifierVolumes)
            return;
        DeviceContext.IASetInputLayout(ModVolInputLayout);
        DeviceContext.IASetVertexBuffer(0, ModvolBuffer, 3 * sizeof(float), 0);
        DeviceContext.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        DeviceContext.OMSetBlendState(BlendStates.GetState(false, 0, 0, true), null, uint.MaxValue);
        DeviceContext.PSSetShader(_shaders.GetModVolShader());
        SetCullMode(0);

        int modBase = -1;
        int curMVMat = -1;
        int curProjMat = -1;
        for (int cmv = 0; cmv < count; cmv++)
        {
            ModifierVolumeParam param = RendContext.GlobalParamMvo[first + cmv];
            uint mvMode = param.Isp.DepthMode;
            if (modBase == -1)
                modBase = (int)param.First;

            if (param.IsNaomi2() && (param.MvMatrix != curMVMat || param.ProjMatrix != curProjMat))
            {
                curMVMat = param.MvMatrix;
                curProjMat = param.ProjMatrix;
                Span<N2Matrix> matrices = CollectionsMarshal.AsSpan(RendContext.Matrices);
                fixed (float* mvMatrix = matrices[param.MvMatrix].Mat)
                fixed (float* projMatrix = matrices[param.ProjMatrix].Mat)
                    N2Helper.SetConstants(mvMatrix, projMatrix);
            }
            DeviceContext.VSSetShader(_shaders.GetMVVertexShader(param.IsNaomi2()));
            if (param.Isp.VolumeLast == 0 && mvMode > 0)
                DeviceContext.OMSetDepthStencilState(DepthStencilStates.GetMVState(Or), 2);
            else
                DeviceContext.OMSetDepthStencilState(DepthStencilStates.GetMVState(Xor), 0);

            PvrRect clipRect = default;
            SetTileClip(param.TileClip, ref clipRect);
            if (param.Count > 0)
            {
                SetCullMode((int)param.Isp.CullMode);
                DeviceContext.Draw(param.Count * 3, param.First * 3);
            }

            if (mvMode == 1 || mvMode == 2)
            {
                DeviceContext.OMSetDepthStencilState(DepthStencilStates.GetMVState(mvMode == 1 ? Inclusion : Exclusion), 1);
                DeviceContext.Draw((param.First + param.Count - (uint)modBase) * 3, (uint)modBase * 3);
                modBase = -1;
            }
        }
        SetCullMode(0);
        DeviceContext.OMSetBlendState(BlendStates.GetState(true, 4, 5), null, uint.MaxValue);
        DeviceContext.RSSetScissorRect(ScissorRect);
        DeviceContext.OMSetDepthStencilState(DepthStencilStates.GetMVState(Final), 0x81);
        DeviceContext.IASetInputLayout(MainInputLayout);
        DeviceContext.IASetVertexBuffer(0, VertexBuffer, (uint)sizeof(TaVertex), 0);
        DeviceContext.IASetIndexBuffer(IndexBuffer, Format.R32_UInt, 0);
        DeviceContext.IASetPrimitiveTopology(PrimitiveTopology.TriangleStrip);
        DeviceContext.VSSetShader(_shaders.GetMVVertexShader(false));
        DeviceContext.DrawIndexed(4, 0, 0);
    }

    private void DrawStrips()
    {
        RenderPass previousPass = default;
        for (int renderPass = 0; renderPass < RendContext.RenderPasses.Count; renderPass++)
        {
            RenderPass currentPass = RendContext.RenderPasses[renderPass];
            uint opCount = currentPass.OpCount - previousPass.OpCount;
            uint ptCount = currentPass.PtCount - previousPass.PtCount;
            uint trCount = currentPass.TrCount - previousPass.TrCount;
            uint mvoCount = currentPass.MvoCount - previousPass.MvoCount;
            PvrRuntime.Log.Debug("Render pass {0} OP {1} PT {2} TR {3} MV {4} autosort {5}",
                renderPass + 1, opCount, ptCount, trCount, mvoCount, currentPass.AutoSort);

            DrawList(RendContext.GlobalParamOp, (int)previousPass.OpCount, (int)opCount, ListType_Opaque, false);
            DrawList(RendContext.GlobalParamPt, (int)previousPass.PtCount, (int)ptCount, ListType_Punch_Through, false);
            DrawModVols((int)previousPass.MvoCount, (int)mvoCount);
            if (currentPass.AutoSort)
            {
                if (!PvrConfiguration.PerStripSorting)
                    DrawSorted((int)previousPass.SortedTrCount, (int)(currentPass.SortedTrCount - previousPass.SortedTrCount),
                        renderPass < RendContext.RenderPasses.Count - 1);
                else
                    DrawList(RendContext.GlobalParamTr, (int)previousPass.TrCount, (int)trCount, ListType_Translucent, true);
            }
            else
            {
                DrawList(RendContext.GlobalParamTr, (int)previousPass.TrCount, (int)trCount, ListType_Translucent, false);
            }
            previousPass = currentPass;
        }
    }

    internal override bool RenderLastFrame()
    {
        if (!FrameRenderedOnce || ClearLastFrame)
            return false;
        DisplayFramebuffer();
        return true;
    }

    internal override void RenderFramebuffer(in PvrFramebufferInfo info)
    {
        RendContext = null!;
        using var pixels = new PvrPixelBuffer<uint>();
        int width;
        int height;
        if (info.ReadControl.fb_enable == 0 || info.VideoControl.blank_video == 1)
        {
            width = height = 1;
            pixels.Init((uint)width, (uint)height, false);
            byte* p = (byte*)pixels.Data(0, 0);
            p[0] = (byte)info.VideoBorderColor._blue;
            p[1] = (byte)info.VideoBorderColor._green;
            p[2] = (byte)info.VideoBorderColor._red;
            p[3] = 255;
        }
        else
        {
            PvrTextureCache.ReadFramebuffer<PvrBgraPacker>(in info, pixels, out width, out height);
        }

        if (_dcfbTexture != null)
        {
            Texture2DDescription desc = _dcfbTexture.Description;
            if ((int)desc.Width != width || (int)desc.Height != height)
            {
                Reset(ref _dcfbTexture);
                Reset(ref _dcfbTextureView);
            }
        }
        if (_dcfbTexture == null)
        {
            Texture2DDescription desc = default;
            desc.Width = (uint)width;
            desc.Height = (uint)height;
            desc.ArraySize = 1;
            desc.SampleDescription.Count = 1;
            desc.Usage = ResourceUsage.Default;
            desc.BindFlags = BindFlags.ShaderResource;
            desc.Format = Format.B8G8R8A8_UNorm;
            desc.MipLevels = 1;

            var result = Device.CreateTexture2D(desc, null, out _dcfbTexture);
            if (result.Failure)
                PvrRuntime.Log.Warn("DC Framebuffer texture creation failed");
            ShaderResourceViewDescription viewDesc = default;
            viewDesc.Format = desc.Format;
            viewDesc.ViewDimension = ShaderResourceViewDimension.Texture2D;
            viewDesc.Texture2D.MipLevels = 1;
            result = Device.CreateShaderResourceView(_dcfbTexture, viewDesc, out _dcfbTextureView);
            if (result.Failure)
                PvrRuntime.Log.Warn("DC Framebuffer texture view creation failed");
        }
        DeviceContext.UpdateSubresource(_dcfbTexture, 0, null, (nint)pixels.Data(), (uint)(width * sizeof(uint)), (uint)(width * sizeof(uint) * height));
        DeviceContext.PSSetShaderResource(0, null);
        Resize(width, height);
        DeviceContext.OMSetRenderTargets(FbRenderTarget, null);
        Span<float> colors = stackalloc float[4];
        info.VideoBorderColor.GetRGBColor(colors);
        colors[3] = 1f;
        DeviceContext.ClearRenderTargetView(FbRenderTarget, new Color4(colors[0], colors[1], colors[2], colors[3]));
        DeviceContext.RSSetViewport(new Viewport(0, 0, Width, Height, 0f, 1f));
        DeviceContext.RSSetScissorRect(new RawRect(0, 0, (int)Width, (int)Height));
        DeviceContext.OMSetBlendState(BlendStates.GetState(false), null, uint.MaxValue);
        DeviceContext.GSSetShader(null);
        DeviceContext.HSSetShader(null);
        DeviceContext.DSSetShader(null);
        DeviceContext.CSSetShader(null);
        _quad!.Draw(_dcfbTextureView, Samplers.GetSampler(true));

        AspectRatio = PvrTransformMatrix.GetDcFramebufferAspectRatio();
        DeviceContext.OMSetRenderTargets(PvrDx11Context.Instance.RenderTarget, null);
        DisplayFramebuffer();
        DrawOSD();
        RenderVideoRouting();
        PvrDx11Context.Instance.SetFrameRendered();
        FrameRendered = true;
        FrameRenderedOnce = true;
        ClearLastFrame = false;
    }

    private void SetBaseScissor()
    {
        PvrRect scissor = Matrices.GetBaseScissor();
        ScissorRect = new RawRect(scissor.Origin.X, scissor.Origin.Y, scissor.Origin.X + scissor.Size.X, scissor.Origin.Y + scissor.Size.Y);
        if (RendContext.IsRtt)
            _scissorEnable = true;
        else
        {
            if (scissor.Origin.X != 0 || scissor.Origin.Y != 0
                || scissor.Size.X != (int)RendContext.FramebufferWidth || scissor.Size.Y != (int)RendContext.FramebufferHeight)
                _scissorEnable = true;
            else
                _scissorEnable = false;
        }
        DeviceContext.RSSetScissorRect(ScissorRect);
    }

    private void PrepareRttRenderTarget(uint texAddress)
    {
        uint fbw = RendContext.FramebufferWidth;
        uint fbh = RendContext.FramebufferHeight;
        PvrRuntime.Log.Debug("RTT packmode={0} stride={1} - {2} x {3} @ {4:x6}",
            RendContext.FbWCtrl.fb_packmode, RendContext.FbWLineStride * 8, fbw, fbh, texAddress);

        CreateTexAndRenderTarget(ref _rttTexture, ref RttRenderTarget, (int)fbw, (int)fbh);
        CreateDepthTexAndView(ref _rttDepthTex, ref _rttDepthTexView, (int)fbw, (int)fbh);
        DeviceContext.ClearDepthStencilView(_rttDepthTexView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 0f, 0);
        DeviceContext.OMSetRenderTargets(RttRenderTarget, _rttDepthTexView);
        DeviceContext.RSSetViewport(new Viewport(0, 0, fbw, fbh, 0f, 1f));
        SetRTTSize((int)fbw, (int)fbh);
    }

    protected void ReadRttRenderTarget(uint texAddress)
    {
        uint w = RendContext.FramebufferWidth;
        uint h = RendContext.FramebufferHeight;
        if (PvrConfiguration.RenderToTextureBuffer)
        {
            Texture2DDescription desc = _rttTexture!.Description;
            desc.Usage = ResourceUsage.Staging;
            desc.BindFlags = 0;
            desc.Format = Format.B8G8R8A8_UNorm;
            desc.CPUAccessFlags = CpuAccessFlags.Read;

            var result = Device.CreateTexture2D(desc, null, out ID3D11Texture2D? stagingTex);
            using (stagingTex)
            {
                if (result.Failure)
                {
                    PvrRuntime.Log.Warn("Staging RTT texture creation failed");
                    return;
                }
                DeviceContext.CopyResource(stagingTex, _rttTexture);

                using var pixels = new PvrPixelBuffer<uint>();
                pixels.Init(w, h);
                byte* p = (byte*)pixels.Data();
                result = DeviceContext.Map(stagingTex, 0, MapMode.Read, MapFlags.None, out MappedSubresource mapped);
                if (result.Failure)
                {
                    PvrRuntime.Log.Warn("Failed to map staging RTT texture");
                    return;
                }
                try
                {
                    uint pitch = w * sizeof(uint);
                    if (pitch == mapped.RowPitch)
                        Buffer.MemoryCopy((void*)mapped.DataPointer, p, pitch * h, pitch * h);
                    else
                    {
                        byte* src = (byte*)mapped.DataPointer;
                        for (uint y = 0; y < h; y++)
                        {
                            Buffer.MemoryCopy(src, p, pitch, pitch);
                            p += pitch;
                            src += mapped.RowPitch;
                        }
                    }
                }
                finally
                {
                    DeviceContext.Unmap(stagingTex, 0);
                }
                ushort* dst = (ushort*)(PvrMemory.VramPointer + texAddress);
                PvrTextureCache.WriteTextureToVram(w, h, (byte*)pixels.Data(), dst, RendContext.FbWCtrl,
                    RendContext.FbWLineStride * 8, RendContext.FbClip, 2, 1, 0, 3);
            }
        }
        else
        {
            PvrTransformMatrix.GetPvrFramebufferSize(RendContext, out int wpo2, out int hpo2);
            if (wpo2 <= 1024 && hpo2 <= 1024)
            {
                PvrDx11Texture texture = _texCache.GetRenderToTexture(texAddress, RendContext.FbWCtrl.fb_packmode, (uint)wpo2, (uint)hpo2);
                Reset(ref texture.Texture);
                texture.Texture = _rttTexture;
                _rttTexture = null;
                Reset(ref RttRenderTarget);
                Reset(ref texture.TextureView);
                ShaderResourceViewDescription viewDesc = default;
                viewDesc.Format = Format.B8G8R8A8_UNorm;
                viewDesc.ViewDimension = ShaderResourceViewDimension.Texture2D;
                viewDesc.Texture2D.MipLevels = 1;
                Device.CreateShaderResourceView(texture.Texture, viewDesc, out texture.TextureView);

                texture.Dirty = 0;
                texture.UnprotectVram();
            }
        }
    }

    protected void UpdatePaletteTexture()
    {
        if (UpdatePalette)
        {
            UpdatePalette = false;
            fixed (uint* palette = PvrTextureConversion.Palette32Ram)
                DeviceContext.UpdateSubresource(_paletteTexture, 0, null, (nint)palette, 32 * sizeof(uint), 32 * sizeof(uint) * 32);
        }
        DeviceContext.PSSetShaderResource(1, _paletteTextureView);
        DeviceContext.PSSetSampler(1, Samplers.GetSampler(false));
    }

    protected void UpdateFogTexture()
    {
        if (!PvrConfiguration.Fog)
            return;
        if (UpdateFogTable)
        {
            UpdateFogTable = false;
            byte* buffer = stackalloc byte[256];
            PvrTextureCache.MakeFogTexture(buffer);
            DeviceContext.UpdateSubresource(_fogTexture, 0, null, (nint)buffer, 128, 128 * 2);
        }
        DeviceContext.PSSetShaderResource(2, _fogTextureView);
        DeviceContext.PSSetSampler(2, Samplers.GetSampler(true));
    }

    protected void DrawOSD()
    {
        PvrDx11Context.Instance.SetOverlay(true);
        try
        {
            PvrRuntime.Gui.DisplayOsd();
        }
        finally
        {
            PvrDx11Context.Instance.SetOverlay(false);
        }
    }

    protected void WriteFramebufferToVRAM()
    {
        uint width = (uint)RendContext.GlobClip.X;
        uint height = (uint)RendContext.GlobClip.Y;
        PvrTransformMatrix.GetWriteFramebufferToVramParams(RendContext, out PvrInt2 scaledSize, out PvrRect finalClip);
        ID3D11Texture2D? fbTexture = FbTex;

        if (scaledSize.X != (int)width || scaledSize.Y != (int)height)
        {
            uint scaledW = (uint)scaledSize.X;
            uint scaledH = (uint)scaledSize.Y;
            if (_fbScaledTexture != null)
            {
                Texture2DDescription scaledDesc = _fbScaledTexture.Description;
                if (scaledDesc.Width != scaledW || scaledDesc.Height != scaledH)
                {
                    Reset(ref _fbScaledTexture);
                    Reset(ref _fbScaledTextureView);
                    Reset(ref _fbScaledRenderTarget);
                }
            }
            if (_fbScaledTexture == null)
            {
                CreateTexAndRenderTarget(ref _fbScaledTexture, ref _fbScaledRenderTarget, (int)scaledW, (int)scaledH);
                ShaderResourceViewDescription viewDesc = default;
                viewDesc.Format = Format.B8G8R8A8_UNorm;
                viewDesc.ViewDimension = ShaderResourceViewDimension.Texture2D;
                viewDesc.Texture2D.MipLevels = 1;
                Device.CreateShaderResourceView(_fbScaledTexture, viewDesc, out _fbScaledTextureView);
            }

            DeviceContext.OMSetRenderTargets(_fbScaledRenderTarget, null);
            DeviceContext.RSSetViewport(new Viewport(0, 0, scaledW, scaledH, 0f, 1f));
            DeviceContext.OMSetBlendState(BlendStates.GetState(false), null, uint.MaxValue);
            _quad!.Draw(FbTextureView, Samplers.GetSampler(true));

            width = scaledW;
            height = scaledH;
            fbTexture = _fbScaledTexture;
        }
        uint texAddress = RendContext.FbWSof1 & PvrMemory.VramMask;
        uint lineStride = RendContext.FbWLineStride * 8;

        Texture2DDescription desc = fbTexture!.Description;
        desc.Usage = ResourceUsage.Staging;
        desc.BindFlags = 0;
        desc.Format = Format.B8G8R8A8_UNorm;
        desc.CPUAccessFlags = CpuAccessFlags.Read;

        var result = Device.CreateTexture2D(desc, null, out ID3D11Texture2D? stagingTex);
        using (stagingTex)
        {
            if (result.Failure)
            {
                PvrRuntime.Log.Warn("Staging RTT texture creation failed");
                return;
            }
            DeviceContext.CopyResource(stagingTex, fbTexture);

            using var pixels = new PvrPixelBuffer<uint>();
            pixels.Init(width, height);
            byte* p = (byte*)pixels.Data();
            result = DeviceContext.Map(stagingTex, 0, MapMode.Read, MapFlags.None, out MappedSubresource mapped);
            if (result.Failure)
            {
                PvrRuntime.Log.Warn("Failed to map staging RTT texture");
                return;
            }
            try
            {
                uint pitch = width * sizeof(uint);
                if (pitch == mapped.RowPitch)
                    Buffer.MemoryCopy((void*)mapped.DataPointer, p, pitch * height, pitch * height);
                else
                {
                    byte* src = (byte*)mapped.DataPointer;
                    for (uint y = 0; y < height; y++)
                    {
                        Buffer.MemoryCopy(src, p, pitch, pitch);
                        p += pitch;
                        src += mapped.RowPitch;
                    }
                }
            }
            finally
            {
                DeviceContext.Unmap(stagingTex, 0);
            }

            PvrTextureCache.WriteFramebuffer(width, height, (byte*)pixels.Data(), texAddress, RendContext.FbWCtrl,
                lineStride, finalClip, 2, 1, 0, 3);
        }
    }

    internal override bool GetLastFrame(List<byte> data, ref int width, ref int height)
    {
        if (!FrameRenderedOnce)
            return false;
        if (width != 0)
            height = (int)(width / AspectRatio);
        else if (height != 0)
            width = (int)(AspectRatio * height);
        else
        {
            width = (int)Width;
            height = (int)Height;
            if (PvrConfiguration.Rotate90)
                (width, height) = (height, width);
            int w = (int)(AspectRatio * height);
            if (width > w)
                height = (int)(width / AspectRatio);
            else
                width = w;
        }

        ID3D11Texture2D? dstTex = null;
        ID3D11RenderTargetView? dstRenderTarget = null;
        try
        {
            CreateTexAndRenderTarget(ref dstTex, ref dstRenderTarget, width, height);
            DeviceContext.PSSetShaderResource(0, null);
            DeviceContext.OMSetRenderTargets(dstRenderTarget, null);
            DeviceContext.RSSetViewport(new Viewport(0, 0, width, height, 0f, 1f));
            DeviceContext.RSSetScissorRect(new RawRect(0, 0, width, height));
            DeviceContext.OMSetBlendState(BlendStates.GetState(false), null, uint.MaxValue);
            DeviceContext.GSSetShader(null);
            DeviceContext.HSSetShader(null);
            DeviceContext.DSSetShader(null);
            DeviceContext.CSSetShader(null);
            _quad!.Draw(FbTextureView, Samplers.GetSampler(true), null, -1f, -1f, 2f, 2f, PvrConfiguration.Rotate90);

            DeviceContext.OMSetRenderTargets(PvrDx11Context.Instance.RenderTarget, null);
            Texture2DDescription desc = dstTex!.Description;
            desc.Usage = ResourceUsage.Staging;
            desc.BindFlags = 0;
            desc.Format = Format.B8G8R8A8_UNorm;
            desc.CPUAccessFlags = CpuAccessFlags.Read;

            var result = Device.CreateTexture2D(desc, null, out ID3D11Texture2D? stagingTex);
            using (stagingTex)
            {
                if (result.Failure)
                {
                    PvrRuntime.Log.Warn("Staging screenshot texture creation failed");
                    return false;
                }
                DeviceContext.CopyResource(stagingTex, dstTex);

                result = DeviceContext.Map(stagingTex, 0, MapMode.Read, MapFlags.None, out MappedSubresource mapped);
                if (result.Failure)
                {
                    PvrRuntime.Log.Warn("Failed to map staging screenshot texture");
                    return false;
                }
                try
                {
                    byte* src = (byte*)mapped.DataPointer;
                    for (int y = 0; y < height; y++)
                    {
                        byte* p = src + y * mapped.RowPitch;
                        for (int x = 0; x < width; x++, p += 4)
                        {
                            data.Add(p[2]);
                            data.Add(p[1]);
                            data.Add(p[0]);
                        }
                    }
                }
                finally
                {
                    DeviceContext.Unmap(stagingTex, 0);
                }

                return true;
            }
        }
        finally
        {
            Reset(ref dstRenderTarget);
            Reset(ref dstTex);
        }
    }

    protected void RenderVideoRouting()
    {
#if VIDEO_ROUTING
        if (PvrConfiguration.VideoRouting)
        {
            ID3D11RenderTargetView renderTarget = PvrDx11Context.Instance.RenderTarget!;
            using ID3D11Resource resource = renderTarget.Resource;
            using ID3D11Texture2D backBufferTexture = resource.QueryInterface<ID3D11Texture2D>();

            if (PvrConfiguration.VideoRoutingScale)
            {
                Texture2DDescription backBufferDesc = backBufferTexture.Description;
                Texture2DDescription vrsDesc = _vrStagingTexture?.Description ?? default;
                if (_vrStagingTexture == null || backBufferDesc.Width != vrsDesc.Width || backBufferDesc.Height != vrsDesc.Height)
                {
                    Reset(ref _vrStagingTexture);
                    Reset(ref _vrStagingTextureSRV);

                    Texture2DDescription srvDesc = backBufferDesc;
                    srvDesc.BindFlags = BindFlags.ShaderResource;
                    srvDesc.Usage = ResourceUsage.Default;
                    Device.CreateTexture2D(srvDesc, null, out _vrStagingTexture);

                    ShaderResourceViewDescription viewDesc = default;
                    viewDesc.Format = Format.B8G8R8A8_UNorm;
                    viewDesc.ViewDimension = ShaderResourceViewDimension.Texture2D;
                    viewDesc.Texture2D.MipLevels = 1;
                    Device.CreateShaderResourceView(_vrStagingTexture, viewDesc, out _vrStagingTextureSRV);
                }

                Texture2DDescription vrscDesc = _vrScaledTexture?.Description ?? default;
                int targetWidth = PvrConfiguration.VideoRoutingVRes * PvrRuntime.Display.Width / PvrRuntime.Display.Height;
                if (_vrScaledTexture == null || (int)vrscDesc.Height != PvrConfiguration.VideoRoutingVRes)
                {
                    Reset(ref _vrScaledTexture);
                    Reset(ref _vrScaledRenderTarget);
                    CreateTexAndRenderTarget(ref _vrScaledTexture, ref _vrScaledRenderTarget,
                        targetWidth, PvrConfiguration.VideoRoutingVRes);
                }
                var viewport = new Viewport(0, 0, targetWidth, PvrConfiguration.VideoRoutingVRes, 0f, 1f);

                DeviceContext.OMSetRenderTargets(_vrScaledRenderTarget, null);
                DeviceContext.RSSetViewport(viewport);
                DeviceContext.CopyResource(_vrStagingTexture, backBufferTexture);
                DeviceContext.OMSetBlendState(BlendStates.GetState(false), null, uint.MaxValue);
                _quad!.Draw(_vrStagingTextureSRV, Samplers.GetSampler(true));
                PvrVideoRouting.PublishFrameTexture(_vrScaledTexture);
                DeviceContext.OMSetRenderTargets(PvrDx11Context.Instance.RenderTarget, null);
            }
            else
            {
                PvrVideoRouting.PublishFrameTexture(backBufferTexture);
            }
        }
        else
        {
            PvrVideoRouting.TerminateDirectX();
        }
#endif
    }

    internal static PvrRenderBackend Create() => new PvrDx11Renderer();

    private void UploadMapped(ID3D11Buffer buffer, void* source, long size)
    {
        MappedSubresource mapped = DeviceContext.Map(buffer, 0, MapMode.WriteDiscard, MapFlags.None);
        try
        {
            Buffer.MemoryCopy(source, (void*)mapped.DataPointer, size, size);
        }
        finally
        {
            DeviceContext.Unmap(buffer, 0);
        }
    }

    private static void Reset<T>(ref T? resource) where T : class, IDisposable
    {
        resource?.Dispose();
        resource = null;
    }
}
