using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal enum Sh4MmuAccess
{
    InstructionRead = 0,
    DataWrite = 1,
    DataRead = 2
}

internal enum Sh4MmuError
{
    None,
    TlbMiss,
    TlbMultiHit,
    Protected,
    FirstWrite,
    BadAddress
}

internal struct Sh4TlbEntry
{
    internal uint Address;
    internal uint Data;
    internal uint Assistance;

    internal uint VirtualPageNumber
    {
        readonly get => Address >> 10;
        set => Address = (Address & 0x3FF) | (value << 10);
    }

    internal uint PhysicalPageNumber
    {
        readonly get => Data >> 10 & 0x7FFFF;
        set => Data = (Data & ~0x1FFFFC00u) | ((value & 0x7FFFF) << 10);
    }

    internal readonly uint Asid => Address & 0xFF;
    internal readonly int PageSize => (int)((Data >> 4 & 1) | (Data >> 6 & 2));
    internal readonly uint Protection => Data >> 5 & 3;
    internal readonly bool Valid => (Data & 0x100) != 0;
    internal readonly bool Shared => (Data & 2) != 0;
    internal readonly bool Dirty => (Data & 4) != 0;

    internal bool WriteThrough
    {
        readonly get => (Data & 1) != 0;
        set => Data = (Data & ~1u) | (value ? 1u : 0u);
    }

    internal bool Cacheable
    {
        readonly get => (Data & 8) != 0;
        set => Data = (Data & ~8u) | (value ? 8u : 0u);
    }

    internal uint SpaceAttributes
    {
        readonly get => Assistance & 7;
        set => Assistance = (Assistance & ~7u) | (value & 7);
    }

    internal bool TimingControl
    {
        readonly get => (Assistance & 8) != 0;
        set => Assistance = (Assistance & ~8u) | (value ? 8u : 0u);
    }
}

internal readonly record struct Sh4MmuControl(uint Value)
{
    internal int Urc => (int)(Value >> 10 & 0x3F);
}

internal sealed partial class Sh4Mmu(
    Sh4Context context, Sh4AddressSpace memory, Sh4CacheAndTlbController registers, Sh4Debugger debugger)
{
#if SH4_STRICT_MODE
    private static readonly bool FastMmu = false;
#else
    private static readonly bool FastMmu = true;
#endif
    private static readonly bool[] FastRegions = [false, false, false, false, true, true, false, true];
    private static readonly uint[] PageMasks = [0xFFFFFC00, 0xFFFFF000, 0xFFFF0000, 0xFFF00000];

    private readonly Sh4Context _context = context;
    private readonly Sh4AddressSpace _memory = memory;
    private readonly Sh4CacheAndTlbController _registers = registers;
    private readonly Sh4Debugger _debugger = debugger;

    internal bool Enabled => _mmuOn;
    internal Sh4TlbEntry[] UnifiedTlb => _unifiedTlb;
    internal Sh4TlbEntry[] InstructionTlb => _instructionTlb;
    internal Sh4MmuControl Control => new(_registers.MmuControl);
    internal uint PageTableEntryHigh => _registers.PageTableEntryHigh;
    internal uint PageTableEntryLow => _registers.PageTableEntryLow;
    internal uint PageTableEntryAssistance => _registers.PageTableEntryAssistance;
    internal uint TrapRegister { set => _registers.TrapRegister = value; }

    internal bool SynchronizeUnifiedTlb(int index) => SynchronizeUnified(index);

    internal Sh4MmuError Lookup(uint address, out uint physicalAddress) =>
        LookupFull(address, out _, out physicalAddress);

    internal Sh4MmuError TranslateInstruction(uint address, out uint physicalAddress)
    {
#if SH4_STRICT_MODE
        return TranslateInstructionAccurate(address, out physicalAddress);
#else
        if (FastRegions[address >> 29])
        {
            physicalAddress = address;
            return Sh4MmuError.None;
        }

        return Lookup(address, out physicalAddress);
#endif
    }

    internal void RaiseInstructionException(uint address, Sh4MmuError error) =>
        DoException(address, error, Sh4MmuAccess.InstructionRead);

    internal bool IsTranslated(uint address, uint size)
    {
#if SH4_STRICT_MODE
        if ((address & (Math.Min(size, 4u) - 1)) != 0)
            return true;
#endif

        if (FastRegions[address >> 29])
            return false;

        if ((address & 0xFC000000) == 0x7C000000)
            return false;

        return true;
    }

    internal Sh4MmuError TranslateData(uint address, bool write, out uint physicalAddress) =>
        TranslateData(address, write ? Sh4MmuAccess.DataWrite : Sh4MmuAccess.DataRead, out physicalAddress);

    internal void FlushAddressLookupTable(bool full)
    {
        if (full)
        {
            Array.Clear(_addressLookupTable, 0, _addressLookupTable.Length / 2);
        }
        else
        {
            const int slotPages = (32 * 1024 * 1024) >> 12;
            Array.Clear(_addressLookupTable, 0, slotPages);
        }
    }

#if !SH4_DYNAREC_NONE
    internal static unsafe nuint DynarecLookupEntry =>
        (nuint)(delegate* unmanaged<uint, uint, uint, uint>)&NativeDynarecLookup;

    internal static unsafe nuint DynarecLookupX86Entry =>
        (nuint)(delegate* unmanaged[Fastcall]<uint, uint, uint, uint>)&NativeDynarecLookupX86;

    [UnmanagedCallersOnly]
    private static uint NativeDynarecLookup(uint address, uint write, uint pc) =>
        Sh4Context.Active.Mmu.LookupForDynarec(address, write, pc);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static uint NativeDynarecLookupX86(uint address, uint write, uint pc) =>
        Sh4Context.Active.Mmu.LookupForDynarec(address, write, pc);

    private unsafe uint LookupForDynarec(uint address, uint write, uint pc)
    {
        uint physicalAddress;
        Sh4MmuError error;
        if (write != 0)
            error = TranslateData(address, Sh4MmuAccess.DataWrite, out physicalAddress);
        else
            error = TranslateData(address, Sh4MmuAccess.DataRead, out physicalAddress);

        if (error != Sh4MmuError.None)
        {
            _context.ProgramCounter = pc;
            DoException(address, error, write != 0 ? Sh4MmuAccess.DataWrite : Sh4MmuAccess.DataRead);
            Sh4HostContext hostContext = default;
            Sh4NativeBackend.Current!.HandleException(ref hostContext);
            ((delegate* unmanaged<void>)hostContext.ProgramCounter)();
            return 0;
        }

#if !SH4_STRICT_MODE
        if (address >> 31 == 0)
            _addressLookupTable[address >> 12] = physicalAddress & ~0xFFFu;
#endif
        return physicalAddress;
    }
#endif
}
