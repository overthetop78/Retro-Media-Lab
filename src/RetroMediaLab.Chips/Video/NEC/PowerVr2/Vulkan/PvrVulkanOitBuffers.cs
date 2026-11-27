using Silk.NET.Vulkan;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Vulkan;

internal unsafe sealed class PvrVulkanOitBuffers
{
    private static DescriptorBufferInfo _pixelBufferInfo = new(default, 0, ulong.MaxValue);
    private static DescriptorBufferInfo _pixelCounterBufferInfo = new(default, 0, 4);
    private static DescriptorBufferInfo _abufferPointerInfo = new(default, 0, ulong.MaxValue);
    private PvrVulkanBuffer? _pixelBuffer;
    private PvrVulkanBuffer? _pixelCounter;
    private PvrVulkanBuffer? _pixelCounterReset;
    private PvrVulkanBuffer? _abufferPointer;
    private bool _firstFrameAfterInit;
    private int _maxWidth;
    private int _maxHeight;
    private long _pixelBufferSize;
    private ulong _pixelBufferAddress;

    internal void Init(int width, int height)
    {
        PvrVulkanContext context = PvrVulkanContext.Instance;
        if (width <= _maxWidth && height <= _maxHeight)
            return;
        _maxWidth = Math.Max(_maxWidth, width);
        _maxHeight = Math.Max(_maxHeight, height);

        if (_pixelCounter == null)
        {
            _pixelCounter = new PvrVulkanBuffer(4,
                BufferUsageFlags.StorageBufferBit | BufferUsageFlags.TransferDstBit, MemoryPropertyFlags.DeviceLocalBit);
            _pixelCounterReset = new PvrVulkanBuffer(4, BufferUsageFlags.TransferSrcBit);
            int zero = 0;
            _pixelCounterReset.Upload(sizeof(int), &zero);
        }

        context.WaitIdle();
        _abufferPointer?.Dispose();
        _abufferPointer = new PvrVulkanBuffer((ulong)(_maxWidth * _maxHeight) * sizeof(int),
            BufferUsageFlags.StorageBufferBit, MemoryPropertyFlags.DeviceLocalBit);
        _firstFrameAfterInit = true;
    }

    internal void UpdateDescriptorSet(DescriptorSet descSet, List<PvrVulkanDescriptorWrite> writeDescSets)
    {
        _pixelBufferInfo.Buffer = _pixelBuffer!.Buffer;
        _pixelBufferInfo.Range = _pixelBufferAddress != 0 ? 1ul : ulong.MaxValue;
        writeDescSets.Add(new(descSet, 7, 0, DescriptorType.StorageBuffer, null, _pixelBufferInfo));
        _pixelCounterBufferInfo.Buffer = _pixelCounter!.Buffer;
        writeDescSets.Add(new(descSet, 8, 0, DescriptorType.StorageBuffer, null, _pixelCounterBufferInfo));
        _abufferPointerInfo.Buffer = _abufferPointer!.Buffer;
        writeDescSets.Add(new(descSet, 9, 0, DescriptorType.StorageBuffer, null, _abufferPointerInfo));
    }

    internal void OnNewFrame(CommandBuffer commandBuffer)
    {
        _firstFrameAfterInit = false;
        if (_pixelBufferSize != PvrConfiguration.PixelBufferSize)
        {
            _pixelBufferSize = PvrConfiguration.PixelBufferSize;
            PvrVulkanContext.Instance.WaitIdle();
            MakePixelBuffer();
        }
    }

    internal void ResetPixelCounter(CommandBuffer commandBuffer)
    {
        Vk api = PvrVulkanContext.Instance.Api;
        BufferMemoryBarrier barrier = new()
        {
            SType = StructureType.BufferMemoryBarrier,
            SrcAccessMask = AccessFlags.ShaderWriteBit,
            DstAccessMask = AccessFlags.TransferWriteBit,
            SrcQueueFamilyIndex = uint.MaxValue,
            DstQueueFamilyIndex = uint.MaxValue,
            Buffer = _pixelCounter!.Buffer,
            Offset = 0,
            Size = ulong.MaxValue
        };
        api.CmdPipelineBarrier(commandBuffer, PipelineStageFlags.FragmentShaderBit, PipelineStageFlags.TransferBit,
            0, 0, null, 1, &barrier, 0, null);
        BufferCopy copy = new(0, 0, sizeof(int));
        api.CmdCopyBuffer(commandBuffer, _pixelCounterReset!.Buffer, _pixelCounter.Buffer, 1, &copy);

        barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
        barrier.DstAccessMask = AccessFlags.ShaderReadBit | AccessFlags.ShaderWriteBit;
        api.CmdPipelineBarrier(commandBuffer, PipelineStageFlags.TransferBit, PipelineStageFlags.FragmentShaderBit,
            0, 0, null, 1, &barrier, 0, null);
    }

    internal void Term()
    {
        _pixelBufferAddress = 0;
        try
        {
            _pixelBuffer?.Dispose();
        }
        finally
        {
            _pixelBuffer = null;
            try
            {
                _pixelCounter?.Dispose();
            }
            finally
            {
                _pixelCounter = null;
                try
                {
                    _pixelCounterReset?.Dispose();
                }
                finally
                {
                    _pixelCounterReset = null;
                    _abufferPointer?.Dispose();
                    _abufferPointer = null;
                }
            }
        }
    }

    internal bool IsFirstFrameAfterInit() => _firstFrameAfterInit;

    internal ulong GetPixelBufferAddress() => _pixelBufferAddress;

    internal ulong GetPixelBufferSize() => _pixelBuffer?.BufferSize ?? 0;

    private void MakePixelBuffer()
    {
        PvrVulkanContext context = PvrVulkanContext.Instance;
        uint maxStorageBufferRange = context.GetMaxStorageBufferRange();
        ulong allocSize = Math.Min((ulong)_pixelBufferSize, context.GetMaxMemoryAllocationSize());
        BufferUsageFlags usage = BufferUsageFlags.StorageBufferBit;
        if (allocSize > maxStorageBufferRange)
        {
            if (context.SupportsBufferDeviceAddress())
            {
                PvrRuntime.Log.Notice("PixelBuffer allocSize {0} > maxStorageBufferRange {1}; will use BDA", allocSize, maxStorageBufferRange);
                usage |= BufferUsageFlags.ShaderDeviceAddressBit;
            }
            else
            {
                PvrRuntime.Log.Notice("PixelBuffer allocSize {0} > maxStorageBufferRange {1}; capping allocSize, as the GPU doesn't support BDA", allocSize, maxStorageBufferRange);
                allocSize = maxStorageBufferRange;
            }
        }

        PvrVulkanBuffer? previousBuffer = _pixelBuffer;
        _pixelBuffer = new PvrVulkanBuffer(allocSize, usage, MemoryPropertyFlags.DeviceLocalBit);
        previousBuffer?.Dispose();
        _pixelBufferAddress = (usage & BufferUsageFlags.ShaderDeviceAddressBit) != 0
            ? context.Api.GetBufferDeviceAddress(context.GetDevice(), new BufferDeviceAddressInfo
            {
                SType = StructureType.BufferDeviceAddressInfo,
                Buffer = _pixelBuffer.Buffer
            })
            : 0;
        PvrRuntime.Log.Debug("PixelBuffer allocated. size {0} address {1}", allocSize, _pixelBufferAddress);
    }
}
