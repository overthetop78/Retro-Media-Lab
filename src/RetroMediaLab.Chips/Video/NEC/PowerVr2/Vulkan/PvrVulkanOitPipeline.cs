using Silk.NET.Vulkan;
using static RetroMediaLab.Chips.Video.NEC.PowerVr2.PowerVr2Ta;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Vulkan;

internal unsafe partial class PvrVulkanOitPipelineManager
{
    private const ColorComponentFlags AllComponents = ColorComponentFlags.RBit | ColorComponentFlags.GBit
        | ColorComponentFlags.BBit | ColorComponentFlags.ABit;

    private void CreatePipeline(uint listType, bool autosort, in PolyParam poly, PvrVulkanOitPass pass, int gpuPalette, bool useBda)
    {
        var (vertexBindings, vertexAttributes) = GetMainVertexInputStateCreateInfo(true, poly.IsNaomi2());
        CullModeFlags cullMode = poly.Isp.CullMode == 3 ? CullModeFlags.BackBit
            : poly.Isp.CullMode == 2 ? CullModeFlags.FrontBit : 0;
        CompareOp depthOp = listType == ListType_Punch_Through || autosort
            ? CompareOp.GreaterOrEqual : PvrVulkanUtilities.DepthOps[poly.Isp.DepthMode];
        bool depthWrite = listType == ListType_Punch_Through || poly.Isp.ZWriteDis == 0;

        bool shadowed = pass == PvrVulkanOitPass.Depth && (listType == ListType_Opaque || listType == ListType_Punch_Through);
        StencilOpState stencil = shadowed
            ? new(StencilOp.Keep, StencilOp.Replace, StencilOp.Keep, CompareOp.Always, 0, 0x80, poly.Pcw.Shadow != 0 ? 0x80u : 0)
            : new(StencilOp.Keep, StencilOp.Keep, StencilOp.Keep, CompareOp.Never);
        PipelineDepthStencilStateCreateInfo depthStencil = new()
        {
            SType = StructureType.PipelineDepthStencilStateCreateInfo,
            DepthTestEnable = true,
            DepthWriteEnable = depthWrite,
            DepthCompareOp = depthOp,
            StencilTestEnable = shadowed,
            Front = stencil,
            Back = stencil
        };

        PipelineColorBlendAttachmentState blend;
        if (listType == ListType_Punch_Through || pass == PvrVulkanOitPass.Color)
        {
            BlendFactor src = PvrVulkanUtilities.GetBlendFactor(poly.Tsp.SrcInstr, true);
            BlendFactor dst = PvrVulkanUtilities.GetBlendFactor(poly.Tsp.DstInstr, false);
            blend = new(true, src, dst, BlendOp.Add, src, dst, BlendOp.Add, AllComponents);
        }
        else
        {
            ColorComponentFlags writeMask = pass == PvrVulkanOitPass.Depth || pass == PvrVulkanOitPass.Oit ? 0 : AllComponents;
            blend = new(false, BlendFactor.Zero, BlendFactor.Zero, BlendOp.Add, BlendFactor.Zero, BlendFactor.Zero, BlendOp.Add, writeMask);
        }

        bool twoVolume = poly.Tsp1.full != uint.MaxValue || poly.Tcw1.full != uint.MaxValue;
        bool divPosZ = !PvrConfiguration.IsNaomi2 && PvrConfiguration.NativeDepthInterpolation;
        ShaderModule vertex = ShaderManager!.GetVertexShader(new(poly.Pcw.Gouraud == 1, poly.IsNaomi2(),
            pass != PvrVulkanOitPass.Depth, twoVolume, poly.IsNaomi2() && poly.Pcw.Texture == 1, divPosZ));
        ShaderModule fragment = ShaderManager.GetFragmentShader(new(listType == ListType_Punch_Through,
            (poly.TileClip >> 28) == 3, poly.Tsp.UseAlpha != 0, poly.Pcw.Texture != 0,
            poly.Tsp.IgnoreTexA != 0 || poly.Tcw.PixelFmt == (uint)PixelFormat.Pixel565, (int)poly.Tsp.ShadInstr,
            poly.Pcw.Offset != 0, PvrConfiguration.Fog ? (int)poly.Tsp.FogCtrl : 2, poly.Pcw.Gouraud != 0,
            poly.Tcw.PixelFmt == (uint)PixelFormat.PixelBumpMap, poly.Tsp.ColorClamp != 0, twoVolume,
            gpuPalette, divPosZ, useBda, pass));
        uint subpass = pass == PvrVulkanOitPass.Depth ? (listType == ListType_Translucent ? 2u : 0u) : 1;
        _pipelines[Hash(listType, autosort, poly, pass, gpuPalette, useBda)] = CreateGraphicsPipeline(vertexBindings,
            vertexAttributes.Span, PrimitiveTopology.TriangleStrip, true, cullMode, GetContext().HasProvokingVertex(),
            depthStencil, blend, vertex, fragment, subpass);
    }

    private void CreateFinalPipeline(bool dithering, bool useBda)
    {
        var (bindings, attributes) = PvrVulkanQuadBuffer.GetQuadInputStateCreateInfo(false);
        PipelineColorBlendAttachmentState blend = new(false, BlendFactor.Zero, BlendFactor.Zero,
            BlendOp.Add, BlendFactor.Zero, BlendFactor.Zero, BlendOp.Add, AllComponents);
        ShaderModule vertex = ShaderManager!.GetFinalVertexShader();
        ShaderModule fragment = ShaderManager.GetFinalShader(new(dithering, useBda));
        _finalPipelines[Hash(dithering, useBda)] = CreateGraphicsPipeline(bindings, attributes.Span,
            PrimitiveTopology.TriangleStrip, false, 0, false, new(), blend, vertex, fragment, 2);
    }

    private void CreateClearPipeline()
    {
        var (bindings, attributes) = PvrVulkanQuadBuffer.GetQuadInputStateCreateInfo(false);
        ShaderModule vertex = ShaderManager!.GetFinalVertexShader();
        ShaderModule fragment = ShaderManager.GetClearShader();
        _clearPipeline = CreateGraphicsPipeline(bindings, attributes.Span, PrimitiveTopology.TriangleStrip,
            false, 0, false, new(), new(), vertex, fragment, 1);
    }

    private void CreateModVolPipeline(PvrVulkanModVolMode mode, int cullMode, bool naomi2)
    {
        PvrRuntime.Verify(mode != PvrVulkanModVolMode.Final);
        VertexInputBindingDescription binding = new(0, sizeof(float) * 3, VertexInputRate.Vertex);
        VertexInputAttributeDescription attribute = new(0, 0, Format.R32G32B32Sfloat, 0);
        CullModeFlags culling = cullMode == 3 ? CullModeFlags.BackBit : cullMode == 2 ? CullModeFlags.FrontBit : 0;
        StencilOpState stencil = mode switch
        {
            PvrVulkanModVolMode.Xor => new(StencilOp.Keep, StencilOp.Invert, StencilOp.Keep, CompareOp.Always, 0, 2, 2),
            PvrVulkanModVolMode.Or => new(StencilOp.Keep, StencilOp.Replace, StencilOp.Keep, CompareOp.Always, 2, 2, 2),
            PvrVulkanModVolMode.Inclusion => new(StencilOp.Zero, StencilOp.Replace, StencilOp.Zero, CompareOp.LessOrEqual, 3, 3, 1),
            PvrVulkanModVolMode.Exclusion => new(StencilOp.Zero, StencilOp.Keep, StencilOp.Zero, CompareOp.Equal, 3, 3, 1),
            _ => new()
        };
        PipelineDepthStencilStateCreateInfo depthStencil = new()
        {
            SType = StructureType.PipelineDepthStencilStateCreateInfo,
            DepthTestEnable = mode is PvrVulkanModVolMode.Xor or PvrVulkanModVolMode.Or,
            DepthCompareOp = CompareOp.Greater,
            StencilTestEnable = true,
            Front = stencil,
            Back = stencil
        };
        bool divPosZ = !PvrConfiguration.IsNaomi2 && PvrConfiguration.NativeDepthInterpolation;
        ShaderModule vertex = ShaderManager!.GetModVolVertexShader(new(naomi2, divPosZ));
        ShaderModule fragment = ShaderManager.GetModVolShader(divPosZ);
        _modVolPipelines[Hash(mode, cullMode, naomi2, false)] = CreateGraphicsPipeline([binding], [attribute],
            PrimitiveTopology.TriangleList, false, culling, false, depthStencil, new(), vertex, fragment, 0);
    }

    private void CreateTrModVolPipeline(PvrVulkanModVolMode mode, int cullMode, bool naomi2, bool useBda)
    {
        PvrRuntime.Verify(mode != PvrVulkanModVolMode.Final);
        VertexInputBindingDescription binding = new(0, sizeof(float) * 3, VertexInputRate.Vertex);
        VertexInputAttributeDescription attribute = new(0, 0, Format.R32G32B32Sfloat, 0);
        CullModeFlags culling = cullMode == 3 ? CullModeFlags.BackBit : cullMode == 2 ? CullModeFlags.FrontBit : 0;
        bool divPosZ = !PvrConfiguration.IsNaomi2 && PvrConfiguration.NativeDepthInterpolation;
        ShaderModule vertex = ShaderManager!.GetModVolVertexShader(new(naomi2, divPosZ));
        ShaderModule fragment = ShaderManager.GetTrModVolShader(new(mode, divPosZ, useBda));
        _trModVolPipelines[Hash(mode, cullMode, naomi2, useBda)] = CreateGraphicsPipeline([binding], [attribute],
            PrimitiveTopology.TriangleList, false, culling, false, new(), new(), vertex, fragment, 2);
    }

    private PvrVulkanOwned<Pipeline> CreateGraphicsPipeline(
        ReadOnlySpan<VertexInputBindingDescription> bindings, ReadOnlySpan<VertexInputAttributeDescription> attributes,
        PrimitiveTopology topology, bool primitiveRestart, CullModeFlags cullMode, bool provokingVertex,
        PipelineDepthStencilStateCreateInfo depthStencil, PipelineColorBlendAttachmentState blendAttachment,
        ShaderModule vertex, ShaderModule fragment, uint subpass)
    {
        fixed (VertexInputBindingDescription* bindingData = bindings)
        fixed (VertexInputAttributeDescription* attributeData = attributes)
        fixed (byte* main = "main\0"u8)
        {
            PipelineVertexInputStateCreateInfo vertexInput = new()
            {
                SType = StructureType.PipelineVertexInputStateCreateInfo,
                VertexBindingDescriptionCount = (uint)bindings.Length,
                PVertexBindingDescriptions = bindingData,
                VertexAttributeDescriptionCount = (uint)attributes.Length,
                PVertexAttributeDescriptions = attributeData
            };
            PipelineInputAssemblyStateCreateInfo inputAssembly = new()
            {
                SType = StructureType.PipelineInputAssemblyStateCreateInfo,
                Topology = topology,
                PrimitiveRestartEnable = primitiveRestart
            };
            PipelineViewportStateCreateInfo viewport = new()
            {
                SType = StructureType.PipelineViewportStateCreateInfo,
                ViewportCount = 1,
                ScissorCount = 1
            };
            PipelineRasterizationStateCreateInfo raster = new()
            {
                SType = StructureType.PipelineRasterizationStateCreateInfo,
                PolygonMode = PolygonMode.Fill,
                CullMode = cullMode,
                FrontFace = FrontFace.CounterClockwise,
                LineWidth = 1f
            };
            PipelineRasterizationProvokingVertexStateCreateInfoEXT provoking = new()
            {
                SType = StructureType.PipelineRasterizationProvokingVertexStateCreateInfoExt,
                ProvokingVertexMode = ProvokingVertexModeEXT.LastVertexExt
            };
            if (provokingVertex)
                raster.PNext = &provoking;
            PipelineMultisampleStateCreateInfo multisample = new()
            {
                SType = StructureType.PipelineMultisampleStateCreateInfo,
                RasterizationSamples = SampleCountFlags.Count1Bit
            };
            depthStencil.SType = StructureType.PipelineDepthStencilStateCreateInfo;
            PipelineColorBlendStateCreateInfo blendState = new()
            {
                SType = StructureType.PipelineColorBlendStateCreateInfo,
                LogicOp = LogicOp.NoOp,
                AttachmentCount = 1,
                PAttachments = &blendAttachment
            };
            for (int i = 0; i < 4; i++)
                blendState.BlendConstants[i] = 1f;
            DynamicState* dynamicStates = stackalloc DynamicState[2] { DynamicState.Viewport, DynamicState.Scissor };
            PipelineDynamicStateCreateInfo dynamic = new()
            {
                SType = StructureType.PipelineDynamicStateCreateInfo,
                DynamicStateCount = 2,
                PDynamicStates = dynamicStates
            };
            PipelineShaderStageCreateInfo* stages = stackalloc PipelineShaderStageCreateInfo[2]
            {
                new() { SType = StructureType.PipelineShaderStageCreateInfo, Stage = ShaderStageFlags.VertexBit, Module = vertex, PName = main },
                new() { SType = StructureType.PipelineShaderStageCreateInfo, Stage = ShaderStageFlags.FragmentBit, Module = fragment, PName = main }
            };
            GraphicsPipelineCreateInfo pipeline = new()
            {
                SType = StructureType.GraphicsPipelineCreateInfo,
                StageCount = 2,
                PStages = stages,
                PVertexInputState = &vertexInput,
                PInputAssemblyState = &inputAssembly,
                PViewportState = &viewport,
                PRasterizationState = &raster,
                PMultisampleState = &multisample,
                PDepthStencilState = &depthStencil,
                PColorBlendState = &blendState,
                PDynamicState = &dynamic,
                Layout = _pipelineLayout!.Handle,
                RenderPass = RenderPasses.GetRenderPass(true, true),
                Subpass = subpass,
                BasePipelineIndex = -1
            };
            return GetContext().GetDevice().CreateGraphicsPipelineUnique(GetContext().GetPipelineCache(), pipeline);
        }
    }

    private void CheckMaxLayers()
    {
        int layers = Math.Clamp(PvrConfiguration.PerPixelLayers, 1, 256);
        if (_maxLayers != layers)
        {
            _maxLayers = layers;
            ReleasePipelines(_trModVolPipelines);
            ReleasePipelines(_finalPipelines);
        }
    }
}
