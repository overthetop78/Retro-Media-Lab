using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Vulkan;

internal sealed unsafe class PvrVulkanDescriptorSetManager
{
    internal void Init(PvrVulkanSamplerManager samplerManager, PipelineLayout pipelineLayout,
        DescriptorSetLayout perFrameLayout, DescriptorSetLayout perPolyLayout)
    {
        _samplerManager = samplerManager;
        _pipelineLayout = pipelineLayout;
        _perFrameAlloc.SetLayout(perFrameLayout);
        _perPolyAlloc.SetLayout(perPolyLayout);
    }

    internal void UpdateUniforms(Buffer buffer, uint vertexUniformOffset, uint fragmentUniformOffset,
        ImageView fogImageView, ImageView paletteImageView)
    {
        _perFrameDescSet = _perFrameAlloc.Alloc();
        _perPolyDescSets.Clear();

        DescriptorBufferInfo vertexInfo = new(buffer, vertexUniformOffset, (ulong)sizeof(PvrVulkanVertexShaderUniforms));
        DescriptorBufferInfo fragmentInfo = new(buffer, fragmentUniformOffset, (ulong)sizeof(PvrVulkanFragmentShaderUniforms));
        List<PvrVulkanDescriptorWrite> writes =
        [
            new(_perFrameDescSet, 0, 0, DescriptorType.UniformBuffer, null, vertexInfo),
            new(_perFrameDescSet, 1, 0, DescriptorType.UniformBuffer, null, fragmentInfo)
        ];
        if (fogImageView.Handle != 0)
        {
            TSP fogTsp = new() { FilterMode = 1, ClampU = 1, ClampV = 1 };
            Sampler sampler = _samplerManager!.GetSampler(fogTsp);
            writes.Add(new(_perFrameDescSet, 2, 0, DescriptorType.CombinedImageSampler,
                new DescriptorImageInfo(sampler, fogImageView, ImageLayout.ShaderReadOnlyOptimal), null));
        }
        if (paletteImageView.Handle != 0)
        {
            TSP paletteTsp = new() { FilterMode = 0, ClampU = 1, ClampV = 1 };
            Sampler sampler = _samplerManager!.GetSampler(paletteTsp);
            writes.Add(new(_perFrameDescSet, 3, 0, DescriptorType.CombinedImageSampler,
                new DescriptorImageInfo(sampler, paletteImageView, ImageLayout.ShaderReadOnlyOptimal), null));
        }
        GetContext().GetDevice().UpdateDescriptorSets(writes);
    }

    internal void BindPerPolyDescriptorSets(CommandBuffer commandBuffer, in PolyParam poly, int polyNumber,
        Buffer buffer, ulong uniformOffset, ulong lightOffset, bool punchThrough)
    {
        nuint key = (nuint)Unsafe.AsPointer(ref Unsafe.AsRef(in poly));
        if (!_perPolyDescSets.TryGetValue(key, out DescriptorSet set))
        {
            set = _perPolyAlloc.Alloc();
            List<PvrVulkanDescriptorWrite> writes = [];
            if (poly.Texture != null)
            {
                DescriptorImageInfo image = new(_samplerManager!.GetSampler(poly, punchThrough),
                    ((PvrVulkanTexture)poly.Texture).GetReadOnlyImageView(), ImageLayout.ShaderReadOnlyOptimal);
                writes.Add(new(set, 0, 0, DescriptorType.CombinedImageSampler, image, null));
            }
            if (poly.IsNaomi2())
            {
                uint alignment = (uint)GetContext().GetUniformBufferAlignment();
                uint size = (uint)sizeof(PvrVulkanN2VertexShaderUniforms);
                ulong stride = size + PvrVulkanUtilities.Align(size, alignment);
                writes.Add(new(set, 2, 0, DescriptorType.UniformBuffer, null,
                    new DescriptorBufferInfo(buffer, uniformOffset + (ulong)polyNumber * stride, size)));

                size = (uint)sizeof(N2LightModel);
                stride = size + PvrVulkanUtilities.Align(size, alignment);
                writes.Add(new(set, 3, 0, DescriptorType.UniformBuffer, null,
                    new DescriptorBufferInfo(buffer, lightOffset + (ulong)poly.LightModel * stride, size)));
            }
            GetContext().GetDevice().UpdateDescriptorSets(writes);
            _perPolyDescSets[key] = set;
        }
        GetContext().Api.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, _pipelineLayout, 1, 1, &set, 0, null);
    }

    internal void BindPerPolyDescriptorSets(CommandBuffer commandBuffer, in ModifierVolumeParam modifier, int polyNumber,
        Buffer buffer, ulong uniformOffset)
    {
        if (!modifier.IsNaomi2())
            return;

        nuint key = (nuint)Unsafe.AsPointer(ref Unsafe.AsRef(in modifier));
        if (!_perPolyDescSets.TryGetValue(key, out DescriptorSet set))
        {
            set = _perPolyAlloc.Alloc();
            uint alignment = (uint)GetContext().GetUniformBufferAlignment();
            uint size = (uint)sizeof(PvrVulkanN2VertexShaderUniforms);
            ulong stride = size + PvrVulkanUtilities.Align(size, alignment);
            PvrVulkanDescriptorWrite write = new(set, 2, 0, DescriptorType.UniformBuffer, null,
                new DescriptorBufferInfo(buffer, uniformOffset + (ulong)polyNumber * stride, size));
            GetContext().GetDevice().UpdateDescriptorSets([write]);
            _perPolyDescSets[key] = set;
        }
        GetContext().Api.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, _pipelineLayout, 1, 1, &set, 0, null);
    }

    internal void BindPerFrameDescriptorSets(CommandBuffer commandBuffer)
    {
        DescriptorSet set = _perFrameDescSet;
        GetContext().Api.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, _pipelineLayout, 0, 1, &set, 0, null);
    }

    internal void NextFrame()
    {
        _perFrameAlloc.NextFrame();
        _perPolyAlloc.NextFrame();
        _perFrameDescSet = default;
        _perPolyDescSets.Clear();
    }

    internal void Term()
    {
        try
        {
            _perFrameAlloc.Term();
        }
        finally
        {
            _perPolyAlloc.Term();
        }
    }

    private static PvrVulkanContext GetContext() => PvrVulkanContext.Instance;
    private PipelineLayout _pipelineLayout;
    private readonly PvrVulkanDescriptorSets _perFrameAlloc = new();
    private readonly PvrVulkanDescriptorSets _perPolyAlloc = new();
    private DescriptorSet _perFrameDescSet;
    private readonly Dictionary<nuint, DescriptorSet> _perPolyDescSets = [];
    private PvrVulkanSamplerManager? _samplerManager;
}

internal unsafe partial class PvrVulkanPipelineManager : IDisposable
{
    internal void Init(PvrVulkanShaders shaderManager, RenderPass renderPass)
    {
        ShaderManager = shaderManager;
        if (_perFrameLayout == null)
        {
            DescriptorSetLayoutBinding[] perFrameBindings =
            [
                new(0, DescriptorType.UniformBuffer, 1, ShaderStageFlags.VertexBit),
                new(1, DescriptorType.UniformBuffer, 1, ShaderStageFlags.FragmentBit),
                new(2, DescriptorType.CombinedImageSampler, 1, ShaderStageFlags.FragmentBit),
                new(3, DescriptorType.CombinedImageSampler, 1, ShaderStageFlags.FragmentBit)
            ];
            DescriptorSetLayoutBinding[] perPolyBindings =
            [
                new(0, DescriptorType.CombinedImageSampler, 1, ShaderStageFlags.FragmentBit),
                new(2, DescriptorType.UniformBuffer, 1, ShaderStageFlags.VertexBit),
                new(3, DescriptorType.UniformBuffer, 1, ShaderStageFlags.VertexBit)
            ];

            PvrVulkanDevice device = GetContext().GetDevice();
            _perFrameLayout = device.CreateDescriptorSetLayoutUnique(perFrameBindings);
            _perPolyLayout = device.CreateDescriptorSetLayoutUnique(perPolyBindings);
            DescriptorSetLayout[] layouts = [_perFrameLayout.Handle, _perPolyLayout.Handle];
            PushConstantRange pushConstant = new(ShaderStageFlags.FragmentBit, 0, 24);
            _pipelineLayout = device.CreatePipelineLayoutUnique(layouts, [pushConstant]);
        }

        if (_renderPass.Handle != renderPass.Handle)
        {
            _renderPass = renderPass;
            Reset();
        }
    }

    internal Pipeline GetPipeline(uint listType, bool sortTriangles, in PolyParam poly, int gpuPalette, bool dithering)
    {
        ulong hash = Hash(listType, sortTriangles, in poly, gpuPalette, dithering);
        if (_pipelines.TryGetValue(hash, out PvrVulkanOwned<Pipeline>? pipeline))
            return pipeline.Handle;
        CreatePipeline(listType, sortTriangles, in poly, gpuPalette, dithering);
        return _pipelines[hash].Handle;
    }

    internal Pipeline GetModifierVolumePipeline(PvrVulkanModVolMode mode, int cullMode, bool naomi2)
    {
        uint hash = Hash(mode, cullMode, naomi2);
        if (_modVolPipelines.TryGetValue(hash, out PvrVulkanOwned<Pipeline>? pipeline))
            return pipeline.Handle;
        CreateModVolPipeline(mode, cullMode, naomi2);
        return _modVolPipelines[hash].Handle;
    }

    internal Pipeline GetDepthPassPipeline(int cullMode, bool naomi2)
    {
        uint hash = Hash(cullMode, naomi2);
        if (_depthPassPipelines.TryGetValue(hash, out PvrVulkanOwned<Pipeline>? pipeline))
            return pipeline.Handle;
        CreateDepthPassPipeline(cullMode, naomi2);
        return _depthPassPipelines[hash].Handle;
    }

    internal void Reset()
    {
        try
        {
            ReleasePipelines(_pipelines);
        }
        finally
        {
            ReleasePipelines(_modVolPipelines);
        }
    }

    internal PipelineLayout GetPipelineLayout() => _pipelineLayout!.Handle;
    internal DescriptorSetLayout GetPerFrameDSLayout() => _perFrameLayout!.Handle;
    internal DescriptorSetLayout GetPerPolyDSLayout() => _perPolyLayout!.Handle;
    internal RenderPass GetRenderPass() => _renderPass;

    private static ulong Hash(uint listType, bool sortTriangles, in PolyParam poly, int gpuPalette, bool dithering)
    {
        ulong hash = poly.Pcw.Gouraud | (poly.Pcw.Offset << 1) | (poly.Pcw.Texture << 2) | (poly.Pcw.Shadow << 3)
            | (((poly.TileClip >> 28) == 3 ? 1u : 0u) << 4);
        hash |= (listType >> 1) << 5;
        bool ignoreTexAlpha = poly.Tsp.IgnoreTexA != 0 || poly.Tcw.PixelFmt == (uint)PixelFormat.Pixel565;
        hash |= (poly.Tsp.ShadInstr << 7) | ((ignoreTexAlpha ? 1u : 0u) << 9) | (poly.Tsp.UseAlpha << 10)
            | (poly.Tsp.ColorClamp << 11) | ((PvrConfiguration.Fog ? poly.Tsp.FogCtrl : 2u) << 12)
            | (poly.Tsp.SrcInstr << 14) | (poly.Tsp.DstInstr << 17);
        hash |= (poly.Isp.ZWriteDis << 20) | (poly.Isp.CullMode << 21) | (poly.Isp.DepthMode << 23);
        hash |= ((sortTriangles ? 1ul : 0ul) << 26) | ((ulong)gpuPalette << 27) | ((poly.IsNaomi2() ? 1ul : 0ul) << 29);
        hash |= (!PvrConfiguration.IsNaomi2 && PvrConfiguration.NativeDepthInterpolation ? 1ul : 0ul) << 30;
        hash |= (poly.Tcw.PixelFmt == (uint)PixelFormat.PixelBumpMap ? 1ul : 0ul) << 31;
        hash |= (dithering ? 1ul : 0ul) << 32;
        return hash;
    }

    private static uint Hash(PvrVulkanModVolMode mode, int cullMode, bool naomi2) =>
        ((uint)mode << 2) | (uint)cullMode | ((naomi2 ? 1u : 0u) << 5)
        | ((!PvrConfiguration.IsNaomi2 && PvrConfiguration.NativeDepthInterpolation ? 1u : 0u) << 6);

    private static uint Hash(int cullMode, bool naomi2) =>
        (uint)cullMode | ((naomi2 ? 1u : 0u) << 2)
        | ((!PvrConfiguration.IsNaomi2 && PvrConfiguration.NativeDepthInterpolation ? 1u : 0u) << 3);

    private static ReadOnlySpan<VertexInputAttributeDescription> GetMainVertexInputStateCreateInfo(bool full = true, bool naomi2 = false)
    {
        ReadOnlySpan<VertexInputAttributeDescription> attributes = MainLightAttributes;
        if (full)
        {
            attributes = MainAttributes;
            if (!naomi2)
                attributes = attributes[..^1];
        }
        return attributes;
    }

    private static readonly VertexInputBindingDescription[] MainBindings =
    [
        new(0, (uint)sizeof(TaVertex), VertexInputRate.Vertex)
    ];

    private static readonly VertexInputAttributeDescription[] MainAttributes =
    [
        new(0, 0, Format.R32G32B32Sfloat, (uint)Marshal.OffsetOf<TaVertex>(nameof(TaVertex.X))),
        new(1, 0, Format.R8G8B8A8Unorm, (uint)Marshal.OffsetOf<TaVertex>(nameof(TaVertex.Col))),
        new(2, 0, Format.R8G8B8A8Unorm, (uint)Marshal.OffsetOf<TaVertex>(nameof(TaVertex.Spc))),
        new(3, 0, Format.R32G32Sfloat, (uint)Marshal.OffsetOf<TaVertex>(nameof(TaVertex.U))),
        new(4, 0, Format.R32G32B32Sfloat, (uint)Marshal.OffsetOf<TaVertex>(nameof(TaVertex.Nx)))
    ];

    private static readonly VertexInputAttributeDescription[] MainLightAttributes = [MainAttributes[0]];
    private readonly SortedDictionary<ulong, PvrVulkanOwned<Pipeline>> _pipelines = [];
    private readonly SortedDictionary<uint, PvrVulkanOwned<Pipeline>> _modVolPipelines = [];
    private readonly SortedDictionary<uint, PvrVulkanOwned<Pipeline>> _depthPassPipelines = [];
    private PvrVulkanOwned<PipelineLayout>? _pipelineLayout;
    private PvrVulkanOwned<DescriptorSetLayout>? _perFrameLayout;
    private PvrVulkanOwned<DescriptorSetLayout>? _perPolyLayout;

    protected static PvrVulkanContext GetContext() => PvrVulkanContext.Instance;
    private RenderPass _renderPass;
    protected PvrVulkanShaders? ShaderManager;

    public virtual void Dispose()
    {
        try
        {
            Reset();
        }
        finally
        {
            try
            {
                ReleasePipelines(_depthPassPipelines);
            }
            finally
            {
                try
                {
                    _pipelineLayout?.Dispose();
                }
                finally
                {
                    try
                    {
                        _perFrameLayout?.Dispose();
                    }
                    finally
                    {
                        _perPolyLayout?.Dispose();
                    }
                }
            }
        }
    }

    private static void ReleasePipelines<T>(SortedDictionary<T, PvrVulkanOwned<Pipeline>> pipelines) where T : notnull
    {
        try
        {
            foreach (PvrVulkanOwned<Pipeline> pipeline in pipelines.Values)
                pipeline.Dispose();
        }
        finally
        {
            pipelines.Clear();
        }
    }
}

internal sealed unsafe class PvrVulkanRttPipelineManager : PvrVulkanPipelineManager
{
    internal void Init(PvrVulkanShaders shaderManager)
    {
        _renderToTextureBuffer = PvrConfiguration.RenderToTextureBuffer;
        AttachmentDescription* attachments = stackalloc AttachmentDescription[2]
        {
            new(0, Format.R8G8B8A8Unorm, SampleCountFlags.Count1Bit,
                AttachmentLoadOp.Clear, AttachmentStoreOp.Store, AttachmentLoadOp.DontCare, AttachmentStoreOp.DontCare,
                ImageLayout.ColorAttachmentOptimal, _renderToTextureBuffer ? ImageLayout.TransferSrcOptimal : ImageLayout.ShaderReadOnlyOptimal),
            new(0, GetContext().GetDepthFormat(), SampleCountFlags.Count1Bit,
                AttachmentLoadOp.Clear, AttachmentStoreOp.DontCare, AttachmentLoadOp.Clear, AttachmentStoreOp.DontCare,
                ImageLayout.Undefined, ImageLayout.DepthStencilAttachmentOptimal)
        };
        AttachmentReference colorReference = new(0, ImageLayout.ColorAttachmentOptimal);
        AttachmentReference depthReference = new(1, ImageLayout.DepthStencilAttachmentOptimal);
        SubpassDescription subpass = new()
        {
            PipelineBindPoint = PipelineBindPoint.Graphics,
            ColorAttachmentCount = 1,
            PColorAttachments = &colorReference,
            PDepthStencilAttachment = &depthReference
        };
        SubpassDependency* dependencies = stackalloc SubpassDependency[2]
        {
            new(uint.MaxValue, 0, PipelineStageFlags.FragmentShaderBit, PipelineStageFlags.ColorAttachmentOutputBit,
                AccessFlags.ShaderReadBit, AccessFlags.ColorAttachmentWriteBit),
            new(0, uint.MaxValue, PipelineStageFlags.ColorAttachmentOutputBit, PipelineStageFlags.FragmentShaderBit,
                AccessFlags.ColorAttachmentWriteBit, AccessFlags.ShaderReadBit)
        };
        SubpassDependency vramWriteDependency = new(0, uint.MaxValue,
            PipelineStageFlags.ColorAttachmentOutputBit, PipelineStageFlags.TransferBit | PipelineStageFlags.HostBit,
            AccessFlags.ColorAttachmentWriteBit, AccessFlags.TransferReadBit | AccessFlags.HostReadBit);
        RenderPassCreateInfo info = new()
        {
            SType = StructureType.RenderPassCreateInfo,
            AttachmentCount = 2,
            PAttachments = attachments,
            SubpassCount = 1,
            PSubpasses = &subpass,
            DependencyCount = _renderToTextureBuffer ? 1u : 2u,
            PDependencies = _renderToTextureBuffer ? &vramWriteDependency : dependencies
        };
        var renderPass = GetContext().GetDevice().CreateRenderPassUnique(info);
        _rttRenderPass?.Dispose();
        _rttRenderPass = renderPass;

        base.Init(shaderManager, _rttRenderPass.Handle);
    }

    internal void CheckSettingsChange()
    {
        if (_renderToTextureBuffer != PvrConfiguration.RenderToTextureBuffer)
            Init(ShaderManager!);
    }

    public override void Dispose()
    {
        try
        {
            _rttRenderPass?.Dispose();
        }
        finally
        {
            base.Dispose();
        }
    }

    private PvrVulkanOwned<RenderPass>? _rttRenderPass;
    private bool _renderToTextureBuffer;
}
