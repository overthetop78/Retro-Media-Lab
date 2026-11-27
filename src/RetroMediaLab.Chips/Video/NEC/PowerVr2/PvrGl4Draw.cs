using System.Runtime.InteropServices;
using static RetroMediaLab.Chips.Video.NEC.PowerVr2.PowerVr2Ta;
using static RetroMediaLab.Chips.Video.NEC.PowerVr2.PvrRegisters;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal static unsafe partial class PvrGl4
{
    private static PvrGl4PipelineShader? _currentShader;

    private static PvrGl4PipelineShader GetProgram(bool alphaTest, bool insideClipping, bool texture,
        bool useAlpha, bool ignoreTexA, uint shadInstr, bool offset, uint fogCtrl, bool twoVolumes,
        bool gouraud, bool bumpMap, bool fogClamping, int palette, bool naomi2, PvrGl4Pass pass)
    {
        uint key = 0;
        key |= insideClipping ? 1u : 0u;
        key = (key << 1) | (alphaTest ? 1u : 0u);
        key = (key << 1) | (texture ? 1u : 0u);
        key = (key << 1) | (useAlpha ? 1u : 0u);
        key = (key << 1) | (ignoreTexA ? 1u : 0u);
        key = (key << 2) | shadInstr;
        key = (key << 1) | (offset ? 1u : 0u);
        key = (key << 2) | fogCtrl;
        key = (key << 1) | (twoVolumes ? 1u : 0u);
        key = (key << 1) | (gouraud ? 1u : 0u);
        key = (key << 1) | (bumpMap ? 1u : 0u);
        key = (key << 1) | (fogClamping ? 1u : 0u);
        key = (key << 2) | (uint)palette;
        key = (key << 1) | (naomi2 ? 1u : 0u);
        key = (key << 2) | (uint)pass;
        bool divPosZ = !PvrConfiguration.IsNaomi2 && PvrConfiguration.NativeDepthInterpolation;
        key = (key << 1) | (divPosZ ? 1u : 0u);

        if (!Gl.Shaders.TryGetValue(key, out PvrGl4PipelineShader? shader))
            Gl.Shaders.Add(key, shader = new PvrGl4PipelineShader());
        if (shader.Program == 0)
        {
            shader.AlphaTest = alphaTest;
            shader.InsideClipping = insideClipping;
            shader.Texture = texture;
            shader.UseAlpha = useAlpha;
            shader.IgnoreTexA = ignoreTexA;
            shader.ShadInstr = shadInstr;
            shader.Offset = offset;
            shader.FogCtrl = fogCtrl;
            shader.TwoVolumes = twoVolumes;
            shader.Gouraud = gouraud;
            shader.BumpMap = bumpMap;
            shader.FogClamping = fogClamping;
            shader.Palette = palette;
            shader.Naomi2 = naomi2;
            shader.Pass = pass;
            shader.DivPosZ = divPosZ;
            CompilePipelineShader(shader);
        }
        return shader;
    }

    private static void SetTextureRepeatMode(int index, uint direction, uint clamp, uint mirror)
    {
        if (clamp != 0)
            PvrGl.SamplerParameteri(TextureSamplers[index], direction, PvrGl.ClampToEdge);
        else
            PvrGl.SamplerParameteri(TextureSamplers[index], direction, mirror != 0 ? PvrGl.MirroredRepeat : PvrGl.Repeat);
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

    private static void SetGpState(in PolyParam polygon, uint listType, bool sortingEnabled, PvrGl4Pass pass)
    {
        if (polygon.Pcw.Texture != 0 && polygon.Tsp.FilterMode > 1
            && listType != ListType_Punch_Through && polygon.Tcw.MipMapped == 1)
        {
            ShaderUniforms.TrilinearAlpha = 0.25f * (polygon.Tsp.MipMapD & 3);
            if (polygon.Tsp.FilterMode == 2)
                ShaderUniforms.TrilinearAlpha = 1.0f - ShaderUniforms.TrilinearAlpha;
        }
        else
        {
            ShaderUniforms.TrilinearAlpha = 1.0f;
        }

        PvrGlContext gl = PvrGles.Gl;
        RenderContext context = gl.RendContext!;
        PvrRect clipRect = default;
        TileClipping clipMode = gl.Matrices.GetTileClip(polygon.TileClip, ref clipRect);
        int gpuPalette = polygon.Texture == null || !polygon.Texture.GpuPalette ? 0 : (int)polygon.Tsp.FilterMode + 1;
        if (gpuPalette != 0)
        {
            if (PvrConfiguration.TextureFiltering == 1)
                gpuPalette = 1;
            else if (PvrConfiguration.TextureFiltering == 2)
                gpuPalette = 2;
        }

        if (pass == PvrGl4Pass.Depth)
        {
            if (listType != ListType_Punch_Through)
                gpuPalette = 0;
            _currentShader = GetProgram(listType == ListType_Punch_Through,
                clipMode == TileClipping.Inside, listType == ListType_Punch_Through && polygon.Pcw.Texture != 0,
                true, polygon.Tsp.IgnoreTexA != 0, 0, false, 2, false, false, false, false,
                gpuPalette, polygon.IsNaomi2(), pass);
        }
        else
        {
            bool twoVolumesMode = polygon.Tsp1.full != uint.MaxValue && listType != ListType_Translucent;
            bool colorClamp = polygon.Tsp.ColorClamp != 0
                && (context.FogClampMin.full != 0 || context.FogClampMax.full != uint.MaxValue);
            uint fogControl = PvrConfiguration.Fog ? polygon.Tsp.FogCtrl : 2;
            _currentShader = GetProgram(listType == ListType_Punch_Through,
                clipMode == TileClipping.Inside, polygon.Pcw.Texture != 0, polygon.Tsp.UseAlpha != 0,
                polygon.Tsp.IgnoreTexA != 0, polygon.Tsp.ShadInstr, polygon.Pcw.Offset != 0,
                fogControl, twoVolumesMode, polygon.Pcw.Gouraud != 0, polygon.Tcw.PixelFmt == (uint)PixelFormat.PixelBumpMap,
                colorClamp, gpuPalette, polygon.IsNaomi2(), pass);
        }
        PvrGl4PipelineShader shader = _currentShader;
        PvrGlCache cache = PvrGlCache.Instance;
        cache.UseProgram(shader.Program);

        if (gpuPalette != 0)
        {
            if (polygon.Tcw.PixelFmt == (uint)PixelFormat.PixelPal4)
                ShaderUniforms.PaletteIndex = (int)(polygon.Tcw.PalSelect << 4);
            else
                ShaderUniforms.PaletteIndex = (int)((polygon.Tcw.PalSelect >> 4) << 8);
        }

        ShaderUniforms.Tsp0 = polygon.Tsp;
        ShaderUniforms.Tsp1 = polygon.Tsp1;
        ShaderUniforms.Tcw0 = polygon.Tcw;
        ShaderUniforms.Tcw1 = polygon.Tcw1;
        ShaderUniforms.Set(shader);

        if (pass == PvrGl4Pass.Color)
        {
            cache.Enable(PvrGl.Blend);
            cache.BlendFunc(PvrGles.SrcBlendGl[polygon.Tsp.SrcInstr], PvrGles.DstBlendGl[polygon.Tsp.DstInstr]);
        }
        else
            cache.Disable(PvrGl.Blend);

        if (clipMode == TileClipping.Inside)
            PvrGl.Uniform4f(shader.ClipTest, clipRect.Origin.X, clipRect.Origin.Y, clipRect.BottomRight().X, clipRect.BottomRight().Y);
        if (clipMode == TileClipping.Outside)
        {
            cache.Enable(PvrGl.ScissorTest);
            cache.Scissor(clipRect.Origin.X, clipRect.Origin.Y, clipRect.Size.X, clipRect.Size.Y);
        }
        else
            SetBaseClipping();

        uint stencil = polygon.Pcw.Shadow != 0 ? 0x80u : 0;
        cache.StencilFunc(PvrGl.Always, (int)stencil, stencil);

        if (shader.Texture)
        {
            for (int i = 0; i < 2; i++)
            {
                PvrGl.ActiveTexture(PvrGl.Texture0 + (uint)i);
                var texture = (PvrGlTextureCacheData?)(i == 0 ? polygon.Texture : polygon.Texture1);
                if (i == 0)
                    cache.BindTexture(PvrGl.Texture2D, texture?.TextureId ?? 0);
                else
                    PvrGl.BindTexture(PvrGl.Texture2D, texture?.TextureId ?? 0);
                if (texture != null)
                {
                    TSP tsp = i == 0 ? polygon.Tsp : polygon.Tsp1;
                    PvrGl.BindSampler((uint)i, TextureSamplers[i]);
                    SetTextureRepeatMode(i, PvrGl.TextureWrapS, tsp.ClampU, tsp.FlipU);
                    SetTextureRepeatMode(i, PvrGl.TextureWrapT, tsp.ClampV, tsp.FlipV);

                    bool nearestFilter;
                    if (gpuPalette != 0)
                        nearestFilter = true;
                    else if (PvrConfiguration.TextureFiltering == 0)
                        nearestFilter = tsp.FilterMode == 0;
                    else if (PvrConfiguration.TextureFiltering == 1)
                        nearestFilter = true;
                    else
                        nearestFilter = false;

                    bool mipmapped = polygon.Tcw.MipMapped != 0 && polygon.Tcw.ScanOrder == 0 && PvrConfiguration.UseMipmaps;
                    if (nearestFilter)
                    {
                        PvrGl.SamplerParameteri(TextureSamplers[i], PvrGl.TextureMinFilter, PvrGl.Nearest);
                        PvrGl.SamplerParameteri(TextureSamplers[i], PvrGl.TextureMagFilter, PvrGl.Nearest);
                    }
                    else
                    {
                        PvrGl.SamplerParameteri(TextureSamplers[i], PvrGl.TextureMinFilter,
                            mipmapped && listType != ListType_Punch_Through ? PvrGl.LinearMipmapLinear : PvrGl.Linear);
                        PvrGl.SamplerParameteri(TextureSamplers[i], PvrGl.TextureMagFilter, PvrGl.Linear);
                    }
                    if (mipmapped && !gl.IsGles)
                        PvrGl.SamplerParameterf(TextureSamplers[i], PvrGl.TextureLodBias, PvrTextureCache.DAdjustLodBias[tsp.MipMapD]);

                    if (gl.MaxAnisotropy > 1f)
                    {
                        if (PvrConfiguration.AnisotropicFiltering > 1 && !nearestFilter)
                            PvrGl.TexParameterf(PvrGl.Texture2D, PvrGl.TextureMaxAnisotropy,
                                Math.Min(PvrConfiguration.AnisotropicFiltering, gl.MaxAnisotropy));
                        else
                            PvrGl.TexParameterf(PvrGl.Texture2D, PvrGl.TextureMaxAnisotropy, 1f);
                    }
                }
            }
            PvrGl.ActiveTexture(PvrGl.Texture0);
        }

        PvrGles.SetCull(polygon.Isp.CullMode ^ 1);
        if (listType == ListType_Punch_Through || (pass == PvrGl4Pass.Depth && sortingEnabled))
            cache.DepthFunc(PvrGl.Gequal);
        else if (listType == ListType_Opaque || (listType == ListType_Translucent && !sortingEnabled))
            cache.DepthFunc(PvrGles.ZFunction[polygon.Isp.DepthMode]);

        if (pass is PvrGl4Pass.Depth or PvrGl4Pass.Color)
        {
            if (listType == ListType_Punch_Through)
                cache.DepthMask(1);
            else
                cache.DepthMask(polygon.Isp.ZWriteDis == 0 ? (byte)1 : (byte)0);
        }
        else
            cache.DepthMask(0);
        if (polygon.IsNaomi2())
            PvrGlNaomi2.SetN2Uniforms(polygon, shader, context);
    }

    private static void DrawList(List<PolyParam> polygons, uint first, uint count, uint listType,
        bool sortingEnabled, PvrGl4Pass pass)
    {
        if (count == 0)
            return;
        uint firstVertexIndex = listType == ListType_Translucent ? PvrGles.Gl.RendContext!.Idx[(int)polygons[0].First] : 0;
        uint index = first;
        while (count-- > 0)
        {
            PolyParam polygon = polygons[(int)index];
            if (polygon.Count > 2)
            {
                if ((listType == ListType_Opaque || (listType == ListType_Translucent && !sortingEnabled))
                    && polygon.Isp.DepthMode == 0)
                {
                    index++;
                    continue;
                }
                if (pass == PvrGl4Pass.Oit && listType == ListType_Translucent
                    && polygon.Tsp.SrcInstr == 0 && polygon.Tsp.DstInstr == 1)
                {
                    index++;
                    continue;
                }
                ShaderUniforms.PolyNumber = unchecked((int)((index << 17) - firstVertexIndex));
                SetGpState(polygon, listType, sortingEnabled, pass);
                PvrGl.DrawElements(PvrGl.TriangleStrip, (int)polygon.Count, PvrGl.UnsignedInt, (nint)(sizeof(uint) * polygon.First));
                PvrGles.Check();
            }
            index++;
        }
    }

    internal static void SetupMainVbo() => Gl.Vbo.GetMainVao().Bind(Gl.Vbo.GetVertexBuffer(), Gl.Vbo.GetIndexBuffer());
    internal static void SetupModvolVbo() => Gl.Vbo.GetModVolVao().Bind(Gl.Vbo.GetModVolBuffer());

    private static void DrawModVols(uint first, uint count)
    {
        RenderContext context = PvrGles.Gl.RendContext!;
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
        for (uint i = 0; i < count; i++)
        {
            ModifierVolumeParam param = context.GlobalParamMvo[(int)(first + i)];
            if (param.Count == 0)
                continue;
            if (param.IsNaomi2())
            {
                cache.UseProgram(Gl.N2ModvolShader.Program);
                fixed (float* matrix = CollectionsMarshal.AsSpan(context.Matrices)[param.MvMatrix].Mat)
                    PvrGl.UniformMatrix4fv(Gl.N2ModvolShader.MvMat, 1, 0, matrix);
                fixed (float* matrix = CollectionsMarshal.AsSpan(context.Matrices)[param.ProjMatrix].Mat)
                    PvrGl.UniformMatrix4fv(Gl.N2ModvolShader.ProjMat, 1, 0, matrix);
            }
            else
                cache.UseProgram(Gl.ModvolShader.Program);
            uint mode = param.Isp.DepthMode;
            if (modBase == -1)
                modBase = (int)param.First;

            PvrGles.SetMvsMode(param.Isp.VolumeLast == 0 && mode > 0
                ? PvrGlModifierVolumeMode.Or : PvrGlModifierVolumeMode.Xor, param.Isp);
            PvrGl.DrawArrays(PvrGl.Triangles, (int)param.First * 3, (int)param.Count * 3);
            if (mode == 1 || mode == 2)
            {
                PvrGles.SetMvsMode(mode == 1 ? PvrGlModifierVolumeMode.Inclusion : PvrGlModifierVolumeMode.Exclusion, param.Isp);
                PvrGl.DrawArrays(PvrGl.Triangles, modBase * 3, ((int)(param.First + param.Count) - modBase) * 3);
                modBase = -1;
            }
        }

        SetupMainVbo();
        cache.Enable(PvrGl.DepthTest);
        cache.DepthMask(1);
    }

    private static uint CreateColorFboTexture(int width, int height)
    {
        PvrGlCache cache = PvrGlCache.Instance;
        uint texture = cache.GenTexture();
        cache.BindTexture(PvrGl.Texture2D, texture);
        cache.TexParameteri(PvrGl.Texture2D, PvrGl.TextureMinFilter, PvrGl.Nearest);
        cache.TexParameteri(PvrGl.Texture2D, PvrGl.TextureMagFilter, PvrGl.Nearest);
        PvrGl.TexImage2D(PvrGl.Texture2D, 0, PvrGl.Rgba, width, height, 0, PvrGl.Rgba, PvrGl.UnsignedByte, null);
        PvrGl.FramebufferTexture2D(PvrGl.Framebuffer, PvrGl.ColorAttachment0, PvrGl.Texture2D, texture, 0);
        PvrGles.Check();
        return texture;
    }

    internal static void CreateTextures(int width, int height)
    {
        if (GeometryFbo[0] == 0)
            fixed (uint* fbo = GeometryFbo)
                PvrGl.GenFramebuffers(2, fbo);
        PvrGlCache cache = PvrGlCache.Instance;
        StencilTextureId = cache.GenTexture();
        DepthTextureId = cache.GenTexture();
        for (int i = 0; i < 2; i++)
        {
            PvrGl.BindFramebuffer(PvrGl.Framebuffer, GeometryFbo[i]);
            OpaqueTextureId[i] = CreateColorFboTexture(width, height);

            uint depthStencil = i == 0 ? StencilTextureId : DepthTextureId;
            cache.BindTexture(PvrGl.Texture2D, depthStencil);
            PvrGles.Check();
            cache.TexParameteri(PvrGl.Texture2D, PvrGl.TextureMinFilter, PvrGl.Nearest);
            cache.TexParameteri(PvrGl.Texture2D, PvrGl.TextureMagFilter, PvrGl.Nearest);
            cache.TexParameteri(PvrGl.Texture2D, DepthStencilTextureMode, i == 0 ? (int)StencilIndex : PvrGl.DepthComponent);
            PvrGl.TexStorage2D(PvrGl.Texture2D, 1, PvrGles.Gl.Mali ? PvrGl.Depth24Stencil8 : PvrGl.Depth32fStencil8,
                width, height);
            PvrGl.FramebufferTexture2D(PvrGl.Framebuffer, PvrGl.DepthStencilAttachment, PvrGl.Texture2D, depthStencil, 0);
            PvrGles.Check();

            uint status = PvrGl.CheckFramebufferStatus(PvrGl.Framebuffer);
            if (status != PvrGl.FramebufferComplete)
            {
                PvrRuntime.Log.Error("Framebuffer creation failed: {0:x}", status);
                throw new PvrRendererException("OpenGL framebuffer creation failed");
            }
        }
    }

    internal static void DrawStrips(uint outputFbo, int width, int height)
    {
        CheckOverflowAndReset();
        RenderContext context = PvrGles.Gl.RendContext!;
        PvrGlCache cache = PvrGlCache.Instance;
        if (!context.IsRtt)
        {
            cache.Disable(PvrGl.ScissorTest);
            PvrGl.ColorMask(1, 1, 1, 1);
            if (context.ClearFramebuffer)
            {
                PvrGl.BindFramebuffer(PvrGl.Framebuffer, GeometryFbo[1]);
                cache.ClearColor(VO_BORDER_COL.Red(), VO_BORDER_COL.Green(), VO_BORDER_COL.Blue(), 1f);
                PvrGl.Clear(PvrGl.ColorBufferBit);
            }
            else
            {
                PvrGl.BindFramebuffer(PvrGl.DrawFramebuffer, GeometryFbo[1]);
                PvrGl.BindFramebuffer(PvrGl.ReadFramebuffer, outputFbo);
                PvrGl.BlitFramebuffer(0, 0, width, height, 0, 0, width, height, PvrGl.ColorBufferBit, PvrGl.Nearest);
                PvrGles.Check();
            }
            if (ShaderUniforms.BaseClipping.Enabled)
                cache.Enable(PvrGl.ScissorTest);
        }
        if (TextureSamplers[0] == 0)
            fixed (uint* samplers = TextureSamplers)
                PvrGl.GenSamplers(2, samplers);

        PvrGl.BindFramebuffer(PvrGl.Framebuffer, GeometryFbo[0]);
        cache.DepthMask(1);
        PvrGl.ClearDepthf(0f);
        cache.StencilMask(0xff);
        PvrGl.Clear(PvrGl.StencilBufferBit | PvrGl.DepthBufferBit);
        PvrGles.Check();

        PvrGl.ActiveTexture(PvrGl.Texture0);
        if (!PvrGles.Gl.IsGles)
            PvrGl.ProvokingVertex(PvrGl.LastVertexConvention);

        RenderPass previous = default;
        int passCount = context.RenderPasses.Count;
        for (int passIndex = 0; passIndex < passCount; passIndex++)
        {
            RenderPass current = context.RenderPasses[passIndex];
            bool skipOpPt = true;
            bool skipTr = true;
            for (uint i = previous.OpCount; skipOpPt && i < current.OpCount; i++)
                if (context.GlobalParamOp[(int)i].Count > 2)
                    skipOpPt = false;
            for (uint i = previous.PtCount; skipOpPt && i < current.PtCount; i++)
                if (context.GlobalParamPt[(int)i].Count > 2)
                    skipOpPt = false;
            for (uint i = previous.TrCount; skipTr && i < current.TrCount; i++)
                if (context.GlobalParamTr[(int)i].Count > 2)
                    skipTr = false;
            if (skipOpPt && skipTr)
            {
                previous = current;
                continue;
            }
            PvrRuntime.Log.Debug("Render pass {0}/{1} OP {2} PT {3} TR {4} MV {5} TMV {6} autosort {7}",
                passIndex + 1, passCount, current.OpCount - previous.OpCount, current.PtCount - previous.PtCount,
                current.TrCount - previous.TrCount, current.MvoCount - previous.MvoCount,
                current.MvOpTrShared ? current.MvoCount - previous.MvoCount : current.MvoTrCount - previous.MvoTrCount,
                current.AutoSort ? 1 : 0);
            SetupMainVbo();
            if (!skipOpPt)
            {
                PvrGl.BindFramebuffer(PvrGl.Framebuffer, GeometryFbo[0]);
                PvrGl.ColorMask(0, 0, 0, 0);
                cache.Enable(PvrGl.DepthTest);
                cache.DepthMask(1);
                cache.Enable(PvrGl.StencilTest);
                cache.StencilOp(PvrGl.Keep, PvrGl.Keep, PvrGl.Replace);
                DrawList(context.GlobalParamOp, previous.OpCount, current.OpCount - previous.OpCount, ListType_Opaque, false, PvrGl4Pass.Depth);
                DrawList(context.GlobalParamPt, previous.PtCount, current.PtCount - previous.PtCount, ListType_Punch_Through, false, PvrGl4Pass.Depth);
                if (PvrConfiguration.ModifierVolumes)
                    DrawModVols(previous.MvoCount, current.MvoCount - previous.MvoCount);

                PvrGl.BindFramebuffer(PvrGl.Framebuffer, GeometryFbo[1]);
                if (passIndex == 0)
                {
                    cache.DepthMask(1);
                    PvrGl.ClearDepthf(0f);
                    PvrGl.Clear(PvrGl.DepthBufferBit);
                }
                PvrGl.ColorMask(1, 1, 1, 1);
                cache.Disable(PvrGl.StencilTest);
                PvrGl.ActiveTexture(PvrGl.Texture3);
                PvrGl.BindTexture(PvrGl.Texture2D, StencilTextureId);
                PvrGl.ActiveTexture(PvrGl.Texture0);
                PvrGles.Check();
                DrawList(context.GlobalParamOp, previous.OpCount, current.OpCount - previous.OpCount, ListType_Opaque, false, PvrGl4Pass.Color);
                DrawList(context.GlobalParamPt, previous.PtCount, current.PtCount - previous.PtCount, ListType_Punch_Through, false, PvrGl4Pass.Color);
                PvrGl.ActiveTexture(PvrGl.Texture3);
                PvrGl.BindTexture(PvrGl.Texture2D, 0);
                PvrGl.ActiveTexture(PvrGl.Texture0);
            }

            if (!skipTr)
            {
                if (current.AutoSort)
                {
                    PvrGl.BindFramebuffer(PvrGl.Framebuffer, GeometryFbo[0]);
                    PvrGl.ColorMask(0, 0, 0, 0);
                    cache.Disable(PvrGl.DepthTest);
                    PvrGl.ActiveTexture(PvrGl.Texture2);
                    PvrGl.BindTexture(PvrGl.Texture2D, DepthTextureId);
                    PvrGl.ActiveTexture(PvrGl.Texture0);
                    DrawList(context.GlobalParamTr, previous.TrCount, current.TrCount - previous.TrCount, ListType_Translucent, true, PvrGl4Pass.Oit);
                    if (PvrConfiguration.ModifierVolumes)
                    {
                        SetBaseClipping();
                        if (current.MvOpTrShared)
                            DrawTranslucentModVols((int)previous.MvoCount, (int)(current.MvoCount - previous.MvoCount), true);
                        else
                            DrawTranslucentModVols((int)previous.MvoTrCount, (int)(current.MvoTrCount - previous.MvoTrCount), false);
                    }

                    if (passIndex < passCount - 1)
                    {
                        PvrGl.ActiveTexture(PvrGl.Texture2);
                        PvrGl.BindTexture(PvrGl.Texture2D, 0);
                        PvrGl.ActiveTexture(PvrGl.Texture0);
                        PvrGl.BindFramebuffer(PvrGl.Framebuffer, GeometryFbo[1]);
                        cache.Enable(PvrGl.DepthTest);
                        DrawList(context.GlobalParamTr, previous.TrCount, current.TrCount - previous.TrCount, ListType_Translucent, true, PvrGl4Pass.Depth);
                    }
                }
                else
                {
                    PvrGl.BindFramebuffer(PvrGl.Framebuffer, GeometryFbo[1]);
                    PvrGl.ColorMask(1, 1, 1, 1);
                    cache.Enable(PvrGl.DepthTest);
                    DrawList(context.GlobalParamTr, previous.TrCount, current.TrCount - previous.TrCount, ListType_Translucent, false, PvrGl4Pass.Color);
                    cache.Disable(PvrGl.Blend);
                }
                PvrGles.Check();
                if (passIndex < passCount - 1)
                {
                    PvrGl.BindFramebuffer(PvrGl.Framebuffer, GeometryFbo[0]);
                    PvrGl.ColorMask(1, 1, 1, 1);
                    PvrGl.ActiveTexture(PvrGl.Texture0);
                    PvrGl.BindSampler(0, 0);
                    cache.BindTexture(PvrGl.Texture2D, OpaqueTextureId[1]);
                    RenderABuffer(false);
                    cache.BindTexture(PvrGl.Texture2D, 0);
                    PvrGles.Check();
                    (OpaqueTextureId[0], OpaqueTextureId[1]) = (OpaqueTextureId[1], OpaqueTextureId[0]);
                    PvrGl.FramebufferTexture2D(PvrGl.Framebuffer, PvrGl.ColorAttachment0, PvrGl.Texture2D, OpaqueTextureId[0], 0);
                    PvrGl.BindFramebuffer(PvrGl.Framebuffer, GeometryFbo[1]);
                    PvrGl.FramebufferTexture2D(PvrGl.Framebuffer, PvrGl.ColorAttachment0, PvrGl.Texture2D, OpaqueTextureId[1], 0);
                }
            }
            if (!skipOpPt && passIndex < passCount - 1)
            {
                PvrGl.BindFramebuffer(PvrGl.Framebuffer, GeometryFbo[0]);
                cache.StencilMask(0xff);
                PvrGl.Clear(PvrGl.StencilBufferBit);
            }
            previous = current;
        }

        PvrGl.BindFramebuffer(PvrGl.Framebuffer, outputFbo);
        PvrGles.Check();
        PvrGl.ColorMask(1, 1, 1, 1);
        PvrGl.ActiveTexture(PvrGl.Texture0);
        PvrGl.BindSampler(0, 0);
        cache.BindTexture(PvrGl.Texture2D, OpaqueTextureId[1]);
        RenderABuffer(true);
    }
}

internal sealed unsafe partial class PvrGl4MainVertexArray
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

        PvrGl.EnableVertexAttribArray(PvrGles.VertexColBase1Array);
        PvrGl.VertexAttribPointer(PvrGles.VertexColBase1Array, 4, PvrGl.UnsignedByte, 1, sizeof(TaVertex), Marshal.OffsetOf<TaVertex>(nameof(TaVertex.Col1)));

        PvrGl.EnableVertexAttribArray(PvrGles.VertexColOffs1Array);
        PvrGl.VertexAttribPointer(PvrGles.VertexColOffs1Array, 4, PvrGl.UnsignedByte, 1, sizeof(TaVertex), Marshal.OffsetOf<TaVertex>(nameof(TaVertex.Spc1)));

        PvrGl.EnableVertexAttribArray(PvrGles.VertexUv1Array);
        PvrGl.VertexAttribPointer(PvrGles.VertexUv1Array, 2, PvrGl.Float, 0, sizeof(TaVertex), Marshal.OffsetOf<TaVertex>(nameof(TaVertex.U1)));

        PvrGl.EnableVertexAttribArray(PvrGles.VertexNormArray);
        PvrGl.VertexAttribPointer(PvrGles.VertexNormArray, 3, PvrGl.Float, 0, sizeof(TaVertex), Marshal.OffsetOf<TaVertex>(nameof(TaVertex.Nx)));
    }
}

internal sealed unsafe partial class PvrGl4ModvolVertexArray
{
    protected override void DefineVtxAttribs()
    {
        PvrGl.EnableVertexAttribArray(PvrGles.VertexPosArray);
        PvrGles.Check();
        PvrGl.VertexAttribPointer(PvrGles.VertexPosArray, 3, PvrGl.Float, 0, sizeof(float) * 3, 0);
        PvrGles.Check();
    }
}
