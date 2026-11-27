using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

using static Sh4BlockEndType;
using static Sh4DecoderConstants;

#if !SH4_DYNAREC_NONE
internal sealed partial class Sh4CodeBuffer
{
    internal const uint CodeSize = 10 * 1024 * 1024;
    internal const uint TemporaryCodeSize = 1024 * 1024;
    internal const uint FullSize = CodeSize + TemporaryCodeSize;

    internal nuint WritableBase;
    internal nuint TemporaryWritableBase;
    internal static nint ExecutableOffset;

    internal nuint Current => UseTemporaryBuffer
        ? TemporaryWritableBase + _temporaryLastAddress
        : WritableBase + _lastAddress;

    internal void Advance(uint size)
    {
        if (UseTemporaryBuffer)
            _temporaryLastAddress += size;
        else
            _lastAddress += size;
    }

    internal uint FreeSpace => UseTemporaryBuffer
        ? TemporaryCodeSize - _temporaryLastAddress
        : CodeSize - _lastAddress;

    internal void Reset(bool temporary)
    {
        if (temporary)
            _temporaryLastAddress = 0;
        else
            _lastAddress = 0;
    }
}

internal sealed unsafe partial class Sh4RecompilerDriver : Sh4Interpreter
{
    private static readonly Lazy<nuint> DeclaredCodeCache = new(() => Sh4VirtualMemory.DeclareCodeCache(Sh4CodeBuffer.FullSize));
    private static readonly HashSet<uint> SelfModifyingCodeHotspots = [];
    private static readonly Sh4CodeBuffer SharedCodeBuffer = new();

    private static nuint _failedToFindBlock = (nuint)(delegate* unmanaged[Cdecl]<void>)&HandleFailedToFindBlock;
    private readonly Sh4AddressSpace _addressSpace;
    private readonly Sh4BlockManager _blocks;
    private readonly Sh4Decoder _decoder;

    internal Sh4RecompilerDriver()
        : base(Sh4DreamcastRuntime.AddressSpace, Sh4DreamcastRuntime.Debugger)
    {
        _addressSpace = Sh4DreamcastRuntime.AddressSpace;
        _blocks = new Sh4BlockManager(this, _addressSpace);
        _decoder = new Sh4Decoder(this);
        Current = this;
    }

    internal Sh4AddressSpace AddressSpace => _addressSpace;
    internal Sh4BlockManager BlockManager => _blocks;
    internal Sh4NativeBackend NativeBackend => Sh4NativeBackend.Current!;
    internal Sh4Decoder Decoder => _decoder;
    internal const uint MainClock = 200_000_000;
    internal Sh4CodeBuffer CodeBuffer => SharedCodeBuffer;
    internal nuint FailedToFindBlock => _failedToFindBlock;

    internal void ClearTemporaryCache(bool full)
    {
        SharedCodeBuffer.Reset(temporary: true);
        _blocks.ResetTemporaryCache(full);
    }

    internal override void ResetCache()
    {
        Trace.TraceInformation($"recSh4:Dynarec Cache clear at {Context.ProgramCounter:X8} free space {SharedCodeBuffer.FreeSpace}");
        SharedCodeBuffer.Reset(temporary: false);
        _blocks.ResetCache();
        SelfModifyingCodeHotspots.Clear();
        ClearTemporaryCache(full: true);
    }

    internal override void Run()
    {
        Sh4Context? previousContext = Sh4Context.ExchangeActive(Context);
        try
        {
            Context.RestoreHostRoundingMode();

            nuint recompilerContext = Context.NativeAddress + Sh4ContextLayout.Size;
            nint blockTableOffset = unchecked((nint)(Context.RecompilerControlBlock - recompilerContext));
            nint programCounterOffset = unchecked((nint)(Context.NativeAddress + Sh4ContextLayout.Pc - recompilerContext));
            Trace.TraceInformation($"cntx // fpcb offset: {blockTableOffset} // pc offset: {programCounterOffset} // pc {Context.ProgramCounter:X8}");

            NativeBackend.MainLoop(recompilerContext);
        }
        finally
        {
            Context.CpuRunning = false;
            Sh4Context.ExchangeActive(previousContext);
        }
    }

    internal bool SetupBlock(Sh4RuntimeBlockInfo block, uint pc, Sh4Fpscr fpuConfiguration)
    {
        block.Address = block.HostCodeSize = 0;
        block.GuestCycles = block.GuestOpcodes = block.HostOpcodes = 0;
        block.GuestCodeSize = 0;
        block.Branch = block.Next = null;
        block.Code = 0;
        block.HasConditionalJump = false;
        block.BranchAddress = NullAddress;
        block.NextAddress = NullAddress;
        block.BlockType = BET_SCL_Intr;
        block.HasFpuOperation = false;
        block.Temporary = false;

        block.VirtualAddress = pc;
        if ((block.VirtualAddress & 1) != 0)
        {
            Context.RaiseException(block.VirtualAddress, Sh4ExceptionEvent.AddressErrorRead);
            return false;
        }
        else if (Mmu.Enabled)
        {
            Sh4MmuError result = Mmu.TranslateInstruction(block.VirtualAddress, out uint address);
            if (result != Sh4MmuError.None)
            {
                Mmu.RaiseInstructionException(block.VirtualAddress, result);
                return false;
            }

            block.Address = address;
        }
        else
        {
            block.Address = block.VirtualAddress;
        }

        block.FpuConfiguration = fpuConfiguration;
        block.Opcodes.Clear();

        try
        {
            if (!_decoder.DecodeBlock(block, Sh4Scheduler.TimeSlice / 2))
                return false;
        }
        catch (Sh4InstructionException exception)
        {
            Context.RaiseException(pc, exception.Event);
            return false;
        }

        block.SetProtectedFlags();
        Sh4Shil.AnalyseBlock(block, this);
        return true;
    }

    private nuint CompileProgramCounter(uint blockCheckFailures)
    {
        uint pc = Context.ProgramCounter;
        if (SharedCodeBuffer.FreeSpace < 32 * 1024 || pc == 0x8c0000e0 || pc == 0xac010000 || pc == 0xac008300)
            ResetCache();

        Sh4RuntimeBlockInfo block = NativeBackend.AllocateBlock();
        if (!block.Setup(this, pc, Context.Fpscr))
        {
            block.Dispose();
            return 0;
        }

        block.BlockCheckFailures = blockCheckFailures;
        if (SelfModifyingCodeHotspots.Contains(block.Address))
        {
            SharedCodeBuffer.UseTemporaryBuffer = true;
            if (SharedCodeBuffer.FreeSpace < 32 * 1024)
                ClearTemporaryCache(full: false);

            block.Temporary = true;
            if (block.ReadOnly)
                Trace.TraceInformation($"WARNING: temp block {block.VirtualAddress:x} ({block.Address:x}) is protected!");
        }

        bool optimize = !block.Temporary;
        bool blockCheck = !block.ReadOnly;
        NativeBackend.Compile(block, blockCheck, optimize);
        Sh4HostRuntime.Verify(block.Code != 0, "rbi->code != nullptr");
        _blocks.AddBlock(block);
        SharedCodeBuffer.UseTemporaryBuffer = false;
        return block.Code;
    }

    internal nuint FindMissingBlockAtProgramCounter()
    {
        return FindMissingBlock(Context.ProgramCounter);
    }

    internal nuint FindMissingBlock(uint pc)
    {
        Context.ProgramCounter = pc;
        nuint code = CompileProgramCounter(0);
        if (code == 0)
            code = _blocks.GetCodeByVirtualAddress(Context.ProgramCounter);
        else
            code = SharedCodeBuffer.ToExecutable(code);
        return code;
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void HandleFailedToFindBlock()
    {
        Current.FindMissingBlock(Current.Context.ProgramCounter);
    }

    internal nuint OnBlockCheckFailed(uint address)
    {
        Debug.WriteLine($"rdv_BlockCheckFail @ {address:x8}");
        uint blockCheckFailures = 0;
        if (Mmu.Enabled)
        {
            Sh4RuntimeBlockInfo? block = _blocks.GetBlock(address);
            if (block is not null)
            {
                try
                {
                    blockCheckFailures = block.BlockCheckFailures + 1;
                    if (blockCheckFailures > 5)
                    {
                        bool inserted = SelfModifyingCodeHotspots.Add(address);
                        if (inserted)
                            Debug.WriteLine($"rdv_BlockCheckFail SMC hotspot @ {address:x8} fails {blockCheckFailures}");
                    }
                    _blocks.DiscardBlock(block);
                }
                finally
                {
                    block.ReleaseReference();
                }
            }
        }
        else
        {
            Context.ProgramCounter = address;
            ResetCache();
        }

        return SharedCodeBuffer.ToExecutable(CompileProgramCounter(blockCheckFailures));
    }

    private nuint FindOrCompile()
    {
        nuint result = _blocks.GetCodeByVirtualAddress(Context.ProgramCounter);
        if (result == _failedToFindBlock)
            result = SharedCodeBuffer.ToExecutable(CompileProgramCounter(0));

        return result;
    }

    internal nuint LinkBlock(nuint code, uint dynamicProgramCounter)
    {
        Sh4RuntimeBlockInfo? block = _blocks.GetBlock(code);
        bool stale = false;
        if (block is null)
        {
            stale = true;
            block = _blocks.GetStaleBlock(code);
        }

        Sh4HostRuntime.Verify(block is not null, "rbi != NULL");

        try
        {
            Sh4BlockEndType blockClass = GetBlockClass(block!.BlockType);
            if (blockClass == BET_CLS_Static)
            {
                if (block.BlockType == BET_StaticIntr)
                    Context.ProgramCounter = block.NextAddress;
                else
                    Context.ProgramCounter = block.BranchAddress;
            }
            else if (blockClass == BET_CLS_Dynamic)
            {
                Context.ProgramCounter = dynamicProgramCounter;
            }
            else if (blockClass == BET_CLS_COND)
            {
                if (dynamicProgramCounter != 0)
                    Context.ProgramCounter = block.BranchAddress;
                else
                    Context.ProgramCounter = block.NextAddress;
            }

            nuint result = FindOrCompile();
            if (Mmu.Enabled)
                return result;

            if (!stale)
            {
                if (blockClass == BET_CLS_Dynamic)
                {
                    Sh4HostRuntime.Verify(block.RelinkData == 0 || block.Branch is null,
                        "rbi->relink_data == 0 || rbi->pBranchBlock == NULL");

                    if (block.Branch is not null)
                    {
                        block.Branch.RemoveRef(block);
                        block.Branch = null;
                        block.RelinkData = 1;
                    }
                    else if (block.RelinkData == 0)
                    {
                        Sh4RuntimeBlockInfo next = _blocks.GetBlock(Context.ProgramCounter)!;
                        block.Branch = next;
                        next.ReleaseReference();
                        block.Branch.AddRef(block);
                    }
                }
                else
                {
                    Sh4RuntimeBlockInfo next = _blocks.GetBlock(Context.ProgramCounter)!;
                    next.ReleaseReference();

                    if (block.BranchAddress == Context.ProgramCounter)
                        block.Branch = next;
                    if (block.NextAddress == Context.ProgramCounter)
                        block.Next = next;

                    next.AddRef(block);
                }

                uint newSize = block.RelinkOffset + block.Relink();
                Sh4HostRuntime.Verify(block.HostCodeSize >= newSize, "rbi->host_code_size >= ncs");
                block.HostCodeSize = newSize;
            }
            else
            {
                Trace.TraceInformation($"null RBI: from {block.VirtualAddress:X8} to {Context.ProgramCounter:X8} -- unlinked stale block -- code 0x{code:x} next 0x{result:x}");
            }

            return result;
        }
        finally
        {
            block?.ReleaseReference();
        }
    }

    internal override void Reset(bool hard)
    {
        base.Reset(hard);
        if (hard)
            NativeBackend.Initialize(Context, SharedCodeBuffer);

        ResetCache();
        if (hard)
            _blocks.Reset();
    }

    internal override void Initialize()
    {
        Trace.TraceInformation("Sh4Recompiler::Init");
        base.Initialize();
        bool initialized = false;
        try
        {
            _blocks.Initialize();

            if (_addressSpace.VirtualMemoryEnabled)
                Sh4HostRuntime.Verify(_addressSpace.RamBuffer == Context.StoreQueueBufferEnd + 0x0C000000);

            SharedCodeBuffer.WritableBase = 0;
#if SH4_NO_RWX_PAGES || SH4_SWITCH
            bool result = Sh4VirtualMemory.PrepareJitBlock(DeclaredCodeCache.Value, Sh4CodeBuffer.FullSize,
                out SharedCodeBuffer.WritableBase, out Sh4CodeBuffer.ExecutableOffset);
#else
            bool result = Sh4VirtualMemory.PrepareJitBlock(DeclaredCodeCache.Value, Sh4CodeBuffer.FullSize,
                out SharedCodeBuffer.WritableBase);
#endif
            Sh4HostRuntime.Verify(result, "rc");
            Sh4HostRuntime.Verify(SharedCodeBuffer.WritableBase != 0, "CodeCache != nullptr");

            SharedCodeBuffer.TemporaryWritableBase = SharedCodeBuffer.WritableBase + Sh4CodeBuffer.CodeSize;
            NativeBackend.Initialize(Context, SharedCodeBuffer);
            _blocks.ResetCache();
            initialized = true;
        }
        finally
        {
            if (!initialized)
                Terminate();
        }
    }

    internal override void Terminate()
    {
        Trace.TraceInformation("Sh4Recompiler::Term");
        try
        {
#if SH4_NO_RWX_PAGES || SH4_SWITCH
            if (SharedCodeBuffer.WritableBase != 0)
                Sh4VirtualMemory.ReleaseJitBlock(SharedCodeBuffer.WritableBase,
                    SharedCodeBuffer.ToExecutable(SharedCodeBuffer.WritableBase), Sh4CodeBuffer.FullSize);
#else
            if (SharedCodeBuffer.WritableBase != 0 && SharedCodeBuffer.WritableBase != DeclaredCodeCache.Value)
                Sh4VirtualMemory.ReleaseJitBlock(SharedCodeBuffer.WritableBase, Sh4CodeBuffer.FullSize);
#endif
        }
        finally
        {
            SharedCodeBuffer.WritableBase = 0;
            SharedCodeBuffer.TemporaryWritableBase = 0;
            try
            {
                _blocks.Terminate();
            }
            finally
            {
                try
                {
                    base.Terminate();
                }
                finally
                {
                    if (ReferenceEquals(Current, this))
                        Current = null!;
                }
            }
        }
    }

    private bool TranslateAddress(uint address, int size, bool write, ref uint outputAddress, Sh4RuntimeBlockInfo? block)
    {
        if (Mmu.Enabled && Mmu.IsTranslated(address, size))
        {
            if ((address & (uint)(size - 1)) != 0)
                return false;

            if (block is not null && (address >> 12) != (block.VirtualAddress >> 12)
                && (address >> 12) != ((block.VirtualAddress + block.GuestCodeSize - 1) >> 12))
                return false;

            Sh4MmuError result = Mmu.TranslateData(address, write, out uint physicalAddress);
            if (result != Sh4MmuError.None)
                return false;

            address = physicalAddress;
        }

        outputAddress = address;
        return true;
    }

    internal bool TryReadMemoryImmediate(uint address, int size, ref nuint pointer, ref bool isRam,
        ref uint physicalAddress, Sh4RuntimeBlockInfo? block = null)
    {
        size = Math.Min(size, 4);
        if (!TranslateAddress(address, size, write: false, ref physicalAddress, block))
            return false;

        pointer = _addressSpace.ReadConstant(physicalAddress, out isRam, size);
        return true;
    }

    internal bool TryWriteMemoryImmediate(uint address, int size, ref nuint pointer, ref bool isRam,
        ref uint physicalAddress, Sh4RuntimeBlockInfo? block = null)
    {
        size = Math.Min(size, 4);
        if (!TranslateAddress(address, size, write: true, ref physicalAddress, block))
            return false;

        pointer = _addressSpace.WriteConstant(physicalAddress, out isRam, size);
        return true;
    }

    internal void SetFailedToFindBlockHandler(nuint handler)
    {
        _failedToFindBlock = handler;
    }

    internal static nuint FindMissingBlockAtProgramCounterEntry =>
        (nuint)(delegate* unmanaged<nuint>)&NativeFindMissingBlockAtProgramCounter;

    internal static nuint FindMissingBlockEntry =>
        (nuint)(delegate* unmanaged<uint, nuint>)&NativeFindMissingBlock;

    internal static nuint OnBlockCheckFailedEntry =>
        (nuint)(delegate* unmanaged<uint, nuint>)&NativeOnBlockCheckFailed;

    internal static nuint LinkBlockEntry =>
        (nuint)(delegate* unmanaged<nuint, uint, nuint>)&NativeLinkBlock;

    internal static nuint UpdateSystemInterruptsEntry =>
        (nuint)(delegate* unmanaged<int>)&NativeUpdateSystemInterrupts;

    [UnmanagedCallersOnly]
    private static nuint NativeFindMissingBlockAtProgramCounter() => Current.FindMissingBlockAtProgramCounter();

    [UnmanagedCallersOnly]
    private static nuint NativeFindMissingBlock(uint pc) => Current.FindMissingBlock(pc);

    [UnmanagedCallersOnly]
    private static nuint NativeOnBlockCheckFailed(uint address) => Current.OnBlockCheckFailed(address);

    [UnmanagedCallersOnly]
    private static nuint NativeLinkBlock(nuint code, uint pc) => Current.LinkBlock(code, pc);

    [UnmanagedCallersOnly]
    private static int NativeUpdateSystemInterrupts() => Current.UpdateSystemInterrupts();

    internal void ResetNativeBackend() => NativeBackend.Reset();
    internal nuint GetCodeByVirtualAddress(uint address) => _blocks.GetCodeByVirtualAddress(address);
    internal ushort ReadInstruction16(uint pc) => _addressSpace.ReadGuestInstruction(pc);
    internal void RaiseReadAddressError(uint pc) => Context.RaiseException(pc, Sh4ExceptionEvent.AddressErrorRead);
    internal void RaiseFpuDisabled(uint pc) => Context.RaiseException(pc, Sh4ExceptionEvent.FpuDisabled);
    internal string Disassemble(uint pc, ushort opcode) => OpcodeTable[opcode].Disassemble(pc, opcode, Context);
    internal string GetWritableDataPath(string name) => Sh4Platform.GetWritableDataPath(name);
}
internal abstract partial class Sh4Executor
{
    internal static partial Sh4Executor GetSh4Recompiler() => new Sh4RecompilerDriver();
}
#endif
