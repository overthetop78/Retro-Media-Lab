using System.Diagnostics;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal sealed partial class Sh4Mmu
{
    private const int BucketCount = 4096;
    private const int NoRecentEntry = int.MinValue;

    private struct LinkedTlbEntry
    {
        public Sh4TlbEntry Entry;
        public int Next;
    }

    private readonly LinkedTlbEntry[] _fullTable = new LinkedTlbEntry[65536];
    private readonly int[] _entryBuckets = new int[BucketCount];
    private int _fullTableSize;
    private int _recentEntry = NoRecentEntry;
    private uint _recentMask;
    private uint _recentAddress;

    private static int BucketIndex(uint address, int size, uint asid) =>
        (int)(((address >> 20) ^ (address >> 12) ^ (address | asid | ((uint)size << 8))) & (BucketCount - 1));

    private void CacheEntry(in Sh4TlbEntry entry)
    {
        if (entry.PageSize == 0 || _fullTableSize >= _fullTable.Length)
            return;

        int bucket = BucketIndex(entry.VirtualPageNumber << 10, entry.PageSize, entry.Asid);
        _fullTable[_fullTableSize].Entry = entry;
        _fullTable[_fullTableSize].Next = _entryBuckets[bucket];
        _entryBuckets[bucket] = ++_fullTableSize;
    }

    private void FlushCache()
    {
        _fullTableSize = 0;
        Array.Clear(_entryBuckets);
    }

    private int FindEntryByPageSize(uint address, int size)
    {
        int shift = size switch { 1 => 2, 2 => 6, 3 => 10, _ => 0 };
        uint vpn = address >> (10 + shift) << shift;
        uint asid = _registers.PageTableEntryHigh & 0xFF;
        int bucket = BucketIndex(vpn << 10, size, asid);
        for (int index = _entryBuckets[bucket]; index != 0; index = _fullTable[index - 1].Next)
        {
            ref Sh4TlbEntry entry = ref _fullTable[index - 1].Entry;
            if (entry.VirtualPageNumber == vpn && entry.PageSize == size && (entry.Shared || entry.Asid == asid))
                return index - 1;
        }

        return -1;
    }

    private int FindEntry(uint address)
    {
        int index = FindEntryByPageSize(address, 1);
        if (index >= 0)
            return index;
        index = FindEntryByPageSize(address, 2);
        return index >= 0 ? index : FindEntryByPageSize(address, 3);
    }

    private bool SynchronizeUnifiedFast(int index)
    {
        ref Sh4TlbEntry entry = ref _unifiedTlb[index];
        uint mask = PageMasks[entry.PageSize];
        entry.VirtualPageNumber &= mask >> 10;
        entry.PhysicalPageNumber &= mask >> 10;

        _recentEntry = ~index;
        _recentMask = mask;
        _recentAddress = entry.VirtualPageNumber << 10;

        CacheEntry(entry);
        if (!Enabled)
        {
            if ((entry.VirtualPageNumber & (0xFC000000 >> 10)) == (0xE0000000 >> 10))
            {
                uint queueIndex = (entry.VirtualPageNumber & 0x7FFFF) >> 10 & 0x3F;
                _storeQueueRemap[queueIndex] = entry.PhysicalPageNumber << 10;
            }
            else if ((_registers.MmuControl & 1) != 0
                && entry.VirtualPageNumber != 0x30040
                && entry.VirtualPageNumber != 0x30000
                && entry.Valid)
            {
                _mmuOn = true;
                Trace.TraceInformation("Enabling on-demand Full MMU support");
                SetState();
            }
        }

        return true;
    }

    private static void SynchronizeInstructionFast(int index)
    {
    }

    private Sh4MmuError LookupFast(uint address, out Sh4TlbEntry entry, out uint physicalAddress)
    {
        if (_recentEntry != NoRecentEntry)
        {
            ref Sh4TlbEntry recent = ref (_recentEntry < 0
                ? ref _unifiedTlb[~_recentEntry]
                : ref _fullTable[_recentEntry].Entry);
            uint asid = _registers.PageTableEntryHigh & 0xFF;
            if (_recentAddress == (address & _recentMask) && (recent.Asid == asid || recent.Shared))
            {
                entry = recent;
                physicalAddress = (recent.PhysicalPageNumber << 10) | (address & ~_recentMask);
                return Sh4MmuError.None;
            }
        }

        int index = FindEntry(address);
        if (index >= 0)
        {
            entry = _fullTable[index].Entry;
            uint mask = PageMasks[entry.PageSize];
            physicalAddress = (entry.PhysicalPageNumber << 10) | (address & ~mask);
            _recentEntry = index;
            _recentMask = mask;
            _recentAddress = entry.VirtualPageNumber << 10;
            return Sh4MmuError.None;
        }

        int replacement = (int)(_registers.MmuControl >> 10 & 0x3F);
        ref Sh4TlbEntry resolved = ref _unifiedTlb[replacement];
        if (ResolveWindowsCeAddress(address, ref resolved))
        {
            _registers.PageTableEntryLow = resolved.Data;
            _registers.PageTableEntryAssistance = resolved.Assistance;
            _registers.PageTableEntryHigh = resolved.Address;
            uint mask = PageMasks[resolved.PageSize];
            _recentEntry = ~replacement;
            _recentMask = mask;
            _recentAddress = address & mask;
            resolved.PhysicalPageNumber &= mask >> 10;
            entry = resolved;
            physicalAddress = (resolved.PhysicalPageNumber << 10) | (address & ~mask);

            CacheEntry(resolved);
            _context.CycleCounter -= 164;
            return Sh4MmuError.None;
        }

        entry = default;
        physicalAddress = 0;
        return Sh4MmuError.TlbMiss;
    }

    private Sh4MmuError TranslateStoreQueueFast(uint address, out uint physicalAddress)
    {
        Sh4MmuError lookup = LookupFast(address, out _, out physicalAddress);
        if (lookup == Sh4MmuError.None)
            physicalAddress &= ~31u;
        return lookup;
    }

    private Sh4MmuError TranslateDataFast(uint address, out uint physicalAddress)
    {
        if (FastRegions[address >> 29] || (address & 0xFC000000) == 0x7C000000)
        {
            physicalAddress = address;
            return Sh4MmuError.None;
        }

        Sh4MmuError lookup = LookupFast(address, out _, out physicalAddress);
        if (lookup == Sh4MmuError.None && (physicalAddress & 0x1C000000) == 0x1C000000)
            physicalAddress |= 0xF0000000;
#if TRACE_WINCE_SYSCALLS
        if (lookup == Sh4MmuError.None)
            TraceResolvedWindowsCeString(address);
#endif
        return lookup;
    }

    private void FlushTableFast()
    {
        _recentEntry = NoRecentEntry;
        FlushCache();
        FlushAddressLookupTable(full: true);
    }
}
