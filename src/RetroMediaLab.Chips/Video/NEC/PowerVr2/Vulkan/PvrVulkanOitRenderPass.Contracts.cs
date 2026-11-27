using Silk.NET.Vulkan;
using VkRenderPass = Silk.NET.Vulkan.RenderPass;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Vulkan;

internal partial class PvrVulkanOitRenderPasses : IDisposable
{
    internal VkRenderPass GetRenderPass(bool initial, bool last, bool loadClear = false)
    {
        int index = (initial ? 1 : 0) | (last ? 2 : 0) | (loadClear ? 4 : 0);
        _renderPasses[index] ??= MakeRenderPass(initial, last, loadClear);
        return _renderPasses[index]!.Handle;
    }

    internal void Reset()
    {
        for (int index = 0; index < _renderPasses.Length; index++)
        {
            _renderPasses[index]?.Dispose();
            _renderPasses[index] = null;
        }
    }

    public void Dispose() => Reset();

    protected static PvrVulkanContext GetContext() => PvrVulkanContext.Instance;

    protected virtual AttachmentDescription GetAttachment0Description(bool initial, bool last, bool loadClear) =>
        new(0, Format.R8G8B8A8Unorm, SampleCountFlags.Count1Bit,
            initial && loadClear ? AttachmentLoadOp.Clear : AttachmentLoadOp.Load, AttachmentStoreOp.Store,
            AttachmentLoadOp.DontCare, AttachmentStoreOp.DontCare,
            PvrConfiguration.EmulateFramebuffer && initial ? ImageLayout.TransferSrcOptimal : ImageLayout.ShaderReadOnlyOptimal,
            PvrConfiguration.EmulateFramebuffer && last ? ImageLayout.TransferSrcOptimal : ImageLayout.ShaderReadOnlyOptimal);

    protected virtual List<SubpassDependency> GetSubpassDependencies()
    {
        if (PvrConfiguration.EmulateFramebuffer)
            return [new(2, uint.MaxValue, PipelineStageFlags.ColorAttachmentOutputBit,
                PipelineStageFlags.TransferBit | PipelineStageFlags.HostBit, AccessFlags.ColorAttachmentWriteBit,
                AccessFlags.TransferReadBit | AccessFlags.HostReadBit, DependencyFlags.ByRegionBit)];
        else
            return [new(2, uint.MaxValue, PipelineStageFlags.ColorAttachmentOutputBit, PipelineStageFlags.FragmentShaderBit,
                AccessFlags.ColorAttachmentWriteBit, AccessFlags.ShaderReadBit, DependencyFlags.ByRegionBit)];
    }

    private readonly PvrVulkanOwned<VkRenderPass>?[] _renderPasses = new PvrVulkanOwned<VkRenderPass>?[8];
}

internal sealed class PvrVulkanOitRttRenderPasses : PvrVulkanOitRenderPasses
{
    protected override AttachmentDescription GetAttachment0Description(bool initial, bool last, bool loadClear) =>
        new(0, Format.R8G8B8A8Unorm, SampleCountFlags.Count1Bit, AttachmentLoadOp.Clear, AttachmentStoreOp.Store,
            AttachmentLoadOp.DontCare, AttachmentStoreOp.DontCare, ImageLayout.Undefined,
            PvrConfiguration.RenderToTextureBuffer && last ? ImageLayout.TransferSrcOptimal : ImageLayout.ShaderReadOnlyOptimal);

    protected override List<SubpassDependency> GetSubpassDependencies()
    {
        if (PvrConfiguration.RenderToTextureBuffer)
            return [new(2, uint.MaxValue, PipelineStageFlags.ColorAttachmentOutputBit,
                PipelineStageFlags.TransferBit | PipelineStageFlags.HostBit, AccessFlags.ColorAttachmentWriteBit,
                AccessFlags.TransferReadBit | AccessFlags.HostReadBit)];
        else
            return [new(2, uint.MaxValue, PipelineStageFlags.ColorAttachmentOutputBit, PipelineStageFlags.FragmentShaderBit,
                AccessFlags.ColorAttachmentWriteBit, AccessFlags.ShaderReadBit)];
    }
}
