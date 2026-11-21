// Copyright 2003-2005 Guillaume Duhamel
// Copyright 2005 Theo Berkau
// Translated from Yabause cs1.c.
// SPDX-License-Identifier: GPL-2.0-or-later

namespace RetroMediaLab.Chips.Controllers.Sega;

internal sealed class SaturnCartridgeChipSelect<TSh2>(
    Func<byte> cartridgeId,
    Func<TSh2, uint, byte> cartridgeReadByte,
    Func<TSh2, uint, ushort> cartridgeReadWord,
    Func<TSh2, uint, uint> cartridgeReadLong,
    Action<TSh2, uint, byte> cartridgeWriteByte,
    Action<TSh2, uint, ushort> cartridgeWriteWord,
    Action<TSh2, uint, uint> cartridgeWriteLong)
{
    internal byte Cs1ReadByte(TSh2 sh, uint address)
    {
        address &= 0xFFFFFF;
        if (address == 0xFFFFFF)
            return cartridgeId();

        return cartridgeReadByte(sh, address);
    }

    internal ushort Cs1ReadWord(TSh2 sh, uint address)
    {
        address &= 0xFFFFFF;
        if (address == 0xFFFFFE)
            return (ushort)(0xFF00 | cartridgeId());

        return cartridgeReadWord(sh, address);
    }

    internal uint Cs1ReadLong(TSh2 sh, uint address)
    {
        address &= 0xFFFFFF;
        if (address == 0xFFFFFC)
            return 0xFF00FF00u | ((uint)cartridgeId() << 16) | cartridgeId();

        return cartridgeReadLong(sh, address);
    }

    internal void Cs1WriteByte(TSh2 sh, uint address, byte value)
    {
        address &= 0xFFFFFF;
        if (address == 0xFFFFFF)
            return;

        cartridgeWriteByte(sh, address, value);
    }

    internal void Cs1WriteWord(TSh2 sh, uint address, ushort value)
    {
        address &= 0xFFFFFF;
        if (address == 0xFFFFFE)
            return;

        cartridgeWriteWord(sh, address, value);
    }

    internal void Cs1WriteLong(TSh2 sh, uint address, uint value)
    {
        address &= 0xFFFFFF;
        if (address == 0xFFFFFC)
            return;

        cartridgeWriteLong(sh, address, value);
    }
}
