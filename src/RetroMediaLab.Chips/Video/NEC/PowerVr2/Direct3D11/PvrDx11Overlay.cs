using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Direct3D11;

internal sealed unsafe partial class PvrDx11Overlay
{
    internal void Draw(int width, int height, bool vmu, bool crosshair)
    {
        _deviceContext.RSSetScissorRect(new RawRect(0, 0, width, height));
        if (vmu)
        {
            float vmuPadding = 8f * PvrRuntime.Display.UiScale;
            float vmuHeight = 70f * PvrRuntime.Display.UiScale;
            float vmuWidth = 48f / 32f * vmuHeight;

            var blendFactor = new Color4(0.75f, 0.75f, 0.75f, 0.75f);
            _deviceContext.OMSetBlendState(_blendStates.GetState(true, 8, 8), blendFactor, uint.MaxValue);

            uint* data = stackalloc uint[48 * 32];
            for (int i = 0; i < _vmuTextures.Length; i++)
            {
                if (!PvrOsd.VmuLcdStatus[i])
                {
                    _vmuTextureViews[i]?.Dispose();
                    _vmuTextureViews[i] = null;
                    _vmuTextures[i]?.Dispose();
                    _vmuTextures[i] = null;
                    continue;
                }
                if (_vmuTextures[i] == null || _vmuLastChanged[i] != PvrOsd.VmuLastChanged[i])
                {
                    _vmuTextureViews[i]?.Dispose();
                    _vmuTextureViews[i] = null;
                    _vmuTextures[i]?.Dispose();
                    _vmuTextures[i] = null;
                    Texture2DDescription desc = default;
                    desc.Width = 48;
                    desc.Height = 32;
                    desc.ArraySize = 1;
                    desc.SampleDescription.Count = 1;
                    desc.Usage = ResourceUsage.Default;
                    desc.BindFlags = BindFlags.ShaderResource;
                    desc.Format = Format.R8G8B8A8_UNorm;
                    desc.MipLevels = 1;

                    if (_device.CreateTexture2D(desc, null, out _vmuTextures[i]).Success)
                    {
                        ShaderResourceViewDescription viewDesc = default;
                        viewDesc.Format = desc.Format;
                        viewDesc.ViewDimension = ShaderResourceViewDimension.Texture2D;
                        viewDesc.Texture2D.MipLevels = desc.MipLevels;
                        _device.CreateShaderResourceView(_vmuTextures[i], viewDesc, out _vmuTextureViews[i]);

                        for (int y = 0; y < 32; y++)
                            fixed (uint* source = &PvrOsd.VmuLcdData[i, (31 - y) * 48])
                                Buffer.MemoryCopy(source, data + y * 48, sizeof(uint) * 48, sizeof(uint) * 48);
                        _deviceContext.UpdateSubresource(_vmuTextures[i], 0, null, (nint)data, 48 * 4, 48 * 4 * 32);
                        _vmuLastChanged[i] = PvrOsd.VmuLastChanged[i];
                    }
                }

                float x;
                float y;
                float w = vmuWidth;
                float h = vmuHeight;
                if ((i & 2) != 0)
                    x = width - vmuPadding - vmuWidth;
                else
                    x = vmuPadding;
                if ((i & 4) != 0)
                {
                    y = height - vmuPadding - vmuHeight;
                    if ((i & 1) != 0)
                        y -= vmuPadding + vmuHeight;
                }
                else
                {
                    y = vmuPadding;
                    if ((i & 1) != 0)
                        y += vmuPadding + vmuHeight;
                }

                _deviceContext.RSSetViewport(new Viewport(x, y, w, h, 0f, 1f));
                _quad.Draw(_vmuTextureViews[i], _samplers.GetSampler(false));
            }
        }

        if (crosshair)
        {
            float* colors = stackalloc float[4];
            for (int i = 0; i < PvrConfiguration.CrosshairColor.Length; i++)
            {
                if (!PvrOsd.CrosshairNeeded(i))
                    continue;
                if (_xhairTexture == null)
                {
                    ReadOnlySpan<uint> texData = PvrOsd.GetCrosshairTextureData();
                    Texture2DDescription desc = default;
                    desc.Width = 16;
                    desc.Height = 16;
                    desc.ArraySize = 1;
                    desc.SampleDescription.Count = 1;
                    desc.Usage = ResourceUsage.Default;
                    desc.BindFlags = BindFlags.ShaderResource;
                    desc.Format = Format.R8G8B8A8_UNorm;
                    desc.MipLevels = 1;

                    if (_device.CreateTexture2D(desc, null, out _xhairTexture).Success)
                    {
                        ShaderResourceViewDescription viewDesc = default;
                        viewDesc.Format = desc.Format;
                        viewDesc.ViewDimension = ShaderResourceViewDimension.Texture2D;
                        viewDesc.Texture2D.MipLevels = desc.MipLevels;
                        _device.CreateShaderResourceView(_xhairTexture, viewDesc, out _xhairTextureView);

                        fixed (uint* source = texData)
                            _deviceContext.UpdateSubresource(_xhairTexture, 0, null, (nint)source, 16 * 4, 16 * 4 * 16);
                    }
                }

                (float x, float y) = PvrOsd.GetCrosshairPosition(i);
                float halfWidth = PvrConfiguration.CrosshairSize * PvrRuntime.Display.UiScale / 2f;
                float halfHeight = halfWidth;
                _deviceContext.RSSetViewport(new Viewport(x - halfWidth, y - halfHeight, halfWidth * 2, halfHeight * 2, 0f, 1f));

                uint color = PvrConfiguration.CrosshairColor[i];
                colors[0] = (color & 0xff) / 255f;
                colors[1] = ((color >> 8) & 0xff) / 255f;
                colors[2] = ((color >> 16) & 0xff) / 255f;
                colors[3] = ((color >> 24) & 0xff) / 255f;
                _deviceContext.OMSetBlendState(_blendStates.GetState(true, 4, 5, false, false), null, uint.MaxValue);
                _quad.Draw(_xhairTextureView, _samplers.GetSampler(false), colors);
            }
        }
    }
}
