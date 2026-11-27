using Silk.NET.Vulkan;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Vulkan;

internal sealed unsafe class PvrVulkanDriver : PvrImGuiDriver
{
    internal override void Reset()
    {
        base.Reset();
        try
        {
            foreach (VkTexture value in _textures.Values)
                value.Texture?.Dispose();
            _textures.Clear();
            _linearSampler?.Dispose();
            _linearSampler = null;
            _pointSampler?.Dispose();
            _pointSampler = null;
        }
        finally
        {
            PvrImGuiVulkan.Shutdown();
            _justStarted = true;
        }
    }

    public override void Dispose() => Reset();

    internal override void NewFrame()
    {
        PvrImGuiVulkan.NewFrame();
    }

    internal override void RenderDrawData(nint drawData, bool guiOpen)
    {
        PvrVulkanContext context = GetContext();
        if (!context.IsValid())
            return;
        try
        {
            bool rendering = context.IsRendering();
            if (!rendering)
                context.NewFrame();
            if (!rendering || _newFrameStarted)
            {
                context.BeginRenderPass();
                if (PvrRenderer.Backend!.RenderLastFrame())
                    context.PresentLastFrame();
            }
            if (!_justStarted)
            {
                CommandBuffer target = GetCommandBuffer();
                using var scope = new PvrVulkanCommandBufferDebugScope(target, "ImGui", [0.26f, 0.59f, 0.98f, 0.31f]);
                PvrImGuiVulkan.RenderDrawData(drawData, GetCommandBuffer());
            }
            _justStarted = false;
            if (!rendering || _newFrameStarted)
                context.EndFrame();
            _newFrameStarted = false;
        }
        catch (PvrInvalidVulkanContextException)
        {
            context.Resize();
        }
        catch (PvrVulkanException error)
        {
            PvrRuntime.Log.Error("Vulkan system error: {0}", error.Message);
            throw new PvrRendererException("Vulkan system error");
        }
    }

    internal override void Present()
    {
        try
        {
            GetContext().Present();
        }
        catch (PvrVulkanException error)
        {
            PvrRuntime.Log.Error("Vulkan system error: {0}", error.Message);
            throw new PvrRendererException("Vulkan system error");
        }
    }

    internal override nint GetTexture(string name) => _textures.TryGetValue(name, out var texture) ? texture.TextureId : 0;

    internal override nint UpdateTexture(string name, byte* data, int width, int height, bool nearestSampling)
    {
        if (_justStarted)
            return 0;
        try
        {
            var vkTexture = new VkTexture(new PvrVulkanTexture { TextureType = PvrTextureType._8888 });
            bool stored = false;
            try
            {
                vkTexture.Texture!.SetCommandBuffer(GetCommandBuffer());
                vkTexture.Texture.UploadToGpu(width, height, data, false);
                vkTexture.Texture.SetCommandBuffer(default);
                Sampler sampler;
                if (nearestSampling)
                {
                    _pointSampler ??= GetContext().GetDevice().CreateSamplerUnique(new SamplerCreateInfo
                    {
                        SType = StructureType.SamplerCreateInfo, MagFilter = Filter.Nearest, MinFilter = Filter.Nearest,
                        MipmapMode = SamplerMipmapMode.Nearest, AddressModeU = SamplerAddressMode.ClampToBorder,
                        AddressModeV = SamplerAddressMode.ClampToBorder, AddressModeW = SamplerAddressMode.ClampToBorder,
                        CompareOp = CompareOp.Never, MaxLod = 1000f, BorderColor = BorderColor.FloatTransparentBlack
                    });
                    sampler = _pointSampler.Handle;
                }
                else
                {
                    _linearSampler ??= GetContext().GetDevice().CreateSamplerUnique(new SamplerCreateInfo
                    {
                        SType = StructureType.SamplerCreateInfo, MagFilter = Filter.Linear, MinFilter = Filter.Linear,
                        MipmapMode = SamplerMipmapMode.Linear, AddressModeU = SamplerAddressMode.ClampToBorder,
                        AddressModeV = SamplerAddressMode.ClampToBorder, AddressModeW = SamplerAddressMode.ClampToBorder,
                        CompareOp = CompareOp.Never, MaxLod = 1000f, BorderColor = BorderColor.FloatTransparentBlack
                    });
                    sampler = _linearSampler.Handle;
                }
                nint textureId = (nint)PvrImGuiVulkan.AddTexture(sampler, vkTexture.Texture.GetImageView(), ImageLayout.ShaderReadOnlyOptimal).Handle;
                vkTexture.TextureId = textureId;

                if (_textures.TryGetValue(name, out var previous))
                    previous.Texture?.Dispose();
                _textures[name] = vkTexture;
                stored = true;
                return textureId;
            }
            finally
            {
                if (!stored)
                    vkTexture.Texture?.Dispose();
            }
        }
        catch (PvrVulkanException error)
        {
            PvrRuntime.Log.Error("Vulkan system error: {0}", error.Message);
            throw new PvrRendererException("Vulkan system error");
        }
    }

    internal override void DeleteTexture(string name)
    {
        if (_textures.TryGetValue(name, out var value))
        {
            PvrVulkanContext context = GetContext();
            context.AddToFlight(new DescriptorSetDeleter(new DescriptorSet((ulong)value.TextureId)));
            if (value.Texture != null)
            {
                value.Texture.DeferDeleteResource(context);
                value.Texture.Dispose();
            }
            _textures.Remove(name);
        }
    }

    private sealed class DescriptorSetDeleter(DescriptorSet set) : IPvrVulkanDeletable
    {
        public void Dispose() => PvrImGuiVulkan.RemoveTexture(set);
    }

    private struct VkTexture(PvrVulkanTexture texture, nint textureId = 0)
    {
        internal PvrVulkanTexture? Texture = texture;
        internal nint TextureId = textureId;
    }

    private static PvrVulkanContext GetContext() => PvrVulkanContext.Instance;

    private CommandBuffer GetCommandBuffer()
    {
        PvrVulkanContext context = GetContext();
        if (!context.IsRendering())
        {
            if (context.RecreateSwapChainIfNeeded())
                throw new PvrInvalidVulkanContextException();
            context.NewFrame();
            _newFrameStarted = true;
        }
        return context.GetCurrentCommandBuffer();
    }

    private readonly Dictionary<string, VkTexture> _textures = [];
    private PvrVulkanOwned<Sampler>? _linearSampler;
    private PvrVulkanOwned<Sampler>? _pointSampler;
    private bool _newFrameStarted;
    private bool _justStarted = true;
}
