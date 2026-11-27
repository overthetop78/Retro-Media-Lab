using Silk.NET.Vulkan;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Vulkan;

internal sealed unsafe partial class PvrVulkanBuffer
{
    internal PvrVulkanBuffer(ulong size, BufferUsageFlags usage,
        MemoryPropertyFlags properties = MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit)
    {
        BufferSize = size;
        _usage = usage;
        PvrVulkanContext context = PvrVulkanContext.Instance;
        _buffer = context.GetDevice().CreateBufferUnique(new BufferCreateInfo
        {
            SType = StructureType.BufferCreateInfo,
            Size = size,
            Usage = usage
        });
        bool initialized = false;
        try
        {
            VmaAllocationCreateInfo allocationInfo = default;
            if ((properties & MemoryPropertyFlags.DeviceLocalBit) != 0)
            {
                allocationInfo.RequiredFlags = MemoryPropertyFlags.DeviceLocalBit;
            }
            else
            {
#if OSX
                allocationInfo.Flags |= VmaAllocationCreateFlags.DedicatedMemoryBit;
                properties &= ~MemoryPropertyFlags.HostCoherentBit;
#endif
                if ((properties & MemoryPropertyFlags.HostVisibleBit) != 0)
                {
                    allocationInfo.RequiredFlags = MemoryPropertyFlags.HostVisibleBit;
                    if ((properties & MemoryPropertyFlags.HostCachedBit) != 0)
                        allocationInfo.PreferredFlags |= MemoryPropertyFlags.HostCachedBit;
                    if ((properties & MemoryPropertyFlags.HostCoherentBit) != 0)
                        allocationInfo.PreferredFlags |= MemoryPropertyFlags.HostCoherentBit;
                }
            }

            _allocation = context.GetAllocator().AllocateForBuffer(_buffer.Handle, allocationInfo);
            initialized = true;
        }
        finally
        {
            if (!initialized)
                Dispose();
        }
    }
}

internal sealed unsafe partial class PvrVulkanBufferPacker
{
    internal PvrVulkanBufferPacker()
    {
        _uniformAlignment = PvrVulkanContext.Instance.GetUniformBufferAlignment();
        _storageAlignment = PvrVulkanContext.Instance.GetStorageBufferAlignment();
    }
}
