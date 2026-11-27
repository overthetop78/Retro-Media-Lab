using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;
using static RetroMediaLab.Chips.Video.NEC.PowerVr2.PowerVr2Ta;
using static RetroMediaLab.Chips.Video.NEC.PowerVr2.PvrRegisters;
using static RetroMediaLab.Chips.Video.NEC.PowerVr2.Vulkan.PvrVulkanImageLayout;
using static RetroMediaLab.Chips.Video.NEC.PowerVr2.Vulkan.PvrVulkanOitDescriptorSets;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Vulkan;

internal abstract unsafe partial class PvrVulkanOitDrawer
{
    private void DrawPoly(CommandBuffer commandBuffer, uint listType, bool autosort, PvrVulkanOitPass pass,
        in PolyParam poly, uint first, uint count, int polygonIndex)
    {
        using var scope = new PvrVulkanCommandBufferDebugScope(commandBuffer, "DrawPoly(OIT)", [0.25f, 0.50f, 0.25f, 1f]);
        SetTileClip(commandBuffer, poly.TileClip, out Rect2D scissorRect);

        float trilinearAlpha = 1f;
        if (poly.Tsp.FilterMode > 1 && poly.Pcw.Texture != 0 && listType != ListType_Punch_Through && poly.Tcw.MipMapped == 1)
        {
            trilinearAlpha = 0.25f * (poly.Tsp.MipMapD & 3);
            if (poly.Tsp.FilterMode == 2)
                trilinearAlpha = 1f - trilinearAlpha;
        }

        bool twoVolumes = poly.Tsp1.full != uint.MaxValue || poly.Tcw1.full != uint.MaxValue;

        int gpuPalette = poly.Texture == null || !poly.Texture.GpuPalette ? 0 : (int)poly.Tsp.FilterMode + 1;
        float paletteIndex = 0;
        if (gpuPalette != 0)
        {
            if (PvrConfiguration.TextureFiltering == 1)
                gpuPalette = 1;
            else if (PvrConfiguration.TextureFiltering == 2)
                gpuPalette = 2;
            paletteIndex = poly.Tcw.PixelFmt == (uint)PixelFormat.PixelPal4
                ? (poly.Tcw.PalSelect << 4) / 1023f : ((poly.Tcw.PalSelect >> 4) << 8) / 1023f;
        }

        PushConstants push = new()
        {
            ClipTest = new Vector4(scissorRect.Offset.X, scissorRect.Offset.Y,
                scissorRect.Offset.X + (float)scissorRect.Extent.Width,
                scissorRect.Offset.Y + (float)scissorRect.Extent.Height),
            TrilinearAlpha = trilinearAlpha,
            PaletteIndex = paletteIndex
        };
        push.BlendMode0[0] = (int)poly.Tsp.SrcInstr;
        push.BlendMode0[1] = (int)poly.Tsp.DstInstr;
        if (twoVolumes)
        {
            push.BlendMode1[0] = (int)poly.Tsp1.SrcInstr;
            push.BlendMode1[1] = (int)poly.Tsp1.DstInstr;
            push.ShadingInstr0 = (int)poly.Tsp.ShadInstr;
            push.ShadingInstr1 = (int)poly.Tsp1.ShadInstr;
            push.FogControl0 = (int)poly.Tsp.FogCtrl;
            push.FogControl1 = (int)poly.Tsp1.FogCtrl;
            push.UseAlpha0 = (int)poly.Tsp.UseAlpha;
            push.UseAlpha1 = (int)poly.Tsp1.UseAlpha;
            push.IgnoreTexAlpha0 = (int)poly.Tsp.IgnoreTexA;
            push.IgnoreTexAlpha1 = (int)poly.Tsp1.IgnoreTexA;
        }
        Vk api = GetContext().Api;
        PipelineLayout layout = _pipelineManager!.GetPipelineLayout();
        api.CmdPushConstants(commandBuffer, layout, ShaderStageFlags.FragmentBit, 0, (uint)sizeof(PushConstants), &push);
        if (!poly.IsNaomi2())
        {
            VtxPushConstants vtxPush = new();
            if (listType == ListType_Translucent)
            {
                uint firstVertexIdx = RendContext!.Idx[(int)RendContext.GlobalParamTr[0].First];
                vtxPush.PolyNumber = unchecked((polygonIndex << 17) - (int)firstVertexIdx);
            }
            api.CmdPushConstants(commandBuffer, layout, ShaderStageFlags.VertexBit, (uint)sizeof(PushConstants),
                (uint)sizeof(VtxPushConstants), &vtxPush);
        }

        if (poly.Pcw.Texture == 1 || poly.IsNaomi2())
        {
            ulong offset = 0;
            int polyNumber = 0;
            if (poly.IsNaomi2())
            {
                switch (listType)
                {
                    case ListType_Opaque:
                        offset = _offsets.Naomi2OpaqueOffset;
                        polyNumber = polygonIndex;
                        break;
                    case ListType_Punch_Through:
                        offset = _offsets.Naomi2PunchThroughOffset;
                        polyNumber = polygonIndex;
                        break;
                    case ListType_Translucent:
                        offset = _offsets.Naomi2TranslucentOffset;
                        polyNumber = polygonIndex;
                        break;
                }
            }
            _descriptorSets.BindPerPolyDescriptorSets(commandBuffer, poly, polyNumber, _curMainBuffer, offset,
                _offsets.LightsOffset, listType == ListType_Punch_Through);
        }

        Pipeline pipeline = _pipelineManager.GetPipeline(listType, autosort, poly, pass, gpuPalette);
        api.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, pipeline);
        api.CmdDrawIndexed(commandBuffer, count, 1, first, 0, 0);
    }

    private void DrawList(CommandBuffer commandBuffer, uint listType, bool sortTriangles, PvrVulkanOitPass pass,
        List<PolyParam> polygons, uint first, uint last)
    {
        if (first == last)
            return;
        using var scope = new PvrVulkanCommandBufferDebugScope(commandBuffer, "DrawList(OIT)", [0.50f, 0.25f, 0.50f, 1f]);
        Span<PolyParam> span = CollectionsMarshal.AsSpan(polygons);
        for (int index = (int)first; index < last; index++)
        {
            ref readonly PolyParam poly = ref span[index];
            if (poly.Count > 2)
                DrawPoly(commandBuffer, listType, sortTriangles, pass, poly, poly.First, poly.Count, index);
        }
    }

    private void DrawModifierVolumes(CommandBuffer commandBuffer, int first, int count,
        List<ModifierVolumeParam> volumes, bool translucent)
    {
        if (count == 0 || RendContext!.ModTrig.Count == 0 || !PvrConfiguration.ModifierVolumes)
            return;

        using var scope = new PvrVulkanCommandBufferDebugScope(commandBuffer, "DrawModVols(OIT)", [0.75f, 0.25f, 0.25f, 1f]);
        Vk api = GetContext().Api;
        BindMainVertexBuffer(commandBuffer, _offsets.ModVolOffset);
        SetScissor(commandBuffer, BaseScissor);
        Span<ModifierVolumeParam> parameters = CollectionsMarshal.AsSpan(volumes).Slice(first, count);
        int modBase = -1;
        for (int index = 0; index < count; index++)
        {
            ref readonly ModifierVolumeParam parameter = ref parameters[index];
            if (parameter.Count == 0)
                continue;

            uint mvMode = parameter.Isp.DepthMode;
            PvrRuntime.Verify(parameter.First + parameter.Count <= RendContext.ModTrig.Count);
            if (modBase == -1)
                modBase = (int)parameter.First;

            PvrVulkanModVolMode mode = parameter.Isp.VolumeLast == 0 && mvMode > 0 ? PvrVulkanModVolMode.Or : PvrVulkanModVolMode.Xor;
            Pipeline pipeline = translucent
                ? _pipelineManager!.GetTrModifierVolumePipeline(mode, (int)parameter.Isp.CullMode, parameter.IsNaomi2())
                : _pipelineManager!.GetModifierVolumePipeline(mode, (int)parameter.Isp.CullMode, parameter.IsNaomi2());
            api.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, pipeline);

            ulong uniformOffset = translucent ? _offsets.Naomi2TrModVolOffset : _offsets.Naomi2ModVolOffset;
            _descriptorSets.BindPerPolyDescriptorSets(commandBuffer, parameter, first + index, _curMainBuffer, uniformOffset);
            api.CmdDraw(commandBuffer, parameter.Count * 3, 1, parameter.First * 3, 0);

            if (mvMode is 1 or 2)
            {
                mode = mvMode == 1 ? PvrVulkanModVolMode.Inclusion : PvrVulkanModVolMode.Exclusion;
                if (translucent)
                {
                    ShaderWriteBarrier(commandBuffer);
                    pipeline = _pipelineManager.GetTrModifierVolumePipeline(mode, (int)parameter.Isp.CullMode, parameter.IsNaomi2());
                }
                else
                {
                    pipeline = _pipelineManager.GetModifierVolumePipeline(mode, (int)parameter.Isp.CullMode, parameter.IsNaomi2());
                }
                api.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, pipeline);
                api.CmdDraw(commandBuffer, (parameter.First + parameter.Count - (uint)modBase) * 3, 1, (uint)modBase * 3, 0);
                modBase = -1;
                if (translucent)
                    ShaderWriteBarrier(commandBuffer);
            }
        }
        BindMainVertexBuffer(commandBuffer, 0);
    }

    private void UploadMainBuffer(in VertexShaderUniforms vertexUniforms, in FragmentShaderUniforms fragmentUniforms)
    {
        var packer = new PvrVulkanBufferPacker();
        RenderContext context = RendContext!;
        fixed (TaVertex* vertices = CollectionsMarshal.AsSpan(context.Verts))
        fixed (ModTriangle* modifierVolumes = CollectionsMarshal.AsSpan(context.ModTrig))
        fixed (uint* indices = CollectionsMarshal.AsSpan(context.Idx))
        fixed (VertexShaderUniforms* vertexData = &vertexUniforms)
        fixed (FragmentShaderUniforms* fragmentData = &fragmentUniforms)
        fixed (N2LightModel* lights = CollectionsMarshal.AsSpan(context.LightModels))
        {
            packer.Add(vertices, (nuint)context.Verts.Count * (uint)sizeof(TaVertex));
            _offsets.ModVolOffset = packer.Add(modifierVolumes, (nuint)context.ModTrig.Count * (uint)sizeof(ModTriangle));
            _offsets.IndexOffset = packer.Add(indices, (nuint)context.Idx.Count * sizeof(uint));
            _offsets.VertexUniformOffset = packer.AddUniform(vertexData, (uint)sizeof(VertexShaderUniforms));
            _offsets.FragmentUniformOffset = packer.AddUniform(fragmentData, (uint)sizeof(FragmentShaderUniforms));

            uint[] trPolyParams = new uint[context.GlobalParamTr.Count == 0 ? 1 : context.GlobalParamTr.Count * 2];
            for (int poly = 0, i = 0; poly < context.GlobalParamTr.Count; poly++, i += 2)
            {
                PolyParam pp = context.GlobalParamTr[poly];
                trPolyParams[i] = (pp.Tsp.full & 0xffff00c0) | ((pp.Isp.full >> 16) & 0xe400) | ((pp.Pcw.full >> 7) & 1);
                trPolyParams[i + 1] = pp.Tsp1.full;
            }

            fixed (uint* trPolyData = trPolyParams)
            {
                _offsets.PolyParamsSize = (ulong)trPolyParams.Length * 4;
                _offsets.PolyParamsOffset = packer.AddStorage(trPolyData, (nuint)_offsets.PolyParamsSize);
                byte[]? naomi2Uniforms = null;
                if (PvrConfiguration.IsNaomi2)
                {
                    naomi2Uniforms = PackNaomi2Uniforms(packer, true, out _offsets.Naomi2OpaqueOffset,
                        out _offsets.Naomi2PunchThroughOffset, out _offsets.Naomi2TranslucentOffset,
                        out _offsets.Naomi2ModVolOffset, out _offsets.Naomi2TrModVolOffset);
                    _offsets.LightsOffset = PackNaomi2Lights(packer, lights);
                }

                PvrVulkanBuffer buffer = GetMainBuffer((uint)packer.Size());
                packer.Upload(buffer);
                _curMainBuffer = buffer.Buffer;
                GC.KeepAlive(naomi2Uniforms);
            }
        }
    }

    internal bool Draw(PvrVulkanTexture? fogTexture, PvrVulkanTexture? paletteTexture)
    {
        CommandBuffer commandBuffer = NewFrame();
        using var scope = new PvrVulkanCommandBufferDebugScope(commandBuffer, "Draw(OIT)", [0.75f, 0.75f, 0.75f, 1f]);
        PvrVulkanContext vulkan = GetContext();
        Vk api = vulkan.Api;
        RenderContext context = RendContext!;

        if (_needAttachmentTransition)
        {
            _needAttachmentTransition = false;
            foreach (PvrVulkanFramebufferAttachment? attachment in ColorAttachments)
                SetImageLayout(commandBuffer, attachment!.GetImage(), Format.R8G8B8A8Unorm, 1,
                    ImageLayout.Undefined, GetAttachmentInitialLayout());
            foreach (PvrVulkanFramebufferAttachment? attachment in DepthAttachments)
                SetImageLayout(commandBuffer, attachment!.GetImage(), vulkan.GetDepthFormat(), 1,
                    ImageLayout.Undefined, ImageLayout.DepthStencilReadOnlyOptimal);
        }

        VertexShaderUniforms vertexUniforms = new() { NdcMat = Matrices.GetNormalMatrix() };
        bool firstFrameAfterInit = _oitBuffers!.IsFirstFrameAfterInit();
        _oitBuffers.OnNewFrame(commandBuffer);
        ulong pixelBufferAddress = _oitBuffers.GetPixelBufferAddress();
        FragmentShaderUniforms fragmentUniforms = MakeFragmentUniforms<FragmentShaderUniforms>();
        fragmentUniforms.ShadeScaleFactor = FPU_SHAD_SCALE.scale_factor / 256f;
        fragmentUniforms.PixelBufferSize = (uint)(_oitBuffers.GetPixelBufferSize() / 16);
        fragmentUniforms.PixelBufferAddress = pixelBufferAddress;
        fragmentUniforms.ViewportWidth = (uint)MaxWidth;
        _dithering = PvrConfiguration.EmulateFramebuffer && context.FbWCtrl.fb_dither != 0 && context.FbWCtrl.fb_packmode <= 3;
        if (_dithering)
        {
            switch (context.FbWCtrl.fb_packmode)
            {
                case 0:
                case 3:
                    fragmentUniforms.DitherDivisor[0] = fragmentUniforms.DitherDivisor[1] = fragmentUniforms.DitherDivisor[2] = 2f;
                    break;
                case 1:
                    fragmentUniforms.DitherDivisor[0] = fragmentUniforms.DitherDivisor[2] = 2f;
                    fragmentUniforms.DitherDivisor[1] = 4f;
                    break;
                case 2:
                    fragmentUniforms.DitherDivisor[0] = fragmentUniforms.DitherDivisor[1] = fragmentUniforms.DitherDivisor[2] = 1f;
                    break;
            }
            fragmentUniforms.DitherDivisor[3] = 1f;
        }

        CurrentScissor = default;
        if (!vulkan.HasProvokingVertex())
            PvrSorter.SetFirstProvokingVertex(context);

        UploadMainBuffer(vertexUniforms, fragmentUniforms);
        _quadBuffer!.Update();

        _descriptorSets.UpdateUniforms(_curMainBuffer, (uint)_offsets.VertexUniformOffset, (uint)_offsets.FragmentUniformOffset,
            fogTexture!.GetImageView(), (uint)_offsets.PolyParamsOffset, (uint)_offsets.PolyParamsSize,
            DepthAttachments[0]!.GetStencilView(), DepthAttachments[0]!.GetImageView(), paletteTexture!.GetImageView(), _oitBuffers);
        _descriptorSets.BindPerFrameDescriptorSets(commandBuffer);
        _descriptorSets.UpdateColorInputDescSet(0, ColorAttachments[0]!.GetImageView());
        _descriptorSets.UpdateColorInputDescSet(1, ColorAttachments[1]!.GetImageView());

        BindMainVertexBuffer(commandBuffer, 0);
        api.CmdBindIndexBuffer(commandBuffer, _curMainBuffer, _offsets.IndexOffset, IndexType.Uint32);

        PushConstants push = new();
        VtxPushConstants vtxPush = new();
        PipelineLayout layout = _pipelineManager!.GetPipelineLayout();
        api.CmdPushConstants(commandBuffer, layout, ShaderStageFlags.FragmentBit, 0, (uint)sizeof(PushConstants), &push);
        api.CmdPushConstants(commandBuffer, layout, ShaderStageFlags.VertexBit, (uint)sizeof(PushConstants),
            (uint)sizeof(VtxPushConstants), &vtxPush);

        ClearColorValue color = context.IsRtt ? new ClearColorValue(0f, 0f, 0f, 1f) : PvrVulkanUtilities.GetBorderColor();
        ClearValue* clearColors = stackalloc ClearValue[4]
        {
            new() { Color = color },
            new() { Color = color },
            new() { DepthStencil = new ClearDepthStencilValue(0f, 0) },
            new() { DepthStencil = new ClearDepthStencilValue(0f, 0) }
        };

        RetroMediaLab.Chips.Video.NEC.PowerVr2.RenderPass previous = default;
        for (int renderPass = 0; renderPass < context.RenderPasses.Count; renderPass++)
        {
            RetroMediaLab.Chips.Video.NEC.PowerVr2.RenderPass current = context.RenderPasses[renderPass];
            PvrRuntime.Log.Debug("Render pass {0} OP {1} PT {2} TR {3} MV {4} TrMV {5} autosort {6} BDA {7}", renderPass + 1,
                current.OpCount - previous.OpCount, current.PtCount - previous.PtCount, current.TrCount - previous.TrCount,
                current.MvoCount - previous.MvoCount, current.MvOpTrShared
                    ? current.MvoCount - previous.MvoCount : current.MvoTrCount - previous.MvoTrCount,
                current.AutoSort ? 1 : 0, pixelBufferAddress != 0 ? 1 : 0);

            _oitBuffers.ResetPixelCounter(commandBuffer);
            bool initial = renderPass == 0;
            bool final = renderPass == context.RenderPasses.Count - 1;
            Framebuffer target = GetFramebuffer(renderPass, context.RenderPasses.Count);
            RenderPassBeginInfo begin = new()
            {
                SType = StructureType.RenderPassBeginInfo,
                RenderPass = _pipelineManager.GetRenderPass(initial, final, initial && context.ClearFramebuffer),
                Framebuffer = target,
                RenderArea = Viewport,
                ClearValueCount = 4,
                PClearValues = clearColors
            };
            api.CmdBeginRenderPass(commandBuffer, &begin, SubpassContents.Inline);
            try
            {
                DrawList(commandBuffer, ListType_Opaque, false, PvrVulkanOitPass.Depth, context.GlobalParamOp, previous.OpCount, current.OpCount);
                DrawList(commandBuffer, ListType_Punch_Through, false, PvrVulkanOitPass.Depth, context.GlobalParamPt, previous.PtCount, current.PtCount);
                DrawModifierVolumes(commandBuffer, (int)previous.MvoCount, (int)(current.MvoCount - previous.MvoCount),
                    context.GlobalParamMvo, false);

                api.CmdNextSubpass(commandBuffer, SubpassContents.Inline);
                DrawList(commandBuffer, ListType_Opaque, false, PvrVulkanOitPass.Color, context.GlobalParamOp, previous.OpCount, current.OpCount);
                DrawList(commandBuffer, ListType_Punch_Through, false, PvrVulkanOitPass.Color, context.GlobalParamPt, previous.PtCount, current.PtCount);

                if (firstFrameAfterInit)
                {
                    api.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, _pipelineManager.GetClearPipeline());
                    _quadBuffer.Bind(commandBuffer);
                    _quadBuffer.Draw(commandBuffer);
                    ShaderWriteBarrier(commandBuffer);
                    BindMainVertexBuffer(commandBuffer, 0);
                    firstFrameAfterInit = false;
                }
                if (current.AutoSort)
                    DrawList(commandBuffer, ListType_Translucent, true, PvrVulkanOitPass.Oit, context.GlobalParamTr, previous.TrCount, current.TrCount);
                else
                    DrawList(commandBuffer, ListType_Translucent, false, PvrVulkanOitPass.Color, context.GlobalParamTr, previous.TrCount, current.TrCount);

                api.CmdNextSubpass(commandBuffer, SubpassContents.Inline);
                _descriptorSets.BindColorInputDescSet(commandBuffer, 1 - GetFramebufferIndex());

                if (initial && !context.IsRtt && ClearNeeded[GetFramebufferIndex()])
                {
                    ClearNeeded[GetFramebufferIndex()] = false;
                    SetScissor(commandBuffer, Viewport);
                    ClearAttachment attachment = new(ImageAspectFlags.ColorBit, 0, clearColors[0]);
                    ClearRect rect = new(Viewport, 0, 1);
                    api.CmdClearAttachments(commandBuffer, 1, &attachment, 1, &rect);
                }

                if (vulkan.GetVendorId() != PvrVulkanContext.VendorQualcomm)
                {
                    if (current.MvOpTrShared)
                        DrawModifierVolumes(commandBuffer, (int)previous.MvoCount, (int)(current.MvoCount - previous.MvoCount),
                            context.GlobalParamMvo, true);
                    else
                        DrawModifierVolumes(commandBuffer, (int)previous.MvoTrCount, (int)(current.MvoTrCount - previous.MvoTrCount),
                            context.GlobalParamMvoTr, true);
                }

                SetScissor(commandBuffer, Viewport);
                Pipeline pipeline = _pipelineManager.GetFinalPipeline(_dithering && final);
                api.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, pipeline);
                _quadBuffer.Bind(commandBuffer);
                _quadBuffer.Draw(commandBuffer);

                if (!final)
                {
                    BindMainVertexBuffer(commandBuffer, 0);
                    api.CmdBindIndexBuffer(commandBuffer, _curMainBuffer, _offsets.IndexOffset, IndexType.Uint32);
                    DrawList(commandBuffer, ListType_Translucent, current.AutoSort, PvrVulkanOitPass.Depth,
                        context.GlobalParamTr, previous.TrCount, current.TrCount);
                }
            }
            finally
            {
                api.CmdEndRenderPass(commandBuffer);
            }
            previous = current;
        }
        _curMainBuffer = default;
        return !context.IsRtt;
    }

    protected void MakeBuffers(int width, int height, ImageUsageFlags colorUsage = 0)
    {
        _oitBuffers!.Init(width, height);
        if (width <= MaxWidth && height <= MaxHeight && colorUsage == _currentBufferUsage)
            return;
        MaxWidth = Math.Max(MaxWidth, width);
        MaxHeight = Math.Max(MaxHeight, height);
        _currentBufferUsage = colorUsage;

        foreach (PvrVulkanOwned<Framebuffer>? framebuffer in TempFramebuffers)
            if (framebuffer != null)
                CommandPool!.AddToFlight(new PvrVulkanDeleter<PvrVulkanOwned<Framebuffer>>(framebuffer));

        PvrVulkanContext context = GetContext();
        var device = context.GetDevice();
        ImageUsageFlags usage = ImageUsageFlags.ColorAttachmentBit | ImageUsageFlags.InputAttachmentBit | colorUsage;
        for (int i = 0; i < ColorAttachments.Length; i++)
        {
            if (ColorAttachments[i] != null)
                CommandPool!.AddToFlight(new PvrVulkanDeleter<PvrVulkanFramebufferAttachment>(ColorAttachments[i]!));
            ColorAttachments[i] = new PvrVulkanFramebufferAttachment(context.GetPhysicalDevice(), device);
            ColorAttachments[i]!.Init((uint)MaxWidth, (uint)MaxHeight, Format.R8G8B8A8Unorm, usage, "COLOR ATTACHMENT " + i);
        }
        for (int i = 0; i < DepthAttachments.Length; i++)
        {
            if (DepthAttachments[i] != null)
                CommandPool!.AddToFlight(new PvrVulkanDeleter<PvrVulkanFramebufferAttachment>(DepthAttachments[i]!));
            DepthAttachments[i] = new PvrVulkanFramebufferAttachment(context.GetPhysicalDevice(), device);
            DepthAttachments[i]!.Init((uint)MaxWidth, (uint)MaxHeight, context.GetDepthFormat(),
                ImageUsageFlags.DepthStencilAttachmentBit | ImageUsageFlags.InputAttachmentBit, "DEPTH ATTACHMENT" + i);
        }
        _needAttachmentTransition = true;

        ImageView[] attachments =
        [
            ColorAttachments[0]!.GetImageView(),
            ColorAttachments[1]!.GetImageView(),
            DepthAttachments[0]!.GetImageView(),
            DepthAttachments[1]!.GetImageView()
        ];
        Silk.NET.Vulkan.RenderPass renderPass = _pipelineManager!.GetRenderPass(true, true);
        TempFramebuffers[0] = device.CreateFramebufferUnique(renderPass, attachments, (uint)MaxWidth, (uint)MaxHeight, 1);
        attachments[0] = attachments[1];
        attachments[1] = ColorAttachments[0]!.GetImageView();
        TempFramebuffers[1] = device.CreateFramebufferUnique(renderPass, attachments, (uint)MaxWidth, (uint)MaxHeight, 1);
    }

    private void BindMainVertexBuffer(CommandBuffer commandBuffer, ulong offset)
    {
        Buffer buffer = _curMainBuffer;
        GetContext().Api.CmdBindVertexBuffers(commandBuffer, 0, 1, &buffer, &offset);
    }

    private static void ShaderWriteBarrier(CommandBuffer commandBuffer)
    {
        MemoryBarrier barrier = new() { SType = StructureType.MemoryBarrier,
            SrcAccessMask = AccessFlags.ShaderWriteBit, DstAccessMask = AccessFlags.ShaderReadBit };
        PvrVulkanContext.Instance.Api.CmdPipelineBarrier(commandBuffer, PipelineStageFlags.FragmentShaderBit,
            PipelineStageFlags.FragmentShaderBit, DependencyFlags.ByRegionBit, 1, &barrier, 0, null, 0, null);
    }
}

internal unsafe sealed partial class PvrVulkanOitScreenDrawer
{
    protected override Framebuffer GetFramebuffer(int renderPass, int renderPassCount)
    {
        FramebufferIndex = 1 - FramebufferIndex;
        return TempFramebuffers[FramebufferIndex]!.Handle;
    }

    private void MakeFramebuffers(in Extent2D viewport)
    {
        Viewport.Offset.X = 0;
        Viewport.Offset.Y = 0;
        Viewport.Extent = viewport;
        MaxWidth = 0;
        MaxHeight = 0;
        MakeBuffers((int)viewport.Width, (int)viewport.Height,
            _emulateFramebuffer ? ImageUsageFlags.TransferSrcBit : ImageUsageFlags.SampledBit);
        ClearNeeded = [true, true];
    }

    internal override CommandBuffer NewFrame()
    {
        Vk api = GetContext().Api;
        if (!_frameStarted)
        {
            _frameStarted = true;
            _frameRendered = false;
            NewImage();
            CurrentCommandBuffer = CommandPool!.Allocate(true);
            PvrVk.Begin(CurrentCommandBuffer, CommandBufferUsageFlags.OneTimeSubmitBit);
        }

        Matrices.CalcMatrices(RendContext!, (int)Viewport.Extent.Width, (int)Viewport.Extent.Height);
        SetBaseScissor(Viewport.Extent);
        Rect2D scissor = BaseScissor;
        api.CmdSetScissor(CurrentCommandBuffer, 0, 1, &scissor);
        Silk.NET.Vulkan.Viewport viewport = new(Viewport.Offset.X, Viewport.Offset.Y, Viewport.Extent.Width, Viewport.Extent.Height, 1f, 0f);
        api.CmdSetViewport(CurrentCommandBuffer, 0, 1, &viewport);
        return CurrentCommandBuffer;
    }
}

internal unsafe sealed partial class PvrVulkanOitTextureDrawer
{
    protected override Framebuffer GetFramebuffer(int renderPass, int renderPassCount)
    {
        if (renderPass < renderPassCount - 1)
        {
            FramebufferIndex = (renderPassCount - renderPass) % 2;
            return TempFramebuffers[FramebufferIndex]!.Handle;
        }
        FramebufferIndex = 0;
        return _framebuffer!.Handle;
    }

    internal override CommandBuffer NewFrame()
    {
        RenderContext context = RendContext!;
        PvrRuntime.Log.Debug("RenderToTexture packmode={0} stride={1} - {2} x {3} @ {4:x6}",
            context.FbWCtrl.fb_packmode, context.FbWLineStride * 8, context.FbClip.Size.X, context.FbClip.Size.Y,
            context.FbWSof1 & PvrMemory.VramMask);
        NewImage();

        _textureAddr = context.FbWSof1 & PvrMemory.VramMask;
        uint width = context.FramebufferWidth;
        uint height = context.FramebufferHeight;
        Matrices.CalcMatrices(context, (int)width, (int)height);

        _rttPipelineManager!.CheckSettingsChange();
        PvrVulkanContext vulkan = GetContext();
        var device = vulkan.GetDevice();
        Vk api = vulkan.Api;

        CommandBuffer commandBuffer = CommandPool!.Allocate(true);
        PvrVk.Begin(commandBuffer, CommandBufferUsageFlags.OneTimeSubmitBit);

        MakeBuffers((int)width, (int)height,
            PvrConfiguration.RenderToTextureBuffer ? ImageUsageFlags.TransferSrcBit : ImageUsageFlags.SampledBit);

        ImageView colorImageView;
        ImageLayout colorImageCurrentLayout;
        if (!PvrConfiguration.RenderToTextureBuffer)
        {
            PvrTransformMatrix.GetPvrFramebufferSize(context, out int wpo2, out int hpo2);
            _texture = _textureCache!.GetRenderToTexture(_textureAddr, context.FbWCtrl.fb_packmode, (uint)wpo2, (uint)hpo2);
            if (_textureCache.IsInFlight(_texture, false))
            {
                _texture.ReadOnlyImageView = _texture.GetImageView();
                _texture.DeferDeleteResource(CommandPool);
            }
            _textureCache.SetInFlight(_texture);

            const ImageUsageFlags usage = ImageUsageFlags.ColorAttachmentBit | ImageUsageFlags.SampledBit;
            if (_texture.ImageOwner == null || _texture.Format != Format.R8G8B8A8Unorm
                || _texture.Extent.Width != width || _texture.Extent.Height != height || (_texture.UsageFlags & usage) != usage)
            {
                _texture.Extent = new Extent2D(width, height);
                _texture.Format = Format.R8G8B8A8Unorm;
                _texture.NeedsStaging = true;
                _texture.CreateImage(ImageTiling.Optimal, usage, ImageLayout.Undefined, ImageAspectFlags.ColorBit);
                colorImageCurrentLayout = ImageLayout.Undefined;
            }
            else
            {
                colorImageCurrentLayout = ImageLayout.ShaderReadOnlyOptimal;
            }
            _colorImage = _texture.GetImage();
            colorImageView = _texture.GetImageView();
        }
        else
        {
            if (_colorAttachment == null || width > _colorAttachment.GetExtent().Width || height > _colorAttachment.GetExtent().Height)
            {
                if (_colorAttachment == null)
                    _colorAttachment = new PvrVulkanFramebufferAttachment(vulkan.GetPhysicalDevice(), device);
                else
                    vulkan.WaitIdle();
                _colorAttachment.Init(width, height, Format.R8G8B8A8Unorm,
                    ImageUsageFlags.ColorAttachmentBit | ImageUsageFlags.TransferSrcBit, "RTT COLOR ATTACHMENT");
                colorImageCurrentLayout = ImageLayout.Undefined;
            }
            else
            {
                colorImageCurrentLayout = ImageLayout.TransferSrcOptimal;
            }
            _colorImage = _colorAttachment.GetImage();
            colorImageView = _colorAttachment.GetImageView();
        }

        Viewport.Offset.X = 0;
        Viewport.Offset.Y = 0;
        Viewport.Extent = new Extent2D(width, height);
        SetImageLayout(commandBuffer, _colorImage, Format.R8G8B8A8Unorm, 1, colorImageCurrentLayout, ImageLayout.ColorAttachmentOptimal);
        ImageView[] views =
        [
            colorImageView,
            ColorAttachments[1]!.GetImageView(),
            DepthAttachments[0]!.GetImageView(),
            DepthAttachments[1]!.GetImageView()
        ];

        if (_framebuffer != null)
            CommandPool.AddToFlight(new PvrVulkanDeleter<PvrVulkanOwned<Framebuffer>>(_framebuffer));
        _framebuffer = device.CreateFramebufferUnique(_rttPipelineManager.GetRenderPass(true, true), views, width, height, 1);

        Silk.NET.Vulkan.Viewport viewport = new(0f, 0f, width, height, 1f, 0f);
        api.CmdSetViewport(commandBuffer, 0, 1, &viewport);
        PvrRect scissor = Matrices.GetBaseScissor();
        Rect2D rect = new(new Offset2D(scissor.Origin.X, scissor.Origin.Y),
            new Extent2D((uint)scissor.Size.X, (uint)scissor.Size.Y));
        BaseScissor = rect;
        api.CmdSetScissor(commandBuffer, 0, 1, &rect);
        CurrentCommandBuffer = commandBuffer;
        return commandBuffer;
    }

    internal override void EndFrame()
    {
        RenderContext context = RendContext!;
        Vk api = GetContext().Api;
        uint width = context.FramebufferWidth;
        uint height = context.FramebufferHeight;
        if (PvrConfiguration.RenderToTextureBuffer)
        {
            BufferImageCopy copyRegion = new(0, width, height, new ImageSubresourceLayers(ImageAspectFlags.ColorBit, 0, 0, 1),
                new Offset3D(0, 0, 0), new Extent3D(width, height, 1));
            Buffer staging = _colorAttachment!.GetBufferData()!.Buffer;
            api.CmdCopyImageToBuffer(CurrentCommandBuffer, _colorAttachment.GetImage(), ImageLayout.TransferSrcOptimal,
                staging, 1, &copyRegion);
            BufferMemoryBarrier barrier = new() { SType = StructureType.BufferMemoryBarrier,
                SrcAccessMask = AccessFlags.TransferWriteBit, DstAccessMask = AccessFlags.HostReadBit,
                SrcQueueFamilyIndex = uint.MaxValue, DstQueueFamilyIndex = uint.MaxValue, Buffer = staging, Offset = 0, Size = ulong.MaxValue };
            api.CmdPipelineBarrier(CurrentCommandBuffer, PipelineStageFlags.TransferBit, PipelineStageFlags.HostBit,
                0, 0, null, 1, &barrier, 0, null);
        }
        PvrVk.End(CurrentCommandBuffer);
        _colorImage = default;
        CurrentCommandBuffer = default;

        if (PvrConfiguration.RenderToTextureBuffer)
        {
            CommandPool!.EndFrameAndWait();
            ushort* destination = (ushort*)(PvrMemory.VramPointer + _textureAddr);
            using var temporary = new PvrPixelBuffer<uint>();
            temporary.Init(width, height);
            _colorAttachment!.GetBufferData()!.Download(width * height * 4, temporary.Data());
            PvrTextureCache.WriteTextureToVram(width, height, (byte*)temporary.Data(), destination, context.FbWCtrl,
                context.FbWLineStride * 8, context.FbClip);
        }
        else
        {
            CommandPool!.EndFrame();
            _texture!.Dirty = 0;
            _texture.UnprotectVram();
        }
    }
}
