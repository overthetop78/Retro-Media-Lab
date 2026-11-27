using System.Globalization;
using Vortice.Direct3D11;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Direct3D11;

internal sealed unsafe partial class PvrDx11Texture : PvrBaseTextureCacheData
{
    internal PvrDx11Texture(TSP tsp = default, TCW tcw = default, int area = 0) : base(tsp, tcw, area)
    {
    }

    internal PvrDx11Texture(PvrDx11Texture other) : base(other)
    {
        (Texture, other.Texture) = (other.Texture, Texture);
        (TextureView, other.TextureView) = (other.TextureView, TextureView);
    }

    internal ID3D11Texture2D? Texture;
    internal ID3D11ShaderResourceView? TextureView;

    internal override string GetId() => ((nuint)(Texture?.NativePointer ?? 0)).ToString(CultureInfo.InvariantCulture);
}

internal sealed class PvrDx11TextureCache : PvrBaseTextureCache<PvrDx11Texture>, IDisposable
{
    private readonly List<ID3D11Texture2D> _texturesToDelete = [];

    internal PvrDx11TextureCache()
    {
        PvrBaseTextureCacheData.SetDirectXColorOrder(true);
    }

    protected override PvrDx11Texture CreateTexture(TSP tsp, TCW tcw, int area) => new(tsp, tcw, area);

    public void Dispose()
    {
        try
        {
            Clear();
        }
        finally
        {
            foreach (ID3D11Texture2D texture in _texturesToDelete)
                texture.Dispose();
            _texturesToDelete.Clear();
        }
    }

    internal void Cleanup()
    {
        foreach (ID3D11Texture2D texture in _texturesToDelete)
            texture.Dispose();
        _texturesToDelete.Clear();
        CollectCleanup();
    }

    internal void DeleteLater(ID3D11Texture2D texture)
    {
        _texturesToDelete.Add(texture.QueryInterface<ID3D11Texture2D>());
    }
}

internal sealed partial class PvrDx11Samplers
{
    private readonly Dictionary<int, ID3D11SamplerState?> _samplers = [];

    internal ID3D11SamplerState? GetSampler(bool linear, bool clampU = true, bool clampV = true,
        bool flipU = false, bool flipV = false, bool punchThrough = false)
    {
        int hash = (clampU ? 1 : 0) | ((clampV ? 1 : 0) << 1) | ((flipU ? 1 : 0) << 2) |
            ((flipV ? 1 : 0) << 3) | ((linear ? 1 : 0) << 4) | ((punchThrough ? 1 : 0) << 5);
        if (!_samplers.TryGetValue(hash, out ID3D11SamplerState? sampler) || sampler is null)
        {
            SamplerDescription description = default;
            if (linear)
            {
                if (punchThrough)
                    description.Filter = Filter.MinMagLinearMipPoint;
                else if (PvrConfiguration.AnisotropicFiltering > 1)
                    description.Filter = Filter.Anisotropic;
                else
                    description.Filter = Filter.MinMagMipLinear;
            }
            else
            {
                description.Filter = Filter.MinMagMipPoint;
            }

            description.AddressU = clampU ? TextureAddressMode.Clamp : flipU ? TextureAddressMode.Mirror : TextureAddressMode.Wrap;
            description.AddressV = clampV ? TextureAddressMode.Clamp : flipV ? TextureAddressMode.Mirror : TextureAddressMode.Wrap;
            description.AddressW = TextureAddressMode.Wrap;
            description.ComparisonFunc = ComparisonFunction.Never;
            description.MaxAnisotropy = (uint)PvrConfiguration.AnisotropicFiltering;
            description.MaxLOD = float.MaxValue;
            description.MipLODBias = -1.5f;
            CreateSampler(description, out sampler);
            _samplers[hash] = sampler;
        }

        return sampler;
    }

    internal void Term()
    {
        foreach (ID3D11SamplerState? sampler in _samplers.Values)
            sampler?.Dispose();
        _samplers.Clear();
    }
}
