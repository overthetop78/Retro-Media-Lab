// Data layout from Yabause sh2cache.h.
// SPDX-License-Identifier: GPL-2.0-or-later

namespace RetroMediaLab.Chips.CPU.Hitachi.SH2;

internal sealed class SaturnSh2CacheState
{
    internal sealed class Line
    {
        internal uint Tag;
        internal uint Valid;
        internal readonly byte[] Data = new byte[16];
    }

    internal uint Enable;
    internal readonly uint[] Lru = new uint[64];
    internal readonly Line[,] Way = new Line[4, 64];

    internal SaturnSh2CacheState()
    {
        for (int way = 0; way < 4; way++)
            for (int entry = 0; entry < 64; entry++)
                Way[way, entry] = new Line();
    }
}
