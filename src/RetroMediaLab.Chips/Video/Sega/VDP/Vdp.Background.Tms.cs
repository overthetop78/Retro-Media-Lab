using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Chips.Video.Sega.VDP;

public sealed partial class Vdp
{
    private void render_bg_m0(int line)
    {
        int output = 0x20;
        int names = ((reg[2] << 10) & 0x3c00) + ((line & 0xf8) << 2);
        int colors = (reg[3] << 6) & 0x3fc0;
        int patterns = ((reg[4] << 11) & 0x3800) + (line & 7);
        for (int column = 0; column < 32; column++)
        {
            int name = vram[names++];
            int color = vram[colors + (name >> 3)];
            int pattern = vram[patterns + (name << 3)];
            DrawTmsPattern(ref output, pattern, color, 8);
        }
    }

    private void render_bg_m1(int line) => RenderText(line, false);
    private void render_bg_m1x(int line) => RenderText(line, true);
    private void RenderText(int line, bool extended)
    {
        int output = 0x28;
        int names = ((reg[2] << 10) & 0x3c00) + (line >> 3) * 40;
        int patterns;
        if (extended)
        {
            int mask = unchecked((ushort)(~0x3800 ^ (reg[4] << 11)));
            if (system_hw > SYSTEM_SMS) mask |= 0x1800;
            patterns = ((0x2000 + ((line & 0xc0) << 5)) & mask) + (line & 7);
        }
        else patterns = ((reg[4] << 11) & 0x3800) + (line & 7);
        linebuf.AsSpan(0x20, 8).Fill(0x40);
        for (int column = 0; column < 40; column++)
            DrawTmsPattern(ref output, vram[patterns + (vram[names++] << 3)], reg[7], 6);
        linebuf.AsSpan(output, 8).Fill(0x40);
    }

    private void render_bg_m2(int line)
    {
        int output = 0x20;
        int names = ((reg[2] << 10) & 0x3c00) + ((line & 0xf8) << 2);
        int colorMask = unchecked((ushort)(~0x3fc0 ^ (reg[3] << 6)));
        int patternMask = unchecked((ushort)(~0x3800 ^ (reg[4] << 11)));
        if (system_hw > SYSTEM_SMS) { colorMask |= 0x1fc0; patternMask |= 0x1800; }
        int colors = ((0x2000 + ((line & 0xc0) << 5)) & colorMask) + (line & 7);
        int patterns = ((0x2000 + ((line & 0xc0) << 5)) & patternMask) + (line & 7);
        for (int column = 0; column < 32; column++)
        {
            int name = vram[names++] << 3;
            DrawTmsPattern(ref output, vram[patterns + name], vram[colors + (name & colorMask)], 8);
        }
    }

    private void render_bg_m3(int line) => RenderMulticolor(line, false);
    private void render_bg_m3x(int line) => RenderMulticolor(line, true);
    private void RenderMulticolor(int line, bool extended)
    {
        int output = 0x20;
        int names = ((reg[2] << 10) & 0x3c00) + ((line & 0xf8) << 2);
        int patterns;
        if (extended)
        {
            int mask = unchecked((ushort)(~0x3800 ^ (reg[4] << 11)));
            if (system_hw > SYSTEM_SMS) mask |= 0x1800;
            patterns = ((0x2000 + ((line & 0xc0) << 5)) & mask) + ((line >> 2) & 7);
        }
        else patterns = ((reg[4] << 11) & 0x3800) + ((line >> 2) & 7);
        for (int column = 0; column < 32; column++)
        {
            int color = vram[patterns + (vram[names++] << 3)];
            linebuf.AsSpan(output, 4).Fill((byte)(0x10 | (color >> 4)));
            linebuf.AsSpan(output + 4, 4).Fill((byte)(0x10 | (color & 15)));
            output += 8;
        }
    }

    private void render_bg_inv(int line)
    {
        int output = 0x28;
        int color = reg[7];
        linebuf.AsSpan(0x20, 8).Fill(0x40);
        for (int column = 0; column < 40; column++)
        {
            linebuf.AsSpan(output, 4).Fill((byte)(0x10 | (color >> 4)));
            linebuf.AsSpan(output + 4, 2).Fill((byte)(0x10 | (color & 15)));
            output += 6;
        }
        linebuf.AsSpan(output, 8).Fill(0x40);
    }

    private void DrawTmsPattern(ref int output, int pattern, int color, int width)
    {
        for (int x = 0; x < width; x++)
            linebuf[output++] = (byte)(0x10 | ((color >> (((pattern >> (7 - x)) & 1) << 2)) & 15));
    }
}
