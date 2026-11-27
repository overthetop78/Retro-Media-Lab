using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;
using VkRenderPass = Silk.NET.Vulkan.RenderPass;
using static RetroMediaLab.Chips.Video.NEC.PowerVr2.PvrRegisters;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Vulkan;

internal abstract unsafe partial class PvrVulkanBaseDrawer
{
    internal void SetCommandPool(PvrVulkanCommandPool commandPool) => CommandPool = commandPool;
    internal void SetRendContext(RenderContext context) => RendContext = context;

    protected PvrVulkanContext GetContext() => PvrVulkanContext.Instance;

    protected void SetScissor(CommandBuffer commandBuffer, in Rect2D scissor)
    {
        if (!scissor.Equals(CurrentScissor))
        {
            Rect2D bounds = scissor;
            GetContext().Api.CmdSetScissor(commandBuffer, 0, 1, &bounds);
            CurrentScissor = scissor;
        }
    }

    protected PvrVulkanBuffer GetMainBuffer(uint size, BufferUsageFlags extraFlags = 0)
    {
        BufferUsageFlags usage = BufferUsageFlags.VertexBufferBit | BufferUsageFlags.IndexBufferBit
            | BufferUsageFlags.UniformBufferBit | extraFlags;
        PvrVulkanBuffer buffer;
        if (MainBuffers.Count != 0)
        {
            buffer = MainBuffers[^1];
            MainBuffers.RemoveAt(MainBuffers.Count - 1);
            if (buffer.BufferSize < size)
            {
                CommandPool!.AddToFlight(new PvrVulkanDeleter<PvrVulkanBuffer>(buffer));
                uint newSize = (uint)buffer.BufferSize;
                while (newSize < size)
                    newSize *= 2;
                PvrRuntime.Log.Info("Increasing main buffer size {0} -> {1}", buffer.BufferSize, newSize);
                buffer = new PvrVulkanBuffer(newSize, usage);
            }
        }
        else
        {
            buffer = new PvrVulkanBuffer(Math.Max(512u * 1024, size), usage);
        }

        CommandPool!.AddToFlight(new BufferHolder(buffer, this));
        return buffer;
    }

    private sealed class BufferHolder(PvrVulkanBuffer buffer, PvrVulkanBaseDrawer drawer) : IPvrVulkanDeletable
    {
        public void Dispose() => drawer.MainBuffers.Add(buffer);
    }

    [SkipLocalsInit]
    protected T MakeFragmentUniforms<T>() where T : unmanaged
    {
        Unsafe.SkipInit(out T values);
        var uniforms = (PvrVulkanFragmentShaderUniforms*)&values;
        FOG_COL_VERT.GetRGBColor(new Span<float>(uniforms->FogColVert, 3));
        FOG_COL_RAM.GetRGBColor(new Span<float>(uniforms->FogColRam, 3));
        uniforms->FogDensity = FOG_DENSITY.Get() * PvrConfiguration.ExtraDepthScale;
        RendContext!.FogClampMin.GetRGBAColor(new Span<float>(uniforms->ColorClampMin, 4));
        RendContext.FogClampMax.GetRGBAColor(new Span<float>(uniforms->ColorClampMax, 4));
        uniforms->AlphaTestValue = (PT_ALPHA_REF & 0xff) / 255f;
        return values;
    }

    protected byte[] PackNaomi2Uniforms(PvrVulkanBufferPacker packer, bool trModVolIncluded,
        out ulong opaqueOffset, out ulong punchThroughOffset, out ulong translucentOffset,
        out ulong modVolOffset, out ulong trModVolOffset)
    {
        const uint uniformSize = (uint)sizeof(PvrVulkanN2VertexShaderUniforms);
        int stride = (int)(uniformSize + PvrVulkanUtilities.Align(uniformSize, (uint)GetContext().GetUniformBufferAlignment()));
        RenderContext context = RendContext!;
        int items = context.GlobalParamOp.Count + context.GlobalParamPt.Count + context.GlobalParamTr.Count + context.GlobalParamMvo.Count;
        if (trModVolIncluded)
            items += context.GlobalParamMvoTr.Count;
        byte[] data = GC.AllocateArray<byte>(items * stride, pinned: true);
        int offset = 0;
        foreach (PolyParam poly in context.GlobalParamOp)
            AddUniform(poly, 0);
        int ptStart = offset;
        foreach (PolyParam poly in context.GlobalParamPt)
            AddUniform(poly, 0);
        int trStart = offset;
        if (context.GlobalParamTr.Count != 0)
        {
            uint firstVertex = context.Idx[(int)context.GlobalParamTr[0].First];
            for (int index = 0; index < context.GlobalParamTr.Count; index++)
                AddUniform(context.GlobalParamTr[index], unchecked((index << 17) - (int)firstVertex));
        }
        int modVolStart = offset;
        foreach (ModifierVolumeParam volume in context.GlobalParamMvo)
            AddVolumeUniform(volume);
        int trModVolStart = offset;
        if (trModVolIncluded)
            foreach (ModifierVolumeParam volume in context.GlobalParamMvoTr)
                AddVolumeUniform(volume);

        fixed (byte* baseAddress = data)
            opaqueOffset = packer.AddUniform(baseAddress, (nuint)offset);
        punchThroughOffset = opaqueOffset + (uint)ptStart;
        translucentOffset = opaqueOffset + (uint)trStart;
        modVolOffset = opaqueOffset + (uint)modVolStart;
        trModVolOffset = opaqueOffset + (uint)trModVolStart;
        return data;

        void AddUniform(in PolyParam poly, int polyNumber)
        {
            if (poly.IsNaomi2())
            {
                fixed (byte* address = &data[offset])
                {
                    var uniform = (PvrVulkanN2VertexShaderUniforms*)address;
                    uniform->MvMat = ReadMatrix(context.Matrices[poly.MvMatrix]);
                    uniform->NormalMat = ReadMatrix(context.Matrices[poly.NormalMatrix]);
                    uniform->ProjMat = ReadMatrix(context.Matrices[poly.ProjMatrix]);
                    uniform->BumpMapping = poly.Pcw.Texture == 1 && poly.Tcw.PixelFmt == (uint)PixelFormat.PixelBumpMap ? 1 : 0;
                    uniform->PolyNumber = polyNumber;
                    for (int i = 0; i < 2; i++)
                    {
                        uniform->EnvMapping[i] = poly.EnvMapping[i] ? 1 : 0;
                        uniform->GlossCoef[i] = poly.GlossCoef[i];
                        uniform->ConstantColor[i] = poly.ConstantColor[i] ? 1 : 0;
                    }
                }
            }
            offset += stride;
        }

        void AddVolumeUniform(in ModifierVolumeParam volume)
        {
            if (volume.IsNaomi2())
            {
                fixed (byte* address = &data[offset])
                {
                    var uniform = (PvrVulkanN2VertexShaderUniforms*)address;
                    uniform->MvMat = ReadMatrix(context.Matrices[volume.MvMatrix]);
                    uniform->ProjMat = ReadMatrix(context.Matrices[volume.ProjMatrix]);
                }
            }
            offset += stride;
        }

        static System.Numerics.Matrix4x4 ReadMatrix(in N2Matrix source)
        {
            fixed (float* matrix = source.Mat)
                return Unsafe.ReadUnaligned<System.Numerics.Matrix4x4>(matrix);
        }
    }

    protected ulong PackNaomi2Lights(PvrVulkanBufferPacker packer, N2LightModel* lights)
    {
        ulong offset = ulong.MaxValue;
        uint size = (uint)sizeof(N2LightModel);
        ulong lightStride = size + PvrVulkanUtilities.Align(size, (uint)GetContext().GetUniformBufferAlignment());
        int count = RendContext!.LightModels.Count;
        if (lightStride == size && count != 0)
        {
            offset = packer.AddUniform(lights, (nuint)count * size);
        }
        else
        {
            for (int index = 0; index < count; index++)
            {
                ulong lightOffset = packer.AddUniform(lights + index, size);
                if (offset == ulong.MaxValue)
                    offset = lightOffset;
            }
        }
        return offset;
    }

    protected Rect2D BaseScissor;
    protected Rect2D CurrentScissor;
    protected readonly PvrTransformMatrix Matrices = new();
    protected PvrVulkanCommandPool? CommandPool;
    protected readonly List<PvrVulkanBuffer> MainBuffers = [];
    protected RenderContext? RendContext;
}

internal abstract unsafe partial class PvrVulkanDrawer : PvrVulkanBaseDrawer
{
    internal virtual void Term()
    {
        try
        {
            _descriptorSets.Term();
        }
        finally
        {
            foreach (PvrVulkanBuffer buffer in MainBuffers)
                buffer.Dispose();
            MainBuffers.Clear();
        }
    }

    internal virtual void EndRenderPass() => RenderPassStarted = false;
    internal CommandBuffer GetCurrentCommandBuffer() => CurrentCommandBuffer;

    protected virtual uint GetSwapChainSize() => GetContext().GetSwapChainSize();
    protected abstract CommandBuffer BeginRenderPass();

    protected void NewImage()
    {
        _descriptorSets.NextFrame();
        _imageIndex = (int)((_imageIndex + 1) % GetSwapChainSize());
        if (_perStripSorting != PvrConfiguration.PerStripSorting)
        {
            _perStripSorting = PvrConfiguration.PerStripSorting;
            _pipelineManager!.Reset();
        }
    }

    protected void Init(PvrVulkanSamplerManager samplerManager, PvrVulkanPipelineManager pipelineManager)
    {
        _pipelineManager = pipelineManager;
        SamplerManager = samplerManager;
        _descriptorSets.Init(samplerManager, pipelineManager.GetPipelineLayout(), pipelineManager.GetPerFrameDSLayout(),
            pipelineManager.GetPerPolyDSLayout());
    }

    protected int GetCurrentImage() => _imageIndex;

    protected CommandBuffer CurrentCommandBuffer;
    protected PvrVulkanSamplerManager? SamplerManager;
    protected bool RenderPassStarted;

    private struct BufferOffsets
    {
        internal ulong IndexOffset;
        internal ulong ModVolOffset;
        internal ulong VertexUniformOffset;
        internal ulong FragmentUniformOffset;
        internal ulong Naomi2OpaqueOffset;
        internal ulong Naomi2PunchThroughOffset;
        internal ulong Naomi2TranslucentOffset;
        internal ulong Naomi2ModVolOffset;
        internal ulong Naomi2TrModVolOffset;
        internal ulong LightsOffset;
    }

    private int _imageIndex;
    private BufferOffsets _offsets;
    private readonly PvrVulkanDescriptorSetManager _descriptorSets = new();
    private Buffer _curMainBuffer;
    private PvrVulkanPipelineManager? _pipelineManager;
    private bool _perStripSorting;
    private bool _dithering;
}

internal unsafe sealed partial class PvrVulkanScreenDrawer : PvrVulkanDrawer
{
    internal override void Term()
    {
        try
        {
            _screenPipelineManager?.Dispose();
            _renderPassLoad?.Dispose();
            _renderPassClear?.Dispose();
            foreach (PvrVulkanOwned<Framebuffer> framebuffer in _framebuffers)
                framebuffer.Dispose();
            foreach (PvrVulkanFramebufferAttachment attachment in _colorAttachments)
                attachment.Dispose();
            _depthAttachment?.Dispose();
        }
        finally
        {
            _screenPipelineManager = null;
            _renderPassLoad = null;
            _renderPassClear = null;
            _framebuffers.Clear();
            _colorAttachments.Clear();
            _depthAttachment = null;
            _transitionNeeded.Clear();
            _clearNeeded.Clear();
            base.Term();
        }
    }

    internal VkRenderPass GetRenderPass() => _renderPassClear!.Handle;

    internal bool PresentFrame()
    {
        EndRenderPass();
        if (!_frameRendered)
            return false;
        _frameRendered = false;
        PvrVulkanFramebufferAttachment attachment = _colorAttachments[GetCurrentImage()];
        GetContext().PresentFrame(attachment.GetImage(), attachment.GetImageView(), _viewport, _aspectRatio);
        return true;
    }

    protected override uint GetSwapChainSize() => 2;

    private PvrVulkanPipelineManager? _screenPipelineManager;
    private PvrVulkanOwned<VkRenderPass>? _renderPassLoad;
    private PvrVulkanOwned<VkRenderPass>? _renderPassClear;
    private readonly List<PvrVulkanOwned<Framebuffer>> _framebuffers = [];
    private readonly List<PvrVulkanFramebufferAttachment> _colorAttachments = [];
    private PvrVulkanFramebufferAttachment? _depthAttachment;
    private Extent2D _viewport;
    private PvrVulkanShaders? _shaderManager;
    private readonly List<bool> _transitionNeeded = [];
    private readonly List<bool> _clearNeeded = [];
    private bool _frameRendered;
    private float _aspectRatio;
    private bool _emulateFramebuffer;
}

internal unsafe sealed partial class PvrVulkanTextureDrawer : PvrVulkanDrawer
{
    internal override void Term()
    {
        try
        {
            _rttPipelineManager?.Dispose();
            foreach (PvrVulkanOwned<Framebuffer> framebuffer in _framebuffers)
                framebuffer.Dispose();
            _colorAttachment?.Dispose();
            _depthAttachment?.Dispose();
        }
        finally
        {
            _rttPipelineManager = null;
            _framebuffers.Clear();
            _colorAttachment = null;
            _depthAttachment = null;
            base.Term();
        }
    }

    private uint _width;
    private uint _height;
    private uint _textureAddr;
    private PvrVulkanRttPipelineManager? _rttPipelineManager;
    private PvrVulkanTexture? _texture;
    private readonly List<PvrVulkanOwned<Framebuffer>> _framebuffers = [];
    private PvrVulkanFramebufferAttachment? _colorAttachment;
    private PvrVulkanFramebufferAttachment? _depthAttachment;
    private PvrVulkanTextureCache? _textureCache;
}
