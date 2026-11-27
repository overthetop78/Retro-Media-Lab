using Silk.NET.Vulkan;
using RenderPass = Silk.NET.Vulkan.RenderPass;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Vulkan;

internal abstract unsafe partial class PvrVulkanRenderer
{
    protected bool BaseInit(RenderPass renderPass, int subpass = 0)
    {
        TexCommandPool.Init();
        _fbCommandPool.Init();
        _quadPipeline?.Dispose();
        _quadPipeline = new PvrVulkanQuadPipeline(false, false);
        _quadPipeline.Init(ShaderManager, renderPass, subpass);
        _framebufferDrawer?.Dispose();
        _framebufferDrawer = new PvrVulkanQuadDrawer();
        _framebufferDrawer.Init(_quadPipeline);
        return true;
    }

    internal override void Term()
    {
        try
        {
            GetContext().WaitIdle();
            GetContext().PresentFrame(default, default, default, 0);
#if VIDEO_ROUTING && OSX
            PvrRuntime.VideoRoutingTermVk();
#endif
        }
        finally
        {
            _framebufferDrawer?.Dispose();
            _framebufferDrawer = null;
            _quadPipeline?.Dispose();
            _quadPipeline = null;
            TextureCache.Clear();
            FogTexture?.Dispose();
            FogTexture = null;
            PaletteTexture?.Dispose();
            PaletteTexture = null;
            TexCommandPool.Term();
            _fbCommandPool.Term();
            foreach (PvrVulkanTexture? texture in _framebufferTextures)
                texture?.Dispose();
            _framebufferTextures.Clear();
            _framebufferTexIndex = 0;
            ShaderManager.Term();
        }
    }

    internal override PvrBaseTextureCacheData? GetTexture(TSP tsp, TCW tcw, int area)
    {
        PvrVulkanTexture texture = TextureCache.GetTextureCacheData(tsp, tcw, area);
        if (texture.NeedsUpdate())
        {
            texture.SetCommandBuffer(TexCommandBuffer);
            if (!texture.Update())
            {
                texture.SetCommandBuffer(default);
                return null;
            }
        }
        else if (texture.IsCustomTextureAvailable())
        {
            texture.DeferDeleteResource(TexCommandPool);
            texture.SetCommandBuffer(TexCommandBuffer);
            texture.CheckCustomTexture();
        }
        texture.SetCommandBuffer(default);
        TextureCache.SetInFlight(texture);
        return texture;
    }

    internal override void Process(TaContext context)
    {
        RendContext = context.Rend;
        if (!context.Rend.IsRtt)
        {
            FramebufferRendered = false;
            if (!PvrConfiguration.EmulateFramebuffer)
                ClearLastFrame = false;
        }
        if (ResetTextureCache)
        {
            TextureCache.Clear();
            ResetTextureCache = false;
        }

        TexCommandPool.BeginFrame();
        TextureCache.SetCurrentIndex(TexCommandPool.GetIndex());
        TextureCache.Cleanup();

        TexCommandBuffer = TexCommandPool.Allocate();
        PvrVk.Begin(TexCommandBuffer, CommandBufferUsageFlags.OneTimeSubmitBit);
        if (!context.Rend.IsRtt && context.Rend.SwapInterval > 0)
            GetContext().SetSwapInterval(context.Rend.SwapInterval);
        PowerVr2Ta.Parse(context, true);
        CheckFogTexture();
        CheckPaletteTexture();
        PvrVk.End(TexCommandBuffer);
    }

    internal void ReInitOsd()
    {
        TexCommandPool.Init();
        _fbCommandPool.Init();
    }

    internal override void RenderFramebuffer(in PvrFramebufferInfo info)
    {
        RendContext = null;
        int swapChainSize = (int)GetContext().GetSwapChainSize();
        _framebufferTexIndex = (_framebufferTexIndex + 1) % swapChainSize;
        for (int i = _framebufferTextures.Count - 1; i >= swapChainSize; i--)
        {
            _framebufferTextures[i]?.Dispose();
            _framebufferTextures.RemoveAt(i);
        }
        while (_framebufferTextures.Count < swapChainSize)
            _framebufferTextures.Add(null);

        PvrVulkanTexture texture = _framebufferTextures[_framebufferTexIndex] ??=
            new PvrVulkanTexture { TextureType = PvrTextureType._8888 };

        _fbCommandPool.BeginFrame();
        CommandBuffer commandBuffer = _fbCommandPool.Allocate();
        PvrVk.Begin(commandBuffer, CommandBufferUsageFlags.OneTimeSubmitBit);
        texture.SetCommandBuffer(commandBuffer);
        using (var debug = new PvrVulkanCommandBufferDebugScope(commandBuffer, "RenderFramebuffer", [0f, 1f, 0f, 1f]))
        {
            if (info.ReadControl.fb_enable == 0 || info.VideoControl.blank_video == 1)
            {
                byte* rgba = stackalloc byte[4]
                {
                    (byte)info.VideoBorderColor._red, (byte)info.VideoBorderColor._green,
                    (byte)info.VideoBorderColor._blue, 255
                };
                texture.UploadToGpu(1, 1, rgba, false);
            }
            else
            {
                using var pixels = new PvrPixelBuffer<uint>();
                PvrTextureCache.ReadFramebuffer<PvrRgbaPacker>(in info, pixels, out int width, out int height);
                texture.UploadToGpu(width, height, (byte*)pixels.Data(0, 0), false);
            }
        }
        texture.SetCommandBuffer(default);
        PvrVk.End(commandBuffer);
        _fbCommandPool.EndFrame();
        FramebufferRendered = true;
        ClearLastFrame = false;
    }

    internal void RenderVideoRouting()
    {
#if VIDEO_ROUTING && OSX
        if (PvrConfiguration.VideoRouting)
        {
            var device = GetContext().GetDevice();
            Image source = device.GetSwapchainImages(GetContext().GetSwapChain())[GetContext().GetCurrentImageIndex()];
            Queue queue = device.GetQueue(GetContext().GetGraphicsQueueFamilyIndex(), 0);
            int width = PvrConfiguration.VideoRoutingScale
                ? PvrConfiguration.VideoRoutingVRes * PvrRuntime.DisplayWidth / PvrRuntime.DisplayHeight
                : PvrRuntime.DisplayWidth;
            int height = PvrConfiguration.VideoRoutingScale ? PvrConfiguration.VideoRoutingVRes : PvrRuntime.DisplayHeight;
            PvrRuntime.VideoRoutingPublishFrameTexture(device, source, queue, 0, 0, width, height);
        }
        else
        {
            PvrRuntime.VideoRoutingTermVk();
        }
#endif
    }

    protected void CheckFogTexture()
    {
        if (FogTexture == null)
        {
            FogTexture = new PvrVulkanTexture { TextureType = PvrTextureType._8 };
            UpdateFogTable = true;
        }
        if (!UpdateFogTable || !PvrConfiguration.Fog)
            return;
        UpdateFogTable = false;
        byte* data = stackalloc byte[256];
        PvrTextureCache.MakeFogTexture(data);

        FogTexture.SetCommandBuffer(TexCommandBuffer);
        FogTexture.UploadToGpu(128, 2, data, false);
        FogTexture.SetCommandBuffer(default);
    }

    protected void CheckPaletteTexture()
    {
        if (PaletteTexture == null)
        {
            PaletteTexture = new PvrVulkanTexture { TextureType = PvrTextureType._8888 };
        }
        else if (!UpdatePalette)
        {
            return;
        }
        UpdatePalette = false;
        PaletteTexture.SetCommandBuffer(TexCommandBuffer);
        fixed (uint* palette = PvrTextureConversion.Palette32Ram)
            PaletteTexture.UploadToGpu(1024, 1, (byte*)palette, false);
        PaletteTexture.SetCommandBuffer(default);
    }

    protected bool PresentFramebuffer()
    {
        if (_framebufferTexIndex >= _framebufferTextures.Count)
            return false;
        PvrVulkanTexture? texture = _framebufferTextures[_framebufferTexIndex];
        if (texture == null)
            return false;
        GetContext().PresentFrame(texture.GetImage(), texture.GetImageView(), texture.GetSize(),
            PvrTransformMatrix.GetDcFramebufferAspectRatio());
        FramebufferRendered = false;
        return true;
    }

    internal static PvrRenderBackend Create() => new PvrVulkanStandardRenderer();

    internal static void ReInitOsdForActiveRenderer()
    {
        if (PvrRenderer.Backend is PvrVulkanRenderer renderer)
            renderer.ReInitOsd();
    }
}

internal sealed class PvrVulkanStandardRenderer : PvrVulkanRenderer
{
    internal override bool Init()
    {
        PvrRuntime.Log.Notice("VulkanRenderer::Init");
        try
        {
            _textureDrawer.Init(_samplerManager, ShaderManager, TextureCache);
            _textureDrawer.SetCommandPool(TexCommandPool);

            _screenDrawer.Init(_samplerManager, ShaderManager, Viewport);
            _screenDrawer.SetCommandPool(TexCommandPool);
            BaseInit(_screenDrawer.GetRenderPass());
            _emulateFramebuffer = PvrConfiguration.EmulateFramebuffer;
            return true;
        }
        catch (PvrVulkanException error)
        {
            PvrRuntime.Log.Error("Vulkan system error {0}", error.Message);
            return false;
        }
    }

    internal override void Term()
    {
        try
        {
            try
            {
                PvrRuntime.Log.Debug("VulkanRenderer::Term");
                GetContext().WaitIdle();
            }
            finally
            {
                TexCommandPool.Term();
                _screenDrawer.Term();
                _textureDrawer.Term();
                _samplerManager.Term();
                base.Term();
            }
        }
        catch (PvrVulkanException error)
        {
            PvrRuntime.Log.Error("Vulkan system error {0}", error.Message);
        }
    }

    internal override void Process(TaContext context)
    {
        try
        {
            if (_emulateFramebuffer != PvrConfiguration.EmulateFramebuffer)
            {
                _screenDrawer.EndRenderPass();
                PvrVulkanContext.Instance.WaitIdle();
                _screenDrawer.Term();
                _screenDrawer.Init(_samplerManager, ShaderManager, Viewport);
                BaseInit(_screenDrawer.GetRenderPass());
                _emulateFramebuffer = PvrConfiguration.EmulateFramebuffer;
            }
            else if (context.Rend.IsRtt)
            {
                _screenDrawer.EndRenderPass();
            }
            base.Process(context);
        }
        catch (PvrVulkanException error) when (error.Result == Result.ErrorOutOfDeviceMemory)
        {
            PvrRuntime.Log.Error("Vulkan out of device memory: {0}", error.Message);
            throw new PvrRendererException("Out of device memory");
        }
        catch (PvrVulkanException error)
        {
            PvrRuntime.Log.Error("Vulkan system error {0}", error.Message);
            throw new PvrRendererException("Vulkan system error");
        }
    }

    internal override bool Render()
    {
        try
        {
            PvrVulkanDrawer drawer;
            if (RendContext!.IsRtt)
            {
                drawer = _textureDrawer;
            }
            else
            {
                Resize(RendContext.FramebufferWidth, RendContext.FramebufferHeight);
                drawer = _screenDrawer;
            }
            drawer.SetRendContext(RendContext);
            drawer.Draw(FogTexture, PaletteTexture);
            if (PvrConfiguration.EmulateFramebuffer || RendContext.IsRtt)
                drawer.EndRenderPass();

            return !RendContext.IsRtt;
        }
        catch (PvrVulkanException error) when (error.Result == Result.ErrorOutOfDeviceMemory)
        {
            PvrRuntime.Log.Error("Vulkan out of device memory: {0}", error.Message);
            throw new PvrRendererException("Out of device memory");
        }
        catch (PvrVulkanException error)
        {
            PvrRuntime.Log.Error("Vulkan system error {0}", error.Message);
            throw new PvrRendererException("Vulkan system error");
        }
    }

    internal override bool Present()
    {
        if (ClearLastFrame)
            return false;
        try
        {
            if (PvrConfiguration.EmulateFramebuffer || FramebufferRendered)
                return PresentFramebuffer();
            return _screenDrawer.PresentFrame();
        }
        catch (PvrVulkanException error)
        {
            PvrRuntime.Log.Error("Vulkan system error {0}", error.Message);
            throw new PvrRendererException("Vulkan system error");
        }
    }

    protected override void Resize(int width, int height)
    {
        if ((uint)width == Viewport.Width && (uint)height == Viewport.Height)
            return;
        base.Resize(width, height);
        GetContext().WaitIdle();
        _screenDrawer.Init(_samplerManager, ShaderManager, Viewport);
    }

    private readonly PvrVulkanSamplerManager _samplerManager = new();
    private readonly PvrVulkanScreenDrawer _screenDrawer = new();
    private readonly PvrVulkanTextureDrawer _textureDrawer = new();
    private bool _emulateFramebuffer;
}
