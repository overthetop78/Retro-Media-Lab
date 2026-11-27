using System.Diagnostics;
using RetroMediaLab.Chips.CPU.Hitachi.SH4;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Direct3D11;

internal sealed unsafe partial class PvrDx11Texture
{
    internal override void UploadToGpu(int width, int height, byte* tempTextureBuffer, bool mipmapped, bool mipmapsIncluded = false)
    {
        Texture2DDescription description = default;
        description.Width = (uint)width;
        description.Height = (uint)height;
        description.ArraySize = 1;
        description.SampleDescription.Count = 1;
        description.Usage = ResourceUsage.Default;
        description.BindFlags = BindFlags.ShaderResource;

        uint bytesPerPixel = 2;
        switch (TextureType)
        {
            case PvrTextureType._5551:
                description.Format = Format.B5G5R5A1_UNorm;
                break;
            case PvrTextureType._4444:
                description.Format = Format.B4G4R4A4_UNorm;
                break;
            case PvrTextureType._565:
                description.Format = Format.B5G6R5_UNorm;
                break;
            case PvrTextureType._8888:
                bytesPerPixel = 4;
                description.Format = Format.B8G8R8A8_UNorm;
                break;
            case PvrTextureType._8:
                bytesPerPixel = 1;
                description.Format = Format.A8_UNorm;
                break;
            default:
                return;
        }

        int mipmapLevels = 1;
        if (mipmapsIncluded)
        {
            mipmapLevels = 0;
            int dimension = width;
            while (dimension != 0)
            {
                mipmapLevels++;
                dimension >>= 1;
            }
        }
        description.MipLevels = (uint)mipmapLevels;

        if (Texture is not null)
        {
            Texture2DDescription current = Texture.Description;
            if (description.Width != current.Width || description.Height != current.Height
                || description.Format != current.Format || description.MipLevels != current.MipLevels)
            {
                TextureView?.Dispose();
                TextureView = null;
                Texture.Dispose();
                Texture = null;
            }
        }

        if (Texture is null)
        {
            if (mipmapped && !mipmapsIncluded)
            {
                description.MipLevels = 0;
                description.MiscFlags = ResourceOptionFlags.GenerateMips;
                description.BindFlags |= BindFlags.RenderTarget;
                mipmapLevels = 1;
            }

            if (PvrDx11Context.Instance.Device.CreateTexture2D(description, default, out Texture).Success)
            {
                ShaderResourceViewDescription viewDescription = default;
                viewDescription.Format = description.Format;
                viewDescription.ViewDimension = ShaderResourceViewDimension.Texture2D;
                viewDescription.Texture2D.MipLevels = description.MipLevels == 0 ? uint.MaxValue : description.MipLevels;
                TextureView = PvrDx11Context.Instance.Device.CreateShaderResourceView(Texture!, viewDescription);
            }
            else
            {
                Trace.TraceError($"Texture creation failed type {(int)TextureType} dim {width}x{height} mipmap {(mipmapped ? 1 : 0)} (included {(mipmapsIncluded ? 1 : 0)})");
            }

            Sh4HostRuntime.Verify(Texture is not null, "texture != nullptr");
            Sh4HostRuntime.Verify(TextureView is not null, "textureView != nullptr");
        }

        for (int i = 0; i < mipmapLevels; i++)
        {
            uint w = mipmapLevels == 1 ? (uint)width : 1u << i;
            uint h = mipmapLevels == 1 ? (uint)height : 1u << i;
            PvrDx11Context.Instance.DeviceContext.UpdateSubresource(Texture!, (uint)(mipmapLevels - i - 1),
                null, (nint)tempTextureBuffer, w * bytesPerPixel, w * bytesPerPixel * h);
            tempTextureBuffer += (1u << (2 * i)) * bytesPerPixel;
        }
        if (mipmapped && !mipmapsIncluded)
            PvrDx11Context.Instance.DeviceContext.GenerateMips(TextureView!);
    }

#if !TARGET_UWP
    internal override bool Force32BitTexture(PvrTextureType type)
    {
        if (!PvrDx11Context.Instance.TextureFormatSupported(type))
            return true;
        if (OperatingSystem.IsWindowsVersionAtLeast(6, 2))
            return false;
        return type is PvrTextureType._565 or PvrTextureType._5551 or PvrTextureType._4444;
    }
#endif

    internal override bool Delete()
    {
        if (!base.Delete())
            return false;

        TextureView?.Dispose();
        TextureView = null;
        Texture?.Dispose();
        Texture = null;
        return true;
    }

    internal void LoadCustomTexture()
    {
        uint size = CustomWidth * CustomHeight;
        byte* pixel = CustomImageData;
        while (size-- != 0)
        {
            byte red = pixel[0];
            pixel[0] = pixel[2];
            pixel[2] = red;
            pixel += 4;
        }
        CheckCustomTexture();
    }
}

internal sealed partial class PvrDx11Samplers
{
    private static SharpGen.Runtime.Result CreateSampler(in SamplerDescription description, out ID3D11SamplerState? sampler) =>
        PvrDx11Context.Instance.Device.CreateSamplerState(description, out sampler);
}
