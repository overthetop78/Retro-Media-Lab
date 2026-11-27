using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;
using static RetroMediaLab.Chips.Video.NEC.PowerVr2.PowerVr2Ta;
using static RetroMediaLab.Chips.Video.NEC.PowerVr2.PvrRegisters;
using static RetroMediaLab.Chips.Video.NEC.PowerVr2.Direct3D11.PvrDx11DepthStencilStates.ModifierVolumeMode;
using Pass = RetroMediaLab.Chips.Video.NEC.PowerVr2.Direct3D11.PvrDx11OitShaders.Pass;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Direct3D11;

internal sealed unsafe class PvrDx11OitRenderer : PvrDx11Renderer
{
    private static readonly InputElementDescription[] OitMainLayout =
    [
        new("POSITION", 0, Format.R32G32B32_Float, (uint)Marshal.OffsetOf<TaVertex>(nameof(TaVertex.X)), 0, InputClassification.PerVertexData, 0),
        new("COLOR", 0, Format.B8G8R8A8_UNorm, (uint)Marshal.OffsetOf<TaVertex>(nameof(TaVertex.Col)), 0, InputClassification.PerVertexData, 0),
        new("COLOR", 1, Format.B8G8R8A8_UNorm, (uint)Marshal.OffsetOf<TaVertex>(nameof(TaVertex.Spc)), 0, InputClassification.PerVertexData, 0),
        new("TEXCOORD", 0, Format.R32G32_Float, (uint)Marshal.OffsetOf<TaVertex>(nameof(TaVertex.U)), 0, InputClassification.PerVertexData, 0),
        new("COLOR", 2, Format.B8G8R8A8_UNorm, (uint)Marshal.OffsetOf<TaVertex>(nameof(TaVertex.Col1)), 0, InputClassification.PerVertexData, 0),
        new("COLOR", 3, Format.B8G8R8A8_UNorm, (uint)Marshal.OffsetOf<TaVertex>(nameof(TaVertex.Spc1)), 0, InputClassification.PerVertexData, 0),
        new("TEXCOORD", 1, Format.R32G32_Float, (uint)Marshal.OffsetOf<TaVertex>(nameof(TaVertex.U1)), 0, InputClassification.PerVertexData, 0),
        new("NORMAL", 0, Format.R32G32B32_Float, (uint)Marshal.OffsetOf<TaVertex>(nameof(TaVertex.Nx)), 0, InputClassification.PerVertexData, 0)
    ];

    private struct OitPixelPolyConstants
    {
        internal fixed float ClipTest[4];
        internal fixed int BlendMode0[2];
        internal fixed int BlendMode1[2];
        internal float PaletteIndex;
        internal float TrilinearAlpha;
        internal int ShadingInstr0;
        internal int ShadingInstr1;
        internal int FogControl0;
        internal int FogControl1;
        internal int UseAlpha0;
        internal int UseAlpha1;
        internal int IgnoreTexAlpha0;
        internal int IgnoreTexAlpha1;
    }

    internal override bool Init()
    {
        if (!base.Init())
            return false;
        Reset(ref PxlPolyConstants);
        BufferDescription desc = default;
        desc.Usage = ResourceUsage.Dynamic;
        desc.BindFlags = BindFlags.ConstantBuffer;
        desc.CPUAccessFlags = CpuAccessFlags.Write;
        desc.ByteWidth = (uint)((((sizeof(OitPixelPolyConstants) - 1) >> 4) + 1) << 4);
        bool success = Device.CreateBuffer(desc, null, out PxlPolyConstants).Success;

        _shaders.Init(Device, PvrDx11Context.Instance.GetCompiler());
        _buffers.Init(Device, DeviceContext);
        _pixelBufferSize = PvrConfiguration.PixelBufferSize;
        using (Blob blob = _shaders.GetVertexShaderBlob()!)
        {
            Reset(ref _mainInputLayout);
            success = Device.CreateInputLayout(OitMainLayout, new ReadOnlySpan<byte>((void*)blob.BufferPointer, (int)blob.BufferSize), out _mainInputLayout).Success && success;
        }
        using (Blob blob = _shaders.GetFinalVertexShaderBlob()!)
            success = Device.CreateInputLayout([], new ReadOnlySpan<byte>((void*)blob.BufferPointer, (int)blob.BufferSize), out _finalInputLayout).Success && success;

        desc.ByteWidth = (uint)((((sizeof(int) - 1) >> 4) + 1) << 4);
        desc.Usage = ResourceUsage.Dynamic;
        desc.BindFlags = BindFlags.ConstantBuffer;
        desc.CPUAccessFlags = CpuAccessFlags.Write;
        success = Device.CreateBuffer(desc, null, out _vtxPolyConstants).Success && success;
        return success;
    }

    private void ResizeInternal(uint width, uint height)
    {
        if (width > _maxWidth || height > _maxHeight || _opaqueTex == null)
        {
            _maxWidth = Math.Max(_maxWidth, width);
            _maxHeight = Math.Max(_maxHeight, height);
            _buffers.Resize((int)_maxWidth, (int)_maxHeight);

            CreateTexAndRenderTarget(ref _opaqueTex, ref _opaqueRenderTarget, (int)_maxWidth, (int)_maxHeight);
            Reset(ref _multipassTex);
            Reset(ref _multipassRenderTarget);
            Reset(ref _multipassTextureView);
            Reset(ref _opaqueTextureView);
            ShaderResourceViewDescription viewDesc = default;
            viewDesc.Format = Format.B8G8R8A8_UNorm;
            viewDesc.ViewDimension = ShaderResourceViewDimension.Texture2D;
            viewDesc.Texture2D.MipLevels = 1;
            Device.CreateShaderResourceView(_opaqueTex, viewDesc, out _opaqueTextureView);

            CreateDepthTexAndView(ref _depthStencilTex2, ref _depthStencilView2, (int)_maxWidth, (int)_maxHeight, Format.R32G8X24_Typeless, BindFlags.ShaderResource);
            Reset(ref _stencilView);
            viewDesc.Format = Format.X32_Typeless_G8X24_UInt;
            Device.CreateShaderResourceView(_depthStencilTex2, viewDesc, out _stencilView);

            Reset(ref _depthView);
            viewDesc.Format = Format.R32_Float_X8X24_Typeless;
            Device.CreateShaderResourceView(_depthStencilTex2, viewDesc, out _depthView);

            CreateDepthTexAndView(ref DepthTex, ref DepthTexView, (int)_maxWidth, (int)_maxHeight, Format.R32G8X24_Typeless);
        }
        else if (DepthTex == null)
            CreateDepthTexAndView(ref DepthTex, ref DepthTexView, (int)_maxWidth, (int)_maxHeight, Format.R32G8X24_Typeless);
    }

    protected override void Resize(int width, int height)
    {
        base.Resize(width, height);
        Reset(ref DepthTexView);
        Reset(ref DepthTex);
        ResizeInternal((uint)width, (uint)height);
    }

    protected override void SetRTTSize(int width, int height)
    {
        ResizeInternal((uint)width, (uint)height);
    }

    internal override void Term()
    {
        try
        {
            Reset(ref _vtxPolyConstants);
            Reset(ref _finalInputLayout);
            Reset(ref _mainInputLayout);
            Reset(ref _multipassTextureView);
            Reset(ref _multipassRenderTarget);
            Reset(ref _multipassTex);
            Reset(ref _opaqueTextureView);
            Reset(ref _opaqueRenderTarget);
            Reset(ref _opaqueTex);
            _shaders.Term();
        }
        finally
        {
            try
            {
                _buffers.Term();
                _maxWidth = _maxHeight = 0;
                base.Term();
            }
            finally
            {
                Reset(ref _stencilView);
                Reset(ref _depthView);
                Reset(ref _depthStencilTex2);
                Reset(ref _depthStencilView2);
                Reset(ref _trPolyParamsBuffer);
                Reset(ref _trPolyParamsBufferView);
            }
        }
    }

    [SkipLocalsInit]
    private void SetRenderState(in PolyParam polygon, int polyNumber, uint type, bool sortingEnabled, Pass pass)
    {
        ID3D11VertexShader? vertexShader = _shaders.GetVertexShader(polygon.Pcw.Gouraud != 0, polygon.IsNaomi2(), false, pass != Pass.Depth);
        DeviceContext.VSSetShader(vertexShader);

        Unsafe.SkipInit(out OitPixelPolyConstants constants);
        if (polygon.Pcw.Texture != 0 && polygon.Tsp.FilterMode > 1 && type != ListType_Punch_Through && polygon.Tcw.MipMapped == 1)
        {
            constants.TrilinearAlpha = 0.25f * (polygon.Tsp.MipMapD & 3);
            if (polygon.Tsp.FilterMode == 2)
                constants.TrilinearAlpha = 1f - constants.TrilinearAlpha;
        }
        else
            constants.TrilinearAlpha = 1f;

        PvrRect clipRect = default;
        TileClipping clipMode = SetTileClip(polygon.TileClip, ref clipRect);
        int gpuPalette = polygon.Texture == null || !polygon.Texture.GpuPalette ? 0 : (int)polygon.Tsp.FilterMode + 1;
        if (gpuPalette != 0)
        {
            if (PvrConfiguration.TextureFiltering == 1)
                gpuPalette = 1;
            else if (PvrConfiguration.TextureFiltering == 2)
                gpuPalette = 2;
        }

        bool twoVolumes = polygon.Tsp1.full != uint.MaxValue && type != ListType_Translucent;
        bool useTexture;
        ID3D11PixelShader? pixelShader;
        if (pass == Pass.Depth)
        {
            useTexture = type == ListType_Punch_Through && polygon.Pcw.Texture != 0;
            pixelShader = _shaders.GetShader(useTexture, polygon.Tsp.UseAlpha != 0,
                polygon.Tsp.IgnoreTexA != 0 || polygon.Tcw.PixelFmt == (uint)PixelFormat.Pixel565,
                0, false, 2, false, false, gpuPalette, polygon.Pcw.Gouraud != 0, useTexture,
                clipMode == TileClipping.Inside, twoVolumes, pass);
        }
        else
        {
            bool colorClamp = polygon.Tsp.ColorClamp != 0
                && (RendContext.FogClampMin.full != 0 || RendContext.FogClampMax.full != uint.MaxValue);
            uint fogControl = PvrConfiguration.Fog ? polygon.Tsp.FogCtrl : 2u;
            useTexture = polygon.Pcw.Texture != 0;
            pixelShader = _shaders.GetShader(useTexture, polygon.Tsp.UseAlpha != 0,
                polygon.Tsp.IgnoreTexA != 0 || polygon.Tcw.PixelFmt == (uint)PixelFormat.Pixel565,
                polygon.Tsp.ShadInstr, polygon.Pcw.Offset != 0, fogControl,
                polygon.Tcw.PixelFmt == (uint)PixelFormat.PixelBumpMap, colorClamp, gpuPalette,
                polygon.Pcw.Gouraud != 0, type == ListType_Punch_Through, clipMode == TileClipping.Inside, twoVolumes, pass);
        }
        DeviceContext.PSSetShader(pixelShader);
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
        constants.BlendMode0[0] = (int)polygon.Tsp.SrcInstr;
        constants.BlendMode0[1] = (int)polygon.Tsp.DstInstr;
        if (twoVolumes)
        {
            constants.BlendMode1[0] = (int)polygon.Tsp1.SrcInstr;
            constants.BlendMode1[1] = (int)polygon.Tsp1.DstInstr;
            constants.ShadingInstr0 = (int)polygon.Tsp.ShadInstr;
            constants.ShadingInstr1 = (int)polygon.Tsp1.ShadInstr;
            constants.FogControl0 = (int)polygon.Tsp.FogCtrl;
            constants.FogControl1 = (int)polygon.Tsp1.FogCtrl;
            constants.UseAlpha0 = (int)polygon.Tsp.UseAlpha;
            constants.UseAlpha1 = (int)polygon.Tsp1.UseAlpha;
            constants.IgnoreTexAlpha0 = (int)polygon.Tsp.IgnoreTexA;
            constants.IgnoreTexAlpha1 = (int)polygon.Tsp1.IgnoreTexA;
        }
        UploadMapped(PxlPolyConstants!, &constants, sizeof(OitPixelPolyConstants));
        if (!polygon.IsNaomi2())
        {
            UploadMapped(_vtxPolyConstants!, &polyNumber, sizeof(int));
            DeviceContext.VSSetConstantBuffer(1, _vtxPolyConstants);
        }

        if (pass == Pass.Color)
            DeviceContext.OMSetBlendState(BlendStates.GetState(true, (int)polygon.Tsp.SrcInstr, (int)polygon.Tsp.DstInstr), null, uint.MaxValue);

        if (useTexture)
        {
            for (int i = 0; i < 2; i++)
            {
                var texture = (PvrDx11Texture?)(i == 0 ? polygon.Texture : polygon.Texture1);
                if (texture == null)
                    continue;
                uint slot = i == 0 ? 0u : 3u;
                DeviceContext.PSSetShaderResource(slot, texture.TextureView);
                TSP tsp = i == 0 ? polygon.Tsp : polygon.Tsp1;
                bool linear;
                if (gpuPalette != 0)
                    linear = false;
                else if (PvrConfiguration.TextureFiltering == 0)
                    linear = tsp.FilterMode != 0;
                else if (PvrConfiguration.TextureFiltering == 1)
                    linear = false;
                else
                    linear = true;
                DeviceContext.PSSetSampler(slot, Samplers.GetSampler(linear, tsp.ClampU != 0, tsp.ClampV != 0,
                    tsp.FlipU != 0, tsp.FlipV != 0, type == ListType_Punch_Through));
            }
        }

        SetCullMode((int)polygon.Isp.CullMode);
        int zFunc = type == ListType_Punch_Through || sortingEnabled ? 6 : (int)polygon.Isp.DepthMode;
        bool zWriteEnable = type == ListType_Punch_Through || polygon.Isp.ZWriteDis == 0;
        bool needStencil = PvrConfiguration.ModifierVolumes && pass == Pass.Depth && type != ListType_Translucent;
        uint stencil = polygon.Pcw.Shadow != 0 ? 0x80u : 0u;
        DeviceContext.OMSetDepthStencilState(DepthStencilStates.GetState(true, zWriteEnable, zFunc, needStencil), stencil);

        if (polygon.IsNaomi2())
            N2Helper.SetConstants(in polygon, unchecked((uint)polyNumber), RendContext);
    }

    private void DrawList(IReadOnlyList<PolyParam> polygons, uint first, uint count, uint type, bool sortingEnabled, Pass pass)
    {
        if (count == 0)
            return;
        DeviceContext.IASetPrimitiveTopology(PrimitiveTopology.TriangleStrip);
        uint firstVertexIndex = type == ListType_Translucent ? RendContext.Idx[(int)polygons[0].First] : 0;
        int index = (int)first;
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
                SetRenderState(in polygon, unchecked((index << 17) - (int)firstVertexIndex), type, sortingEnabled, pass);
                DeviceContext.DrawIndexed(polygon.Count, polygon.First, 0);
            }
            index++;
        }
    }

    private void DrawModVols(uint first, uint count, IReadOnlyList<ModifierVolumeParam> parameters, bool transparent)
    {
        if (count == 0 || RendContext.ModTrig.Count == 0 || !PvrConfiguration.ModifierVolumes)
            return;
        DeviceContext.IASetInputLayout(ModVolInputLayout);
        DeviceContext.IASetVertexBuffer(0, ModvolBuffer, 3 * sizeof(float), 0);
        DeviceContext.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        if (!transparent)
            DeviceContext.PSSetShader(_shaders.GetModVolShader());
        DeviceContext.RSSetScissorRect(ScissorRect);

        int modBase = -1;
        int curMvMat = -1;
        int curProjMat = -1;
        for (uint cmv = 0; cmv < count; cmv++)
        {
            ModifierVolumeParam param = parameters[(int)(first + cmv)];
            uint mvMode = param.Isp.DepthMode;
            if (modBase == -1)
                modBase = (int)param.First;
            if (param.Count > 0)
            {
                if (param.IsNaomi2() && (param.MvMatrix != curMvMat || param.ProjMatrix != curProjMat))
                {
                    curMvMat = param.MvMatrix;
                    curProjMat = param.ProjMatrix;
                    N2Matrix mv = RendContext.Matrices[param.MvMatrix];
                    N2Matrix proj = RendContext.Matrices[param.ProjMatrix];
                    N2Helper.SetConstants(mv.Mat, proj.Mat);
                }
                DeviceContext.VSSetShader(_shaders.GetMVVertexShader(param.IsNaomi2()));
                if (transparent)
                {
                    if (param.Isp.VolumeLast == 0 && mvMode > 0)
                        DeviceContext.PSSetShader(_shaders.GetTrModVolShader((int)Or));
                    else
                        DeviceContext.PSSetShader(_shaders.GetTrModVolShader((int)Xor));
                }
                else
                {
                    if (param.Isp.VolumeLast == 0 && mvMode > 0)
                        DeviceContext.OMSetDepthStencilState(DepthStencilStates.GetMVState(Or), 2);
                    else
                        DeviceContext.OMSetDepthStencilState(DepthStencilStates.GetMVState(Xor), 0);
                }
                SetCullMode((int)param.Isp.CullMode);
                DeviceContext.Draw(param.Count * 3, param.First * 3);
            }

            if (mvMode == 1 || mvMode == 2)
            {
                if (transparent)
                    DeviceContext.PSSetShader(_shaders.GetTrModVolShader((int)(mvMode == 1 ? Inclusion : Exclusion)));
                else
                    DeviceContext.OMSetDepthStencilState(DepthStencilStates.GetMVState(mvMode == 1 ? Inclusion : Exclusion), 1);
                DeviceContext.Draw((param.First + param.Count - (uint)modBase) * 3, (uint)modBase * 3);
                modBase = -1;
            }
        }
        DeviceContext.IASetInputLayout(_mainInputLayout);
        DeviceContext.IASetVertexBuffer(0, VertexBuffer, (uint)sizeof(TaVertex), 0);
        DeviceContext.IASetIndexBuffer(IndexBuffer, Format.R32_UInt, 0);
    }

    private void RenderABuffer(bool lastPass)
    {
        if (!lastPass)
            SetTargets(_multipassRenderTarget, null);
        else if (RendContext.IsRtt)
            SetTargets(RttRenderTarget, null);
        else
            SetTargets(FbRenderTarget, null);
        DeviceContext.OMSetBlendState(BlendStates.GetState(false), null, uint.MaxValue);
        DeviceContext.PSSetShaderResource(0, _opaqueTextureView);
        DeviceContext.PSSetSampler(0, Samplers.GetSampler(false));
        RawRect rect = RendContext.IsRtt
            ? new RawRect(0, 0, (int)RendContext.FramebufferWidth, (int)RendContext.FramebufferHeight)
            : new RawRect(0, 0, (int)Width, (int)Height);
        DeviceContext.RSSetScissorRect(rect);
        DeviceContext.OMSetDepthStencilState(DepthStencilStates.GetState(false, false, 0, false), 0);
        SetCullMode(0);

        DeviceContext.IASetInputLayout(_finalInputLayout);
        DeviceContext.VSSetShader(_shaders.GetFinalVertexShader());
        DeviceContext.PSSetShader(_shaders.GetFinalShader(Dithering && lastPass));
        DeviceContext.IASetPrimitiveTopology(PrimitiveTopology.TriangleStrip);
        DeviceContext.Draw(4, 0);
    }

    private void DrawStrips()
    {
        {
            var trPolyParams = new uint[RendContext.GlobalParamTr.Count * 2];
            int i = 0;
            foreach (PolyParam polygon in RendContext.GlobalParamTr)
            {
                trPolyParams[i++] = (polygon.Tsp.full & 0xffff00c0) | ((polygon.Isp.full >> 16) & 0xe400) | ((polygon.Pcw.full >> 7) & 1);
                trPolyParams[i++] = polygon.Tsp1.full;
            }
            uint newSize = (uint)(trPolyParams.Length * sizeof(uint));
            if (newSize > 0)
            {
                if (_trPolyParamsBuffer == null || _trPolyParamsBufferSize < newSize)
                {
                    Reset(ref _trPolyParamsBufferView);
                    Reset(ref _trPolyParamsBuffer);
                    BufferDescription desc = default;
                    desc.ByteWidth = newSize;
                    desc.Usage = ResourceUsage.Dynamic;
                    desc.BindFlags = BindFlags.ShaderResource;
                    desc.MiscFlags = ResourceOptionFlags.BufferStructured;
                    desc.StructureByteStride = sizeof(uint) * 2;
                    desc.CPUAccessFlags = CpuAccessFlags.Write;

                    var result = Device.CreateBuffer(desc, null, out _trPolyParamsBuffer);
                    if (result.Failure)
                        PvrRuntime.Log.Warn("TR poly params buffer creation failed");
                    else
                    {
                        _trPolyParamsBufferSize = newSize;
                        ShaderResourceViewDescription viewDesc = default;
                        viewDesc.ViewDimension = ShaderResourceViewDimension.Buffer;
                        viewDesc.Format = Format.Unknown;
                        viewDesc.Buffer.NumElements = desc.ByteWidth / desc.StructureByteStride;
                        result = Device.CreateShaderResourceView(_trPolyParamsBuffer, viewDesc, out _trPolyParamsBufferView);
                        if (result.Failure)
                            PvrRuntime.Log.Warn("TR poly params buffer view creation failed");
                    }
                }
                fixed (uint* values = trPolyParams)
                    UploadMapped(_trPolyParamsBuffer!, values, newSize);
                DeviceContext.PSSetShaderResource(5, _trPolyParamsBufferView);
            }
        }

        _buffers.Bind();
        DeviceContext.ClearDepthStencilView(DepthTexView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 0f, 0);
        DeviceContext.ClearDepthStencilView(_depthStencilView2, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 0f, 0);
        if (!RendContext.IsRtt)
        {
            if (RendContext.ClearFramebuffer)
            {
                float* colors = stackalloc float[4];
                VO_BORDER_COL.GetRGBColor(colors);
                colors[3] = 1f;
                DeviceContext.ClearRenderTargetView(_opaqueRenderTarget, new Color4(colors[0], colors[1], colors[2], colors[3]));
            }
            else
            {
                (_opaqueTex, FbTex) = (FbTex, _opaqueTex);
                (_opaqueRenderTarget, FbRenderTarget) = (FbRenderTarget, _opaqueRenderTarget);
                (_opaqueTextureView, FbTextureView) = (FbTextureView, _opaqueTextureView);
            }
        }

        RenderPass previous = default;
        int renderPassCount = RendContext.RenderPasses.Count;
        for (int renderPass = 0; renderPass < renderPassCount; renderPass++)
        {
            RenderPass current = RendContext.RenderPasses[renderPass];
            uint opCount = current.OpCount - previous.OpCount;
            uint ptCount = current.PtCount - previous.PtCount;
            uint trCount = current.TrCount - previous.TrCount;
            uint mvoCount = current.MvoCount - previous.MvoCount;
            PvrRuntime.Log.Debug("Render pass {0} OP {1} PT {2} TR {3} MV {4} Tr MV {5} autosort {6}",
                renderPass + 1, opCount, ptCount, trCount, mvoCount,
                current.MvOpTrShared ? mvoCount : current.MvoTrCount - previous.MvoTrCount, current.AutoSort);

            DeviceContext.PSSetShaderResource(4, null);
            DeviceContext.OMSetBlendState(BlendStates.GetState(false, 0, 0, true), null, uint.MaxValue);
            SetTargets(_opaqueRenderTarget, _depthStencilView2);
            DrawList(RendContext.GlobalParamOp, previous.OpCount, opCount, ListType_Opaque, false, Pass.Depth);
            DrawList(RendContext.GlobalParamPt, previous.PtCount, ptCount, ListType_Punch_Through, false, Pass.Depth);
            DrawModVols(previous.MvoCount, mvoCount, RendContext.GlobalParamMvo, false);

            SetTargets(_opaqueRenderTarget, DepthTexView);
            DeviceContext.PSSetShaderResource(4, _stencilView);
            DrawList(RendContext.GlobalParamOp, previous.OpCount, opCount, ListType_Opaque, false, Pass.Color);
            DrawList(RendContext.GlobalParamPt, previous.PtCount, ptCount, ListType_Punch_Through, false, Pass.Color);
            DeviceContext.PSSetShaderResource(4, null);

            if (current.AutoSort)
            {
                SetTargets(_opaqueRenderTarget, DepthTexView);
                DeviceContext.PSSetShaderResource(4, _depthView);
                DeviceContext.OMSetBlendState(BlendStates.GetState(false, 0, 0, true), null, uint.MaxValue);
                DrawList(RendContext.GlobalParamTr, previous.TrCount, trCount, ListType_Translucent, true, Pass.Oit);
                DeviceContext.PSSetShaderResource(4, null);
                if (!PvrDx11Context.Instance.IsIntel)
                {
                    if (current.MvOpTrShared)
                        DrawModVols(previous.MvoCount, mvoCount, RendContext.GlobalParamMvo, true);
                    else
                        DrawModVols(previous.MvoTrCount, current.MvoTrCount - previous.MvoTrCount, RendContext.GlobalParamMvoTr, true);
                }
            }
            else
            {
                SetTargets(_opaqueRenderTarget, DepthTexView);
                DrawList(RendContext.GlobalParamTr, previous.TrCount, trCount, ListType_Translucent, false, Pass.Color);
            }

            if (renderPass < renderPassCount - 1)
            {
                SetTargets(_opaqueRenderTarget, _depthStencilView2);
                if (current.AutoSort)
                    DrawList(RendContext.GlobalParamTr, previous.TrCount, trCount, ListType_Translucent, true, Pass.Depth);
                else
                    DrawList(RendContext.GlobalParamTr, previous.TrCount, trCount, ListType_Translucent, false, Pass.Depth);
                if (_multipassTex == null)
                {
                    CreateTexAndRenderTarget(ref _multipassTex, ref _multipassRenderTarget, (int)Width, (int)Height);
                    Reset(ref _multipassTextureView);
                    ShaderResourceViewDescription viewDesc = default;
                    viewDesc.Format = Format.B8G8R8A8_UNorm;
                    viewDesc.ViewDimension = ShaderResourceViewDimension.Texture2D;
                    viewDesc.Texture2D.MipLevels = 1;
                    Device.CreateShaderResourceView(_multipassTex, viewDesc, out _multipassTextureView);
                }

                RenderABuffer(false);
                (_opaqueTex, _multipassTex) = (_multipassTex, _opaqueTex);
                (_opaqueRenderTarget, _multipassRenderTarget) = (_multipassRenderTarget, _opaqueRenderTarget);
                (_opaqueTextureView, _multipassTextureView) = (_multipassTextureView, _opaqueTextureView);
                DeviceContext.PSSetShaderResource(0, null);
                DeviceContext.IASetInputLayout(_mainInputLayout);
                DeviceContext.ClearDepthStencilView(_depthStencilView2, DepthStencilClearFlags.Stencil, 0f, 0);
            }
            previous = current;
        }
        RenderABuffer(true);
    }

    internal override bool Render()
    {
        ResetContextState();
        bool isRtt = RendContext.IsRtt;
        if (!isRtt)
            Resize((int)RendContext.FramebufferWidth, (int)RendContext.FramebufferHeight);
        if (_pixelBufferSize != PvrConfiguration.PixelBufferSize)
        {
            _buffers.Init(Device, DeviceContext);
            _pixelBufferSize = PvrConfiguration.PixelBufferSize;
        }

        DeviceContext.PSSetShaderResource(0, null);
        DeviceContext.OMSetRenderTargets(FbRenderTarget, null);
        ConfigVertexShader();
        DeviceContext.IASetInputLayout(_mainInputLayout);
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

    private void SetTargets(ID3D11RenderTargetView? renderTarget, ID3D11DepthStencilView? depthStencil)
    {
        DeviceContext.OMSetRenderTargetsAndUnorderedAccessViews(1, [renderTarget], depthStencil, 0, uint.MaxValue, null, null);
    }

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

    private readonly PvrDx11OitBuffers _buffers = new();
    private readonly PvrDx11OitShaders _shaders = new();
    private ID3D11Texture2D? _opaqueTex;
    private ID3D11RenderTargetView? _opaqueRenderTarget;
    private ID3D11ShaderResourceView? _opaqueTextureView;
    private ID3D11Texture2D? _multipassTex;
    private ID3D11RenderTargetView? _multipassRenderTarget;
    private ID3D11ShaderResourceView? _multipassTextureView;
    private ID3D11ShaderResourceView? _stencilView;
    private ID3D11ShaderResourceView? _depthView;
    private ID3D11Texture2D? _depthStencilTex2;
    private ID3D11DepthStencilView? _depthStencilView2;
    private ID3D11Buffer? _trPolyParamsBuffer;
    private uint _trPolyParamsBufferSize;
    private ID3D11ShaderResourceView? _trPolyParamsBufferView;
    private ID3D11InputLayout? _mainInputLayout;
    private ID3D11InputLayout? _finalInputLayout;
    private ID3D11Buffer? _vtxPolyConstants;
    private ulong _pixelBufferSize;
    private uint _maxWidth;
    private uint _maxHeight;

    internal new static PvrRenderBackend Create() => new PvrDx11OitRenderer();
}
