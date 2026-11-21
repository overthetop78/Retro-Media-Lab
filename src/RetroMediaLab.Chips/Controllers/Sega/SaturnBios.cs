// Copyright 2006-2007 Theo Berkau
// Translated from Yabause bios.c.
// SPDX-License-Identifier: GPL-2.0-or-later

using RetroMediaLab.Chips.CPU.Hitachi.SH2;
using MemoryMap = RetroMediaLab.Chips.Controllers.Sega.SaturnMemory.Sh2MemoryMap;

namespace RetroMediaLab.Chips.Controllers.Sega;

public sealed class SaturnBios(
    SaturnMemory mainMemory, SaturnCartridge<SaturnSh2Core> cartridge, SaturnSmpc smpc, Action flushBackups)
{
    private readonly byte[] _sh2MaskList =
    [
        0xF0, 0xE0, 0xD0, 0xC0, 0xB0, 0xA0, 0x90, 0x80,
        0x80, 0x70, 0x70, 0x70, 0x70, 0x70, 0x70, 0x70,
        0x70, 0x70, 0x70, 0x70, 0x70, 0x70, 0x70, 0x70,
        0x70, 0x70, 0x70, 0x70, 0x70, 0x70, 0x70, 0x70
    ];

    private readonly uint[] _scuMaskList =
    [
        0xFFFFFFFF, 0xFFFFFFFE, 0xFFFFFFFC, 0xFFFFFFF8,
        0xFFFFFFF0, 0xFFFFFFE0, 0xFFFFFFC0, 0xFFFFFF80,
        0xFFFFFF80, 0xFFFFFE00, 0xFFFFFE00, 0xFFFFFE00,
        0xFFFFFE00, 0xFFFFFE00, 0xFFFFFE00, 0xFFFFFE00,
        0xFFFFFE00, 0xFFFFFE00, 0xFFFFFE00, 0xFFFFFE00,
        0xFFFFFE00, 0xFFFFFE00, 0xFFFFFE00, 0xFFFFFE00,
        0xFFFFFE00, 0xFFFFFE00, 0xFFFFFE00, 0xFFFFFE00,
        0xFFFFFE00, 0xFFFFFE00, 0xFFFFFE00, 0xFFFFFE00
    ];

    private readonly uint[][] _interruptList = [new uint[0x80], new uint[0x80]];

    public void Initialize(MemoryMap master)
    {
        master.MappedMemoryWriteLongNocache(0x06000600, 0x002B0009);
        master.MappedMemoryWriteLongNocache(0x06000604, 0xE0F0600C);
        master.MappedMemoryWriteLongNocache(0x06000608, 0x400E8BFE);
        master.MappedMemoryWriteLongNocache(0x0600060C, 0x00090009);
        master.MappedMemoryWriteLongNocache(0x06000610, 0x000B0009);

        for (uint i = 0; i < 0x200; i += 4)
        {
            master.MappedMemoryWriteLongNocache(0x06000000 + i, 0x06000600);
            master.MappedMemoryWriteLongNocache(0x06000400 + i, 0x06000600);
            _interruptList[0][i >> 2] = 0x06000600;
            _interruptList[1][i >> 2] = 0x06000600;
        }

        master.MappedMemoryWriteLongNocache(0x06000010, 0x06000604);
        master.MappedMemoryWriteLongNocache(0x06000018, 0x06000604);
        master.MappedMemoryWriteLongNocache(0x06000024, 0x06000604);
        master.MappedMemoryWriteLongNocache(0x06000028, 0x06000604);
        _interruptList[0][4] = 0x06000604;
        _interruptList[0][6] = 0x06000604;
        _interruptList[0][9] = 0x06000604;
        _interruptList[0][10] = 0x06000604;

        master.MappedMemoryWriteLongNocache(0x06000410, 0x06000604);
        master.MappedMemoryWriteLongNocache(0x06000418, 0x06000604);
        master.MappedMemoryWriteLongNocache(0x06000424, 0x06000604);
        master.MappedMemoryWriteLongNocache(0x06000428, 0x06000604);
        _interruptList[1][4] = 0x06000604;
        _interruptList[1][6] = 0x06000604;
        _interruptList[1][9] = 0x06000604;
        _interruptList[1][10] = 0x06000604;

        for (uint i = 0; i < 0x38; i += 4)
        {
            master.MappedMemoryWriteLongNocache(0x06000100 + i, 0x00000400 + i);
            _interruptList[0][0x40 + (i >> 2)] = 0x00000400 + i;
        }
        for (uint i = 0; i < 0x40; i += 4)
        {
            master.MappedMemoryWriteLongNocache(0x06000140 + i, 0x00000440 + i);
            _interruptList[0][0x50 + (i >> 2)] = 0x00000440 + i;
        }
        for (uint i = 0; i < 0x100; i += 4)
            master.MappedMemoryWriteLongNocache(0x06000A00 + i, 0x06000610);

        master.MappedMemoryWriteLongNocache(0x06000210, 0x00000210);
        master.MappedMemoryWriteLongNocache(0x0600026C, 0x0000026C);
        master.MappedMemoryWriteLongNocache(0x06000274, 0x00000274);
        master.MappedMemoryWriteLongNocache(0x06000280, 0x00000280);
        master.MappedMemoryWriteLongNocache(0x0600029C, 0x0000029C);
        master.MappedMemoryWriteLongNocache(0x060002DC, 0x000002DC);
        master.MappedMemoryWriteLongNocache(0x06000300, 0x00000300);
        master.MappedMemoryWriteLongNocache(0x06000304, 0x00000304);
        master.MappedMemoryWriteLongNocache(0x06000310, 0x00000310);
        master.MappedMemoryWriteLongNocache(0x06000314, 0x00000314);
        master.MappedMemoryWriteLongNocache(0x06000320, 0x00000320);
        master.MappedMemoryWriteLongNocache(0x06000324, 0x00000000);
        master.MappedMemoryWriteLongNocache(0x06000330, 0x00000330);
        master.MappedMemoryWriteLongNocache(0x06000334, 0x00000334);
        master.MappedMemoryWriteLongNocache(0x06000340, 0x00000340);
        master.MappedMemoryWriteLongNocache(0x06000344, 0x00000344);
        master.MappedMemoryWriteLongNocache(0x06000348, 0xFFFFFFFF);
        master.MappedMemoryWriteLongNocache(0x06000354, 0x00000000);
        master.MappedMemoryWriteLongNocache(0x06000358, 0x00000358);
    }

    private static void SetScuInterrupt(SaturnSh2State sh, MemoryMap memory)
    {
        uint address = 0x06000900 + (sh.R[4] << 2);
        if (sh.R[5] == 0)
        {
            memory.Write32(address, 0x06000610);
            sh.Cycles += 8;
        }
        else
        {
            memory.Write32(address, sh.R[5]);
            sh.Cycles += 9;
        }
        sh.PC = sh.PR;
    }

    private static void GetScuInterrupt(SaturnSh2State sh, MemoryMap memory)
    {
        sh.R[0] = memory.Read32(0x06000900 + (sh.R[4] << 2));
        sh.Cycles += 5;
        sh.PC = sh.PR;
    }

    private void SetSh2Interrupt(SaturnSh2State sh, MemoryMap memory, bool isSlave)
    {
        uint address = sh.VBR + (sh.R[4] << 2);
        if (sh.R[5] == 0)
        {
            memory.Write32(address, _interruptList[isSlave ? 1 : 0][sh.R[4]]);
            sh.Cycles += 8;
        }
        else
        {
            memory.Write32(address, sh.R[5]);
            sh.Cycles += 9;
        }
        sh.PC = sh.PR;
    }

    private static void GetSh2Interrupt(SaturnSh2State sh, MemoryMap memory)
    {
        sh.R[0] = memory.Read32(sh.VBR + (sh.R[4] << 2));
        sh.Cycles += 5;
        sh.PC = sh.PR;
    }

    private static void SetScuInterruptMask(SaturnSh2State sh, MemoryMap memory, bool isSlave)
    {
        if (!isSlave)
        {
            memory.Write32(0x06000348, sh.R[4]);
            memory.Write32(0x25FE00A0, sh.R[4]);
        }

        if ((sh.R[4] & 0x8000) == 0)
            memory.Write32(0x25FE00A8, 1);
        sh.Cycles += 17;
        sh.PC = sh.PR;
    }

    private static void ChangeScuInterruptMask(SaturnSh2State sh, MemoryMap memory, bool isSlave)
    {
        uint newMask = (memory.Read32(0x06000348) & sh.R[4]) | sh.R[5];
        if (!isSlave)
        {
            memory.Write32(0x06000348, newMask);
            memory.Write32(0x25FE00A0, newMask);
            memory.Write32(0x25FE00A4, unchecked((uint)(short)sh.R[4]));
        }

        if ((sh.R[4] & 0x8000) == 0)
            memory.Write32(0x25FE00A8, 1);
        sh.Cycles += 20;
        sh.PC = sh.PR;
    }

    private static void CdInit2(SaturnSh2State sh)
    {
        sh.PC = sh.PR;
    }

    private static void CdInit1(SaturnSh2State sh)
    {
        sh.PC = sh.PR;
    }

    private static void GetSemaphore(SaturnSh2State sh, MemoryMap memory)
    {
        uint address = 0x06000B00 + sh.R[4];
        byte value = memory.Read8(address);
        sh.R[0] = value == 0 ? 1u : 0u;
        memory.Write8(address, (byte)(value | 0x80));
        sh.Cycles += 11;
        sh.PC = sh.PR;
    }

    private static void ClearSemaphore(SaturnSh2State sh, MemoryMap memory)
    {
        memory.Write8(0x06000B00 + sh.R[4], 0);
        sh.Cycles += 5;
        sh.PC = sh.PR;
    }

    private static void ChangeSystemClock(SaturnSh2State sh, MemoryMap memory, SaturnSmpc smpc)
    {
        memory.Write32(0x06000324, sh.R[4]);
        memory.Write32(0x25FE00A8, 0);
        memory.Write32(0x25FE00B8, 0);
        memory.Write8(0xFFFFFE91, 0x80);
        memory.Write16(0xFFFFFE80, 0xA51D);
        memory.Write16(0xFFFFFEE0, 0x8000);

        if (sh.R[4] == 0)
            smpc.ChangeClock320();
        else
            smpc.ChangeClock352();

        for (uint channel = 0; channel < 3; channel++)
        {
            for (uint i = 0; i < 7; i++)
                memory.Write32(0x25FE0000 + channel * 0xC + i * 4, 0);
        }

        memory.Write32(0x25FE0060, 0);
        memory.Write32(0x25FE0080, 0);
        memory.Write32(0x25FE00B0, 0x1FF01FF0);
        memory.Write32(0x25FE00B4, 0x1FF01FF0);
        memory.Write32(0x25FE00B8, 0x1F);
        memory.Write32(0x25FE00A8, 1);
        memory.Write32(0x25FE0090, 0x3FF);
        memory.Write32(0x25FE0094, 0x1FF);
        memory.Write32(0x25FE0098, 0);

        uint mask = memory.Read32(0x06000348);
        memory.Write32(0x25FE00A0, mask);
        if ((mask & 0x8000) == 0)
            memory.Write32(0x25FE00A8, 1);
        sh.PC = sh.PR;
    }

    private void ChangeScuInterruptPriority(SaturnSh2State sh, MemoryMap memory)
    {
        for (uint i = 0; i < 0x20; i++)
        {
            _scuMaskList[i] = memory.Read32(sh.R[4] + (i << 2));
            _sh2MaskList[i] = unchecked((byte)(_scuMaskList[i] >> 16));
            if ((_scuMaskList[i] & 0x8000) != 0)
                _scuMaskList[i] |= 0xFFFF0000;
            else
                _scuMaskList[i] &= 0x0000FFFF;
        }
        sh.Cycles += 186;
        sh.PC = sh.PR;
    }

    private static void ExecuteCdPlayer(SaturnSh2State sh)
    {
        sh.PC = sh.PR;
    }

    private static void PowerOnMemoryClear(SaturnSh2State sh)
    {
        sh.PC = sh.PR;
    }

    private static void CheckMpegCard(SaturnSh2State sh)
    {
        sh.PC = sh.PR;
    }

    private static uint GetDeviceStats(uint device, byte cartridgeId, out uint size, out uint address, out uint blockSize)
    {
        switch (device)
        {
            case 0:
                address = 0x00180000;
                size = 0x8000;
                blockSize = 0x40;
                return 0;
            case 1 when (cartridgeId & 0xF0) == 0x20:
                address = 0x04000000;
                size = 0x40000u << (cartridgeId & 0xF);
                blockSize = cartridgeId == 0x24 ? 0x400u : 0x200u;
                return 0;
            default:
                address = size = blockSize = 0;
                return 1;
        }
    }

    private static int CheckHeader(uint device) => 0;

    private static uint CalcSaveSize(MemoryMap memory, uint tableAddress, uint blockSize)
    {
        uint blockCount = 0;
        for (;;)
        {
            if (((tableAddress - 1) & ((blockSize << 1) - 1)) == 0)
                tableAddress += 8;
            int block = (memory.Read8(tableAddress) << 8) | memory.Read8(tableAddress + 2);
            if (block == 0)
                break;
            tableAddress += 4;
            blockCount++;
        }
        return blockCount;
    }

    private static uint GetFreeSpace(MemoryMap memory, uint size, uint address, uint blockSize)
    {
        uint usedBlocks = 0;
        for (uint i = (2 * blockSize) << 1; i < (size << 1); i += blockSize << 1)
        {
            if ((memory.Read8(address + i + 1) & 0x80) != 0)
                usedBlocks += CalcSaveSize(memory, address + i + 0x45, blockSize) + 1;
        }
        return unchecked(size / blockSize - 2 - usedBlocks);
    }

    private static uint FindSave(MemoryMap memory, uint stringAddress, uint blockOffset, uint size, uint address, uint blockSize)
    {
        for (uint i = (blockOffset * blockSize) << 1; i < (size << 1); i += blockSize << 1)
        {
            if ((memory.Read8(address + i + 1) & 0x80) == 0)
                continue;

            for (uint j = 0; j < 11; j++)
            {
                byte data = memory.Read8(stringAddress + j);
                if (memory.Read8(address + i + 0x9 + j * 2) != data)
                {
                    if (data == 0)
                        return (i / blockSize) >> 1;
                    break;
                }
                if (j == 10 || data == 0)
                    return (i / blockSize) >> 1;
            }
        }
        return 0;
    }

    private static uint FindSave2(MemoryMap memory, ReadOnlySpan<byte> name, uint blockOffset, uint size, uint address, uint blockSize)
    {
        for (uint i = (blockOffset * blockSize) << 1; i < (size << 1); i += blockSize << 1)
        {
            if ((memory.Read8(address + i + 1) & 0x80) == 0)
                continue;

            for (int j = 0; j < 11; j++)
            {
                byte data = j < name.Length ? name[j] : (byte)0;
                if (memory.Read8(address + i + 0x9 + (uint)j * 2) != data)
                {
                    if (data == 0)
                        return (i / blockSize) >> 1;
                    break;
                }
                if (j == 10 || data == 0)
                    return (i / blockSize) >> 1;
            }
        }
        return 0;
    }

    private static void DeleteSave(MemoryMap memory, uint address, uint blockOffset, uint blockSize)
    {
        memory.Write8(address + blockOffset * blockSize * 2 + 1, 0);
    }

    private static ushort[]? GetFreeBlocks(MemoryMap memory, uint address, uint blockSize, uint blockCount, uint size)
    {
        byte[] used;
        try { used = new byte[size / blockSize]; }
        catch (OutOfMemoryException) { return null; }

        for (uint i = (2 * blockSize) << 1; i < (size << 1); i += blockSize << 1)
        {
            if ((memory.Read8(address + i + 1) & 0x80) == 0)
                continue;

            uint tableAddress = address + i + 0x45;
            used[i / (blockSize << 1)] = 1;
            for (;;)
            {
                if (((tableAddress - 1) & ((blockSize << 1) - 1)) == 0)
                    tableAddress += 8;
                int block = (memory.Read8(tableAddress) << 8) | memory.Read8(tableAddress + 2);
                if (block == 0)
                    break;
                tableAddress += 4;
                used[block] = 1;
            }
        }

        ushort[] free;
        try { free = new ushort[blockCount]; }
        catch (OutOfMemoryException) { return null; }

        uint found = 0;
        for (uint i = 2; i < size / blockSize; i++)
        {
            if (used[i] == 0)
            {
                free[found++] = (ushort)i;
                if (found >= blockCount)
                    break;
            }
        }
        return free;
    }

    private static ushort[]? ReadBlockTable(
        MemoryMap memory, uint address, uint block, uint blockSize,
        out uint tableAddress, out uint blockCount, out uint blocksRead)
    {
        tableAddress = address + block * blockSize * 2 + 0x45;
        blocksRead = 0;
        blockCount = CalcSaveSize(memory, tableAddress, blockSize);

        ushort[] table;
        try { table = new ushort[blockCount]; }
        catch (OutOfMemoryException) { return null; }

        for (uint i = 0; i < blockCount; i++)
        {
            ushort entry = (ushort)((memory.Read8(tableAddress) << 8) | memory.Read8(tableAddress + 2));
            tableAddress += 4;
            if (((tableAddress - 1) & ((blockSize << 1) - 1)) == 0)
                tableAddress = address + table[blocksRead++] * blockSize * 2 + 9;
            table[i] = entry;
        }
        tableAddress += 4;
        return table;
    }

    private static void BupInit(SaturnSh2State sh, MemoryMap memory, byte cartridgeId)
    {
        memory.Write32(0x06000354, sh.R[5]);
        memory.Write32(sh.R[5] + 0x00, 0x00000380);
        memory.Write32(sh.R[5] + 0x04, 0x00000384);
        memory.Write32(sh.R[5] + 0x08, 0x00000388);
        memory.Write32(sh.R[5] + 0x0C, 0x0000038C);
        memory.Write32(sh.R[5] + 0x10, 0x00000390);
        memory.Write32(sh.R[5] + 0x14, 0x00000394);
        memory.Write32(sh.R[5] + 0x18, 0x00000398);
        memory.Write32(sh.R[5] + 0x1C, 0x0000039C);
        memory.Write32(sh.R[5] + 0x20, 0x000003A0);
        memory.Write32(sh.R[5] + 0x24, 0x000003A4);
        memory.Write32(sh.R[5] + 0x28, 0x000003A8);
        memory.Write32(sh.R[5] + 0x2C, 0x000003AC);
        memory.Write16(sh.R[6], 1);
        memory.Write16(sh.R[6] + 2, 1);
        if ((cartridgeId & 0xF0) == 0x20)
        {
            memory.Write16(sh.R[6] + 4, 2);
            memory.Write16(sh.R[6] + 6, 1);
        }
        else
        {
            memory.Write16(sh.R[6] + 4, 0);
            memory.Write16(sh.R[6] + 6, 0);
        }
        memory.Write16(sh.R[6] + 8, 0);
        memory.Write16(sh.R[6] + 0xA, 0);
        sh.PC = sh.PR;
    }

    private static void BupSelectPartition(SaturnSh2State sh)
    {
        sh.R[0] = 0;
        sh.PC = sh.PR;
    }

    private void BupFormat(SaturnSh2State sh)
    {
        FormatBackup(sh.R[4]);
        sh.R[0] = 0;
        sh.PC = sh.PR;
    }

    private static void BupStatus(SaturnSh2State sh, MemoryMap memory, byte cartridgeId)
    {
        uint result = GetDeviceStats(sh.R[4], cartridgeId, out uint size, out uint address, out uint blockSize);
        if (result == 1 || CheckHeader(sh.R[4]) != 0)
        {
            sh.R[0] = result;
            sh.PC = sh.PR;
            return;
        }

        uint freeBlocks = GetFreeSpace(memory, size, address, blockSize);
        uint freeSpace = unchecked((blockSize - 6) * freeBlocks - 30);
        int afterSize = unchecked((int)(freeSpace - sh.R[5]));
        if (afterSize < 0)
            afterSize = 0;

        memory.Write32(sh.R[6], size);
        memory.Write32(sh.R[6] + 0x04, size / blockSize);
        memory.Write32(sh.R[6] + 0x08, blockSize);
        memory.Write32(sh.R[6] + 0x0C, freeSpace);
        memory.Write32(sh.R[6] + 0x10, freeBlocks);
        memory.Write32(sh.R[6] + 0x14, (uint)afterSize / blockSize);
        sh.R[0] = result;
        sh.PC = sh.PR;
    }

    private static void BupWrite(SaturnSh2State sh, MemoryMap memory, byte cartridgeId, Action flushBackups)
    {
        uint result = GetDeviceStats(sh.R[4], cartridgeId, out uint size, out uint address, out uint blockSize);
        if (result == 1)
        {
            sh.R[0] = result;
            sh.PC = sh.PR;
            return;
        }

        uint block = FindSave(memory, sh.R[5], 2, size, address, blockSize);
        if (block != 0)
        {
            if (sh.R[7] != 0)
            {
                sh.R[0] = 6;
                sh.PC = sh.PR;
                return;
            }
            DeleteSave(memory, address, block, blockSize);
        }

        uint dataSize = memory.Read32(sh.R[5] + 0x1C);
        uint saveSize = 1 + unchecked(dataSize + 0x1D) / (blockSize - 6);
        if (saveSize > GetFreeSpace(memory, size, address, blockSize))
        {
            sh.R[0] = 4;
            sh.PC = sh.PR;
            return;
        }

        ushort[]? table = GetFreeBlocks(memory, address, blockSize, saveSize, size);
        if (table is null)
        {
            sh.R[0] = 8;
            sh.PC = sh.PR;
            return;
        }

        uint workAddress = address + table[0] * blockSize * 2;
        memory.Write8(workAddress + 1, 0x80);

        for (uint i = workAddress + 0x9; i < workAddress + 0x9 + 11 * 2; i += 2)
        {
            memory.Write8(i, memory.Read8(sh.R[5]));
            sh.R[5]++;
        }
        sh.R[5]++;

        for (uint i = workAddress + 0x21; i < workAddress + 0x21 + 10 * 2; i += 2)
        {
            memory.Write8(i, memory.Read8(sh.R[5]));
            sh.R[5]++;
        }
        memory.Write8(workAddress + 0x1F, memory.Read8(sh.R[5]));
        sh.R[5] += 2;

        for (uint i = workAddress + 0x35; i < workAddress + 0x35 + 4 * 2; i += 2)
        {
            memory.Write8(i, memory.Read8(sh.R[5]));
            sh.R[5]++;
        }
        for (uint i = workAddress + 0x3D; i < workAddress + 0x3D + 4 * 2; i += 2)
        {
            memory.Write8(i, memory.Read8(sh.R[5]));
            sh.R[5]++;
        }

        workAddress += 0x45;
        uint blocksWritten = 0;
        for (uint i = 1; i < saveSize; i++)
        {
            memory.Write8(workAddress, (byte)(table[i] >> 8));
            workAddress += 2;
            memory.Write8(workAddress, unchecked((byte)table[i]));
            workAddress += 2;
            if (((workAddress - 1) & ((blockSize << 1) - 1)) == 0)
                workAddress = address + table[++blocksWritten] * blockSize * 2 + 9;
        }
        memory.Write8(workAddress, 0);
        workAddress += 2;
        memory.Write8(workAddress, 0);
        workAddress += 2;

        while (dataSize > 0)
        {
            if (((workAddress - 1) & ((blockSize << 1) - 1)) == 0)
                workAddress = address + table[++blocksWritten] * blockSize * 2 + 9;
            memory.Write8(workAddress, memory.Read8(sh.R[6]));
            dataSize--;
            sh.R[6]++;
            workAddress += 2;
        }

        flushBackups();
        sh.R[0] = 0;
        sh.PC = sh.PR;
    }

    private static void BupRead(SaturnSh2State sh, MemoryMap memory, byte cartridgeId)
    {
        uint result = GetDeviceStats(sh.R[4], cartridgeId, out uint size, out uint address, out uint blockSize);
        if (result == 1)
        {
            sh.R[0] = result;
            sh.PC = sh.PR;
            return;
        }

        uint block = FindSave(memory, sh.R[5], 2, size, address, blockSize);
        if (block == 0)
        {
            sh.R[0] = 5;
            sh.PC = sh.PR;
            return;
        }

        uint tableAddress = address + block * blockSize * 2 + 0x3D;
        uint dataSize = ((uint)memory.Read8(tableAddress) << 24) | ((uint)memory.Read8(tableAddress + 2) << 16) |
            ((uint)memory.Read8(tableAddress + 4) << 8) | memory.Read8(tableAddress + 6);
        ushort[]? table = ReadBlockTable(memory, address, block, blockSize, out tableAddress, out _, out uint blocksRead);
        if (table is null)
        {
            sh.R[0] = 8;
            sh.PC = sh.PR;
            return;
        }

        while (dataSize > 0)
        {
            if (((tableAddress - 1) & ((blockSize << 1) - 1)) == 0)
            {
                tableAddress = address + table[blocksRead] * blockSize * 2 + 9;
                blocksRead++;
            }

            memory.Write8(sh.R[6], memory.Read8(tableAddress));
            dataSize--;
            sh.R[6]++;
            tableAddress += 2;
        }

        sh.R[0] = 0;
        sh.PC = sh.PR;
    }

    private static void BupDelete(SaturnSh2State sh, MemoryMap memory, byte cartridgeId)
    {
        uint result = GetDeviceStats(sh.R[4], cartridgeId, out uint size, out uint address, out uint blockSize);
        if (result == 1)
        {
            sh.R[0] = result;
            sh.PC = sh.PR;
            return;
        }

        uint block = FindSave(memory, sh.R[5], 2, size, address, blockSize);
        if (block == 0)
        {
            sh.R[0] = 5;
            sh.PC = sh.PR;
            return;
        }

        DeleteSave(memory, address, block, blockSize);
        sh.R[0] = 0;
        sh.PC = sh.PR;
    }

    private static void BupDirectory(SaturnSh2State sh, MemoryMap memory, byte cartridgeId)
    {
        Span<byte> filename = stackalloc byte[12];
        for (uint i = 0; i < 12; i++)
            filename[(int)i] = memory.Read8(sh.R[5] + i);

        uint result = GetDeviceStats(sh.R[4], cartridgeId, out uint size, out uint address, out uint blockSize);
        if (result == 1)
        {
            sh.R[0] = result;
            sh.PC = sh.PR;
            return;
        }

        uint blockOffset = 2;
        uint entryIndex;
        for (entryIndex = 0; entryIndex < 256; entryIndex++)
        {
            uint block = FindSave(memory, sh.R[5], blockOffset, size, address, blockSize);
            if (block == 0)
                break;
            blockOffset = block + 1;
        }

        if (sh.R[6] < entryIndex)
        {
            sh.R[0] = unchecked((uint)-(int)entryIndex);
            sh.PC = sh.PR;
            return;
        }

        blockOffset = 2;
        for (entryIndex = 0; entryIndex < sh.R[6]; entryIndex++)
        {
            uint dataSize = 0;
            uint block = FindSave(memory, sh.R[5], blockOffset, size, address, blockSize);
            if (block == 0)
                break;
            blockOffset = block + 1;
            block = address + blockSize * block * 2;

            for (uint i = block + 0x9; i < block + 0x9 + 11 * 2; i += 2)
            {
                memory.Write8(sh.R[7], memory.Read8(i));
                sh.R[7]++;
            }
            memory.Write8(sh.R[7], 0);
            sh.R[7]++;

            for (uint i = block + 0x21; i < block + 0x21 + 10 * 2; i += 2)
            {
                memory.Write8(sh.R[7], memory.Read8(i));
                sh.R[7]++;
            }
            memory.Write8(sh.R[7], memory.Read8(block + 0x1F));
            sh.R[7]++;
            memory.Write8(sh.R[7], 0);
            sh.R[7]++;

            for (uint i = block + 0x35; i < block + 0x35 + 4 * 2; i += 2)
            {
                memory.Write8(sh.R[7], memory.Read8(i));
                sh.R[7]++;
            }

            for (uint i = block + 0x3D; i < block + 0x3D + 4 * 2; i += 2)
            {
                dataSize <<= 8;
                byte data = memory.Read8(i);
                memory.Write8(sh.R[7], data);
                dataSize |= data;
                sh.R[7]++;
            }

            memory.Write16(sh.R[7], unchecked((ushort)((dataSize + 0x1D) / (blockSize - 6) + 1)));
            sh.R[7] += 4;
        }

        sh.R[0] = entryIndex;
        sh.PC = sh.PR;
    }

    private static void BupVerify(SaturnSh2State sh, MemoryMap memory, byte cartridgeId)
    {
        uint result = GetDeviceStats(sh.R[4], cartridgeId, out uint size, out uint address, out uint blockSize);
        if (result == 1)
        {
            sh.R[0] = result;
            sh.PC = sh.PR;
            return;
        }

        uint block = FindSave(memory, sh.R[5], 2, size, address, blockSize);
        if (block == 0)
        {
            sh.R[0] = 5;
            sh.PC = sh.PR;
            return;
        }

        uint tableAddress = address + block * blockSize * 2 + 0x3D;
        uint dataSize = ((uint)memory.Read8(tableAddress) << 24) | ((uint)memory.Read8(tableAddress + 2) << 16) |
            ((uint)memory.Read8(tableAddress + 4) << 8) | memory.Read8(tableAddress + 6);
        ushort[]? table = ReadBlockTable(memory, address, block, blockSize, out tableAddress, out _, out uint blocksRead);
        if (table is null)
        {
            sh.R[0] = 8;
            sh.PC = sh.PR;
            return;
        }

        while (dataSize > 0)
        {
            if (((tableAddress - 1) & ((blockSize << 1) - 1)) == 0)
            {
                tableAddress = address + table[blocksRead] * blockSize * 2 + 9;
                blocksRead++;
            }

            if (memory.Read8(sh.R[6]) != memory.Read8(tableAddress))
            {
                sh.R[0] = 7;
                sh.PC = sh.PR;
                return;
            }

            dataSize--;
            sh.R[6]++;
            tableAddress += 2;
        }

        sh.R[0] = 0;
        sh.PC = sh.PR;
    }

    private static void ConvertMonthAndDay(MemoryMap memory, uint data, uint monthAddress, uint dayAddress, int type)
    {
        ReadOnlySpan<ushort> months = [31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334];
        if (data < months[0])
        {
            memory.Write8(monthAddress, 1);
            memory.Write8(dayAddress, (byte)(data + 1));
            return;
        }

        int i;
        for (i = 1; i < 11; i++)
        {
            if (data <= months[i])
                break;
        }

        if (type == 1)
        {
            memory.Write8(monthAddress, (byte)(i + 1));
            if (i + 1 == 2)
                memory.Write8(dayAddress, unchecked((byte)(data - months[i - 1] + 1)));
            else
                memory.Write8(dayAddress, unchecked((byte)(data - months[i - 1])));
        }
        else
        {
            memory.Write8(monthAddress, (byte)(i + 1));
            memory.Write8(dayAddress, unchecked((byte)(data - months[i - 1] + 1)));
        }
    }

    private static void BupGetDate(SaturnSh2State sh, MemoryMap memory)
    {
        uint date = sh.R[4];
        memory.Write8(sh.R[5] + 3, (byte)((date % 0x5A0) / 0x3C));
        memory.Write8(sh.R[5] + 4, (byte)(date % 0x3C));

        uint days = date / 0x5A0;
        if (days > 0xAB71)
            memory.Write8(sh.R[5] + 5, (byte)((days + 1) % 7));
        else
            memory.Write8(sh.R[5] + 5, (byte)((days + 2) % 7));

        uint yearRemainder = days % 0x5B5;
        uint yearOffset;
        if (yearRemainder > 0x16E)
        {
            yearOffset = (yearRemainder - 1) / 0x16D;
            ConvertMonthAndDay(memory, (yearRemainder - 1) % 0x16D, sh.R[5] + 1, sh.R[5] + 2, 0);
        }
        else
        {
            yearOffset = 0;
            ConvertMonthAndDay(memory, 0, sh.R[5] + 1, sh.R[5] + 2, 1);
        }

        memory.Write8(sh.R[5], unchecked((byte)((days / 0x5B5) * 4 + yearOffset)));
        sh.PC = sh.PR;
    }

    private static void BupSetDate(SaturnSh2State sh, MemoryMap memory)
    {
        ReadOnlySpan<ushort> months = [31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334];
        byte data = memory.Read8(sh.R[4]);
        uint date = (uint)((data / 4) * 0x5B5);
        uint remainder = (uint)(data % 4);
        if (remainder != 0)
            date += remainder * 0x16D + 1;

        data = memory.Read8(sh.R[4] + 1);
        if (data != 1 && data < 13)
        {
            date += months[data - 2];
            if (date > 2 && remainder == 0)
                date++;
        }

        unchecked
        {
            date += (uint)(memory.Read8(sh.R[4] + 2) - 1);
            date *= 0x5A0;
            date += (uint)(memory.Read8(sh.R[4] + 3) * 0x3C);
            date += memory.Read8(sh.R[4] + 4);
        }
        sh.R[0] = date;
        sh.PC = sh.PR;
    }

    private void HandleScuInterrupt(SaturnSh2State sh, MemoryMap memory, uint vector)
    {
        for (int i = 0; i <= 3; i++)
        {
            sh.R[15] -= 4;
            memory.Write32(sh.R[15], sh.R[i]);
        }
        sh.R[15] -= 4;
        memory.Write32(sh.R[15], memory.Read32(0x06000348));
        for (int i = 4; i <= 7; i++)
        {
            sh.R[15] -= 4;
            memory.Write32(sh.R[15], sh.R[i]);
        }
        sh.R[15] -= 4;
        memory.Write32(sh.R[15], sh.PR);
        sh.R[15] -= 4;
        memory.Write32(sh.R[15], sh.GBR);

        sh.SR.All = _sh2MaskList[vector - 0x40];
        memory.Write32(0x06000348, memory.Read32(0x06000348) | _scuMaskList[vector - 0x40]);
        memory.Write32(0x25FE00A0, memory.Read32(0x06000348) | _scuMaskList[vector - 0x40]);
        sh.PR = 0x00000480;
        sh.PC = memory.Read32(0x06000900 + (vector << 2));
        sh.Cycles += 33;
    }

    private static void HandleScuInterruptReturn(SaturnSh2State sh, MemoryMap memory)
    {
        sh.GBR = memory.Read32(sh.R[15]);
        sh.R[15] += 4;
        sh.PR = memory.Read32(sh.R[15]);
        sh.R[15] += 4;
        for (int i = 7; i >= 4; i--)
        {
            sh.R[i] = memory.Read32(sh.R[15]);
            sh.R[15] += 4;
        }

        sh.SR.All = 0xF0;
        uint oldMask = memory.Read32(sh.R[15]);
        memory.Write32(0x06000348, oldMask);
        memory.Write32(0x25FE00A0, oldMask);
        sh.R[15] += 4;
        for (int i = 3; i >= 0; i--)
        {
            sh.R[i] = memory.Read32(sh.R[15]);
            sh.R[15] += 4;
        }

        sh.PC = memory.Read32(sh.R[15]);
        sh.R[15] += 4;
        sh.SR.All = memory.Read32(sh.R[15]) & 0x000003F3;
        sh.R[15] += 4;
        sh.Cycles += 24;
    }

    public bool HandleFunction(SaturnSh2State sh, MemoryMap memory, bool isSlave)
    {
        byte cartridgeId = cartridge.CartridgeId;
        switch (unchecked(sh.PC - 0x200) >> 2)
        {
            case 0x04: PowerOnMemoryClear(sh); break;
            case 0x1B: ExecuteCdPlayer(sh); break;
            case 0x1D: CheckMpegCard(sh); break;
            case 0x20: ChangeScuInterruptPriority(sh, memory); break;
            case 0x27: CdInit2(sh); break;
            case 0x37: CdInit1(sh); break;
            case 0x40: SetScuInterrupt(sh, memory); break;
            case 0x41: GetScuInterrupt(sh, memory); break;
            case 0x44: SetSh2Interrupt(sh, memory, isSlave); break;
            case 0x45: GetSh2Interrupt(sh, memory); break;
            case 0x48: ChangeSystemClock(sh, memory, smpc); break;
            case 0x4C: GetSemaphore(sh, memory); break;
            case 0x4D: ClearSemaphore(sh, memory); break;
            case 0x50: SetScuInterruptMask(sh, memory, isSlave); break;
            case 0x51: ChangeScuInterruptMask(sh, memory, isSlave); break;
            case 0x56: BupInit(sh, memory, cartridgeId); break;
            case 0x60: break;
            case 0x61: BupSelectPartition(sh); break;
            case 0x62: BupFormat(sh); break;
            case 0x63: BupStatus(sh, memory, cartridgeId); break;
            case 0x64: BupWrite(sh, memory, cartridgeId, flushBackups); break;
            case 0x65: BupRead(sh, memory, cartridgeId); break;
            case 0x66: BupDelete(sh, memory, cartridgeId); break;
            case 0x67: BupDirectory(sh, memory, cartridgeId); break;
            case 0x68: BupVerify(sh, memory, cartridgeId); break;
            case 0x69: BupGetDate(sh, memory); break;
            case 0x6A: BupSetDate(sh, memory); break;
            case 0x6B: break;
            case >= 0x80 and <= 0x8D:
            case >= 0x90 and <= 0x9F:
                HandleScuInterrupt(sh, memory, (sh.PC - 0x300) >> 2);
                break;
            case 0xA0: HandleScuInterruptReturn(sh, memory); break;
            default: return false;
        }

        return true;
    }

    private void FormatBackup(uint device)
    {
        switch (device)
        {
            case 0:
                mainMemory.FormatBackupRam();
                mainMemory.BackupRamWritten = true;
                break;
            case 1:
                cartridge.FormatBackup();
                break;
        }
    }
}
