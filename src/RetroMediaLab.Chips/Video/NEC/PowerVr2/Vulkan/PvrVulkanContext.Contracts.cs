using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using RenderPass = Silk.NET.Vulkan.RenderPass;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Vulkan;

internal sealed class PvrInvalidVulkanContextException() : InvalidOperationException("Invalid Vulkan context");

internal readonly ref struct PvrVulkanCommandBufferDebugScope
{
#if VK_DEBUG
    private readonly CommandBuffer _commandBuffer;
#endif

    internal PvrVulkanCommandBufferDebugScope(CommandBuffer commandBuffer, string name, ReadOnlySpan<float> color)
    {
#if VK_DEBUG
        _commandBuffer = commandBuffer;
        PvrVk.BeginDebugUtilsLabel(_commandBuffer, name, color);
#endif
    }

    internal void Insert(string name, ReadOnlySpan<float> color)
    {
#if VK_DEBUG
        PvrVk.InsertDebugUtilsLabel(_commandBuffer, name, color);
#endif
    }

    public void Dispose()
    {
#if VK_DEBUG
        PvrVk.EndDebugUtilsLabel(_commandBuffer);
#endif
    }
}

internal sealed unsafe partial class PvrVulkanContext : PvrGraphicsContext, IPvrVulkanFlightManager
{
    internal Instance GetInstance() => _instance!.Handle;
    internal uint GetGraphicsQueueFamilyIndex() => _graphicsQueueIndex;
    internal override void Resize() => _resized = true;
    internal bool IsValid() => _width != 0 && _height != 0;

    internal PhysicalDevice GetPhysicalDevice() => _physicalDevice;
    internal PvrVulkanDevice GetDevice() => _device!;
    internal PipelineCache GetPipelineCache() => _pipelineCache!.Handle;
    internal RenderPass GetRenderPass() => _renderPass!.Handle;
    internal CommandBuffer GetCurrentCommandBuffer() => _commandBuffers[GetCurrentImageIndex()].Handle;
    internal DescriptorPool GetDescriptorPool() => _descriptorPool!.Handle;
    internal Extent2D GetViewPort() => new((uint)PvrRuntime.DisplayWidth, (uint)PvrRuntime.DisplayHeight);
    internal SwapchainKHR GetSwapChain() => _swapChain!.Handle;
    internal uint GetSwapChainSize() => (uint)_imageViews.Count;
    internal int GetCurrentImageIndex() => (int)_currentImage;
    internal bool IsRendering() => _rendering;
    internal ulong GetUniformBufferAlignment() => _uniformBufferAlignment;
    internal ulong GetStorageBufferAlignment() => _storageBufferAlignment;

    internal bool IsFormatSupported(PvrTextureType textureType)
    {
        switch (textureType)
        {
            case PvrTextureType._4444: return _optimalTilingSupported4444;
            case PvrTextureType._565: return _optimalTilingSupported565;
            case PvrTextureType._5551: return _optimalTilingSupported1555;
            default: return true;
        }
    }

    internal override string DriverName => _driverName;
    internal override string DriverVersion => _driverVersion;
    internal override bool IsAmd => _vendorId == VendorAti || _vendorId == VendorAmd;

    internal Format GetDepthFormat() => _depthFormat;
    internal new static PvrVulkanContext Instance => (PvrVulkanContext)PvrGraphicsContext.Instance!;
    internal bool SupportsSamplerAnisotropy() => _samplerAnisotropy;
    internal float GetMaxSamplerAnisotropy() => _samplerAnisotropy ? _maxSamplerAnisotropy : 1f;
    internal bool SupportsDedicatedAllocation() => _dedicatedAllocationSupported;
    internal bool SupportsBufferDeviceAddress() => _bufferDeviceAddressSupported;
    internal PvrVulkanMemoryAllocator GetAllocator() => _allocator;
    internal ulong GetMaxMemoryAllocationSize() => _maxMemoryAllocationSize;
    internal uint GetMaxStorageBufferRange() => _maxStorageBufferRange;
    internal uint GetVendorId() => _vendorId;

    internal void SubmitCommandBuffers(ReadOnlySpan<CommandBuffer> buffers, Fence fence)
    {
        fixed (CommandBuffer* pointer = buffers)
        {
            var submission = new SubmitInfo
            {
                SType = StructureType.SubmitInfo,
                CommandBufferCount = (uint)buffers.Length,
                PCommandBuffers = pointer
            };
            PvrVk.ResultCheck(Api.QueueSubmit(_graphicsQueue, 1, &submission, fence), "QueueSubmit");
        }
    }

    internal override bool HasPerPixel => _fragmentStoresAndAtomics;
    internal bool HasProvokingVertex() => _provokingVertexSupported;

    public void AddToFlight(IPvrVulkanDeletable value) => _inFlightObjects[GetCurrentImageIndex()].Add(value);

#if VK_DEBUG
    internal void SetObjectName<T>(T handle, string name) where T : unmanaged
    {
        if (_device != null)
            PvrVk.SetDebugUtilsObjectName(_device, handle, name);
    }
#endif

    internal const int VendorAmd = 0x1022;
    internal const int VendorAti = 0x1002;
    internal const int VendorArm = 0x13b5;
    internal const int VendorIntel = 0x8086;
    internal const int VendorNvidia = 0x10de;
    internal const int VendorQualcomm = 0x5143;
    internal const int VendorMesa = 0x10005;

    private SurfaceKHR GetSurface() => _surface!.Handle;

    private readonly PvrVulkanMemoryAllocator _allocator = new();
    private bool _rendering;
    private bool _renderDone;
    private uint _width;
    private uint _height;
    private bool _resized;
    private bool _swapOnVSync = true;
    private PvrVulkanOwned<Instance>? _instance;
    private PhysicalDevice _physicalDevice;
    private uint _graphicsQueueIndex;
    private uint _presentQueueIndex;
    private ulong _uniformBufferAlignment;
    private ulong _storageBufferAlignment;
    private ulong _maxMemoryAllocationSize = uint.MaxValue;
    private uint _maxStorageBufferRange = uint.MaxValue;
    private bool _optimalTilingSupported565;
    private bool _optimalTilingSupported1555;
    private bool _optimalTilingSupported4444;
    private bool _fragmentStoresAndAtomics;
    private bool _samplerAnisotropy;
    private float _maxSamplerAnisotropy;
    private bool _dedicatedAllocationSupported;
    private bool _provokingVertexSupported;
    private bool _bufferDeviceAddressSupported;
    private uint _vendorId;
    private int _swapInterval = 1;
    private PvrVulkanDevice? _device;

    private PvrVulkanOwned<SurfaceKHR>? _surface;
    private PvrVulkanOwned<SwapchainKHR>? _swapChain;
    private readonly List<PvrVulkanOwned<ImageView>> _imageViews = [];
    private uint _currentImage;
    private Format _presentFormat = Format.Undefined;
    private Queue _graphicsQueue;
    private Queue _presentQueue;
    private PvrVulkanOwned<DescriptorPool>? _descriptorPool;
    private PvrVulkanOwned<RenderPass>? _renderPass;
    private Format _depthFormat = Format.Undefined;
    private readonly List<PvrVulkanOwned<CommandPool>> _commandPools = [];
    private readonly List<PvrVulkanOwned<CommandBuffer>> _commandBuffers = [];
    private readonly List<PvrVulkanOwned<Framebuffer>> _framebuffers = [];
    private readonly List<PvrVulkanOwned<Fence>> _drawFences = [];
    private readonly List<PvrVulkanOwned<Semaphore>> _renderCompleteSemaphores = [];
    private readonly List<PvrVulkanOwned<Semaphore>> _imageAcquiredSemaphores = [];
    private uint _currentSemaphore;
    private PvrVulkanOwned<PipelineCache>? _pipelineCache;

    private PvrVulkanQuadPipeline? _quadPipelineWithAlpha;
    private PvrVulkanQuadPipeline? _quadPipeline;
    private PvrVulkanQuadPipeline? _quadRotatePipeline;
    private PvrVulkanQuadDrawer? _quadDrawer;
    private PvrVulkanQuadDrawer? _quadRotateDrawer;
    private PvrVulkanShaders? _shaderManager;

    private ImageView _lastFrameView;
    private Extent2D _lastFrameExtent;
    private float _lastFrameAspectRatio;
    private PvrVulkanOverlay? _overlay;
    private readonly List<List<IPvrVulkanDeletable>> _inFlightObjects = [];

    private string _driverName = "";
    private string _driverVersion = "";
    private int _gameSwapInterval = 1;
#if VK_DEBUG
    private PvrVulkanOwned<DebugUtilsMessengerEXT>? _debugUtilsMessenger;
#endif

    internal Vk Api => PvrVk.Api;
    internal KhrSwapchain SwapchainApi => PvrVk.SwapchainApi;
}

internal static class PvrVulkanDepth
{
    internal static Format FindDepthFormat(PhysicalDevice physicalDevice)
    {
        ReadOnlySpan<Format> formats = [Format.D32SfloatS8Uint, Format.D24UnormS8Uint, Format.D16UnormS8Uint];
        ImageTiling tiling = default;
        Format depthFormat = Format.Undefined;
        Vk api = PvrVk.Api;
        foreach (Format format in formats)
        {
            api.GetPhysicalDeviceFormatProperties(physicalDevice, format, out FormatProperties properties);
            if ((properties.OptimalTilingFeatures & FormatFeatureFlags.DepthStencilAttachmentBit) != 0)
            {
                tiling = ImageTiling.Optimal;
                depthFormat = format;
                break;
            }
        }
        if (depthFormat == Format.Undefined)
        {
            foreach (Format format in formats)
            {
                api.GetPhysicalDeviceFormatProperties(physicalDevice, format, out FormatProperties properties);
                if ((properties.LinearTilingFeatures & FormatFeatureFlags.DepthStencilAttachmentBit) != 0)
                {
                    tiling = ImageTiling.Linear;
                    depthFormat = format;
                    break;
                }
            }
            if (depthFormat == Format.Undefined)
            {
                PvrRuntime.Log.Error("No supported depth/stencil format found");
                return depthFormat;
            }
        }
        PvrRuntime.Log.Notice("Using depth format {0} tiling {1}", depthFormat, tiling);
        return depthFormat;
    }
}
