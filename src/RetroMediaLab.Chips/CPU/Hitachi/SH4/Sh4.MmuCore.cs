using System.Diagnostics;
using System.IO;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal sealed partial class Sh4Mmu
{
    private static readonly uint[] InstructionLruOr = [0x00, 0x20, 0x14, 0x0B];
    private static readonly uint[] InstructionLruAnd = [0x07, 0x39, 0x3E, 0x3F];

    private readonly Sh4TlbEntry[] _unifiedTlb = new Sh4TlbEntry[64];
    private readonly Sh4TlbEntry[] _instructionTlb = new Sh4TlbEntry[4];
    private readonly int[] _instructionLruReplacement = new int[64];
    private readonly uint[] _storeQueueRemap = new uint[64];
    private readonly uint[] _addressLookupTable = GC.AllocateArray<uint>(0x100000, pinned: true);
    private bool _mmuOn;

#if !SH4_DYNAREC_NONE
    internal unsafe nuint AddressLookupTablePointer
    {
        get
        {
            fixed (uint* pointer = _addressLookupTable)
                return (nuint)pointer;
        }
    }
#endif

    internal bool SynchronizeUnified(int index)
    {
        if (FastMmu)
            return SynchronizeUnifiedFast(index);

        ref readonly Sh4TlbEntry entry = ref _unifiedTlb[index];
        Debug.WriteLine($"UTLB MEM remap {index} : 0x{entry.VirtualPageNumber << 10:X} to 0x{entry.PhysicalPageNumber << 10:X} : {(entry.Valid ? 1 : 0)} asid {entry.Asid} size {entry.PageSize}");
        if (!entry.Valid)
            return true;
        if ((entry.VirtualPageNumber & (0xFC000000 >> 10)) != (0xE0000000 >> 10))
            return false;
        _storeQueueRemap[(entry.VirtualPageNumber & 0x7FFFF) >> 10 & 0x3F] = entry.PhysicalPageNumber << 10;
        return true;
    }

    internal void SynchronizeInstruction(int index)
    {
        if (FastMmu)
        {
            SynchronizeInstructionFast(index);
            return;
        }

        ref readonly Sh4TlbEntry entry = ref _instructionTlb[index];
        Debug.WriteLine($"ITLB MEM remap {index} : 0x{entry.VirtualPageNumber << 10:X} to 0x{entry.PhysicalPageNumber << 10:X} : {(entry.Valid ? 1 : 0)}");
    }

    private void MmuException(Sh4MmuError error, uint address, Sh4MmuAccess access, Action<Sh4ExceptionEvent> raise)
    {
        Debug.WriteLine($"MMU exception -> pc = 0x{_context.ProgramCounter:X} : ");
        _registers.TranslationExceptionAddress = address;
        _registers.PageTableEntryHigh = (address & 0xFFFFFC00) | (_registers.PageTableEntryHigh & 0x3FF);
        switch (error)
        {
            case Sh4MmuError.None:
                Sh4HostRuntime.Die("Error: mmu_error == MmuError::NONE)");
                return;
            case Sh4MmuError.TlbMiss:
                Debug.WriteLine($"MmuError::UTLB_MISS 0x{address:X}, handled");
                raise(access == Sh4MmuAccess.DataWrite ? Sh4ExceptionEvent.TlbMissWrite : Sh4ExceptionEvent.TlbMissRead);
                return;
            case Sh4MmuError.TlbMultiHit:
                Trace.TraceError($"MmuError::TLB_MHIT @ 0x{address:X}");
                raise(Sh4ExceptionEvent.TlbMultiHit);
                break;
            case Sh4MmuError.Protected:
                Debug.WriteLine($"MmuError::PROTECTED 0x{address:X}, handled");
                raise(access == Sh4MmuAccess.DataWrite ? Sh4ExceptionEvent.TlbProtectionWrite : Sh4ExceptionEvent.TlbProtectionRead);
                return;
            case Sh4MmuError.FirstWrite:
                Debug.WriteLine("MmuError::FIRSTWRITE");
                Sh4HostRuntime.Verify(access == Sh4MmuAccess.DataWrite);
                raise(Sh4ExceptionEvent.TlbInitialPageWrite);
                return;
            case Sh4MmuError.BadAddress:
                if (access == Sh4MmuAccess.DataWrite)
                {
                    Debug.WriteLine($"MmuError::BADADDR(dw) 0x{address:X}");
                    raise(Sh4ExceptionEvent.AddressErrorWrite);
                }
                else if (access == Sh4MmuAccess.DataRead)
                {
                    Debug.WriteLine($"MmuError::BADADDR(dr) 0x{address:X}");
                    raise(Sh4ExceptionEvent.AddressErrorRead);
                }
                else
                {
#if TRACE_WINCE_SYSCALLS
                    if (!TraceWindowsCeSystemCall(address))
#endif
                        Debug.WriteLine($"MmuError::BADADDR(i) 0x{address:X}");
                    raise(Sh4ExceptionEvent.AddressErrorRead);
                }
                return;
            default:
                Sh4HostRuntime.Die("Unknown mmu_error");
                break;
        }
    }

    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    internal void RaiseException(uint address, Sh4MmuError error, Sh4MmuAccess access)
    {
        MmuException(error, address, access, exception =>
        {
            _debugger.Trap(exception);
            throw new Sh4InstructionException(_context.ProgramCounter - 2, exception);
        });
        Sh4HostRuntime.Die("Unknown mmu_error");
    }

    private void DoException(uint address, Sh4MmuError error, Sh4MmuAccess access) =>
        MmuException(error, address, access, exception => _context.RaiseException(_context.ProgramCounter, exception));

    private bool KernelMode => _context.Status.MD;

    internal bool Match(uint address, in Sh4TlbEntry entry)
    {
        if (!entry.Valid)
            return false;
        uint mask = PageMasks[entry.PageSize];
        if (((entry.VirtualPageNumber << 10) & mask) == (address & mask))
        {
            bool needAsid = !entry.Shared && (!KernelMode || (_registers.MmuControl & 0x100) == 0);
            if (!needAsid || entry.Asid == (_registers.PageTableEntryHigh & 0xFF))
                return true;
        }

        return false;
    }

    internal Sh4MmuError LookupFull(uint address, out Sh4TlbEntry entry, out uint physicalAddress)
    {
        if (FastMmu)
            return LookupFast(address, out entry, out physicalAddress);

        uint control = _registers.MmuControl;
        uint counter = ((control >> 10) + 1) & 0x3F;
        if ((control >> 18 & 0x3F) == counter)
            counter = 0;
        _registers.MmuControl = (control & ~(0x3Fu << 10)) | (counter << 10);

        bool found = false;
        entry = default;
        physicalAddress = 0;
        foreach (ref readonly Sh4TlbEntry candidate in _unifiedTlb.AsSpan())
        {
            if (!Match(address, candidate))
                continue;
            if (found)
                return Sh4MmuError.TlbMultiHit;
            found = true;
            entry = candidate;
            uint mask = PageMasks[entry.PageSize];
            physicalAddress = ((entry.PhysicalPageNumber << 10) & mask) | (address & ~mask);
        }

        return found ? Sh4MmuError.None : Sh4MmuError.TlbMiss;
    }

    private uint TranslateQueueArea(uint address)
    {
        uint queueAddress = (address & 32) != 0 ? _registers.QueueAddress1 : _registers.QueueAddress0;
        return ((queueAddress & 0x1C) << 24) | (address & 0x03FFFFE0);
    }

    internal Sh4MmuError TranslateStoreQueue(uint address, Sh4MmuAccess access, out uint physicalAddress)
    {
        if (FastMmu)
            return TranslateStoreQueueFast(address, out physicalAddress);

        physicalAddress = 0;
        uint control = _registers.MmuControl;
        if ((address & 3) != 0 || ((control & 0x200) != 0 && !KernelMode))
            return Sh4MmuError.BadAddress;
        if ((control & 1) != 0)
        {
            Sh4MmuError error = LookupFull(address, out Sh4TlbEntry entry, out physicalAddress);
            physicalAddress &= ~31u;
            if (error != Sh4MmuError.None)
                return error;

            uint mode = entry.Protection >> 1;
            if (mode == 0 && !KernelMode)
                return Sh4MmuError.Protected;

            if (access == Sh4MmuAccess.DataWrite)
            {
                if ((entry.Protection & 1) == 0)
                    return Sh4MmuError.Protected;
                else if (!entry.Dirty)
                    return Sh4MmuError.FirstWrite;
            }
        }
        else
        {
            physicalAddress = TranslateQueueArea(address);
        }
        return Sh4MmuError.None;
    }

    private Sh4MmuError TranslateData(uint address, Sh4MmuAccess access, out uint physicalAddress)
    {
        if (FastMmu)
            return TranslateDataFast(address, out physicalAddress);

        physicalAddress = address;
        if (access == Sh4MmuAccess.DataWrite && (address & 0xFC000000) == 0xE0000000)
        {
            Sh4MmuError storeQueueError = TranslateStoreQueue(address, access, out physicalAddress);
            if (storeQueueError != Sh4MmuError.None)
                return storeQueueError;
            physicalAddress = address;
            return Sh4MmuError.None;
        }
        if (!KernelMode && (address & 0x80000000) != 0)
            return Sh4MmuError.BadAddress;
        if ((address & 0xFC000000) == 0x7C000000)
            return Sh4MmuError.None;

        if (FastRegions[address >> 29])
            return Sh4MmuError.None;

        Sh4MmuError error = LookupFull(address, out Sh4TlbEntry entry, out physicalAddress);
        if (error != Sh4MmuError.None)
            return error;
#if TRACE_WINCE_SYSCALLS
        TraceResolvedWindowsCeString(address);
#endif
        uint mode = entry.Protection >> 1;
        if (mode == 0 && !KernelMode)
            return Sh4MmuError.Protected;

        if (access == Sh4MmuAccess.DataWrite)
        {
            if ((entry.Protection & 1) == 0)
                return Sh4MmuError.Protected;
            else if (!entry.Dirty)
                return Sh4MmuError.FirstWrite;
        }
        if ((physicalAddress & 0x1C000000) == 0x1C000000)
            physicalAddress |= 0xF0000000;
        return Sh4MmuError.None;
    }

    private Sh4MmuError TranslateInstructionAccurate(uint address, out uint physicalAddress)
    {
        physicalAddress = address;
        if (!KernelMode && (address & 0x80000000) != 0)
            return Sh4MmuError.BadAddress;
        if (address >> 29 == 7)
            return Sh4MmuError.BadAddress;

        if (FastRegions[address >> 29])
            return Sh4MmuError.None;
        Sh4MmuError error = LookupInstruction(address, out Sh4TlbEntry entry, out physicalAddress);
        if (error != Sh4MmuError.None)
            return error;

        uint mode = entry.Protection >> 1;
        if (mode == 0 && !KernelMode)
            return Sh4MmuError.Protected;

        return Sh4MmuError.None;
    }

    internal Sh4MmuError LookupInstruction(uint address, out Sh4TlbEntry entry, out uint physicalAddress)
    {
        entry = default;
        physicalAddress = 0;
        bool retried = false;
        while (true)
        {
            int match = -1;
            for (int index = 0; index < _instructionTlb.Length; index++)
            {
                ref readonly Sh4TlbEntry candidate = ref _instructionTlb[index];
                if (!candidate.Valid)
                    continue;
                uint mask = PageMasks[candidate.PageSize];
                if (((candidate.VirtualPageNumber << 10) & mask) == (address & mask))
                {
                    bool needAsid = !candidate.Shared && (!KernelMode || (_registers.MmuControl & 0x100) == 0);
                    if (!needAsid || candidate.Asid == (_registers.PageTableEntryHigh & 0xFF))
                    {
                        if (match >= 0)
                            return Sh4MmuError.TlbMultiHit;
                        match = index;
                        entry = candidate;
                        physicalAddress = ((entry.PhysicalPageNumber << 10) & mask) | (address & ~mask);
                    }
                }
            }

            uint control = _registers.MmuControl;
            uint lru = control >> 26;
            if (match >= 0)
            {
                lru &= InstructionLruAnd[match];
                lru |= InstructionLruOr[match];
                _registers.MmuControl = (control & 0x03FFFFFF) | (lru << 26);
                return Sh4MmuError.None;
            }

#if SH4_STRICT_MODE
            Sh4HostRuntime.Verify(!retried);
#else
            if (retried)
                return Sh4MmuError.TlbMiss;
#endif
            Sh4MmuError error = LookupFull(address, out Sh4TlbEntry replacement, out physicalAddress);
            if (error != Sh4MmuError.None)
                return error;
            int replacementIndex = _instructionLruReplacement[lru];
            Sh4HostRuntime.Verify(replacementIndex != -1);
            _instructionTlb[replacementIndex] = replacement;
            SynchronizeInstruction(replacementIndex);
            retried = true;
        }
    }

    internal unsafe void SetState()
    {
        if ((_registers.MmuControl & 1) != 0)
        {
            ReadOnlySpan<byte> magic = "S\0H\0-\04\0 \0K\0e\0r\0n\0e\0l\0"u8;
            if (new ReadOnlySpan<byte>(_memory.GetMemoryPointer(0x8C0110A8, 4), magic.Length).SequenceEqual(magic)
                || new ReadOnlySpan<byte>(_memory.GetMemoryPointer(0x8C011118, 4), magic.Length).SequenceEqual(magic))
            {
                _mmuOn = true;
                Trace.TraceInformation("Enabling Full MMU support");
            }
        }
        else
        {
            _mmuOn = false;
        }

        _memory.SetMemoryHandlers();
        _context.SetStoreQueueWriteHandler();
    }

    internal void Initialize()
    {
        Array.Fill(_instructionLruReplacement, -1);
        for (int entry = 0; entry < _instructionTlb.Length; entry++)
        {
            uint key = ~InstructionLruAnd[entry] & 0x3F;
            uint mask = key | InstructionLruOr[entry];
            for (int index = 0; index < _instructionLruReplacement.Length; index++)
            {
                if (((uint)index & mask) == key)
                {
                    Sh4HostRuntime.Verify(_instructionLruReplacement[index] == -1);
                    _instructionLruReplacement[index] = entry;
                }
            }
        }

        SetState();
        if (FastMmu)
        {
            for (int page = _addressLookupTable.Length / 2; page < _addressLookupTable.Length; page++)
                _addressLookupTable[page] = (uint)page << 12;
        }
    }

    internal void Reset()
    {
        Array.Clear(_unifiedTlb);
        Array.Clear(_instructionTlb);
        SetState();
        FlushTable();
        Array.Clear(_storeQueueRemap);
    }

    internal static void Terminate()
    {
    }

    internal void FlushTable()
    {
        if (FastMmu)
        {
            FlushTableFast();
            return;
        }
        foreach (ref Sh4TlbEntry entry in _instructionTlb.AsSpan())
            entry.Data &= ~0x100u;
        foreach (ref Sh4TlbEntry entry in _unifiedTlb.AsSpan())
            entry.Data &= ~0x100u;
    }

    internal T ReadMemory<T>(uint address) where T : unmanaged
    {
        if ((address & (uint)(Math.Min(System.Runtime.CompilerServices.Unsafe.SizeOf<T>(), 4) - 1)) != 0)
            RaiseException(address, Sh4MmuError.BadAddress, Sh4MmuAccess.DataRead);
        Sh4MmuError error = TranslateData(address, Sh4MmuAccess.DataRead, out uint physicalAddress);
        if (error != Sh4MmuError.None)
            RaiseException(address, error, Sh4MmuAccess.DataRead);
        return _memory.Read<T>(physicalAddress);
    }

    internal ushort ReadInstruction16(uint address)
    {
        if ((address & 1) != 0)
            RaiseException(address, Sh4MmuError.BadAddress, Sh4MmuAccess.InstructionRead);
        Sh4MmuError error = TranslateInstruction(address, out uint physicalAddress);
        if (error != Sh4MmuError.None)
            RaiseException(address, error, Sh4MmuAccess.InstructionRead);
        return _memory.Read16(physicalAddress);
    }

    internal void WriteMemory<T>(uint address, T value) where T : unmanaged
    {
        if ((address & (uint)(Math.Min(System.Runtime.CompilerServices.Unsafe.SizeOf<T>(), 4) - 1)) != 0)
            RaiseException(address, Sh4MmuError.BadAddress, Sh4MmuAccess.DataWrite);
        Sh4MmuError error = TranslateData(address, Sh4MmuAccess.DataWrite, out uint physicalAddress);
        if (error != Sh4MmuError.None)
            RaiseException(address, error, Sh4MmuAccess.DataWrite);
        _memory.Write(physicalAddress, value);
    }

    internal uint TranslateStoreQueueWriteBack(uint address)
    {
        if (!_mmuOn)
            return _storeQueueRemap[address >> 20 & 0x3F] | (address & 0xFFFE0);
        Sh4MmuError error = TranslateStoreQueue(address, Sh4MmuAccess.DataRead, out uint physicalAddress);
        if (error != Sh4MmuError.None)
            RaiseException(address, error, Sh4MmuAccess.DataRead);
        return physicalAddress;
    }

    internal void Serialize(BinaryWriter writer)
    {
        WriteEntries(writer, _unifiedTlb);
        WriteEntries(writer, _instructionTlb);
        foreach (uint mapping in _storeQueueRemap)
            writer.Write(mapping);
    }

    internal void Deserialize(BinaryReader reader, int version)
    {
        const int saveVersion23 = 818;
        const int saveVersion33 = 828;

        if (version < saveVersion33)
            reader.ReadUInt64();
        ReadEntries(reader, _unifiedTlb);
        ReadEntries(reader, _instructionTlb);
        for (int index = 0; index < _storeQueueRemap.Length; index++)
            _storeQueueRemap[index] = reader.ReadUInt32();
        if (version < saveVersion23)
        {
            for (int index = 0; index < 64; index++)
                reader.ReadUInt32();
        }
    }

    private static void WriteEntries(BinaryWriter writer, ReadOnlySpan<Sh4TlbEntry> entries)
    {
        foreach (ref readonly Sh4TlbEntry entry in entries)
        {
            writer.Write(entry.Address);
            writer.Write(entry.Data);
            writer.Write(entry.Assistance);
        }
    }

    private static void ReadEntries(BinaryReader reader, Span<Sh4TlbEntry> entries)
    {
        foreach (ref Sh4TlbEntry entry in entries)
        {
            entry.Address = reader.ReadUInt32();
            entry.Data = reader.ReadUInt32();
            entry.Assistance = reader.ReadUInt32();
        }
    }
}
