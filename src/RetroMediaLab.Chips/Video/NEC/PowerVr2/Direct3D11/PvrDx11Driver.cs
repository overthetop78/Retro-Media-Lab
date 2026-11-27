using System.Runtime.InteropServices;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Direct3D11;

internal sealed unsafe class PvrDx11Driver : PvrImGuiDriver
{
    private sealed class Texture : IDisposable
    {
        internal ID3D11Texture2D? Resource;
        internal ID3D11ShaderResourceView? View;
        internal readonly PvrDx11ImGuiTexture Image = new();
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
                View?.Dispose();
            }
            finally
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
    }

    private bool _frameRendered;
    private readonly Dictionary<string, Texture> _textures = [];

    internal PvrDx11Driver(ID3D11Device device, ID3D11DeviceContext deviceContext)
    {
        PvrImGuiDx11.Init(device, deviceContext);
    }

    public override void Dispose()
    {
        try
        {
            PvrImGuiDx11.Shutdown();
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

    internal override void NewFrame() => PvrImGuiDx11.NewFrame();

    internal override void RenderDrawData(nint drawData, bool guiOpen)
    {
        PvrDx11Context.Instance.EndImGuiFrame();
        if (guiOpen)
            _frameRendered = true;
    }

    internal override void Present()
    {
        if (_frameRendered)
            PvrDx11Context.Instance.Present();
        _frameRendered = false;
    }

    internal override void SetFrameRendered()
    {
        _frameRendered = true;
    }

    internal override nint GetTexture(string name) => _textures.TryGetValue(name, out Texture? texture) ? texture.Id : 0;

    internal override nint UpdateTexture(string name, byte* data, int width, int height, bool nearestSampling)
    {
        if (!_textures.TryGetValue(name, out Texture? texture))
        {
            texture = new Texture();
            _textures.Add(name, texture);
        }

        try
        {
            texture.Resource?.Dispose();
        }
        finally
        {
            texture.Resource = null;
            texture.View?.Dispose();
            texture.View = null;
        }

        Texture2DDescription description = default;
        description.Width = (uint)width;
        description.Height = (uint)height;
        description.ArraySize = 1;
        description.SampleDescription.Count = 1;
        description.Usage = ResourceUsage.Default;
        description.BindFlags = BindFlags.ShaderResource;
        description.Format = Format.R8G8B8A8_UNorm;
        description.MipLevels = 1;
        texture.Resource = PvrDx11Context.Instance.Device.CreateTexture2D(description);

        ShaderResourceViewDescription viewDescription = default;
        viewDescription.Format = description.Format;
        viewDescription.ViewDimension = Vortice.Direct3D.ShaderResourceViewDimension.Texture2D;
        viewDescription.Texture2D.MipLevels = description.MipLevels;
        texture.View = PvrDx11Context.Instance.Device.CreateShaderResourceView(texture.Resource, viewDescription);

        PvrDx11Context.Instance.DeviceContext.UpdateSubresource(texture.Resource, 0, null, (nint)data, (uint)(width * 4), (uint)(width * 4 * height));
        texture.Image.ShaderResourceView = texture.View;
        texture.Image.PointSampling = nearestSampling;
        return texture.Id;
    }

    internal override void DeleteTexture(string name)
    {
        if (_textures.Remove(name, out Texture? texture))
            texture.Dispose();
    }
}
