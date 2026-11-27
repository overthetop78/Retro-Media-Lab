using Silk.NET.Vulkan;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Vulkan;

internal sealed unsafe partial class PvrVulkanOverlay : IDisposable
{
    private readonly PvrVulkanTexture?[] _vmuTextures = new PvrVulkanTexture?[8];
    private readonly List<PvrVulkanOwned<CommandBuffer>?> _commandBuffers = [];
    private readonly PvrVulkanQuadDrawer?[] _drawers = new PvrVulkanQuadDrawer?[8];
    private readonly ulong[] _vmuLastChanged = new ulong[8];
    private PvrVulkanQuadPipeline? _pipeline;
    private PvrVulkanTexture? _crosshairTexture;
    private PvrVulkanQuadDrawer? _crosshairDrawer;
}
