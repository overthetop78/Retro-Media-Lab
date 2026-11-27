using System;
using Vortice.Direct3D9;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Direct3D9;

internal sealed unsafe partial class PvrDx9Texture
{
    internal override void UploadToGpu(int width, int height, byte* tempTextureBuffer, bool mipmapped, bool mipmapsIncluded = false)
    {
        Format format;
        uint bytesPerPixel = 2;
        switch (TextureType)
        {
            case PvrTextureType._5551:
                format = Format.A1R5G5B5;
                break;
            case PvrTextureType._4444:
                format = Format.A4R4G4B4;
                break;
            case PvrTextureType._565:
                format = Format.R5G6B5;
                break;
            case PvrTextureType._8888:
                bytesPerPixel = 4;
                format = Format.A8R8G8B8;
                break;
            case PvrTextureType._8:
                bytesPerPixel = 1;
                format = Format.A8;
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

        if (Texture != null)
        {
            if (Texture.GetLevelDesc(0, out SurfaceDescription description).Failure || description.Width != (uint)width
                || description.Height != (uint)height || description.Format != format || Texture.GetLevelCount() != (uint)mipmapLevels)
            {
                Texture.Dispose();
                Texture = null;
            }
        }

        LockedRectangle rect;
        while (true)
        {
            if (Texture == null)
            {
                uint levels = (uint)mipmapLevels;
                Usage usage = 0;
                if (mipmapped && !mipmapsIncluded)
                {
                    levels = 0;
                    usage = Usage.AutoGenerateMipMap;
                }

                PvrDx9Context.Instance.Device.CreateTexture((uint)width, (uint)height, levels, usage, format, Pool.Managed, out Texture, 0);
                PvrRuntime.Verify(Texture != null);
            }
            if (Texture!.LockRect((uint)(mipmapLevels - 1), out rect, null, 0).Success)
                break;
            Texture.GetLevelDesc(0, out SurfaceDescription description);
            if (description.Pool != Pool.Default)
                return;
            Texture.Dispose();
            Texture = null;
        }

        for (int i = 0; i < mipmapLevels; i++)
        {
            uint w = mipmapLevels == 1 ? (uint)width : 1u << i;
            uint h = mipmapLevels == 1 ? (uint)height : 1u << i;
            try
            {
                if (w * bytesPerPixel == (uint)rect.Pitch)
                {
                    uint size = w * bytesPerPixel * h;
                    Buffer.MemoryCopy(tempTextureBuffer, (void*)rect.Bits, size, size);
                }
                else
                {
                    byte* dst = (byte*)rect.Bits;
                    byte* src = tempTextureBuffer;
                    for (uint line = 0; line < h; line++)
                    {
                        Buffer.MemoryCopy(src, dst, w * bytesPerPixel, w * bytesPerPixel);
                        dst += rect.Pitch;
                        src += w * bytesPerPixel;
                    }
                }
            }
            finally
            {
                Texture.UnlockRect((uint)(mipmapLevels - i - 1));
            }
            tempTextureBuffer += (1u << (2 * i)) * bytesPerPixel;
            if (i < mipmapLevels - 1)
                if (Texture.LockRect((uint)(mipmapLevels - i - 2), out rect, null, 0).Failure)
                    break;
        }
    }

    internal override bool Delete()
    {
        if (!base.Delete())
            return false;
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
