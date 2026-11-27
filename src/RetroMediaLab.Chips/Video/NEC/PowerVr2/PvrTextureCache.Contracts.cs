using System.Diagnostics;
using static RetroMediaLab.Chips.Video.NEC.PowerVr2.PvrRegisters;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal sealed class PvrVramBlock
{
    internal uint Start;
    internal uint End;
    internal PvrBaseTextureCacheData Texture = null!;
}

internal abstract unsafe partial class PvrBaseTextureCacheData
{
    protected PvrBaseTextureCacheData(PvrBaseTextureCacheData other)
    {
        Tsp = other.Tsp;
        Tcw = other.Tcw;
        TextureType = other.TextureType;
        StartAddress = other.StartAddress;
        Dirty = other.Dirty;
        (LockBlock, other.LockBlock) = (other.LockBlock, LockBlock);
        MipmapStartAddress = other.MipmapStartAddress;
        Width = other.Width;
        Height = other.Height;
        Size = other.Size;
        _format = other._format;
        _convert = other._convert;
        _convert32 = other._convert32;
        _convert8 = other._convert8;
        Updates = other.Updates;
        PaletteHash = other.PaletteHash;
        TextureHash = other.TextureHash;
        OldVqTextureHash = other.OldVqTextureHash;
        OldTextureHash = other.OldTextureHash;
        byte* imageData = CustomImageData;
        CustomImageData = other.CustomImageData;
        other.CustomImageData = imageData;
        CustomWidth = other.CustomWidth;
        CustomHeight = other.CustomHeight;
        CustomLoadInProgress = 0;
        GpuPalette = other.GpuPalette;
        Area = other.Area;
    }

    internal TSP Tsp;
    internal TCW Tcw;
    internal PvrTextureType TextureType;
    internal uint StartAddress;
    internal uint Dirty;
    internal PvrVramBlock? LockBlock;
    internal uint MipmapStartAddress;
    internal ushort Width;
    internal ushort Height;
    internal uint Size;
    private PvrTextureInfo _format;
    private PvrTextureConverter<ushort>? _convert;
    private PvrTextureConverter<uint>? _convert32;
    private PvrTextureConverter<byte>? _convert8;
    internal uint Updates;
    internal uint PaletteHash;
    internal uint TextureHash;
    internal uint OldVqTextureHash;
    internal uint OldTextureHash;
    internal byte* CustomImageData;
    internal uint CustomWidth;
    internal uint CustomHeight;
    internal int CustomLoadInProgress;
    internal bool IsCustomReplaced;
    internal bool GpuPalette;
    internal byte Area;

    internal abstract string GetId();

    internal bool IsPaletted() => (PixelFormat)Tcw.PixelFmt is PixelFormat.PixelPal4 or PixelFormat.PixelPal8;

    internal bool IsMipmapped() => Tcw.MipMapped != 0 && Tcw.ScanOrder == 0 && PvrConfiguration.UseMipmaps;

    internal string GetPixelFormatName() => (PixelFormat)Tcw.PixelFmt switch
    {
        PixelFormat.Pixel1555 => "1555",
        PixelFormat.Pixel565 => "565",
        PixelFormat.Pixel4444 => "4444",
        PixelFormat.PixelYUV => "yuv",
        PixelFormat.PixelBumpMap => "bumpmap",
        PixelFormat.PixelPal4 => "pal4",
        PixelFormat.PixelPal8 => "pal8",
        _ => "unknown"
    };

    internal bool IsCustomTextureAvailable() =>
        Volatile.Read(ref CustomLoadInProgress) == 0 && CustomImageData != null;

    internal abstract void UploadToGpu(int width, int height, byte* tempTextureBuffer, bool mipmapped,
        bool mipmapsIncluded = false);

    internal virtual bool Force32BitTexture(PvrTextureType type) => false;

    internal static bool IsGpuHandledPaletted(TSP tsp, TCW tcw, int area) =>
        (PixelFormat)tcw.PixelFmt is (PixelFormat.PixelPal4 or PixelFormat.PixelPal8)
        && PvrConfiguration.TextureUpscale == 1
        && !PvrConfiguration.DumpTextures
        && !PvrCustomTexture.Instance.Enabled()
        && tsp.FilterMode <= 1
        && tcw.MipMapped == 0
        && tcw.VQ_Comp == 0
        && area == 0;

    internal partial void PrintTextureName();
    internal partial void ComputeHash();
    internal partial bool Update();
    internal partial void CheckCustomTexture();
    internal partial bool NeedsUpdate();
    internal partial void ProtectVram();
    internal partial void UnprotectVram();
    internal partial void Invalidate();
    internal static partial void SetDirectXColorOrder(bool enabled);
}

internal abstract class PvrBaseTextureCache<T> where T : PvrBaseTextureCacheData
{
    protected readonly Dictionary<ulong, T> Cache = new();

    private const uint TspTextureCacheMask = 0x3f;
    private const uint TcwTextureCacheMask = 0xfc1fffff;
    private const uint TcwPalTextureCacheMask = 0xf81fffff;

    protected abstract T CreateTexture(TSP tsp, TCW tcw, int area);

    internal T GetTextureCacheData(TSP tsp, TCW tcw, int area)
    {
        ulong key = tsp.full & TspTextureCacheMask;
        if ((PixelFormat)tcw.PixelFmt is PixelFormat.PixelPal4 or PixelFormat.PixelPal8)
        {
            if (PvrBaseTextureCacheData.IsGpuHandledPaletted(tsp, tcw, area))
                key |= (ulong)(tcw.full & TcwPalTextureCacheMask) << 32;
            else
                key |= ((ulong)tcw.full << 32) | ((PAL_RAM_CTRL & 3u) << 6) | (tsp.FilterMode != 0 ? 1u << 8 : 0u);
        }
        else
            key |= (ulong)(tcw.full & TcwTextureCacheMask) << 32;

        if (Cache.TryGetValue(key, out T? texture))
            texture.Tcw.StrideSel = tcw.StrideSel;
        else
        {
            texture = CreateTexture(tsp, tcw, area);
            Cache.Add(key, texture);
        }
        return texture;
    }

    internal T GetRenderToTexture(uint address, uint packMode, uint width, uint height)
    {
        TCW tcw = new() { TexAddr = address >> 3, StrideSel = 0, ScanOrder = 1 };
        switch (packMode)
        {
            case 0:
            case 3:
                tcw.PixelFmt = (uint)PixelFormat.Pixel1555;
                break;
            case 1:
                tcw.PixelFmt = (uint)PixelFormat.Pixel565;
                break;
            case 2:
                tcw.PixelFmt = (uint)PixelFormat.Pixel4444;
                break;
        }
        TSP tsp = default;
        uint u;
        uint v;
        for (u = 0; u <= 7 && (8u << (int)u) < width; u++)
        {
        }
        for (v = 0; v <= 7 && (8u << (int)v) < height; v++)
        {
        }
        tsp.TexU = u;
        tsp.TexV = v;
        return GetTextureCacheData(tsp, tcw, 0);
    }

    internal void CollectCleanup()
    {
        List<ulong> list = [];
        uint targetFrame = Math.Max(120u, PvrRenderer.FrameCount) - 120;
        foreach ((ulong id, T texture) in Cache)
        {
            if (texture.Dirty != 0 && texture.Dirty < targetFrame)
                list.Add(id);
            if (list.Count > 5)
                break;
        }
        foreach (ulong id in list)
        {
            if (Cache[id].Delete())
                Cache.Remove(id);
        }
    }

    internal void Clear()
    {
        foreach (T texture in Cache.Values)
            texture.Delete();

        Cache.Clear();
        Trace.TraceInformation("Texture cache cleared");
    }
}

internal static unsafe partial class PvrTextureCache
{
    internal static partial bool VramLockedWriteOffset(nuint offset);
    internal static partial bool VramLockedWrite(byte* address);
    internal static partial void UpscaleXbrz(int factor, uint* source, uint* destination, int width, int height, bool hasAlpha);
    internal static partial void ReadFramebuffer<TPacker>(in PvrFramebufferInfo info, PvrPixelBuffer<uint> pixels, out int width, out int height)
        where TPacker : IPvrColorPacker;
    internal static partial void WriteFramebuffer(uint width, uint height, byte* data, uint address, FB_W_CTRL_type control,
        uint lineStride, in PvrRect clip, int red = 0, int green = 1, int blue = 2, int alpha = 3);
    internal static partial void WriteTextureToVram(uint width, uint height, byte* data, ushort* destination, FB_W_CTRL_type control,
        uint lineStride, in PvrRect clip, int red = 0, int green = 1, int blue = 2, int alpha = 3);

    internal static void MakeFogTexture(byte* texture)
    {
        byte* fogTable = (byte*)FOG_TABLE;
        for (int i = 0; i < 128; i++)
        {
            texture[i] = fogTable[i * 4];
            texture[i + 128] = fogTable[i * 4 + 1];
        }
    }

#if TEST_AUTOMATION
    internal static partial void DumpScreenshot(byte* buffer, uint width, uint height, bool alpha = false,
        uint rowPitch = 0, bool invertY = false);
#endif
}
