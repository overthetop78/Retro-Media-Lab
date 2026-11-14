using RetroMediaLab.Chips.Common.Memory;

using System.Buffers.Binary;

namespace RetroMediaLab.Chips.Video.Sega.VDP;

public sealed partial class Vdp
{
    private void update_bg_pattern_cache_m4(int count)
    {
        for (int i = 0; i < count; i++)
        {
            int name = bg_name_list[i];
            int destination = name << 6;
            for (int y = 0; y < 8; y++)
            {
                if ((bg_name_dirty[name] & (1 << y)) == 0) continue;
                int address = (name << 5) | (y << 2);
                int low = BinaryPrimitives.ReadUInt16LittleEndian(vram.AsSpan(address, 2));
                int high = BinaryPrimitives.ReadUInt16LittleEndian(vram.AsSpan(address + 2, 2));
                uint bits = (bp_lut[low] >> 2) | bp_lut[high];
                for (int x = 0; x < 8; x++)
                {
                    byte color = (byte)(bits & 15);
                    bg_pattern_cache[destination + ((y << 3) | x)] = color;
                    bg_pattern_cache[destination + (0x08000 | (y << 3) | (x ^ 7))] = color;
                    bg_pattern_cache[destination + (0x10000 | ((y ^ 7) << 3) | x)] = color;
                    bg_pattern_cache[destination + (0x18000 | ((y ^ 7) << 3) | (x ^ 7))] = color;
                    bits >>= 4;
                }
            }
            bg_name_dirty[name] = 0;
        }
    }

    private void update_bg_pattern_cache_m5(int count)
    {
        for (int i = 0; i < count; i++)
        {
            int name = bg_name_list[i];
            int destination = name << 6;
            for (int y = 0; y < 8; y++)
            {
                if ((bg_name_dirty[name] & (1 << y)) == 0) continue;
                uint bits = BinaryPrimitives.ReadUInt32LittleEndian(vram.AsSpan((name << 5) | (y << 2), 4));
                for (int x = 0; x < 8; x++)
                {
                    byte color = (byte)(bits & 15);
                    bg_pattern_cache[destination + ((y << 3) | (x ^ 3))] = color;
                    bg_pattern_cache[destination + (0x20000 | (y << 3) | (x ^ 4))] = color;
                    bg_pattern_cache[destination + (0x40000 | ((y ^ 7) << 3) | (x ^ 3))] = color;
                    bg_pattern_cache[destination + (0x60000 | ((y ^ 7) << 3) | (x ^ 4))] = color;
                    bits >>= 4;
                }
            }
            bg_name_dirty[name] = 0;
        }
    }
}
