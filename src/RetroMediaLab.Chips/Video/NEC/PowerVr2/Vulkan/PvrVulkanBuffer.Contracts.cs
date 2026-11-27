using Silk.NET.Vulkan;
using VkBuffer = Silk.NET.Vulkan.Buffer;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Vulkan;

internal sealed unsafe partial class PvrVulkanBuffer : IDisposable
{
    public void Dispose()
    {
        try
        {
            _buffer?.Dispose();
        }
        finally
        {
            _buffer = null;
            _allocation?.Dispose();
            _allocation = null;
        }
    }

    internal void Upload(uint size, void* data, uint bufferOffset = 0)
    {
        PvrRuntime.Verify(bufferOffset + size <= BufferSize);
        byte* destination = (byte*)_allocation!.MapMemory() + bufferOffset;
        try
        {
            System.Buffer.MemoryCopy(data, destination, size, size);
        }
        finally
        {
            _allocation.UnmapMemory();
        }
    }

    internal void Upload(nuint count, uint* sizes, void** data, uint bufferOffset = 0)
    {
        uint totalSize = 0;
        for (nuint i = 0; i < count; i++)
            totalSize += sizes[i];
        PvrRuntime.Verify(bufferOffset + totalSize <= BufferSize);
        byte* destination = (byte*)_allocation!.MapMemory() + bufferOffset;
        try
        {
            for (nuint i = 0; i < count; i++)
            {
                if (data[i] != null)
                    System.Buffer.MemoryCopy(data[i], destination, sizes[i], sizes[i]);
                destination += sizes[i];
            }
        }
        finally
        {
            _allocation.UnmapMemory();
        }
    }

    internal void Download(uint size, void* data, uint bufferOffset = 0)
    {
        PvrRuntime.Verify(bufferOffset + size <= BufferSize);
        byte* source = (byte*)_allocation!.MapMemory() + bufferOffset;
        try
        {
            System.Buffer.MemoryCopy(source, data, size, size);
        }
        finally
        {
            _allocation.UnmapMemory();
        }
    }

    internal void* MapMemory() => _allocation!.MapMemory();
    internal void UnmapMemory() => _allocation!.UnmapMemory();

    internal VkBuffer Buffer => _buffer!.Handle;
    internal ulong BufferSize { get; }
    private PvrVulkanOwned<VkBuffer>? _buffer;
    private PvrVulkanAllocation? _allocation;
    private readonly BufferUsageFlags _usage;
}

internal sealed unsafe partial class PvrVulkanBufferPacker
{
    internal ulong AddUniform(void* pointer, nuint size) => Add(pointer, size, (uint)_uniformAlignment);
    internal ulong AddStorage(void* pointer, nuint size) => Add(pointer, size, (uint)_storageAlignment);

    internal ulong Add(void* pointer, nuint size, uint alignment = 4)
    {
        uint padding = PvrVulkanUtilities.Align(_offset, Math.Max(4u, alignment));
        if (padding != 0)
        {
            _chunks.Add(0);
            _chunkSizes.Add(padding);
            _offset += padding;
        }

        ulong start = _offset;
        _chunks.Add((nint)pointer);
        _chunkSizes.Add((uint)size);
        _offset += size;
        return start;
    }

    internal void Upload(PvrVulkanBuffer buffer, uint bufferOffset = 0)
    {
        if (_chunks.Count != 0)
        {
            fixed (uint* sizes = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_chunkSizes))
            fixed (nint* chunks = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_chunks))
                buffer.Upload((nuint)_chunks.Count, sizes, (void**)chunks, bufferOffset);
        }
    }

    internal ulong Size() => _offset;

    private readonly List<nint> _chunks = [];
    private readonly List<uint> _chunkSizes = [];
    private ulong _offset;
    private readonly ulong _uniformAlignment;
    private readonly ulong _storageAlignment;
}
