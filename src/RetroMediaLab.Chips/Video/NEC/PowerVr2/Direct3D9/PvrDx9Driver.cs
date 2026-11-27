using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Vortice.Direct3D9;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Direct3D9;

internal sealed unsafe class PvrDx9Driver : PvrImGuiDriver
{
    internal PvrDx9Driver(IDirect3DDevice9 device)
    {
        PvrImGuiDx9.Init(device);
    }

    public override void Dispose()
    {
        try
        {
            PvrImGuiDx9.Shutdown();
        }
        finally
        {
            try
            {
                foreach (Texture texture in _textures.Values)
                    texture.Dispose();
            }
            finally
            {
                _textures.Clear();
            }
        }
    }

    internal override void NewFrame() => PvrImGuiDx9.NewFrame();

    internal override void RenderDrawData(nint drawData, bool guiOpen)
    {
        PvrDx9Context.Instance.EndImGuiFrame();
        if (guiOpen)
            _frameRendered = true;
    }

    internal override void Present()
    {
        if (_frameRendered)
            PvrDx9Context.Instance.Present();
        _frameRendered = false;
    }

    internal override void SetFrameRendered() => _frameRendered = true;

    internal override nint GetTexture(string name) => _textures.TryGetValue(name, out Texture? texture) ? texture.Id : 0;

    internal override nint UpdateTexture(string name, byte* data, int width, int height, bool nearestSampling)
    {
        if (!_textures.TryGetValue(name, out Texture? texture))
        {
            texture = new Texture();
            _textures.Add(name, texture);
        }

        texture.Resource?.Dispose();
        texture.Resource = null;
        var result = PvrDx9Context.Instance.Device.CreateTexture((uint)width, (uint)height, 1,
            0, Format.A8R8G8B8, Pool.Managed, out texture.Resource, 0);
        if (result.Failure || texture.Resource == null)
        {
            PvrRuntime.Log.Warn("CreateTexture failed ({0} x {1}): error {2:x}", width, height, result.Code);
            DeleteTexture(name);
            return 0;
        }

        width *= 4;
        texture.Resource.LockRect(0, out LockedRectangle rect, null, 0);
        try
        {
            byte* destination = (byte*)rect.Bits;
            byte* source = data;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x += 4)
                {
                    destination[x] = source[x + 2];
                    destination[x + 1] = source[x + 1];
                    destination[x + 2] = source[x];
                    destination[x + 3] = source[x + 3];
                }
                destination += rect.Pitch;
                source += width;
            }
        }
        finally
        {
            texture.Resource.UnlockRect(0);
        }
        texture.Image.D3dTexture = texture.Resource;
        texture.Image.PointSampling = nearestSampling;
        return texture.Id;
    }

    internal override void DeleteTexture(string name)
    {
        if (_textures.Remove(name, out Texture? texture))
            texture.Dispose();
    }

    private bool _frameRendered;

    private sealed class Texture : IDisposable
    {
        internal IDirect3DTexture9? Resource;
        internal readonly PvrDx9ImGuiTexture Image = new();
        private GCHandle _handle;

        internal Texture()
        {
            _handle = GCHandle.Alloc(Image);
        }

        internal nint Id => GCHandle.ToIntPtr(_handle);

        public void Dispose()
        {
            try
            {
                Resource?.Dispose();
            }
            finally
            {
                if (_handle.IsAllocated)
                    _handle.Free();
            }
        }
    }

    private readonly Dictionary<string, Texture> _textures = [];
}
