namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Vulkan;

internal sealed class PvrVulkanOitRenderer : PvrVulkanRenderer
{
    internal override bool Init()
    {
        PvrRuntime.Log.Notice("OITVulkanRenderer::Init");
        try
        {
            _oitBuffers.Init((int)Viewport.Width, (int)Viewport.Height);
            _textureDrawer.Init(_samplerManager, _oitShaderManager, TextureCache, _oitBuffers);
            _textureDrawer.SetCommandPool(TexCommandPool);

            _screenDrawer.Init(_samplerManager, _oitShaderManager, _oitBuffers, Viewport);
            _screenDrawer.SetCommandPool(TexCommandPool);
            BaseInit(_screenDrawer.GetRenderPass(), 2);
            _emulateFramebuffer = PvrConfiguration.EmulateFramebuffer;
            return true;
        }
        catch (PvrVulkanException error)
        {
            PvrRuntime.Log.Error("Vulkan system error: {0}", error.Message);
            return false;
        }
    }

    internal override void Term()
    {
        try
        {
            PvrRuntime.Log.Debug("OITVulkanRenderer::Term");
            GetContext().WaitIdle();
        }
        catch (PvrVulkanException error)
        {
            PvrRuntime.Log.Error("Vulkan system error: {0}", error.Message);
        }
        finally
        {
            try
            {
                TexCommandPool.Term();
                _screenDrawer.Term();
                _textureDrawer.Term();
                _oitBuffers.Term();
                _oitShaderManager.Term();
                _samplerManager.Term();
                base.Term();
            }
            catch (PvrVulkanException error)
            {
                PvrRuntime.Log.Error("Vulkan system error: {0}", error.Message);
            }
        }
    }

    internal override void Process(TaContext context)
    {
        try
        {
            if (_emulateFramebuffer != PvrConfiguration.EmulateFramebuffer)
            {
                _screenDrawer.EndFrame();
                PvrVulkanContext.Instance.WaitIdle();
                _screenDrawer.Term();
                _screenDrawer.Init(_samplerManager, _oitShaderManager, _oitBuffers, Viewport);
                BaseInit(_screenDrawer.GetRenderPass(), 2);
                _emulateFramebuffer = PvrConfiguration.EmulateFramebuffer;
            }
            else if (context.Rend.IsRtt)
            {
                _screenDrawer.EndFrame();
            }
            base.Process(context);
        }
        catch (PvrVulkanException error) when (error.Result == Silk.NET.Vulkan.Result.ErrorOutOfDeviceMemory)
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
            PvrVulkanOitDrawer drawer;
            if (RendContext!.IsRtt)
            {
                drawer = _textureDrawer;
            }
            else
            {
                Resize((int)RendContext.FramebufferWidth, (int)RendContext.FramebufferHeight);
                drawer = _screenDrawer;
            }

            drawer.SetRendContext(RendContext);
            drawer.Draw(FogTexture, PaletteTexture);
            if (PvrConfiguration.EmulateFramebuffer || RendContext.IsRtt)
                drawer.EndFrame();
            return !RendContext.IsRtt;
        }
        catch (PvrVulkanException error) when (error.Result == Silk.NET.Vulkan.Result.ErrorOutOfDeviceMemory)
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
            return PvrConfiguration.EmulateFramebuffer || FramebufferRendered
                ? PresentFramebuffer() : _screenDrawer.PresentFrame();
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
        _screenDrawer.Init(_samplerManager, _oitShaderManager, _oitBuffers, Viewport);
    }

    private readonly PvrVulkanOitBuffers _oitBuffers = new();
    private readonly PvrVulkanSamplerManager _samplerManager = new();
    private readonly PvrVulkanOitShaders _oitShaderManager = new();
    private readonly PvrVulkanOitScreenDrawer _screenDrawer = new();
    private readonly PvrVulkanOitTextureDrawer _textureDrawer = new();
    private bool _emulateFramebuffer;

    internal static PvrRenderBackend Create() => new PvrVulkanOitRenderer();
}
