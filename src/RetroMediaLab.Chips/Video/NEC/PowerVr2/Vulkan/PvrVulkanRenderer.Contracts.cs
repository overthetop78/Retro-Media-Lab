using Silk.NET.Vulkan;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Vulkan;

internal abstract unsafe partial class PvrVulkanRenderer : PvrRenderBackend
{
    protected PvrVulkanRenderer()
    {
        Viewport = new Extent2D(640, 480);
    }

    internal override bool RenderLastFrame() => !ClearLastFrame;

    internal override bool GetLastFrame(List<byte> data, ref int width, ref int height) =>
        GetContext().GetLastFrame(data, ref width, ref height);

    protected PvrVulkanContext GetContext() => PvrVulkanContext.Instance;

    protected virtual void Resize(int width, int height)
    {
        Viewport.Width = (uint)width;
        Viewport.Height = (uint)height;
    }

    protected readonly PvrVulkanShaders ShaderManager = new();
    protected PvrVulkanTexture? FogTexture;
    protected PvrVulkanTexture? PaletteTexture;
    protected readonly PvrVulkanCommandPool TexCommandPool = new();
    private readonly List<PvrVulkanTexture?> _framebufferTextures = [];
    private int _framebufferTexIndex;
    protected readonly PvrVulkanTextureCache TextureCache = new();
    protected Extent2D Viewport;
    protected CommandBuffer TexCommandBuffer;
    private PvrVulkanQuadPipeline? _quadPipeline;
    private PvrVulkanQuadDrawer? _framebufferDrawer;
    private readonly PvrVulkanCommandPool _fbCommandPool = new();
    protected bool FramebufferRendered;
    protected RenderContext? RendContext;
}
