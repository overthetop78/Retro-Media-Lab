using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using VkRenderPass = Silk.NET.Vulkan.RenderPass;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Vulkan;

internal sealed unsafe partial class PvrVulkanContext
{
#if VK_DEBUG
    private static uint DebugUtilsMessengerCallback(DebugUtilsMessageSeverityFlagsEXT severity, DebugUtilsMessageTypeFlagsEXT types,
        DebugUtilsMessengerCallbackDataEXT* data, void* userData)
    {
        string message = $"{severity}: {types}: ";
        if (data->PMessageIdName != null)
            message += "messageIDName=" + Marshal.PtrToStringUTF8((nint)data->PMessageIdName) + " ";
        if (data->PMessage != null)
            message += Marshal.PtrToStringUTF8((nint)data->PMessage);
        switch (severity)
        {
            case DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt: PvrRuntime.Log.Debug("{0}", message); break;
            case DebugUtilsMessageSeverityFlagsEXT.InfoBitExt: PvrRuntime.Log.Info("{0}", message); break;
            case DebugUtilsMessageSeverityFlagsEXT.WarningBitExt: PvrRuntime.Log.Warn("{0}", message); break;
            default: PvrRuntime.Log.Error("{0}", message); break;
        }
        return 0;
    }

    private static void CheckImGuiResult(Result result)
    {
        if (result != Result.Success)
            PvrRuntime.Log.Warn("ImGui Vulkan error {0}", (int)result);
    }
#endif

    private bool InitInstance(ReadOnlySpan<string> extensions)
    {
        try
        {
            if (!PvrVk.LoadInstanceProcAddress())
            {
                PvrRuntime.Log.Error("Vulkan entry point vkGetInstanceProcAddr not found");
                return false;
            }

            bool vulkan11 = PvrVk.HasEnumerateInstanceVersion && PvrVk.EnumerateInstanceVersion() >= Vk.Version11;
            var instanceExtensions = new List<string>(extensions.ToArray());
            var layers = new List<string>();
#if VK_DEBUG
            instanceExtensions.Add("VK_EXT_debug_utils");
            instanceExtensions.Add("VK_EXT_debug_report");
            layers.Add("VK_LAYER_KHRONOS_validation");
#endif
            _instance = PvrVk.CreateInstanceUnique("SegaEmu", 1, "SegaEmu", 1,
                vulkan11 ? Vk.Version11 : Vk.Version10, layers, instanceExtensions);
            PvrVk.InitInstance(_instance.Handle);
#if VK_DEBUG
            _debugUtilsMessenger = PvrVk.CreateDebugUtilsMessengerUnique(_instance.Handle,
                DebugUtilsMessageSeverityFlagsEXT.InfoBitExt | DebugUtilsMessageSeverityFlagsEXT.WarningBitExt
                    | DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt,
                DebugUtilsMessageTypeFlagsEXT.GeneralBitExt | DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt
                    | DebugUtilsMessageTypeFlagsEXT.ValidationBitExt,
                DebugUtilsMessengerCallback);
#endif

            PhysicalDevice[] devices = PvrVk.EnumeratePhysicalDevices(_instance.Handle);
            if (devices.Length == 0)
            {
                PvrRuntime.Log.Error("Vulkan error: no physical devices found");
                return false;
            }

            devices = devices.OrderByDescending(device => SupportsOptimal(device, Format.R5G6B5UnormPack16)).ToArray();
            devices = devices.OrderByDescending(device => SupportsOptimal(device, Format.R5G5B5A1UnormPack16)).ToArray();
            devices = devices.OrderByDescending(device => SupportsOptimal(device, Format.R4G4B4A4UnormPack16)).ToArray();
            devices = devices.OrderByDescending(device =>
            {
                Api.GetPhysicalDeviceFeatures(device, out PhysicalDeviceFeatures features);
                return (bool)features.FragmentStoresAndAtomics;
            }).ToArray();
            devices = devices.OrderByDescending(device =>
            {
                Api.GetPhysicalDeviceProperties(device, out PhysicalDeviceProperties properties);
                return properties.DeviceType == PhysicalDeviceType.DiscreteGpu;
            }).ToArray();

            _physicalDevice = devices[0];
            Api.GetPhysicalDeviceProperties(_physicalDevice, out PhysicalDeviceProperties properties);
            if (vulkan11 && properties.ApiVersion >= Vk.Version11)
            {
                var maintenance = new PhysicalDeviceMaintenance3Properties { SType = StructureType.PhysicalDeviceMaintenance3Properties };
                var properties2 = new PhysicalDeviceProperties2 { SType = StructureType.PhysicalDeviceProperties2, PNext = &maintenance };
                Api.GetPhysicalDeviceProperties2(_physicalDevice, &properties2);
                properties = properties2.Properties;
                _maxMemoryAllocationSize = maintenance.MaxMemoryAllocationSize;
                if (_maxMemoryAllocationSize == 0)
                    _maxMemoryAllocationSize = uint.MaxValue;
            }

            _uniformBufferAlignment = properties.Limits.MinUniformBufferOffsetAlignment;
            _storageBufferAlignment = properties.Limits.MinStorageBufferOffsetAlignment;
            _maxStorageBufferRange = properties.Limits.MaxStorageBufferRange;
            _maxSamplerAnisotropy = properties.Limits.MaxSamplerAnisotropy;
            _vendorId = properties.VendorID;
            PvrRuntime.Log.Notice("Vulkan API {0}. Device {1}", vulkan11 ? "1.1" : "1.0", Marshal.PtrToStringUTF8((nint)properties.DeviceName));
            _optimalTilingSupported1555 = SupportsOptimal(_physicalDevice, Format.R5G5B5A1UnormPack16);
            if (!_optimalTilingSupported1555)
                PvrRuntime.Log.Notice("eR5G5B5A1UnormPack16 not supported for optimal tiling");
            _optimalTilingSupported565 = SupportsOptimal(_physicalDevice, Format.R5G6B5UnormPack16);
            if (!_optimalTilingSupported565)
                PvrRuntime.Log.Notice("eR5G6B5UnormPack16 not supported for optimal tiling");
            _optimalTilingSupported4444 = SupportsOptimal(_physicalDevice, Format.R4G4B4A4UnormPack16);
            if (!_optimalTilingSupported4444)
                PvrRuntime.Log.Notice("eR4G4B4A4UnormPack16 not supported for optimal tiling");
            PvrVulkanShaderCompiler.Init();
            return true;
        }
        catch (PvrVulkanException error)
        {
            PvrRuntime.Log.Error("Vulkan error: {0}", error.Message);
        }
        catch (Exception error)
        {
            PvrRuntime.Log.Error("Vulkan instance init failed: {0}", error.Message);
        }
        return false;
    }

    private bool SupportsOptimal(PhysicalDevice device, Format format)
    {
        Api.GetPhysicalDeviceFormatProperties(device, format, out FormatProperties props);
        return (props.OptimalTilingFeatures & FormatFeatureFlags.SampledImageBit) != 0
            && (props.OptimalTilingFeatures & FormatFeatureFlags.BlitDstBit) != 0
            && (props.OptimalTilingFeatures & FormatFeatureFlags.BlitSrcBit) != 0;
    }

    private void InitImgui()
    {
        if (ImGuiDriver is not PvrVulkanDriver vkDriver)
        {
            ImGuiDriver?.Dispose();
            ImGuiDriver = new PvrVulkanDriver();
        }
        else
        {
            vkDriver.Reset();
        }

        var info = new PvrImGuiVulkanInitInfo
        {
            Instance = _instance!.Handle, PhysicalDevice = _physicalDevice, Device = _device!,
            QueueFamily = _graphicsQueueIndex, Queue = _graphicsQueue, PipelineCache = _pipelineCache!.Handle,
            DescriptorPool = _descriptorPool!.Handle, RenderPass = _renderPass!.Handle,
            MinImageCount = 2, ImageCount = GetSwapChainSize()
        };
#if VK_DEBUG
        info.CheckVkResult = CheckImGuiResult;
#endif
        PvrImGuiVulkan.LoadFunctions(0, name => PvrVk.GetInstanceProcAddr(GetInstance(), name));
        if (!PvrImGuiVulkan.Init(info))
            throw new PvrRendererException("Vulkan ImGui initialization failed");
    }

    private bool InitDevice()
    {
        if (_instance == null)
            return false;
        try
        {
            Api.GetPhysicalDeviceProperties(_physicalDevice, out PhysicalDeviceProperties props);
            QueueFamilyProperties[] queues = PvrVk.GetQueueFamilyProperties(_physicalDevice);
#if VK_DEBUG
            foreach (QueueFamilyProperties queue in queues)
                PvrRuntime.Log.Info("Queue Family: count {0} flags {1} minImgGranularity {2} x {3} x {4}",
                    queue.QueueCount, queue.QueueFlags, queue.MinImageTransferGranularity.Width,
                    queue.MinImageTransferGranularity.Height, queue.MinImageTransferGranularity.Depth);
#endif
            uint count = (uint)queues.Length;
            int firstGraphics = Array.FindIndex(queues, queue => (queue.QueueFlags & QueueFlags.GraphicsBit) != 0);
            _graphicsQueueIndex = firstGraphics < 0 ? count : (uint)firstGraphics;
            _presentQueueIndex = PvrVk.GetSurfaceSupport(_physicalDevice, _graphicsQueueIndex, GetSurface()) ? _graphicsQueueIndex : count;
            if (_presentQueueIndex == count)
            {
                for (uint i = 0; i < count; i++)
                {
                    if ((queues[i].QueueFlags & QueueFlags.GraphicsBit) != 0 && PvrVk.GetSurfaceSupport(_physicalDevice, i, GetSurface()))
                    {
                        _graphicsQueueIndex = _presentQueueIndex = i;
                        break;
                    }
                }
                if (_presentQueueIndex == count)
                {
                    PvrRuntime.Log.Debug("Using separate Graphics and Present queue families");
                    for (uint i = 0; i < count; i++)
                    {
                        if (PvrVk.GetSurfaceSupport(_physicalDevice, i, GetSurface()))
                        {
                            _presentQueueIndex = i;
                            break;
                        }
                    }
                }
            }
            if (_graphicsQueueIndex == count || _presentQueueIndex == count)
            {
                PvrRuntime.Log.Error("Could not find a queue for graphics or present");
                return false;
            }
            PvrRuntime.Log.Debug(_graphicsQueueIndex == _presentQueueIndex ? "Using Graphics+Present queue family"
                : "Using distinct Graphics and Present queue families");

            HashSet<string> supportedExtensions = [.. PvrVk.EnumerateDeviceExtensions(_physicalDevice)];
            List<string> enabledExtensions = [];
            bool TryAddDeviceExtension(string name)
            {
                if (supportedExtensions.Contains(name))
                {
                    enabledExtensions.Add(name);
                    PvrRuntime.Log.Notice("Device extension enabled: {0}", name);
                    return true;
                }
                PvrRuntime.Log.Notice("Device extension unavailable: {0}", name);
                return false;
            }

            TryAddDeviceExtension("VK_KHR_swapchain");
#if VK_ENABLE_BETA_EXTENSIONS
            TryAddDeviceExtension("VK_KHR_portability_subset");
#endif
#if VK_DEBUG
            TryAddDeviceExtension("VK_EXT_debug_marker");
#endif
            if (props.ApiVersion >= Vk.Version11)
                _dedicatedAllocationSupported = true;
            else if (TryAddDeviceExtension("VK_KHR_get_memory_requirements2"))
                _dedicatedAllocationSupported = TryAddDeviceExtension("VK_KHR_dedicated_allocation");

            bool properties2 = props.ApiVersion >= Vk.Version11 || TryAddDeviceExtension("VK_KHR_get_physical_device_properties2");
            if (properties2)
            {
                _provokingVertexSupported = TryAddDeviceExtension("VK_EXT_provoking_vertex");
                _bufferDeviceAddressSupported = TryAddDeviceExtension("VK_KHR_buffer_device_address");
            }
            bool googleTiming = false;
#if SWAPPY
            googleTiming = TryAddDeviceExtension("VK_GOOGLE_display_timing");
            if (googleTiming)
                PvrRuntime.Log.Notice("VK_GOOGLE_display_timing supported");
#endif
            var address = new PhysicalDeviceBufferDeviceAddressFeatures { SType = StructureType.PhysicalDeviceBufferDeviceAddressFeatures };
            var provoking = new PhysicalDeviceProvokingVertexFeaturesEXT
            {
                SType = StructureType.PhysicalDeviceProvokingVertexFeaturesExt, PNext = _bufferDeviceAddressSupported ? &address : null
            };
            var features = new PhysicalDeviceFeatures2
            {
                SType = StructureType.PhysicalDeviceFeatures2,
                PNext = _provokingVertexSupported ? (void*)&provoking : _bufferDeviceAddressSupported ? &address : null
            };
            if (properties2 && features.PNext != null)
                Api.GetPhysicalDeviceFeatures2(_physicalDevice, &features);
            else
                Api.GetPhysicalDeviceFeatures(_physicalDevice, &features.Features);

            if (_provokingVertexSupported)
            {
                _provokingVertexSupported &= provoking.ProvokingVertexLast;
                PvrRuntime.Log.Notice("provokingVertexSupported {0}", _provokingVertexSupported);
            }
            if (_bufferDeviceAddressSupported)
            {
                _bufferDeviceAddressSupported &= address.BufferDeviceAddress;
                PvrRuntime.Log.Notice("bufferDeviceAddressSupported {0}", _bufferDeviceAddressSupported);
            }
            _samplerAnisotropy = features.Features.SamplerAnisotropy;
            _fragmentStoresAndAtomics = features.Features.FragmentStoresAndAtomics;
            if (!_fragmentStoresAndAtomics)
                PvrRuntime.Log.Notice("Fragment stores & atomic not supported: no per-pixel sorting");

            float priority = 1f;
            var queueInfo = new DeviceQueueCreateInfo
            {
                SType = StructureType.DeviceQueueCreateInfo, QueueFamilyIndex = _graphicsQueueIndex,
                QueueCount = 1, PQueuePriorities = &priority
            };
            _device = PvrVk.CreateDeviceUnique(_physicalDevice, queueInfo, enabledExtensions,
                properties2 ? &features : null, properties2 ? null : &features.Features);
            PvrVk.InitDevice(_device);
            _graphicsQueue = _device.GetQueue(_graphicsQueueIndex, 0);
            _presentQueue = _device.GetQueue(_presentQueueIndex, 0);
            PvrSwappyVk.SetQueueFamilyIndex(googleTiming, _device, _presentQueue, _presentQueueIndex);

            var pools = new DescriptorPoolSize[]
            {
                new(DescriptorType.Sampler, 2), new(DescriptorType.CombinedImageSampler, 40000),
                new(DescriptorType.SampledImage, 2), new(DescriptorType.StorageImage, 12),
                new(DescriptorType.UniformTexelBuffer, 2), new(DescriptorType.StorageTexelBuffer, 2),
                new(DescriptorType.UniformBuffer, 80000), new(DescriptorType.StorageBuffer, 100),
                new(DescriptorType.UniformBufferDynamic, 2), new(DescriptorType.StorageBufferDynamic, 2),
                new(DescriptorType.InputAttachment, 100)
            };
            _descriptorPool = _device.CreateDescriptorPoolUnique(DescriptorPoolCreateFlags.FreeDescriptorSetBit, 40000, pools);
            const string section = "validate";
            const string key = "vkPipelineCache";
            string path = PvrRuntime.GetShaderCachePath("vulkan_pipeline.cache");
            if (PvrConfiguration.LoadBool(section, key, false))
            {
                File.Delete(path);
                PvrConfiguration.SaveBool(section, key, false);
            }
            if (!File.Exists(path))
            {
                _pipelineCache = _device.CreatePipelineCacheUnique([]);
            }
            else
            {
                byte[] bytes = File.ReadAllBytes(path);
                PvrConfiguration.SaveBool(section, key, true);
                try
                {
                    _pipelineCache = _device.CreatePipelineCacheUnique(bytes);
                    PvrRuntime.Log.Info("Vulkan pipeline cache loaded from {0}: {1} bytes", path, bytes.Length);
                }
                catch (PvrVulkanException error)
                {
                    PvrRuntime.Log.Warn("Error loading pipeline cache: {0}", error.Message);
                    _pipelineCache = _device.CreatePipelineCacheUnique([]);
                    File.Delete(path);
                }
                finally
                {
                    PvrConfiguration.SaveBool(section, key, false);
                }
            }
            _allocator.Init(_physicalDevice, _device, _instance.Handle);

            _shaderManager = new PvrVulkanShaders();
            _quadPipeline = new PvrVulkanQuadPipeline(true, false);
            _quadPipelineWithAlpha = new PvrVulkanQuadPipeline(false, false);
            _quadDrawer = new PvrVulkanQuadDrawer();
            _quadRotatePipeline = new PvrVulkanQuadPipeline(true, true);
            _quadRotateDrawer = new PvrVulkanQuadDrawer();

            Api.GetPhysicalDeviceProperties(_physicalDevice, out props);
            _driverName = Marshal.PtrToStringUTF8((nint)props.DeviceName) ?? "";
            _driverVersion = $"{props.DriverVersion >> 22}.{(props.DriverVersion >> 12) & 0x3ff}.{props.DriverVersion & 0xfff}";
            CreateSwapChain();
            return true;
        }
        catch (PvrVulkanException error)
        {
            PvrRuntime.Log.Error("Vulkan error: {0}", error.Message);
        }
        catch (PvrInvalidVulkanContextException)
        {
        }
        catch (Exception)
        {
            PvrRuntime.Log.Error("Unknown error");
        }
        return false;
    }

    private void CreateSwapChain()
    {
        try
        {
            _device!.WaitIdle();
            if (_drawFences.Count != 0)
            {
                Fence[] fences = _drawFences.ConvertAll(fence => fence.Handle).ToArray();
                Result result = _device.WaitForFences(fences, true, ulong.MaxValue);
                if (result != Result.Success)
                    PvrRuntime.Log.Warn("VulkanContext::CreateSwapChain: waitForFences failed {0}", (int)result);
            }
            ClearInFlight();
            _overlay!.Term();
            ClearOwned(_framebuffers);
            ClearOwned(_drawFences);
            ClearOwned(_imageAcquiredSemaphores);
            ClearOwned(_renderCompleteSemaphores);
            ClearOwned(_commandBuffers);
            ClearOwned(_commandPools);
            ClearOwned(_imageViews);
            _rendering = false;
            _renderDone = false;

            SurfaceFormatKHR target = PvrVk.GetSurfaceFormats(_physicalDevice, GetSurface())
                .OrderByDescending(format => !PvrVk.IsSrgbFormat(format.Format))
                .OrderByDescending(format => format.ColorSpace == ColorSpaceKHR.SpaceSrgbNonlinearKhr).First();
            _presentFormat = target.Format;

            int tries = 0;
            do
            {
                SurfaceCapabilitiesKHR capabilities = PvrVk.GetSurfaceCapabilities(_physicalDevice, GetSurface());
                PvrRuntime.Log.Debug("Surface capabilities: {0} x {1}, {2}, image count: {3} - {4}",
                    capabilities.CurrentExtent.Width, capabilities.CurrentExtent.Height, capabilities.CurrentTransform,
                    capabilities.MinImageCount, capabilities.MaxImageCount);
                Extent2D extent = capabilities.CurrentExtent.Width == uint.MaxValue
                    ? new Extent2D(
                        Math.Min(Math.Max((uint)PvrRuntime.DisplayWidth, capabilities.MinImageExtent.Width), capabilities.MaxImageExtent.Width),
                        Math.Min(Math.Max((uint)PvrRuntime.DisplayHeight, capabilities.MinImageExtent.Height), capabilities.MaxImageExtent.Height))
                    : capabilities.CurrentExtent;
                SetWindowSize(extent.Width, extent.Height);
                _resized = false;
                if (!IsValid())
                    throw new PvrInvalidVulkanContextException();

                PresentModeKHR presentMode = PresentModeKHR.FifoKhr;
                bool mailboxSupported = false;
                foreach (PresentModeKHR mode in PvrVk.GetSurfacePresentModes(_physicalDevice, GetSurface()))
                {
                    if (mode == PresentModeKHR.MailboxKhr)
                        mailboxSupported = true;
                    if (_swapOnVSync && mode == PresentModeKHR.MailboxKhr && _vendorId != VendorAti && _vendorId != VendorAmd)
                    {
                        PvrRuntime.Log.Info("Using mailbox present mode");
                        presentMode = PresentModeKHR.MailboxKhr;
                        break;
                    }
                    if (!_swapOnVSync && mode == PresentModeKHR.ImmediateKhr)
                    {
                        PvrRuntime.Log.Info("Using immediate present mode");
                        presentMode = PresentModeKHR.ImmediateKhr;
                        break;
                    }
                }
                if (!_swapOnVSync && presentMode == PresentModeKHR.FifoKhr && mailboxSupported)
                {
                    PvrRuntime.Log.Info("Using mailbox present mode");
                    presentMode = PresentModeKHR.MailboxKhr;
                }
#if !SWAPPY
                if (_swapOnVSync && PvrConfiguration.DupeFrames && PvrRuntime.DisplayRefreshRate > 60f)
                    _swapInterval = (int)(PvrRuntime.DisplayRefreshRate / 60f);
                else
#endif
                    _swapInterval = 1;

                SurfaceTransformFlagsKHR transform =
                    (capabilities.SupportedTransforms & SurfaceTransformFlagsKHR.IdentityBitKhr) != 0
                        ? SurfaceTransformFlagsKHR.IdentityBitKhr : capabilities.CurrentTransform;
                PvrRuntime.Log.Info("Surface capabilities: minImageCount {0} maxImageCount {1} swapInterval {2}",
                    capabilities.MinImageCount, capabilities.MaxImageCount, _swapInterval);
#if SWAPPY
                uint imageCount = 3;
#else
                uint imageCount = 4;
#endif
                imageCount = Math.Max(imageCount * (uint)_swapInterval, capabilities.MinImageCount);
                if (capabilities.MaxImageCount != 0)
                    imageCount = Math.Min(imageCount, capabilities.MaxImageCount);
                ImageUsageFlags usage = ImageUsageFlags.ColorAttachmentBit;
#if TEST_AUTOMATION
                usage |= ImageUsageFlags.TransferSrcBit;
#endif
                var info = new SwapchainCreateInfoKHR
                {
                    SType = StructureType.SwapchainCreateInfoKhr, Surface = GetSurface(), MinImageCount = imageCount,
                    ImageFormat = target.Format, ImageColorSpace = target.ColorSpace, ImageExtent = extent,
                    ImageArrayLayers = 1, ImageUsage = usage, ImageSharingMode = SharingMode.Exclusive, PreTransform = transform,
                    CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr, PresentMode = presentMode, Clipped = true
                };
                uint* indices = stackalloc uint[2] { _graphicsQueueIndex, _presentQueueIndex };
                if (_graphicsQueueIndex != _presentQueueIndex)
                {
                    info.ImageSharingMode = SharingMode.Concurrent;
                    info.QueueFamilyIndexCount = 2;
                    info.PQueueFamilyIndices = indices;
                }

                PvrSwappyVk.DetachSwapchain();
                Reset(ref _swapChain);
                try
                {
                    _swapChain = _device.CreateSwapchainUnique(info);
                }
                catch (PvrVulkanException error)
                {
                    PvrRuntime.Log.Debug("createSwapchainKHRUnique failed: {0}", error.Message);
                    if (++tries > 10)
                        throw new PvrInvalidVulkanContextException();
                }
            } while (_swapChain == null);

            PvrSwappyVk.Init(_physicalDevice, _device, _swapChain.Handle);
            Image[] images = _device.GetSwapchainImages(_swapChain.Handle);
            foreach (Image image in images)
            {
                var viewInfo = new ImageViewCreateInfo
                {
                    SType = StructureType.ImageViewCreateInfo, Image = image, ViewType = ImageViewType.Type2D, Format = _presentFormat,
                    Components = new ComponentMapping(ComponentSwizzle.R, ComponentSwizzle.G, ComponentSwizzle.B, ComponentSwizzle.A),
                    SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, 1)
                };
                _imageViews.Add(_device.CreateImageViewUnique(viewInfo));
                _commandPools.Add(_device.CreateCommandPoolUnique(CommandPoolCreateFlags.TransientBit, _graphicsQueueIndex));
                _commandBuffers.Add(_device.AllocateCommandBufferUnique(_commandPools[^1].Handle, CommandBufferLevel.Primary));
            }

            _depthFormat = PvrVulkanDepth.FindDepthFormat(_physicalDevice);
            if (_depthFormat == Format.Undefined)
            {
                SetWindowSize(0, 0);
                throw new PvrInvalidVulkanContextException();
            }

            Reset(ref _renderPass);
            _renderPass = CreatePresentationRenderPass(_presentFormat, ImageLayout.PresentSrcKhr);
            foreach (var view in _imageViews)
            {
                _framebuffers.Add(_device.CreateFramebufferUnique(_renderPass.Handle, [view.Handle], _width, _height, 1));
                _drawFences.Add(_device.CreateFenceUnique(FenceCreateFlags.SignaledBit));
            }
            for (int i = 0; i < _imageViews.Count; i++)
                _renderCompleteSemaphores.Add(_device.CreateSemaphoreUnique());
            for (int i = 0; i < _imageViews.Count + 1; i++)
                _imageAcquiredSemaphores.Add(_device.CreateSemaphoreUnique());
            for (int i = 0; i < _imageViews.Count; i++)
                _inFlightObjects.Add([]);
            _currentSemaphore = 0;
            _quadPipeline!.Init(_shaderManager!, _renderPass.Handle, 0);
            _quadPipelineWithAlpha!.Init(_shaderManager!, _renderPass.Handle, 0);
            _quadDrawer!.Init(_quadPipeline);
            _quadRotatePipeline!.Init(_shaderManager!, _renderPass.Handle, 0);
            _quadRotateDrawer!.Init(_quadRotatePipeline);
            _overlay.Init(_quadPipelineWithAlpha);
            InitImgui();

            _currentImage = GetSwapChainSize() - 1;
            PvrVulkanRenderer.ReInitOsdForActiveRenderer();
            PvrRuntime.Log.Info("Vulkan swap chain created: {0} x {1}, swap chain size {2}", _width, _height, _imageViews.Count);
        }
        catch (PvrVulkanException error)
        {
            PvrRuntime.Log.Error("Vulkan error: {0}", error.Message);
            SetWindowSize(0, 0);
            throw new PvrInvalidVulkanContextException();
        }
    }

    private PvrVulkanOwned<VkRenderPass> CreatePresentationRenderPass(Format format, ImageLayout finalLayout)
    {
        var attachment = new AttachmentDescription
        {
            Format = format, Samples = SampleCountFlags.Count1Bit, LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.Store, StencilLoadOp = AttachmentLoadOp.DontCare,
            StencilStoreOp = AttachmentStoreOp.DontCare, InitialLayout = ImageLayout.Undefined, FinalLayout = finalLayout
        };
        var color = new AttachmentReference(0, ImageLayout.ColorAttachmentOptimal);
        var subpass = new SubpassDescription
        {
            PipelineBindPoint = PipelineBindPoint.Graphics, ColorAttachmentCount = 1, PColorAttachments = &color
        };
        var info = new RenderPassCreateInfo
        {
            SType = StructureType.RenderPassCreateInfo, AttachmentCount = 1, PAttachments = &attachment,
            SubpassCount = 1, PSubpasses = &subpass
        };
        return _device!.CreateRenderPassUnique(info);
    }

    private bool Init()
    {
        List<string> extensions = ["VK_KHR_surface"];
#if USE_SDL
        if (!PvrSdl.RecreateWindow(PvrSdl.WindowVulkan))
            return false;
        extensions.AddRange(PvrSdl.GetVulkanInstanceExtensions(Window));
#else
        PvrRuntime.CreateMainWindow();
        extensions.Add("VK_KHR_win32_surface");
#endif
        if (!InitInstance(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(extensions)))
        {
            Term();
            return false;
        }
#if USE_SDL
        if (!PvrSdl.CreateVulkanSurface(Window, _instance!.Handle, out SurfaceKHR surface))
        {
            Term();
            return false;
        }
        _surface = new PvrVulkanOwned<SurfaceKHR>(surface, value => PvrVk.DestroySurface(_instance.Handle, value));
        PvrSdl.UpdateDisplayMetrics(Window, PvrSdl.WindowVulkan);
#else
        _surface = PvrVk.CreateWin32SurfaceUnique(_instance!.Handle, PvrWin32.GetModuleHandle(null), Window);
#endif
        _overlay = new PvrVulkanOverlay();
        if (!InitDevice())
        {
            Term();
            return false;
        }
        return true;
    }

    internal bool RecreateSwapChainIfNeeded()
    {
        if (_resized || HasSurfaceDimensionChanged())
        {
            CreateSwapChain();
            _lastFrameView = default;
            return true;
        }
        return false;
    }

    internal void NewFrame()
    {
        RecreateSwapChainIfNeeded();
        if (!IsValid())
            throw new PvrInvalidVulkanContextException();
        Result result = _device!.AcquireNextImage(_swapChain!.Handle, ulong.MaxValue,
            _imageAcquiredSemaphores[(int)_currentSemaphore].Handle, default, out _currentImage);
        if (result != Result.Success)
            throw new PvrInvalidVulkanContextException();
        try
        {
            result = _device.WaitForFences([_drawFences[(int)_currentImage].Handle], true, ulong.MaxValue);
            if (result != Result.Success)
                throw new PvrInvalidVulkanContextException();
        }
        catch (PvrVulkanException error)
        {
            PvrRuntime.Log.Warn("vk:SystemError: {0}", error.Message);
            throw new PvrRendererException("Vulkan system error");
        }
        _device.ResetCommandPool(_commandPools[(int)_currentImage].Handle, CommandPoolResetFlags.ReleaseResourcesBit);
        ClearOwned(_inFlightObjects[(int)_currentImage]);
        PvrVk.Begin(GetCurrentCommandBuffer(), CommandBufferUsageFlags.OneTimeSubmitBit);
        Debug.Assert(!_rendering);
        _rendering = true;
    }

    internal void BeginRenderPass()
    {
        if (!IsValid())
            return;
        ClearValue* clear = stackalloc ClearValue[2];
        clear[0].Color = PvrVulkanUtilities.GetBorderColor();
        clear[1].DepthStencil = new ClearDepthStencilValue(0f, 0);
        var info = new RenderPassBeginInfo
        {
            SType = StructureType.RenderPassBeginInfo, RenderPass = _renderPass!.Handle,
            Framebuffer = _framebuffers[(int)_currentImage].Handle,
            RenderArea = new Rect2D(default, new Extent2D(_width, _height)), ClearValueCount = 2, PClearValues = clear
        };
        Api.CmdBeginRenderPass(GetCurrentCommandBuffer(), &info, SubpassContents.Inline);
    }

    internal void EndFrame(CommandBuffer overlayCommandBuffer = default)
    {
        if (!IsValid())
            return;
        CommandBuffer buffer = GetCurrentCommandBuffer();
        Api.CmdEndRenderPass(buffer);
        PvrVk.End(buffer);
        PipelineStageFlags waitStage = PipelineStageFlags.ColorAttachmentOutputBit;
        Semaphore acquired = _imageAcquiredSemaphores[(int)_currentSemaphore].Handle;
        Semaphore complete = _renderCompleteSemaphores[(int)_currentImage].Handle;
        CommandBuffer* buffers = stackalloc CommandBuffer[2];
        uint count = 0;
        if (overlayCommandBuffer.Handle != 0)
            buffers[count++] = overlayCommandBuffer;
        buffers[count++] = buffer;
        var submission = new SubmitInfo
        {
            SType = StructureType.SubmitInfo, WaitSemaphoreCount = 1, PWaitSemaphores = &acquired,
            PWaitDstStageMask = &waitStage, CommandBufferCount = count, PCommandBuffers = buffers,
            SignalSemaphoreCount = 1, PSignalSemaphores = &complete
        };
        Fence fence = _drawFences[(int)_currentImage].Handle;
        _device!.ResetFences([fence]);
        PvrVk.ResultCheck(Api.QueueSubmit(_graphicsQueue, 1, &submission, fence), "QueueSubmit");
        _currentSemaphore = (_currentSemaphore + 1) % (uint)_imageAcquiredSemaphores.Count;
        Debug.Assert(_rendering);
        _rendering = false;
        _renderDone = true;
    }

    internal void Present()
    {
        if (_renderDone)
        {
            try
            {
                DoSwapAutomation();
                PresentCurrentImage(useSwappy: true);
#if !SWAPPY
                if (_lastFrameView.Handle != 0 && IsValid() && !PvrRuntime.GuiIsOpen && _swapOnVSync && PvrConfiguration.DupeFrames)
                {
                    int interval = _swapInterval * _gameSwapInterval;
                    for (int i = 1; i < interval && !_resized; i++)
                    {
                        PresentFrame(default, _lastFrameView, _lastFrameExtent, _lastFrameAspectRatio);
                        PresentCurrentImage(useSwappy: false);
                    }
                }
#endif
            }
            catch (PvrVulkanException error)
            {
                PvrRuntime.Log.Info("vk::SystemError {0}", error.Message);
                _resized = true;
                _width = _height = 0;
            }
            _renderDone = false;
        }
        if (_swapOnVSync == (PvrRuntime.FastForwardMode || !PvrConfiguration.VSync))
        {
            _swapOnVSync = !PvrRuntime.FastForwardMode && PvrConfiguration.VSync;
            _resized = true;
        }
        if (_resized)
        {
            try
            {
                CreateSwapChain();
                _lastFrameView = default;
            }
            catch (PvrInvalidVulkanContextException)
            {
            }
        }
    }

    private void PresentCurrentImage(bool useSwappy)
    {
        Semaphore complete = _renderCompleteSemaphores[(int)_currentImage].Handle;
        SwapchainKHR swapChain = _swapChain!.Handle;
        uint index = _currentImage;
        var info = new PresentInfoKHR
        {
            SType = StructureType.PresentInfoKhr, WaitSemaphoreCount = 1, PWaitSemaphores = &complete,
            SwapchainCount = 1, PSwapchains = &swapChain, PImageIndices = &index
        };
        Result result = useSwappy ? PvrSwappyVk.Present(_presentQueue, info) : SwapchainApi.QueuePresent(_presentQueue, info);
        if (result != Result.SuboptimalKhr)
            PvrVk.ResultCheck(result, "QueuePresent");
    }

    private void DrawFrame(ImageView imageView, in Extent2D extent, float aspectRatio)
    {
        PvrVulkanQuadVertex* vertices = stackalloc PvrVulkanQuadVertex[4]
        {
            new(-1, -1, 0, 0, 0), new(1, -1, 0, 1, 0),
            new(-1, 1, 0, 0, 1), new(1, 1, 0, 1, 1)
        };
        PvrTransformMatrix.GetVideoShift(out float shiftX, out float shiftY);
        vertices[0].X = vertices[2].X = -1f + shiftX * 2f / extent.Width;
        vertices[1].X = vertices[3].X = vertices[0].X + 2;
        vertices[0].Y = vertices[1].Y = -1f + shiftY * 2f / extent.Height;
        vertices[2].Y = vertices[3].Y = vertices[0].Y + 2;
        CommandBuffer buffer = GetCurrentCommandBuffer();
        using var debug = new PvrVulkanCommandBufferDebugScope(buffer, "DrawFrame", [1f, 1f, 1f, 1f]);
        if (PvrConfiguration.Rotate90)
            _quadRotatePipeline!.BindPipeline(buffer);
        else
            _quadPipeline!.BindPipeline(buffer);

        int dx = 0, dy = 0;
        PvrTransformMatrix.GetWindowboxDimensions((int)_width, (int)_height, aspectRatio, ref dx, ref dy, PvrConfiguration.Rotate90);
        var viewport = new Viewport(dx, dy, _width - dx * 2, _height - dy * 2, 0f, 1f);
        var scissor = new Rect2D(new Offset2D(dx, dy), new Extent2D((uint)(_width - dx * 2), (uint)(_height - dy * 2)));
        Api.CmdSetViewport(buffer, 0, 1, &viewport);
        Api.CmdSetScissor(buffer, 0, 1, &scissor);
        if (PvrConfiguration.Rotate90)
            _quadRotateDrawer!.Draw(buffer, imageView, vertices, !PvrConfiguration.LinearInterpolation);
        else
            _quadDrawer!.Draw(buffer, imageView, vertices, !PvrConfiguration.LinearInterpolation);
    }

    internal void WaitIdle()
    {
        try
        {
            PvrVk.ResultCheck(Api.QueueWaitIdle(_graphicsQueue), "QueueWaitIdle");
        }
        catch (PvrVulkanException error)
        {
            PvrRuntime.Log.Warn("WaitIdle: {0}", error.Message);
        }
    }

    internal CommandBuffer PrepareOverlay(bool vmu, bool crosshair) =>
        _overlay!.Prepare(_commandPools[GetCurrentImageIndex()].Handle, vmu, crosshair);

    internal void DrawOverlay(float scaling, bool vmu, bool crosshair)
    {
        if (IsValid())
            _overlay!.Draw(GetCurrentCommandBuffer(), new Extent2D(_width, _height), scaling, vmu, crosshair);
    }

    internal void PresentFrame(Image image, ImageView imageView, in Extent2D extent, float aspectRatio)
    {
        _lastFrameView = imageView;
        _lastFrameExtent = extent;
        _lastFrameAspectRatio = aspectRatio;
        if (imageView.Handle != 0 && IsValid())
        {
            try
            {
                NewFrame();
                CommandBuffer overlay = PrepareOverlay(PvrConfiguration.FloatVmus, true);
                PvrRuntime.GuiDrawOsd();
                if (_vendorId == VendorNvidia && image.Handle != 0)
                {
                    var barrier = MakeImageBarrier(image, AccessFlags.ColorAttachmentWriteBit, AccessFlags.ShaderReadBit,
                        ImageLayout.ShaderReadOnlyOptimal, ImageLayout.ShaderReadOnlyOptimal);
                    PvrVk.PipelineBarrier(GetCurrentCommandBuffer(), PipelineStageFlags.ColorAttachmentOutputBit,
                        PipelineStageFlags.FragmentShaderBit, barrier);
                }
                BeginRenderPass();
                if (_lastFrameView.Handle != 0)
                    DrawFrame(imageView, extent, aspectRatio);
                DrawOverlay(PvrRuntime.DisplayUiScale, PvrConfiguration.FloatVmus, true);
                ImGuiDriver!.RenderDrawData(PvrRuntime.GetImGuiDrawData(), false);
                EndFrame(overlay);
                ((PvrVulkanRenderer)PvrRenderer.Backend!).RenderVideoRouting();
            }
            catch (PvrInvalidVulkanContextException)
            {
                _resized = true;
            }
        }
    }

    internal void PresentLastFrame()
    {
        if (_lastFrameView.Handle != 0 && IsValid())
            DrawFrame(_lastFrameView, _lastFrameExtent, _lastFrameAspectRatio);
    }

    private void Term()
    {
        _lastFrameView = default;
        try
        {
            if (_device != null && _graphicsQueue.Handle != 0)
                WaitIdle();
            if (_device != null && _drawFences.Count != 0)
            {
                Fence[] fences = _drawFences.ConvertAll(fence => fence.Handle).ToArray();
                try
                {
                    Result result = _device.WaitForFences(fences, true, ulong.MaxValue);
                    if (result != Result.Success)
                        PvrRuntime.Log.Info("VulkanContext::term: waitForFences failed {0}", (int)result);
                }
                catch (PvrVulkanException)
                {
                }
            }
            ClearInFlight();
            ImGuiDriver?.Dispose();
            ImGuiDriver = null;
            if (_device != null && _pipelineCache != null)
            {
                byte[] data = _device.GetPipelineCacheData(_pipelineCache.Handle);
                if (data.Length != 0)
                {
                    string path = PvrRuntime.GetShaderCachePath("vulkan_pipeline.cache");
                    try
                    {
                        File.WriteAllBytes(path, data);
                    }
                    catch (IOException)
                    {
                    }
                }
            }
        }
        finally
        {
            try
            {
                Reset(ref _overlay);
                PvrVulkanShaderCompiler.Term();
                Reset(ref _swapChain);
                ClearOwned(_imageViews);
                ClearOwned(_framebuffers);
                Reset(ref _renderPass);
                Reset(ref _quadDrawer);
                Reset(ref _quadPipeline);
                Reset(ref _quadPipelineWithAlpha);
                Reset(ref _quadRotateDrawer);
                Reset(ref _quadRotatePipeline);
                _shaderManager?.Term();
                _shaderManager = null;
                Reset(ref _descriptorPool);
                ClearOwned(_commandBuffers);
                ClearOwned(_commandPools);
                ClearOwned(_imageAcquiredSemaphores);
                ClearOwned(_renderCompleteSemaphores);
                ClearOwned(_drawFences);
                _allocator.Term();
                Reset(ref _surface);
                Reset(ref _pipelineCache);
                PvrSwappyVk.DetachDevice();
            }
            finally
            {
                Reset(ref _device);
#if VK_DEBUG
                Reset(ref _debugUtilsMessenger);
#endif
                Reset(ref _instance);
            }
        }
    }

    private void DoSwapAutomation()
    {
#if TEST_AUTOMATION
        if (PvrRuntime.DoScreenshot)
        {
            bool supportsBlit = true;
            Api.GetPhysicalDeviceFormatProperties(_physicalDevice, _presentFormat, out FormatProperties properties);
            if ((properties.OptimalTilingFeatures & FormatFeatureFlags.BlitSrcBit) == 0)
                supportsBlit = false;
            Api.GetPhysicalDeviceFormatProperties(_physicalDevice, Format.R8G8B8A8Unorm, out properties);
            if ((properties.LinearTilingFeatures & FormatFeatureFlags.BlitDstBit) == 0)
                supportsBlit = false;

            {
                Image source = _device!.GetSwapchainImages(_swapChain!.Handle)[(int)_currentImage];
                var imageInfo = new ImageCreateInfo
                {
                    SType = StructureType.ImageCreateInfo, ImageType = ImageType.Type2D, Format = Format.R8G8B8A8Unorm,
                    Extent = new Extent3D(_width, _height, 1), MipLevels = 1, ArrayLayers = 1, Samples = SampleCountFlags.Count1Bit,
                    Tiling = ImageTiling.Linear, Usage = ImageUsageFlags.TransferDstBit
                };
                using var image = _device.CreateImageUnique(imageInfo);
                MemoryRequirements requirements = _device.GetImageMemoryRequirements(image.Handle);
                Api.GetPhysicalDeviceMemoryProperties(_physicalDevice, out PhysicalDeviceMemoryProperties memoryProperties);
                uint type = PvrVulkanUtilities.FindMemoryType(memoryProperties, requirements.MemoryTypeBits,
                    MemoryPropertyFlags.HostCoherentBit | MemoryPropertyFlags.HostVisibleBit);
                using var memory = _device.AllocateMemoryUnique(requirements.Size, type);
                _device.BindImageMemory(image.Handle, memory.Handle, 0);
                using var buffer = _device.AllocateCommandBufferUnique(_commandPools[^1].Handle, CommandBufferLevel.Primary);
                PvrVk.Begin(buffer.Handle, CommandBufferUsageFlags.OneTimeSubmitBit);
                var barrier = MakeImageBarrier(image.Handle, default, AccessFlags.TransferWriteBit,
                    ImageLayout.Undefined, ImageLayout.TransferDstOptimal);
                PvrVk.PipelineBarrier(buffer.Handle, PipelineStageFlags.TransferBit, PipelineStageFlags.TransferBit, barrier);
                barrier = MakeImageBarrier(source, AccessFlags.MemoryReadBit, AccessFlags.TransferReadBit,
                    ImageLayout.PresentSrcKhr, ImageLayout.TransferSrcOptimal);
                PvrVk.PipelineBarrier(buffer.Handle, PipelineStageFlags.TransferBit, PipelineStageFlags.TransferBit, barrier);
                var layers = new ImageSubresourceLayers(ImageAspectFlags.ColorBit, 0, 0, 1);
                if (supportsBlit)
                {
                    var blit = new ImageBlit { SrcSubresource = layers, DstSubresource = layers };
                    blit.SrcOffsets[1] = new Offset3D((int)_width, (int)_height, 1);
                    blit.DstOffsets[1] = blit.SrcOffsets[1];
                    PvrVk.BlitImage(buffer.Handle, source, ImageLayout.TransferSrcOptimal, image.Handle,
                        ImageLayout.TransferDstOptimal, blit, Filter.Nearest);
                }
                else
                {
                    var copy = new ImageCopy(layers, default, layers, default, new Extent3D(_width, _height, 1));
                    Api.CmdCopyImage(buffer.Handle, source, ImageLayout.TransferSrcOptimal, image.Handle,
                        ImageLayout.TransferDstOptimal, 1, &copy);
                }
                barrier = MakeImageBarrier(image.Handle, AccessFlags.TransferWriteBit, AccessFlags.MemoryReadBit,
                    ImageLayout.TransferDstOptimal, ImageLayout.General);
                PvrVk.PipelineBarrier(buffer.Handle, PipelineStageFlags.TransferBit, PipelineStageFlags.TransferBit, barrier);
                barrier = MakeImageBarrier(source, AccessFlags.TransferReadBit, AccessFlags.MemoryReadBit,
                    ImageLayout.TransferSrcOptimal, ImageLayout.PresentSrcKhr);
                PvrVk.PipelineBarrier(buffer.Handle, PipelineStageFlags.TransferBit, PipelineStageFlags.TransferBit, barrier);
                PvrVk.End(buffer.Handle);
                SubmitCommandBuffers([buffer.Handle], default);
                PvrVk.ResultCheck(Api.QueueWaitIdle(_graphicsQueue), "QueueWaitIdle");

                SubresourceLayout layout = _device.GetImageSubresourceLayout(image.Handle, new ImageSubresource(ImageAspectFlags.ColorBit, 0, 0));
                byte* pixels = (byte*)_device.MapMemory(memory.Handle, 0, ulong.MaxValue) + layout.Offset;
                try
                {
                    byte* end = pixels + PvrRuntime.DisplayWidth * PvrRuntime.DisplayHeight * 4;
                    if (!supportsBlit && _presentFormat == Format.B8G8R8A8Unorm)
                    {
                        for (byte* p = pixels; p < end; p += 4)
                        {
                            byte blue = p[0];
                            p[0] = p[2];
                            p[2] = blue;
                            p[3] = 0xff;
                        }
                    }
                    else
                    {
                        for (byte* p = pixels; p < end; p += 4)
                            p[3] = 0xff;
                    }
                    PvrRuntime.DumpScreenshot(pixels, PvrRuntime.DisplayWidth, PvrRuntime.DisplayHeight, true, layout.RowPitch, false);
                }
                finally
                {
                    _device.UnmapMemory(memory.Handle);
                }
            }
            PvrRuntime.DcExit();
            PvrRuntime.Terminate();
            Environment.Exit(0);
        }
#endif
    }

    private bool HasSurfaceDimensionChanged()
    {
        SurfaceCapabilitiesKHR capabilities = PvrVk.GetSurfaceCapabilities(_physicalDevice, GetSurface());
        Extent2D extent = capabilities.CurrentExtent.Width == uint.MaxValue
            ? new Extent2D(
                Math.Min(Math.Max(_width, capabilities.MinImageExtent.Width), capabilities.MaxImageExtent.Width),
                Math.Min(Math.Max(_height, capabilities.MinImageExtent.Height), capabilities.MaxImageExtent.Height))
            : capabilities.CurrentExtent;
        return _width != extent.Width || _height != extent.Height;
    }

    private void SetWindowSize(uint width, uint height)
    {
        if (width != _width || height != _height)
        {
            _width = width;
            _height = height;
            if (width != 0)
                PvrRuntime.DisplayWidth = (int)width;
            if (height != 0)
                PvrRuntime.DisplayHeight = (int)height;
            Resize();
        }
    }

    internal static void Create(nint window, nint display) => _ = new PvrVulkanContext(window, display);

    private PvrVulkanContext(nint window, nint display) : base(window, display)
    {
        bool ready = false;
        try
        {
            if (!Init())
                throw new PvrRendererException("Vulkan initialization failed");
            ready = true;
        }
        finally
        {
            if (!ready && _instance != null)
                Term();
        }
    }

    public override void Dispose() => Term();

    internal bool GetLastFrame(List<byte> data, ref int width, ref int height)
    {
        if (_lastFrameView.Handle == 0)
            return false;
        if (width != 0)
        {
            height = (int)(width / _lastFrameAspectRatio);
        }
        else if (height != 0)
        {
            width = (int)(_lastFrameAspectRatio * height);
        }
        else
        {
            width = (int)_lastFrameExtent.Width;
            height = (int)_lastFrameExtent.Height;
            if (PvrConfiguration.Rotate90)
                (width, height) = (height, width);
            int correctedWidth = (int)(_lastFrameAspectRatio * height);
            if (width > correctedWidth)
                height = (int)(width / _lastFrameAspectRatio);
            else
                width = correctedWidth;
        }

        Format format = Format.R8G8B8A8Unorm;
        const ImageUsageFlags usage = ImageUsageFlags.ColorAttachmentBit | ImageUsageFlags.TransferSrcBit;
        bool nativeRgb8 = Api.GetPhysicalDeviceImageFormatProperties(_physicalDevice, Format.R8G8B8Unorm,
            ImageType.Type2D, ImageTiling.Optimal, usage, 0, out _) == Result.Success;
        if (nativeRgb8)
            format = Format.R8G8B8Unorm;

        using var attachment = new PvrVulkanFramebufferAttachment(_physicalDevice, _device!);
        attachment.Init((uint)width, (uint)height, format, usage, "screenshot");
        using var buffer = _device!.AllocateCommandBufferUnique(_commandPools[^1].Handle, CommandBufferLevel.Primary);
        PvrVk.Begin(buffer.Handle, CommandBufferUsageFlags.OneTimeSubmitBit);
        using var debug = new PvrVulkanCommandBufferDebugScope(buffer.Handle, "GetLastFrame", [1f, 1f, 0f, 1f]);
        using var renderPass = CreatePresentationRenderPass(format, ImageLayout.TransferSrcOptimal);
        using var framebuffer = _device.CreateFramebufferUnique(renderPass.Handle, [attachment.GetImageView()], (uint)width, (uint)height, 1);
        ClearValue clear = default;
        var info = new RenderPassBeginInfo
        {
            SType = StructureType.RenderPassBeginInfo, RenderPass = renderPass.Handle, Framebuffer = framebuffer.Handle,
            RenderArea = new Rect2D(default, new Extent2D((uint)width, (uint)height)), ClearValueCount = 1, PClearValues = &clear
        };
        Api.CmdBeginRenderPass(buffer.Handle, &info, SubpassContents.Inline);
        using var pipeline = new PvrVulkanQuadPipeline(true, PvrConfiguration.Rotate90);
        pipeline.Init(_shaderManager!, renderPass.Handle, 0);
        pipeline.BindPipeline(buffer.Handle);

        PvrVulkanQuadVertex* vertices = stackalloc PvrVulkanQuadVertex[4]
        {
            new(-1, -1, 0, 0, 0), new(1, -1, 0, 1, 0),
            new(-1, 1, 0, 0, 1), new(1, 1, 0, 1, 1)
        };
        var viewport = new Viewport(0, 0, width, height, 0f, 1f);
        var scissor = new Rect2D(default, new Extent2D((uint)width, (uint)height));
        Api.CmdSetViewport(buffer.Handle, 0, 1, &viewport);
        Api.CmdSetScissor(buffer.Handle, 0, 1, &scissor);
        using var drawer = new PvrVulkanQuadDrawer();
        drawer.Init(pipeline);
        drawer.Draw(buffer.Handle, _lastFrameView, vertices, false);
        Api.CmdEndRenderPass(buffer.Handle);

        if (_vendorId == VendorArm)
        {
            var memoryBarrier = new MemoryBarrier
            {
                SType = StructureType.MemoryBarrier, SrcAccessMask = AccessFlags.ColorAttachmentWriteBit,
                DstAccessMask = AccessFlags.TransferReadBit
            };
            Api.CmdPipelineBarrier(buffer.Handle, PipelineStageFlags.ColorAttachmentOutputBit, PipelineStageFlags.TransferBit,
                0, 1, &memoryBarrier, 0, null, 0, null);
        }

        var copy = new BufferImageCopy(0, (uint)width, (uint)height,
            new ImageSubresourceLayers(ImageAspectFlags.ColorBit, 0, 0, 1), default, new Extent3D((uint)width, (uint)height, 1));
        PvrVulkanBuffer staging = attachment.GetBufferData()!;
        Api.CmdCopyImageToBuffer(buffer.Handle, attachment.GetImage(), ImageLayout.TransferSrcOptimal, staging.Buffer, 1, &copy);
        var bufferBarrier = new BufferMemoryBarrier
        {
            SType = StructureType.BufferMemoryBarrier, SrcAccessMask = AccessFlags.TransferWriteBit,
            DstAccessMask = AccessFlags.HostReadBit, SrcQueueFamilyIndex = uint.MaxValue, DstQueueFamilyIndex = uint.MaxValue,
            Buffer = staging.Buffer, Offset = 0, Size = ulong.MaxValue
        };
        Api.CmdPipelineBarrier(buffer.Handle, PipelineStageFlags.TransferBit, PipelineStageFlags.HostBit, 0, 0, null, 1, &bufferBarrier, 0, null);
        PvrVk.End(buffer.Handle);

        using var fence = _device.CreateFenceUnique(default);
        SubmitCommandBuffers([buffer.Handle], fence.Handle);
        Result result = _device.WaitForFences([fence.Handle], true, ulong.MaxValue);
        if (result != Result.Success)
            PvrRuntime.Log.Warn("VulkanContext::GetLastFrame: waitForFences failed {0}", (int)result);
        byte* pixels = (byte*)staging.MapMemory();
        try
        {
            data.Clear();
            if (nativeRgb8)
            {
                CollectionsMarshal.SetCount(data, width * height * 3);
                new ReadOnlySpan<byte>(pixels, width * height * 3).CopyTo(CollectionsMarshal.AsSpan(data));
            }
            else
            {
                data.EnsureCapacity(width * height * 3);
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        data.Add(*pixels++);
                        data.Add(*pixels++);
                        data.Add(*pixels++);
                        pixels++;
                    }
                }
            }
        }
        finally
        {
            staging.UnmapMemory();
        }
        return true;
    }

    internal override void SetSwapInterval(int interval)
    {
        interval = Math.Min(interval, 2);
        if (_gameSwapInterval != interval)
        {
            PvrRuntime.Log.Notice("Swap interval changed to {0}", _swapOnVSync ? _swapInterval * interval : 0);
            PvrSwappyVk.SetSwapInterval(interval);
        }
        _gameSwapInterval = interval;
    }

    private static ImageMemoryBarrier MakeImageBarrier(Image image, AccessFlags source, AccessFlags destination,
        ImageLayout oldLayout, ImageLayout newLayout) => new()
    {
        SType = StructureType.ImageMemoryBarrier, SrcAccessMask = source, DstAccessMask = destination,
        OldLayout = oldLayout, NewLayout = newLayout, SrcQueueFamilyIndex = uint.MaxValue, DstQueueFamilyIndex = uint.MaxValue,
        Image = image, SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, 1)
    };

    private static void Reset<T>(ref T? value) where T : class, IDisposable
    {
        try
        {
            value?.Dispose();
        }
        finally
        {
            value = null;
        }
    }

    private static void ClearOwned<T>(List<T> values) where T : IDisposable
    {
        try
        {
            foreach (T value in values)
                value.Dispose();
        }
        finally
        {
            values.Clear();
        }
    }

    private void ClearInFlight()
    {
        try
        {
            foreach (var frame in _inFlightObjects)
                ClearOwned(frame);
        }
        finally
        {
            _inFlightObjects.Clear();
        }
    }
}
