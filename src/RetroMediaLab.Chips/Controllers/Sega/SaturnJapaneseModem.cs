// Copyright 2013 Theo Berkau
// Translated from Yabause japmodem.c.
// SPDX-License-Identifier: GPL-2.0-or-later

namespace RetroMediaLab.Chips.Controllers.Sega;

internal static class SaturnJapaneseModem<TSh2>
{
    internal static byte Cs0ReadByte(TSh2 sh, uint address)
    {
        if ((address & 1) != 0)
            return 0xA5;
        else
            return 0xFF;
    }

    internal static ushort Cs0ReadWord(TSh2 sh, uint address) => 0xFFA5;

    internal static uint Cs0ReadLong(TSh2 sh, uint address) => 0xFFA5FFA5;

    internal static byte Cs1ReadByte(TSh2 sh, uint address) => 0xFF;

    internal static ushort Cs1ReadWord(TSh2 sh, uint address) => 0xFFFF;

    internal static uint Cs1ReadLong(TSh2 sh, uint address) => 0xFFFFFFFF;

    internal static void Cs1WriteByte(TSh2 sh, uint address, byte value) { }

    internal static void Cs1WriteWord(TSh2 sh, uint address, ushort value) { }

    internal static void Cs1WriteLong(TSh2 sh, uint address, uint value) { }

    internal static byte Cs2ReadByte(TSh2 sh, uint address) => 0xFF;

    internal static void Cs2WriteByte(TSh2 sh, uint address, byte value) { }
}
