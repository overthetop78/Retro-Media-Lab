using Silk.NET.Vulkan;
using VkRenderPass = Silk.NET.Vulkan.RenderPass;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Vulkan;

internal unsafe partial class PvrVulkanOitRenderPasses
{
    protected PvrVulkanOwned<VkRenderPass> MakeRenderPass(bool initial, bool last, bool loadClear)
    {
        AttachmentDescription attachment0 = GetAttachment0Description(initial, last, loadClear);
        AttachmentDescription[] attachments =
        [
            attachment0,
            new(0, Format.R8G8B8A8Unorm, SampleCountFlags.Count1Bit,
                loadClear && initial ? AttachmentLoadOp.Clear : AttachmentLoadOp.Load,
                last ? AttachmentStoreOp.DontCare : AttachmentStoreOp.Store,
                AttachmentLoadOp.DontCare, AttachmentStoreOp.DontCare,
                loadClear && initial ? ImageLayout.Undefined : attachment0.FinalLayout, attachment0.FinalLayout),
            new(0, GetContext().GetDepthFormat(), SampleCountFlags.Count1Bit,
                initial ? AttachmentLoadOp.Clear : AttachmentLoadOp.Load,
                last ? AttachmentStoreOp.DontCare : AttachmentStoreOp.Store,
                AttachmentLoadOp.Clear, AttachmentStoreOp.DontCare,
                initial ? ImageLayout.Undefined : ImageLayout.DepthStencilAttachmentOptimal, ImageLayout.DepthStencilAttachmentOptimal),
            new(0, GetContext().GetDepthFormat(), SampleCountFlags.Count1Bit,
                initial ? AttachmentLoadOp.Clear : AttachmentLoadOp.Load,
                last ? AttachmentStoreOp.DontCare : AttachmentStoreOp.Store,
                AttachmentLoadOp.Clear, AttachmentStoreOp.DontCare,
                initial ? ImageLayout.Undefined : ImageLayout.DepthStencilAttachmentOptimal, ImageLayout.DepthStencilAttachmentOptimal)
        ];
        AttachmentReference swapChainReference = new(0, ImageLayout.ColorAttachmentOptimal);
        AttachmentReference colorReference = new(1, ImageLayout.ColorAttachmentOptimal);
        AttachmentReference depthReference = new(2, ImageLayout.DepthStencilAttachmentOptimal);
        AttachmentReference depthReadOnlyRef = new(2, ImageLayout.DepthStencilReadOnlyOptimal);
        AttachmentReference depthReference2 = new(3, ImageLayout.DepthStencilAttachmentOptimal);
        AttachmentReference colorInput = new(1, ImageLayout.ShaderReadOnlyOptimal);

        SubpassDescription[] subpasses =
        [
            new() { PipelineBindPoint = PipelineBindPoint.Graphics, ColorAttachmentCount = 1, PColorAttachments = &colorReference, PDepthStencilAttachment = &depthReference },
            new() { PipelineBindPoint = PipelineBindPoint.Graphics, InputAttachmentCount = 1, PInputAttachments = &depthReadOnlyRef,
                ColorAttachmentCount = 1, PColorAttachments = &colorReference, PDepthStencilAttachment = &depthReference2 },
            new() { PipelineBindPoint = PipelineBindPoint.Graphics, InputAttachmentCount = 1, PInputAttachments = &colorInput,
                ColorAttachmentCount = 1, PColorAttachments = &swapChainReference, PDepthStencilAttachment = &depthReference }
        ];

        List<SubpassDependency> dependencies = GetSubpassDependencies();
        dependencies.Add(new(uint.MaxValue, 1, PipelineStageFlags.FragmentShaderBit, PipelineStageFlags.ColorAttachmentOutputBit,
            AccessFlags.InputAttachmentReadBit, AccessFlags.ColorAttachmentWriteBit, DependencyFlags.ByRegionBit));
        const PipelineStageFlags oitStages = PipelineStageFlags.EarlyFragmentTestsBit | PipelineStageFlags.LateFragmentTestsBit
            | PipelineStageFlags.FragmentShaderBit | PipelineStageFlags.ColorAttachmentOutputBit;
        for (uint subpass = 0; subpass <= 1; subpass++)
            dependencies.Add(new(uint.MaxValue, subpass, oitStages, oitStages,
                AccessFlags.ColorAttachmentWriteBit | AccessFlags.DepthStencilAttachmentWriteBit | AccessFlags.ShaderWriteBit,
                AccessFlags.ColorAttachmentReadBit | AccessFlags.ColorAttachmentWriteBit | AccessFlags.DepthStencilAttachmentReadBit
                | AccessFlags.DepthStencilAttachmentWriteBit | AccessFlags.InputAttachmentReadBit | AccessFlags.ShaderReadBit | AccessFlags.ShaderWriteBit));
        dependencies.Add(new(uint.MaxValue, 2, PipelineStageFlags.FragmentShaderBit | PipelineStageFlags.ColorAttachmentOutputBit,
            PipelineStageFlags.FragmentShaderBit | PipelineStageFlags.ColorAttachmentOutputBit,
            AccessFlags.ColorAttachmentWriteBit, AccessFlags.ColorAttachmentReadBit | AccessFlags.ColorAttachmentWriteBit));
        dependencies.Add(new(0, 1, PipelineStageFlags.LateFragmentTestsBit, PipelineStageFlags.FragmentShaderBit,
            AccessFlags.DepthStencilAttachmentReadBit | AccessFlags.DepthStencilAttachmentWriteBit,
            AccessFlags.InputAttachmentReadBit | AccessFlags.ShaderReadBit, DependencyFlags.ByRegionBit));
        dependencies.Add(new(0, 1, PipelineStageFlags.ColorAttachmentOutputBit, PipelineStageFlags.ColorAttachmentOutputBit,
            AccessFlags.ColorAttachmentWriteBit, AccessFlags.ColorAttachmentReadBit | AccessFlags.ColorAttachmentWriteBit, DependencyFlags.ByRegionBit));
        dependencies.Add(new(1, 2, PipelineStageFlags.ColorAttachmentOutputBit,
            PipelineStageFlags.FragmentShaderBit | PipelineStageFlags.ColorAttachmentOutputBit,
            AccessFlags.ColorAttachmentWriteBit, AccessFlags.InputAttachmentReadBit | AccessFlags.ColorAttachmentWriteBit, DependencyFlags.ByRegionBit));
        dependencies.Add(new(1, 2, PipelineStageFlags.FragmentShaderBit,
            PipelineStageFlags.EarlyFragmentTestsBit | PipelineStageFlags.LateFragmentTestsBit,
            AccessFlags.InputAttachmentReadBit | AccessFlags.ShaderReadBit,
            AccessFlags.DepthStencilAttachmentReadBit | AccessFlags.DepthStencilAttachmentWriteBit, DependencyFlags.ByRegionBit));
        dependencies.Add(new(1, 1, PipelineStageFlags.FragmentShaderBit, PipelineStageFlags.FragmentShaderBit,
            AccessFlags.ShaderReadBit | AccessFlags.ShaderWriteBit,
            AccessFlags.ShaderReadBit | AccessFlags.ShaderWriteBit, DependencyFlags.ByRegionBit));
        dependencies.Add(new(2, 2, PipelineStageFlags.FragmentShaderBit, PipelineStageFlags.FragmentShaderBit,
            AccessFlags.ShaderReadBit | AccessFlags.ShaderWriteBit,
            AccessFlags.ShaderReadBit | AccessFlags.ShaderWriteBit, DependencyFlags.ByRegionBit));
        if (GetContext().GetVendorId() == PvrVulkanContext.VendorArm)
            dependencies.Add(new(2, uint.MaxValue, PipelineStageFlags.ColorAttachmentOutputBit, PipelineStageFlags.TopOfPipeBit,
                AccessFlags.ColorAttachmentWriteBit, 0));

        return CreateRenderPass(GetContext().GetDevice(), attachments, subpasses, dependencies.ToArray());
    }
    private static PvrVulkanOwned<VkRenderPass> CreateRenderPass(PvrVulkanDevice device,
        ReadOnlySpan<AttachmentDescription> attachments, ReadOnlySpan<SubpassDescription> subpasses,
        ReadOnlySpan<SubpassDependency> dependencies)
    {
        fixed (AttachmentDescription* attachmentData = attachments)
        fixed (SubpassDescription* subpassData = subpasses)
        fixed (SubpassDependency* dependencyData = dependencies)
        {
            RenderPassCreateInfo info = new()
            {
                SType = StructureType.RenderPassCreateInfo,
                AttachmentCount = (uint)attachments.Length,
                PAttachments = attachmentData,
                SubpassCount = (uint)subpasses.Length,
                PSubpasses = subpassData,
                DependencyCount = (uint)dependencies.Length,
                PDependencies = dependencyData
            };
            return device.CreateRenderPassUnique(info);
        }
    }
}
