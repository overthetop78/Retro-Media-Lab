using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;
using VkRenderPass = Silk.NET.Vulkan.RenderPass;
using static RetroMediaLab.Chips.Video.NEC.PowerVr2.PowerVr2Ta;
using static RetroMediaLab.Chips.Video.NEC.PowerVr2.PvrRegisters;
using static RetroMediaLab.Chips.Video.NEC.PowerVr2.Vulkan.PvrVulkanImageLayout;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Vulkan;

internal abstract unsafe partial class PvrVulkanBaseDrawer
{
    protected TileClipping SetTileClip(CommandBuffer commandBuffer, uint value, out Rect2D clip)
    {
        PvrRect bounds = default;
        TileClipping mode = Matrices.GetTileClip(value, ref bounds);
        clip = default;
        if (mode != TileClipping.Off)
            clip = new Rect2D(new Offset2D(bounds.Origin.X, bounds.Origin.Y),
                new Extent2D((uint)bounds.Size.X, (uint)bounds.Size.Y));
        SetScissor(commandBuffer, mode == TileClipping.Outside ? clip : BaseScissor);
        return mode;
    }

    protected void SetBaseScissor(in Extent2D viewport = default)
    {
        PvrRect bounds = Matrices.GetBaseScissor();
        BaseScissor = new Rect2D(new Offset2D(bounds.Origin.X, bounds.Origin.Y),
            new Extent2D((uint)bounds.Size.X, (uint)bounds.Size.Y));
    }

    protected void ScaleAndWriteFramebuffer(CommandBuffer commandBuffer, PvrVulkanFramebufferAttachment finalFramebuffer)
    {
        using var scope = new PvrVulkanCommandBufferDebugScope(commandBuffer, "scaleAndWriteFramebuffer", [0.25f, 0.25f, 0.25f, 0.25f]);
        RenderContext context = RendContext!;
        uint width = (uint)context.GlobClip.X;
        uint height = (uint)context.GlobClip.Y;
        PvrTransformMatrix.GetWriteFramebufferToVramParams(context, out PvrInt2 scaledSize, out PvrRect finalClip);
        PvrVulkanFramebufferAttachment? scaledFramebuffer = null;
        PvrVulkanContext vulkan = GetContext();
        Vk api = vulkan.Api;
        try
        {
            if (scaledSize.X != (int)width || scaledSize.Y != (int)height)
            {
                uint scaledWidth = (uint)scaledSize.X;
                uint scaledHeight = (uint)scaledSize.Y;
                scaledFramebuffer = new PvrVulkanFramebufferAttachment(vulkan.GetPhysicalDevice(), vulkan.GetDevice());
                scaledFramebuffer.Init(scaledWidth, scaledHeight, Format.R8G8B8A8Unorm,
                    ImageUsageFlags.TransferSrcBit | ImageUsageFlags.TransferDstBit, "SCALED FRAMEBUFFER");
                SetImageLayout(commandBuffer, scaledFramebuffer.GetImage(), Format.R8G8B8A8Unorm, 1,
                    ImageLayout.Undefined, ImageLayout.TransferDstOptimal);
                ImageBlit blit = new()
                {
                    SrcSubresource = new ImageSubresourceLayers(ImageAspectFlags.ColorBit, 0, 0, 1)
                };
                blit.SrcOffsets[0] = new Offset3D(0, 0, 0);
                blit.SrcOffsets[1] = new Offset3D((int)width, (int)height, 1);
                blit.DstOffsets[0] = new Offset3D(0, 0, 0);
                blit.DstOffsets[1] = new Offset3D((int)scaledWidth, (int)scaledHeight, 1);
                blit.DstSubresource = blit.SrcSubresource;
                api.CmdBlitImage(commandBuffer, finalFramebuffer.GetImage(), ImageLayout.TransferSrcOptimal,
                    scaledFramebuffer.GetImage(), ImageLayout.TransferDstOptimal, 1, &blit, Filter.Linear);
                SetImageLayout(commandBuffer, scaledFramebuffer.GetImage(), Format.R8G8B8A8Unorm, 1,
                    ImageLayout.TransferDstOptimal, ImageLayout.TransferSrcOptimal);
                finalFramebuffer = scaledFramebuffer;
                width = scaledWidth;
                height = scaledHeight;
            }
            CopyFramebufferToBuffer(commandBuffer, finalFramebuffer, width, height);
            PvrVk.End(commandBuffer);
            CommandPool!.EndFrameAndWait();

            using var temporary = new PvrPixelBuffer<uint>();
            temporary.Init(width, height);
            finalFramebuffer.GetBufferData()!.Download(width * height * 4, temporary.Data());
            PvrTextureCache.WriteFramebuffer(width, height, (byte*)temporary.Data(), context.FbWSof1 & PvrMemory.VramMask,
                context.FbWCtrl, context.FbWLineStride * 8, finalClip);
        }
        finally
        {
            scaledFramebuffer?.Dispose();
        }
    }

    protected void CopyFramebufferToBuffer(CommandBuffer commandBuffer, PvrVulkanFramebufferAttachment source, uint width, uint height)
    {
        Vk api = GetContext().Api;
        BufferImageCopy region = new(0, width, height,
            new ImageSubresourceLayers(ImageAspectFlags.ColorBit, 0, 0, 1),
            new Offset3D(0, 0, 0), new Extent3D(width, height, 1));
        Buffer target = source.GetBufferData()!.Buffer;
        api.CmdCopyImageToBuffer(commandBuffer, source.GetImage(), ImageLayout.TransferSrcOptimal, target, 1, &region);
        BufferMemoryBarrier barrier = new()
        {
            SType = StructureType.BufferMemoryBarrier,
            SrcAccessMask = AccessFlags.TransferWriteBit,
            DstAccessMask = AccessFlags.HostReadBit,
            SrcQueueFamilyIndex = uint.MaxValue,
            DstQueueFamilyIndex = uint.MaxValue,
            Buffer = target,
            Size = ulong.MaxValue
        };
        api.CmdPipelineBarrier(commandBuffer, PipelineStageFlags.TransferBit, PipelineStageFlags.HostBit, 0,
            0, null, 1, &barrier, 0, null);
    }
}

internal abstract unsafe partial class PvrVulkanDrawer
{
    private void DrawPoly(CommandBuffer commandBuffer, uint listType, bool sortTriangles, in PolyParam poly, uint first, uint count, int polygonIndex)
    {
        using var scope = new PvrVulkanCommandBufferDebugScope(commandBuffer, "DrawPoly", [0.25f, 0.50f, 0.25f, 1f]);
        TileClipping tileClip = SetTileClip(commandBuffer, poly.TileClip, out Rect2D scissor);

        float trilinearAlpha = 1f;
        if (poly.Tsp.FilterMode > 1 && poly.Pcw.Texture != 0 && listType != ListType_Punch_Through && poly.Tcw.MipMapped == 1)
        {
            trilinearAlpha = 0.25f * (poly.Tsp.MipMapD & 3);
            if (poly.Tsp.FilterMode == 2)
                trilinearAlpha = 1f - trilinearAlpha;
        }
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

        Vk api = GetContext().Api;
        if (tileClip == TileClipping.Inside || trilinearAlpha != 1f || gpuPalette != 0)
        {
            float* constants = stackalloc float[6]
            {
                scissor.Offset.X, scissor.Offset.Y, scissor.Offset.X + (float)scissor.Extent.Width,
                scissor.Offset.Y + (float)scissor.Extent.Height, trilinearAlpha, paletteIndex
            };
            api.CmdPushConstants(commandBuffer, _pipelineManager!.GetPipelineLayout(), ShaderStageFlags.FragmentBit, 0, sizeof(float) * 6, constants);
        }

        Pipeline pipeline = _pipelineManager!.GetPipeline(listType, sortTriangles, poly, gpuPalette, _dithering);
        api.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, pipeline);
        if (poly.Pcw.Texture != 0 || poly.IsNaomi2())
        {
            ulong offset = 0;
            int index = 0;
            if (poly.IsNaomi2())
            {
                switch (listType)
                {
                    case ListType_Opaque:
                        offset = _offsets.Naomi2OpaqueOffset;
                        index = polygonIndex;
                        break;
                    case ListType_Punch_Through:
                        offset = _offsets.Naomi2PunchThroughOffset;
                        index = polygonIndex;
                        break;
                    case ListType_Translucent:
                        offset = _offsets.Naomi2TranslucentOffset;
                        index = polygonIndex;
                        break;
                }
            }
            _descriptorSets.BindPerPolyDescriptorSets(commandBuffer, poly, index, _curMainBuffer, offset,
                _offsets.LightsOffset, listType == ListType_Punch_Through);
        }
        api.CmdDrawIndexed(commandBuffer, count, 1, first, 0, 0);
    }

    private void DrawSorted(CommandBuffer commandBuffer, List<SortedTriangle> polys, uint first, uint last, bool multipass)
    {
        if (first == last)
            return;

        using var scope = new PvrVulkanCommandBufferDebugScope(commandBuffer, "DrawSorted", [0.25f, 0.50f, 0.50f, 1f]);
        Span<PolyParam> polygons = CollectionsMarshal.AsSpan(RendContext!.GlobalParamTr);
        for (uint index = first; index < last; index++)
        {
            SortedTriangle triangle = polys[(int)index];
            DrawPoly(commandBuffer, ListType_Translucent, true, polygons[(int)triangle.PolyIndex], triangle.First, triangle.Count,
                (int)triangle.PolyIndex);
        }

        if (multipass && PvrConfiguration.TranslucentPolygonDepthMask)
        {
            Vk api = GetContext().Api;
            for (uint index = first; index < last; index++)
            {
                SortedTriangle triangle = polys[(int)index];
                ref PolyParam poly = ref polygons[(int)triangle.PolyIndex];
                if (poly.Isp.ZWriteDis != 0)
                    continue;
                Pipeline pipeline = _pipelineManager!.GetDepthPassPipeline((int)poly.Isp.CullMode, poly.IsNaomi2());
                api.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, pipeline);
                SetTileClip(commandBuffer, poly.TileClip, out _);
                api.CmdDrawIndexed(commandBuffer, triangle.Count, 1, (uint)RendContext.Idx.Count + triangle.First, 0, 0);
            }
        }
    }

    private void DrawList(CommandBuffer commandBuffer, uint listType, bool sortTriangles, List<PolyParam> polys, uint first, uint last)
    {
        if (first == last)
            return;

        using var scope = new PvrVulkanCommandBufferDebugScope(commandBuffer, "DrawList", [0.50f, 0.25f, 0.50f, 1f]);
        Span<PolyParam> polygons = CollectionsMarshal.AsSpan(polys);
        for (int index = (int)first; index < last; index++)
        {
            ref readonly PolyParam poly = ref polygons[index];
            if (poly.Count > 2)
                DrawPoly(commandBuffer, listType, sortTriangles, poly, poly.First, poly.Count, index);
        }
    }

    private void DrawModVols(CommandBuffer commandBuffer, int first, int count)
    {
        if (count == 0 || RendContext!.ModTrig.Count == 0 || !PvrConfiguration.ModifierVolumes)
            return;

        using var scope = new PvrVulkanCommandBufferDebugScope(commandBuffer, "DrawModVols", [0.75f, 0.25f, 0.25f, 1f]);
        Vk api = GetContext().Api;
        BindMainVertexBuffer(commandBuffer, _offsets.ModVolOffset);
        Span<ModifierVolumeParam> parameters = CollectionsMarshal.AsSpan(RendContext.GlobalParamMvo).Slice(first, count);

        int modBase = -1;
        for (int index = 0; index < count; index++)
        {
            ref ModifierVolumeParam param = ref parameters[index];
            if (param.Count == 0)
                continue;
            uint mvMode = param.Isp.DepthMode;
            if (modBase == -1)
                modBase = (int)param.First;

            PvrVulkanModVolMode mode = param.Isp.VolumeLast == 0 && mvMode > 0 ? PvrVulkanModVolMode.Or : PvrVulkanModVolMode.Xor;
            Pipeline pipeline = _pipelineManager!.GetModifierVolumePipeline(mode, (int)param.Isp.CullMode, param.IsNaomi2());
            api.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, pipeline);
            _descriptorSets.BindPerPolyDescriptorSets(commandBuffer, param, first + index, _curMainBuffer, _offsets.Naomi2ModVolOffset);
            SetTileClip(commandBuffer, param.TileClip, out _);
            api.CmdDraw(commandBuffer, param.Count * 3, 1, param.First * 3, 0);
            if (mvMode is 1 or 2)
            {
                pipeline = _pipelineManager.GetModifierVolumePipeline(
                    mvMode == 1 ? PvrVulkanModVolMode.Inclusion : PvrVulkanModVolMode.Exclusion, (int)param.Isp.CullMode, param.IsNaomi2());
                api.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, pipeline);
                api.CmdDraw(commandBuffer, (param.First + param.Count - (uint)modBase) * 3, 1, (uint)modBase * 3, 0);
                modBase = -1;
            }
        }

        BindMainVertexBuffer(commandBuffer, 0);
        SetTileClip(commandBuffer, 0, out _);
        float* constants = stackalloc float[6] { 1 - FPU_SHAD_SCALE.scale_factor / 256f, 0, 0, 0, 0, 0 };
        api.CmdPushConstants(commandBuffer, _pipelineManager!.GetPipelineLayout(), ShaderStageFlags.FragmentBit, 0, sizeof(float) * 6, constants);
        Pipeline final = _pipelineManager.GetModifierVolumePipeline(PvrVulkanModVolMode.Final, 0, false);
        api.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, final);
        api.CmdDrawIndexed(commandBuffer, 4, 1, 0, 0, 0);
    }

    private void UploadMainBuffer(in PvrVulkanVertexShaderUniforms vertexUniforms, in PvrVulkanFragmentShaderUniforms fragmentUniforms)
    {
        var packer = new PvrVulkanBufferPacker();
        RenderContext context = RendContext!;
        fixed (TaVertex* vertices = CollectionsMarshal.AsSpan(context.Verts))
        fixed (ModTriangle* modifierVolumes = CollectionsMarshal.AsSpan(context.ModTrig))
        fixed (uint* indices = CollectionsMarshal.AsSpan(context.Idx))
        fixed (PvrVulkanVertexShaderUniforms* vertexData = &vertexUniforms)
        fixed (PvrVulkanFragmentShaderUniforms* fragmentData = &fragmentUniforms)
        fixed (N2LightModel* lights = CollectionsMarshal.AsSpan(context.LightModels))
        {
            packer.Add(vertices, (nuint)context.Verts.Count * (uint)sizeof(TaVertex));
            _offsets.ModVolOffset = packer.Add(modifierVolumes, (nuint)context.ModTrig.Count * (uint)sizeof(ModTriangle));
            _offsets.IndexOffset = packer.Add(indices, (nuint)context.Idx.Count * sizeof(uint));
            _offsets.VertexUniformOffset = packer.AddUniform(vertexData, (uint)sizeof(PvrVulkanVertexShaderUniforms));
            _offsets.FragmentUniformOffset = packer.AddUniform(fragmentData, (uint)sizeof(PvrVulkanFragmentShaderUniforms));

            byte[]? naomi2Uniforms = null;
            if (PvrConfiguration.IsNaomi2)
            {
                naomi2Uniforms = PackNaomi2Uniforms(packer, false, out _offsets.Naomi2OpaqueOffset,
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

    internal bool Draw(PvrVulkanTexture? fogTexture, PvrVulkanTexture? paletteTexture)
    {
        RenderContext context = RendContext!;
        PvrVulkanFragmentShaderUniforms fragmentUniforms = MakeFragmentUniforms<PvrVulkanFragmentShaderUniforms>();
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

        CommandBuffer commandBuffer = BeginRenderPass();
        using var scope = new PvrVulkanCommandBufferDebugScope(commandBuffer, "Draw", [0.75f, 0.75f, 0.75f, 1f]);
        if (!GetContext().HasProvokingVertex())
            PvrSorter.SetFirstProvokingVertex(context);

        PvrVulkanVertexShaderUniforms vertexUniforms = new() { NdcMat = Matrices.GetNormalMatrix() };
        UploadMainBuffer(vertexUniforms, fragmentUniforms);
        _descriptorSets.UpdateUniforms(_curMainBuffer, (uint)_offsets.VertexUniformOffset, (uint)_offsets.FragmentUniformOffset,
            fogTexture!.GetImageView(), paletteTexture!.GetImageView());
        _descriptorSets.BindPerFrameDescriptorSets(commandBuffer);

        Vk api = GetContext().Api;
        BindMainVertexBuffer(commandBuffer, 0);
        api.CmdBindIndexBuffer(commandBuffer, _curMainBuffer, _offsets.IndexOffset, IndexType.Uint32);
        float* constants = stackalloc float[6] { 0, 0, 0, 0, 0, 0 };
        api.CmdPushConstants(commandBuffer, _pipelineManager!.GetPipelineLayout(), ShaderStageFlags.FragmentBit, 0, sizeof(float) * 6, constants);

        RetroMediaLab.Chips.Video.NEC.PowerVr2.RenderPass previous = default;
        for (int index = 0; index < context.RenderPasses.Count; index++)
        {
            RetroMediaLab.Chips.Video.NEC.PowerVr2.RenderPass current = context.RenderPasses[index];
            PvrRuntime.Log.Debug("Render pass {0} OP {1} PT {2} TR {3} MV {4} autosort {5}", index + 1,
                current.OpCount - previous.OpCount, current.PtCount - previous.PtCount,
                current.TrCount - previous.TrCount, current.MvoCount - previous.MvoCount, current.AutoSort ? 1 : 0);
            DrawList(commandBuffer, ListType_Opaque, false, context.GlobalParamOp, previous.OpCount, current.OpCount);
            DrawList(commandBuffer, ListType_Punch_Through, false, context.GlobalParamPt, previous.PtCount, current.PtCount);
            DrawModVols(commandBuffer, (int)previous.MvoCount, (int)(current.MvoCount - previous.MvoCount));
            if (current.AutoSort)
            {
                if (!PvrConfiguration.PerStripSorting)
                    DrawSorted(commandBuffer, context.SortedTriangles, previous.SortedTrCount, current.SortedTrCount,
                        index + 1 < context.RenderPasses.Count);
                else
                    DrawList(commandBuffer, ListType_Translucent, true, context.GlobalParamTr, previous.TrCount, current.TrCount);
            }
            else
            {
                DrawList(commandBuffer, ListType_Translucent, false, context.GlobalParamTr, previous.TrCount, current.TrCount);
            }
            previous = current;
        }
        _curMainBuffer = default;
        return !context.IsRtt;
    }

    private void BindMainVertexBuffer(CommandBuffer commandBuffer, ulong offset)
    {
        Buffer buffer = _curMainBuffer;
        GetContext().Api.CmdBindVertexBuffers(commandBuffer, 0, 1, &buffer, &offset);
    }

    protected void BeginFramebufferRenderPass(CommandBuffer commandBuffer, VkRenderPass renderPass, Framebuffer framebuffer, Extent2D extent)
    {
        ClearValue* clears = stackalloc ClearValue[2]
        {
            new() { Color = new ClearColorValue(0f, 0f, 0f, 1f) },
            new() { DepthStencil = new ClearDepthStencilValue(0f, 0) }
        };
        RenderPassBeginInfo info = new()
        {
            SType = StructureType.RenderPassBeginInfo,
            RenderPass = renderPass,
            Framebuffer = framebuffer,
            RenderArea = new Rect2D(new Offset2D(0, 0), extent),
            ClearValueCount = 2,
            PClearValues = clears
        };
        GetContext().Api.CmdBeginRenderPass(commandBuffer, &info, SubpassContents.Inline);
    }
}

internal unsafe sealed partial class PvrVulkanTextureDrawer
{
    internal void Init(PvrVulkanSamplerManager samplerManager, PvrVulkanShaders shaderManager, PvrVulkanTextureCache textureCache)
    {
        _rttPipelineManager ??= new PvrVulkanRttPipelineManager();
        _rttPipelineManager.Init(shaderManager);
        base.Init(samplerManager, _rttPipelineManager);
        _textureCache = textureCache;
    }

    protected override CommandBuffer BeginRenderPass()
    {
        RenderContext context = RendContext!;
        PvrRuntime.Log.Debug("RenderToTexture packmode={0} stride={1} - {2} x {3} @ {4:x6}",
            context.FbWCtrl.fb_packmode, context.FbWLineStride * 8, context.FbClip.Size.X, context.FbClip.Size.Y,
            context.FbWSof1 & PvrMemory.VramMask);

        _textureAddr = context.FbWSof1 & PvrMemory.VramMask;
        uint width = context.FramebufferWidth;
        uint height = context.FramebufferHeight;
        Matrices.CalcMatrices(context, (int)width, (int)height);
        _rttPipelineManager!.CheckSettingsChange();
        PvrVulkanContext vulkan = GetContext();
        var device = vulkan.GetDevice();
        Vk api = vulkan.Api;

        NewImage();
        CommandBuffer commandBuffer = CommandPool!.Allocate(true);
        PvrVk.Begin(commandBuffer, CommandBufferUsageFlags.OneTimeSubmitBit);
        if (_depthAttachment == null || width > _depthAttachment.GetExtent().Width || height > _depthAttachment.GetExtent().Height)
        {
            if (_depthAttachment == null)
                _depthAttachment = new PvrVulkanFramebufferAttachment(vulkan.GetPhysicalDevice(), device);
            else
                vulkan.WaitIdle();

            _depthAttachment.Init(width, height, vulkan.GetDepthFormat(),
                ImageUsageFlags.DepthStencilAttachmentBit | ImageUsageFlags.TransientAttachmentBit, "RTT DEPTH ATTACHMENT");
            SetImageLayout(commandBuffer, _depthAttachment.GetImage(), vulkan.GetDepthFormat(), 1,
                ImageLayout.Undefined, ImageLayout.DepthStencilAttachmentOptimal);
        }

        Image colorImage;
        ImageView colorImageView;
        ImageLayout currentLayout;
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
                currentLayout = ImageLayout.Undefined;
            }
            else
            {
                currentLayout = ImageLayout.ShaderReadOnlyOptimal;
            }
            colorImage = _texture.GetImage();
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
                currentLayout = ImageLayout.Undefined;
            }
            else
            {
                currentLayout = ImageLayout.TransferSrcOptimal;
            }
            colorImage = _colorAttachment.GetImage();
            colorImageView = _colorAttachment.GetImageView();
        }

        _width = width;
        _height = height;
        SetImageLayout(commandBuffer, colorImage, Format.R8G8B8A8Unorm, 1, currentLayout, ImageLayout.ColorAttachmentOptimal);

        ImageView[] views = [colorImageView, _depthAttachment.GetImageView()];
        int swapChainSize = (int)vulkan.GetSwapChainSize();
        while (_framebuffers.Count > swapChainSize)
        {
            _framebuffers[^1]?.Dispose();
            _framebuffers.RemoveAt(_framebuffers.Count - 1);
        }
        while (_framebuffers.Count < swapChainSize)
            _framebuffers.Add(null!);
        int index = GetCurrentImage();
        _framebuffers[index]?.Dispose();
        VkRenderPass renderPass = _rttPipelineManager.GetRenderPass();
        _framebuffers[index] = device.CreateFramebufferUnique(renderPass, views, width, height, 1);
        BeginFramebufferRenderPass(commandBuffer, renderPass, _framebuffers[index].Handle, new Extent2D(width, height));
        Viewport viewport = new(0f, 0f, width, height, 1f, 0f);
        api.CmdSetViewport(commandBuffer, 0, 1, &viewport);
        SetBaseScissor();
        Rect2D scissor = BaseScissor;
        api.CmdSetScissor(commandBuffer, 0, 1, &scissor);
        CurrentCommandBuffer = commandBuffer;
        return commandBuffer;
    }

    internal override void EndRenderPass()
    {
        Vk api = GetContext().Api;
        api.CmdEndRenderPass(CurrentCommandBuffer);
        RenderContext context = RendContext!;
        uint width = context.FramebufferWidth;
        uint height = context.FramebufferHeight;
        if (PvrConfiguration.RenderToTextureBuffer)
            CopyFramebufferToBuffer(CurrentCommandBuffer, _colorAttachment!, width, height);

        PvrVk.End(CurrentCommandBuffer);
        CurrentCommandBuffer = default;

        if (PvrConfiguration.RenderToTextureBuffer)
        {
            CommandPool!.EndFrameAndWait();
            ushort* destination = (ushort*)(PvrMemory.VramPointer + _textureAddr);
            using var temporary = new PvrPixelBuffer<uint>();
            temporary.Init(width, height);
            _colorAttachment!.GetBufferData()!.Download(width * height * 4, temporary.Data());
            PvrTextureCache.WriteTextureToVram(width, height, (byte*)temporary.Data(), destination,
                context.FbWCtrl, context.FbWLineStride * 8, context.FbClip);
        }
        else
        {
            CommandPool!.EndFrame();
            _texture!.Dirty = 0;
            _texture.UnprotectVram();
        }
        base.EndRenderPass();
    }
}

internal unsafe sealed partial class PvrVulkanScreenDrawer
{
    internal void Init(PvrVulkanSamplerManager samplerManager, PvrVulkanShaders shaderManager, in Extent2D viewport)
    {
        _emulateFramebuffer = PvrConfiguration.EmulateFramebuffer;
        _shaderManager = shaderManager;
        if (!_viewport.Equals(viewport))
        {
            if (_framebuffers.Count != 0)
            {
                PvrRuntime.Verify(CommandPool != null);
                foreach (PvrVulkanOwned<Framebuffer> framebuffer in _framebuffers)
                    CommandPool!.AddToFlight(new PvrVulkanDeleter<PvrVulkanOwned<Framebuffer>>(framebuffer));
                _framebuffers.Clear();
            }
            foreach (PvrVulkanFramebufferAttachment attachment in _colorAttachments)
                CommandPool!.AddToFlight(new PvrVulkanDeleter<PvrVulkanFramebufferAttachment>(attachment));
            _colorAttachments.Clear();
            if (_depthAttachment != null)
            {
                CommandPool!.AddToFlight(new PvrVulkanDeleter<PvrVulkanFramebufferAttachment>(_depthAttachment));
                _depthAttachment = null;
            }
            _transitionNeeded.Clear();
            _clearNeeded.Clear();
        }
        _viewport = viewport;

        PvrVulkanContext vulkan = GetContext();
        var device = vulkan.GetDevice();
        if (_depthAttachment == null)
        {
            _depthAttachment = new PvrVulkanFramebufferAttachment(vulkan.GetPhysicalDevice(), device);
            _depthAttachment.Init(viewport.Width, viewport.Height, vulkan.GetDepthFormat(),
                ImageUsageFlags.DepthStencilAttachmentBit | ImageUsageFlags.TransientAttachmentBit, "DEPTH ATTACHMENT");
        }

        if (_renderPassLoad == null)
        {
            ImageLayout layout = PvrConfiguration.EmulateFramebuffer ? ImageLayout.TransferSrcOptimal : ImageLayout.ShaderReadOnlyOptimal;
            AttachmentDescription[] attachments =
            [
                new(0, Format.R8G8B8A8Unorm, SampleCountFlags.Count1Bit,
                    AttachmentLoadOp.Load, AttachmentStoreOp.Store, AttachmentLoadOp.DontCare, AttachmentStoreOp.DontCare,
                    layout, layout),
                new(0, vulkan.GetDepthFormat(), SampleCountFlags.Count1Bit,
                    AttachmentLoadOp.Clear, AttachmentStoreOp.DontCare, AttachmentLoadOp.Clear, AttachmentStoreOp.DontCare,
                    ImageLayout.Undefined, ImageLayout.DepthStencilAttachmentOptimal)
            ];
            AttachmentReference color = new(0, ImageLayout.ColorAttachmentOptimal);
            AttachmentReference depth = new(1, ImageLayout.DepthStencilAttachmentOptimal);
            SubpassDescription subpass = new() { PipelineBindPoint = PipelineBindPoint.Graphics, ColorAttachmentCount = 1,
                PColorAttachments = &color, PDepthStencilAttachment = &depth };
            SubpassDependency dependency = new(0, uint.MaxValue, PipelineStageFlags.ColorAttachmentOutputBit,
                PipelineStageFlags.FragmentShaderBit, AccessFlags.ColorAttachmentWriteBit, AccessFlags.ShaderReadBit, DependencyFlags.ByRegionBit);
            fixed (AttachmentDescription* attachmentData = attachments)
            {
                RenderPassCreateInfo info = new()
                {
                    SType = StructureType.RenderPassCreateInfo,
                    AttachmentCount = 2,
                    PAttachments = attachmentData,
                    SubpassCount = 1,
                    PSubpasses = &subpass,
                    DependencyCount = 1,
                    PDependencies = &dependency
                };
                _renderPassLoad = device.CreateRenderPassUnique(info);
                attachments[0].LoadOp = AttachmentLoadOp.Clear;
                _renderPassClear = device.CreateRenderPassUnique(info);
            }
        }

        int size = (int)GetSwapChainSize();
        if (_colorAttachments.Count > size)
        {
            while (_colorAttachments.Count > size)
            {
                _colorAttachments[^1].Dispose();
                _colorAttachments.RemoveAt(_colorAttachments.Count - 1);
                _framebuffers[^1].Dispose();
                _framebuffers.RemoveAt(_framebuffers.Count - 1);
                _transitionNeeded.RemoveAt(_transitionNeeded.Count - 1);
                _clearNeeded.RemoveAt(_clearNeeded.Count - 1);
            }
        }
        else
        {
            ImageView[] attachments = [default, _depthAttachment.GetImageView()];
            while (_colorAttachments.Count < size)
            {
                PvrVulkanFramebufferAttachment attachment = new(vulkan.GetPhysicalDevice(), device);
                _colorAttachments.Add(attachment);
                ImageUsageFlags usage = ImageUsageFlags.ColorAttachmentBit;
                if (PvrConfiguration.EmulateFramebuffer)
                    usage |= ImageUsageFlags.TransferSrcBit;
                else
                    usage |= ImageUsageFlags.SampledBit;
                attachment.Init(viewport.Width, viewport.Height, Format.R8G8B8A8Unorm, usage,
                    "COLOR ATTACHMENT " + (_colorAttachments.Count - 1));
                attachments[0] = attachment.GetImageView();
                _framebuffers.Add(device.CreateFramebufferUnique(_renderPassLoad.Handle, attachments, viewport.Width, viewport.Height, 1));
                _transitionNeeded.Add(true);
                _clearNeeded.Add(true);
            }
        }
        _frameRendered = false;

        _screenPipelineManager ??= new PvrVulkanPipelineManager();
        _screenPipelineManager.Init(shaderManager, _renderPassLoad.Handle);
        base.Init(samplerManager, _screenPipelineManager);
    }

    protected override CommandBuffer BeginRenderPass()
    {
        Vk api = GetContext().Api;
        if (!RenderPassStarted)
        {
            NewImage();
            _frameRendered = false;
            CommandBuffer commandBuffer = CommandPool!.Allocate(true);
            PvrVk.Begin(commandBuffer, CommandBufferUsageFlags.OneTimeSubmitBit);

            int index = GetCurrentImage();
            if (_transitionNeeded[index])
            {
                SetImageLayout(commandBuffer, _colorAttachments[index].GetImage(), Format.R8G8B8A8Unorm, 1,
                    ImageLayout.Undefined, _emulateFramebuffer ? ImageLayout.TransferSrcOptimal : ImageLayout.ShaderReadOnlyOptimal);
                _transitionNeeded[index] = false;
            }
            VkRenderPass renderPass = _clearNeeded[index] || RendContext!.ClearFramebuffer
                ? _renderPassClear!.Handle : _renderPassLoad!.Handle;
            _clearNeeded[index] = false;
            BeginFramebufferRenderPass(commandBuffer, renderPass, _framebuffers[index].Handle, _viewport);
            CurrentCommandBuffer = commandBuffer;
            RenderPassStarted = true;
        }

        Viewport viewport = new(0f, 0f, _viewport.Width, _viewport.Height, 1f, 0f);
        api.CmdSetViewport(CurrentCommandBuffer, 0, 1, &viewport);
        Matrices.CalcMatrices(RendContext!, (int)_viewport.Width, (int)_viewport.Height);
        SetBaseScissor(_viewport);
        Rect2D scissor = BaseScissor;
        api.CmdSetScissor(CurrentCommandBuffer, 0, 1, &scissor);
        return CurrentCommandBuffer;
    }

    internal override void EndRenderPass()
    {
        if (!RenderPassStarted)
            return;
        Vk api = GetContext().Api;
        api.CmdEndRenderPass(CurrentCommandBuffer);
        if (_emulateFramebuffer)
        {
            ScaleAndWriteFramebuffer(CurrentCommandBuffer, _colorAttachments[GetCurrentImage()]);
        }
        else
        {
            PvrVk.End(CurrentCommandBuffer);
            CommandPool!.EndFrame();
            _aspectRatio = PvrTransformMatrix.GetOutputFramebufferAspectRatio();
        }
        CurrentCommandBuffer = default;
        base.EndRenderPass();
        _frameRendered = true;
    }
}
