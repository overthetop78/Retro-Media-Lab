namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

internal sealed partial class Vdp
{
    private uint MakePixel(int red, int green, int blue) => EffectivePixelDepth switch
    {
        8 => (uint)((red >> 1) << 5 | (green >> 1) << 2 | blue >> 2),
        15 => (uint)(0x8000 | red << 11 | (red >> 3) << 10 | green << 6 | (green >> 3) << 5 | blue << 1 | blue >> 3),
        16 => (uint)(red << 12 | (red >> 3) << 11 | green << 7 | (green >> 2) << 5 | blue << 1 | blue >> 3),
        _ => unchecked(0xff000000u | (uint)(red << 20 | red << 16 | green << 12 | green << 8 | blue << 4 | blue))
    };

    private void SetPixel(int index, uint value) { pixel[index] = value; _palette16[index] = unchecked((ushort)value); }
    private void SetPixelQuartet(int index, uint value)
    {
        SetPixel(index, value); SetPixel(0x20 | index, value);
        SetPixel(0x80 | index, value); SetPixel(0xa0 | index, value);
    }

    private void InitializePalette()
    {
        for (int i = 0; i < 0x200; i++)
        {
            int red = i & 7, green = i >> 3 & 7, blue = i >> 6 & 7;
            pixel_lut[i] = MakePixel(red, green, blue);
            pixel_lut[0x200 + i] = MakePixel(red << 1, green << 1, blue << 1);
            pixel_lut[0x400 + i] = MakePixel(red + 7, green + 7, blue + 7);
        }
        for (int i = 0; i < 0x40; i++)
        {
            int red = i & 3, green = i >> 2 & 3, blue = i >> 4 & 3;
            pixel_lut_m4[i] = MakePixel(red << 2 | red, green << 2 | green, blue << 2 | blue);
        }
    }

    private void color_update_m4(int index, uint data)
    {
        switch (system_hw)
        {
            case SYSTEM_GG:
                data = MakePixel((int)data & 15, (int)(data >> 4) & 15, (int)(data >> 8) & 15);
                break;
            case SYSTEM_SG:
            case SYSTEM_SGII:
            case SYSTEM_SGII_RAM_EXT:
                data = GetTmsColor((index & 15) != 0 ? index & 15 : reg[7] & 15);
                break;
            default:
                if ((reg[0] & 4) == 0)
                {
                    if ((system_hw & SYSTEM_MD) != 0) data = 0;
                    else if (system_hw != SYSTEM_GGMS) data = tms_crom[(index & 15) != 0 ? index & 15 : reg[7] & 15];
                }
                data = pixel_lut_m4[(int)data & 63];
                break;
        }
        if ((reg[0] & 4) != 0)
        {
            SetPixelQuartet(index, data);
        }
        else
        {
            if (index == 0x40 || index == (0x10 | (reg[7] & 15)))
            {
                SetPixel(0x40, data); SetPixel(0x10, data); SetPixel(0x30, data); SetPixel(0x90, data); SetPixel(0xb0, data);
            }
            if ((index & 15) != 0)
                SetPixelQuartet(index, data);
        }
    }

    private void color_update_m5(int index, uint data)
    {
        if ((reg[0] & 4) == 0) data &= 0x49;
        int color = (int)data;
        if ((reg[12] & 8) != 0)
        {
            SetPixel(index, pixel_lut[color]);
            SetPixel(0x40 | index, pixel_lut[0x200 + color]);
            SetPixel(0x80 | index, pixel_lut[0x400 + color]);
        }
        else
        {
            data = pixel_lut[0x200 + color];
            SetPixel(index, data); SetPixel(0x40 | index, data); SetPixel(0x80 | index, data);
        }
    }

    private uint GetTmsColor(int index) => EffectivePixelDepth switch
    {
        8 => TmsPalette8[index], 15 => TmsPalette15[index], 16 => TmsPalette16[index], _ => tms_palette[index]
    };
    private static ReadOnlySpan<byte> TmsPalette8 => [0, 0, 0x39, 0x79, 0x4b, 0x6f, 0xc9, 0x5b, 0xe9, 0xed, 0xd5, 0xd9, 0x35, 0xce, 0xda, 0xff];
    private static ReadOnlySpan<ushort> TmsPalette15 => [0x8000, 0x8000, 0x9308, 0xaf6f, 0xa95d, 0xbddf, 0xe949, 0xa3be, 0xfd4a, 0xfdef, 0xeb0a, 0xf330, 0x92a7, 0xe177, 0xe739, 0xffff];
    private static ReadOnlySpan<ushort> TmsPalette16 => [0, 0, 0x2648, 0x5ecf, 0x52bd, 0x7bbe, 0xd289, 0x475e, 0xf2aa, 0xfbcf, 0xd60a, 0xe670, 0x2567, 0xc2f7, 0xce59, 0xffff];
}
