using System.Runtime.InteropServices;
using RetroMediaLab.Chips.CPU.Hitachi.SH4;
using static RetroMediaLab.Chips.Video.NEC.PowerVr2.PowerVr2Ta;
using static RetroMediaLab.Chips.Video.NEC.PowerVr2.PvrRegisters;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal static unsafe partial class PvrGles
{
    private static readonly uint[] CullModes = [PvrGl.None, PvrGl.None, PvrGl.Front, PvrGl.Back];

    internal static readonly uint[] ZFunction =
    [
        PvrGl.Never, PvrGl.Less, PvrGl.Equal, PvrGl.Lequal, PvrGl.Greater, PvrGl.Notequal,
        PvrGl.Gequal, PvrGl.Always
    ];

    internal static readonly uint[] DstBlendGl =
    [
        PvrGl.Zero, PvrGl.One, PvrGl.SrcColor, PvrGl.OneMinusSrcColor, PvrGl.SrcAlpha,
        PvrGl.OneMinusSrcAlpha, PvrGl.DstAlpha, PvrGl.OneMinusDstAlpha
    ];

    internal static readonly uint[] SrcBlendGl =
    [
        PvrGl.Zero, PvrGl.One, PvrGl.DstColor, PvrGl.OneMinusDstColor, PvrGl.SrcAlpha,
        PvrGl.OneMinusSrcAlpha, PvrGl.DstAlpha, PvrGl.OneMinusDstAlpha
    ];

    private static PvrGlPipelineShader? _currentShader;

    internal static void SetCull(uint cullMode)
    {
        PvrGlCache cache = PvrGlCache.Instance;
        if (CullModes[cullMode] == PvrGl.None)
            cache.Disable(PvrGl.CullFaceMode);
        else
        {
            cache.Enable(PvrGl.CullFaceMode);
            cache.CullFace(CullModes[cullMode]);
        }
    }

    private static void SetTextureRepeatMode(uint direction, uint clamp, uint mirror)
    {
        if (clamp != 0)
            PvrGlCache.Instance.TexParameteri(PvrGl.Texture2D, direction, PvrGl.ClampToEdge);
        else
            PvrGlCache.Instance.TexParameteri(PvrGl.Texture2D, direction,
                mirror != 0 ? PvrGl.MirroredRepeat : PvrGl.Repeat);
    }

    private static void SetBaseClipping()
    {
        PvrGlBaseClipping clipping = ShaderUniforms.BaseClipping;
        if (clipping.Enabled)
        {
            PvrGlCache.Instance.Enable(PvrGl.ScissorTest);
            PvrGlCache.Instance.Scissor(clipping.X, clipping.Y, clipping.Width, clipping.Height);
        }
        else
        {
            PvrGlCache.Instance.Disable(PvrGl.ScissorTest);
        }
    }

    private static TileClipping SetTileClip(uint tileClip, ref PvrRect rectangle)
    {
        TileClipping mode = Gl.Matrices.GetTileClip(tileClip, ref rectangle);
        if (mode == TileClipping.Outside)
        {
            PvrGlCache.Instance.Enable(PvrGl.ScissorTest);
            PvrGlCache.Instance.Scissor(rectangle.Origin.X, rectangle.Origin.Y, rectangle.Size.X, rectangle.Size.Y);
        }
        else
        {
            SetBaseClipping();
        }
        return mode;
    }

    private static void SetGpState(in PolyParam polygon, uint listType, bool sortingEnabled, uint cflip = 0)
    {
        float trilinearAlpha;
        if (polygon.Pcw.Texture != 0 && polygon.Tsp.FilterMode > 1
            && listType != ListType_Punch_Through && polygon.Tcw.MipMapped == 1)
        {
            trilinearAlpha = 0.25f * (polygon.Tsp.MipMapD & 3);
            if (polygon.Tsp.FilterMode == 2)
                trilinearAlpha = 1f - trilinearAlpha;
        }
        else
        {
            trilinearAlpha = 1f;
        }

        RenderContext context = Gl.RendContext!;
        bool colorClamp = polygon.Tsp.ColorClamp != 0
            && (context.FogClampMin.full != 0 || context.FogClampMax.full != uint.MaxValue);
        uint fogControl = PvrConfiguration.Fog ? polygon.Tsp.FogCtrl : 2;
        PvrRect clipRect = default;
        TileClipping mode = SetTileClip(polygon.TileClip, ref clipRect);
        var texture = (PvrGlTextureCacheData?)polygon.Texture;
        int gpuPalette = texture == null || !texture.GpuPalette ? 0 : (int)polygon.Tsp.FilterMode + 1;
        if (gpuPalette != 0)
        {
            if (PvrConfiguration.TextureFiltering == 1)
                gpuPalette = 1;
            else if (PvrConfiguration.TextureFiltering == 2)
                gpuPalette = 2;
        }

        PvrGlPipelineShader shader = _currentShader = GetProgram(listType == ListType_Punch_Through,
            mode == TileClipping.Inside, polygon.Pcw.Texture != 0, polygon.Tsp.UseAlpha != 0, polygon.Tsp.IgnoreTexA != 0,
            polygon.Tsp.ShadInstr, polygon.Pcw.Offset != 0, fogControl, polygon.Pcw.Gouraud != 0,
            polygon.Tcw.PixelFmt == (uint)PixelFormat.PixelBumpMap, colorClamp, trilinearAlpha != 1f,
            gpuPalette, polygon.IsNaomi2(), ShaderUniforms.Dithering);

        PvrGlCache cache = PvrGlCache.Instance;
        cache.UseProgram(shader.Program);
        if (shader.TrilinearAlpha != -1)
            PvrGl.Uniform1f(shader.TrilinearAlpha, trilinearAlpha);
        if (gpuPalette != 0)
        {
            int paletteIndex = polygon.Tcw.PixelFmt == (uint)PixelFormat.PixelPal4
                ? (int)(polygon.Tcw.PalSelect << 4) : (int)((polygon.Tcw.PalSelect >> 4) << 8);
            PvrGl.Uniform1i(shader.PaletteIndex, paletteIndex);
            if (gpuPalette == 2 && shader.TexSize != -1)
            {
                float* size = stackalloc float[2] { texture!.Width, texture.Height };
                PvrGl.Uniform2fv(shader.TexSize, 1, size);
            }
        }
        if (mode == TileClipping.Inside)
            PvrGl.Uniform4f(shader.ClipTest, clipRect.Origin.X, clipRect.Origin.Y, clipRect.BottomRight().X, clipRect.BottomRight().Y);
        if (PvrConfiguration.ModifierVolumes)
        {
            uint stencil = polygon.Pcw.Shadow != 0 ? 0x80u : 0;
            cache.StencilFunc(PvrGl.Always, (int)stencil, stencil);
        }
        if (texture != null)
        {
            cache.BindTexture(PvrGl.Texture2D, texture.TextureId);
            SetTextureRepeatMode(PvrGl.TextureWrapS, polygon.Tsp.ClampU, polygon.Tsp.FlipU);
            SetTextureRepeatMode(PvrGl.TextureWrapT, polygon.Tsp.ClampV, polygon.Tsp.FlipV);
            bool nearestFilter;
            if (gpuPalette != 0)
                nearestFilter = true;
            else if (PvrConfiguration.TextureFiltering == 0)
                nearestFilter = polygon.Tsp.FilterMode == 0;
            else if (PvrConfiguration.TextureFiltering == 1)
                nearestFilter = true;
            else
                nearestFilter = false;
            bool mipmapped = texture.IsMipmapped();

            if (nearestFilter)
            {
                cache.TexParameteri(PvrGl.Texture2D, PvrGl.TextureMinFilter, PvrGl.Nearest);
                cache.TexParameteri(PvrGl.Texture2D, PvrGl.TextureMagFilter, PvrGl.Nearest);
            }
            else
            {
                cache.TexParameteri(PvrGl.Texture2D, PvrGl.TextureMinFilter,
                    mipmapped && listType != ListType_Punch_Through ? PvrGl.LinearMipmapNearest : PvrGl.Linear);
                cache.TexParameteri(PvrGl.Texture2D, PvrGl.TextureMagFilter, PvrGl.Linear);
            }
            if (!Gl.IsGles && Gl.Major >= 3 && mipmapped)
                PvrGl.TexParameterf(PvrGl.Texture2D, PvrGl.TextureLodBias, PvrTextureCache.DAdjustLodBias[polygon.Tsp.MipMapD]);
            if (Gl.MaxAnisotropy > 1f)
            {
                if (PvrConfiguration.AnisotropicFiltering > 1 && !nearestFilter)
                {
                    PvrGl.TexParameterf(PvrGl.Texture2D, PvrGl.TextureMaxAnisotropy,
                        Math.Min(PvrConfiguration.AnisotropicFiltering, Gl.MaxAnisotropy));
                    if (mipmapped && listType != ListType_Punch_Through)
                        cache.TexParameteri(PvrGl.Texture2D, PvrGl.TextureMinFilter, PvrGl.LinearMipmapLinear);
                }
                else
                    PvrGl.TexParameterf(PvrGl.Texture2D, PvrGl.TextureMaxAnisotropy, 1f);
            }
        }
        cache.Enable(PvrGl.Blend);
        cache.BlendFunc(SrcBlendGl[polygon.Tsp.SrcInstr], DstBlendGl[polygon.Tsp.DstInstr]);
        SetCull(polygon.Isp.CullMode ^ cflip ^ 1);
        if (listType == ListType_Punch_Through || (listType == ListType_Translucent && sortingEnabled))
            cache.DepthFunc(ZFunction[6]);
        else
            cache.DepthFunc(ZFunction[polygon.Isp.DepthMode]);

        if (sortingEnabled)
            cache.DepthMask(0);
        else
            cache.DepthMask(listType == ListType_Punch_Through || polygon.Isp.ZWriteDis == 0 ? (byte)1 : (byte)0);
        if (shader.Naomi2)
            PvrGlNaomi2.SetN2Uniforms(polygon, shader, context);
    }

    private static void DrawList(List<PolyParam> polygons, uint first, uint count, uint listType, bool sortingEnabled)
    {
        if (count == 0)
            return;
        PvrGlCache cache = PvrGlCache.Instance;
        cache.Enable(PvrGl.StencilTest);
        cache.StencilFunc(PvrGl.Always, 0, 0);
        cache.StencilOp(PvrGl.Keep, PvrGl.Keep, PvrGl.Replace);

        for (uint i = first; count > 0; count--, i++)
        {
            PolyParam polygon = polygons[(int)i];
            if (polygon.Count < 3)
                continue;
            if ((listType == ListType_Opaque || (listType == ListType_Translucent && !sortingEnabled)) && polygon.Isp.DepthMode == 0)
                continue;
            SetGpState(polygon, listType, sortingEnabled);
            PvrGl.DrawElements(PvrGl.TriangleStrip, (int)polygon.Count, Gl.IndexType, (nint)(Gl.GetIndexSize() * polygon.First));
            Check();
        }
    }

    private static void DrawSorted(uint first, uint count, bool multipass)
    {
        if (count == 0)
            return;
        PvrGlCache cache = PvrGlCache.Instance;
        cache.Enable(PvrGl.StencilTest);
        cache.StencilFunc(PvrGl.Always, 0, 0);
        cache.StencilOp(PvrGl.Keep, PvrGl.Keep, PvrGl.Replace);
        RenderContext context = Gl.RendContext!;
        uint end = first + count;
        for (uint i = first; i < end; i++)
        {
            SortedTriangle triangle = context.SortedTriangles[(int)i];
            PolyParam polygon = context.GlobalParamTr[(int)triangle.PolyIndex];
            SetGpState(polygon, ListType_Translucent, true);
            PvrGl.DrawElements(PvrGl.Triangles, (int)triangle.Count, Gl.IndexType, (nint)(Gl.GetIndexSize() * triangle.First));
        }
        if (multipass && PvrConfiguration.TranslucentPolygonDepthMask)
        {
            PvrGl.ColorMask(0, 0, 0, 0);
            cache.Disable(PvrGl.Blend);
            cache.StencilMask(0);
            cache.UseProgram(Gl.ModvolShader.Program);
            PvrGl.Uniform1f(Gl.ModvolShader.ShaderColor, 1f);
            cache.DepthFunc(PvrGl.Gequal);
            cache.DepthMask(1);
            for (uint i = first; i < end; i++)
            {
                SortedTriangle triangle = context.SortedTriangles[(int)i];
                PolyParam polygon = context.GlobalParamTr[(int)triangle.PolyIndex];
                if (polygon.Isp.ZWriteDis == 0)
                {
                    SetCull(polygon.Isp.CullMode ^ 1);
                    PvrGl.DrawElements(PvrGl.Triangles, (int)triangle.Count, Gl.IndexType, (nint)(Gl.GetIndexSize() * triangle.First));
                }
            }
            cache.StencilMask(0xff);
            PvrGl.ColorMask(1, 1, 1, 1);
        }
    }

    internal static void SetMvsMode(PvrGlModifierVolumeMode mode, ISP_Modvol isp)
    {
        PvrGlCache cache = PvrGlCache.Instance;
        if (mode == PvrGlModifierVolumeMode.Xor)
        {
            cache.Enable(PvrGl.DepthTest);
            cache.StencilMask(2);
            cache.StencilFunc(PvrGl.Always, 0, 2);
            cache.StencilOp(PvrGl.Keep, PvrGl.Keep, PvrGl.Invert);
            SetCull(isp.CullMode);
        }
        else if (mode == PvrGlModifierVolumeMode.Or)
        {
            cache.Enable(PvrGl.DepthTest);
            cache.StencilMask(2);
            cache.StencilFunc(PvrGl.Always, 2, 2);
            cache.StencilOp(PvrGl.Keep, PvrGl.Keep, PvrGl.Replace);
            SetCull(isp.CullMode);
        }
        else
        {
            cache.Disable(PvrGl.DepthTest);
            cache.StencilMask(3);
            if (mode == PvrGlModifierVolumeMode.Inclusion)
            {
                cache.StencilFunc(PvrGl.Lequal, 1, 3);
                cache.StencilOp(PvrGl.Zero, PvrGl.Zero, PvrGl.Replace);
            }
            else
            {
                cache.StencilFunc(PvrGl.Equal, 1, 3);
                cache.StencilOp(PvrGl.Zero, PvrGl.Zero, PvrGl.Keep);
            }
        }
    }

    internal static void SetupMainVbo()
    {
        Gl.Vbo.MainVao.Bind(Gl.Vbo.Geometry!, Gl.Vbo.Indices);
        Check();
    }

    private static void SetupModvolVbo()
    {
        Gl.Vbo.ModvolVao.Bind(Gl.Vbo.Modvols!);
    }

    internal static void DrawModVols(uint first, uint count)
    {
        RenderContext context = Gl.RendContext!;
        if (count == 0 || context.ModTrig.Count == 0)
            return;

        SetupModvolVbo();
        PvrGlCache cache = PvrGlCache.Instance;
        cache.Disable(PvrGl.Blend);
        SetBaseClipping();
        cache.Enable(PvrGl.DepthTest);
        cache.DepthMask(0);
        cache.DepthFunc(PvrGl.Greater);
        PvrGl.ColorMask(0, 0, 0, 0);

        int modBase = -1;
        int currentMvMat = -1;
        int currentProjMat = -1;
        for (uint i = 0; i < count; i++)
        {
            ModifierVolumeParam param = context.GlobalParamMvo[(int)(first + i)];
            if (param.Count == 0)
                continue;
            if (param.IsNaomi2())
            {
                cache.UseProgram(Gl.N2ModvolShader.Program);
                if (param.MvMatrix != currentMvMat)
                {
                    currentMvMat = param.MvMatrix;
                    fixed (float* matrix = CollectionsMarshal.AsSpan(context.Matrices)[currentMvMat].Mat)
                        PvrGl.UniformMatrix4fv(Gl.N2ModvolShader.MvMat, 1, 0, matrix);
                }
                if (param.ProjMatrix != currentProjMat)
                {
                    currentProjMat = param.ProjMatrix;
                    fixed (float* matrix = CollectionsMarshal.AsSpan(context.Matrices)[currentProjMat].Mat)
                        PvrGl.UniformMatrix4fv(Gl.N2ModvolShader.ProjMat, 1, 0, matrix);
                }
            }
            else
            {
                cache.UseProgram(Gl.ModvolShader.Program);
            }
            PvrRect clipRect = default;
            SetTileClip(param.TileClip, ref clipRect);
            uint mode = param.Isp.DepthMode;
            if (modBase == -1)
                modBase = (int)param.First;
            SetMvsMode(param.Isp.VolumeLast == 0 && mode > 0 ? PvrGlModifierVolumeMode.Or : PvrGlModifierVolumeMode.Xor, param.Isp);
            PvrGl.DrawArrays(PvrGl.Triangles, (int)param.First * 3, (int)param.Count * 3);
            if (mode == 1 || mode == 2)
            {
                SetMvsMode(mode == 1 ? PvrGlModifierVolumeMode.Inclusion : PvrGlModifierVolumeMode.Exclusion, param.Isp);
                PvrGl.DrawArrays(PvrGl.Triangles, modBase * 3, ((int)(param.First + param.Count) - modBase) * 3);
                modBase = -1;
            }
        }

        SetCull(0);
        PvrGl.ColorMask(1, 1, 1, 1);
        SetBaseClipping();
        cache.Enable(PvrGl.Blend);
        cache.BlendFunc(PvrGl.SrcAlpha, PvrGl.OneMinusSrcAlpha);
        cache.Enable(PvrGl.StencilTest);
        cache.StencilFunc(PvrGl.Equal, 0x81, 0x81);
        cache.StencilMask(0x3);
        cache.StencilOp(PvrGl.Zero, PvrGl.Zero, PvrGl.Zero);
        cache.Disable(PvrGl.DepthTest);
        SetupMainVbo();
        PvrGl.DrawArrays(PvrGl.TriangleStrip, 0, 4);
        cache.Enable(PvrGl.DepthTest);
    }

    internal static void DrawStrips()
    {
        SetupMainVbo();
        PvrGl.ActiveTexture(PvrGl.Texture0);
        RenderContext context = Gl.RendContext!;
        RenderPass previous = default;
        for (int passIndex = 0; passIndex < context.RenderPasses.Count; passIndex++)
        {
            RenderPass current = context.RenderPasses[passIndex];
            PvrRuntime.Log.Debug("Render pass {0} OP {1} PT {2} TR {3} MV {4}", passIndex + 1,
                current.OpCount - previous.OpCount, current.PtCount - previous.PtCount,
                current.TrCount - previous.TrCount, current.MvoCount - previous.MvoCount);
            PvrGlCache.Instance.Enable(PvrGl.DepthTest);
            PvrGlCache.Instance.DepthMask(1);
            DrawList(context.GlobalParamOp, previous.OpCount, current.OpCount - previous.OpCount, ListType_Opaque, false);
            DrawList(context.GlobalParamPt, previous.PtCount, current.PtCount - previous.PtCount, ListType_Punch_Through, false);
            if (PvrConfiguration.ModifierVolumes)
                DrawModVols(previous.MvoCount, current.MvoCount - previous.MvoCount);
            if (current.AutoSort)
            {
                if (!PvrConfiguration.PerStripSorting)
                    DrawSorted(previous.SortedTrCount, current.SortedTrCount - previous.SortedTrCount,
                        passIndex < context.RenderPasses.Count - 1);
                else
                    DrawList(context.GlobalParamTr, previous.TrCount, current.TrCount - previous.TrCount, ListType_Translucent, true);
            }
            else
            {
                DrawList(context.GlobalParamTr, previous.TrCount, current.TrCount - previous.TrCount, ListType_Translucent, false);
            }
            previous = current;
        }
    }

    internal static void WriteFramebufferToVram()
    {
        RenderContext context = Gl.RendContext!;
        uint width = (uint)context.GlobClip.X;
        uint height = (uint)context.GlobClip.Y;
        PvrTransformMatrix.GetWriteFramebufferToVramParams(context, out PvrInt2 scaledSize, out PvrRect finalClip);
        PvrGlCache cache = PvrGlCache.Instance;
        if (scaledSize.X != (int)width || scaledSize.Y != (int)height)
        {
            int scaledWidth = scaledSize.X;
            int scaledHeight = scaledSize.Y;
            if (Gl.FbScaling.Framebuffer is { } framebuffer
                && (framebuffer.GetWidth() != scaledWidth || framebuffer.GetHeight() != scaledHeight))
            {
                framebuffer.Dispose();
                Gl.FbScaling.Framebuffer = null;
            }
            Gl.FbScaling.Framebuffer ??= new PvrGlFramebuffer(scaledWidth, scaledHeight);
            if (Gl.BogusBlitFramebuffer)
            {
                Gl.FbScaling.Framebuffer.Bind();
                PvrGl.Viewport(0, 0, scaledWidth, scaledHeight);
                cache.Disable(PvrGl.ScissorTest);
                cache.ClearColor(0f, 0f, 0f, 0f);
                PvrGl.Clear(PvrGl.ColorBufferBit);
                cache.BindTexture(PvrGl.Texture2D, Gl.Ofbo.Framebuffer!.GetTexture());
                cache.TexParameteri(PvrGl.Texture2D, PvrGl.TextureMinFilter, PvrGl.Linear);
                cache.TexParameteri(PvrGl.Texture2D, PvrGl.TextureMagFilter, PvrGl.Linear);
                cache.Disable(PvrGl.Blend);
                Gl.QuadDrawer!.Draw(Gl.Ofbo.Framebuffer.GetTexture(), false);
            }
            else
            {
                Gl.Ofbo.Framebuffer!.Bind(PvrGl.ReadFramebuffer);
                Gl.FbScaling.Framebuffer.Bind(PvrGl.DrawFramebuffer);
                cache.Disable(PvrGl.ScissorTest);
                PvrGl.BlitFramebuffer(0, 0, (int)width, (int)height, 0, 0, scaledWidth, scaledHeight, PvrGl.ColorBufferBit, PvrGl.Linear);
                Gl.FbScaling.Framebuffer.Bind();
            }
            width = (uint)scaledWidth;
            height = (uint)scaledHeight;
        }

        uint address = context.FbWSof1 & PvrMemory.VramMask;
        PvrGl.PixelStorei(PvrGl.PackAlignment, 1);
        uint lineStride = context.FbWLineStride * 8;
        using var buffer = new PvrPixelBuffer<uint>();
        buffer.Init(width, height);
        byte* pixels = (byte*)buffer.Data();
        PvrGl.ReadPixels(0, 0, (int)width, (int)height, PvrGl.Rgba, PvrGl.UnsignedByte, pixels);
        PvrTextureCache.WriteFramebuffer(width, height, pixels, address, context.FbWCtrl, lineStride, finalClip);
        PvrGl.BindFramebuffer(PvrGl.Framebuffer, Gl.Ofbo.OriginalFbo);
        Check();
    }

    private static readonly uint[] VmuTextureIds = new uint[8];
    private static uint _lightgunTextureId;
    private static readonly ulong[] VmuLastUpdated = new ulong[8];

    private static void UpdateVmuTexture(int vmuIndex)
    {
        PvrGlCache cache = PvrGlCache.Instance;
        if (VmuTextureIds[vmuIndex] == 0)
        {
            VmuTextureIds[vmuIndex] = cache.GenTexture();
            cache.BindTexture(PvrGl.Texture2D, VmuTextureIds[vmuIndex]);
            cache.TexParameteri(PvrGl.Texture2D, PvrGl.TextureMinFilter, PvrGl.Nearest);
            cache.TexParameteri(PvrGl.Texture2D, PvrGl.TextureMagFilter, PvrGl.Nearest);
        }
        else
        {
            cache.BindTexture(PvrGl.Texture2D, VmuTextureIds[vmuIndex]);
        }
        fixed (uint* pixels = &PvrOsd.VmuLcdData[vmuIndex, 0])
            PvrGl.TexImage2D(PvrGl.Texture2D, 0, PvrGl.Rgba, 48, 32, 0, PvrGl.Rgba, PvrGl.UnsignedByte, pixels);
        VmuLastUpdated[vmuIndex] = PvrOsd.VmuLastChanged[vmuIndex];
    }

    private static void DrawVmuTexture(int vmuIndex, int width, int height)
    {
        float padding = 8f * PvrRuntime.Display.UiScale;
        float vmuWidth = 96f * PvrRuntime.Display.UiScale;
        float vmuHeight = 64f * PvrRuntime.Display.UiScale;

        float x = (vmuIndex & 2) != 0 ? width - padding - vmuWidth : padding;
        float y;
        if ((vmuIndex & 4) != 0)
        {
            y = height - padding - vmuHeight;
            if ((vmuIndex & 1) != 0)
                y -= padding + vmuHeight;
        }
        else
        {
            y = padding;
            if ((vmuIndex & 1) != 0)
                y += padding + vmuHeight;
        }
        float* color = stackalloc float[4] { 0.75f, 0.75f, 0.75f, 0.75f };
        if (PvrOsd.VmuLastChanged[vmuIndex] != VmuLastUpdated[vmuIndex] || VmuTextureIds[vmuIndex] == 0)
            UpdateVmuTexture(vmuIndex);

        float x1 = (x + vmuWidth) * 2 / width - 1;
        float y1 = -(y + vmuHeight) * 2 / height + 1;
        x = x * 2 / width - 1;
        y = -y * 2 / height + 1;
        float* vertices = stackalloc float[20]
        {
            x, y1, 1f, 0f, 0f,
            x, y, 1f, 0f, 1f,
            x1, y1, 1f, 1f, 0f,
            x1, y, 1f, 1f, 1f,
        };
        PvrGlCache.Instance.Enable(PvrGl.Blend);
        PvrGlCache.Instance.BlendFunc(PvrGl.SrcAlpha, PvrGl.OneMinusSrcAlpha);
        Gl.QuadDrawer!.Draw(VmuTextureIds[vmuIndex], false, false, vertices, color);
    }

    private static void UpdateLightgunTexture()
    {
        if (_lightgunTextureId == 0)
        {
            PvrGlCache cache = PvrGlCache.Instance;
            _lightgunTextureId = cache.GenTexture();
            cache.BindTexture(PvrGl.Texture2D, _lightgunTextureId);
            cache.TexParameteri(PvrGl.Texture2D, PvrGl.TextureMinFilter, PvrGl.Nearest);
            cache.TexParameteri(PvrGl.Texture2D, PvrGl.TextureMagFilter, PvrGl.Nearest);
            fixed (uint* data = PvrOsd.GetCrosshairTextureData())
                PvrGl.TexImage2D(PvrGl.Texture2D, 0, PvrGl.Rgba, 16, 16, 0, PvrGl.Rgba, PvrGl.UnsignedByte, data);
        }
    }

    private static void DrawGunCrosshair(int port, int width, int height)
    {
        (float x, float y) = PvrOsd.GetCrosshairPosition(port);
        float halfWidth = PvrConfiguration.CrosshairSize * PvrRuntime.Display.UiScale / 2f;
        float halfHeight = halfWidth;

        UpdateLightgunTexture();
        float x1 = (x + halfWidth) * 2 / width - 1;
        float y1 = -(y + halfHeight) * 2 / height + 1;
        x = (x - halfWidth) * 2 / width - 1;
        y = -(y - halfHeight) * 2 / height + 1;
        float* vertices = stackalloc float[20]
        {
            x, y1, 1f, 0f, 0f,
            x, y, 1f, 0f, 1f,
            x1, y1, 1f, 1f, 0f,
            x1, y, 1f, 1f, 1f,
        };
        uint rgba = PvrConfiguration.CrosshairColor[port];
        float* color = stackalloc float[4]
        {
            (rgba & 0xff) / 255f,
            ((rgba >> 8) & 0xff) / 255f,
            ((rgba >> 16) & 0xff) / 255f,
            ((rgba >> 24) & 0xff) / 255f,
        };
        PvrGlCache.Instance.Enable(PvrGl.Blend);
        PvrGlCache.Instance.BlendFunc(PvrGl.SrcAlpha, PvrGl.OneMinusSrcAlpha);
        Gl.QuadDrawer!.Draw(_lightgunTextureId, false, false, vertices, color);
    }

    internal static void DrawVmusAndCrosshairs(int width, int height)
    {
        width = PvrRuntime.Display.Width;
        height = PvrRuntime.Display.Height;
        PvrGl.Viewport(0, 0, width, height);
        PvrGl.BindFramebuffer(PvrGl.Framebuffer, Gl.Ofbo.OriginalFbo);
        bool showVmus = PvrConfiguration.FloatVmus;
        if (Sh4DreamcastRuntime.Platform.IsConsole && showVmus)
        {
            for (int i = 0; i < 8; i++)
                if (PvrOsd.VmuLcdStatus[i])
                    DrawVmuTexture(i, width, height);
        }
        for (int i = 0; i < 4; i++)
            if (PvrOsd.CrosshairNeeded(i))
                DrawGunCrosshair(i, width, height);
        Check();
    }

    internal static void TermVmuLightgun()
    {
        fixed (uint* textures = VmuTextureIds)
            PvrGlCache.Instance.DeleteTextures(VmuTextureIds.Length, textures);
        Array.Clear(VmuTextureIds);
        fixed (uint* texture = &_lightgunTextureId)
            PvrGlCache.Instance.DeleteTextures(1, texture);
        _lightgunTextureId = 0;
    }
}

internal sealed unsafe partial class PvrGlMainVertexArray
{
    protected override void DefineVtxAttribs()
    {
        PvrGl.EnableVertexAttribArray(PvrGles.VertexPosArray);
        PvrGl.VertexAttribPointer(PvrGles.VertexPosArray, 3, PvrGl.Float, 0, sizeof(TaVertex), Marshal.OffsetOf<TaVertex>(nameof(TaVertex.X)));

        PvrGl.EnableVertexAttribArray(PvrGles.VertexColBaseArray);
        PvrGl.VertexAttribPointer(PvrGles.VertexColBaseArray, 4, PvrGl.UnsignedByte, 1, sizeof(TaVertex), Marshal.OffsetOf<TaVertex>(nameof(TaVertex.Col)));

        PvrGl.EnableVertexAttribArray(PvrGles.VertexColOffsArray);
        PvrGl.VertexAttribPointer(PvrGles.VertexColOffsArray, 4, PvrGl.UnsignedByte, 1, sizeof(TaVertex), Marshal.OffsetOf<TaVertex>(nameof(TaVertex.Spc)));

        PvrGl.EnableVertexAttribArray(PvrGles.VertexUvArray);
        PvrGl.VertexAttribPointer(PvrGles.VertexUvArray, 2, PvrGl.Float, 0, sizeof(TaVertex), Marshal.OffsetOf<TaVertex>(nameof(TaVertex.U)));

        PvrGl.EnableVertexAttribArray(PvrGles.VertexNormArray);
        PvrGl.VertexAttribPointer(PvrGles.VertexNormArray, 3, PvrGl.Float, 0, sizeof(TaVertex), Marshal.OffsetOf<TaVertex>(nameof(TaVertex.Nx)));
    }
}

internal sealed unsafe partial class PvrGlModvolVertexArray
{
    protected override void DefineVtxAttribs()
    {
        PvrGl.EnableVertexAttribArray(PvrGles.VertexPosArray);
        PvrGl.VertexAttribPointer(PvrGles.VertexPosArray, 3, PvrGl.Float, 0, sizeof(float) * 3, 0);
        PvrGl.DisableVertexAttribArray(PvrGles.VertexUvArray);
        PvrGl.DisableVertexAttribArray(PvrGles.VertexColOffsArray);
        PvrGl.DisableVertexAttribArray(PvrGles.VertexColBaseArray);
    }
}

internal unsafe partial class PvrOpenGlRenderer
{
    private static readonly float[] ShiftedVertices =
    [
        -1f, -1f, 1f, 0f, 1f,
        -1f,  1f, 1f, 0f, 0f,
         1f, -1f, 1f, 1f, 1f,
         1f,  1f, 1f, 1f, 0f,
    ];

    private static readonly float[] RotatedVertices =
    [
        -1f,  1f, 1f, 1f, 0f,
        -1f, -1f, 1f, 1f, 1f,
         1f,  1f, 1f, 0f, 0f,
         1f, -1f, 1f, 0f, 1f,
    ];

    internal override void RenderFramebuffer(in PvrFramebufferInfo info)
    {
        PvrGlContext gl = PvrGles.Gl;
        gl.RendContext = null;
        PvrGles.GlReadFramebuffer(info);
        SaveCurrentFramebuffer();
        try
        {
            InitVideoRoutingFramebuffer();
            PvrTransformMatrix.GetVideoShift(out gl.Ofbo.ShiftX, out gl.Ofbo.ShiftY);
            if (gl.Ofbo2.Framebuffer is { } framebuffer
                && (gl.Dcfb.Width != framebuffer.GetWidth() || gl.Dcfb.Height != framebuffer.GetHeight()))
            {
                framebuffer.Dispose();
                gl.Ofbo2.Framebuffer = null;
            }
            if (gl.Ofbo2.Framebuffer == null)
                gl.Ofbo2.Framebuffer = new PvrGlFramebuffer(gl.Dcfb.Width, gl.Dcfb.Height, false, true);
            else
                gl.Ofbo2.Framebuffer.Bind();
            PvrGles.Check();
            gl.Ofbo2.Ready = true;
            Width = gl.Dcfb.Width;
            Height = gl.Dcfb.Height;
            gl.Ofbo.AspectRatio = PvrTransformMatrix.GetDcFramebufferAspectRatio();

            PvrGl.Viewport(0, 0, gl.Dcfb.Width, gl.Dcfb.Height);
            PvrGlCache cache = PvrGlCache.Instance;
            cache.Disable(PvrGl.ScissorTest);
            if (info.ReadControl.fb_enable == 0 || info.VideoControl.blank_video == 1)
            {
                cache.ClearColor(info.VideoBorderColor.Red(), info.VideoBorderColor.Green(), info.VideoBorderColor.Blue(), 1f);
                PvrGl.Clear(PvrGl.ColorBufferBit);
            }
            else
            {
                cache.Disable(PvrGl.Blend);
                gl.QuadDrawer!.Draw(gl.Dcfb.Texture, false, false);
            }
            RenderLastFrameCore();
            DrawOsd();
            FrameRendered = true;
            ClearLastFrame = false;
            RenderVideoRouting();
        }
        finally
        {
            RestoreCurrentFramebuffer();
        }
    }

    protected static bool RenderLastFrameCore()
    {
        PvrGlContext gl = PvrGles.Gl;
        PvrGlFramebuffer? framebuffer = gl.Ofbo2.Ready ? gl.Ofbo2.Framebuffer : gl.Ofbo.Framebuffer;
        if (framebuffer == null)
            return false;

        int dx = 0;
        int dy = 0;
        PvrGlCache cache = PvrGlCache.Instance;
        cache.Disable(PvrGl.ScissorTest);
        int width = PvrRuntime.Display.Width;
        int height = PvrRuntime.Display.Height;
        PvrTransformMatrix.GetWindowboxDimensions(width, height, gl.Ofbo.AspectRatio, ref dx, ref dy, PvrConfiguration.Rotate90);
        if (gl.BogusBlitFramebuffer || PvrConfiguration.Rotate90)
        {
            PvrGl.Viewport(dx, dy, width - dx * 2, height - dy * 2);
            PvrGl.BindFramebuffer(PvrGl.Framebuffer, gl.Ofbo.OriginalFbo);
            cache.ClearColor(VO_BORDER_COL.Red(), VO_BORDER_COL.Green(), VO_BORDER_COL.Blue(), 1f);
            PvrGl.Clear(PvrGl.ColorBufferBit);
            cache.TexParameteri(PvrGl.Texture2D, PvrGl.TextureMinFilter, PvrConfiguration.LinearInterpolation ? PvrGl.Linear : PvrGl.Nearest);
            cache.TexParameteri(PvrGl.Texture2D, PvrGl.TextureMagFilter, PvrConfiguration.LinearInterpolation ? PvrGl.Linear : PvrGl.Nearest);
            float[]? vertices = null;
            if (gl.Ofbo.ShiftX != 0 || gl.Ofbo.ShiftY != 0)
            {
                ShiftedVertices[0] = ShiftedVertices[5] = -1f + gl.Ofbo.ShiftX * 2f / framebuffer.GetWidth();
                ShiftedVertices[10] = ShiftedVertices[15] = ShiftedVertices[0] + 2;
                ShiftedVertices[1] = ShiftedVertices[11] = -1f - gl.Ofbo.ShiftY * 2f / framebuffer.GetHeight();
                ShiftedVertices[6] = ShiftedVertices[16] = ShiftedVertices[1] + 2;
                vertices = ShiftedVertices;
            }
            cache.Disable(PvrGl.Blend);
            fixed (float* pixels = vertices)
                gl.QuadDrawer!.Draw(framebuffer.GetTexture(), PvrConfiguration.Rotate90, true, pixels);
        }
        else
        {
            framebuffer.Bind(PvrGl.ReadFramebuffer);
            PvrGl.BindFramebuffer(PvrGl.DrawFramebuffer, gl.Ofbo.OriginalFbo);
            cache.ClearColor(VO_BORDER_COL.Red(), VO_BORDER_COL.Green(), VO_BORDER_COL.Blue(), 1f);
            PvrGl.Clear(PvrGl.ColorBufferBit);
            PvrGl.BlitFramebuffer((int)-gl.Ofbo.ShiftX, (int)-gl.Ofbo.ShiftY,
                (int)(framebuffer.GetWidth() - gl.Ofbo.ShiftX), (int)(framebuffer.GetHeight() - gl.Ofbo.ShiftY),
                dx, height - dy, width - dx, dy, PvrGl.ColorBufferBit,
                PvrConfiguration.LinearInterpolation ? PvrGl.Linear : PvrGl.Nearest);
            PvrGl.BindFramebuffer(PvrGl.Framebuffer, gl.Ofbo.OriginalFbo);
        }
        return true;
    }

    internal override bool GetLastFrame(List<byte> data, ref int width, ref int height)
    {
        PvrGlContext gl = PvrGles.Gl;
        PvrGlFramebuffer? framebuffer = gl.Ofbo2.Ready ? gl.Ofbo2.Framebuffer : gl.Ofbo.Framebuffer;
        if (framebuffer == null)
            return false;
        if (width != 0)
        {
            height = (int)(width / gl.Ofbo.AspectRatio);
        }
        else if (height != 0)
        {
            width = (int)(gl.Ofbo.AspectRatio * height);
        }
        else
        {
            width = framebuffer.GetWidth();
            height = framebuffer.GetHeight();
            if (PvrConfiguration.Rotate90)
                (width, height) = (height, width);
            int adjustedWidth = (int)(gl.Ofbo.AspectRatio * height);
            if (width > adjustedWidth)
                height = (int)(width / gl.Ofbo.AspectRatio);
            else
                width = adjustedWidth;
        }

        using var destination = new PvrGlFramebuffer(width, height, false, false);
        PvrGl.Viewport(0, 0, width, height);
        PvrGlCache.Instance.Disable(PvrGl.Blend);
        if (framebuffer.GetTexture() == 0)
        {
            PvrRuntime.Log.Warn("GetLastFrame: framebuffer->texture is 0");
            return false;
        }
        float[]? vertices = PvrConfiguration.Rotate90 ? RotatedVertices : null;
        fixed (float* coords = vertices)
            gl.QuadDrawer!.Draw(framebuffer.GetTexture(), PvrConfiguration.Rotate90, false, coords);

        CollectionsMarshal.SetCount(data, width * height * 3);
        PvrGl.PixelStorei(PvrGl.PackAlignment, 1);
        fixed (byte* output = CollectionsMarshal.AsSpan(data))
        {
            if (gl.IsGles)
            {
                byte[] temporary = new byte[width * height * 4];
                fixed (byte* pixels = temporary)
                {
                    PvrGl.ReadPixels(0, 0, width, height, PvrGl.Rgba, PvrGl.UnsignedByte, pixels);
                    byte* dst = output;
                    byte* src = pixels;
                    while (src <= pixels + temporary.Length - 1)
                    {
                        *dst++ = *src++;
                        *dst++ = *src++;
                        *dst++ = *src++;
                        src++;
                    }
                }
            }
            else
            {
                PvrGl.ReadPixels(0, 0, width, height, PvrGl.Rgb, PvrGl.UnsignedByte, output);
            }
        }
        RestoreCurrentFramebuffer();
        PvrGles.Check();
        return true;
    }
}
