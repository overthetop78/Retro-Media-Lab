using System.Diagnostics;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Vulkan;

internal sealed unsafe partial class PvrVulkanMemoryAllocator
{
#if DEBUG || DEBUGFAST
    [UnmanagedCallersOnly]
    private static void OnAllocate(nint allocator, uint memoryType, DeviceMemory memory, ulong size, void* userData)
    {
        PvrRuntime.Log.Debug("VMAAllocator: {0} bytes allocated (type {1})", size, memoryType);
    }

    [UnmanagedCallersOnly]
    private static void OnFree(nint allocator, uint memoryType, DeviceMemory memory, ulong size, void* userData)
    {
        PvrRuntime.Log.Debug("VMAAllocator: {0} bytes freed (type {1})", size, memoryType);
    }

    private static readonly VmaDeviceMemoryCallbacks MemoryCallbacks = new(&OnAllocate, &OnFree);
#endif

    internal void Init(PhysicalDevice physicalDevice, Device device, Instance instance)
    {
        Debug.Assert(_allocator == 0);
        PvrVulkanContext context = PvrVulkanContext.Instance;
        VmaAllocatorCreateInfo createInfo = new()
        {
            Flags = VmaAllocatorCreateFlags.ExternallySynchronizedBit
        };
        if (context.SupportsDedicatedAllocation())
            createInfo.Flags |= VmaAllocatorCreateFlags.KhrDedicatedAllocationBit;
        if (context.SupportsBufferDeviceAddress())
            createInfo.Flags |= VmaAllocatorCreateFlags.BufferDeviceAddressBit;
        createInfo.PhysicalDevice = physicalDevice;
        context.Api.GetPhysicalDeviceProperties(physicalDevice, out PhysicalDeviceProperties properties);
        createInfo.VulkanApiVersion = properties.ApiVersion >= Vk.Version11 ? Vk.Version11 : Vk.Version10;
        createInfo.Device = device;
        createInfo.Instance = instance;
#if DEBUG || DEBUGFAST
        VmaDeviceMemoryCallbacks callbacks = MemoryCallbacks;
        createInfo.DeviceMemoryCallbacks = &callbacks;
#endif
#if VMA_DYNAMIC_VULKAN_FUNCTIONS
        VmaVulkanFunctions functions = new()
        {
            GetInstanceProcAddr = PvrVk.GetInstanceProcAddrPointer,
            GetDeviceProcAddr = PvrVk.GetDeviceProcAddrPointer
        };
        createInfo.VulkanFunctions = &functions;
#endif
        Result result = Vma.CreateAllocator(createInfo, out _allocator);
        PvrVk.ResultCheck(result, "vmaCreateAllocator failed");
    }
}
