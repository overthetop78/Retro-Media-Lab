using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Vulkan;

internal sealed unsafe partial class PvrVulkanTexture : PvrBaseTextureCacheData, IDisposable
{
    internal PvrVulkanTexture(TSP tsp = default, TCW tcw = default, int area = 0) : base(tsp, tcw, area)
    {
        _physicalDevice = PvrVulkanContext.Instance.GetPhysicalDevice();
        Device = PvrVulkanContext.Instance.GetDevice();
    }

    internal PvrVulkanTexture(PvrVulkanTexture other) : base(other)
    {
        (Format, other.Format) = (other.Format, Format);
        (Extent, other.Extent) = (other.Extent, Extent);
        (MipmapLevels, other.MipmapLevels) = (other.MipmapLevels, MipmapLevels);
        (NeedsStaging, other.NeedsStaging) = (other.NeedsStaging, NeedsStaging);
        (StagingBuffer, other.StagingBuffer) = (other.StagingBuffer, StagingBuffer);
        (CommandBuffer, other.CommandBuffer) = (other.CommandBuffer, CommandBuffer);
        (Allocation, other.Allocation) = (other.Allocation, Allocation);
        (ImageOwner, other.ImageOwner) = (other.ImageOwner, ImageOwner);
        (ImageViewOwner, other.ImageViewOwner) = (other.ImageViewOwner, ImageViewOwner);
        (ReadOnlyImageView, other.ReadOnlyImageView) = (other.ReadOnlyImageView, ReadOnlyImageView);
        (_physicalDevice, other._physicalDevice) = (other._physicalDevice, _physicalDevice);
        (Device, other.Device) = (other.Device, Device);
    }

    internal ulong GetIntId()
    {
        PvrVulkanTexture self = this;
        return (ulong)Unsafe.As<PvrVulkanTexture, nuint>(ref self);
    }
    internal override string GetId() => $"0x{GetIntId():x}";
    internal ImageView GetImageView() => ImageViewOwner!.Handle;
    internal Image GetImage() => ImageOwner!.Handle;
    internal ImageView GetReadOnlyImageView() => ReadOnlyImageView.Handle != 0 ? ReadOnlyImageView : GetImageView();
    internal void SetCommandBuffer(CommandBuffer commandBuffer) => CommandBuffer = commandBuffer;
    internal override bool Force32BitTexture(PvrTextureType type) => !PvrVulkanContext.Instance.IsFormatSupported(type);
    internal Extent2D GetSize() => Extent;

    public void Dispose()
    {
        try
        {
            Delete();
        }
        finally
        {
            try
            {
                ImageViewOwner?.Dispose();
                ImageOwner?.Dispose();
            }
            finally
            {
                StagingBuffer?.Dispose();
                Allocation?.Dispose();
                ImageViewOwner = null;
                ImageOwner = null;
                StagingBuffer = null;
                Allocation = null;
            }
        }
    }

    internal Format Format = Format.Undefined;
    internal Extent2D Extent;
    internal uint MipmapLevels = 1;
    internal ImageUsageFlags UsageFlags;
    internal bool NeedsStaging;
    internal PvrVulkanBuffer? StagingBuffer;
    internal CommandBuffer CommandBuffer;
    internal PvrVulkanAllocation? Allocation;
    internal PvrVulkanOwned<Image>? ImageOwner;
    internal PvrVulkanOwned<ImageView>? ImageViewOwner;
    internal ImageView ReadOnlyImageView;
    private PhysicalDevice _physicalDevice;
    internal PvrVulkanDevice? Device;
}

internal sealed class PvrVulkanSamplerManager
{
    internal void Term()
    {
        try
        {
            foreach (var sampler in _samplers.Values)
                sampler.Dispose();
        }
        finally
        {
            _samplers.Clear();
        }
    }

    internal Sampler GetSampler(in PolyParam poly, bool punchThrough, bool texture1 = false)
    {
        TSP tsp = texture1 ? poly.Tsp1 : poly.Tsp;
        if (poly.Texture != null && poly.Texture.GpuPalette)
            tsp.FilterMode = 0;
        else if (PvrConfiguration.TextureFiltering == 1)
            tsp.FilterMode = 0;
        else if (PvrConfiguration.TextureFiltering == 2)
            tsp.FilterMode = 1;
        return GetSampler(tsp, punchThrough);
    }

    internal Sampler GetSampler(TSP tsp, bool punchThrough = false)
    {
        uint hash = (tsp.full & TspMask) | (punchThrough ? 1u : 0u);
        if (_samplers.TryGetValue(hash, out var cached))
            return cached.Handle;
        Filter filter = tsp.FilterMode == 0 ? Filter.Nearest : Filter.Linear;
        SamplerAddressMode uRepeat = tsp.ClampU != 0 ? SamplerAddressMode.ClampToEdge
            : tsp.FlipU != 0 ? SamplerAddressMode.MirroredRepeat : SamplerAddressMode.Repeat;
        SamplerAddressMode vRepeat = tsp.ClampV != 0 ? SamplerAddressMode.ClampToEdge
            : tsp.FlipV != 0 ? SamplerAddressMode.MirroredRepeat : SamplerAddressMode.Repeat;
        SamplerAddressMode wRepeat = vRepeat;
        PvrVulkanContext context = PvrVulkanContext.Instance;
        bool anisotropic = PvrConfiguration.AnisotropicFiltering > 1 && context.SupportsSamplerAnisotropy()
            && filter == Filter.Linear && !punchThrough;
#if OSX
        const float mipLodBias = 0;
#else
        float mipLodBias = PvrTextureCache.DAdjustLodBias[tsp.MipMapD] - 1f;
#endif
        SamplerMipmapMode mode = !punchThrough && filter == Filter.Linear ? SamplerMipmapMode.Linear : SamplerMipmapMode.Nearest;
        var sampler = context.GetDevice().CreateSamplerUnique(new SamplerCreateInfo
        {
            SType = StructureType.SamplerCreateInfo,
            MagFilter = filter, MinFilter = filter, MipmapMode = mode,
            AddressModeU = uRepeat, AddressModeV = vRepeat, AddressModeW = wRepeat,
            MipLodBias = mipLodBias, AnisotropyEnable = anisotropic,
            MaxAnisotropy = Math.Min(PvrConfiguration.AnisotropicFiltering, context.GetMaxSamplerAnisotropy()),
            CompareOp = CompareOp.Never, MinLod = 0f, MaxLod = 1000f, BorderColor = BorderColor.FloatOpaqueBlack
        });
        _samplers.Add(hash, sampler);
        return sampler.Handle;
    }

    internal const uint TspMask = 0x7ef00;
    private readonly SortedDictionary<uint, PvrVulkanOwned<Sampler>> _samplers = [];
}

internal sealed partial class PvrVulkanFramebufferAttachment(PhysicalDevice physicalDevice, PvrVulkanDevice device) : IDisposable
{
    internal void Reset()
    {
        try
        {
            _image?.Dispose();
        }
        finally
        {
            _image = null;
            _imageView?.Dispose();
            _imageView = null;
        }
    }

    public void Dispose()
    {
        try
        {
            _stencilView?.Dispose();
            _imageView?.Dispose();
            _image?.Dispose();
        }
        finally
        {
            _allocation?.Dispose();
            _stagingBuffer?.Dispose();
            _stencilView = null;
            _imageView = null;
            _image = null;
            _allocation = null;
            _stagingBuffer = null;
        }
    }

    internal ImageView GetImageView() => _imageView!.Handle;
    internal Image GetImage() => _image!.Handle;
    internal PvrVulkanBuffer? GetBufferData() => _stagingBuffer;
    internal ImageView GetStencilView() => _stencilView!.Handle;
    internal Extent2D GetExtent() => _extent;

    private Format _format = Format.Undefined;
    private Extent2D _extent;
    private PvrVulkanBuffer? _stagingBuffer;
    private PvrVulkanAllocation? _allocation;
    private PvrVulkanOwned<Image>? _image;
    private PvrVulkanOwned<ImageView>? _imageView;
    private PvrVulkanOwned<ImageView>? _stencilView;
    private readonly PhysicalDevice _physicalDevice = physicalDevice;
    private readonly PvrVulkanDevice _device = device;
}

internal sealed partial class PvrVulkanTextureCache : PvrBaseTextureCache<PvrVulkanTexture>
{
    internal PvrVulkanTextureCache()
    {
        PvrBaseTextureCacheData.SetDirectXColorOrder(false);
    }

    protected override PvrVulkanTexture CreateTexture(TSP tsp, TCW tcw, int area) => new(tsp, tcw, area);

    internal void SetCurrentIndex(int index)
    {
        if (index == (int)_currentIndex)
            return;
        if (_currentIndex < _inFlightTextures.Count)
            foreach (PvrVulkanTexture texture in _inFlightTextures[(int)_currentIndex])
                texture.ReadOnlyImageView = default;
        _currentIndex = (uint)index;
        EmptyTrash(_inFlightTextures);
    }

    internal bool IsInFlight(PvrVulkanTexture texture, bool previous)
    {
        for (int i = 0; i < _inFlightTextures.Count; i++)
            if ((!previous || i != _currentIndex) && _inFlightTextures[i].Contains(texture))
                return true;
        return false;
    }

    internal void SetInFlight(PvrVulkanTexture texture)
    {
        _inFlightTextures[(int)_currentIndex].Add(texture);
    }

    internal new void Clear()
    {
        PvrVulkanContext context = PvrVulkanContext.Instance;
        foreach (var set in _inFlightTextures)
        {
            foreach (PvrVulkanTexture texture in set)
                texture.DeferDeleteResource(context);
            set.Clear();
        }
        base.Clear();
    }

    private bool ClearTexture(PvrVulkanTexture texture)
    {
        foreach (var set in _inFlightTextures)
            set.Remove(texture);
        return texture.Delete();
    }

    private void EmptyTrash(List<HashSet<PvrVulkanTexture>> textures)
    {
        if (textures.Count < _currentIndex + 1)
        {
            while (textures.Count < _currentIndex + 1)
                textures.Add([]);
        }
        else
        {
            textures[(int)_currentIndex].Clear();
        }
    }

    private readonly List<HashSet<PvrVulkanTexture>> _inFlightTextures = [];
    private uint _currentIndex = uint.MaxValue;
}
