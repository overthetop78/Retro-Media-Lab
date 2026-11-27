using System;
using Silk.NET.Vulkan;
using static RetroMediaLab.Chips.Video.NEC.PowerVr2.PowerVr2Ta;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Vulkan;

internal unsafe partial class PvrVulkanPipelineManager
{
    private void CreateModVolPipeline(PvrVulkanModVolMode mode, int cullMode, bool naomi2)
    {
        static StencilOpState State(StencilOp fail, StencilOp pass, StencilOp depthFail,
            CompareOp compare, uint compareMask, uint writeMask, uint reference) => new()
        {
            FailOp = fail,
            PassOp = pass,
            DepthFailOp = depthFail,
            CompareOp = compare,
            CompareMask = compareMask,
            WriteMask = writeMask,
            Reference = reference
        };

        ReadOnlySpan<VertexInputBindingDescription> bindings;
        ReadOnlySpan<VertexInputAttributeDescription> attributes;
        PrimitiveTopology topology;
        VertexInputBindingDescription binding = new(0, sizeof(float) * 3, VertexInputRate.Vertex);
        VertexInputAttributeDescription attribute = new(0, 0, Format.R32G32B32Sfloat, 0);
        if (mode == PvrVulkanModVolMode.Final)
        {
            bindings = MainBindings;
            attributes = GetMainVertexInputStateCreateInfo(false, naomi2);
            topology = PrimitiveTopology.TriangleStrip;
        }
        else
        {
            bindings = new ReadOnlySpan<VertexInputBindingDescription>(&binding, 1);
            attributes = new ReadOnlySpan<VertexInputAttributeDescription>(&attribute, 1);
            topology = PrimitiveTopology.TriangleList;
        }

        StencilOpState stencilOpState = default;
        switch (mode)
        {
            case PvrVulkanModVolMode.Xor:
                stencilOpState = State(StencilOp.Keep, StencilOp.Invert, StencilOp.Keep, CompareOp.Always, 0, 2, 2);
                break;
            case PvrVulkanModVolMode.Or:
                stencilOpState = State(StencilOp.Keep, StencilOp.Replace, StencilOp.Keep, CompareOp.Always, 2, 2, 2);
                break;
            case PvrVulkanModVolMode.Inclusion:
                stencilOpState = State(StencilOp.Zero, StencilOp.Replace, StencilOp.Zero, CompareOp.LessOrEqual, 3, 3, 1);
                break;
            case PvrVulkanModVolMode.Exclusion:
                stencilOpState = State(StencilOp.Zero, StencilOp.Keep, StencilOp.Zero, CompareOp.Equal, 3, 3, 1);
                break;
            case PvrVulkanModVolMode.Final:
                stencilOpState = State(StencilOp.Zero, StencilOp.Zero, StencilOp.Zero, CompareOp.Equal, 0x81, 3, 0x81);
                break;
        }
        PipelineDepthStencilStateCreateInfo depthStencil = new()
        {
            DepthTestEnable = mode == PvrVulkanModVolMode.Xor || mode == PvrVulkanModVolMode.Or,
            DepthWriteEnable = false,
            DepthCompareOp = CompareOp.Greater,
            DepthBoundsTestEnable = false,
            StencilTestEnable = true,
            Front = stencilOpState,
            Back = stencilOpState
        };

        PipelineColorBlendAttachmentState blend = new()
        {
            BlendEnable = mode == PvrVulkanModVolMode.Final,
            SrcColorBlendFactor = BlendFactor.SrcAlpha,
            DstColorBlendFactor = BlendFactor.OneMinusSrcAlpha,
            ColorBlendOp = BlendOp.Add,
            SrcAlphaBlendFactor = BlendFactor.SrcAlpha,
            DstAlphaBlendFactor = BlendFactor.OneMinusSrcAlpha,
            AlphaBlendOp = BlendOp.Add,
            ColorWriteMask = mode != PvrVulkanModVolMode.Final ? 0
                : ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit
        };
        CullModeFlags culling = cullMode == 3 ? CullModeFlags.BackBit : cullMode == 2 ? CullModeFlags.FrontBit : 0;
        bool divPosZ = !PvrConfiguration.IsNaomi2 && PvrConfiguration.NativeDepthInterpolation;
        ShaderModule vertex = ShaderManager!.GetModVolVertexShader(new(naomi2, divPosZ));
        ShaderModule fragment = ShaderManager.GetModVolShader(divPosZ);
        _modVolPipelines[Hash(mode, cullMode, naomi2)] =
            CreateGraphicsPipeline(bindings, attributes, topology, false, culling, false, depthStencil, blend, vertex, fragment);
    }

    private void CreateDepthPassPipeline(int cullMode, bool naomi2)
    {
        PipelineDepthStencilStateCreateInfo depthStencil = new()
        {
            DepthTestEnable = true,
            DepthWriteEnable = true,
            DepthCompareOp = CompareOp.GreaterOrEqual,
            DepthBoundsTestEnable = false,
            StencilTestEnable = false,
            Front = default,
            Back = default
        };
        PipelineColorBlendAttachmentState blend = new()
        {
            BlendEnable = false,
            SrcColorBlendFactor = BlendFactor.Zero,
            DstColorBlendFactor = BlendFactor.Zero,
            ColorBlendOp = BlendOp.Add,
            SrcAlphaBlendFactor = BlendFactor.Zero,
            DstAlphaBlendFactor = BlendFactor.Zero,
            AlphaBlendOp = BlendOp.Add,
            ColorWriteMask = 0
        };
        CullModeFlags culling = cullMode == 3 ? CullModeFlags.BackBit : cullMode == 2 ? CullModeFlags.FrontBit : 0;
        bool divPosZ = !PvrConfiguration.IsNaomi2 && PvrConfiguration.NativeDepthInterpolation;
        ShaderModule vertex = ShaderManager!.GetModVolVertexShader(new(naomi2, divPosZ));
        ShaderModule fragment = ShaderManager.GetModVolShader(divPosZ);
        _depthPassPipelines[Hash(cullMode, naomi2)] = CreateGraphicsPipeline(MainBindings, GetMainVertexInputStateCreateInfo(false, false),
            PrimitiveTopology.TriangleList, false, culling, GetContext().HasProvokingVertex(), depthStencil, blend, vertex, fragment);
    }

    private void CreatePipeline(uint listType, bool sortTriangles, in PolyParam poly, int gpuPalette, bool dithering)
    {
        bool naomi2 = poly.IsNaomi2();
        ReadOnlySpan<VertexInputAttributeDescription> attributes = GetMainVertexInputStateCreateInfo(true, naomi2);

        PrimitiveTopology topology;
        bool restart = false;
        if (sortTriangles && !PvrConfiguration.PerStripSorting)
            topology = PrimitiveTopology.TriangleList;
        else
        {
            topology = PrimitiveTopology.TriangleStrip;
            restart = true;
        }

        CullModeFlags culling = poly.Isp.CullMode == 3
            ? CullModeFlags.BackBit : poly.Isp.CullMode == 2 ? CullModeFlags.FrontBit : 0;
        CompareOp depthOp;
        if (listType == ListType_Punch_Through || sortTriangles)
            depthOp = CompareOp.GreaterOrEqual;
        else
            depthOp = PvrVulkanUtilities.DepthOps[poly.Isp.DepthMode];

        bool depthWrite;
        if (sortTriangles)
            depthWrite = GetContext().GetVendorId() == PvrVulkanContext.VendorIntel;
        else
        {
            if (listType == ListType_Punch_Through)
                depthWrite = true;
            else
                depthWrite = poly.Isp.ZWriteDis == 0;
        }

        bool shadowed = listType == ListType_Opaque || listType == ListType_Punch_Through;
        StencilOpState stencilState;
        if (shadowed)
        {
            stencilState = new()
            {
                FailOp = StencilOp.Keep,
                PassOp = StencilOp.Replace,
                DepthFailOp = StencilOp.Keep,
                CompareOp = CompareOp.Always,
                CompareMask = 0,
                WriteMask = 0x80,
                Reference = poly.Pcw.Shadow != 0 ? 0x80u : 0u
            };
        }
        else
        {
            stencilState = new()
            {
                FailOp = StencilOp.Keep,
                PassOp = StencilOp.Keep,
                DepthFailOp = StencilOp.Keep,
                CompareOp = CompareOp.Never
            };
        }
        PipelineDepthStencilStateCreateInfo depthStencil = new()
        {
            DepthTestEnable = true,
            DepthWriteEnable = depthWrite,
            DepthCompareOp = depthOp,
            DepthBoundsTestEnable = false,
            StencilTestEnable = shadowed,
            Front = stencilState,
            Back = stencilState
        };

        uint src = poly.Tsp.SrcInstr;
        uint dst = poly.Tsp.DstInstr;
        PipelineColorBlendAttachmentState blend = new()
        {
            BlendEnable = true,
            SrcColorBlendFactor = PvrVulkanUtilities.GetBlendFactor(src, true),
            DstColorBlendFactor = PvrVulkanUtilities.GetBlendFactor(dst, false),
            ColorBlendOp = BlendOp.Add,
            SrcAlphaBlendFactor = PvrVulkanUtilities.GetBlendFactor(src, true),
            DstAlphaBlendFactor = PvrVulkanUtilities.GetBlendFactor(dst, false),
            AlphaBlendOp = BlendOp.Add,
            ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit
        };

        bool divPosZ = !PvrConfiguration.IsNaomi2 && PvrConfiguration.NativeDepthInterpolation;
        ShaderModule vertex = ShaderManager!.GetVertexShader(new(poly.Pcw.Gouraud == 1, naomi2, divPosZ, naomi2 && poly.Pcw.Texture == 1));
        PvrVulkanFragmentShaderParams parameters = new()
        {
            AlphaTest = listType == ListType_Punch_Through,
            Bumpmap = poly.Tcw.PixelFmt == (uint)PixelFormat.PixelBumpMap,
            Clamping = poly.Tsp.ColorClamp != 0,
            InsideClipTest = (poly.TileClip >> 28) == 3,
            Fog = PvrConfiguration.Fog ? (int)poly.Tsp.FogCtrl : 2,
            Gouraud = poly.Pcw.Gouraud != 0,
            IgnoreTexAlpha = poly.Tsp.IgnoreTexA != 0 || poly.Tcw.PixelFmt == (uint)PixelFormat.Pixel565,
            Offset = poly.Pcw.Offset != 0,
            ShaderInstr = (int)poly.Tsp.ShadInstr,
            Texture = poly.Pcw.Texture != 0,
            Trilinear = poly.Pcw.Texture != 0 && poly.Tsp.FilterMode > 1 && listType != ListType_Punch_Through && poly.Tcw.MipMapped == 1,
            UseAlpha = poly.Tsp.UseAlpha != 0,
            Palette = gpuPalette,
            DivPosZ = divPosZ,
            Dithering = dithering
        };
        ShaderModule fragment = ShaderManager.GetFragmentShader(parameters);
        _pipelines[Hash(listType, sortTriangles, in poly, gpuPalette, dithering)] =
            CreateGraphicsPipeline(MainBindings, attributes, topology, restart, culling, GetContext().HasProvokingVertex(),
                depthStencil, blend, vertex, fragment);
    }

    private PvrVulkanOwned<Pipeline> CreateGraphicsPipeline(
        ReadOnlySpan<VertexInputBindingDescription> bindings, ReadOnlySpan<VertexInputAttributeDescription> attributes,
        PrimitiveTopology topology, bool primitiveRestart, CullModeFlags cullMode, bool provokingVertex,
        PipelineDepthStencilStateCreateInfo depthStencil, PipelineColorBlendAttachmentState blendAttachment,
        ShaderModule vertex, ShaderModule fragment)
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
                RenderPass = _renderPass,
                Subpass = 0,
                BasePipelineIndex = -1
            };
            return GetContext().GetDevice().CreateGraphicsPipelineUnique(GetContext().GetPipelineCache(), pipeline);
        }
    }

}
