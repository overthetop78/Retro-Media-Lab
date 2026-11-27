using Silk.NET.Vulkan;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Vulkan;

internal sealed unsafe partial class PvrVulkanOverlay
{
    public void Dispose() => Term();

    internal void Init(PvrVulkanQuadPipeline pipeline)
    {
        _pipeline = pipeline;
        for (int i = 0; i < _drawers.Length; i++)
        {
            _drawers[i]?.Dispose();
            _drawers[i] = new PvrVulkanQuadDrawer();
            _drawers[i]!.Init(pipeline);
        }
        _crosshairDrawer?.Dispose();
        _crosshairDrawer = new PvrVulkanQuadDrawer();
        _crosshairDrawer.Init(pipeline);
        Array.Clear(_vmuLastChanged);
    }

    internal void Term()
    {
        try
        {
            foreach (var buffer in _commandBuffers)
                buffer?.Dispose();
        }
        finally
        {
            _commandBuffers.Clear();
            try
            {
                for (int i = 0; i < _drawers.Length; i++)
                {
                    _drawers[i]?.Dispose();
                    _drawers[i] = null;
                }
                _crosshairDrawer?.Dispose();
                _crosshairDrawer = null;
            }
            finally
            {
                for (int i = 0; i < _vmuTextures.Length; i++)
                {
                    _vmuTextures[i]?.Dispose();
                    _vmuTextures[i] = null;
                }
                _crosshairTexture?.Dispose();
                _crosshairTexture = null;
            }
        }
    }

    private static PvrVulkanTexture CreateTexture(CommandBuffer commandBuffer, int width, int height, byte* data)
    {
        var texture = new PvrVulkanTexture { TextureType = PvrTextureType._8888 };
        bool initialized = false;
        try
        {
            texture.SetCommandBuffer(commandBuffer);
            texture.UploadToGpu(width, height, data, false);
            texture.SetCommandBuffer(default);
            initialized = true;
            return texture;
        }
        finally
        {
            if (!initialized)
                texture.Dispose();
        }
    }

    internal void Prepare(CommandBuffer commandBuffer, bool vmu, bool crosshair)
    {
        if (vmu)
        {
            for (int i = 0; i < _vmuTextures.Length; i++)
            {
                ref PvrVulkanTexture? texture = ref _vmuTextures[i];
                if (!PvrOsd.VmuLcdStatus[i])
                {
                    if (texture != null)
                    {
                        texture.DeferDeleteResource(PvrVulkanContext.Instance);
                        texture.Dispose();
                        texture = null;
                    }
                    continue;
                }
                if (texture != null && PvrOsd.VmuLastChanged[i] == _vmuLastChanged[i])
                    continue;

                if (texture != null)
                {
                    texture.DeferDeleteResource(PvrVulkanContext.Instance);
                    texture.Dispose();
                }
                fixed (uint* data = &PvrOsd.VmuLcdData[i, 0])
                    texture = CreateTexture(commandBuffer, 48, 32, (byte*)data);
#if VK_DEBUG
                PvrVulkanContext.Instance.SetObjectName(texture.GetImageView(), $"VMU {i}");
#endif
                _vmuLastChanged[i] = PvrOsd.VmuLastChanged[i];
            }
        }
        if (crosshair && _crosshairTexture == null)
        {
            fixed (uint* data = PvrOsd.GetCrosshairTextureData())
                _crosshairTexture = CreateTexture(commandBuffer, 16, 16, (byte*)data);
        }
    }

    internal CommandBuffer Prepare(CommandPool commandPool, bool vmu, bool crosshair)
    {
        PvrVulkanContext context = PvrVulkanContext.Instance;
        int count = (int)context.GetSwapChainSize();
        while (_commandBuffers.Count > count)
        {
            _commandBuffers[^1]?.Dispose();
            _commandBuffers.RemoveAt(_commandBuffers.Count - 1);
        }
        while (_commandBuffers.Count < count)
            _commandBuffers.Add(null);
        int index = (int)context.GetCurrentImageIndex();
        _commandBuffers[index]?.Dispose();
        _commandBuffers[index] = context.GetDevice().AllocateCommandBufferUnique(commandPool, CommandBufferLevel.Primary);
        CommandBuffer buffer = _commandBuffers[index]!.Handle;
        PvrVk.Begin(buffer, CommandBufferUsageFlags.OneTimeSubmitBit);
        Prepare(buffer, vmu, crosshair);
        PvrVk.End(buffer);
        return buffer;
    }

    internal void Draw(CommandBuffer commandBuffer, Extent2D viewport, float scaling, bool vmu, bool crosshair)
    {
        PvrVulkanQuadVertex* vertices = stackalloc PvrVulkanQuadVertex[4]
        {
            new(-1f, -1f, 0f, 0f, 1f), new( 1f, -1f, 0f, 1f, 1f),
            new(-1f,  1f, 0f, 0f, 0f), new( 1f,  1f, 0f, 1f, 0f)
        };
        Vk api = PvrVulkanContext.Instance.Api;
        if (vmu)
        {
            float padding = 8f * scaling;
            float height = 32f * scaling;
            float width = 48f * scaling;
            _pipeline!.BindPipeline(commandBuffer);
            height *= 2f;
            width *= 2f;
            float* color = stackalloc float[4] { 0.75f, 0.75f, 0.75f, 0.75f };
            for (int i = 0; i < _vmuTextures.Length; i++)
            {
                var texture = _vmuTextures[i];
                if (texture == null)
                    continue;
                float x = (i & 2) != 0 ? viewport.Width - padding - width : padding;
                float y;
                if ((i & 4) != 0)
                {
                    y = viewport.Height - padding - height;
                    if ((i & 1) != 0)
                        y -= padding + height;
                }
                else
                {
                    y = padding;
                    if ((i & 1) != 0)
                        y += padding + height;
                }
                var view = new Viewport(x, y, width, height, 0f, 1f);
                var scissor = new Rect2D(new Offset2D((int)x, (int)y), new Extent2D((uint)width, (uint)height));
                api.CmdSetViewport(commandBuffer, 0, 1, &view);
                api.CmdSetScissor(commandBuffer, 0, 1, &scissor);
                _drawers[i]!.Draw(commandBuffer, texture.GetImageView(), vertices, true, color);
            }
        }
        if (crosshair)
        {
            _pipeline!.BindPipeline(commandBuffer);
            bool imageViewBound = false;
            float* color = stackalloc float[4];
            for (int i = 0; i < PvrConfiguration.CrosshairColor.Length; i++)
            {
                if (!PvrOsd.CrosshairNeeded(i))
                    continue;
                (float x, float y) = PvrOsd.GetCrosshairPosition(i);
                float width = PvrConfiguration.CrosshairSize * scaling;
                float height = PvrConfiguration.CrosshairSize * scaling;
                x -= width / 2;
                y -= height / 2;
                var view = new Viewport(x, y, width, height, 0f, 1f);
                var scissor = new Rect2D(new Offset2D((int)Math.Max(0f, x), (int)Math.Max(0f, y)),
                    new Extent2D((uint)width, (uint)height));
                api.CmdSetViewport(commandBuffer, 0, 1, &view);
                api.CmdSetScissor(commandBuffer, 0, 1, &scissor);
                uint packed = PvrConfiguration.CrosshairColor[i];
                color[0] = (packed & 0xff) / 255f;
                color[1] = ((packed >> 8) & 0xff) / 255f;
                color[2] = ((packed >> 16) & 0xff) / 255f;
                color[3] = ((packed >> 24) & 0xff) / 255f;
                _crosshairDrawer!.Draw(commandBuffer, imageViewBound ? default : _crosshairTexture!.GetImageView(), vertices, true, color);
                imageViewBound = true;
            }
        }
    }
}
