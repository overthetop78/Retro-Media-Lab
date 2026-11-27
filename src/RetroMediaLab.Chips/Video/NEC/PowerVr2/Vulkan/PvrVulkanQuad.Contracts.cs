using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using RenderPass = Silk.NET.Vulkan.RenderPass;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Vulkan;

[StructLayout(LayoutKind.Sequential)]
internal record struct PvrVulkanQuadVertex(float X, float Y, float Z, float U, float V);

internal sealed unsafe partial class PvrVulkanQuadBuffer : IDisposable
{
    internal PvrVulkanQuadBuffer()
    {
        _buffer = new PvrVulkanBuffer((ulong)(sizeof(PvrVulkanQuadVertex) * 4), BufferUsageFlags.VertexBufferBit);
    }

    internal void Bind(CommandBuffer commandBuffer)
    {
        PvrVk.BindVertexBuffers(commandBuffer, 0, [_buffer.Buffer], [0]);
    }

    internal void Draw(CommandBuffer commandBuffer)
    {
        PvrVulkanContext.Instance.Api.CmdDraw(commandBuffer, 4, 1, 0, 0);
    }

    internal void Update(PvrVulkanQuadVertex* vertices = null)
    {
        PvrVulkanQuadVertex* defaultVertices = stackalloc PvrVulkanQuadVertex[4]
        {
            new(-1f, -1f, 0f, 0f, 0f), new( 1f, -1f, 0f, 1f, 0f),
            new(-1f,  1f, 0f, 0f, 1f), new( 1f,  1f, 0f, 1f, 1f)
        };
        if (vertices == null)
            vertices = defaultVertices;
        nuint hash = HashQuadVertexData(vertices);
        if (hash == _lastUploadHash)
            return;
        _lastUploadHash = hash;
        _buffer.Upload((uint)(sizeof(PvrVulkanQuadVertex) * 4), vertices);
    }

    public void Dispose() => _buffer.Dispose();

    private readonly PvrVulkanBuffer _buffer;
    private nuint _lastUploadHash;
}

internal sealed unsafe partial class PvrVulkanQuadPipeline(bool ignoreTexAlpha, bool rotate = false) : IDisposable
{
    internal void Init(PvrVulkanQuadPipeline other) => Init(other._shaderManager!, other._renderPass, other._subpass);

    internal void Term()
    {
        try
        {
            _pipeline?.Dispose();
            _linearSampler?.Dispose();
            _nearestSampler?.Dispose();
        }
        finally
        {
            _pipelineLayout?.Dispose();
            _descSetLayout?.Dispose();
            _pipeline = null;
            _linearSampler = null;
            _nearestSampler = null;
            _pipelineLayout = null;
            _descSetLayout = null;
        }
    }

    public void Dispose() => Term();

    internal void BindPipeline(CommandBuffer commandBuffer) =>
        PvrVulkanContext.Instance.Api.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, GetPipeline());

    internal DescriptorSetLayout GetDescSetLayout() => _descSetLayout!.Handle;
    internal PipelineLayout GetPipelineLayout() => _pipelineLayout!.Handle;
    internal Sampler GetLinearSampler() => _linearSampler!.Handle;
    internal Sampler GetNearestSampler() => _nearestSampler!.Handle;

    private Pipeline GetPipeline()
    {
        if (_pipeline == null)
            CreatePipeline();
        return _pipeline!.Handle;
    }

    private RenderPass _renderPass;
    private int _subpass;
    private PvrVulkanOwned<Pipeline>? _pipeline;
    private PvrVulkanOwned<Sampler>? _linearSampler;
    private PvrVulkanOwned<Sampler>? _nearestSampler;
    private PvrVulkanOwned<PipelineLayout>? _pipelineLayout;
    private PvrVulkanOwned<DescriptorSetLayout>? _descSetLayout;
    private PvrVulkanShaders? _shaderManager;
    private readonly bool _rotate = rotate;
    private readonly bool _ignoreTexAlpha = ignoreTexAlpha;
}

internal sealed unsafe partial class PvrVulkanQuadDrawer : IDisposable
{
    public void Dispose()
    {
        try
        {
            foreach (var set in _descriptorSets)
                set?.Dispose();
        }
        finally
        {
            _descriptorSets.Clear();
            _buffer?.Dispose();
            _buffer = null;
        }
    }

    private PvrVulkanQuadPipeline? _pipeline;
    private PvrVulkanQuadBuffer? _buffer;
    private readonly List<PvrVulkanOwned<DescriptorSet>?> _descriptorSets = [];
}
