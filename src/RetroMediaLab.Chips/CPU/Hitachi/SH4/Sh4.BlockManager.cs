#if !SH4_DYNAREC_NONE
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal sealed partial class Sh4BlockManager(Sh4RecompilerDriver driver, Sh4AddressSpace addressSpace)
{
    private const uint FpcbMask = (32 * 1024 * 1024 / 2) - 1;
    private const uint MainRamBase = 0x0C000000;
    private const uint MainRamWindowEnd = 0x20000000;

    private static readonly IComparer<Sh4RuntimeBlockInfo> BlockPointerOrder =
        Comparer<Sh4RuntimeBlockInfo>.Create((left, right) => GetBlockAddress(left).CompareTo(GetBlockAddress(right)));
    private static readonly SortedSet<Sh4RuntimeBlockInfo> _temporaryBlocks = new(BlockPointerOrder);
    private static readonly List<Sh4RuntimeBlockInfo> _deletedBlocks = [];
    private static uint _pageCount;
    private static bool[] _unprotectedPages = [];
    private static SortedSet<Sh4RuntimeBlockInfo>[] _blocksPerPage = [];
    private static readonly SortedList<nuint, Sh4RuntimeBlockInfo> _blocks = [];
    private static bool _printStats = true;
#if DYNA_OPROF
    private static nint _oprofileHandle;

    [DllImport("libopagent.so", EntryPoint = "op_open_agent", CallingConvention = CallingConvention.Cdecl)]
    private static extern nint OpenOProfileAgent();

    [DllImport("libopagent.so", EntryPoint = "op_close_agent", CallingConvention = CallingConvention.Cdecl)]
    private static extern int CloseOProfileAgent(nint handle);

    [DllImport("libopagent.so", EntryPoint = "op_write_native_code", CallingConvention = CallingConvention.Cdecl)]
    private static extern int WriteOProfileNativeCode(nint handle, [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
        ulong address, nuint code, uint size);

    [DllImport("libopagent.so", EntryPoint = "op_unload_native_code", CallingConvention = CallingConvention.Cdecl)]
    private static extern int UnloadOProfileNativeCode(nint handle, ulong address);
#endif

    public static uint ProtectedBlocks { get; private set; }
    public static uint UnprotectedBlocks { get; private set; }
    private uint PageSize => addressSpace.PageSize;

    private nuint GetCode(uint address) => addressSpace.ReadFpcb((address >> 1) & FpcbMask);

    private void SetCode(uint address, nuint code) => addressSpace.WriteFpcb((address >> 1) & FpcbMask, code);

    internal static unsafe nuint GetCodeByVirtualAddressEntry =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<uint, nuint>)&NativeGetCodeByVirtualAddressX86
            : (nuint)(delegate* unmanaged<uint, nuint>)&NativeGetCodeByVirtualAddress;

    [UnmanagedCallersOnly]
    private static nuint NativeGetCodeByVirtualAddress(uint address) =>
        Sh4RecompilerDriver.Current.BlockManager.GetCodeByVirtualAddress(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static nuint NativeGetCodeByVirtualAddressX86(uint address) =>
        Sh4RecompilerDriver.Current.BlockManager.GetCodeByVirtualAddress(address);

    public unsafe nuint GetCodeByVirtualAddress(uint address)
    {
        if (!driver.Mmu.Enabled)
            return GetCode(address);

        if ((address & 1) != 0)
        {
            var context = driver.Context;
            switch (address)
            {
#if !SH4_STRICT_MODE
                case 0xFFFFFDE7:
                    context.Registers[0] = (uint)(driver.Scheduler.Now64() * 1000 / Sh4RecompilerDriver.MainClock);
                    context.ProgramCounter = context.ProcedureRegister;
                    context.CycleCounter -= 100;
                    break;
                case 0xFFFFFD05:
                    if (driver.TryWriteMemoryImmediate(context.Registers[4], sizeof(ulong), out nuint pointer,
                            out bool isRam, out _) && isRam)
                    {
                        *(ulong*)pointer = driver.Scheduler.Now64() >> 4;
                        context.Registers[0] = 1;
                        context.ProgramCounter = context.ProcedureRegister;
                        context.CycleCounter -= 100;
                    }
                    else
                    {
                        driver.RaiseReadAddressError(address);
                    }
                    break;
                case 0xFFFFFDED:
                    if (IsDricasAuthenticationFunction(context.Registers[5]))
                    {
                        Trace.TraceInformation("GetProcAddressW('UA_QueryD') intercepted");
                        context.Registers[0] = 0xFFFF0001;
                        context.ProgramCounter = context.ProcedureRegister;
                    }
                    else
                    {
                        driver.RaiseReadAddressError(address);
                    }
                    break;
                case 0xFFFF0001:
                    Debug.WriteLine("UA_QueryD called");
                    context.Registers[0] = 0;
                    context.ProgramCounter = context.ProcedureRegister;
                    break;
#endif
                default:
                    driver.RaiseReadAddressError(address);
                    break;
            }
            address = context.ProgramCounter;
        }

        var error = driver.Mmu.TranslateInstruction(address, out uint physicalAddress);
        if (error != Sh4MmuError.None)
        {
            driver.Mmu.RaiseInstructionException(address, error);
            driver.Mmu.TranslateInstruction(driver.Context.ProgramCounter, out physicalAddress);
        }
        return GetCode(physicalAddress);
    }

#if !SH4_STRICT_MODE
    private bool IsDricasAuthenticationFunction(uint virtualAddress)
    {
        if ((virtualAddress & 0xFFFF0000) == 0)
            return false;

        uint physicalAddress = 0;
        foreach (char expected in "UA_QueryD\0")
        {
            if (physicalAddress == 0 && driver.Mmu.Lookup(virtualAddress, out physicalAddress) != Sh4MmuError.None)
                return false;
            ushort actual = addressSpace.ReadMemory16WithoutMmu(physicalAddress);
            if (actual != expected)
                return false;
            if (actual == 0)
                return true;
            virtualAddress += 2;
            physicalAddress = (virtualAddress & 0xFFF) == 0 ? 0 : physicalAddress + 2;
        }
        return false;
    }
#endif

    public Sh4RuntimeBlockInfo? GetBlock(uint address)
    {
        nuint code = GetCode(address);
        return code == driver.FailedToFindBlock ? null : GetBlock(code);
    }

    public Sh4RuntimeBlockInfo? GetBlock(nuint executableCode)
    {
        if (_blocks.Count == 0)
            return null;

        nuint writableCode = driver.CodeBuffer.ToWritable(executableCode);
        int start = 0;
        int end = _blocks.Count;
        while (start < end)
        {
            int middle = start + ((end - start) >> 1);
            if (_blocks.Keys[middle] <= writableCode)
                start = middle + 1;
            else
                end = middle;
        }
        if (start == 0)
            return null;

        var block = _blocks.Values[start - 1];
        if (!block.ContainsCode(writableCode))
            return null;

        block.RetainReference();
        return block;
    }

    private void CleanupDeletedBlocks()
    {
        foreach (var block in _deletedBlocks)
            block.ReleaseReference();
        _deletedBlocks.Clear();
    }

    public Sh4RuntimeBlockInfo? GetStaleBlock(nuint executableCode)
    {
        nuint writableCode = driver.CodeBuffer.ToWritable(executableCode);
        if (_deletedBlocks.Count == 0)
            return null;

        int index = _deletedBlocks.Count;
        do
        {
            var block = _deletedBlocks[--index];
            if (block.ContainsCode(writableCode))
            {
                block.RetainReference();
                return block;
            }
        } while (index != 0);

        return null;
    }

    public void AddBlock(Sh4RuntimeBlockInfo block)
    {
        block.RetainReference();
        try
        {
            if (block.Temporary && _temporaryBlocks.Add(block))
                block.RetainReference();
            if (_blocks.TryGetValue(block.Code, out Sh4RuntimeBlockInfo? existing))
            {
                Trace.TraceError($"DUP: {existing.Address:X8} 0x{existing.Code:x} {block.Address:X8} 0x{block.Code:x}");
                Sh4HostRuntime.Die("Duplicated block");
            }
            block.RetainReference();
            _blocks[block.Code] = block;
            existing?.ReleaseReference();
            Sh4HostRuntime.Verify(GetCode(block.Address) == driver.FailedToFindBlock);
            SetCode(block.Address, driver.CodeBuffer.ToExecutable(block.Code));
#if DYNA_OPROF
            if (_oprofileHandle != 0)
            {
                string name = $"sh4:{block.Address:X8},c:{block.GuestCycles},s:{block.GuestOpcodes},h:{block.HostOpcodes}";
                if (WriteOProfileNativeCode(_oprofileHandle, name, (ulong)block.Code, block.Code, block.HostCodeSize) != 0)
                    Trace.TraceInformation("op_write_native_code error");
            }
#endif
        }
        finally
        {
            block.ReleaseReference();
        }
    }

    public void DiscardBlock(Sh4RuntimeBlockInfo block)
    {
        bool found = _blocks.TryGetValue(block.Code, out Sh4RuntimeBlockInfo? entry);
        Sh4HostRuntime.Verify(found, "it != blkmap.end()");
        var registered = entry!;
        registered.RetainReference();
        _blocks.Remove(block.Code);
        try
        {
            registered.ReleaseReference();
            registered.Next = null;
            registered.Branch = null;
            registered.Relink();
            Sh4HostRuntime.Verify(GetCode(registered.Address) == driver.CodeBuffer.ToExecutable(registered.Code));
            SetCode(registered.Address, driver.FailedToFindBlock);

            if (registered.Temporary && _temporaryBlocks.Remove(registered))
                registered.ReleaseReference();
            _deletedBlocks.Add(registered);
            registered.RetainReference();
            registered.Discard();
        }
        finally
        {
            registered.ReleaseReference();
        }
    }

    public void PeriodicalOneSecond() => CleanupDeletedBlocks();

    public void FillVirtualMemoryPage(Span<nuint> entries) => entries.Fill(driver.FailedToFindBlock);

    public void Reset()
    {
        CleanupDeletedBlocks();
        ProtectedBlocks = 0;
        UnprotectedBlocks = 0;

#if !SH4_SWITCH
        if (addressSpace.VirtualMemoryEnabled)
        {
            nuint ram = addressSpace.RamBase + MainRamBase;
            uint size = addressSpace.RamSize;
            addressSpace.UnlockRegion(ram, size);
            if (size == 16 * 1024 * 1024)
            {
                addressSpace.UnlockRegion(ram + 0x01000000, size);
                addressSpace.UnlockRegion(ram + 0x02000000, size);
                addressSpace.UnlockRegion(ram + 0x03000000, size);
            }
            else
            {
                addressSpace.UnlockRegion(ram + 0x02000000, size);
            }
            return;
        }
#endif
        addressSpace.UnlockRegion(addressSpace.RamBuffer, addressSpace.RamSize);
    }

    public void LockPage(uint address, uint size)
    {
        address &= addressSpace.RamMask - addressSpace.PageMask;
        nuint ram = addressSpace.VirtualMemoryEnabled ? addressSpace.RamBase + MainRamBase : addressSpace.RamBuffer;
        addressSpace.LockRegion(ram + address, size);
    }

    public void UnlockPage(uint address, uint size)
    {
        address &= addressSpace.RamMask - addressSpace.PageMask;
        nuint ram = addressSpace.VirtualMemoryEnabled ? addressSpace.RamBase + MainRamBase : addressSpace.RamBuffer;
        addressSpace.UnlockRegion(ram + address, size);
    }

    public void ResetCache()
    {
        driver.ResetNativeBackend();
        addressSpace.ResetFpcb();

        foreach (var block in _blocks.Values)
        {
            block.RetainReference();
            try
            {
                block.RelinkData = 0;
                block.Next = null;
                block.Branch = null;
                block.Relink();
                block.Discard();
                _deletedBlocks.Add(block);
                block.RetainReference();
            }
            finally
            {
                block.ReleaseReference();
            }
        }

        foreach (var block in _blocks.Values)
            block.ReleaseReference();
        _blocks.Clear();
        foreach (var block in _temporaryBlocks)
            block.ReleaseReference();
        _temporaryBlocks.Clear();
        for (uint page = 0; page < _pageCount; page++)
            _blocksPerPage[page].Clear();
        Array.Clear(_unprotectedPages);
#if DYNA_OPROF
        if (_oprofileHandle != 0)
        {
            foreach (var block in _deletedBlocks)
            {
                if (UnloadOProfileNativeCode(_oprofileHandle, (ulong)block.Code) != 0)
                    Trace.TraceInformation("op_unload_native_code error");
            }
        }
#endif
    }

    public void ResetTemporaryCache(bool full)
    {
        if (!full)
        {
            foreach (var block in _temporaryBlocks)
            {
                SetCode(block.Address, driver.FailedToFindBlock);
                if (_blocks.Remove(block.Code))
                    block.ReleaseReference();
            }
        }
        _deletedBlocks.InsertRange(0, _temporaryBlocks);
        foreach (var block in _temporaryBlocks)
            block.RetainReference();
        foreach (var block in _temporaryBlocks)
            block.ReleaseReference();
        _temporaryBlocks.Clear();
    }

    public void Initialize()
    {
        _pageCount = addressSpace.MaximumRamSize / PageSize;
        _unprotectedPages = GC.AllocateUninitializedArray<bool>(checked((int)_pageCount));
        _blocksPerPage = new SortedSet<Sh4RuntimeBlockInfo>[_pageCount];
        for (uint page = 0; page < _pageCount; page++)
            _blocksPerPage[page] = new(BlockPointerOrder);
#if DYNA_OPROF
        _oprofileHandle = OpenOProfileAgent();
        if (_oprofileHandle == 0)
            Trace.TraceInformation("bm: Failed to open oprofile");
        else
            Trace.TraceInformation("bm: Oprofile integration enabled !");
#endif
    }

    public void Terminate()
    {
#if DYNA_OPROF
        try
        {
            if (_oprofileHandle != 0)
                CloseOProfileAgent(_oprofileHandle);
        }
        finally
        {
            _oprofileHandle = 0;
            TerminateBlockManager();
        }
#else
        TerminateBlockManager();
#endif
    }

    private void TerminateBlockManager()
    {
        Reset();
        _unprotectedPages = [];
        _blocksPerPage = [];
    }

    public void WriteBlockMap(string path)
    {
        var writer = OpenBlockMap(path);
        if (writer is null)
            return;
        using (writer)
        {
            writer.NewLine = "\n";
            Trace.TraceInformation("Writing block map !");
            foreach (var block in _blocks.Values)
            {
                writer.WriteLine($"block: {(int)block.BlockType}:{block.Address:X8}:0x{block.Code:x}:{block.HostCodeSize}:{block.GuestCycles}:{block.GuestOpcodes}");
                for (int index = 0; index < block.Opcodes.Count; index++)
                {
                    Sh4ShilOpcode opcode = block.Opcodes[index];
                    writer.WriteLine($"\top: {index}:{opcode.GuestOffset}:{opcode.Disassemble()}");
                }
            }
        }
        Trace.TraceInformation("Finished writing block map");
    }

    public void WriteJitSymbols(TextWriter writer)
    {
        foreach (var block in _blocks.Values)
            writer.WriteLine($"0x{block.Code:x} {block.HostCodeSize} {block.Address:X8}");
    }

    public void ReleaseBlock(Sh4RuntimeBlockInfo block)
    {
        if (block.GuestCodeSize == 0)
            return;
        if (block.ReadOnly)
            ProtectedBlocks--;
        else
            UnprotectedBlocks--;
    }

    public static void AddReference(Sh4RuntimeBlockInfo block, Sh4RuntimeBlockInfo predecessor)
    {
        block.Predecessors.Add(predecessor);
        predecessor.RetainReference();
    }

    public static void RemoveReference(Sh4RuntimeBlockInfo block, Sh4RuntimeBlockInfo predecessor)
    {
        if (block.Predecessors.Remove(predecessor))
            predecessor.ReleaseReference();
    }

    internal void DiscardReferences(Sh4RuntimeBlockInfo block)
    {
        foreach (var predecessor in block.Predecessors)
        {
            if (ReferenceEquals(predecessor.Next, block))
                predecessor.Next = null;
            if (ReferenceEquals(predecessor.Branch, block))
                predecessor.Branch = null;
            predecessor.RelinkData = 0;
            predecessor.Relink();
        }
        block.ReleasePredecessors();

        if (block.ReadOnly)
        {
            for (uint address = block.Address & ~addressSpace.PageMask; address < block.Address + block.GuestCodeSize;
                 address += addressSpace.PageSize)
                _blocksPerPage[GetRamPage(address)].Remove(block);
        }
    }

    public void SetProtectedFlags(Sh4RuntimeBlockInfo block)
    {
        if (!Sh4AddressSpace.IsOnRam(block.Address) || (block.Address & 0x1FFF0000) == MainRamBase)
        {
            block.ReadOnly = false;
            UnprotectedBlocks++;
            return;
        }

        uint start = block.Address & ~addressSpace.PageMask;
        uint end = block.Address + block.GuestCodeSize;
        for (uint address = start; address < end; address += addressSpace.PageSize)
        {
            if (_unprotectedPages[GetRamPage(address)])
            {
                block.ReadOnly = false;
                UnprotectedBlocks++;
                return;
            }
        }

        block.ReadOnly = true;
        ProtectedBlocks++;
        for (uint address = start; address < end; address += addressSpace.PageSize)
        {
            var blocks = _blocksPerPage[GetRamPage(address)];
            if (blocks.Count == 0)
                LockPage(address, addressSpace.PageSize);
            blocks.Add(block);
        }
    }

    public void RamWriteAccess(uint address)
    {
        address &= addressSpace.RamMask;
        int page = GetRamPage(address);
        if (_unprotectedPages[page])
            return;

        _unprotectedPages[page] = true;
        UnlockPage(address, addressSpace.PageSize);
        var blocks = _blocksPerPage[page];
        if (blocks.Count != 0)
        {
            List<Sh4RuntimeBlockInfo> copy = [];
            copy.InsertRange(0, blocks);
            if (copy.Count != 0)
                Debug.WriteLine($"bm_RamWriteAccess write access to {address:x8} pc {driver.Context.ProgramCounter:x8}");
            foreach (var block in copy)
                DiscardBlock(block);
            Sh4HostRuntime.Verify(blocks.Count == 0, "block_list.empty()");
        }
    }

    private int GetRamPage(uint address) => (int)((address & addressSpace.RamMask) / addressSpace.PageSize);

    public uint GetRamOffset(nuint pointer)
    {
#if !SH4_SWITCH
        if (addressSpace.VirtualMemoryEnabled)
        {
            nuint offset = pointer - addressSpace.RamBase;
            if (offset >= MainRamWindowEnd || !Sh4AddressSpace.IsOnRam((uint)offset))
                return uint.MaxValue;
            return (uint)offset & addressSpace.RamMask;
        }
#endif

        nuint ramOffset = addressSpace.Untag(pointer) - addressSpace.Untag(addressSpace.RamBuffer);
        return ramOffset < addressSpace.RamSize ? (uint)ramOffset : uint.MaxValue;
    }

    public bool RamWriteAccess(nuint pointer)
    {
        uint offset = GetRamOffset(pointer);
        if (offset == uint.MaxValue)
            return false;
        RamWriteAccess(offset);
        return true;
    }

    internal static void WriteHex(TextWriter writer, string prefix, ReadOnlySpan<byte> code, ref int offset, int limit)
    {
        int start = offset;
        int count = 0;
        while (offset < limit)
        {
            if (count == 32)
            {
                writer.WriteLine();
                count = 0;
            }
            if (count == 0)
                writer.Write($"{prefix}:{offset - start}:");
            writer.Write($" {code[offset++]:X2}");
            count++;
        }
        writer.WriteLine();
    }

    public void PrintBlocks()
    {
        StreamWriter? blockMap = null;
        if (_printStats)
        {
            blockMap = OpenBlockMap(driver.GetWritableDataPath("blkmap.lst"));
            _printStats = false;
            Trace.TraceInformation($"Writing blocks to 0x{Unsafe.As<StreamWriter?, nuint>(ref blockMap):x}");
        }

        using var writer = blockMap;
        if (writer is null)
            return;

        foreach (var block in _blocks.Values)
        {
            writer.WriteLine($"block: 0x{GetBlockAddress(block):x}");
            writer.WriteLine($"vaddr: {block.VirtualAddress:X8}");
            writer.WriteLine($"paddr: {block.Address:X8}");
            writer.WriteLine($"code: 0x{block.Code:x}");
            writer.WriteLine($"BlockType: {(int)block.BlockType}");
            writer.WriteLine($"NextBlock: {block.NextAddress:X8}");
            writer.WriteLine($"BranchBlock: {block.BranchAddress:X8}");
            writer.WriteLine($"pNextBlock: 0x{GetBlockAddress(block.Next):x}");
            writer.WriteLine($"pBranchBlock: 0x{GetBlockAddress(block.Branch):x}");
            writer.WriteLine($"guest_cycles: {block.GuestCycles}");
            writer.WriteLine($"guest_opcodes: {block.GuestOpcodes}");
            writer.WriteLine($"host_opcodes: {block.HostOpcodes}");
            writer.WriteLine($"il_opcodes: {block.Opcodes.Count}");
            writer.WriteLine("{");

            int guestOffset = -1;
            foreach (var opcode in block.Opcodes)
            {
                if (guestOffset != opcode.GuestOffset)
                {
                    guestOffset = opcode.GuestOffset;
                    uint pc = block.VirtualAddress + (uint)guestOffset;
                    try
                    {
                        ushort guestOpcode = driver.ReadInstruction16(pc);
                        writer.WriteLine($"//g: {guestOpcode:X4} {driver.Disassemble(pc, guestOpcode)}");
                    }
                    catch (Sh4InstructionException)
                    {
                        writer.WriteLine("//g: ???? (page fault)");
                    }
                }
                writer.WriteLine($"//il:{opcode.GuestOffset}:{opcode.HostOffset}: {opcode.Disassemble()}");
            }
            writer.WriteLine("}");
        }
    }

    private static nuint GetBlockAddress(Sh4RuntimeBlockInfo? block) =>
        block?.NativeIdentity ?? 0;

    private static StreamWriter? OpenBlockMap(string path)
    {
        try
        {
            return new StreamWriter(path);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}
#endif
