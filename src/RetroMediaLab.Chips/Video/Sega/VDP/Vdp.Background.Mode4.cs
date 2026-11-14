using RetroMediaLab.Chips.Common.Memory;

using System.Buffers.Binary;

namespace RetroMediaLab.Chips.Video.Sega.VDP;

public sealed partial class Vdp
{
    private void render_bg_m4(int line)
    {
        int scroll = (reg[0] & 0x40) != 0 && line < 0x10 ? 0x100 : reg[8];
        int shift = scroll & 7;
        int output = 0x20 + shift;
        int vertical = line + vscroll;
        int mask = unchecked((ushort)(~0x3c00 ^ (reg[2] << 10)));
        if (system_hw > SYSTEM_SMS) mask |= 0x400;
        int names;
        if (ViewportHeight > 192)
        {
            vertical %= 256;
            names = (0x3700 & mask) + ((vertical >> 3) << 6);
        }
        else
        {
            vertical %= 224;
            names = (0x3800 + ((vertical >> 3) << 6)) & mask;
        }
        vertical = (vertical & 7) << 3;
        int index = (0x100 - scroll) >> 3;
        if (shift != 0) { linebuf.AsSpan(0x20, shift).Clear(); index++; }
        for (int column = 0; column < 32; column++, index++)
        {
            if (column == 24 && (reg[0] & 0x80) != 0)
            {
                names = ViewportHeight > 192
                    ? (0x3700 & mask) + ((line >> 3) << 6)
                    : (0x3800 + ((line >> 3) << 6)) & mask;
                vertical = (line & 7) << 3;
            }
            int attribute = BinaryPrimitives.ReadUInt16LittleEndian(vram.AsSpan(names + ((index & 31) * 2), 2));
            byte expansion = (byte)((attribute >> 11 & 3) << 4);
            if (system_hw <= SYSTEM_SMS)
            {
                int low = ((attribute & (0x601 | (reg[3] << 1))) << 6) | vertical;
                int high = ((attribute & (0x63f | ((reg[4] & 7) << 6))) << 6) | vertical;
                for (int x = 0; x < 8; x++)
                    linebuf[output++] = (byte)((bg_pattern_cache[low + x] & 3) | (bg_pattern_cache[high + x] & 12) | expansion);
            }
            else
            {
                int pattern = ((attribute & 0x7ff) << 6) | vertical;
                for (int x = 0; x < 8; x++) linebuf[output++] = (byte)(bg_pattern_cache[pattern + x] | expansion);
            }
        }
    }
}
