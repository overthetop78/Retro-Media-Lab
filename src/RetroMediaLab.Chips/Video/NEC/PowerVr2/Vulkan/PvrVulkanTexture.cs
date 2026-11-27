using Silk.NET.Vulkan;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Vulkan;

internal static class PvrVulkanImageLayout
{
    internal static void SetImageLayout(CommandBuffer commandBuffer, Image image, Format format, uint levels,
        ImageLayout oldLayout, ImageLayout newLayout)
    {
        using var scope = new PvrVulkanCommandBufferDebugScope(commandBuffer, "setImageLayout", [0.75f, 0.75f, 0f, 1f]);
        AccessFlags sourceAccess = default;
        PipelineStageFlags sourceStage = default;
        switch (oldLayout)
        {
            case ImageLayout.TransferDstOptimal: sourceAccess = AccessFlags.TransferWriteBit; break;
            case ImageLayout.TransferSrcOptimal: sourceAccess = AccessFlags.TransferReadBit; break;
            case ImageLayout.Preinitialized: sourceAccess = AccessFlags.HostWriteBit; break;
            case ImageLayout.General:
            case ImageLayout.Undefined: break;
            case ImageLayout.ShaderReadOnlyOptimal: sourceAccess = AccessFlags.ShaderReadBit; break;
            default: PvrRuntime.Verify(false); break;
        }
        switch (oldLayout)
        {
            case ImageLayout.General:
            case ImageLayout.Preinitialized: sourceStage = PipelineStageFlags.HostBit; break;
            case ImageLayout.TransferDstOptimal:
            case ImageLayout.TransferSrcOptimal: sourceStage = PipelineStageFlags.TransferBit; break;
            case ImageLayout.Undefined: sourceStage = PipelineStageFlags.TopOfPipeBit; break;
            case ImageLayout.ShaderReadOnlyOptimal: sourceStage = PipelineStageFlags.FragmentShaderBit; break;
            default: PvrRuntime.Verify(false); break;
        }

        AccessFlags destinationAccess = default;
        PipelineStageFlags destinationStage = default;
        switch (newLayout)
        {
            case ImageLayout.ColorAttachmentOptimal: destinationAccess = AccessFlags.ColorAttachmentWriteBit; break;
            case ImageLayout.DepthStencilAttachmentOptimal:
                destinationAccess = AccessFlags.DepthStencilAttachmentReadBit | AccessFlags.DepthStencilAttachmentWriteBit;
                break;
            case ImageLayout.General: break;
            case ImageLayout.ShaderReadOnlyOptimal: destinationAccess = AccessFlags.ShaderReadBit; break;
            case ImageLayout.TransferSrcOptimal: destinationAccess = AccessFlags.TransferReadBit; break;
            case ImageLayout.TransferDstOptimal: destinationAccess = AccessFlags.TransferWriteBit; break;
            case ImageLayout.DepthStencilReadOnlyOptimal: destinationAccess = AccessFlags.DepthStencilAttachmentReadBit; break;
            default: PvrRuntime.Verify(false); break;
        }
        switch (newLayout)
        {
            case ImageLayout.ColorAttachmentOptimal: destinationStage = PipelineStageFlags.ColorAttachmentOutputBit; break;
            case ImageLayout.DepthStencilAttachmentOptimal: destinationStage = PipelineStageFlags.EarlyFragmentTestsBit; break;
            case ImageLayout.General: destinationStage = PipelineStageFlags.HostBit; break;
            case ImageLayout.ShaderReadOnlyOptimal: destinationStage = PipelineStageFlags.FragmentShaderBit; break;
            case ImageLayout.TransferDstOptimal:
            case ImageLayout.TransferSrcOptimal: destinationStage = PipelineStageFlags.TransferBit; break;
            case ImageLayout.DepthStencilReadOnlyOptimal:
                destinationStage = PipelineStageFlags.EarlyFragmentTestsBit | PipelineStageFlags.LateFragmentTestsBit;
                break;
            default: PvrRuntime.Verify(false); break;
        }

        ImageAspectFlags aspect;
        if (newLayout is ImageLayout.DepthStencilAttachmentOptimal or ImageLayout.DepthStencilReadOnlyOptimal)
        {
            aspect = ImageAspectFlags.DepthBit;
            if (format is Format.D32SfloatS8Uint or Format.D24UnormS8Uint or Format.D16UnormS8Uint)
                aspect |= ImageAspectFlags.StencilBit;
        }
        else
        {
            aspect = ImageAspectFlags.ColorBit;
        }

        var barrier = new ImageMemoryBarrier
        {
            SType = StructureType.ImageMemoryBarrier,
            SrcAccessMask = sourceAccess, DstAccessMask = destinationAccess,
            OldLayout = oldLayout, NewLayout = newLayout,
            SrcQueueFamilyIndex = uint.MaxValue, DstQueueFamilyIndex = uint.MaxValue,
            Image = image, SubresourceRange = new ImageSubresourceRange(aspect, 0, levels, 0, 1)
        };
        PvrVk.PipelineBarrier(commandBuffer, sourceStage, destinationStage, barrier);
    }
}

internal sealed unsafe partial class PvrVulkanTexture
{
    internal override void UploadToGpu(int width, int height, byte* data, bool mipmapped, bool mipmapsIncluded = false)
    {
        Format format = Format.Undefined;
        uint dataSize = (uint)(width * height * 2);
        switch (TextureType)
        {
            case PvrTextureType._5551: format = Format.R5G5B5A1UnormPack16; break;
            case PvrTextureType._565: format = Format.R5G6B5UnormPack16; break;
            case PvrTextureType._4444: format = Format.R4G4B4A4UnormPack16; break;
            case PvrTextureType._8888: format = Format.R8G8B8A8Unorm; dataSize *= 2; break;
            case PvrTextureType._8: format = Format.R8Unorm; dataSize /= 2; break;
        }
        if (mipmapsIncluded)
        {
            int w = width / 2;
            uint size = dataSize / 4;
            while (w != 0)
            {
                dataSize += ((size + 3) >> 2) << 2;
                size /= 4;
                w /= 2;
            }
        }
        bool isNew = true;
        if (width != (int)Extent.Width || height != (int)Extent.Height || format != Format || ImageOwner == null)
            Init((uint)width, (uint)height, format, dataSize, mipmapped, mipmapsIncluded);
        else
            isNew = false;
        SetImage(dataSize, data, isNew, mipmapped && !mipmapsIncluded);
    }

    private void Init(uint width, uint height, Format format, uint dataSize, bool mipmapped, bool mipmapsIncluded)
    {
        Extent = new Extent2D(width, height);
        Format = format;
        MipmapLevels = 1;
        if (mipmapped)
            MipmapLevels += (uint)Math.Floor(Math.Log2(Math.Max(width, height)));

        PvrVulkanContext.Instance.Api.GetPhysicalDeviceFormatProperties(_physicalDevice, format, out FormatProperties properties);
        ImageTiling tiling = (properties.OptimalTilingFeatures & FormatFeatureFlags.SampledImageBit) == FormatFeatureFlags.SampledImageBit
            ? ImageTiling.Optimal : ImageTiling.Linear;
#if !OSX
        if (height <= 32 && dataSize / height <= 64 && !mipmapped
            && (properties.LinearTilingFeatures & FormatFeatureFlags.SampledImageBit) == FormatFeatureFlags.SampledImageBit)
            tiling = ImageTiling.Linear;
#endif
        NeedsStaging = tiling != ImageTiling.Linear;
        ImageLayout initialLayout;
        ImageUsageFlags usage = ImageUsageFlags.SampledBit;
        if (NeedsStaging)
        {
            StagingBuffer?.Dispose();
            StagingBuffer = new PvrVulkanBuffer(dataSize, BufferUsageFlags.TransferSrcBit);
            usage |= ImageUsageFlags.TransferDstBit;
            initialLayout = ImageLayout.Undefined;
        }
        else
        {
            PvrRuntime.Verify((properties.LinearTilingFeatures & FormatFeatureFlags.SampledImageBit) == FormatFeatureFlags.SampledImageBit);
            initialLayout = ImageLayout.Preinitialized;
        }
        if (mipmapped && !mipmapsIncluded)
            usage |= ImageUsageFlags.TransferSrcBit | ImageUsageFlags.TransferDstBit;
        CreateImage(tiling, usage, initialLayout, ImageAspectFlags.ColorBit);
    }

    internal void CreateImage(ImageTiling tiling, ImageUsageFlags usage, ImageLayout initialLayout, ImageAspectFlags aspect)
    {
        UsageFlags = usage;
        var imageInfo = new ImageCreateInfo
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = ImageType.Type2D, Format = Format, Extent = new Extent3D(Extent.Width, Extent.Height, 1),
            MipLevels = MipmapLevels, ArrayLayers = 1, Samples = SampleCountFlags.Count1Bit,
            Tiling = tiling, Usage = usage, SharingMode = SharingMode.Exclusive, InitialLayout = initialLayout
        };
        ImageOwner?.Dispose();
        ImageOwner = Device!.CreateImageUnique(imageInfo);

        var allocationInfo = new VmaAllocationCreateInfo
        {
            Usage = NeedsStaging ? VmaMemoryUsage.GpuOnly : VmaMemoryUsage.CpuToGpu
        };
#if !OSX
        if (!NeedsStaging)
            allocationInfo.Flags = VmaAllocationCreateFlags.MappedBit;
#endif
        Allocation?.Dispose();
        Allocation = PvrVulkanContext.Instance.GetAllocator().AllocateForImage(ImageOwner.Handle, allocationInfo);

        var viewInfo = new ImageViewCreateInfo
        {
            SType = StructureType.ImageViewCreateInfo,
            Image = ImageOwner.Handle, ViewType = ImageViewType.Type2D, Format = Format,
            SubresourceRange = new ImageSubresourceRange(aspect, 0, MipmapLevels, 0, 1)
        };
        ImageViewOwner?.Dispose();
        ImageViewOwner = Device.CreateImageViewUnique(viewInfo);
#if VK_DEBUG
        string name = $"texture @ {StartAddress:x}";
        PvrVulkanContext.Instance.SetObjectName(ImageOwner.Handle, name);
        PvrVulkanContext.Instance.SetObjectName(ImageViewOwner.Handle, name);
#endif
    }

    private void SetImage(uint size, void* sourceData, bool isNew, bool generateMipmaps)
    {
        PvrRuntime.Verify(CommandBuffer.Handle != 0);
        using var scope = new PvrVulkanCommandBufferDebugScope(CommandBuffer, "SetImage", [1f, 1f, 0f, 1f]);
        if (!isNew && !NeedsStaging)
            PvrVulkanImageLayout.SetImageLayout(CommandBuffer, GetImage(), Format, MipmapLevels,
                ImageLayout.ShaderReadOnlyOptimal, ImageLayout.General);

        void* data;
        if (NeedsStaging)
        {
            StagingBuffer ??= new PvrVulkanBuffer(size, BufferUsageFlags.TransferSrcBit);
            data = StagingBuffer.MapMemory();
        }
        else
        {
            data = Allocation!.MapMemory();
        }
        PvrRuntime.Verify(data != null);

        bool packedMipmaps = MipmapLevels > 1 && !generateMipmaps && TextureType != PvrTextureType._8888;
        try
        {
            if (packedMipmaps)
            {
                byte* src = (byte*)sourceData;
                byte* dst = (byte*)data;
                for (uint i = 0; i < MipmapLevels; i++)
                {
                    uint levelSize = (1u << (int)(2 * i)) * 2;
                    System.Buffer.MemoryCopy(src, dst, levelSize, levelSize);
                    dst += ((levelSize + 3) >> 2) << 2;
                    src += levelSize;
                }
            }
            else if (!NeedsStaging)
            {
                SubresourceLayout layout = Device!.GetImageSubresourceLayout(GetImage(), new ImageSubresource(ImageAspectFlags.ColorBit, 0, 0));
                if (layout.Size != size)
                {
                    byte* src = (byte*)sourceData;
                    byte* dst = (byte*)data;
                    uint sourceSize = Extent.Width * 2;
                    if (TextureType == PvrTextureType._8888)
                        sourceSize *= 2;
                    else if (TextureType == PvrTextureType._8)
                        sourceSize /= 2;
                    byte* end = src + sourceSize * Extent.Height;
                    for (; src < end; src += sourceSize, dst += layout.RowPitch)
                        System.Buffer.MemoryCopy(src, dst, sourceSize, sourceSize);
                }
                else
                {
                    System.Buffer.MemoryCopy(sourceData, data, size, size);
                }
            }
            else
            {
                System.Buffer.MemoryCopy(sourceData, data, size, size);
            }
        }
        finally
        {
            if (NeedsStaging)
                StagingBuffer!.UnmapMemory();
            else if (!packedMipmaps)
                Allocation!.UnmapMemory();
        }

        if (NeedsStaging)
        {
            PvrVulkanImageLayout.SetImageLayout(CommandBuffer, GetImage(), Format, MipmapLevels,
                isNew ? ImageLayout.Undefined : ImageLayout.ShaderReadOnlyOptimal, ImageLayout.TransferDstOptimal);

            if (MipmapLevels > 1 && !generateMipmaps)
            {
                ulong offset = 0;
                for (uint i = 0; i < MipmapLevels; i++)
                {
                    uint side = 1u << (int)i;
                    var region = new BufferImageCopy(offset, side, side,
                        new ImageSubresourceLayers(ImageAspectFlags.ColorBit, MipmapLevels - i - 1, 0, 1),
                        default, new Extent3D(side, side, 1));
                    PvrVk.CopyBufferToImage(CommandBuffer, StagingBuffer!.Buffer, GetImage(), ImageLayout.TransferDstOptimal, region);
                    uint levelSize = (1u << (int)(2 * i)) * (TextureType == PvrTextureType._8888 ? 4u : 2u);
                    offset += ((levelSize + 3) >> 2) << 2;
                }
            }
            else
            {
                var region = new BufferImageCopy(0, Extent.Width, Extent.Height,
                    new ImageSubresourceLayers(ImageAspectFlags.ColorBit, 0, 0, 1), default,
                    new Extent3D(Extent.Width, Extent.Height, 1));
                PvrVk.CopyBufferToImage(CommandBuffer, StagingBuffer!.Buffer, GetImage(), ImageLayout.TransferDstOptimal, region);
                if (MipmapLevels > 1)
                    GenerateMipmaps();
            }
            PvrVulkanImageLayout.SetImageLayout(CommandBuffer, GetImage(), Format, MipmapLevels,
                ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal);
        }
        else
        {
            if (MipmapLevels > 1)
                GenerateMipmaps();
            else
                PvrVulkanImageLayout.SetImageLayout(CommandBuffer, GetImage(), Format, MipmapLevels,
                    isNew ? ImageLayout.Preinitialized : ImageLayout.General, ImageLayout.ShaderReadOnlyOptimal);
        }
    }

    private void GenerateMipmaps()
    {
        using var scope = new PvrVulkanCommandBufferDebugScope(CommandBuffer, "GenerateMipmaps", [0.75f, 0.75f, 0f, 1f]);
        uint mipWidth = Extent.Width;
        uint mipHeight = Extent.Height;
        var barrier = new ImageMemoryBarrier
        {
            SType = StructureType.ImageMemoryBarrier,
            SrcAccessMask = AccessFlags.TransferWriteBit, DstAccessMask = AccessFlags.TransferReadBit,
            OldLayout = ImageLayout.TransferDstOptimal, NewLayout = ImageLayout.TransferSrcOptimal,
            SrcQueueFamilyIndex = uint.MaxValue, DstQueueFamilyIndex = uint.MaxValue, Image = GetImage(),
            SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, 1)
        };
        for (uint i = 1; i < MipmapLevels; i++)
        {
            barrier.SubresourceRange.BaseMipLevel = i - 1;
            if (i == 1 && !NeedsStaging)
            {
                barrier.OldLayout = ImageLayout.Preinitialized;
                barrier.SrcAccessMask = AccessFlags.HostWriteBit;
            }
            else
            {
                barrier.OldLayout = ImageLayout.TransferDstOptimal;
                barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
            }
            barrier.NewLayout = ImageLayout.TransferSrcOptimal;
            barrier.DstAccessMask = AccessFlags.TransferReadBit;
            PvrVk.PipelineBarrier(CommandBuffer, PipelineStageFlags.TransferBit, PipelineStageFlags.TransferBit, barrier);

            ImageBlit blit = default;
            blit.SrcSubresource = new ImageSubresourceLayers(ImageAspectFlags.ColorBit, i - 1, 0, 1);
            blit.SrcOffsets[0] = default;
            blit.SrcOffsets[1] = new Offset3D((int)mipWidth, (int)mipHeight, 1);
            blit.DstSubresource = new ImageSubresourceLayers(ImageAspectFlags.ColorBit, i, 0, 1);
            blit.DstOffsets[0] = default;
            blit.DstOffsets[1] = new Offset3D((int)Math.Max(mipWidth / 2, 1u), (int)Math.Max(mipHeight / 2, 1u), 1);
            PvrVk.BlitImage(CommandBuffer, GetImage(), ImageLayout.TransferSrcOptimal,
                GetImage(), ImageLayout.TransferDstOptimal, blit, Filter.Linear);

            barrier.OldLayout = ImageLayout.TransferSrcOptimal;
            barrier.NewLayout = ImageLayout.ShaderReadOnlyOptimal;
            barrier.SrcAccessMask = AccessFlags.TransferReadBit;
            barrier.DstAccessMask = AccessFlags.ShaderReadBit;
            PvrVk.PipelineBarrier(CommandBuffer, PipelineStageFlags.TransferBit, PipelineStageFlags.FragmentShaderBit, barrier);
            mipWidth = Math.Max(mipWidth / 2, 1u);
            mipHeight = Math.Max(mipHeight / 2, 1u);
        }
        barrier.SubresourceRange.BaseMipLevel = MipmapLevels - 1;
        barrier.OldLayout = ImageLayout.TransferDstOptimal;
        barrier.NewLayout = ImageLayout.ShaderReadOnlyOptimal;
        barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
        barrier.DstAccessMask = AccessFlags.ShaderReadBit;
        PvrVk.PipelineBarrier(CommandBuffer, PipelineStageFlags.TransferBit, PipelineStageFlags.FragmentShaderBit, barrier);
    }

    internal void DeferDeleteResource(IPvrVulkanFlightManager manager)
    {
        manager.AddToFlight(new ResourceDeleter(this));
    }

    private sealed class ResourceDeleter : IPvrVulkanDeletable
    {
        internal ResourceDeleter(PvrVulkanTexture texture)
        {
            (_image, texture.ImageOwner) = (texture.ImageOwner, _image);
            (_view, texture.ImageViewOwner) = (texture.ImageViewOwner, _view);
            (_buffer, texture.StagingBuffer) = (texture.StagingBuffer, _buffer);
            (_allocation, texture.Allocation) = (texture.Allocation, _allocation);
        }

        public void Dispose()
        {
            try
            {
                _allocation?.Dispose();
                _buffer?.Dispose();
            }
            finally
            {
                _view?.Dispose();
                _image?.Dispose();
            }
        }

        private PvrVulkanOwned<Image>? _image;
        private PvrVulkanOwned<ImageView>? _view;
        private PvrVulkanBuffer? _buffer;
        private PvrVulkanAllocation? _allocation;
    }
}

internal sealed partial class PvrVulkanFramebufferAttachment
{
    internal void Init(uint width, uint height, Format format, ImageUsageFlags usage, string name = "")
    {
        _format = format;
        _extent = new Extent2D(width, height);
        bool depth = format is Format.D32SfloatS8Uint or Format.D24UnormS8Uint or Format.D16UnormS8Uint;
        if ((usage & ImageUsageFlags.TransferSrcBit) != 0)
        {
            _stagingBuffer?.Dispose();
            _stagingBuffer = new PvrVulkanBuffer(width * height * 4, BufferUsageFlags.TransferSrcBit | BufferUsageFlags.TransferDstBit,
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCachedBit | MemoryPropertyFlags.HostCoherentBit);
        }

        var createInfo = new ImageCreateInfo
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = ImageType.Type2D, Format = format, Extent = new Extent3D(width, height, 1),
            MipLevels = 1, ArrayLayers = 1, Samples = SampleCountFlags.Count1Bit, Tiling = ImageTiling.Optimal,
            Usage = usage, SharingMode = SharingMode.Exclusive, InitialLayout = ImageLayout.Undefined
        };
        _image?.Dispose();
        _image = _device.CreateImageUnique(createInfo);
#if VK_DEBUG
        if (name.Length != 0)
            PvrVulkanContext.Instance.SetObjectName(_image.Handle, name);
#endif
        var allocationInfo = new VmaAllocationCreateInfo { Usage = VmaMemoryUsage.GpuOnly };
        if ((usage & ImageUsageFlags.TransientAttachmentBit) != 0)
            allocationInfo.PreferredFlags = MemoryPropertyFlags.LazilyAllocatedBit;
        _allocation?.Dispose();
        _allocation = PvrVulkanContext.Instance.GetAllocator().AllocateForImage(_image.Handle, allocationInfo);

        if ((usage & (ImageUsageFlags.ColorAttachmentBit | ImageUsageFlags.DepthStencilAttachmentBit)) != 0)
        {
            var viewInfo = new ImageViewCreateInfo
            {
                SType = StructureType.ImageViewCreateInfo,
                Image = _image.Handle, ViewType = ImageViewType.Type2D, Format = format,
                SubresourceRange = new ImageSubresourceRange(depth ? ImageAspectFlags.DepthBit : ImageAspectFlags.ColorBit, 0, 1, 0, 1)
            };
            _imageView?.Dispose();
            _imageView = _device.CreateImageViewUnique(viewInfo);
#if VK_DEBUG
            if (name.Length != 0)
                PvrVulkanContext.Instance.SetObjectName(_imageView.Handle, name);
#endif
            if ((usage & ImageUsageFlags.DepthStencilAttachmentBit) != 0 && (usage & ImageUsageFlags.InputAttachmentBit) != 0)
            {
                viewInfo.SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.StencilBit, 0, 1, 0, 1);
                _stencilView?.Dispose();
                _stencilView = _device.CreateImageViewUnique(viewInfo);
#if VK_DEBUG
                if (name.Length != 0)
                    PvrVulkanContext.Instance.SetObjectName(_stencilView.Handle, name);
#endif
            }
        }
    }
}

internal sealed partial class PvrVulkanTextureCache
{
    internal void Cleanup()
    {
        var ids = new List<ulong>();
        uint targetFrame = Math.Max(120u, PvrRenderer.FrameCount) - 120;
        foreach ((ulong id, PvrVulkanTexture texture) in Cache)
        {
            if (texture.Dirty != 0 && texture.Dirty < targetFrame)
                ids.Add(id);
            if (ids.Count > 5)
                break;
        }
        foreach (ulong id in ids)
            if (ClearTexture(Cache[id]))
                Cache.Remove(id);
    }
}
