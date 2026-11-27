using Silk.NET.Vulkan;
using VkBuffer = Silk.NET.Vulkan.Buffer;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Vulkan;

internal sealed unsafe class PvrVulkanAllocation : IDisposable
{
    internal PvrVulkanAllocation()
    {
    }

    internal PvrVulkanAllocation(PvrVulkanAllocation other)
    {
        _allocator = other._allocator;
        _allocation = other._allocation;
        _info = other._info;
        other._allocator = 0;
        other._allocation = 0;
    }

    internal PvrVulkanAllocation MoveAssign(PvrVulkanAllocation other)
    {
        (_allocator, other._allocator) = (other._allocator, _allocator);
        (_allocation, other._allocation) = (other._allocation, _allocation);
        (_info, other._info) = (other._info, _info);
        return this;
    }

    public void Dispose()
    {
        if (_allocator != 0)
        {
            try
            {
                Vma.FreeMemory(_allocator, _allocation);
            }
            finally
            {
                _allocator = 0;
                _allocation = 0;
            }
        }
    }

    internal bool IsHostVisible()
    {
        Vma.GetMemoryTypeProperties(_allocator, _info.MemoryType, out MemoryPropertyFlags flags);
        return (flags & MemoryPropertyFlags.HostVisibleBit) != 0;
    }

    internal void* MapMemory()
    {
        if (_info.MappedData != null)
            return _info.MappedData;
        Result result = Vma.MapMemory(_allocator, _allocation, out void* pointer);
        PvrVk.ResultCheck(result, "vmaMapMemory failed");
        Vma.GetMemoryTypeProperties(_allocator, _info.MemoryType, out MemoryPropertyFlags flags);
        if ((flags & MemoryPropertyFlags.HostCachedBit) != 0 && (flags & MemoryPropertyFlags.HostCoherentBit) == 0)
            Vma.InvalidateAllocation(_allocator, _allocation, _info.Offset, _info.Size);
        return pointer;
    }

    internal void UnmapMemory()
    {
        if (_info.MappedData != null)
            return;
        try
        {
            Vma.GetMemoryTypeProperties(_allocator, _info.MemoryType, out MemoryPropertyFlags flags);
            if ((flags & MemoryPropertyFlags.HostCachedBit) != 0 && (flags & MemoryPropertyFlags.HostCoherentBit) == 0)
                Vma.FlushAllocation(_allocator, _allocation, _info.Offset, _info.Size);
        }
        finally
        {
            Vma.UnmapMemory(_allocator, _allocation);
        }
    }

    internal PvrVulkanAllocation(nint allocator, nint allocation, VmaAllocationInfo info)
    {
        _allocator = allocator;
        _allocation = allocation;
        _info = info;
    }

    private nint _allocator;
    private nint _allocation;
    private VmaAllocationInfo _info;
}

internal sealed unsafe partial class PvrVulkanMemoryAllocator : IDisposable
{
    public void Dispose() => Term();

    internal void Term()
    {
        if (_allocator != 0)
        {
            try
            {
                Vma.DestroyAllocator(_allocator);
            }
            finally
            {
                _allocator = 0;
            }
        }
    }

    internal PvrVulkanAllocation AllocateMemory(in MemoryRequirements requirements, in VmaAllocationCreateInfo createInfo)
    {
        Result result = Vma.AllocateMemory(_allocator, requirements, createInfo, out nint allocation, out VmaAllocationInfo info);
        PvrVk.ResultCheck(result, "vmaAllocateMemory failed");
        return new PvrVulkanAllocation(_allocator, allocation, info);
    }

    internal PvrVulkanAllocation AllocateForImage(Image image, in VmaAllocationCreateInfo createInfo)
    {
        Result result = Vma.AllocateMemoryForImage(_allocator, image, createInfo, out nint allocation, out VmaAllocationInfo info);
        PvrVk.ResultCheck(result, "vmaAllocateMemoryForImage failed");
        Vma.BindImageMemory(_allocator, allocation, image);
        return new PvrVulkanAllocation(_allocator, allocation, info);
    }

    internal PvrVulkanAllocation AllocateForBuffer(VkBuffer buffer, in VmaAllocationCreateInfo createInfo)
    {
        Result result = Vma.AllocateMemoryForBuffer(_allocator, buffer, createInfo, out nint allocation, out VmaAllocationInfo info);
        PvrVk.ResultCheck(result, "vmaAllocateMemoryForBuffer failed");
        Vma.BindBufferMemory(_allocator, allocation, buffer);
        return new PvrVulkanAllocation(_allocator, allocation, info);
    }

    private nint _allocator;
}
