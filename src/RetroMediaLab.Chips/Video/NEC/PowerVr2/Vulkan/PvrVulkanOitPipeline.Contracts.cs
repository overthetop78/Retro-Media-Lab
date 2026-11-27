using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Vulkan;

internal sealed unsafe class PvrVulkanOitDescriptorSets
{
    internal struct VertexShaderUniforms
    {
        internal Matrix4x4 NdcMat;
    }

    internal struct FragmentShaderUniforms
    {
        internal fixed float ColorClampMin[4];
        internal fixed float ColorClampMax[4];
        internal fixed float FogColRam[4];
        internal fixed float FogColVert[4];
        internal fixed float DitherDivisor[4];
        internal float AlphaTestValue;
        internal float FogDensity;
        internal float ShadeScaleFactor;
        internal uint PixelBufferSize;
        internal ulong PixelBufferAddress;
        internal uint ViewportWidth;
        internal uint Pad;
    }

    [StructLayout(LayoutKind.Sequential, Size = 96)]
    internal struct PushConstants
    {
        internal Vector4 ClipTest;
        internal fixed int BlendMode0[4];
        internal float TrilinearAlpha;
        internal float PaletteIndex;
        internal fixed int Pad[2];
        internal fixed int BlendMode1[4];
        internal int ShadingInstr0;
        internal int ShadingInstr1;
        internal int FogControl0;
        internal int FogControl1;
        internal int UseAlpha0;
        internal int UseAlpha1;
        internal int IgnoreTexAlpha0;
        internal int IgnoreTexAlpha1;
    }

    internal struct VtxPushConstants
    {
        internal int PolyNumber;
    }

    internal void Init(PvrVulkanSamplerManager samplerManager, PipelineLayout pipelineLayout,
        DescriptorSetLayout perFrameLayout, DescriptorSetLayout perPolyLayout, DescriptorSetLayout colorInputLayout)
    {
        _samplerManager = samplerManager;
        _pipelineLayout = pipelineLayout;
        _perFrameAlloc.SetLayout(perFrameLayout);
        _perPolyAlloc.SetLayout(perPolyLayout);
        _colorInputAlloc.SetLayout(colorInputLayout);
    }

    internal void UpdateUniforms(Buffer buffer, uint vertexUniformOffset, uint fragmentUniformOffset, ImageView fogImageView,
        uint polyParamsOffset, uint polyParamsSize, ImageView stencilImageView, ImageView depthImageView,
        ImageView paletteImageView, PvrVulkanOitBuffers oitBuffers)
    {
        _perFrameDescSet = _perFrameAlloc.Alloc();
        _perPolyDescSets.Clear();

        DescriptorBufferInfo vertexInfo = new(buffer, vertexUniformOffset, (ulong)sizeof(VertexShaderUniforms));
        DescriptorBufferInfo fragmentInfo = new(buffer, fragmentUniformOffset, (ulong)sizeof(FragmentShaderUniforms));
        List<PvrVulkanDescriptorWrite> writes =
        [
            new(_perFrameDescSet, 0, 0, DescriptorType.UniformBuffer, null, vertexInfo),
            new(_perFrameDescSet, 1, 0, DescriptorType.UniformBuffer, null, fragmentInfo)
        ];
        if (fogImageView.Handle != 0)
        {
            TSP fogTsp = new() { FilterMode = 1, ClampU = 1, ClampV = 1 };
            Sampler fogSampler = _samplerManager!.GetSampler(fogTsp);
            writes.Add(new(_perFrameDescSet, 2, 0, DescriptorType.CombinedImageSampler,
                new DescriptorImageInfo(fogSampler, fogImageView, ImageLayout.ShaderReadOnlyOptimal), null));
        }
        if (paletteImageView.Handle != 0)
        {
            TSP paletteTsp = new() { FilterMode = 0, ClampU = 1, ClampV = 1 };
            Sampler paletteSampler = _samplerManager!.GetSampler(paletteTsp);
            writes.Add(new(_perFrameDescSet, 6, 0, DescriptorType.CombinedImageSampler,
                new DescriptorImageInfo(paletteSampler, paletteImageView, ImageLayout.ShaderReadOnlyOptimal), null));
        }
        if (polyParamsSize > 0)
            writes.Add(new(_perFrameDescSet, 3, 0, DescriptorType.StorageBuffer, null,
                new DescriptorBufferInfo(buffer, polyParamsOffset, polyParamsSize)));
        writes.Add(new(_perFrameDescSet, 4, 0, DescriptorType.InputAttachment,
            new DescriptorImageInfo(default, stencilImageView, ImageLayout.DepthStencilReadOnlyOptimal), null));
        writes.Add(new(_perFrameDescSet, 5, 0, DescriptorType.InputAttachment,
            new DescriptorImageInfo(default, depthImageView, ImageLayout.DepthStencilReadOnlyOptimal), null));
        oitBuffers.UpdateDescriptorSet(_perFrameDescSet, writes);
        GetContext().GetDevice().UpdateDescriptorSets(writes);
    }

    internal void UpdateColorInputDescSet(int index, ImageView colorImageView)
    {
        _colorInputDescSets[index] = _colorInputAlloc.Alloc();
        PvrVulkanDescriptorWrite write = new(_colorInputDescSets[index], 0, 0, DescriptorType.InputAttachment,
            new DescriptorImageInfo(default, colorImageView, ImageLayout.ShaderReadOnlyOptimal), null);
        GetContext().GetDevice().UpdateDescriptorSets([write]);
    }

    internal void BindPerPolyDescriptorSets(CommandBuffer commandBuffer, in PolyParam poly, int polyNumber, Buffer buffer,
        ulong uniformOffset, ulong lightOffset, bool punchThrough)
    {
        nuint key = (nuint)Unsafe.AsPointer(ref Unsafe.AsRef(in poly));
        if (!_perPolyDescSets.TryGetValue(key, out DescriptorSet set))
        {
            set = _perPolyAlloc.Alloc();
            List<PvrVulkanDescriptorWrite> writes = [];
            if (poly.Texture != null)
            {
                DescriptorImageInfo image = new(_samplerManager!.GetSampler(poly, punchThrough, false),
                    ((PvrVulkanTexture)poly.Texture).GetReadOnlyImageView(), ImageLayout.ShaderReadOnlyOptimal);
                writes.Add(new(set, 0, 0, DescriptorType.CombinedImageSampler, image, null));
            }
            if (poly.Texture1 != null)
            {
                DescriptorImageInfo image = new(_samplerManager!.GetSampler(poly, punchThrough, true),
                    ((PvrVulkanTexture)poly.Texture1).GetReadOnlyImageView(), ImageLayout.ShaderReadOnlyOptimal);
                writes.Add(new(set, 1, 0, DescriptorType.CombinedImageSampler, image, null));
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

    internal void BindPerPolyDescriptorSets(CommandBuffer commandBuffer, in ModifierVolumeParam volume, int polyNumber,
        Buffer buffer, ulong uniformOffset)
    {
        if (!volume.IsNaomi2())
            return;
        nuint key = (nuint)Unsafe.AsPointer(ref Unsafe.AsRef(in volume));
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

    internal void BindColorInputDescSet(CommandBuffer commandBuffer, int index)
    {
        DescriptorSet set = _colorInputDescSets[index];
        GetContext().Api.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, _pipelineLayout, 2, 1, &set, 0, null);
    }

    internal void NextFrame()
    {
        _perFrameDescSet = default;
        _colorInputDescSets[0] = default;
        _colorInputDescSets[1] = default;
        _perPolyDescSets.Clear();
        _perFrameAlloc.NextFrame();
        _perPolyAlloc.NextFrame();
        _colorInputAlloc.NextFrame();
    }

    internal void Term()
    {
        _perFrameAlloc.Term();
        _perPolyAlloc.Term();
        _colorInputAlloc.Term();
    }

    private static PvrVulkanContext GetContext() => PvrVulkanContext.Instance;

    private PipelineLayout _pipelineLayout;
    private readonly DescriptorSet[] _colorInputDescSets = new DescriptorSet[2];
    private readonly PvrVulkanDescriptorSets _perFrameAlloc = new();
    private readonly PvrVulkanDescriptorSets _perPolyAlloc = new();
    private readonly PvrVulkanDescriptorSets _colorInputAlloc = new();
    private DescriptorSet _perFrameDescSet;
    private readonly Dictionary<nuint, DescriptorSet> _perPolyDescSets = [];
    private PvrVulkanSamplerManager? _samplerManager;
}

internal unsafe partial class PvrVulkanOitPipelineManager : IDisposable
{
    internal PvrVulkanOitPipelineManager() => RenderPasses = _ownRenderPasses;

    internal virtual void Init(PvrVulkanOitShaders shaderManager, PvrVulkanOitBuffers oitBuffers)
    {
        ShaderManager = shaderManager;
        OitBuffers = oitBuffers;
        if (_perFrameLayout == null)
        {
            var device = GetContext().GetDevice();
            DescriptorSetLayoutBinding[] perFrameBindings =
            [
                new(0, DescriptorType.UniformBuffer, 1, ShaderStageFlags.VertexBit),
                new(1, DescriptorType.UniformBuffer, 1, ShaderStageFlags.FragmentBit),
                new(2, DescriptorType.CombinedImageSampler, 1, ShaderStageFlags.FragmentBit),
                new(3, DescriptorType.StorageBuffer, 1, ShaderStageFlags.FragmentBit),
                new(4, DescriptorType.InputAttachment, 1, ShaderStageFlags.FragmentBit),
                new(5, DescriptorType.InputAttachment, 1, ShaderStageFlags.FragmentBit),
                new(6, DescriptorType.CombinedImageSampler, 1, ShaderStageFlags.FragmentBit),
                new(7, DescriptorType.StorageBuffer, 1, ShaderStageFlags.FragmentBit),
                new(8, DescriptorType.StorageBuffer, 1, ShaderStageFlags.FragmentBit),
                new(9, DescriptorType.StorageBuffer, 1, ShaderStageFlags.FragmentBit)
            ];
            _perFrameLayout = device.CreateDescriptorSetLayoutUnique(perFrameBindings);

            DescriptorSetLayoutBinding colorInputBinding = new(0, DescriptorType.InputAttachment, 1, ShaderStageFlags.FragmentBit);
            _colorInputLayout = device.CreateDescriptorSetLayoutUnique([colorInputBinding]);

            DescriptorSetLayoutBinding[] perPolyBindings =
            [
                new(0, DescriptorType.CombinedImageSampler, 1, ShaderStageFlags.FragmentBit),
                new(1, DescriptorType.CombinedImageSampler, 1, ShaderStageFlags.FragmentBit),
                new(2, DescriptorType.UniformBuffer, 1, ShaderStageFlags.VertexBit),
                new(3, DescriptorType.UniformBuffer, 1, ShaderStageFlags.VertexBit)
            ];
            _perPolyLayout = device.CreateDescriptorSetLayoutUnique(perPolyBindings);

            PushConstantRange[] pushConstants =
            [
                new(ShaderStageFlags.FragmentBit, 0, (uint)sizeof(PvrVulkanOitDescriptorSets.PushConstants)),
                new(ShaderStageFlags.VertexBit, (uint)sizeof(PvrVulkanOitDescriptorSets.PushConstants),
                    (uint)sizeof(PvrVulkanOitDescriptorSets.VtxPushConstants))
            ];
            DescriptorSetLayout[] layouts = [_perFrameLayout.Handle, _perPolyLayout.Handle, _colorInputLayout.Handle];
            _pipelineLayout = device.CreatePipelineLayoutUnique(layouts, pushConstants);
        }

        ReleasePipelines(_pipelines);
        ReleasePipelines(_modVolPipelines);
        ReleasePipelines(_trModVolPipelines);
        ReleasePipelines(_finalPipelines);
        _clearPipeline?.Dispose();
        _clearPipeline = null;
    }

    public virtual void Dispose()
    {
        ReleasePipelines(_pipelines);
        ReleasePipelines(_modVolPipelines);
        ReleasePipelines(_trModVolPipelines);
        ReleasePipelines(_finalPipelines);
        _clearPipeline?.Dispose();
        _pipelineLayout?.Dispose();
        _perFrameLayout?.Dispose();
        _colorInputLayout?.Dispose();
        _perPolyLayout?.Dispose();
        _ownRenderPasses.Dispose();
    }

    internal Pipeline GetPipeline(uint listType, bool autosort, in PolyParam poly, PvrVulkanOitPass pass, int gpuPalette)
    {
        bool useBda = OitBuffers!.GetPixelBufferAddress() != 0;
        ulong key = Hash(listType, autosort, poly, pass, gpuPalette, useBda);
        if (_pipelines.TryGetValue(key, out PvrVulkanOwned<Pipeline>? pipeline))
            return pipeline.Handle;
        CreatePipeline(listType, autosort, poly, pass, gpuPalette, useBda);
        return _pipelines[key].Handle;
    }

    internal Pipeline GetModifierVolumePipeline(PvrVulkanModVolMode mode, int cullMode, bool naomi2)
    {
        uint key = Hash(mode, cullMode, naomi2, false);
        if (_modVolPipelines.TryGetValue(key, out PvrVulkanOwned<Pipeline>? pipeline))
            return pipeline.Handle;
        CreateModVolPipeline(mode, cullMode, naomi2);
        return _modVolPipelines[key].Handle;
    }

    internal Pipeline GetTrModifierVolumePipeline(PvrVulkanModVolMode mode, int cullMode, bool naomi2)
    {
        CheckMaxLayers();
        bool useBda = OitBuffers!.GetPixelBufferAddress() != 0;
        uint key = Hash(mode, cullMode, naomi2, useBda);
        if (_trModVolPipelines.TryGetValue(key, out PvrVulkanOwned<Pipeline>? pipeline))
            return pipeline.Handle;
        CreateTrModVolPipeline(mode, cullMode, naomi2, useBda);
        return _trModVolPipelines[key].Handle;
    }

    internal Pipeline GetFinalPipeline(bool dithering)
    {
        CheckMaxLayers();
        bool useBda = OitBuffers!.GetPixelBufferAddress() != 0;
        uint key = Hash(dithering, useBda);
        if (_finalPipelines.TryGetValue(key, out PvrVulkanOwned<Pipeline>? pipeline))
            return pipeline.Handle;
        CreateFinalPipeline(dithering, useBda);
        return _finalPipelines[key].Handle;
    }

    internal Pipeline GetClearPipeline()
    {
        if (_clearPipeline == null)
            CreateClearPipeline();
        return _clearPipeline!.Handle;
    }

    internal PipelineLayout GetPipelineLayout() => _pipelineLayout!.Handle;
    internal DescriptorSetLayout GetPerFrameDSLayout() => _perFrameLayout!.Handle;
    internal DescriptorSetLayout GetPerPolyDSLayout() => _perPolyLayout!.Handle;
    internal DescriptorSetLayout GetColorInputDSLayout() => _colorInputLayout!.Handle;

    internal Silk.NET.Vulkan.RenderPass GetRenderPass(bool initial, bool last, bool loadClear = false) =>
        RenderPasses.GetRenderPass(initial, last, loadClear);

    private static ulong Hash(uint listType, bool autosort, in PolyParam poly, PvrVulkanOitPass pass, int gpuPalette, bool useBda)
    {
        ulong hash = poly.Pcw.Gouraud | (poly.Pcw.Offset << 1) | (poly.Pcw.Texture << 2) | (poly.Pcw.Shadow << 3)
            | (((poly.TileClip >> 28) == 3 ? 1u : 0u) << 4);
        hash |= (listType >> 1) << 5;
        if (poly.Tcw1.full != uint.MaxValue || poly.Tsp1.full != uint.MaxValue)
        {
            hash |= (1ul << 33) | (poly.Tsp.ColorClamp << 11);
        }
        else
        {
            bool ignoreTexAlpha = poly.Tsp.IgnoreTexA != 0 || poly.Tcw.PixelFmt == (uint)PixelFormat.Pixel565;
            hash |= (poly.Tsp.ShadInstr << 7) | ((ignoreTexAlpha ? 1u : 0u) << 9) | (poly.Tsp.UseAlpha << 10)
                | (poly.Tsp.ColorClamp << 11) | ((PvrConfiguration.Fog ? poly.Tsp.FogCtrl : 2u) << 12)
                | (poly.Tsp.SrcInstr << 14) | (poly.Tsp.DstInstr << 17);
        }
        hash |= (poly.Isp.ZWriteDis << 20) | (poly.Isp.CullMode << 21) | ((autosort ? 6u : poly.Isp.DepthMode) << 23);
        hash |= ((ulong)gpuPalette << 26) | ((ulong)pass << 28) | ((poly.IsNaomi2() ? 1ul : 0ul) << 30);
        hash |= ((!PvrConfiguration.IsNaomi2 && PvrConfiguration.NativeDepthInterpolation) ? 1ul : 0ul) << 31;
        hash |= (poly.Tcw.PixelFmt == (uint)PixelFormat.PixelBumpMap ? 1ul : 0ul) << 32;
        hash |= (useBda ? 1ul : 0ul) << 34;
        return hash;
    }

    private static uint Hash(PvrVulkanModVolMode mode, int cullMode, bool naomi2, bool useBda) =>
        ((uint)mode << 2) | (uint)cullMode | ((naomi2 ? 1u : 0u) << 5)
        | ((!PvrConfiguration.IsNaomi2 && PvrConfiguration.NativeDepthInterpolation ? 1u : 0u) << 6) | ((useBda ? 1u : 0u) << 7);

    private static uint Hash(bool dithering, bool useBda) => (dithering ? 1u : 0u) | ((useBda ? 1u : 0u) << 1);

    private static readonly VertexInputBindingDescription[] VertexBindings = [new(0, (uint)sizeof(TaVertex), VertexInputRate.Vertex)];

    private static readonly VertexInputAttributeDescription[] VertexAttributes =
    [
        new(0, 0, Format.R32G32B32Sfloat, (uint)Marshal.OffsetOf<TaVertex>(nameof(TaVertex.X))),
        new(1, 0, Format.R8G8B8A8Unorm, (uint)Marshal.OffsetOf<TaVertex>(nameof(TaVertex.Col))),
        new(2, 0, Format.R8G8B8A8Unorm, (uint)Marshal.OffsetOf<TaVertex>(nameof(TaVertex.Spc))),
        new(3, 0, Format.R32G32Sfloat, (uint)Marshal.OffsetOf<TaVertex>(nameof(TaVertex.U))),
        new(4, 0, Format.R8G8B8A8Unorm, (uint)Marshal.OffsetOf<TaVertex>(nameof(TaVertex.Col1))),
        new(5, 0, Format.R8G8B8A8Unorm, (uint)Marshal.OffsetOf<TaVertex>(nameof(TaVertex.Spc1))),
        new(6, 0, Format.R32G32Sfloat, (uint)Marshal.OffsetOf<TaVertex>(nameof(TaVertex.U1))),
        new(7, 0, Format.R32G32B32Sfloat, (uint)Marshal.OffsetOf<TaVertex>(nameof(TaVertex.Nx)))
    ];

    private static (VertexInputBindingDescription[], ReadOnlyMemory<VertexInputAttributeDescription>)
        GetMainVertexInputStateCreateInfo(bool full = true, bool naomi2 = false) =>
        (VertexBindings, VertexAttributes.AsMemory(0, !full ? 1 : naomi2 ? VertexAttributes.Length : VertexAttributes.Length - 1));

    private static void ReleasePipelines<TKey>(SortedDictionary<TKey, PvrVulkanOwned<Pipeline>> pipelines) where TKey : notnull
    {
        foreach (PvrVulkanOwned<Pipeline> pipeline in pipelines.Values)
            pipeline.Dispose();
        pipelines.Clear();
    }

    private readonly SortedDictionary<ulong, PvrVulkanOwned<Pipeline>> _pipelines = [];
    private readonly SortedDictionary<uint, PvrVulkanOwned<Pipeline>> _modVolPipelines = [];
    private readonly SortedDictionary<uint, PvrVulkanOwned<Pipeline>> _trModVolPipelines = [];
    private readonly SortedDictionary<uint, PvrVulkanOwned<Pipeline>> _finalPipelines = [];
    private PvrVulkanOwned<Pipeline>? _clearPipeline;
    private PvrVulkanOwned<PipelineLayout>? _pipelineLayout;
    private PvrVulkanOwned<DescriptorSetLayout>? _perFrameLayout;
    private PvrVulkanOwned<DescriptorSetLayout>? _colorInputLayout;
    private PvrVulkanOwned<DescriptorSetLayout>? _perPolyLayout;
    private readonly PvrVulkanOitRenderPasses _ownRenderPasses = new();
    private int _maxLayers;

    protected static PvrVulkanContext GetContext() => PvrVulkanContext.Instance;
    protected PvrVulkanOitRenderPasses RenderPasses;
    protected PvrVulkanOitShaders? ShaderManager;
    protected PvrVulkanOitBuffers? OitBuffers;
}

internal sealed class PvrVulkanOitRttPipelineManager : PvrVulkanOitPipelineManager
{
    internal PvrVulkanOitRttPipelineManager() => RenderPasses = _rttRenderPasses;

    internal override void Init(PvrVulkanOitShaders shaderManager, PvrVulkanOitBuffers oitBuffers)
    {
        base.Init(shaderManager, oitBuffers);
        _renderToTextureBuffer = PvrConfiguration.RenderToTextureBuffer;
        _rttRenderPasses.Reset();
    }

    internal void CheckSettingsChange()
    {
        if (_renderToTextureBuffer != PvrConfiguration.RenderToTextureBuffer)
            Init(ShaderManager!, OitBuffers!);
    }

    public override void Dispose()
    {
        try
        {
            _rttRenderPasses.Dispose();
        }
        finally
        {
            base.Dispose();
        }
    }

    private bool _renderToTextureBuffer;
    private readonly PvrVulkanOitRttRenderPasses _rttRenderPasses = new();
}
