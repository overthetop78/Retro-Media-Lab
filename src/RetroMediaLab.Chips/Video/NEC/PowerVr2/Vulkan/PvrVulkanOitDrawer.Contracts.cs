using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Vulkan;

internal abstract unsafe partial class PvrVulkanOitDrawer : PvrVulkanBaseDrawer
{
    internal abstract CommandBuffer NewFrame();
    internal abstract void EndFrame();

    protected void Init(PvrVulkanSamplerManager samplerManager, PvrVulkanOitPipelineManager pipelineManager, PvrVulkanOitBuffers oitBuffers)
    {
        _pipelineManager = pipelineManager;
        _samplerManager = samplerManager;
        _quadBuffer ??= new PvrVulkanQuadBuffer();
        _oitBuffers = oitBuffers;
        _descriptorSets.Init(samplerManager, pipelineManager.GetPipelineLayout(), pipelineManager.GetPerFrameDSLayout(),
            pipelineManager.GetPerPolyDSLayout(), pipelineManager.GetColorInputDSLayout());
    }

    internal virtual void Term()
    {
        try
        {
            _quadBuffer?.Dispose();
            ColorAttachments[0]?.Dispose();
            ColorAttachments[1]?.Dispose();
            TempFramebuffers[0]?.Dispose();
            TempFramebuffers[1]?.Dispose();
            DepthAttachments[0]?.Dispose();
            DepthAttachments[1]?.Dispose();
            foreach (PvrVulkanBuffer buffer in MainBuffers)
                buffer.Dispose();
            MainBuffers.Clear();
        }
        finally
        {
            _quadBuffer = null;
            ColorAttachments[0] = ColorAttachments[1] = null;
            TempFramebuffers[0] = TempFramebuffers[1] = null;
            DepthAttachments[0] = DepthAttachments[1] = null;
            _descriptorSets.Term();
            MaxWidth = 0;
            MaxHeight = 0;
        }
    }

    protected void NewImage() => _descriptorSets.NextFrame();

    protected PvrVulkanBuffer GetMainBuffer(uint size) => base.GetMainBuffer(size, BufferUsageFlags.StorageBufferBit);

    protected abstract Framebuffer GetFramebuffer(int renderPass, int renderPassCount);
    protected int GetFramebufferIndex() => FramebufferIndex;
    protected abstract ImageLayout GetAttachmentInitialLayout();

    protected Rect2D Viewport;
    protected readonly PvrVulkanFramebufferAttachment?[] ColorAttachments = new PvrVulkanFramebufferAttachment?[2];
    protected readonly PvrVulkanFramebufferAttachment?[] DepthAttachments = new PvrVulkanFramebufferAttachment?[2];
    protected readonly PvrVulkanOwned<Framebuffer>?[] TempFramebuffers = new PvrVulkanOwned<Framebuffer>?[2];
    protected CommandBuffer CurrentCommandBuffer;
    protected List<bool> ClearNeeded = [];
    protected int MaxWidth;
    protected int MaxHeight;
    protected int FramebufferIndex;

    private struct BufferOffsets
    {
        internal ulong IndexOffset;
        internal ulong ModVolOffset;
        internal ulong VertexUniformOffset;
        internal ulong FragmentUniformOffset;
        internal ulong PolyParamsOffset;
        internal ulong PolyParamsSize;
        internal ulong Naomi2OpaqueOffset;
        internal ulong Naomi2PunchThroughOffset;
        internal ulong Naomi2TranslucentOffset;
        internal ulong Naomi2ModVolOffset;
        internal ulong Naomi2TrModVolOffset;
        internal ulong LightsOffset;
    }

    private BufferOffsets _offsets;
    private PvrVulkanQuadBuffer? _quadBuffer;
    private PvrVulkanOitPipelineManager? _pipelineManager;
    private PvrVulkanSamplerManager? _samplerManager;
    private PvrVulkanOitBuffers? _oitBuffers;
    private bool _needAttachmentTransition;
    private readonly PvrVulkanOitDescriptorSets _descriptorSets = new();
    private Buffer _curMainBuffer;
    private bool _dithering;
    private ImageUsageFlags _currentBufferUsage;
}

internal unsafe sealed partial class PvrVulkanOitScreenDrawer : PvrVulkanOitDrawer
{
    internal void Init(PvrVulkanSamplerManager samplerManager, PvrVulkanOitShaders shaderManager,
        PvrVulkanOitBuffers oitBuffers, in Extent2D viewport)
    {
        _emulateFramebuffer = PvrConfiguration.EmulateFramebuffer;
        if (_screenPipelineManager == null)
        {
            _screenPipelineManager = new PvrVulkanOitPipelineManager();
            _screenPipelineManager.Init(shaderManager, oitBuffers);
        }

        base.Init(samplerManager, _screenPipelineManager, oitBuffers);
        MakeFramebuffers(viewport);
        GetContext().PresentFrame(default, default, viewport, 0);
    }

    internal override void Term()
    {
        try
        {
            _screenPipelineManager?.Dispose();
            _screenPipelineManager = null;
        }
        finally
        {
            base.Term();
        }
    }

    internal override void EndFrame()
    {
        if (!_frameStarted)
            return;
        _frameStarted = false;
        if (_emulateFramebuffer)
        {
            ScaleAndWriteFramebuffer(CurrentCommandBuffer, ColorAttachments[FramebufferIndex]!);
        }
        else
        {
            PvrVk.End(CurrentCommandBuffer);
            CommandPool!.EndFrame();
            _aspectRatio = PvrTransformMatrix.GetOutputFramebufferAspectRatio();
        }
        CurrentCommandBuffer = default;
        _frameRendered = true;
    }

    internal bool PresentFrame()
    {
        EndFrame();
        if (!_frameRendered)
            return false;
        _frameRendered = false;
        PvrVulkanFramebufferAttachment attachment = ColorAttachments[FramebufferIndex]!;
        GetContext().PresentFrame(attachment.GetImage(), attachment.GetImageView(), Viewport.Extent, _aspectRatio);
        return true;
    }

    internal Silk.NET.Vulkan.RenderPass GetRenderPass() => _screenPipelineManager!.GetRenderPass(false, true);
    internal CommandBuffer GetCurrentCommandBuffer() => CurrentCommandBuffer;

    protected override ImageLayout GetAttachmentInitialLayout() =>
        _emulateFramebuffer ? ImageLayout.TransferSrcOptimal : ImageLayout.ShaderReadOnlyOptimal;

    private PvrVulkanOitPipelineManager? _screenPipelineManager;
    private bool _frameRendered;
    private float _aspectRatio;
    private bool _frameStarted;
    private bool _emulateFramebuffer;
}

internal unsafe sealed partial class PvrVulkanOitTextureDrawer : PvrVulkanOitDrawer
{
    internal void Init(PvrVulkanSamplerManager samplerManager, PvrVulkanOitShaders shaderManager,
        PvrVulkanTextureCache textureCache, PvrVulkanOitBuffers oitBuffers)
    {
        _rttPipelineManager ??= new PvrVulkanOitRttPipelineManager();
        _rttPipelineManager.Init(shaderManager, oitBuffers);
        base.Init(samplerManager, _rttPipelineManager, oitBuffers);
        _textureCache = textureCache;
    }

    internal override void Term()
    {
        try
        {
            _framebuffer?.Dispose();
            _colorAttachment?.Dispose();
            _rttPipelineManager?.Dispose();
        }
        finally
        {
            _framebuffer = null;
            _colorAttachment = null;
            _rttPipelineManager = null;
            base.Term();
        }
    }

    protected override ImageLayout GetAttachmentInitialLayout() => ImageLayout.ShaderReadOnlyOptimal;

    private uint _textureAddr;
    private PvrVulkanTexture? _texture;
    private Image _colorImage;
    private PvrVulkanFramebufferAttachment? _colorAttachment;
    private PvrVulkanOitRttPipelineManager? _rttPipelineManager;
    private PvrVulkanOwned<Framebuffer>? _framebuffer;
    private PvrVulkanTextureCache? _textureCache;
}
