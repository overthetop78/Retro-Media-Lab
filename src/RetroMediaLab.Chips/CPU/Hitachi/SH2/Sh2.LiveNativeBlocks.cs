using System.Reflection;
using RetroMediaLab.Chips.CPU.Hitachi.SH2.Compilation;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH2;

public partial class Sh2<TBus> where TBus : Contracts.ISh2Bus
{
    private Sh2X64Loop? _liveLoopCore;
    private static readonly MethodInfo LiveLoopCoreMethod = typeof(Sh2<TBus>).GetMethod(nameof(RunLiveX64LoopCore), BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly MethodInfo LiveMemoryCoreMethod = typeof(Sh2<TBus>).GetMethod(nameof(AccessLiveX64MemoryCore), BindingFlags.Instance | BindingFlags.NonPublic)!;
    private unsafe void AdvanceLiveNativeFrame(nint pointer, int clocks)
    {

        ulong cycles = Cycles + (uint)clocks;
        Cycles = cycles;
        if (_deferClockUpdates)
        {
            _pendingBusClocks += clocks;
            if (cycles < _busDeadline) return;
            AccessX64Memory(pointer, 0, 0, 0x210);
        }
        else AccessX64Memory(pointer, 0, (uint)clocks, 0x211);
    }

    private unsafe void RunLiveX64LoopCore(nint registers, nint pointer, nint access)
    {

        ulong end = _executionEnd;
        while (Cycles < end)
        {
            uint level = (uint)_irqLevel;
            bool interrupt = level == 16 || level > ((uint)SR >> 4 & 15);
            if (level != 0 && interrupt && _branch == 0 && ID == 0)
            { AccessX64Memory(pointer, 0, 0, 0x230); if (_liveError != 0) return; }
            if (Sleeping)
            { ulong current = Cycles; if (end > current) AdvanceLiveNativeFrame(pointer, (int)(end - current)); return; }
            ID = 0;
            if ((PC & 1) != 0)
            { AccessX64Memory(pointer, 0, 0, 0x231); if (_liveError != 0) return; continue; }
            uint address = unchecked(PC - 4);
            ushort opcode; int wait;
            bool mappedEntry = _bus.TryMapFetch(address, out var memory, out uint start, out wait)
                && address >= start && (ulong)(address - start) + 2 <= (ulong)memory.Length;
            if (mappedEntry)
                opcode = System.Buffers.Binary.BinaryPrimitives.ReadUInt16BigEndian(memory.Span.Slice((int)(address - start), 2));
            else
            {
                opcode = (ushort)AccessX64Memory(pointer, address, 0, 0x220);
                if (_liveError != 0) return;
                wait = (int)_liveFetchWait;
            }
            level = (uint)_irqLevel;
            interrupt = level == 16 || level > ((uint)SR >> 4 & 15);
            if (_useMultiInstructionX64Blocks && PC == unchecked(address + 4)
                && _bus.CodeVersion(address) >= 0)
            {
                _entryFetchWait = wait;
                CodeBlock? block = _codeBlocks.GetValueOrDefault(address);
                ulong epoch = _bus.CodeEpoch(address);
                bool valid = block is { FullNative.ExecutesInstructions: true, Active: true }
                    && block.Instructions[0] == opcode
                    && (block.FullNative.LinkedEntryFetchWait < 0
                        || (mappedEntry ? wait : NativeMappedEntryWait(address, opcode)) == block.FullNative.LinkedEntryFetchWait)
                    && (epoch != 0 && epoch == block.CodeEpoch || BlockWordsMatch(block, opcode));
                if (valid && (epoch == 0 || epoch != block!.CodeEpoch)) CaptureBlockVersions(block!);
                if (!valid) block = CompileNativeRange(address, opcode, block, true);
                if (block is { FullNative.ExecutesInstructions: true, Active: true })
                {
                    _liveEntry = PC;
                    _liveExecuted = _liveChanged = 0;
                    _liveHops = 0;
                    _liveFetchWait = (uint)wait;
                    _executingCodeBlock = block; _executingCodeEpoch = block.CodeEpoch;
                    InvokeLeasedNativePayload(block.FullNative, registers, pointer, access);
                    _executingCodeBlock = null;
                    FullNativeInstructions += _liveExecuted; FullNativeBlockRuns++;
                    FullNativeLinkedHops += _liveHops;
                    if (_liveError != 0) return;
                    continue;
                }
            }
            if (_deferClockUpdates && _branch == 0 && !_yield
                && !interrupt && IsCompiledPureInstruction(opcode) && _bus.CodeVersion(address) >= 0)
            {
                _entryFetchWait = wait;
                _codeBlocks.TryGetValue(address, out CodeBlock? block); ulong epoch = _bus.CodeEpoch(address);
                bool valid = block is { FullNative: not null, Active: true } && block.Instructions[0] == opcode
                    && (epoch != 0 && epoch == block.CodeEpoch || BlockWordsMatch(block, opcode));
                if (valid && (epoch == 0 || epoch != block!.CodeEpoch)) CaptureBlockVersions(block!);
                if (!valid) block = CompileNativeRange(address, opcode, block, false);
                if (block is { FullNative: not null, Active: true })
                {
                    block.CodeEpoch = epoch; int clocks = checked(wait + 1 + block.NativeTailClocks);
                    ulong until = Cycles + (uint)clocks;
                    if (until <= end && until < _busDeadline)
                    {
                        InvokeLeasedNativePayload(block.FullNative, registers, pointer, access);
                        PC = unchecked(PC + (uint)(block.Instructions.Length * 2));
                        AdvanceLiveNativeFrame(pointer, clocks); FullNativeInstructions += block.Instructions.Length; FullNativePureRuns++; continue;
                    }
                }
            }
            AdvanceLiveNativeFrame(pointer, wait + 1); if (_liveError != 0) return;
            ushort key = Sh2OpcodeTable.Types[opcode] == 142 ? (ushort)0xffff : opcode;
            if (!_fullNativeOpcodes.TryGetValue(key, out var instruction)) instruction = CompileNativeOpcode(key);
            InvokeLeasedNativePayload(instruction, registers, pointer, access); FullNativeInstructions++;
            if (_liveError != 0) return;
            switch (_branch)
            {
                case 0: PC = unchecked(PC + 2); break;
                case 1: PC = unchecked(PC + 2); _branch = 2; break;
                case 2: PC = PPC; _branch = 0; break;
            }
            if (!_yield) continue;
            _yield = false;
            ulong elapsed = Cycles; if (elapsed < end) AdvanceLiveNativeFrame(pointer, (int)(end - elapsed)); return;
        }
    }

    private unsafe uint AccessLiveX64MemoryCore(nint pointer, uint address, uint value, uint kind)
    {

        if (kind == 0x240) { _liveError = _liveFetchWait = _liveEntry = _liveChanged = _liveExecuted = _liveHops = _liveReserved0 = _liveReserved1 = _liveReserved2 = 0; return 0; }
        if (kind == 0x241) return 0;
        uint entry = _liveEntry, executed = _liveExecuted, changed = _liveChanged;
        uint hops = _liveHops, fetchWait = _liveFetchWait;
        bool guarding = _executingCodeBlock is not null;
        uint result = 0; int wait = 0;
        switch (kind)
        {
            case 1: result = readByte(address); break;
            case 2: result = readWord(address); break;
            case 4: result = readLong(address); break;
            case 0x101: writeByte(address, value); break;
            case 0x102: writeWord(address, value); break;
            case 0x104: writeLong(address, value); break;
            case 0x200: step((int)value); break;
            case 0x210: SynchronizeBusClock(); break;
            case 0x211: _bus.AdvanceCycles((int)value); break;
            case 0x220: result = _bus.Fetch16(address, out wait); break;
            case 0x230: interrupt((uint)_irqLevel, (uint)_irqVector); Sleeping = false; step(5); break;
            case 0x231: addressErrorCPU(); step(5); break;
            case 0x250: if (Cycles < _executionEnd) step((int)(_executionEnd - Cycles)); break;
            case >= 0x300 and <= 0x30f: NativeIdleDt(kind & 15); break;
            case 0x400ffe: case 0x400ffd: NativeIdleBra(kind & 0xfff); break;
            default: throw new InvalidOperationException("Unsupported native SH-2 bus access.");
        }
        if (guarding && !NativeExecutingCodeMatches((int)fetchWait)) changed = 1;
        _liveReserved0 = AccelerateIdleLoops ? 1u : 0;
        _liveEntry = entry;
        _liveExecuted = executed; _liveChanged = changed;
        _liveHops = hops;
        _liveFetchWait = kind == 0x220 ? (uint)wait : fetchWait; return result;
    }

}


