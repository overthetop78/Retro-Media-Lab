using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vortice.Direct3D9;
using Vortice.Mathematics;
using static RetroMediaLab.Chips.Video.NEC.PowerVr2.PowerVr2Ta;
using static RetroMediaLab.Chips.Video.NEC.PowerVr2.PvrRegisters;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Direct3D9;

internal sealed unsafe partial class PvrDx9Renderer
{
    private static readonly uint[] DstBlendGl =
    [
        (uint)Blend.Zero, (uint)Blend.One, (uint)Blend.SourceColor, (uint)Blend.InverseSourceColor,
        (uint)Blend.SourceAlpha, (uint)Blend.InverseSourceAlpha, (uint)Blend.DestinationAlpha, (uint)Blend.InverseDestinationAlpha
    ];

    private static readonly uint[] SrcBlendGl =
    [
        (uint)Blend.Zero, (uint)Blend.One, (uint)Blend.DestinationColor, (uint)Blend.InverseDestinationColor,
        (uint)Blend.SourceAlpha, (uint)Blend.InverseSourceAlpha, (uint)Blend.DestinationAlpha, (uint)Blend.InverseDestinationAlpha
    ];

    private static readonly uint[] CullMode = [(uint)Cull.None, (uint)Cull.None, (uint)Cull.CounterClockwise, (uint)Cull.Clockwise];
    private static readonly uint[] ZFunction =
    [
        (uint)CompareFunction.Never, (uint)CompareFunction.Less, (uint)CompareFunction.Equal, (uint)CompareFunction.LessEqual,
        (uint)CompareFunction.Greater, (uint)CompareFunction.NotEqual, (uint)CompareFunction.GreaterEqual, (uint)CompareFunction.Always
    ];

    private static readonly VertexElement[] MainVtxElement =
    [
        new(0, 0, DeclarationType.Float3, DeclarationMethod.Default, DeclarationUsage.Position, 0),
        new(0, 12, DeclarationType.Color, DeclarationMethod.Default, DeclarationUsage.Color, 0),
        new(0, 16, DeclarationType.Color, DeclarationMethod.Default, DeclarationUsage.Color, 1),
        new(0, 20, DeclarationType.Float2, DeclarationMethod.Default, DeclarationUsage.TexCoord, 0),
        new(0xff, 0, DeclarationType.Unused, 0, 0, 0)
    ];

    private static readonly VertexElement[] ModVolVtxElement =
    [
        new(0, 0, DeclarationType.Float3, DeclarationMethod.Default, DeclarationUsage.Position, 0),
        new(0xff, 0, DeclarationType.Unused, 0, 0, 0)
    ];

    private bool EnsureVertexBufferSize(ref IDirect3DVertexBuffer9? buffer, ref uint currentSize, uint minSize)
    {
        if (minSize <= currentSize && buffer != null)
            return true;
        if (currentSize == 0)
            currentSize = minSize;
        else
            while (currentSize < minSize)
                currentSize *= 2;
        Reset(ref buffer);
        return _device!.CreateVertexBuffer(currentSize, Usage.Dynamic | Usage.WriteOnly, 0, Pool.Default, out buffer, 0).Success;
    }

    private bool EnsureIndexBufferSize(ref IDirect3DIndexBuffer9? buffer, ref uint currentSize, uint minSize)
    {
        if (minSize <= currentSize && buffer != null)
            return true;
        if (currentSize == 0)
            currentSize = minSize;
        else
            while (currentSize < minSize)
                currentSize *= 2;
        Reset(ref buffer);
        return _device!.CreateIndexBuffer(currentSize, Usage.Dynamic | Usage.WriteOnly, Format.Index32, Pool.Default, out buffer, 0).Success;
    }

    internal override bool Init()
    {
        IDirect3D9 d3d9 = PvrDx9Context.Instance.D3d;
        d3d9.GetDeviceCaps(0, DeviceType.Hardware, out Caps caps);
        if (caps.VertexShaderVersion < 0xfffe0100u)
        {
            PvrRuntime.Log.Warn("Vertex shader version {0:x}", caps.VertexShaderVersion);
            return false;
        }
        if (caps.PixelShaderVersion < 0xffff0200u)
        {
            PvrRuntime.Log.Warn("Pixel shader version {0:x}", caps.PixelShaderVersion);
            return false;
        }
        _maxAnisotropy = (int)caps.MaxAnisotropy;

        IDirect3DDevice9 device = PvrDx9Context.Instance.Device;
        device.AddRef();
        _device = new IDirect3DDevice9(device.NativePointer);
        _devCache.SetDevice(_device);

        bool success = EnsureVertexBufferSize(ref _vertexBuffer, ref _vertexBufferSize, 4 * 1024 * 1024);
        success &= EnsureIndexBufferSize(ref _indexBuffer, ref _indexBufferSize, 120 * 1024 * 4);

        success &= _device.CreateVertexDeclaration(MainVtxElement, out _mainVtxDecl).Success;
        success &= _device.CreateVertexDeclaration(ModVolVtxElement, out _modVolVtxDecl).Success;

        _shaders.Init(_device);
        success &= _shaders.GetVertexShader(true) != null;
        success &= _device.CreateTexture(32, 32, 1, Usage.Dynamic, Format.A8R8G8B8, Pool.Default, out _paletteTexture, 0).Success;
        success &= _device.CreateTexture(128, 2, 1, Usage.Dynamic, Format.A8, Pool.Default, out _fogTexture, 0).Success;
        UpdateFogTable = true;
        if (!success)
        {
            PvrRuntime.Log.Warn("DirectX9 renderer initialization failed");
            Term();
        }
        _frameRendered = false;
        _frameRenderedOnce = false;
        return success;
    }

    internal void PreReset()
    {
        try
        {
            _texCache.Clear();
        }
        finally
        {
            Reset(ref _backbuffer);
            Reset(ref _depthSurface);
            Reset(ref _rttSurface);
            Reset(ref _rttTexture);
            Reset(ref _dcfbSurface);
            Reset(ref _dcfbTexture);
            Reset(ref _fbScaledTexture);
            Reset(ref _fbScaledSurface);
            Reset(ref _fogTexture);
            Reset(ref _paletteTexture);
            Reset(ref _modVolVtxDecl);
            Reset(ref _mainVtxDecl);
            Reset(ref _modvolBuffer);
            _modvolBufferSize = 0;
            Reset(ref _indexBuffer);
            _indexBufferSize = 0;
            Reset(ref _vertexBuffer);
            _vertexBufferSize = 0;
            Reset(ref _framebufferSurface);
            Reset(ref _framebufferTexture);
            _frameRendered = false;
            _frameRenderedOnce = false;
        }
    }

    internal void PostReset()
    {
        _devCache.Reset();
        uint w = _width;
        uint h = _height;
        _width = 0;
        _height = 0;
        Resize((int)w, (int)h);
        bool result = EnsureVertexBufferSize(ref _vertexBuffer, ref _vertexBufferSize, 4 * 1024 * 1024);
        PvrRuntime.Verify(result);
        result = EnsureIndexBufferSize(ref _indexBuffer, ref _indexBufferSize, 120 * 1024 * 4);
        PvrRuntime.Verify(result);
        result = _device!.CreateVertexDeclaration(MainVtxElement, out _mainVtxDecl).Success;
        PvrRuntime.Verify(result);
        result = _device.CreateVertexDeclaration(ModVolVtxElement, out _modVolVtxDecl).Success;
        PvrRuntime.Verify(result);
        result = _device.CreateTexture(32, 32, 1, Usage.Dynamic, Format.A8R8G8B8, Pool.Default, out _paletteTexture, 0).Success;
        PvrRuntime.Verify(result);
        result = _device.CreateTexture(128, 2, 1, Usage.Dynamic, Format.A8, Pool.Default, out _fogTexture, 0).Success;
        PvrRuntime.Verify(result);
        UpdateFogTable = true;
        UpdatePalette = true;
    }

    internal override void Term()
    {
        try
        {
            PreReset();
            _devCache.Reset();
        }
        finally
        {
            try
            {
                _shaders.Term();
            }
            finally
            {
                Reset(ref _device);
            }
        }
    }

    internal override PvrBaseTextureCacheData? GetTexture(TSP tsp, TCW tcw, int area = 0)
    {
        if (!PvrDx9Context.Instance.IsReady)
            return null;
        PvrDx9Texture? texture = _texCache.GetTextureCacheData(tsp, tcw, area);
        if (texture.NeedsUpdate())
        {
            if (!texture.Update())
                texture = null;
        }
        else if (texture.IsCustomTextureAvailable())
        {
            if (texture.Texture != null)
                _texCache.DeleteLater(texture.Texture);
            Reset(ref texture.Texture);
            texture.LoadCustomTexture();
        }
        return texture;
    }

    internal override void RenderFramebuffer(in PvrFramebufferInfo info)
    {
        if (!PvrDx9Context.Instance.IsReady)
        {
            _frameRendered = true;
            return;
        }
        _rendContext = null!;
        IDirect3DDevice9 device = _device!;

        Reset(ref _backbuffer);
        device.GetRenderTarget(0, out _backbuffer);
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
            _dcfbTexture.GetLevelDesc(0, out SurfaceDescription desc);
            if ((int)desc.Width != width || (int)desc.Height != height)
                Reset(ref _dcfbTexture);
        }
        if (_dcfbTexture == null)
        {
            device.CreateTexture((uint)width, (uint)height, 1, Usage.Dynamic, Format.A8R8G8B8, Pool.Default, out _dcfbTexture, 0);
            Reset(ref _dcfbSurface);
            _dcfbTexture!.GetSurfaceLevel(0, out _dcfbSurface);
        }

        _dcfbTexture.LockRect(0, out LockedRectangle rect, null, 0);
        try
        {
            uint* source = pixels.Data(0, 0);
            int rowBytes = width * sizeof(uint);
            if (rect.Pitch == rowBytes)
                Buffer.MemoryCopy(source, (void*)rect.Bits, rowBytes * height, rowBytes * height);
            else
            {
                byte* destination = (byte*)rect.Bits;
                for (int y = 0; y < height; y++)
                    Buffer.MemoryCopy(source + y * width, destination + y * rect.Pitch, rowBytes, rowBytes);
            }
        }
        finally
        {
            _dcfbTexture.UnlockRect(0);
        }

        Resize(width, height);
        _devCache.Reset();
        _devCache.SetRenderState(RenderState.ScissorTestEnable, 0);

        uint color = 0xff000000u | ((uint)info.VideoBorderColor._red << 16) | ((uint)info.VideoBorderColor._green << 8)
            | (uint)info.VideoBorderColor._blue;
        device.ColorFill(_framebufferSurface, null, color);
        device.StretchRect(_dcfbSurface, null, _framebufferSurface, null, TextureFilter.Linear);

        _aspectRatio = PvrTransformMatrix.GetDcFramebufferAspectRatio();
        DisplayFramebuffer();
        DrawOSD();
        _frameRendered = true;
        _frameRenderedOnce = true;
        ClearLastFrame = false;
        PvrDx9Context.Instance.SetFrameRendered();
    }

    internal override void Process(TaContext context)
    {
        if (!PvrDx9Context.Instance.IsReady)
        {
            _frameRendered = true;
            return;
        }
        if (PvrConfiguration.IsNaomi2)
            throw new PvrRendererException(PvrRuntime.Translate("DirectX 9 doesn't support Naomi 2 games. Select a different graphics API"));

        _rendContext = context.Rend;
        if (ResetTextureCache)
        {
            _texCache.Clear();
            ResetTextureCache = false;
        }
        _texCache.Cleanup();
        PowerVr2Ta.Parse(context, false);
    }

    private void SetTexMode(SamplerState state, uint clamp, uint mirror)
    {
        if (clamp != 0)
            _devCache.SetSamplerState(0, state, (uint)TextureAddress.Clamp);
        else
        {
            if (mirror != 0)
                _devCache.SetSamplerState(0, state, (uint)TextureAddress.Mirror);
            else
                _devCache.SetSamplerState(0, state, (uint)TextureAddress.Wrap);
        }
    }

    private TileClipping SetTileClip(uint tileClip, ref PvrRect clipRect)
    {
        TileClipping clipMode = _matrices.GetTileClip(tileClip, ref clipRect);
        if (clipMode == TileClipping.Outside)
        {
            _devCache.SetRenderState(RenderState.ScissorTestEnable, 1);
            var rect = new RawRect(clipRect.Origin.X, clipRect.Origin.Y, clipRect.BottomRight().X, clipRect.BottomRight().Y);
            _device!.SetScissorRect(in rect);
        }
        else
        {
            _devCache.SetRenderState(RenderState.ScissorTestEnable, _scissorEnable ? 1u : 0u);
            if (_scissorEnable)
                _device!.SetScissorRect(in _scissorRect);
        }
        return clipMode;
    }

    [SkipLocalsInit]
    private void SetGpState(in PolyParam polygon, uint type, bool sortingEnabled)
    {
        float trilinearAlpha;
        if (polygon.Pcw.Texture != 0 && polygon.Tsp.FilterMode > 1 && type != ListType_Punch_Through && polygon.Tcw.MipMapped == 1)
        {
            trilinearAlpha = 0.25f * (polygon.Tsp.MipMapD & 3);
            if (polygon.Tsp.FilterMode == 2)
                trilinearAlpha = 1f - trilinearAlpha;
        }
        else
            trilinearAlpha = 1f;

        bool colorClamp = polygon.Tsp.ColorClamp != 0
            && (_rendContext.FogClampMin.full != 0 || _rendContext.FogClampMax.full != uint.MaxValue);
        uint fogControl = PvrConfiguration.Fog ? polygon.Tsp.FogCtrl : 2u;

        PvrRect clipRect = default;
        TileClipping clipMode = SetTileClip(polygon.TileClip, ref clipRect);
        var texture = (PvrDx9Texture?)polygon.Texture;
        int gpuPalette = texture == null || !texture.GpuPalette ? 0 : (int)polygon.Tsp.FilterMode + 1;
        if (gpuPalette != 0)
        {
            if (PvrConfiguration.TextureFiltering == 1)
                gpuPalette = 1;
            else if (PvrConfiguration.TextureFiltering == 2)
                gpuPalette = 2;
        }

        _devCache.SetPixelShader(_shaders.GetShader(polygon.Pcw.Texture != 0, polygon.Tsp.UseAlpha != 0,
            polygon.Tsp.IgnoreTexA != 0 || polygon.Tcw.PixelFmt == (uint)PixelFormat.Pixel565,
            polygon.Tsp.ShadInstr, polygon.Pcw.Offset != 0, fogControl,
            polygon.Tcw.PixelFmt == (uint)PixelFormat.PixelBumpMap, colorClamp, trilinearAlpha != 1f,
            gpuPalette, polygon.Pcw.Gouraud != 0, clipMode == TileClipping.Inside, _dithering));

        IDirect3DDevice9 device = _device!;
        if (trilinearAlpha != 1f)
        {
            float* values = stackalloc float[4] { trilinearAlpha, 0, 0, 0 };
            device.SetPixelShaderConstantF(5, values, 1);
        }
        if (gpuPalette != 0)
        {
            float* textureSize = stackalloc float[4] { texture!.Width, texture.Height, 0, 0 };
            device.SetPixelShaderConstantF(9, textureSize, 1);
            float* paletteIndex = stackalloc float[4];
            if (polygon.Tcw.PixelFmt == (uint)PixelFormat.PixelPal4)
                paletteIndex[0] = polygon.Tcw.PalSelect << 4;
            else
                paletteIndex[0] = (polygon.Tcw.PalSelect >> 4) << 8;
            device.SetPixelShaderConstantF(0, paletteIndex, 1);
        }

        _devCache.SetVertexShader(_shaders.GetVertexShader(polygon.Pcw.Gouraud != 0));
        _devCache.SetRenderState(RenderState.ShadeMode, (uint)(polygon.Pcw.Gouraud == 1 ? ShadeMode.Gouraud : ShadeMode.Flat));
        if (clipMode == TileClipping.Inside)
        {
            float* values = stackalloc float[4] { clipRect.Origin.X, clipRect.Origin.Y, clipRect.BottomRight().X, clipRect.BottomRight().Y };
            device.SetPixelShaderConstantF(4, values, 1);
        }

        uint stencil = polygon.Pcw.Shadow != 0 ? 0x80u : 0u;
        if (PvrConfiguration.ModifierVolumes)
            _devCache.SetRenderState(RenderState.StencilRef, stencil);

        if (texture != null)
        {
            _devCache.SetTexture(0, texture.Texture);
            SetTexMode(SamplerState.AddressU, polygon.Tsp.ClampU, polygon.Tsp.FlipU);
            SetTexMode(SamplerState.AddressV, polygon.Tsp.ClampV, polygon.Tsp.FlipV);
            bool linearFiltering;
            if (gpuPalette != 0)
                linearFiltering = false;
            else if (PvrConfiguration.TextureFiltering == 0)
                linearFiltering = polygon.Tsp.FilterMode != 0;
            else if (PvrConfiguration.TextureFiltering == 1)
                linearFiltering = false;
            else
                linearFiltering = true;

            if (!linearFiltering)
            {
                _devCache.SetSamplerState(0, SamplerState.MinFilter, (uint)TextureFilter.Point);
                _devCache.SetSamplerState(0, SamplerState.MagFilter, (uint)TextureFilter.Point);
                _devCache.SetSamplerState(0, SamplerState.MipFilter, (uint)TextureFilter.Point);
            }
            else
            {
                _devCache.SetSamplerState(0, SamplerState.MinFilter, (uint)TextureFilter.Linear);
                _devCache.SetSamplerState(0, SamplerState.MagFilter, (uint)TextureFilter.Linear);
                if (type == ListType_Punch_Through)
                {
                    _devCache.SetSamplerState(0, SamplerState.MipFilter, (uint)TextureFilter.Point);
                    _devCache.SetSamplerState(0, SamplerState.MaxAnisotropy, 1);
                }
                else
                {
                    _devCache.SetSamplerState(0, SamplerState.MipFilter, (uint)TextureFilter.Linear);
                    _devCache.SetSamplerState(0, SamplerState.MaxAnisotropy, (uint)Math.Min(_maxAnisotropy, (int)PvrConfiguration.AnisotropicFiltering));
                }
            }
            _devCache.SetSamplerState(0, SamplerState.MipMapLodBias, BitConverter.SingleToUInt32Bits(-1f));
        }

        _devCache.SetRenderState(RenderState.SrcBlend, SrcBlendGl[polygon.Tsp.SrcInstr]);
        _devCache.SetRenderState(RenderState.DestBlend, DstBlendGl[polygon.Tsp.DstInstr]);
        _devCache.SetRenderState(RenderState.CullMode, CullMode[polygon.Isp.CullMode]);
        if (type == ListType_Punch_Through || (type == ListType_Translucent && sortingEnabled))
            _devCache.SetRenderState(RenderState.ZFunc, ZFunction[6]);
        else
            _devCache.SetRenderState(RenderState.ZFunc, ZFunction[polygon.Isp.DepthMode]);

        if (sortingEnabled)
            _devCache.SetRenderState(RenderState.ZWriteEnable, 0);
        else
        {
            if (type == ListType_Punch_Through)
                _devCache.SetRenderState(RenderState.ZWriteEnable, 1);
            else
                _devCache.SetRenderState(RenderState.ZWriteEnable, polygon.Isp.ZWriteDis == 0 ? 1u : 0u);
        }
    }

    private void DrawList(IReadOnlyList<PolyParam> polygons, uint first, uint count, uint type, bool sortingEnabled)
    {
        if (count == 0)
            return;
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
                SetGpState(in polygon, type, sortingEnabled);
                _device!.DrawIndexedPrimitive(PrimitiveType.TriangleStrip, 0, 0, polygon.Count, polygon.First, polygon.Count - 2);
            }

            index++;
        }
    }

    private void DrawSorted(uint first, uint count, bool multipass)
    {
        if (count == 0)
            return;
        uint end = first + count;
        IDirect3DDevice9 device = _device!;
        for (uint p = first; p < end; p++)
        {
            SortedTriangle triangle = _rendContext.SortedTriangles[(int)p];
            PolyParam polygon = _rendContext.GlobalParamTr[(int)triangle.PolyIndex];
            SetGpState(in polygon, ListType_Translucent, true);
            device.DrawIndexedPrimitive(PrimitiveType.TriangleList, 0, 0, triangle.Count, triangle.First, triangle.Count / 3);
        }
        if (multipass && PvrConfiguration.TranslucentPolygonDepthMask)
        {
            _devCache.SetRenderState(RenderState.ColorWriteEnable, 0);
            _devCache.SetRenderState(RenderState.AlphaBlendEnable, 0);
            _devCache.SetPixelShader(_shaders.GetModVolShader());
            _devCache.SetRenderState(RenderState.ZFunc, (uint)CompareFunction.GreaterEqual);
            _devCache.SetRenderState(RenderState.ZWriteEnable, 1);
            _devCache.SetRenderState(RenderState.ScissorTestEnable, _scissorEnable ? 1u : 0u);
            if (_scissorEnable)
                device.SetScissorRect(in _scissorRect);

            for (uint p = first; p < end; p++)
            {
                SortedTriangle triangle = _rendContext.SortedTriangles[(int)p];
                PolyParam polygon = _rendContext.GlobalParamTr[(int)triangle.PolyIndex];
                if (polygon.Isp.ZWriteDis == 0)
                {
                    _devCache.SetRenderState(RenderState.CullMode, CullMode[polygon.Isp.CullMode]);
                    device.DrawIndexedPrimitive(PrimitiveType.TriangleList, 0, 0, triangle.Count, triangle.First, triangle.Count / 3);
                }
            }
            _devCache.SetRenderState(RenderState.ColorWriteEnable, 15);
        }
    }

    private void SetMvsMode(ModifierVolumeMode mode, ISP_Modvol isp)
    {
        if (mode == ModifierVolumeMode.Xor)
        {
            _devCache.SetRenderState(RenderState.ZEnable, 1);
            _devCache.SetRenderState(RenderState.StencilWriteMask, 2);
            _devCache.SetRenderState(RenderState.StencilFunc, (uint)CompareFunction.Always);
            _devCache.SetRenderState(RenderState.StencilPass, (uint)StencilOperation.Invert);
            _devCache.SetRenderState(RenderState.StencilZFail, (uint)StencilOperation.Keep);
            _devCache.SetRenderState(RenderState.CullMode, CullMode[isp.CullMode]);
        }
        else if (mode == ModifierVolumeMode.Or)
        {
            _devCache.SetRenderState(RenderState.ZEnable, 1);
            _devCache.SetRenderState(RenderState.StencilWriteMask, 2);
            _devCache.SetRenderState(RenderState.StencilFunc, (uint)CompareFunction.Always);
            _devCache.SetRenderState(RenderState.StencilRef, 2);
            _devCache.SetRenderState(RenderState.StencilPass, (uint)StencilOperation.Replace);
            _devCache.SetRenderState(RenderState.StencilZFail, (uint)StencilOperation.Keep);
            _devCache.SetRenderState(RenderState.CullMode, CullMode[isp.CullMode]);
        }
        else
        {
            _devCache.SetRenderState(RenderState.ZEnable, 0);
            _devCache.SetRenderState(RenderState.StencilWriteMask, 3);
            _devCache.SetRenderState(RenderState.StencilMask, 3);
            if (mode == ModifierVolumeMode.Inclusion)
            {
                _devCache.SetRenderState(RenderState.StencilFunc, (uint)CompareFunction.LessEqual);
                _devCache.SetRenderState(RenderState.StencilRef, 1);
                _devCache.SetRenderState(RenderState.StencilPass, (uint)StencilOperation.Replace);
                _devCache.SetRenderState(RenderState.StencilFail, (uint)StencilOperation.Zero);
            }
            else
            {
                _devCache.SetRenderState(RenderState.StencilFunc, (uint)CompareFunction.LessEqual);
                _devCache.SetRenderState(RenderState.StencilRef, 1);
                _devCache.SetRenderState(RenderState.StencilPass, (uint)StencilOperation.Keep);
                _devCache.SetRenderState(RenderState.StencilFail, (uint)StencilOperation.Zero);
            }
        }
    }

    private void DrawModVols(uint first, uint count)
    {
        if (count == 0 || _rendContext.ModTrig.Count == 0 || !PvrConfiguration.ModifierVolumes)
            return;

        IDirect3DDevice9 device = _device!;
        device.SetVertexDeclaration(_modVolVtxDecl);
        device.SetStreamSource(0, _modvolBuffer, 0, 3 * sizeof(float));
        _devCache.SetRenderState(RenderState.ZFunc, (uint)CompareFunction.Greater);
        _devCache.SetRenderState(RenderState.AlphaBlendEnable, 0);
        _devCache.SetRenderState(RenderState.StencilEnable, 1);
        _devCache.SetRenderState(RenderState.ZWriteEnable, 0);
        _devCache.SetRenderState(RenderState.ScissorTestEnable, _scissorEnable ? 1u : 0u);
        if (_scissorEnable)
            device.SetScissorRect(in _scissorRect);

        _devCache.SetPixelShader(_shaders.GetModVolShader());
        _devCache.SetRenderState(RenderState.ColorWriteEnable, 0);

        int modBase = -1;
        PvrRect clipRect = default;
        for (uint cmv = 0; cmv < count; cmv++)
        {
            ModifierVolumeParam param = _rendContext.GlobalParamMvo[(int)(first + cmv)];
            if (param.Count == 0)
                continue;
            uint mode = param.Isp.DepthMode;
            if (modBase == -1)
                modBase = (int)param.First;

            if (param.Isp.VolumeLast == 0 && mode > 0)
                SetMvsMode(ModifierVolumeMode.Or, param.Isp);
            else
                SetMvsMode(ModifierVolumeMode.Xor, param.Isp);

            SetTileClip(param.TileClip, ref clipRect);
            device.DrawPrimitive(PrimitiveType.TriangleList, param.First * 3, param.Count);
            if (mode == 1 || mode == 2)
            {
                SetMvsMode(mode == 1 ? ModifierVolumeMode.Inclusion : ModifierVolumeMode.Exclusion, param.Isp);
                device.DrawPrimitive(PrimitiveType.TriangleList, (uint)modBase * 3, param.First + param.Count - (uint)modBase);
                modBase = -1;
            }
        }
        _devCache.SetRenderState(RenderState.CullMode, (uint)Cull.None);
        _devCache.SetRenderState(RenderState.ColorWriteEnable, 15);
        SetTileClip(0, ref clipRect);
        _devCache.SetRenderState(RenderState.AlphaBlendEnable, 1);
        _devCache.SetRenderState(RenderState.SrcBlend, (uint)Blend.SourceAlpha);
        _devCache.SetRenderState(RenderState.DestBlend, (uint)Blend.InverseSourceAlpha);
        _devCache.SetRenderState(RenderState.StencilFunc, (uint)CompareFunction.Equal);
        _devCache.SetRenderState(RenderState.StencilRef, 0x81);
        _devCache.SetRenderState(RenderState.StencilMask, 0x81);
        _devCache.SetRenderState(RenderState.StencilWriteMask, 3);
        _devCache.SetRenderState(RenderState.StencilPass, (uint)StencilOperation.Zero);
        _devCache.SetRenderState(RenderState.StencilFail, (uint)StencilOperation.Zero);
        _devCache.SetRenderState(RenderState.ZEnable, 0);

        device.SetVertexDeclaration(_mainVtxDecl);
        device.SetStreamSource(0, _vertexBuffer, 0, (uint)sizeof(TaVertex));
        device.SetIndices(_indexBuffer);
        device.DrawIndexedPrimitive(PrimitiveType.TriangleStrip, 0, 0, 4, 0, 2);
        _devCache.SetRenderState(RenderState.StencilEnable, 0);
        _devCache.SetRenderState(RenderState.ZEnable, 1);
    }

    private void DrawStrips()
    {
        RenderPass previous = default;
        for (int renderPass = 0; renderPass < _rendContext.RenderPasses.Count; renderPass++)
        {
            RenderPass current = _rendContext.RenderPasses[renderPass];
            uint opaqueCount = current.OpCount - previous.OpCount;
            uint punchThroughCount = current.PtCount - previous.PtCount;
            uint translucentCount = current.TrCount - previous.TrCount;
            uint modifierCount = current.MvoCount - previous.MvoCount;
            PvrRuntime.Log.Debug("Render pass {0} OP {1} PT {2} TR {3} MV {4}", renderPass + 1,
                opaqueCount, punchThroughCount, translucentCount, modifierCount);
            if (PvrConfiguration.ModifierVolumes)
            {
                _devCache.SetRenderState(RenderState.StencilEnable, 1);
                _devCache.SetRenderState(RenderState.StencilWriteMask, 0xff);
                _devCache.SetRenderState(RenderState.StencilFunc, (uint)CompareFunction.Always);
                _devCache.SetRenderState(RenderState.StencilPass, (uint)StencilOperation.Replace);
                _devCache.SetRenderState(RenderState.StencilZFail, (uint)StencilOperation.Keep);
            }
            else
            {
                _devCache.SetRenderState(RenderState.StencilEnable, 0);
            }
            _devCache.SetRenderState(RenderState.AlphaBlendEnable, 1);
            DrawList(_rendContext.GlobalParamOp, previous.OpCount, opaqueCount, ListType_Opaque, false);

            _devCache.SetRenderState(RenderState.AlphaTestEnable, 1);
            _devCache.SetRenderState(RenderState.AlphaFunc, (uint)CompareFunction.GreaterEqual);
            _devCache.SetRenderState(RenderState.AlphaRef, PT_ALPHA_REF & 0xff);
            DrawList(_rendContext.GlobalParamPt, previous.PtCount, punchThroughCount, ListType_Punch_Through, false);
            _devCache.SetRenderState(RenderState.AlphaTestEnable, 0);
            DrawModVols(previous.MvoCount, modifierCount);
            _devCache.SetRenderState(RenderState.AlphaBlendEnable, 1);
            _devCache.SetRenderState(RenderState.StencilEnable, 0);

            if (current.AutoSort)
            {
                if (!PvrConfiguration.PerStripSorting)
                    DrawSorted(previous.SortedTrCount, current.SortedTrCount - previous.SortedTrCount,
                        renderPass < _rendContext.RenderPasses.Count - 1);
                else
                    DrawList(_rendContext.GlobalParamTr, previous.TrCount, translucentCount, ListType_Translucent, true);
            }
            else
            {
                DrawList(_rendContext.GlobalParamTr, previous.TrCount, translucentCount, ListType_Translucent, false);
            }
            previous = current;
        }
    }

    private void SetBaseScissor()
    {
        PvrRect scissor = _matrices.GetBaseScissor();
        _scissorRect.Left = scissor.Origin.X;
        _scissorRect.Top = scissor.Origin.Y;
        _scissorRect.Right = scissor.BottomRight().X;
        _scissorRect.Bottom = scissor.BottomRight().Y;
        if (_rendContext.IsRtt)
            _scissorEnable = true;
        else
        {
            if (scissor.Origin.X != 0 || scissor.Origin.Y != 0
                || scissor.Size.X != (int)_rendContext.FramebufferWidth || scissor.Size.Y != (int)_rendContext.FramebufferHeight)
                _scissorEnable = true;
            else
                _scissorEnable = false;
        }
        if (_scissorEnable)
        {
            _device!.SetScissorRect(in _scissorRect);
            _devCache.SetRenderState(RenderState.ScissorTestEnable, 1);
        }
        else
            _devCache.SetRenderState(RenderState.ScissorTestEnable, 0);
    }

    private void PrepareRttRenderTarget(uint texAddress, out int viewportWidth, out int viewportHeight)
    {
        IDirect3DDevice9 device = _device!;
        uint width = _rendContext.FramebufferWidth;
        uint height = _rendContext.FramebufferHeight;
        PvrRuntime.Log.Debug("RTT packmode={0} stride={1} - {2} x {3} @ {4:x6}",
            _rendContext.FbWCtrl.fb_packmode, _rendContext.FbWLineStride * 8, width, height, texAddress);

        Reset(ref _rttTexture);
        device.CreateTexture(width, height, 1, Usage.RenderTarget, Format.A8R8G8B8, Pool.Default, out _rttTexture, 0);
        Reset(ref _rttSurface);
        _rttTexture!.GetSurfaceLevel(0, out _rttSurface);
        device.SetRenderTarget(0, _rttSurface);

        if (width > _width || height > _height || _depthSurface == null)
        {
            if (_depthSurface != null)
            {
                _depthSurface.GetDesc(out SurfaceDescription desc);
                if (width > desc.Width || height > desc.Height)
                    Reset(ref _depthSurface);
            }
            if (_depthSurface == null)
            {
                bool result = device.CreateDepthStencilSurface(Math.Max(width, _width), Math.Max(height, _height),
                    Format.D24S8, MultisampleType.None, 0, true, out _depthSurface, 0).Success;
                PvrRuntime.Verify(result);
            }
        }

        Viewport viewport = new() { X = 0, Y = 0, Width = width, Height = height, MinZ = 0f, MaxZ = 1f };
        device.SetViewport(in viewport);
        viewportWidth = (int)width;
        viewportHeight = (int)height;
    }

    private void ReadRttRenderTarget(uint texAddress)
    {
        IDirect3DDevice9 device = _device!;
        uint width = _rendContext.FramebufferWidth;
        uint height = _rendContext.FramebufferHeight;
        if (PvrConfiguration.RenderToTextureBuffer)
        {
            _rttSurface!.GetDesc(out SurfaceDescription desc);
            bool result = device.CreateOffscreenPlainSurface(desc.Width, desc.Height, desc.Format,
                Pool.SystemMemory, out IDirect3DSurface9? offscreen, 0).Success;
            PvrRuntime.Verify(result);
            using (offscreen)
            {
                result = device.GetRenderTargetData(_rttSurface, offscreen).Success;
                PvrRuntime.Verify(result);

                using var pixels = new PvrPixelBuffer<uint>();
                pixels.Init(width, height);
                byte* destination = (byte*)pixels.Data();
                var lockRect = new RawRect(0, 0, (int)width, (int)height);
                result = offscreen!.LockRect(out LockedRectangle rect, lockRect, LockFlags.ReadOnly).Success;
                PvrRuntime.Verify(result);
                try
                {
                    if ((uint)rect.Pitch == width * sizeof(uint))
                    {
                        uint size = width * height * sizeof(uint);
                        Buffer.MemoryCopy((void*)rect.Bits, destination, size, size);
                    }
                    else
                    {
                        byte* source = (byte*)rect.Bits;
                        for (uint y = 0; y < height; y++)
                        {
                            Buffer.MemoryCopy(source, destination, width * sizeof(uint), width * sizeof(uint));
                            source += rect.Pitch;
                            destination += width * sizeof(uint);
                        }
                    }
                }
                finally
                {
                    result = offscreen.UnlockRect().Success;
                    PvrRuntime.Verify(result);
                }

                ushort* dst = (ushort*)(PvrMemory.VramPointer + texAddress);
                PvrTextureCache.WriteTextureToVram(width, height, (byte*)pixels.Data(), dst, _rendContext.FbWCtrl,
                    _rendContext.FbWLineStride * 8, _rendContext.FbClip, 2, 1, 0, 3);
            }
        }
        else
        {
            PvrTransformMatrix.GetPvrFramebufferSize(_rendContext, out int wpo2, out int hpo2);
            if (wpo2 <= 1024 && hpo2 <= 1024)
            {
                PvrDx9Texture texture = _texCache.GetRenderToTexture(texAddress, _rendContext.FbWCtrl.fb_packmode, (uint)wpo2, (uint)hpo2);
                Reset(ref texture.Texture);
                _rttTexture!.AddRef();
                texture.Texture = new IDirect3DTexture9(_rttTexture.NativePointer);
                texture.Dirty = 0;
                texture.UnprotectVram();
            }
        }
    }

    [SkipLocalsInit]
    internal override bool Render()
    {
        if (!PvrDx9Context.Instance.IsReady)
            return false;

        IDirect3DDevice9 device = _device!;
        bool isRtt = _rendContext.IsRtt;
        Reset(ref _backbuffer);
        bool result = device.GetRenderTarget(0, out _backbuffer).Success;
        PvrRuntime.Verify(result);
        uint texAddress = _rendContext.FbWSof1 & PvrMemory.VramMask;
        int viewportWidth;
        int viewportHeight;
        if (isRtt)
            PrepareRttRenderTarget(texAddress, out viewportWidth, out viewportHeight);
        else
        {
            Resize((int)_rendContext.FramebufferWidth, (int)_rendContext.FramebufferHeight);
            if (_rendContext.ClearFramebuffer)
                device.ColorFill(_framebufferSurface, null, GetBorderColor());
            result = device.SetRenderTarget(0, _framebufferSurface).Success;
            PvrRuntime.Verify(result);
            Viewport viewport = new() { X = 0, Y = 0, Width = _width, Height = _height, MinZ = 0f, MaxZ = 1f };
            result = device.SetViewport(in viewport).Success;
            PvrRuntime.Verify(result);
            viewportWidth = (int)_width;
            viewportHeight = (int)_height;
        }
        result = device.SetDepthStencilSurface(_depthSurface).Success;
        PvrRuntime.Verify(result);
        _matrices.CalcMatrices(_rendContext, (int)_rendContext.FramebufferWidth, (int)_rendContext.FramebufferHeight);
        Matrix4x4 normalMatrix = _matrices.GetNormalMatrix() * Matrix4x4.CreateTranslation(-1f / viewportWidth, 1f / viewportHeight, 0);
        result = device.SetVertexShaderConstantF(0, (float*)&normalMatrix, 4).Success;
        PvrRuntime.Verify(result);

        _devCache.Reset();
        _devCache.SetRenderState(RenderState.ScissorTestEnable, 0);
        device.Clear(0, null, ClearFlags.Stencil | ClearFlags.ZBuffer, 0, 0f, 0);
        PvrSorter.SetFirstProvokingVertex(_rendContext);
        _devCache.SetRenderState(RenderState.ClipPlaneEnable, 15);
        float* plane = stackalloc float[4] { 0, 0, 0, 1 };
        plane[0] = 1f;
        device.SetClipPlane(0, plane);
        plane[0] = -1f;
        device.SetClipPlane(1, plane);
        plane[0] = 0f;
        plane[1] = 1f;
        device.SetClipPlane(2, plane);
        plane[1] = -1f;
        device.SetClipPlane(3, plane);

        uint size = (uint)(_rendContext.Verts.Count * sizeof(TaVertex));
        result = EnsureVertexBufferSize(ref _vertexBuffer, ref _vertexBufferSize, size);
        PvrRuntime.Verify(result);
        result = _vertexBuffer!.Lock(0, size, out nint pointer, LockFlags.Discard).Success;
        PvrRuntime.Verify(result);
        try
        {
            fixed (TaVertex* vertices = CollectionsMarshal.AsSpan(_rendContext.Verts))
                Buffer.MemoryCopy(vertices, (void*)pointer, size, size);
        }
        finally
        {
            _vertexBuffer.Unlock();
        }
        size = (uint)(_rendContext.Idx.Count * sizeof(uint));
        result = EnsureIndexBufferSize(ref _indexBuffer, ref _indexBufferSize, size);
        PvrRuntime.Verify(result);
        result = _indexBuffer!.Lock(0, size, out pointer, LockFlags.Discard).Success;
        PvrRuntime.Verify(result);
        try
        {
            fixed (uint* indices = CollectionsMarshal.AsSpan(_rendContext.Idx))
                Buffer.MemoryCopy(indices, (void*)pointer, size, size);
        }
        finally
        {
            _indexBuffer.Unlock();
        }

        if (PvrConfiguration.ModifierVolumes && _rendContext.ModTrig.Count != 0)
        {
            size = (uint)(_rendContext.ModTrig.Count * sizeof(ModTriangle));
            result = EnsureVertexBufferSize(ref _modvolBuffer, ref _modvolBufferSize, size);
            PvrRuntime.Verify(result);
            result = _modvolBuffer!.Lock(0, size, out pointer, LockFlags.Discard).Success;
            PvrRuntime.Verify(result);
            try
            {
                fixed (ModTriangle* triangles = CollectionsMarshal.AsSpan(_rendContext.ModTrig))
                    Buffer.MemoryCopy(triangles, (void*)pointer, size, size);
            }
            finally
            {
                _modvolBuffer.Unlock();
            }
        }
        UpdateFogTexture();
        UpdatePaletteTexture();
        _devCache.SetVertexShader(_shaders.GetVertexShader(true));

        float* fogColVert = stackalloc float[4];
        float* fogColRam = stackalloc float[4];
        FOG_COL_VERT.GetRGBColor(fogColVert);
        FOG_COL_RAM.GetRGBColor(fogColRam);
        device.SetPixelShaderConstantF(1, fogColVert, 1);
        device.SetPixelShaderConstantF(2, fogColRam, 1);

        float fogDensity = FOG_DENSITY.Get() * PvrConfiguration.ExtraDepthScale;
        float* fogDensityAndScale = stackalloc float[4] { fogDensity, 1f - FPU_SHAD_SCALE.scale_factor / 256f, 0, 1 };
        device.SetPixelShaderConstantF(3, fogDensityAndScale, 1);

        float* colorClamp = stackalloc float[4];
        _rendContext.FogClampMin.GetRGBAColor(new Span<float>(colorClamp, 4));
        device.SetPixelShaderConstantF(6, colorClamp, 1);
        _rendContext.FogClampMax.GetRGBAColor(new Span<float>(colorClamp, 4));
        device.SetPixelShaderConstantF(7, colorClamp, 1);

        _dithering = PvrConfiguration.EmulateFramebuffer && _rendContext.FbWCtrl.fb_dither != 0 && _rendContext.FbWCtrl.fb_packmode <= 3;
        if (_dithering)
        {
            float* divisor = stackalloc float[4] { 0f, 0f, 0f, 1f };
            switch (_rendContext.FbWCtrl.fb_packmode)
            {
                case 0:
                case 3:
                    divisor[0] = divisor[1] = divisor[2] = 2f;
                    break;
                case 1:
                    divisor[0] = divisor[2] = 2f;
                    divisor[1] = 4f;
                    break;
                case 2:
                    divisor[0] = divisor[1] = divisor[2] = 1f;
                    break;
            }
            device.SetPixelShaderConstantF(8, divisor, 1);
        }

        _devCache.SetRenderState(RenderState.ZEnable, 1);
        device.SetVertexDeclaration(_mainVtxDecl);
        device.SetStreamSource(0, _vertexBuffer, 0, (uint)sizeof(TaVertex));
        device.SetIndices(_indexBuffer);

        _devCache.SetSamplerState(0, SamplerState.MinFilter, (uint)TextureFilter.Linear);
        _devCache.SetSamplerState(0, SamplerState.MagFilter, (uint)TextureFilter.Linear);
        _devCache.SetRenderState(RenderState.AlphaBlendEnable, 0);
        _devCache.SetRenderState(RenderState.SeparateAlphaBlendEnable, 0);
        _devCache.SetRenderState(RenderState.AlphaTestEnable, 0);
        _devCache.SetRenderState(RenderState.FillMode, (uint)FillMode.Solid);
        _devCache.SetRenderState(RenderState.Clipping, 0);
        SetBaseScissor();

        if (device.BeginScene().Failure)
        {
            PvrRuntime.Log.Warn("Render: BeginScene failed!");
            return false;
        }
        try
        {
            DrawStrips();
        }
        finally
        {
            device.EndScene();
        }
        _devCache.SetRenderState(RenderState.ClipPlaneEnable, 0);
        result = device.SetRenderTarget(0, _backbuffer).Success;
        PvrRuntime.Verify(result);

        if (isRtt)
            ReadRttRenderTarget(texAddress);
        else if (PvrConfiguration.EmulateFramebuffer)
            WriteFramebufferToVram();
        else
        {
            _aspectRatio = PvrTransformMatrix.GetOutputFramebufferAspectRatio();
            DisplayFramebuffer();
            DrawOSD();
            _frameRendered = true;
            _frameRenderedOnce = true;
            ClearLastFrame = false;
            PvrDx9Context.Instance.SetFrameRendered();
        }
        return !isRtt;
    }

    private void Resize(int width, int height)
    {
        if (_width == (uint)width && _height == (uint)height)
            return;
        if (!PvrConfiguration.EmulateFramebuffer)
            PvrRuntime.Log.Notice("D3DRenderer::resize: {0} x {1} -> {2} x {3}", _width, _height, width, height);

        _width = (uint)width;
        _height = (uint)height;
        Reset(ref _framebufferTexture);
        Reset(ref _framebufferSurface);
        var result = _device!.CreateTexture(_width, _height, 1, Usage.RenderTarget, Format.A8R8G8B8, Pool.Default, out _framebufferTexture, 0);
        if (result.Failure)
        {
            PvrRuntime.Log.Error("Framebuffer texture ({0} x {1}) creation failed: {2:x}", width, height, result.Code);
            PvrRuntime.Die("Framebuffer texture creation failed");
        }
        bool success = _framebufferTexture!.GetSurfaceLevel(0, out _framebufferSurface).Success;
        PvrRuntime.Verify(success);
        Reset(ref _depthSurface);
        success = _device.CreateDepthStencilSurface(_width, _height, Format.D24S8,
            MultisampleType.None, 0, true, out _depthSurface, 0).Success;
        PvrRuntime.Verify(success);
        _frameRendered = false;
        _frameRenderedOnce = false;
    }

    private void DisplayFramebuffer()
    {
        IDirect3DDevice9 device = _device!;
        _devCache.SetRenderState(RenderState.ScissorTestEnable, 0);
        device.ColorFill(_backbuffer, null, GetBorderColor());

        int dx = 0;
        int dy = 0;
        int displayWidth = PvrRuntime.Display.Width;
        int displayHeight = PvrRuntime.Display.Height;
        PvrTransformMatrix.GetWindowboxDimensions(displayWidth, displayHeight, _aspectRatio, ref dx, ref dy, PvrConfiguration.Rotate90);
        PvrTransformMatrix.GetVideoShift(out float shiftX, out float shiftY);
        TextureFilter filter = PvrConfiguration.LinearInterpolation ? TextureFilter.Linear : TextureFilter.Point;
        if (!PvrConfiguration.Rotate90 && shiftX == 0 && shiftY == 0)
        {
            var source = new RawRect(0, 0, (int)_width, (int)_height);
            var destination = new RawRect(dx, dy, displayWidth - dx, displayHeight - dy);
            device.StretchRect(_framebufferSurface, source, _backbuffer, destination, filter);
        }
        else
        {
            device.SetPixelShader(null);
            device.SetVertexShader(null);
            device.SetRenderState(RenderState.CullMode, (uint)Cull.None);
            device.SetRenderState(RenderState.ZEnable, 0);
            device.SetRenderState(RenderState.AlphaBlendEnable, 0);
            device.SetRenderState(RenderState.AlphaTestEnable, 0);
            device.SetSamplerState(0, SamplerState.MinFilter, (uint)filter);
            device.SetSamplerState(0, SamplerState.MagFilter, (uint)filter);

            Matrix4x4 identity = Matrix4x4.Identity;
            Matrix4x4 projection = Matrix4x4.CreateTranslation(-1f / displayWidth, 1f / displayHeight, 0);
            if (PvrConfiguration.Rotate90)
                projection = Matrix4x4.CreateRotationZ(MathF.PI / 2f) * projection;

            device.SetTransform(TransformState.World, in identity);
            device.SetTransform(TransformState.View, in identity);
            device.SetTransform(TransformState.Projection, in projection);
            device.SetFVF(FlexibleVertexFormat.Xyz | FlexibleVertexFormat.Tex1);
            Viewport viewport = new()
            {
                X = (uint)dx,
                Y = (uint)dy,
                Width = (uint)(displayWidth - dx * 2),
                Height = (uint)(displayHeight - dy * 2),
                MinZ = 0,
                MaxZ = 1
            };
            bool result = device.SetViewport(in viewport).Success;
            PvrRuntime.Verify(result);
            float* coords = stackalloc float[20]
            {
                -1,  1, 0.5f, 0, 0,
                -1, -1, 0.5f, 0, 1,
                 1,  1, 0.5f, 1, 0,
                 1, -1, 0.5f, 1, 1
            };
            coords[0] = coords[5] = -1f + shiftX * 2f / _width;
            coords[10] = coords[15] = coords[0] + 2;
            coords[1] = coords[11] = 1f - shiftY * 2f / _height;
            coords[6] = coords[16] = coords[1] - 2;

            device.SetTexture(0, _framebufferTexture);
            device.DrawPrimitiveUP(PrimitiveType.TriangleStrip, 2, (nint)coords, sizeof(float) * 5);
        }
    }

    internal override bool RenderLastFrame()
    {
        if (ClearLastFrame || !_frameRenderedOnce || !PvrDx9Context.Instance.IsReady)
            return false;
        Reset(ref _backbuffer);
        bool result = _device!.GetRenderTarget(0, out _backbuffer).Success;
        PvrRuntime.Verify(result);
        _devCache.Reset();
        DisplayFramebuffer();
        return true;
    }

    private void UpdatePaletteTexture()
    {
        if (!UpdatePalette)
            return;
        UpdatePalette = false;
        bool result = _paletteTexture!.LockRect(0, out LockedRectangle rect, null, 0).Success;
        PvrRuntime.Verify(result);
        try
        {
            fixed (uint* palette = PvrTextureConversion.Palette32Ram)
            {
                if (rect.Pitch == 32 * sizeof(uint))
                    Buffer.MemoryCopy(palette, (void*)rect.Bits, 32 * 32 * sizeof(uint), 32 * 32 * sizeof(uint));
                else
                {
                    byte* destination = (byte*)rect.Bits;
                    for (int y = 0; y < 32; y++)
                        Buffer.MemoryCopy(palette + y * 32, destination + y * rect.Pitch, 32 * sizeof(uint), 32 * sizeof(uint));
                }
            }
        }
        finally
        {
            _paletteTexture.UnlockRect(0);
        }
        _device!.SetTexture(1, _paletteTexture);
        _device.SetSamplerState(1, SamplerState.MinFilter, (uint)TextureFilter.Point);
        _device.SetSamplerState(1, SamplerState.MagFilter, (uint)TextureFilter.Point);
    }

    private void UpdateFogTexture()
    {
        if (!UpdateFogTable || !PvrConfiguration.Fog)
            return;
        UpdateFogTable = false;
        byte* data = stackalloc byte[256];
        PvrTextureCache.MakeFogTexture(data);
        bool result = _fogTexture!.LockRect(0, out LockedRectangle rect, null, 0).Success;
        PvrRuntime.Verify(result);
        try
        {
            if (rect.Pitch == 128)
                Buffer.MemoryCopy(data, (void*)rect.Bits, 128 * 2, 128 * 2);
            else
            {
                byte* destination = (byte*)rect.Bits;
                for (int y = 0; y < 2; y++)
                    Buffer.MemoryCopy(data + y * 128, destination + y * rect.Pitch, 128, 128);
            }
        }
        finally
        {
            _fogTexture.UnlockRect(0);
        }
        _device!.SetTexture(2, _fogTexture);
        _device.SetSamplerState(2, SamplerState.MinFilter, (uint)TextureFilter.Linear);
        _device.SetSamplerState(2, SamplerState.MagFilter, (uint)TextureFilter.Linear);
    }

    private static void DrawOSD()
    {
        PvrDx9Context.Instance.SetOverlay(true);
        try
        {
            PvrRuntime.Gui.DisplayOsd();
        }
        finally
        {
            PvrDx9Context.Instance.SetOverlay(false);
        }
    }

    private void WriteFramebufferToVram()
    {
        IDirect3DDevice9 device = _device!;
        uint width = (uint)_rendContext.GlobClip.X;
        uint height = (uint)_rendContext.GlobClip.Y;
        PvrTransformMatrix.GetWriteFramebufferToVramParams(_rendContext, out PvrInt2 scaledSize, out PvrRect finalClip);
        IDirect3DSurface9? surface = _framebufferSurface;
        if (scaledSize.X != (int)width || scaledSize.Y != (int)height)
        {
            uint scaledWidth = (uint)scaledSize.X;
            uint scaledHeight = (uint)scaledSize.Y;
            if (_fbScaledTexture != null)
            {
                _fbScaledTexture.GetLevelDesc(0, out SurfaceDescription desc);
                if (desc.Width != scaledWidth || desc.Height != scaledHeight)
                {
                    Reset(ref _fbScaledTexture);
                    Reset(ref _fbScaledSurface);
                }
            }
            if (_fbScaledTexture == null)
            {
                device.CreateTexture(scaledWidth, scaledHeight, 1, Usage.RenderTarget, Format.A8R8G8B8, Pool.Default, out _fbScaledTexture, 0);
                _fbScaledTexture!.GetSurfaceLevel(0, out _fbScaledSurface);
            }
            device.StretchRect(_framebufferSurface, null, _fbScaledSurface, null, TextureFilter.Linear);

            width = scaledWidth;
            height = scaledHeight;
            surface = _fbScaledSurface;
        }
        uint texAddress = _rendContext.FbWSof1 & PvrMemory.VramMask;
        uint lineStride = _rendContext.FbWLineStride * 8;

        bool result = device.CreateOffscreenPlainSurface(width, height, Format.A8R8G8B8,
            Pool.SystemMemory, out IDirect3DSurface9? offscreen, 0).Success;
        PvrRuntime.Verify(result);
        using (offscreen)
        {
            result = device.GetRenderTargetData(surface, offscreen).Success;
            PvrRuntime.Verify(result);
            using var pixels = new PvrPixelBuffer<uint>();
            pixels.Init(width, height);

            byte* destination = (byte*)pixels.Data();
            var lockRect = new RawRect(0, 0, (int)width, (int)height);
            result = offscreen!.LockRect(out LockedRectangle rect, lockRect, LockFlags.ReadOnly).Success;
            PvrRuntime.Verify(result);
            try
            {
                if ((uint)rect.Pitch == width * sizeof(uint))
                {
                    uint size = width * height * sizeof(uint);
                    Buffer.MemoryCopy((void*)rect.Bits, destination, size, size);
                }
                else
                {
                    byte* source = (byte*)rect.Bits;
                    for (uint y = 0; y < height; y++)
                    {
                        Buffer.MemoryCopy(source, destination, width * sizeof(uint), width * sizeof(uint));
                        source += rect.Pitch;
                        destination += width * sizeof(uint);
                    }
                }
            }
            finally
            {
                result = offscreen.UnlockRect().Success;
                PvrRuntime.Verify(result);
            }
            PvrTextureCache.WriteFramebuffer(width, height, (byte*)pixels.Data(), texAddress, _rendContext.FbWCtrl, lineStride, finalClip, 2, 1, 0, 3);
        }
    }

    internal override bool GetLastFrame(List<byte> data, ref int width, ref int height)
    {
        if (!_frameRenderedOnce || !PvrDx9Context.Instance.IsReady)
            return false;
        if (width != 0)
        {
            height = (int)(width / _aspectRatio);
        }
        else if (height != 0)
        {
            width = (int)(_aspectRatio * height);
        }
        else
        {
            width = (int)_width;
            height = (int)_height;
            if (PvrConfiguration.Rotate90)
                (width, height) = (height, width);
            int w = (int)(_aspectRatio * height);
            if (width > w)
                height = (int)(width / _aspectRatio);
            else
                width = w;
        }

        IDirect3DDevice9 device = _device!;
        Reset(ref _backbuffer);
        device.GetRenderTarget(0, out _backbuffer);

        device.CreateTexture((uint)width, (uint)height, 1, Usage.RenderTarget, Format.A8R8G8B8, Pool.Default, out IDirect3DTexture9? target, 0);
        using (target)
        {
            target!.GetSurfaceLevel(0, out IDirect3DSurface9? surface);
            using (surface)
            {
                device.SetRenderTarget(0, surface);
                try
                {
                    _devCache.SetRenderState(RenderState.ScissorTestEnable, 0);
                    device.SetPixelShader(null);
                    device.SetVertexShader(null);
                    device.SetRenderState(RenderState.CullMode, (uint)Cull.None);
                    device.SetRenderState(RenderState.ZEnable, 0);
                    device.SetRenderState(RenderState.AlphaBlendEnable, 0);
                    device.SetRenderState(RenderState.AlphaTestEnable, 0);
                    device.SetSamplerState(0, SamplerState.MinFilter, (uint)TextureFilter.Linear);
                    device.SetSamplerState(0, SamplerState.MagFilter, (uint)TextureFilter.Linear);

                    Matrix4x4 identity = Matrix4x4.Identity;
                    Matrix4x4 projection = Matrix4x4.CreateTranslation(-1f / width, 1f / height, 0);
                    if (PvrConfiguration.Rotate90)
                        projection = Matrix4x4.CreateRotationZ(MathF.PI / 2f) * projection;
                    device.SetTransform(TransformState.World, in identity);
                    device.SetTransform(TransformState.View, in identity);
                    device.SetTransform(TransformState.Projection, in projection);

                    device.SetFVF(FlexibleVertexFormat.Xyz | FlexibleVertexFormat.Tex1);
                    Viewport viewport = new() { Width = (uint)width, Height = (uint)height, MaxZ = 1 };
                    bool result = device.SetViewport(in viewport).Success;
                    PvrRuntime.Verify(result);
                    float* coords = stackalloc float[20]
                    {
                        -1,  1, 0.5f, 0, 0,
                        -1, -1, 0.5f, 0, 1,
                         1,  1, 0.5f, 1, 0,
                         1, -1, 0.5f, 1, 1
                    };
                    device.SetTexture(0, _framebufferTexture);
                    device.DrawPrimitiveUP(PrimitiveType.TriangleStrip, 2, (nint)coords, sizeof(float) * 5);

                    result = device.CreateOffscreenPlainSurface((uint)width, (uint)height, Format.A8R8G8B8,
                        Pool.SystemMemory, out IDirect3DSurface9? offscreen, 0).Success;
                    PvrRuntime.Verify(result);
                    using (offscreen)
                    {
                        result = device.GetRenderTargetData(surface, offscreen).Success;
                        PvrRuntime.Verify(result);
                        var lockRect = new RawRect(0, 0, width, height);
                        result = offscreen!.LockRect(out LockedRectangle rect, lockRect, LockFlags.ReadOnly).Success;
                        PvrRuntime.Verify(result);
                        try
                        {
                            data.Clear();
                            data.EnsureCapacity(width * height * 3);
                            for (int y = 0; y < height; y++)
                            {
                                byte* source = (byte*)rect.Bits + y * rect.Pitch;
                                for (int x = 0; x < width; x++, source += 4)
                                {
                                    data.Add(source[2]);
                                    data.Add(source[1]);
                                    data.Add(source[0]);
                                }
                            }
                        }
                        finally
                        {
                            offscreen.UnlockRect();
                        }
                    }
                }
                finally
                {
                    device.SetRenderTarget(0, _backbuffer);
                }
            }
        }
        return true;
    }

    private static uint GetBorderColor()
    {
        uint red = (uint)(VO_BORDER_COL.Red() * 255f);
        uint green = (uint)(VO_BORDER_COL.Green() * 255f);
        uint blue = (uint)(VO_BORDER_COL.Blue() * 255f);
        return 0xff000000u | ((red & 0xff) << 16) | ((green & 0xff) << 8) | (blue & 0xff);
    }

    private static void Reset<T>(ref T? resource) where T : class, IDisposable
    {
        resource?.Dispose();
        resource = null;
    }

    internal static PvrRenderBackend Create() => new PvrDx9Renderer();
}
