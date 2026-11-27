using RenderPass = Silk.NET.Vulkan.RenderPass;
using Silk.NET.Vulkan;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Vulkan;

internal sealed unsafe partial class PvrVulkanQuadBuffer
{
    private static nuint HashQuadVertex(in PvrVulkanQuadVertex vertex)
    {
        nuint seed = 0;
        seed ^= (nuint)(uint)vertex.X.GetHashCode() + 0x9e3779b9u + (seed << 6) + (seed >> 2);
        seed ^= (nuint)(uint)vertex.Y.GetHashCode() + 0x9e3779b9u + (seed << 6) + (seed >> 2);
        seed ^= (nuint)(uint)vertex.Z.GetHashCode() + 0x9e3779b9u + (seed << 6) + (seed >> 2);
        seed ^= (nuint)(uint)vertex.U.GetHashCode() + 0x9e3779b9u + (seed << 6) + (seed >> 2);
        seed ^= (nuint)(uint)vertex.V.GetHashCode() + 0x9e3779b9u + (seed << 6) + (seed >> 2);
        return seed;
    }

    private static readonly VertexInputBindingDescription[] VertexBindings =
        [new(0, (uint)sizeof(PvrVulkanQuadVertex), VertexInputRate.Vertex)];
    private static readonly VertexInputAttributeDescription[] VertexAttributes =
        [new(0, 0, Format.R32G32B32Sfloat, 0), new(1, 0, Format.R32G32Sfloat, 3 * sizeof(float))];

    internal static (VertexInputBindingDescription[] Bindings, ReadOnlyMemory<VertexInputAttributeDescription> Attributes)
        GetQuadInputStateCreateInfo(bool uv) => (VertexBindings, VertexAttributes.AsMemory(0, uv ? 2 : 1));

    private static nuint HashQuadVertexData(PvrVulkanQuadVertex* vertices)
    {
        nuint seed = 0;
        seed ^= HashQuadVertex(vertices[0]) + 0x9e3779b9u + (seed << 6) + (seed >> 2);
        seed ^= HashQuadVertex(vertices[1]) + 0x9e3779b9u + (seed << 6) + (seed >> 2);
        seed ^= HashQuadVertex(vertices[2]) + 0x9e3779b9u + (seed << 6) + (seed >> 2);
        seed ^= HashQuadVertex(vertices[3]) + 0x9e3779b9u + (seed << 6) + (seed >> 2);
        return seed;
    }
}

internal sealed unsafe partial class PvrVulkanQuadPipeline
{
    private static PvrVulkanContext GetContext() => PvrVulkanContext.Instance;

    private void CreatePipeline()
    {
        var (bindings, attributes) = PvrVulkanQuadBuffer.GetQuadInputStateCreateInfo(true);
        fixed (VertexInputBindingDescription* bindingsPtr = bindings)
        fixed (VertexInputAttributeDescription* attributesPtr = attributes.Span)
        {
            var vertexInput = new PipelineVertexInputStateCreateInfo
            {
                SType = StructureType.PipelineVertexInputStateCreateInfo,
                VertexBindingDescriptionCount = (uint)bindings.Length, PVertexBindingDescriptions = bindingsPtr,
                VertexAttributeDescriptionCount = (uint)attributes.Length, PVertexAttributeDescriptions = attributesPtr
            };
            var assembly = new PipelineInputAssemblyStateCreateInfo
            {
                SType = StructureType.PipelineInputAssemblyStateCreateInfo,
                Topology = PrimitiveTopology.TriangleStrip
            };
            var viewport = new PipelineViewportStateCreateInfo
            {
                SType = StructureType.PipelineViewportStateCreateInfo, ViewportCount = 1, ScissorCount = 1
            };
            var raster = new PipelineRasterizationStateCreateInfo
            {
                SType = StructureType.PipelineRasterizationStateCreateInfo, LineWidth = 1f
            };
            var multisample = new PipelineMultisampleStateCreateInfo
            {
                SType = StructureType.PipelineMultisampleStateCreateInfo, RasterizationSamples = SampleCountFlags.Count1Bit
            };
            var depth = new PipelineDepthStencilStateCreateInfo { SType = StructureType.PipelineDepthStencilStateCreateInfo };
            var blendAttachment = new PipelineColorBlendAttachmentState
            {
                BlendEnable = true, SrcColorBlendFactor = BlendFactor.SrcAlpha, DstColorBlendFactor = BlendFactor.OneMinusSrcAlpha,
                ColorBlendOp = BlendOp.Add, SrcAlphaBlendFactor = BlendFactor.One, DstAlphaBlendFactor = BlendFactor.OneMinusSrcAlpha,
                AlphaBlendOp = BlendOp.Add, ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit
                    | ColorComponentFlags.BBit | ColorComponentFlags.ABit
            };
            var blend = new PipelineColorBlendStateCreateInfo
            {
                SType = StructureType.PipelineColorBlendStateCreateInfo,
                LogicOp = LogicOp.NoOp, AttachmentCount = 1, PAttachments = &blendAttachment
            };
            for (int i = 0; i < 4; i++)
                blend.BlendConstants[i] = 1f;
            DynamicState* states = stackalloc DynamicState[2] { DynamicState.Viewport, DynamicState.Scissor };
            var dynamic = new PipelineDynamicStateCreateInfo
            {
                SType = StructureType.PipelineDynamicStateCreateInfo, DynamicStateCount = 2, PDynamicStates = states
            };
            byte* entryPoint = stackalloc byte[5] { (byte)'m', (byte)'a', (byte)'i', (byte)'n', 0 };
            PipelineShaderStageCreateInfo* stages = stackalloc PipelineShaderStageCreateInfo[2]
            {
                new() { SType = StructureType.PipelineShaderStageCreateInfo, Stage = ShaderStageFlags.VertexBit,
                    Module = _shaderManager!.GetQuadVertexShader(_rotate), PName = entryPoint },
                new() { SType = StructureType.PipelineShaderStageCreateInfo, Stage = ShaderStageFlags.FragmentBit,
                    Module = _shaderManager!.GetQuadFragmentShader(_ignoreTexAlpha), PName = entryPoint }
            };
            var graphics = new GraphicsPipelineCreateInfo
            {
                SType = StructureType.GraphicsPipelineCreateInfo, StageCount = 2, PStages = stages,
                PVertexInputState = &vertexInput, PInputAssemblyState = &assembly, PViewportState = &viewport,
                PRasterizationState = &raster, PMultisampleState = &multisample, PDepthStencilState = &depth,
                PColorBlendState = &blend, PDynamicState = &dynamic, Layout = _pipelineLayout!.Handle,
                RenderPass = _renderPass, Subpass = (uint)_subpass
            };
            _pipeline = GetContext().GetDevice().CreateGraphicsPipelineUnique(GetContext().GetPipelineCache(), graphics);
        }
    }

    internal void Init(PvrVulkanShaders shaderManager, RenderPass renderPass, int subpass)
    {
        _shaderManager = shaderManager;
        var device = GetContext().GetDevice();
        if (_pipelineLayout == null)
        {
            var binding = new DescriptorSetLayoutBinding(0, DescriptorType.CombinedImageSampler, 1, ShaderStageFlags.FragmentBit);
            _descSetLayout = device.CreateDescriptorSetLayoutUnique([binding]);
            var pushConstant = new PushConstantRange(ShaderStageFlags.FragmentBit, 0, 4 * sizeof(float));
            _pipelineLayout = device.CreatePipelineLayoutUnique([_descSetLayout.Handle], [pushConstant]);
        }
        _linearSampler ??= device.CreateSamplerUnique(new SamplerCreateInfo
        {
            SType = StructureType.SamplerCreateInfo, MagFilter = Filter.Linear, MinFilter = Filter.Linear,
            MipmapMode = SamplerMipmapMode.Linear, AddressModeU = SamplerAddressMode.ClampToEdge,
            AddressModeV = SamplerAddressMode.ClampToEdge, AddressModeW = SamplerAddressMode.ClampToEdge,
            MaxAnisotropy = 16f, CompareOp = CompareOp.Never, MaxLod = 1000f, BorderColor = BorderColor.FloatOpaqueBlack
        });
        _nearestSampler ??= device.CreateSamplerUnique(new SamplerCreateInfo
        {
            SType = StructureType.SamplerCreateInfo, MagFilter = Filter.Nearest, MinFilter = Filter.Nearest,
            MipmapMode = SamplerMipmapMode.Nearest, AddressModeU = SamplerAddressMode.ClampToEdge,
            AddressModeV = SamplerAddressMode.ClampToEdge, AddressModeW = SamplerAddressMode.ClampToEdge,
            MaxAnisotropy = 16f, CompareOp = CompareOp.Never, MaxLod = 1000f, BorderColor = BorderColor.FloatOpaqueBlack
        });
        if (_renderPass.Handle != renderPass.Handle)
        {
            _renderPass = renderPass;
            _subpass = subpass;
            _pipeline?.Dispose();
            _pipeline = null;
        }
    }
}

internal sealed unsafe partial class PvrVulkanQuadDrawer
{
    internal void Init(PvrVulkanQuadPipeline pipeline)
    {
        _pipeline = pipeline;
        _buffer?.Dispose();
        _buffer = new PvrVulkanQuadBuffer();
        int count = (int)PvrVulkanContext.Instance.GetSwapChainSize();
        foreach (var set in _descriptorSets)
            set?.Dispose();
        _descriptorSets.Clear();
        for (int i = 0; i < count; i++)
            _descriptorSets.Add(null);
    }

    internal void Draw(CommandBuffer commandBuffer, ImageView imageView, PvrVulkanQuadVertex* vertices = null,
        bool nearestFilter = false, float* color = null)
    {
        PvrVulkanContext context = PvrVulkanContext.Instance;
        int index = (int)context.GetCurrentImageIndex();
        while (index >= _descriptorSets.Count)
            _descriptorSets.Add(null);
        var set = _descriptorSets[index] ??= context.GetDevice()
            .AllocateDescriptorSetsUnique(context.GetDescriptorPool(), [_pipeline!.GetDescSetLayout()])[0];
        if (imageView.Handle != 0)
        {
            var info = new DescriptorImageInfo(nearestFilter ? _pipeline!.GetNearestSampler() : _pipeline!.GetLinearSampler(),
                imageView, ImageLayout.ShaderReadOnlyOptimal);
            context.GetDevice().UpdateDescriptorSets(
                [new PvrVulkanDescriptorWrite(set.Handle, 0, 0, DescriptorType.CombinedImageSampler, info, default)]);
        }
        PvrVk.BindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, _pipeline!.GetPipelineLayout(), 0, [set.Handle], []);
        _buffer!.Update(vertices);
        _buffer.Bind(commandBuffer);

        float* fullWhite = stackalloc float[4] { 1f, 1f, 1f, 1f };
        if (color == null)
            color = fullWhite;
        context.Api.CmdPushConstants(commandBuffer, _pipeline.GetPipelineLayout(), ShaderStageFlags.FragmentBit, 0, sizeof(float) * 4, color);
        _buffer.Draw(commandBuffer);
    }
}
