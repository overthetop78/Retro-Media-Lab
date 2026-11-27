using System;
using System.Collections.Generic;
using System.Globalization;
using Vortice.Direct3D9;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Direct3D9;

internal sealed unsafe partial class PvrDx9Texture : PvrBaseTextureCacheData
{
    internal PvrDx9Texture(TSP tsp = default, TCW tcw = default, int area = 0) : base(tsp, tcw, area)
    {
    }

    internal PvrDx9Texture(PvrDx9Texture other) : base(other)
    {
        (Texture, other.Texture) = (other.Texture, Texture);
    }

    internal IDirect3DTexture9? Texture;

    internal override string GetId() => ((nuint)(Texture?.NativePointer ?? 0)).ToString(CultureInfo.InvariantCulture);
}

internal sealed class PvrDx9TextureCache : PvrBaseTextureCache<PvrDx9Texture>, IDisposable
{
    internal PvrDx9TextureCache()
    {
        PvrBaseTextureCacheData.SetDirectXColorOrder(true);
    }

    public void Dispose()
    {
        try
        {
            Clear();
        }
        finally
        {
            foreach (IDirect3DTexture9 texture in _texturesToDelete)
                texture.Dispose();
            _texturesToDelete.Clear();
        }
    }

    internal void Cleanup()
    {
        foreach (IDirect3DTexture9 texture in _texturesToDelete)
            texture.Dispose();
        _texturesToDelete.Clear();
        CollectCleanup();
    }

    internal void DeleteLater(IDirect3DTexture9 texture)
    {
        texture.AddRef();
        _texturesToDelete.Add(new IDirect3DTexture9(texture.NativePointer));
    }

    protected override PvrDx9Texture CreateTexture(TSP tsp, TCW tcw, int area) => new(tsp, tcw, area);

    private readonly List<IDirect3DTexture9> _texturesToDelete = [];
}
