// Copyright 2005 Guillaume Duhamel
// Copyright 2016 Shinya Miyamoto
// Translated from Yabause sh2cache.c.
// SPDX-License-Identifier: GPL-2.0-or-later

using RetroMediaLab.Chips.Controllers.Sega;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH2;

internal sealed class SaturnSh2Cache(
    SaturnSh2Core core,
    SaturnMemory.Sh2MemoryMap memory,
    SaturnMemory.Sh2MemoryMap masterMemory,
    Action<int> addCycles)
{
    private readonly SaturnSh2Core _core = core;
    private readonly SaturnSh2CacheState _state = core.Cache;
    private readonly SaturnMemory.Sh2MemoryMap _memory = memory;
    private readonly Action<int> _addCycles = addCycles;

    private const uint AreaMask = 0xE0000000;
    private const uint TagMask = 0x1FFFFC00;
    private const uint EntryMask = 0x000003F0;
    private const uint LineMask = 0x0000000F;
    private const uint CacheUse = 0x00000000;
    private const uint CacheThrough = 0x20000000;
    private const uint CachePurge = 0x40000000;

    internal static void Clear(SaturnSh2CacheState state)
    {
        state.Enable = 0;

        for (int entry = 0; entry < 64; entry++)
        {
            state.Lru[entry] = 0;
            for (int way = 0; way < 4; way++)
            {
                state.Way[way, entry].Tag = 0;
                for (int index = 0; index < 16; index++)
                    state.Way[way, entry].Data[index] = 0;
                state.Way[way, entry].Valid = 0;
            }
        }
    }

    internal static void Enable(SaturnSh2CacheState state) => state.Enable = 1;

    internal static void Disable(SaturnSh2CacheState state) => state.Enable = 0;

    private static void UpdateLru(int way, ref uint lru)
    {
        if (way == 3)
        {
            lru |= 0xB;
            return;
        }
        else if (way == 2)
        {
            lru &= 0x3E;
            lru |= 0x14;
            return;
        }
        else if (way == 1)
        {
            lru |= 1 << 5;
            lru &= 0x39;
            return;
        }
        else
        {
            lru &= 0x7;
            return;
        }
    }

    private static int SelectWayToReplace(byte cacheControl, uint lru)
    {
        if ((cacheControl & (1 << 3)) != 0)
        {
            if ((lru & 1) == 1)
                return 2;
            else
                return 3;
        }
        else
        {
            if ((lru & 0x38) == 0x38)
                return 0;
            else if ((lru & 0x26) == 0x6)
                return 1;
            else if ((lru & 0x15) == 1)
                return 2;
            else if ((lru & 0xB) == 0)
                return 3;
        }

        return 0;
    }

    private static int AdjustCycles(int cycles) => cycles <= 7 ? 0 : cycles - 7;

    private static int GetCacheThroughTimingReadByteWord(uint address)
    {
        address = (address >> 16) & 0xFFF;

        if (address <= 0x00f)// bios
            return AdjustCycles(15);
        else if (address >= 0x010 && address <= 0x017)// smpc
            return AdjustCycles(15);
        else if (address >= 0x018 && address <= 0x01f)// bup
            return AdjustCycles(1);
        else if (address >= 0x020 && address <= 0x02f)// lwram
            return AdjustCycles(14);
        // ignore input capture
        else if (address >= 0x200 && address <= 0x3ff)// cs0
            return AdjustCycles(1);
        else if (address >= 0x400 && address <= 0x4ff)// cs1
            return AdjustCycles(1);
        else if (address >= 0x580 && address <= 0x58f)// cs2
            return AdjustCycles(24);
        else if (address >= 0x5a0 && address <= 0x5af)// sound ram
            return AdjustCycles(53);
        else if (address >= 0x5b0 && address <= 0x5bf)// scsp regs
            return AdjustCycles(52);
        else if (address >= 0x5c0 && address <= 0x5c7)// vdp1 ram
            return AdjustCycles(51);
        else if (address >= 0x5c8 && address <= 0x5cf)// vdp1 fb
            return AdjustCycles(51);
        else if (address >= 0x5d0 && address <= 0x5d7)// vdp1 regs
            return AdjustCycles(35);
        else if (address >= 0x5e0 && address <= 0x5ef)// vdp2 ram
            return AdjustCycles(44);
        else if (address >= 0x5f0 && address <= 0x5f7)// vdp2 color
            return AdjustCycles(44);
        else if (address >= 0x5f8 && address <= 0x5fb)// vdp2 regs
            return AdjustCycles(44);
        else if (address >= 0x5fe && address <= 0x5fe)// scu
            return AdjustCycles(14);
        else if (address >= 0x600 && address <= 0x7ff)// hwram
            return AdjustCycles(14);

        return 0;
    }

    private static int GetCacheThroughTimingReadLong(uint address)
    {
        address = (address >> 16) & 0xFFF;

        if (address <= 0x00f)// bios
            return AdjustCycles(23);
        else if (address >= 0x010 && address <= 0x017)// smpc
            return AdjustCycles(23);
        else if (address >= 0x018 && address <= 0x01f)// bup
            return AdjustCycles(1);
        else if (address >= 0x020 && address <= 0x02f)// lwram
            return AdjustCycles(21);
        // ignore input capture
        else if (address >= 0x200 && address <= 0x3ff)// cs0
            return AdjustCycles(1);
        else if (address >= 0x400 && address <= 0x4ff)// cs1
            return AdjustCycles(1);
        else if (address >= 0x580 && address <= 0x58f)// cs2
            return AdjustCycles(24);
        else if (address >= 0x5a0 && address <= 0x5af)// sound ram
            return AdjustCycles(53);
        else if (address >= 0x5b0 && address <= 0x5bf)// scsp regs
            return AdjustCycles(52);
        else if (address >= 0x5c0 && address <= 0x5c7)// vdp1 ram
            return AdjustCycles(51);
        else if (address >= 0x5c8 && address <= 0x5cf)// vdp1 fb
            return AdjustCycles(51);
        else if (address >= 0x5d0 && address <= 0x5d7)// vdp1 regs
            return AdjustCycles(35);
        else if (address >= 0x5e0 && address <= 0x5ef)// vdp2 ram
            return AdjustCycles(44);
        else if (address >= 0x5f0 && address <= 0x5f7)// vdp2 color
            return AdjustCycles(44);
        else if (address >= 0x5f8 && address <= 0x5fb)// vdp2 regs
            return AdjustCycles(44);
        else if (address >= 0x5fe && address <= 0x5fe)// scu
            return AdjustCycles(14);
        else if (address >= 0x600 && address <= 0x7ff)// hwram
            return AdjustCycles(14);

        return 0;
    }

    private static int GetCacheThroughTimingWriteByteWord(uint address)
    {
        address = (address >> 16) & 0xFFF;

        if (address <= 0x00f)// bios
            return AdjustCycles(8);
        else if (address >= 0x010 && address <= 0x017)// smpc
            return AdjustCycles(8);
        else if (address >= 0x018 && address <= 0x01f)// bup
            return AdjustCycles(1);
        else if (address >= 0x020 && address <= 0x02f)// lwram
            return AdjustCycles(7);
        // ignore input capture
        else if (address >= 0x200 && address <= 0x3ff)// cs0
            return AdjustCycles(1);
        else if (address >= 0x400 && address <= 0x4ff)// cs1
            return AdjustCycles(1);
        else if (address >= 0x580 && address <= 0x58f)// cs2
            return AdjustCycles(7);
        else if (address >= 0x5a0 && address <= 0x5af)// sound ram
            return AdjustCycles(19);
        else if (address >= 0x5b0 && address <= 0x5bf)// scsp regs
            return AdjustCycles(19);
        else if (address >= 0x5c0 && address <= 0x5c7)// vdp1 ram
            return AdjustCycles(11);
        else if (address >= 0x5c8 && address <= 0x5cf)// vdp1 fb
            return AdjustCycles(11);
        else if (address >= 0x5d0 && address <= 0x5d7)// vdp1 regs
            return AdjustCycles(11);
        else if (address >= 0x5e0 && address <= 0x5ef)// vdp2 ram
            return AdjustCycles(7);
        else if (address >= 0x5f0 && address <= 0x5f7)// vdp2 color
            return AdjustCycles(8);
        else if (address >= 0x5f8 && address <= 0x5fb)// vdp2 regs
            return AdjustCycles(7);
        else if (address >= 0x5fe && address <= 0x5fe)// scu
            return AdjustCycles(7);
        else if (address >= 0x600 && address <= 0x7ff)// hwram
            return AdjustCycles(7);

        return 0;
    }

    private static int GetCacheThroughTimingWriteLong(uint address)
    {
        address = (address >> 16) & 0xFFF;

        if (address <= 0x00f)// bios
            return AdjustCycles(16);
        else if (address >= 0x010 && address <= 0x017)// smpc
            return AdjustCycles(16);
        else if (address >= 0x018 && address <= 0x01f)// bup
            return AdjustCycles(1);
        else if (address >= 0x020 && address <= 0x02f)// lwram
            return AdjustCycles(14);
        // ignore input capture
        else if (address >= 0x200 && address <= 0x3ff)// cs0
            return AdjustCycles(1);
        else if (address >= 0x400 && address <= 0x4ff)// cs1
            return AdjustCycles(1);
        else if (address >= 0x580 && address <= 0x58f)// cs2
            return AdjustCycles(14);
        else if (address >= 0x5a0 && address <= 0x5af)// sound ram
            return AdjustCycles(33);
        else if (address >= 0x5b0 && address <= 0x5bf)// scsp regs
            return AdjustCycles(32);
        else if (address >= 0x5c0 && address <= 0x5c7)// vdp1 ram
            return AdjustCycles(12);
        else if (address >= 0x5c8 && address <= 0x5cf)// vdp1 fb
            return AdjustCycles(12);
        else if (address >= 0x5d0 && address <= 0x5d7)// vdp1 regs
            return AdjustCycles(11);
        else if (address >= 0x5e0 && address <= 0x5ef)// vdp2 ram
            return AdjustCycles(7);
        else if (address >= 0x5f0 && address <= 0x5f7)// vdp2 color
            return AdjustCycles(8);
        else if (address >= 0x5f8 && address <= 0x5fb)// vdp2 regs
            return AdjustCycles(7);
        else if (address >= 0x5fe && address <= 0x5fe)// scu
            return AdjustCycles(7);
        else if (address >= 0x600 && address <= 0x7ff)// hwram
            return AdjustCycles(7);

        return 0;
    }

    internal void CacheMemoryWriteByte(uint address, byte value)
    {
        switch (address & AreaMask)
        {
            case CacheUse:
            {
                if (_state.Enable != 0)
                {
                    uint tag = address & TagMask;
                    int entry = (int)((address & EntryMask) >> 4);
                    uint offset = address & LineMask;
                    if (_state.Way[0, entry].Valid != 0 && _state.Way[0, entry].Tag == tag)
                    {
                        _state.Way[0, entry].Data[offset] = value;
                        UpdateLru(0, ref _state.Lru[entry]);
                    }
                    else if (_state.Way[1, entry].Valid != 0 && _state.Way[1, entry].Tag == tag)
                    {
                        _state.Way[1, entry].Data[offset] = value;
                        UpdateLru(1, ref _state.Lru[entry]);
                    }
                    else if (_state.Way[2, entry].Valid != 0 && _state.Way[2, entry].Tag == tag)
                    {
                        _state.Way[2, entry].Data[offset] = value;
                        UpdateLru(2, ref _state.Lru[entry]);
                    }
                    else if (_state.Way[3, entry].Valid != 0 && _state.Way[3, entry].Tag == tag)
                    {
                        _state.Way[3, entry].Data[offset] = value;
                        UpdateLru(3, ref _state.Lru[entry]);
                    }
                }

                _memory.MappedMemoryWriteByteNocache(address, value);
                return;
            }
            case CacheThrough:
                _addCycles(GetCacheThroughTimingWriteByteWord(address));
                _memory.MappedMemoryWriteByteNocache(address, value);
                return;
            default:
                _memory.MappedMemoryWriteByteNocache(address, value);
                return;
        }
    }

    internal void CacheMemoryWriteWord(uint address, ushort value)
    {
        switch (address & AreaMask)
        {
            case CacheUse:
            {
                if (_state.Enable != 0)
                {
                    uint tag = address & TagMask;
                    int entry = (int)((address & EntryMask) >> 4);
                    uint offset = address & LineMask;
                    if (_state.Way[0, entry].Valid != 0 && _state.Way[0, entry].Tag == tag)
                    {
                        _state.Way[0, entry].Data[offset] = (byte)((value >> 8) & 0xFF);
                        _state.Way[0, entry].Data[offset + 1] = (byte)((value) & 0xFF);
                        UpdateLru(0, ref _state.Lru[entry]);
                    }
                    else if (_state.Way[1, entry].Valid != 0 && _state.Way[1, entry].Tag == tag)
                    {
                        _state.Way[1, entry].Data[offset] = (byte)((value >> 8) & 0xFF);
                        _state.Way[1, entry].Data[offset + 1] = (byte)((value) & 0xFF);
                        UpdateLru(1, ref _state.Lru[entry]);
                    }
                    else if (_state.Way[2, entry].Valid != 0 && _state.Way[2, entry].Tag == tag)
                    {
                        _state.Way[2, entry].Data[offset] = (byte)((value >> 8) & 0xFF);
                        _state.Way[2, entry].Data[offset + 1] = (byte)((value) & 0xFF);
                        UpdateLru(2, ref _state.Lru[entry]);
                    }
                    else if (_state.Way[3, entry].Valid != 0 && _state.Way[3, entry].Tag == tag)
                    {
                        _state.Way[3, entry].Data[offset] = (byte)((value >> 8) & 0xFF);
                        _state.Way[3, entry].Data[offset + 1] = (byte)((value) & 0xFF);
                        UpdateLru(3, ref _state.Lru[entry]);
                    }
                }

                _memory.MappedMemoryWriteWordNocache(address, value);
                return;
            }
            case CacheThrough:
                _addCycles(GetCacheThroughTimingWriteByteWord(address));
                _memory.MappedMemoryWriteWordNocache(address, value);
                return;
            default:
                _memory.MappedMemoryWriteWordNocache(address, value);
                return;
        }
    }

    internal void CacheMemoryWriteLong(uint address, uint value)
    {
        switch (address & AreaMask)
        {
            case CachePurge:
            {
                uint tag = address & TagMask;
                int entry = (int)((address & EntryMask) >> 4);
                for (int way = 0; way < 3; way++)
                {
                    if (_state.Way[way, entry].Tag == tag)
                    {
                        _state.Way[way, entry].Valid = 0;
                        break;
                    }
                }
                return;
            }
            case CacheUse:
            {
                if (_state.Enable != 0)
                {
                    uint tag = address & TagMask;
                    int entry = (int)((address & EntryMask) >> 4);
                    uint offset = address & LineMask;
                    if (_state.Way[0, entry].Valid != 0 && _state.Way[0, entry].Tag == tag)
                    {
                        _state.Way[0, entry].Data[offset] = (byte)((value >> 24) & 0xFF);
                        _state.Way[0, entry].Data[offset + 1] = (byte)((value >> 16) & 0xFF);
                        _state.Way[0, entry].Data[offset + 2] = (byte)((value >> 8) & 0xFF);
                        _state.Way[0, entry].Data[offset + 3] = (byte)((value) & 0xFF);
                        UpdateLru(0, ref _state.Lru[entry]);
                    }
                    else if (_state.Way[1, entry].Valid != 0 && _state.Way[1, entry].Tag == tag)
                    {
                        _state.Way[1, entry].Data[offset] = (byte)((value >> 24) & 0xFF);
                        _state.Way[1, entry].Data[offset + 1] = (byte)((value >> 16) & 0xFF);
                        _state.Way[1, entry].Data[offset + 2] = (byte)((value >> 8) & 0xFF);
                        _state.Way[1, entry].Data[offset + 3] = (byte)((value) & 0xFF);
                        UpdateLru(1, ref _state.Lru[entry]);
                    }
                    else if (_state.Way[2, entry].Valid != 0 && _state.Way[2, entry].Tag == tag)
                    {
                        _state.Way[2, entry].Data[offset] = (byte)((value >> 24) & 0xFF);
                        _state.Way[2, entry].Data[offset + 1] = (byte)((value >> 16) & 0xFF);
                        _state.Way[2, entry].Data[offset + 2] = (byte)((value >> 8) & 0xFF);
                        _state.Way[2, entry].Data[offset + 3] = (byte)((value) & 0xFF);
                        UpdateLru(2, ref _state.Lru[entry]);
                    }
                    else if (_state.Way[3, entry].Valid != 0 && _state.Way[3, entry].Tag == tag)
                    {
                        _state.Way[3, entry].Data[offset] = (byte)((value >> 24) & 0xFF);
                        _state.Way[3, entry].Data[offset + 1] = (byte)((value >> 16) & 0xFF);
                        _state.Way[3, entry].Data[offset + 2] = (byte)((value >> 8) & 0xFF);
                        _state.Way[3, entry].Data[offset + 3] = (byte)((value) & 0xFF);
                        UpdateLru(3, ref _state.Lru[entry]);
                    }
                }

                _memory.MappedMemoryWriteLongNocache(address, value);
                return;
            }
            case CacheThrough:
                _addCycles(GetCacheThroughTimingWriteLong(address));
                _memory.MappedMemoryWriteLongNocache(address, value);
                return;
            default:
                _memory.MappedMemoryWriteLongNocache(address, value);
                return;
        }
    }

    private uint CacheRefillRead(uint address)
    {
        address &= 0x0FFFFFFF;
        SaturnMemory instructionMemory = masterMemory.InstructionMemory;
        SaturnMemory.Peripherals peripherals = masterMemory.DmaPeripherals!;

        if (address <= 0x00FFFFF)
            return instructionMemory.BiosRomMemoryReadLong(address);
        if (address >= 0x0100000 && address <= 0x017FFFF)
            return peripherals.Smpc.ReadLong(address);
        if (address >= 0x0180000 && address <= 0x01FFFFF)
            return instructionMemory.BupRamMemoryReadLong(address);
        if (address >= 0x0200000 && address <= 0x02FFFFF)
            return instructionMemory.LowWramMemoryReadLong(address);
        if (address >= 0x1000000 && address <= 0x17FFFFF)
            return 0;
        if (address >= 0x1800000 && address <= 0x1FFFFFF)
            return 0;
        if (address >= 0x2000000 && address <= 0x3FFFFFF)
            return masterMemory.DmaCartridgeCs0.ReadLong(address);
        if (address >= 0x4000000 && address <= 0x4FFFFFF)
            return masterMemory.DmaCartridgeCs1.ReadLong(address);
        if (address >= 0x5800000 && address <= 0x58FFFFF)
            return peripherals.Ygr is SaturnYgr ygr ? ygr.ABusReadLong(address) : peripherals.Cs2.ReadLong(address);
        if (address >= 0x5A00000 && address <= 0x5AFFFFF)
            return peripherals.SoundRam.ReadLong(address);
        if (address >= 0x5B00000 && address <= 0x5BFFFFF)
            return peripherals.Scsp.ReadLong(address);
        if (address >= 0x5C00000 && address <= 0x5C7FFFF)
            return peripherals.Vdp1.RamReadLong(address);
        if (address >= 0x5C80000 && address <= 0x5CFFFFF)
            return peripherals.Vdp1.FrameBufferReadLong(address);
        if (address >= 0x5D00000 && address <= 0x5D7FFFF)
            return peripherals.Vdp1.ReadLong(address);
        if (address >= 0x5E00000 && address <= 0x5EFFFFF)
            return peripherals.Vdp2.RamReadLong(address);
        if (address >= 0x5F00000 && address <= 0x5F7FFFF)
            return peripherals.Vdp2.ColorRamReadLong(address);
        if (address >= 0x5F80000 && address <= 0x5FBFFFF)
            return peripherals.Vdp2.ReadLong(address);
        if (address >= 0x5FE0000 && address <= 0x5FEFFFF)
            return peripherals.Scu.ReadLong(address);
        if (address >= 0x6000000 && address <= 0x7FFFFFF)
            return instructionMemory.HighWramMemoryReadLong(address);

        return 0;
    }

    private void RefillCache(int way, int entry, uint address)
    {
        _addCycles(4);

        for (int index = 0; index < 16; index += 4)
        {
            uint value = CacheRefillRead((address & 0xFFFFFFF0) + (uint)index);
            _state.Way[way, entry].Data[index] = (byte)((value >> 24) & 0xFF);
            _state.Way[way, entry].Data[index + 1] = (byte)((value >> 16) & 0xFF);
            _state.Way[way, entry].Data[index + 2] = (byte)((value >> 8) & 0xFF);
            _state.Way[way, entry].Data[index + 3] = (byte)(value & 0xFF);
        }
    }

    internal byte CacheMemoryReadByte(uint address)
    {
        switch (address & AreaMask)
        {
            case CacheUse:
            {
                if (_state.Enable != 0)
                {
                    uint tag = address & TagMask;
                    int entry = (int)((address & EntryMask) >> 4);
                    uint offset = address & LineMask;
                    if (_state.Way[0, entry].Valid != 0 && _state.Way[0, entry].Tag == tag)
                    {
                        UpdateLru(0, ref _state.Lru[entry]);
                        return _state.Way[0, entry].Data[offset];
                    }
                    else if (_state.Way[1, entry].Valid != 0 && _state.Way[1, entry].Tag == tag)
                    {
                        UpdateLru(1, ref _state.Lru[entry]);
                        return _state.Way[1, entry].Data[offset];
                    }
                    else if (_state.Way[2, entry].Valid != 0 && _state.Way[2, entry].Tag == tag)
                    {
                        UpdateLru(2, ref _state.Lru[entry]);
                        return _state.Way[2, entry].Data[offset];
                    }
                    else if (_state.Way[3, entry].Valid != 0 && _state.Way[3, entry].Tag == tag)
                    {
                        UpdateLru(3, ref _state.Lru[entry]);
                        return _state.Way[3, entry].Data[offset];
                    }

                    int replacementWay = SelectWayToReplace(_core.CacheControl, _state.Lru[entry]);
                    UpdateLru(replacementWay, ref _state.Lru[entry]);
                    _state.Way[replacementWay, entry].Tag = tag;
                    RefillCache(replacementWay, entry, address);
                    _state.Way[replacementWay, entry].Valid = 1;
                    return _state.Way[replacementWay, entry].Data[offset];
                }

                return _memory.MappedMemoryReadByteNocache(address);
            }
            case CacheThrough:
                _addCycles(GetCacheThroughTimingReadByteWord(address));
                return _memory.MappedMemoryReadByteNocache(address);
            default:
                return _memory.MappedMemoryReadByteNocache(address);
        }
    }

    internal ushort CacheMemoryReadWord(uint address)
    {
        switch (address & AreaMask)
        {
            case CacheUse:
            {
                if (_state.Enable != 0)
                {
                    uint tag = address & TagMask;
                    int entry = (int)((address & EntryMask) >> 4);
                    uint offset = address & LineMask;
                    if (_state.Way[0, entry].Valid != 0 && _state.Way[0, entry].Tag == tag)
                    {
                        UpdateLru(0, ref _state.Lru[entry]);
                        return (ushort)((_state.Way[0, entry].Data[offset] << 8) | _state.Way[0, entry].Data[offset + 1]);
                    }
                    else if (_state.Way[1, entry].Valid != 0 && _state.Way[1, entry].Tag == tag)
                    {
                        UpdateLru(1, ref _state.Lru[entry]);
                        return (ushort)((_state.Way[1, entry].Data[offset] << 8) | _state.Way[1, entry].Data[offset + 1]);
                    }
                    else if (_state.Way[2, entry].Valid != 0 && _state.Way[2, entry].Tag == tag)
                    {
                        UpdateLru(2, ref _state.Lru[entry]);
                        return (ushort)((_state.Way[2, entry].Data[offset] << 8) | _state.Way[2, entry].Data[offset + 1]);
                    }
                    else if (_state.Way[3, entry].Valid != 0 && _state.Way[3, entry].Tag == tag)
                    {
                        UpdateLru(3, ref _state.Lru[entry]);
                        return (ushort)((_state.Way[3, entry].Data[offset] << 8) | _state.Way[3, entry].Data[offset + 1]);
                    }

                    int replacementWay = SelectWayToReplace(_core.CacheControl, _state.Lru[entry]);
                    UpdateLru(replacementWay, ref _state.Lru[entry]);
                    _state.Way[replacementWay, entry].Tag = tag;
                    RefillCache(replacementWay, entry, address);
                    _state.Way[replacementWay, entry].Valid = 1;
                    return (ushort)((_state.Way[replacementWay, entry].Data[offset] << 8) | _state.Way[replacementWay, entry].Data[offset + 1]);
                }

                return _memory.MappedMemoryReadWordNocache(address);
            }
            case CacheThrough:
                _addCycles(GetCacheThroughTimingReadByteWord(address));
                return _memory.MappedMemoryReadWordNocache(address);
            default:
                return _memory.MappedMemoryReadWordNocache(address);
        }
    }

    internal uint CacheMemoryReadLong(uint address)
    {
        switch (address & AreaMask)
        {
            case CacheUse:
            {
                if (_state.Enable != 0)
                {
                    uint tag = address & TagMask;
                    int entry = (int)((address & EntryMask) >> 4);
                    uint offset = address & LineMask;
                    if (_state.Way[0, entry].Valid != 0 && _state.Way[0, entry].Tag == tag)
                    {
                        UpdateLru(0, ref _state.Lru[entry]);
                        return ((uint)_state.Way[0, entry].Data[offset] << 24) | ((uint)_state.Way[0, entry].Data[offset + 1] << 16) | ((uint)_state.Way[0, entry].Data[offset + 2] << 8) | _state.Way[0, entry].Data[offset + 3];
                    }
                    else if (_state.Way[1, entry].Valid != 0 && _state.Way[1, entry].Tag == tag)
                    {
                        UpdateLru(1, ref _state.Lru[entry]);
                        return ((uint)_state.Way[1, entry].Data[offset] << 24) | ((uint)_state.Way[1, entry].Data[offset + 1] << 16) | ((uint)_state.Way[1, entry].Data[offset + 2] << 8) | _state.Way[1, entry].Data[offset + 3];
                    }
                    else if (_state.Way[2, entry].Valid != 0 && _state.Way[2, entry].Tag == tag)
                    {
                        UpdateLru(2, ref _state.Lru[entry]);
                        return ((uint)_state.Way[2, entry].Data[offset] << 24) | ((uint)_state.Way[2, entry].Data[offset + 1] << 16) | ((uint)_state.Way[2, entry].Data[offset + 2] << 8) | _state.Way[2, entry].Data[offset + 3];
                    }
                    else if (_state.Way[3, entry].Valid != 0 && _state.Way[3, entry].Tag == tag)
                    {
                        UpdateLru(3, ref _state.Lru[entry]);
                        return ((uint)_state.Way[3, entry].Data[offset] << 24) | ((uint)_state.Way[3, entry].Data[offset + 1] << 16) | ((uint)_state.Way[3, entry].Data[offset + 2] << 8) | _state.Way[3, entry].Data[offset + 3];
                    }

                    int replacementWay = SelectWayToReplace(_core.CacheControl, _state.Lru[entry]);
                    UpdateLru(replacementWay, ref _state.Lru[entry]);
                    _state.Way[replacementWay, entry].Tag = tag;
                    RefillCache(replacementWay, entry, address);
                    _state.Way[replacementWay, entry].Valid = 1;
                    return ((uint)_state.Way[replacementWay, entry].Data[offset] << 24) | ((uint)_state.Way[replacementWay, entry].Data[offset + 1] << 16) | ((uint)_state.Way[replacementWay, entry].Data[offset + 2] << 8) | _state.Way[replacementWay, entry].Data[offset + 3];
                }

                return _memory.MappedMemoryReadLongNocache(address);
            }
            case CacheThrough:
                _addCycles(GetCacheThroughTimingReadLong(address));
                return _memory.MappedMemoryReadLongNocache(address);
            default:
                return _memory.MappedMemoryReadLongNocache(address);
        }
    }
}
