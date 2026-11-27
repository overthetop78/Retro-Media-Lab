using System.Diagnostics;
using System.IO.Hashing;
using System.Runtime.InteropServices;
using RetroMediaLab.Chips.CPU.Hitachi.SH4;
using static RetroMediaLab.Chips.Video.NEC.PowerVr2.PvrRegisters;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal static unsafe partial class PvrTextureCache
{
    internal static readonly float[] DAdjustLodBias = [0f, -4f, -2f, -1f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f];

    private const uint PageSize = 4096;
    private const uint VramSizeMax = 16 * 1024 * 1024;
    private static List<PvrVramBlock?>[]? _vramLocks;
    internal static readonly object VramLock = new();

    internal static void InitializeVramLocks()
    {
        if (_vramLocks is null)
        {
            _vramLocks = new List<PvrVramBlock?>[VramSizeMax / PageSize];
            for (int i = 0; i < _vramLocks.Length; i++)
                _vramLocks[i] = [];
        }
    }

    internal static void RemoveVramLock(PvrVramBlock block)
    {
        uint start = block.Start / PageSize;
        uint end = block.End / PageSize;
        for (uint i = start; i <= end; i++)
        {
            List<PvrVramBlock?> list = _vramLocks![i];
            for (int slot = 0; slot < list.Count; slot++)
            {
                if (list[slot] == block)
                    list[slot] = null;
            }
        }
    }

    internal static void AddVramLock(PvrVramBlock block)
    {
        uint start = block.Start / PageSize;
        uint end = block.End / PageSize;
        for (uint i = start; i <= end; i++)
        {
            List<PvrVramBlock?> list = _vramLocks![i];
            if (list.Count == 0 || list.TrueForAll(static item => item is null))
                PvrRuntime.ProtectVram(i * PageSize, PageSize);
            int slot = list.IndexOf(null);
            if (slot >= 0)
                list[slot] = block;
            else
                list.Add(block);
        }
    }

    internal static partial bool VramLockedWriteOffset(nuint offset)
    {
        if (offset >= PvrMemory.VramSize || _vramLocks is null)
            return false;

        nuint index = offset / PageSize;
        List<PvrVramBlock?> list = _vramLocks[(int)index];
        lock (VramLock)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] is { } vramLock)
                {
                    vramLock.Texture.Invalidate();
                    if (list[i] is not null)
                    {
                        Trace.TraceError("Error : pvr is supposed to remove lock");
                        Sh4HostRuntime.Die("Invalid state");
                    }
                }
            }
            list.Clear();
            PvrRuntime.UnprotectVram((uint)offset & ~(PageSize - 1), PageSize);
        }
        return true;
    }

    internal static partial bool VramLockedWrite(byte* address)
    {
        uint offset = PvrRuntime.GetVramOffset(address);
        if (offset == uint.MaxValue)
            return false;
        return VramLockedWriteOffset(offset);
    }

#if OPENMP
    private static int GetThreadCount()
    {
        int count = Environment.ProcessorCount - 1;
        if (count < 1)
            count = 1;
        return Math.Min(count, PvrConfiguration.MaxThreads);
    }

    private static void Parallelize(Action<int, int> function, int start, int end)
    {
        int count = GetThreadCount();
        Parallel.For(0, count, new ParallelOptions { MaxDegreeOfParallelism = count }, thread =>
        {
            int chunk = (end - start) / count;
            function(start + chunk * thread, count == thread + 1 ? end : start + chunk * (thread + 1));
        });
    }
#endif

    private static readonly PvrXbrz.ScalerConfiguration XbrzConfig = new();

    internal static partial void UpscaleXbrz(int factor, uint* source, uint* destination, int width, int height, bool hasAlpha)
    {
#if OPENMP
        Parallelize((start, end) => PvrXbrz.Scale(factor, source, destination, width, height,
            hasAlpha ? PvrXbrz.ColorFormat.Argb : PvrXbrz.ColorFormat.Rgb, XbrzConfig, start, end), 0, height);
#else
        PvrXbrz.Scale(factor, source, destination, width, height,
            hasAlpha ? PvrXbrz.ColorFormat.Argb : PvrXbrz.ColorFormat.Rgb, XbrzConfig);
#endif
    }

    internal static readonly uint[] VqMipPoint =
    [
        PvrTextureConversion.VqCodebookSize + 0x00000,
        PvrTextureConversion.VqCodebookSize + 0x00001,
        PvrTextureConversion.VqCodebookSize + 0x00002,
        PvrTextureConversion.VqCodebookSize + 0x00006,
        PvrTextureConversion.VqCodebookSize + 0x00016,
        PvrTextureConversion.VqCodebookSize + 0x00056,
        PvrTextureConversion.VqCodebookSize + 0x00156,
        PvrTextureConversion.VqCodebookSize + 0x00556,
        PvrTextureConversion.VqCodebookSize + 0x01556,
        PvrTextureConversion.VqCodebookSize + 0x05556,
        PvrTextureConversion.VqCodebookSize + 0x15556
    ];

    internal static readonly uint[] OtherMipPoint =
    [
        0x00003, 0x00004, 0x00008, 0x00018, 0x00058, 0x00158, 0x00558, 0x01558, 0x05558, 0x15558, 0x55558
    ];
}

internal abstract unsafe partial class PvrBaseTextureCacheData
{
    private static readonly PvrTextureType[] PaletteTypes =
        [PvrTextureType._5551, PvrTextureType._565, PvrTextureType._4444, PvrTextureType._8888];

    internal partial void PrintTextureName()
    {
#if DEBUG || DEBUGFAST
        string name = $"Texture: {GetPixelFormatName()}";
        if (Tcw.VQ_Comp != 0)
            name += " VQ";
        else if (Tcw.ScanOrder == 0 || IsPaletted())
            name += " TW";
        else if (Tcw.StrideSel == 1 && !IsPaletted())
            name += " Stride";

        if (Tcw.ScanOrder == 0 && Tcw.MipMapped != 0)
            name += " MM";
        if (Tsp.FilterMode != 0)
            name += " Bilinear";

        name += $" {8 << (int)Tsp.TexU}x{8 << (int)Tsp.TexV} @ 0x{Tcw.TexAddr << 3:X}";
        name += $" id={GetId()}";
        Debug.WriteLine(name.Length > 511 ? name[..511] : name);
#endif
    }

    internal partial bool NeedsUpdate()
    {
        bool needsUpdate = Dirty != 0;
        if (TextureType != PvrTextureType._8)
        {
            if (Tcw.PixelFmt == (uint)PixelFormat.PixelPal4 && PaletteHash != PvrTextureConversion.PaletteHash16[Tcw.PalSelect])
                needsUpdate = true;
            else if (Tcw.PixelFmt == (uint)PixelFormat.PixelPal8
                && PaletteHash != PvrTextureConversion.PaletteHash256[Tcw.PalSelect >> 4])
                needsUpdate = true;
        }
        return needsUpdate;
    }

    internal partial void ProtectVram()
    {
        uint end = MipmapStartAddress + Size - 1;
        if (end >= PvrMemory.VramSize)
        {
            Trace.TraceWarning("protectVRam: end >= VRAM_SIZE. Tried to lock area out of vram");
            end = PvrMemory.VramSize - 1;
        }
        if (StartAddress > end)
        {
            Trace.TraceWarning("vramlock_Lock: startAddress > end. Tried to lock negative block");
            return;
        }

        var block = new PvrVramBlock { Start = StartAddress, End = end, Texture = this };
        lock (PvrTextureCache.VramLock)
        {
            if (LockBlock is null)
            {
                PvrTextureCache.AddVramLock(block);
                LockBlock = block;
            }
        }
    }

    internal partial void UnprotectVram()
    {
        lock (PvrTextureCache.VramLock)
        {
            if (LockBlock is { } block)
                PvrTextureCache.RemoveVramLock(block);
            LockBlock = null;
        }
    }

    internal virtual bool Delete()
    {
        UnprotectVram();
        if (Volatile.Read(ref CustomLoadInProgress) > 0)
            return false;

        NativeMemory.Free(CustomImageData);
        CustomImageData = null;
        return true;
    }

    protected PvrBaseTextureCacheData(TSP tsp, TCW tcw, int area)
    {
        PvrTextureCache.InitializeVramLocks();

        if (tcw.VQ_Comp == 1 && tcw.MipMapped == 1)
            tcw.ScanOrder = 0;
        Tsp = tsp;
        Tcw = tcw;
        Area = (byte)area;

        Updates = 0;
        Dirty = PvrRenderer.FrameCount;
        LockBlock = null;
        CustomImageData = null;
        CustomLoadInProgress = 0;
        GpuPalette = false;
        IsCustomReplaced = false;

        _format = PvrTextureConversion.Formats[tcw.PixelFmt == (uint)PixelFormat.PixelReserved
            ? (uint)PixelFormat.Pixel1555 : tcw.PixelFmt];
        StartAddress = (tcw.TexAddr << 3) & PvrMemory.VramMask;
        MipmapStartAddress = StartAddress;
        Width = (ushort)(8 << (int)tsp.TexU);
        Height = (ushort)(8 << (int)tsp.TexV);

        _convert8 = null;
        if (tcw.ScanOrder != 0 && _format.Pl32 is not null)
        {
            if (tcw.MipMapped != 0)
            {
                Trace.TraceWarning("Warning: planar texture with mipmaps (invalid)");
                Tcw.MipMapped = 0;
            }

            uint stride = Width;
            if (tcw.StrideSel != 0)
            {
                stride = (TEXT_CONTROL & 31) * 32;
                if (stride == 0)
                    stride = Width;
            }

            _convert = null;
            if (tcw.VQ_Comp != 0)
            {
                _convert32 = _format.PlVq32;
                MipmapStartAddress += PvrTextureConversion.VqCodebookSize;
                Size = stride * Height / 4;
            }
            else
            {
                _convert32 = _format.Pl32;
                Size = stride * Height * (uint)_format.BitsPerPixel / 8;
            }
        }
        else
        {
            if (!IsPaletted())
            {
                Tcw.ScanOrder = 0;
                Tcw.StrideSel = 0;
            }
            if (tcw.MipMapped != 0)
                Height = Width;

            if (tcw.VQ_Comp != 0)
            {
                Sh4HostRuntime.Verify(_format.Vq is not null || _format.Vq32 is not null, "tex->VQ != NULL || tex->VQ32 != NULL");
                if (tcw.MipMapped != 0)
                    MipmapStartAddress += PvrTextureCache.VqMipPoint[tsp.TexU + 3];
                else
                    MipmapStartAddress += PvrTextureConversion.VqCodebookSize;
                _convert = _format.Vq;
                _convert32 = _format.Vq32;
                Size = (uint)Width * Height / 4;
            }
            else
            {
                Sh4HostRuntime.Verify(_format.Tw is not null || _format.Tw32 is not null, "tex->TW != NULL || tex->TW32 != NULL");
                if (tcw.MipMapped != 0)
                    MipmapStartAddress += PvrTextureCache.OtherMipPoint[tsp.TexU + 3] * (uint)_format.BitsPerPixel / 8;
                _convert = _format.Tw;
                _convert32 = _format.Tw32;
                Size = (uint)Width * Height * (uint)_format.BitsPerPixel / 8;
                _convert8 = _format.Tw8;
            }
        }
    }

    internal partial void ComputeHash()
    {
        uint tcwMask = IsPaletted() ? 0xf8000000u : 0xfc000000u;
        if (Tcw.VQ_Comp != 0)
        {
            int oldSize = Width * Height / 8;
            OldVqTextureHash = XxHash32.HashToUInt32(
                new ReadOnlySpan<byte>(PvrMemory.VramPointer + MipmapStartAddress - PvrTextureConversion.VqCodebookSize, oldSize), 7);
            if (IsPaletted())
                OldVqTextureHash ^= PaletteHash;
            OldTextureHash = OldVqTextureHash;
            OldVqTextureHash ^= Tcw.full & tcwMask;

            var state = new XxHash32(7);
            state.Append(new ReadOnlySpan<byte>(PvrMemory.VramPointer + StartAddress, PvrTextureConversion.VqCodebookSize));
            state.Append(new ReadOnlySpan<byte>(PvrMemory.VramPointer + MipmapStartAddress, (int)Size));
            TextureHash = state.GetCurrentHashAsUInt32();
            if (IsPaletted())
                TextureHash ^= PaletteHash;
            TextureHash ^= Tcw.full & tcwMask;
        }
        else
        {
            OldVqTextureHash = 0;
            TextureHash = XxHash32.HashToUInt32(new ReadOnlySpan<byte>(PvrMemory.VramPointer + MipmapStartAddress, (int)Size), 7);
            if (IsPaletted())
                TextureHash ^= PaletteHash;
            OldTextureHash = TextureHash;
            TextureHash ^= Tcw.full & tcwMask;
        }
    }

    internal partial bool Update()
    {
        Updates++;
        Dirty = 0;
        GpuPalette = false;
        TextureType = _format.Type;

        bool hasAlpha = false;
        if (IsPaletted())
        {
            if (IsGpuHandledPaletted(Tsp, Tcw, Area))
            {
                TextureType = PvrTextureType._8;
                GpuPalette = true;
            }
            else
            {
                TextureType = PaletteTypes[PAL_RAM_CTRL & 3];
                if (TextureType != PvrTextureType._565)
                    hasAlpha = true;
            }

            if (Tcw.PixelFmt == (uint)PixelFormat.PixelPal4)
            {
                PaletteHash = PvrTextureConversion.PaletteHash16[Tcw.PalSelect];
                PvrTextureConversion.PaletteIndex = Tcw.PalSelect << 4;
            }
            else
            {
                PaletteHash = PvrTextureConversion.PaletteHash256[Tcw.PalSelect >> 4];
                PvrTextureConversion.PaletteIndex = (Tcw.PalSelect >> 4) << 8;
            }
        }

        byte* vram = PvrMemory.VramPointer;
        if (Tcw.VQ_Comp != 0)
            PvrTextureConversion.VqCodebook = vram + StartAddress;

        uint stride = Width;
        if (Tcw.StrideSel != 0 && Tcw.ScanOrder != 0 && _format.Pl32 is not null)
        {
            stride = (TEXT_CONTROL & 31) * 32;
            if (stride == 0)
                stride = Width;
        }

        uint heightLimit = Height;
        uint originalSize = Size;
        if (StartAddress > PvrMemory.VramSize || MipmapStartAddress + Size > PvrMemory.VramSize)
        {
            heightLimit = 0;
            if (MipmapStartAddress < PvrMemory.VramSize && MipmapStartAddress + Size > PvrMemory.VramSize
                && Tcw.ScanOrder != 0)
            {
                heightLimit = (PvrMemory.VramSize - MipmapStartAddress) * 8 / stride / (uint)_format.BitsPerPixel;
                Size = stride * heightLimit * (uint)_format.BitsPerPixel / 8;
            }
            if (heightLimit == 0)
            {
                Size = originalSize;
                Trace.TraceWarning($"Warning: invalid texture. Address {StartAddress:X8} {MipmapStartAddress:X8} size {Size}");
                Dirty = 1;
                UnprotectVram();
                return false;
            }
        }

        if (PvrCustomTexture.Instance.Enabled())
        {
            uint oldHash = TextureHash;
            ComputeHash();
            if (Updates > 1 && oldHash == TextureHash)
            {
                if (IsCustomReplaced)
                {
                    TextureType = PvrTextureType._8888;
                    GpuPalette = false;
                }
                ProtectVram();
                Size = originalSize;
                return true;
            }
            PvrCustomTexture.Instance.LoadCustomTextureAsync(this);
        }
        IsCustomReplaced = false;

        byte* temporaryTextureBuffer;
        uint upscaledWidth = Width;
        uint upscaledHeight = Height;

        using var pixels16 = new PvrPixelBuffer<ushort>();
        using var pixels32 = new PvrPixelBuffer<uint>();
        using var pixels8 = new PvrPixelBuffer<byte>();

        bool textureUpscaling = PvrConfiguration.TextureUpscale > 1
            && Width * Height <= PvrConfiguration.MaxFilteredTextureSize * PvrConfiguration.MaxFilteredTextureSize
            && Tcw.PixelFmt != (uint)PixelFormat.PixelYUV;
        bool need32BitBuffer = true;
        if (!textureUpscaling
            && (!IsPaletted() || TextureType != PvrTextureType._8888)
            && _convert is not null
            && !Force32BitTexture(TextureType))
            need32BitBuffer = false;

        bool mipmapped = IsMipmapped() && !PvrConfiguration.DumpTextures;

        if (_convert32 is { } convert32 && need32BitBuffer)
        {
            if (textureUpscaling)
                mipmapped = false;
            TextureType = PvrTextureType._8888;

            if (mipmapped)
            {
                pixels32.Init(Width, Height, true);
                for (int i = 0; i <= Tsp.TexU + 3u; i++)
                {
                    pixels32.SetMipmap(i);
                    uint address;
                    if (Tcw.VQ_Comp != 0)
                    {
                        address = StartAddress + PvrTextureCache.VqMipPoint[i];
                        if (i == 0)
                        {
                            using var levelZero = new PvrPixelBuffer<uint>();
                            levelZero.Init(2, 2, false);
                            if (Tcw.PixelFmt == (uint)PixelFormat.PixelYUV)
                                address = StartAddress + PvrTextureCache.VqMipPoint[1];
                            convert32(levelZero, vram + address, 2, 2);
                            *pixels32.Data() = *levelZero.Data(1, 1);
                            continue;
                        }
                    }
                    else
                        address = StartAddress + PvrTextureCache.OtherMipPoint[i] * (uint)_format.BitsPerPixel / 8;

                    if (Tcw.PixelFmt == (uint)PixelFormat.PixelYUV && i == 0)
                        PvrTextureConversion.Formats[(int)PixelFormat.Pixel565].Tw32!(pixels32, vram + address, 1, 1);
                    else
                        convert32(pixels32, vram + address, 1u << i, 1u << i);
                }
                pixels32.SetMipmap(0);
            }
            else
            {
                pixels32.Init(Width, Height);
                convert32(pixels32, vram + MipmapStartAddress, stride, heightLimit);
                if (textureUpscaling)
                {
                    int factor = Math.Clamp(PvrConfiguration.TextureUpscale, 2, 6);
                    using var temporary = new PvrPixelBuffer<uint>();
                    temporary.Init(Width * (uint)factor, Height * (uint)factor);
                    if ((PixelFormat)Tcw.PixelFmt is PixelFormat.Pixel1555 or PixelFormat.Pixel4444)
                        hasAlpha = true;
                    PvrTextureCache.UpscaleXbrz(factor, pixels32.Data(), temporary.Data(), Width, Height, hasAlpha);
                    pixels32.StealData(temporary);
                    upscaledWidth *= (uint)factor;
                    upscaledHeight *= (uint)factor;
                }
            }
            temporaryTextureBuffer = (byte*)pixels32.Data();
        }
        else if (_convert8 is { } convert8 && TextureType == PvrTextureType._8)
        {
            if (mipmapped)
            {
                pixels8.Init(Width, Height, true);
                for (int i = 0; i <= Tsp.TexU + 3u; i++)
                {
                    pixels8.SetMipmap(i);
                    uint address = StartAddress + PvrTextureCache.OtherMipPoint[i] * (uint)_format.BitsPerPixel / 8;
                    convert8(pixels8, vram + address, 1u << i, 1u << i);
                }
                pixels8.SetMipmap(0);
            }
            else
            {
                pixels8.Init(Width, Height);
                convert8(pixels8, vram + MipmapStartAddress, stride, Height);
            }
            temporaryTextureBuffer = pixels8.Data();
        }
        else if (_convert is { } convert16)
        {
            if (mipmapped)
            {
                pixels16.Init(Width, Height, true);
                for (int i = 0; i <= Tsp.TexU + 3u; i++)
                {
                    pixels16.SetMipmap(i);
                    uint address;
                    if (Tcw.VQ_Comp != 0)
                    {
                        address = StartAddress + PvrTextureCache.VqMipPoint[i];
                        if (i == 0)
                        {
                            using var levelZero = new PvrPixelBuffer<ushort>();
                            levelZero.Init(2, 2, false);
                            convert16(levelZero, vram + address, 2, 2);
                            *pixels16.Data() = *levelZero.Data(1, 1);
                            continue;
                        }
                    }
                    else
                        address = StartAddress + PvrTextureCache.OtherMipPoint[i] * (uint)_format.BitsPerPixel / 8;

                    convert16(pixels16, vram + address, 1u << i, 1u << i);
                }
                pixels16.SetMipmap(0);
            }
            else
            {
                pixels16.Init(Width, Height);
                convert16(pixels16, vram + MipmapStartAddress, stride, heightLimit);
            }
            temporaryTextureBuffer = (byte*)pixels16.Data();
        }
        else
        {
            Trace.TraceWarning("UNHANDLED TEXTURE");
            pixels16.Init(Width, Height);
            new Span<byte>(pixels16.Data(), Width * Height * 2).Fill(0x80);
            temporaryTextureBuffer = (byte*)pixels16.Data();
            mipmapped = false;
        }

        ProtectVram();

        UploadToGpu((int)upscaledWidth, (int)upscaledHeight, temporaryTextureBuffer, IsMipmapped(), mipmapped);
        if (PvrConfiguration.DumpTextures)
        {
            ComputeHash();
            PvrCustomTexture.Instance.DumpTexture(this, (int)upscaledWidth, (int)upscaledHeight, temporaryTextureBuffer);
            Trace.TraceInformation($"Dumped texture {TextureHash:x}.png. Old hash {OldTextureHash:x}");
        }
        PrintTextureName();
        Size = originalSize;

        return true;
    }

    internal partial void CheckCustomTexture()
    {
        if (IsCustomTextureAvailable())
        {
            TextureType = PvrTextureType._8888;
            GpuPalette = false;
            IsCustomReplaced = true;
            UploadToGpu((int)CustomWidth, (int)CustomHeight, CustomImageData, IsMipmapped(), false);
            NativeMemory.Free(CustomImageData);
            CustomImageData = null;
        }
    }

    internal static partial void SetDirectXColorOrder(bool enabled)
    {
        PvrTextureConversion.Formats = enabled ? PvrTextureConversion.DirectX.Formats : PvrTextureConversion.OpenGl.Formats;
        PalNeedsUpdate = true;
    }

    internal partial void Invalidate()
    {
        Dirty = PvrRenderer.FrameCount;
        PvrTextureCache.RemoveVramLock(LockBlock!);
        LockBlock = null;
    }
}

internal static unsafe partial class PvrTextureCache
{
    internal static partial void ReadFramebuffer<TPacker>(in PvrFramebufferInfo info, PvrPixelBuffer<uint> pixels, out int width, out int height)
        where TPacker : IPvrColorPacker
    {
        width = ((int)info.ReadSize.fb_x_size + 1) * 2;
        height = (int)info.ReadSize.fb_y_size + 1;
        int modulus = ((int)info.ReadSize.fb_modulus - 1) * 2;

        int bytesPerPixel;
        switch ((fb_depth_enum)info.ReadControl.fb_depth)
        {
            case fb_depth_enum.fbde_0555:
            case fb_depth_enum.fbde_565:
                bytesPerPixel = 2;
                break;
            case fb_depth_enum.fbde_888:
                bytesPerPixel = 3;
                width = width * 2 / 3;
                modulus = modulus * 2 / 3;
                break;
            case fb_depth_enum.fbde_C888:
                bytesPerPixel = 4;
                width /= 2;
                modulus /= 2;
                break;
            default:
                Sh4HostRuntime.Die("Invalid framebuffer format\n");
                bytesPerPixel = 4;
                break;
        }

        uint address = info.ReadStart1;
        if (info.SpgControl.interlace != 0)
        {
            if (width == modulus && info.ReadStart2 == unchecked(info.ReadStart1 + (uint)(width * bytesPerPixel)))
            {
                modulus = 0;
                height *= 2;
            }
            else
                address = info.SpgStatus.fieldnum != 0 ? info.ReadStart2 : info.ReadStart1;
        }
        else if (info.ReadControl.vclk_div == 0)
            height = Math.Min(height, 240);

        pixels.Init((uint)width, (uint)height);
        uint* destination = pixels.Data();
        int concat = (int)info.ReadControl.fb_concat;

        switch ((fb_depth_enum)info.ReadControl.fb_depth)
        {
            case fb_depth_enum.fbde_0555:
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        ushort source = PvrMemory.Read32<ushort>(address);
                        *destination++ = TPacker.Pack(
                            (((source >> 10) & 0x1f) << 3) | concat,
                            (((source >> 5) & 0x1f) << 3) | concat,
                            ((source & 0x1f) << 3) | concat,
                            0xff);
                        address += (uint)bytesPerPixel;
                    }
                    address += unchecked((uint)(modulus * bytesPerPixel));
                }
                break;
            case fb_depth_enum.fbde_565:
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        ushort source = PvrMemory.Read32<ushort>(address);
                        *destination++ = TPacker.Pack(
                            (((source >> 11) & 0x1f) << 3) | concat,
                            (((source >> 5) & 0x3f) << 2) | (concat & 3),
                            ((source & 0x1f) << 3) | concat,
                            0xff);
                        address += (uint)bytesPerPixel;
                    }
                    address += unchecked((uint)(modulus * bytesPerPixel));
                }
                break;
            case fb_depth_enum.fbde_888:
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x += 4)
                    {
                        uint source = PvrMemory.Read32<uint>(address);
                        *destination++ = TPacker.Pack((byte)(source >> 16), (byte)(source >> 8), (byte)source, 0xff);
                        address += 4;
                        if (x + 1 >= width)
                            break;
                        uint source2 = PvrMemory.Read32<uint>(address);
                        *destination++ = TPacker.Pack((byte)(source2 >> 8), (byte)source2, (byte)(source >> 24), 0xff);
                        address += 4;
                        if (x + 2 >= width)
                            break;
                        uint source3 = PvrMemory.Read32<uint>(address);
                        *destination++ = TPacker.Pack((byte)source3, (byte)(source2 >> 24), (byte)(source2 >> 16), 0xff);
                        address += 4;
                        if (x + 3 >= width)
                            break;
                        *destination++ = TPacker.Pack((byte)(source3 >> 24), (byte)(source3 >> 16), (byte)(source3 >> 8), 0xff);
                    }
                    address += unchecked((uint)(modulus * bytesPerPixel));
                }
                break;
            case fb_depth_enum.fbde_C888:
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        uint source = PvrMemory.Read32<uint>(address);
                        *destination++ = TPacker.Pack((byte)(source >> 16), (byte)(source >> 8), (byte)source, 0xff);
                        address += (uint)bytesPerPixel;
                    }
                    address += unchecked((uint)(modulus * bytesPerPixel));
                }
                break;
        }
    }

    private static byte RoundColor(byte value, int bits)
    {
        byte result = (byte)(value >> (8 - bits));
        if (result != 0xffu >> (8 - bits))
            result += (byte)((value >> (8 - bits - 1)) & 1);
        return result;
    }

    private interface IPixelWriter
    {
        void Write<T>(T pixel) where T : unmanaged;
        void Advance(int bytes);
    }

    private sealed class FramebufferPixelWriter(uint address) : IPixelWriter
    {
        private uint _address = address;

        public void Write<T>(T pixel) where T : unmanaged
        {
            PvrMemory.Write32(_address, pixel, internalWrite: true);
            _address += (uint)sizeof(T);
        }

        public void Advance(int bytes) => _address += unchecked((uint)bytes);
    }

    private sealed class TexturePixelWriter(ushort* destination) : IPixelWriter
    {
        private ushort* _destination = destination;

        public void Write<T>(T pixel) where T : unmanaged => *_destination++ = *(ushort*)&pixel;

        public void Advance(int bytes) => _destination = (ushort*)((byte*)_destination + bytes);
    }

    private delegate void LineWriter(int xmin, int xmax, ref byte* pixel, int y);

    private sealed class LineWriter0555(FB_W_CTRL_type control, IPixelWriter writer, int red, int green, int blue, bool round)
    {
        private readonly uint _kvalBit = (control.fb_kval & 0x80) << 8;

        internal const int BytesPerPixel = 2;

        internal void Write(int xmin, int xmax, ref byte* pixel, int y)
        {
            for (int c = xmin; c < xmax; c++)
            {
                byte r;
                byte g;
                byte b;
                if (round)
                {
                    r = RoundColor(pixel[red], 5);
                    g = RoundColor(pixel[green], 5);
                    b = RoundColor(pixel[blue], 5);
                }
                else
                {
                    r = (byte)(pixel[red] >> 3);
                    g = (byte)(pixel[green] >> 3);
                    b = (byte)(pixel[blue] >> 3);
                }
                writer.Write((ushort)(((uint)r << 10) | ((uint)g << 5) | b | _kvalBit));
                pixel += 4;
            }
        }
    }

    private sealed class LineWriter565(IPixelWriter writer, int red, int green, int blue, bool round)
    {
        internal const int BytesPerPixel = 2;

        internal void Write(int xmin, int xmax, ref byte* pixel, int y)
        {
            for (int c = xmin; c < xmax; c++)
            {
                byte r;
                byte g;
                byte b;
                if (round)
                {
                    r = RoundColor(pixel[red], 5);
                    g = RoundColor(pixel[green], 6);
                    b = RoundColor(pixel[blue], 5);
                }
                else
                {
                    r = (byte)(pixel[red] >> 3);
                    g = (byte)(pixel[green] >> 2);
                    b = (byte)(pixel[blue] >> 3);
                }
                writer.Write((ushort)((r << 11) | (g << 5) | b));
                pixel += 4;
            }
        }
    }

    private sealed class LineWriter4444(IPixelWriter writer, int red, int green, int blue, int alpha, bool round)
    {
        internal const int BytesPerPixel = 2;

        internal void Write(int xmin, int xmax, ref byte* pixel, int y)
        {
            for (int c = xmin; c < xmax; c++)
            {
                byte r;
                byte g;
                byte b;
                byte a;
                if (round)
                {
                    r = RoundColor(pixel[red], 4);
                    g = RoundColor(pixel[green], 4);
                    b = RoundColor(pixel[blue], 4);
                    a = RoundColor(pixel[alpha], 4);
                }
                else
                {
                    r = (byte)(pixel[red] >> 4);
                    g = (byte)(pixel[green] >> 4);
                    b = (byte)(pixel[blue] >> 4);
                    a = (byte)(pixel[alpha] >> 4);
                }
                writer.Write((ushort)((r << 8) | (g << 4) | b | (a << 12)));
                pixel += 4;
            }
        }
    }

    private sealed class LineWriter1555(FB_W_CTRL_type control, IPixelWriter writer,
        int red, int green, int blue, int alpha, bool round)
    {
        private readonly uint _alphaThreshold = control.fb_alpha_threshold;

        internal const int BytesPerPixel = 2;

        internal void Write(int xmin, int xmax, ref byte* pixel, int y)
        {
            for (int c = xmin; c < xmax; c++)
            {
                byte r;
                byte g;
                byte b;
                if (round)
                {
                    r = RoundColor(pixel[red], 5);
                    g = RoundColor(pixel[green], 5);
                    b = RoundColor(pixel[blue], 5);
                }
                else
                {
                    r = (byte)(pixel[red] >> 3);
                    g = (byte)(pixel[green] >> 3);
                    b = (byte)(pixel[blue] >> 3);
                }
                writer.Write((ushort)((r << 10) | (g << 5) | b | (pixel[alpha] >= _alphaThreshold ? 0x8000 : 0)));
                pixel += 4;
            }
        }
    }

    private sealed class LineWriter888(IPixelWriter writer, int red, int green, int blue)
    {
        internal const int BytesPerPixel = 3;

        internal void Write(int xmin, int xmax, ref byte* pixel, int y)
        {
            for (int c = xmin; c < xmax; c++)
            {
                writer.Write(pixel[blue]);
                writer.Write(pixel[green]);
                writer.Write(pixel[red]);
                pixel += 4;
            }
        }
    }

    private sealed class LineWriter0888(FB_W_CTRL_type control, IPixelWriter writer, int red, int green, int blue)
    {
        private readonly uint _kval = control.fb_kval << 24;

        internal const int BytesPerPixel = 4;

        internal void Write(int xmin, int xmax, ref byte* pixel, int y)
        {
            for (int c = xmin; c < xmax; c++)
            {
                writer.Write(((uint)pixel[red] << 16) | ((uint)pixel[green] << 8) | pixel[blue] | _kval);
                pixel += 4;
            }
        }
    }

    private sealed class LineWriter8888(IPixelWriter writer, int red, int green, int blue, int alpha)
    {
        internal const int BytesPerPixel = 4;

        internal void Write(int xmin, int xmax, ref byte* pixel, int y)
        {
            for (int c = xmin; c < xmax; c++)
            {
                writer.Write(((uint)pixel[red] << 16) | ((uint)pixel[green] << 8) | pixel[blue] | ((uint)pixel[alpha] << 24));
                pixel += 4;
            }
        }
    }

    private static void WriteTexture(uint width, uint height, byte* data, ushort* destination, FB_W_CTRL_type control,
        uint lineStride, in PvrRect clip, Func<FB_W_CTRL_type, IPixelWriter, LineWriter> createLineWriter)
    {
        int sourcePadding = 0;
        int destinationPadding = 0;
        if (lineStride == 0)
            lineStride = width * 2;
        else if (lineStride > width * 2u)
            destinationPadding = (int)(lineStride - width * 2);
        else
            sourcePadding = (int)((width * 2 - lineStride) * 2);
        height = Math.Min(height, unchecked((uint)(clip.BottomRight().Y + 1)));

        int xmin = clip.Origin.X;
        int xmax = Math.Min(clip.BottomRight().X + 1, (int)width);
        xmax = Math.Min(xmax, (int)(lineStride / 2));
        var pixelWriter = new TexturePixelWriter(destination + clip.Origin.Y * (width + destinationPadding / 2) + xmin);
        data += clip.Origin.Y * (width * 2 + sourcePadding) + xmin * 2;
        int deltaPixel = xmin + (int)Math.Min(lineStride / 2, width) - xmax;
        destinationPadding += deltaPixel * 2;
        sourcePadding += deltaPixel * 4;
        LineWriter lineWriter = createLineWriter(control, pixelWriter);
        for (uint line = unchecked((uint)clip.Origin.Y); line < height; line++)
        {
            lineWriter(xmin, xmax, ref data, (int)line);
            pixelWriter.Advance(destinationPadding);
            data += sourcePadding;
        }
    }

    internal static partial void WriteTextureToVram(uint width, uint height, byte* data, ushort* destination, FB_W_CTRL_type control,
        uint lineStride, in PvrRect clip, int red, int green, int blue, int alpha)
    {
        bool dither = control.fb_dither != 0 && PvrConfiguration.EmulateFramebuffer;
        switch (control.fb_packmode)
        {
            case 0:
                if (dither)
                    WriteTexture(width, height, data, destination, control, lineStride, clip,
                        (register, writer) => new LineWriter0555(register, writer, red, green, blue, false).Write);
                else
                    WriteTexture(width, height, data, destination, control, lineStride, clip,
                        (register, writer) => new LineWriter0555(register, writer, red, green, blue, true).Write);
                break;
            case 1:
                if (dither)
                    WriteTexture(width, height, data, destination, control, lineStride, clip,
                        (_, writer) => new LineWriter565(writer, red, green, blue, false).Write);
                else
                    WriteTexture(width, height, data, destination, control, lineStride, clip,
                        (_, writer) => new LineWriter565(writer, red, green, blue, true).Write);
                break;
            case 2:
                if (dither)
                    WriteTexture(width, height, data, destination, control, lineStride, clip,
                        (_, writer) => new LineWriter4444(writer, red, green, blue, alpha, false).Write);
                else
                    WriteTexture(width, height, data, destination, control, lineStride, clip,
                        (_, writer) => new LineWriter4444(writer, red, green, blue, alpha, true).Write);
                break;
            case 3:
                if (dither)
                    WriteTexture(width, height, data, destination, control, lineStride, clip,
                        (register, writer) => new LineWriter1555(register, writer, red, green, blue, alpha, false).Write);
                else
                    WriteTexture(width, height, data, destination, control, lineStride, clip,
                        (register, writer) => new LineWriter1555(register, writer, red, green, blue, alpha, true).Write);
                break;
            default:
                Trace.TraceError($"Invalid/unsupported texture format: {control.fb_packmode}");
                break;
        }
    }

    private static void WriteFramebufferLines(uint width, uint height, byte* data, uint address,
        FB_W_CTRL_type control, uint lineStride, in PvrRect clip, uint bytesPerPixel,
        Func<FB_W_CTRL_type, IPixelWriter, LineWriter> createLineWriter)
    {
        uint padding = lineStride;
        if (padding > width * bytesPerPixel)
            padding -= width * bytesPerPixel;
        else
            padding = 0;

        byte* pixel = data + 4 * clip.Origin.Y * width;
        address += bytesPerPixel * unchecked((uint)clip.Origin.Y) * (width + padding / bytesPerPixel);

        uint clipWidth = Math.Min(width, unchecked((uint)(clip.Origin.X + clip.Size.X)));
        height = Math.Min(height, unchecked((uint)(clip.Origin.Y + clip.Size.Y)));

        var pixelWriter = new FramebufferPixelWriter(address);
        LineWriter lineWriter = createLineWriter(control, pixelWriter);

        uint sourceAdvance1 = unchecked((uint)(4 * clip.Origin.X));
        uint destinationAdvance1 = bytesPerPixel * unchecked((uint)clip.Origin.X);
        uint destinationAdvance2 = padding + (width - clipWidth) * bytesPerPixel;
        uint sourceAdvance2 = (width - clipWidth) * 4;

        for (uint line = unchecked((uint)clip.Origin.Y); line < height; line++)
        {
            pixel += sourceAdvance1;
            pixelWriter.Advance((int)destinationAdvance1);

            lineWriter(clip.Origin.X, (int)clipWidth, ref pixel, (int)line);

            pixelWriter.Advance((int)destinationAdvance2);
            pixel += sourceAdvance2;
        }
    }

    internal static partial void WriteFramebuffer(uint width, uint height, byte* data, uint address,
        FB_W_CTRL_type control, uint lineStride, in PvrRect clip, int red, int green, int blue, int alpha)
    {
        switch (control.fb_packmode)
        {
            case 0:
                WriteFramebufferLines(width, height, data, address, control, lineStride, clip, LineWriter0555.BytesPerPixel,
                    (register, writer) => new LineWriter0555(register, writer, red, green, blue, false).Write);
                break;
            case 1:
                WriteFramebufferLines(width, height, data, address, control, lineStride, clip, LineWriter565.BytesPerPixel,
                    (_, writer) => new LineWriter565(writer, red, green, blue, false).Write);
                break;
            case 2:
                WriteFramebufferLines(width, height, data, address, control, lineStride, clip, LineWriter4444.BytesPerPixel,
                    (_, writer) => new LineWriter4444(writer, red, green, blue, alpha, false).Write);
                break;
            case 3:
                WriteFramebufferLines(width, height, data, address, control, lineStride, clip, LineWriter1555.BytesPerPixel,
                    (register, writer) => new LineWriter1555(register, writer, red, green, blue, alpha, false).Write);
                break;
            case 4:
                WriteFramebufferLines(width, height, data, address, control, lineStride, clip, LineWriter888.BytesPerPixel,
                    (_, writer) => new LineWriter888(writer, red, green, blue).Write);
                break;
            case 5:
                WriteFramebufferLines(width, height, data, address, control, lineStride, clip, LineWriter0888.BytesPerPixel,
                    (register, writer) => new LineWriter0888(register, writer, red, green, blue).Write);
                break;
            case 6:
                WriteFramebufferLines(width, height, data, address, control, lineStride, clip, LineWriter8888.BytesPerPixel,
                    (_, writer) => new LineWriter8888(writer, red, green, blue, alpha).Write);
                break;
            default:
                Trace.TraceError($"Invalid framebuffer format: {control.fb_packmode}");
                break;
        }
    }

#if TEST_AUTOMATION
    internal static partial void DumpScreenshot(byte* buffer, uint width, uint height, bool alpha, uint rowPitch, bool invertY)
    {
        PvrTextureImageCodec.FlipVerticallyOnWrite(invertY);
        PvrTextureImageCodec.WritePng("screenshot.png", (int)width, (int)height, alpha ? 4 : 3, buffer, (int)rowPitch);
    }
#endif
}
