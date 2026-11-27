using System.IO.Hashing;
using System.Numerics;
using static RetroMediaLab.Chips.Video.NEC.PowerVr2.PvrRegisters;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal static unsafe partial class PvrTextureConversion
{
    internal static byte* VqCodebook;
    internal static uint PaletteIndex;
    internal static readonly uint[] Palette16Ram = new uint[1024];
    internal static readonly uint[] Palette32Ram = new uint[1024];
    internal static readonly uint[] PaletteHash256 = new uint[4];
    internal static readonly uint[] PaletteHash16 = new uint[64];

    private static readonly uint[,,] Detwiddle = new uint[2, 11, 1024];

    static PvrTextureConversion()
    {
        const uint xSize = 1024;
        for (int s = 0; s < 11; s++)
        {
            uint ySize = 1u << s;
            for (uint i = 0; i < xSize; i++)
            {
                Detwiddle[0, s, i] = TwiddleSlow(i, 0, xSize, ySize);
                Detwiddle[1, s, i] = TwiddleSlow(0, i, ySize, xSize);
            }
        }
    }

    private static uint TwiddleSlow(uint x, uint y, uint xSize, uint ySize)
    {
        uint result = 0;
        int shift = 0;
        xSize >>= 1;
        ySize >>= 1;
        while (xSize != 0 || ySize != 0)
        {
            if (ySize != 0)
            {
                uint bit = y & 1;
                result |= bit << shift;
                ySize >>= 1;
                y >>= 1;
                shift++;
            }
            if (xSize != 0)
            {
                uint bit = x & 1;
                result |= bit << shift;
                xSize >>= 1;
                x >>= 1;
                shift++;
            }
        }
        return result;
    }

    internal static void UpdatePalette()
    {
        if (!PalNeedsUpdate)
            return;
        PalNeedsUpdate = false;
        PvrRenderer.UpdatePalette();

        if (!PvrRuntime.IsDirectX(PvrConfiguration.RendererType))
        {
            switch (PAL_RAM_CTRL & 3)
            {
                case 0:
                    for (int i = 0; i < 1024; i++)
                    {
                        Palette16Ram[i] = PvrUnpacker1555.Unpack(PALETTE_RAM[i]);
                        Palette32Ram[i] = PvrUnpacker1555To32<PvrRgbaPacker>.Unpack(PALETTE_RAM[i]);
                    }
                    break;
                case 1:
                    for (int i = 0; i < 1024; i++)
                    {
                        Palette16Ram[i] = PvrNopUnpacker<ushort>.Unpack(PALETTE_RAM[i]);
                        Palette32Ram[i] = PvrUnpacker565To32<PvrRgbaPacker>.Unpack(PALETTE_RAM[i]);
                    }
                    break;
                case 2:
                    for (int i = 0; i < 1024; i++)
                    {
                        Palette16Ram[i] = PvrUnpacker4444.Unpack(PALETTE_RAM[i]);
                        Palette32Ram[i] = PvrUnpacker4444To32<PvrRgbaPacker>.Unpack(PALETTE_RAM[i]);
                    }
                    break;
                case 3:
                    for (int i = 0; i < 1024; i++)
                        Palette32Ram[i] = PvrUnpacker8888<PvrRgbaPacker>.Unpack(PALETTE_RAM[i]);
                    break;
            }
        }
        else
        {
            switch (PAL_RAM_CTRL & 3)
            {
                case 0:
                    for (int i = 0; i < 1024; i++)
                    {
                        Palette16Ram[i] = PvrNopUnpacker<ushort>.Unpack(PALETTE_RAM[i]);
                        Palette32Ram[i] = PvrUnpacker1555To32<PvrBgraPacker>.Unpack(PALETTE_RAM[i]);
                    }
                    break;
                case 1:
                    for (int i = 0; i < 1024; i++)
                    {
                        Palette16Ram[i] = PvrNopUnpacker<ushort>.Unpack(PALETTE_RAM[i]);
                        Palette32Ram[i] = PvrUnpacker565To32<PvrBgraPacker>.Unpack(PALETTE_RAM[i]);
                    }
                    break;
                case 2:
                    for (int i = 0; i < 1024; i++)
                    {
                        Palette16Ram[i] = PvrNopUnpacker<ushort>.Unpack(PALETTE_RAM[i]);
                        Palette32Ram[i] = PvrUnpacker4444To32<PvrBgraPacker>.Unpack(PALETTE_RAM[i]);
                    }
                    break;
                case 3:
                    for (int i = 0; i < 1024; i++)
                        Palette32Ram[i] = PvrNopUnpacker<uint>.Unpack(PALETTE_RAM[i]);
                    break;
            }
        }

        for (int i = 0; i < PaletteHash16.Length; i++)
            PaletteHash16[i] = XxHash32.HashToUInt32(new ReadOnlySpan<byte>(PALETTE_RAM + (i << 4), 16 * 4), 7);
        for (int i = 0; i < PaletteHash256.Length; i++)
            PaletteHash256[i] = XxHash32.HashToUInt32(new ReadOnlySpan<byte>(PALETTE_RAM + (i << 8), 256 * 4), 7);
    }

    private static uint Yuv422<TPacker>(int y, int u, int v) where TPacker : IPvrColorPacker
    {
        u -= 128;
        v -= 128;
        int r = y + v * 11 / 8;
        int g = y - (u * 11 + v * 22) / 32;
        int b = y + u * 110 / 64;
        return TPacker.Pack(Math.Clamp(r, 0, 255), Math.Clamp(g, 0, 255), Math.Clamp(b, 0, 255), 0xFF);
    }

    private static uint Twop(uint x, uint y, int bcx, int bcy) => Detwiddle[0, bcy, x] + Detwiddle[1, bcx, y];

    private interface IConvert<T> where T : unmanaged
    {
        static abstract uint Xpp { get; }
        static abstract uint Ypp { get; }
        static abstract void Convert(PvrPixelBuffer<T> buffer, byte* input);
    }

    private readonly struct Planar<T, TUnpacker> : IConvert<T>
        where T : unmanaged where TUnpacker : IPvrUnpacker<T>
    {
        public static uint Xpp => 4;
        public static uint Ypp => 1;
        public static void Convert(PvrPixelBuffer<T> buffer, byte* data)
        {
            ushort* input = (ushort*)data;
            buffer.Prel(0, TUnpacker.Unpack(input[0]));
            buffer.Prel(1, TUnpacker.Unpack(input[1]));
            buffer.Prel(2, TUnpacker.Unpack(input[2]));
            buffer.Prel(3, TUnpacker.Unpack(input[3]));
        }
    }

    private readonly struct PlanarYuv<TPacker> : IConvert<uint> where TPacker : IPvrColorPacker
    {
        public static uint Xpp => 4;
        public static uint Ypp => 1;
        public static void Convert(PvrPixelBuffer<uint> buffer, byte* data)
        {
            uint* input = (uint*)data;
            int y0 = (int)((input[0] >> 8) & 255);
            int u = (int)(input[0] & 255);
            int y1 = (int)((input[0] >> 24) & 255);
            int v = (int)((input[0] >> 16) & 255);

            buffer.Prel(0, Yuv422<TPacker>(y0, u, v));
            buffer.Prel(1, Yuv422<TPacker>(y1, u, v));

            input += 1;
            y0 = (int)((input[0] >> 8) & 255);
            u = (int)(input[0] & 255);
            y1 = (int)((input[0] >> 24) & 255);
            v = (int)((input[0] >> 16) & 255);

            buffer.Prel(2, Yuv422<TPacker>(y0, u, v));
            buffer.Prel(3, Yuv422<TPacker>(y1, u, v));
        }
    }

    private readonly struct Twiddle<T, TUnpacker> : IConvert<T>
        where T : unmanaged where TUnpacker : IPvrUnpacker<T>
    {
        public static uint Xpp => 2;
        public static uint Ypp => 2;
        public static void Convert(PvrPixelBuffer<T> buffer, byte* data)
        {
            ushort* input = (ushort*)data;
            buffer.Prel(0, 0, TUnpacker.Unpack(input[0]));
            buffer.Prel(0, 1, TUnpacker.Unpack(input[1]));
            buffer.Prel(1, 0, TUnpacker.Unpack(input[2]));
            buffer.Prel(1, 1, TUnpacker.Unpack(input[3]));
        }
    }

    private readonly struct TwiddleYuv<TPacker> : IConvert<uint> where TPacker : IPvrColorPacker
    {
        public static uint Xpp => 2;
        public static uint Ypp => 2;
        public static void Convert(PvrPixelBuffer<uint> buffer, byte* data)
        {
            ushort* input = (ushort*)data;
            int y0 = (input[0] >> 8) & 255;
            int u = input[0] & 255;
            int y1 = (input[2] >> 8) & 255;
            int v = input[2] & 255;
            buffer.Prel(0, 0, Yuv422<TPacker>(y0, u, v));
            buffer.Prel(1, 0, Yuv422<TPacker>(y1, u, v));

            y0 = (input[1] >> 8) & 255;
            u = input[1] & 255;
            y1 = (input[3] >> 8) & 255;
            v = input[3] & 255;
            buffer.Prel(0, 1, Yuv422<TPacker>(y0, u, v));
            buffer.Prel(1, 1, Yuv422<TPacker>(y1, u, v));
        }
    }

    private readonly struct PaletteUnpacker<T> : IPvrUnpacker<T> where T : unmanaged, IBinaryInteger<T>
    {
        public static T Unpack(uint color)
        {
            uint[] palette = sizeof(T) == 2 ? Palette16Ram : Palette32Ram;
            return T.CreateTruncating(palette[PaletteIndex + color]);
        }
    }

    private readonly struct TwiddlePal4<T, TUnpacker> : IConvert<T>
        where T : unmanaged where TUnpacker : IPvrUnpacker<T>
    {
        public static uint Xpp => 4;
        public static uint Ypp => 4;
        public static void Convert(PvrPixelBuffer<T> buffer, byte* input)
        {
            buffer.Prel(0, 0, TUnpacker.Unpack((uint)(input[0] & 0xF)));
            buffer.Prel(0, 1, TUnpacker.Unpack((uint)((input[0] >> 4) & 0xF))); input++;
            buffer.Prel(1, 0, TUnpacker.Unpack((uint)(input[0] & 0xF)));
            buffer.Prel(1, 1, TUnpacker.Unpack((uint)((input[0] >> 4) & 0xF))); input++;
            buffer.Prel(0, 2, TUnpacker.Unpack((uint)(input[0] & 0xF)));
            buffer.Prel(0, 3, TUnpacker.Unpack((uint)((input[0] >> 4) & 0xF))); input++;
            buffer.Prel(1, 2, TUnpacker.Unpack((uint)(input[0] & 0xF)));
            buffer.Prel(1, 3, TUnpacker.Unpack((uint)((input[0] >> 4) & 0xF))); input++;
            buffer.Prel(2, 0, TUnpacker.Unpack((uint)(input[0] & 0xF)));
            buffer.Prel(2, 1, TUnpacker.Unpack((uint)((input[0] >> 4) & 0xF))); input++;
            buffer.Prel(3, 0, TUnpacker.Unpack((uint)(input[0] & 0xF)));
            buffer.Prel(3, 1, TUnpacker.Unpack((uint)((input[0] >> 4) & 0xF))); input++;
            buffer.Prel(2, 2, TUnpacker.Unpack((uint)(input[0] & 0xF)));
            buffer.Prel(2, 3, TUnpacker.Unpack((uint)((input[0] >> 4) & 0xF))); input++;
            buffer.Prel(3, 2, TUnpacker.Unpack((uint)(input[0] & 0xF)));
            buffer.Prel(3, 3, TUnpacker.Unpack((uint)((input[0] >> 4) & 0xF))); input++;
        }
    }

    private readonly struct TwiddlePal8<T, TUnpacker> : IConvert<T>
        where T : unmanaged where TUnpacker : IPvrUnpacker<T>
    {
        public static uint Xpp => 2;
        public static uint Ypp => 4;
        public static void Convert(PvrPixelBuffer<T> buffer, byte* input)
        {
            buffer.Prel(0, 0, TUnpacker.Unpack(*input++));
            buffer.Prel(0, 1, TUnpacker.Unpack(*input++));
            buffer.Prel(1, 0, TUnpacker.Unpack(*input++));
            buffer.Prel(1, 1, TUnpacker.Unpack(*input++));
            buffer.Prel(0, 2, TUnpacker.Unpack(*input++));
            buffer.Prel(0, 3, TUnpacker.Unpack(*input++));
            buffer.Prel(1, 2, TUnpacker.Unpack(*input++));
            buffer.Prel(1, 3, TUnpacker.Unpack(*input++));
        }
    }

    private static void TexturePlanar<T, TConverter>(PvrPixelBuffer<T> buffer, byte* input, uint width, uint height)
        where T : unmanaged where TConverter : IConvert<T>
    {
        buffer.Amove(0, 0);
        height /= TConverter.Ypp;
        width /= TConverter.Xpp;
        for (uint y = 0; y < height; y++)
        {
            for (uint x = 0; x < width; x++)
            {
                byte* source = input;
                TConverter.Convert(buffer, source);
                input += 8;
                buffer.RmoveX(TConverter.Xpp);
            }
            buffer.RmoveY(TConverter.Ypp);
        }
    }

    private static void TexturePlanarVq<T, TConverter>(PvrPixelBuffer<T> buffer, byte* input, uint width, uint height)
        where T : unmanaged where TConverter : IConvert<T>
    {
        buffer.Amove(0, 0);
        height /= TConverter.Ypp;
        width /= TConverter.Xpp;
        for (uint y = 0; y < height; y++)
        {
            for (uint x = 0; x < width; x++)
            {
                byte code = *input++;
                TConverter.Convert(buffer, &VqCodebook[code * 8]);
                buffer.RmoveX(TConverter.Xpp);
            }
            buffer.RmoveY(TConverter.Ypp);
        }
    }

    private static void TextureTwiddle<T, TConverter>(PvrPixelBuffer<T> buffer, byte* input, uint width, uint height)
        where T : unmanaged where TConverter : IConvert<T>
    {
        buffer.Amove(0, 0);
        uint divider = TConverter.Xpp * TConverter.Ypp;
        int bcx = BitOperations.Log2(width);
        int bcy = BitOperations.Log2(height);
        for (uint y = 0; y < height; y += TConverter.Ypp)
        {
            for (uint x = 0; x < width; x += TConverter.Xpp)
            {
                byte* source = &input[(Twop(x, y, bcx, bcy) / divider) << 3];
                TConverter.Convert(buffer, source);
                buffer.RmoveX(TConverter.Xpp);
            }
            buffer.RmoveY(TConverter.Ypp);
        }
    }

    private static void TextureVq<T, TConverter>(PvrPixelBuffer<T> buffer, byte* input, uint width, uint height)
        where T : unmanaged where TConverter : IConvert<T>
    {
        buffer.Amove(0, 0);
        uint divider = TConverter.Xpp * TConverter.Ypp;
        int bcx = BitOperations.Log2(width);
        int bcy = BitOperations.Log2(height);
        for (uint y = 0; y < height; y += TConverter.Ypp)
        {
            for (uint x = 0; x < width; x += TConverter.Xpp)
            {
                byte code = input[Twop(x, y, bcx, bcy) / divider];
                TConverter.Convert(buffer, &VqCodebook[code * 8]);
                buffer.RmoveX(TConverter.Xpp);
            }
            buffer.RmoveY(TConverter.Ypp);
        }
    }

    internal static readonly PvrTextureConverter<ushort> tex565_TW = TextureTwiddle<ushort, Twiddle<ushort, PvrNopUnpacker<ushort>>>;
    internal static readonly PvrTextureConverter<ushort> texPAL4_TW = TextureTwiddle<ushort, TwiddlePal4<ushort, PaletteUnpacker<ushort>>>;
    internal static readonly PvrTextureConverter<ushort> texPAL8_TW = TextureTwiddle<ushort, TwiddlePal8<ushort, PaletteUnpacker<ushort>>>;
    internal static readonly PvrTextureConverter<uint> texPAL4_TW32 = TextureTwiddle<uint, TwiddlePal4<uint, PaletteUnpacker<uint>>>;
    internal static readonly PvrTextureConverter<uint> texPAL8_TW32 = TextureTwiddle<uint, TwiddlePal8<uint, PaletteUnpacker<uint>>>;
    internal static readonly PvrTextureConverter<byte> texPAL4PT_TW = TextureTwiddle<byte, TwiddlePal4<byte, PvrNopUnpacker<byte>>>;
    internal static readonly PvrTextureConverter<byte> texPAL8PT_TW = TextureTwiddle<byte, TwiddlePal8<byte, PvrNopUnpacker<byte>>>;
    internal static readonly PvrTextureConverter<ushort> tex565_VQ = TextureVq<ushort, Twiddle<ushort, PvrNopUnpacker<ushort>>>;
    internal static readonly PvrTextureConverter<ushort> texPAL4_VQ = TextureVq<ushort, TwiddlePal4<ushort, PaletteUnpacker<ushort>>>;
    internal static readonly PvrTextureConverter<ushort> texPAL8_VQ = TextureVq<ushort, TwiddlePal8<ushort, PaletteUnpacker<ushort>>>;
    internal static readonly PvrTextureConverter<uint> texPAL4_VQ32 = TextureVq<uint, TwiddlePal4<uint, PaletteUnpacker<uint>>>;
    internal static readonly PvrTextureConverter<uint> texPAL8_VQ32 = TextureVq<uint, TwiddlePal8<uint, PaletteUnpacker<uint>>>;

    internal static class OpenGl
    {
        internal static readonly PvrTextureConverter<uint> texYUV422_PL = TexturePlanar<uint, PlanarYuv<PvrRgbaPacker>>;
        internal static readonly PvrTextureConverter<uint> tex565_PL32 = TexturePlanar<uint, Planar<uint, PvrUnpacker565To32<PvrRgbaPacker>>>;
        internal static readonly PvrTextureConverter<uint> tex1555_PL32 = TexturePlanar<uint, Planar<uint, PvrUnpacker1555To32<PvrRgbaPacker>>>;
        internal static readonly PvrTextureConverter<uint> tex4444_PL32 = TexturePlanar<uint, Planar<uint, PvrUnpacker4444To32<PvrRgbaPacker>>>;
        internal static readonly PvrTextureConverter<uint> texYUV422_PLVQ = TexturePlanarVq<uint, PlanarYuv<PvrRgbaPacker>>;
        internal static readonly PvrTextureConverter<uint> tex565_PLVQ32 = TexturePlanarVq<uint, Planar<uint, PvrUnpacker565To32<PvrRgbaPacker>>>;
        internal static readonly PvrTextureConverter<uint> tex1555_PLVQ32 = TexturePlanarVq<uint, Planar<uint, PvrUnpacker1555To32<PvrRgbaPacker>>>;
        internal static readonly PvrTextureConverter<uint> tex4444_PLVQ32 = TexturePlanarVq<uint, Planar<uint, PvrUnpacker4444To32<PvrRgbaPacker>>>;
        internal static readonly PvrTextureConverter<ushort> tex1555_TW = TextureTwiddle<ushort, Twiddle<ushort, PvrUnpacker1555>>;
        internal static readonly PvrTextureConverter<ushort> tex4444_TW = TextureTwiddle<ushort, Twiddle<ushort, PvrUnpacker4444>>;
        internal static PvrTextureConverter<ushort> texBMP_TW => tex4444_TW;
        internal static readonly PvrTextureConverter<uint> texYUV422_TW = TextureTwiddle<uint, TwiddleYuv<PvrRgbaPacker>>;
        internal static readonly PvrTextureConverter<uint> tex565_TW32 = TextureTwiddle<uint, Twiddle<uint, PvrUnpacker565To32<PvrRgbaPacker>>>;
        internal static readonly PvrTextureConverter<uint> tex1555_TW32 = TextureTwiddle<uint, Twiddle<uint, PvrUnpacker1555To32<PvrRgbaPacker>>>;
        internal static readonly PvrTextureConverter<uint> tex4444_TW32 = TextureTwiddle<uint, Twiddle<uint, PvrUnpacker4444To32<PvrRgbaPacker>>>;
        internal static readonly PvrTextureConverter<ushort> tex1555_VQ = TextureVq<ushort, Twiddle<ushort, PvrUnpacker1555>>;
        internal static readonly PvrTextureConverter<ushort> tex4444_VQ = TextureVq<ushort, Twiddle<ushort, PvrUnpacker4444>>;
        internal static PvrTextureConverter<ushort> texBMP_VQ => tex4444_VQ;
        internal static readonly PvrTextureConverter<uint> texYUV422_VQ = TextureVq<uint, TwiddleYuv<PvrRgbaPacker>>;
        internal static readonly PvrTextureConverter<uint> tex565_VQ32 = TextureVq<uint, Twiddle<uint, PvrUnpacker565To32<PvrRgbaPacker>>>;
        internal static readonly PvrTextureConverter<uint> tex1555_VQ32 = TextureVq<uint, Twiddle<uint, PvrUnpacker1555To32<PvrRgbaPacker>>>;
        internal static readonly PvrTextureConverter<uint> tex4444_VQ32 = TextureVq<uint, Twiddle<uint, PvrUnpacker4444To32<PvrRgbaPacker>>>;

        internal static readonly PvrTextureInfo[] Formats =
        [
            new("1555", 16, PvrTextureType._5551, tex1555_TW, tex1555_VQ, tex1555_PL32, tex1555_TW32, tex1555_VQ32, tex1555_PLVQ32, null),
            new("565", 16, PvrTextureType._565, tex565_TW, tex565_VQ, tex565_PL32, tex565_TW32, tex565_VQ32, tex565_PLVQ32, null),
            new("4444", 16, PvrTextureType._4444, tex4444_TW, tex4444_VQ, tex4444_PL32, tex4444_TW32, tex4444_VQ32, tex4444_PLVQ32, null),
            new("yuv", 16, PvrTextureType._8888, null, null, texYUV422_PL, texYUV422_TW, texYUV422_VQ, texYUV422_PLVQ, null),
            new("bumpmap", 16, PvrTextureType._4444, texBMP_TW, texBMP_VQ, tex4444_PL32, tex4444_TW32, tex4444_VQ32, tex4444_PLVQ32, null),
            new("pal4", 4, PvrTextureType._5551, texPAL4_TW, texPAL4_VQ, null, texPAL4_TW32, texPAL4_VQ32, null, texPAL4PT_TW),
            new("pal8", 8, PvrTextureType._5551, texPAL8_TW, texPAL8_VQ, null, texPAL8_TW32, texPAL8_VQ32, null, texPAL8PT_TW),
            new("ns/1555", 0),
        ];
    }

    internal static class DirectX
    {
        internal static readonly PvrTextureConverter<uint> texYUV422_PL = TexturePlanar<uint, PlanarYuv<PvrBgraPacker>>;
        internal static readonly PvrTextureConverter<uint> tex565_PL32 = TexturePlanar<uint, Planar<uint, PvrUnpacker565To32<PvrBgraPacker>>>;
        internal static readonly PvrTextureConverter<uint> tex1555_PL32 = TexturePlanar<uint, Planar<uint, PvrUnpacker1555To32<PvrBgraPacker>>>;
        internal static readonly PvrTextureConverter<uint> tex4444_PL32 = TexturePlanar<uint, Planar<uint, PvrUnpacker4444To32<PvrBgraPacker>>>;
        internal static readonly PvrTextureConverter<uint> texYUV422_PLVQ = TexturePlanarVq<uint, PlanarYuv<PvrBgraPacker>>;
        internal static readonly PvrTextureConverter<uint> tex565_PLVQ32 = TexturePlanarVq<uint, Planar<uint, PvrUnpacker565To32<PvrBgraPacker>>>;
        internal static readonly PvrTextureConverter<uint> tex1555_PLVQ32 = TexturePlanarVq<uint, Planar<uint, PvrUnpacker1555To32<PvrBgraPacker>>>;
        internal static readonly PvrTextureConverter<uint> tex4444_PLVQ32 = TexturePlanarVq<uint, Planar<uint, PvrUnpacker4444To32<PvrBgraPacker>>>;
        internal static readonly PvrTextureConverter<ushort> tex1555_TW = TextureTwiddle<ushort, Twiddle<ushort, PvrNopUnpacker<ushort>>>;
        internal static readonly PvrTextureConverter<ushort> tex4444_TW = TextureTwiddle<ushort, Twiddle<ushort, PvrNopUnpacker<ushort>>>;
        internal static PvrTextureConverter<ushort> texBMP_TW => tex4444_TW;
        internal static readonly PvrTextureConverter<uint> texYUV422_TW = TextureTwiddle<uint, TwiddleYuv<PvrBgraPacker>>;
        internal static readonly PvrTextureConverter<uint> tex565_TW32 = TextureTwiddle<uint, Twiddle<uint, PvrUnpacker565To32<PvrBgraPacker>>>;
        internal static readonly PvrTextureConverter<uint> tex1555_TW32 = TextureTwiddle<uint, Twiddle<uint, PvrUnpacker1555To32<PvrBgraPacker>>>;
        internal static readonly PvrTextureConverter<uint> tex4444_TW32 = TextureTwiddle<uint, Twiddle<uint, PvrUnpacker4444To32<PvrBgraPacker>>>;
        internal static readonly PvrTextureConverter<ushort> tex1555_VQ = TextureVq<ushort, Twiddle<ushort, PvrNopUnpacker<ushort>>>;
        internal static readonly PvrTextureConverter<ushort> tex4444_VQ = TextureVq<ushort, Twiddle<ushort, PvrNopUnpacker<ushort>>>;
        internal static PvrTextureConverter<ushort> texBMP_VQ => tex4444_VQ;
        internal static readonly PvrTextureConverter<uint> texYUV422_VQ = TextureVq<uint, TwiddleYuv<PvrBgraPacker>>;
        internal static readonly PvrTextureConverter<uint> tex565_VQ32 = TextureVq<uint, Twiddle<uint, PvrUnpacker565To32<PvrBgraPacker>>>;
        internal static readonly PvrTextureConverter<uint> tex1555_VQ32 = TextureVq<uint, Twiddle<uint, PvrUnpacker1555To32<PvrBgraPacker>>>;
        internal static readonly PvrTextureConverter<uint> tex4444_VQ32 = TextureVq<uint, Twiddle<uint, PvrUnpacker4444To32<PvrBgraPacker>>>;

        internal static readonly PvrTextureInfo[] Formats =
        [
            new("1555", 16, PvrTextureType._5551, tex1555_TW, tex1555_VQ, tex1555_PL32, tex1555_TW32, tex1555_VQ32, tex1555_PLVQ32, null),
            new("565", 16, PvrTextureType._565, tex565_TW, tex565_VQ, tex565_PL32, tex565_TW32, tex565_VQ32, tex565_PLVQ32, null),
            new("4444", 16, PvrTextureType._4444, tex4444_TW, tex4444_VQ, tex4444_PL32, tex4444_TW32, tex4444_VQ32, tex4444_PLVQ32, null),
            new("yuv", 16, PvrTextureType._8888, null, null, texYUV422_PL, texYUV422_TW, texYUV422_VQ, texYUV422_PLVQ, null),
            new("bumpmap", 16, PvrTextureType._4444, texBMP_TW, texBMP_VQ, tex4444_PL32, tex4444_TW32, tex4444_VQ32, tex4444_PLVQ32, null),
            new("pal4", 4, PvrTextureType._5551, texPAL4_TW, texPAL4_VQ, null, texPAL4_TW32, texPAL4_VQ32, null, texPAL4PT_TW),
            new("pal8", 8, PvrTextureType._5551, texPAL8_TW, texPAL8_VQ, null, texPAL8_TW32, texPAL8_VQ32, null, texPAL8PT_TW),
            new("ns/1555", 0),
        ];
    }

    internal static PvrTextureInfo[] Formats = OpenGl.Formats;
}
