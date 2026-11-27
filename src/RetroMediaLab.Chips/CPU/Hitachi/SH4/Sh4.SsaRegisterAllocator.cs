using System.Diagnostics;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

using static Sh4Register;
using static Sh4ShilOperation;

internal abstract class Sh4SsaRegisterAllocator(Sh4Mmu mmu, bool allocateVectorPairs = false)
{
    private Sh4RuntimeBlockInfo _block = null!;
    private readonly LinkedList<int> _hostGeneralRegisters = new();
    private readonly LinkedList<int> _hostFloatRegisters = new();
    private readonly List<Sh4Register> _pendingFlushes = [];
    private readonly SortedDictionary<Sh4Register, Allocation> _allocations = [];
    private readonly int _maxVectorSize = allocateVectorPairs ? 2 : 1;
    private int _operationIndex;
    private bool _finalOperationEnded;
    private bool _fastForwarding;
    internal uint Spills { get; private set; }

    internal void Allocate(Sh4RuntimeBlockInfo block, ReadOnlySpan<int> generalRegisters, ReadOnlySpan<int> floatRegisters)
    {
        _block = block;
        new Sh4SsaOptimizer(block).AddVersionPass();

        Sh4HostRuntime.Verify(_hostGeneralRegisters.Count == 0);
        foreach (int register in generalRegisters)
        {
            if (register == -1)
                break;
            _hostGeneralRegisters.AddLast(register);
        }
        Sh4HostRuntime.Verify(_hostFloatRegisters.Count == 0);
        foreach (int register in floatRegisters)
        {
            if (register == -1)
                break;
            _hostFloatRegisters.AddLast(register);
        }
    }

    internal void BeginOperation(Sh4ShilOpcode opcode, int index)
    {
        _operationIndex = index;
        if (opcode.Operation == shop_ifb)
        {
            FlushAllRegisters(hard: true);
        }
        else if (mmu.Enabled && opcode.Operation is shop_readm or shop_writem or shop_pref)
        {
            FlushAllRegisters(hard: false);
        }
        else if (opcode.Operation == shop_sync_sr)
        {
            FlushRegister(reg_sr_status, hard: true);
            for (var register = reg_r0; register <= reg_r7; register++)
                FlushRegister(register, hard: true);
            for (var register = reg_r0_Bank; register <= reg_r7_Bank; register++)
                FlushRegister(register, hard: true);
        }
        else if (opcode.Operation == shop_sync_fpscr)
        {
            FlushRegister(reg_fpscr, hard: true);
            FlushRegister(reg_old_fpscr, hard: true);
            for (var register = reg_fr_0; register <= reg_xf_15; register++)
                FlushRegister(register, hard: true);
        }

        FlushVector(opcode.Source1, hard: false);
        FlushVector(opcode.Source2, hard: false);
        FlushVector(opcode.Source3, hard: false);
        if (opcode.Operation != shop_ifb)
        {
            AllocateSource(opcode.Source1);
            AllocateSource(opcode.Source2);
            AllocateSource(opcode.Source3);
            FlushVector(opcode.Destination, hard: true);
            FlushVector(opcode.Destination2, hard: true);
            AllocateDestination(opcode.Destination);
            AllocateDestination(opcode.Destination2);
        }
        Debug.WriteLine($"{_block.VirtualAddress + (uint)opcode.GuestOffset:x8}  {opcode.Disassemble()} gregs {_hostGeneralRegisters.Count} fregs {_hostFloatRegisters.Count}");
    }

    private void FlushVector(Sh4ShilParameter parameter, bool hard)
    {
        if (!parameter.IsRegister || parameter.Count <= _maxVectorSize)
            return;
        for (int i = 0; i < parameter.Count; i++)
        {
            var register = parameter.Register + i;
            Sh4HostRuntime.Verify(!hard || !_allocations.TryGetValue(register, out var allocation) || !allocation.WriteBack);
            FlushRegister(register, hard);
        }
    }

    internal void EndOperation(Sh4ShilOpcode opcode)
    {
        foreach (var register in _pendingFlushes)
        {
            Sh4HostRuntime.Verify(!_allocations[register].WriteBack);
            _allocations.Remove(register);
        }
        _pendingFlushes.Clear();

        foreach (var register in _allocations.Keys)
            FlushRegister(register, hard: false);

        if (ReferenceEquals(opcode, _block.Opcodes[^1]))
        {
            FlushAllRegisters(hard: false);
            _finalOperationEnded = true;
        }
    }

    internal void SetOperationIndex(int index)
    {
        _fastForwarding = true;
        for (int i = 0; i < index; i++)
        {
            var opcode = _block.Opcodes[i];
            BeginOperation(opcode, i);
            EndOperation(opcode);
        }
        BeginOperation(_block.Opcodes[index], index);
        _fastForwarding = false;
    }

    internal bool IsAllocated(Sh4ShilParameter parameter)
    {
        if (!parameter.IsRegister)
            return false;
        bool allocated = _allocations.ContainsKey(parameter.Register);
        for (int i = 1; i < parameter.Count; i++)
            Sh4HostRuntime.Verify(_allocations.ContainsKey(parameter.Register + i) == allocated);
        return allocated;
    }

    internal bool IsGeneralAllocated(Sh4ShilParameter parameter) =>
        parameter.IsRegister && parameter.Count <= _maxVectorSize
        && !IsFloat(parameter.Register) && _allocations.ContainsKey(parameter.Register);

    internal bool IsFloatAllocated(Sh4ShilParameter parameter) =>
        parameter.IsRegister && parameter.Count <= _maxVectorSize
        && IsFloat(parameter.Register) && _allocations.ContainsKey(parameter.Register);

    internal int MapGeneral(Sh4ShilParameter parameter)
    {
        Sh4HostRuntime.Verify(IsGeneralAllocated(parameter));
        Sh4HostRuntime.Verify(parameter.Count == 1);
        Sh4HostRuntime.Verify(_allocations.ContainsKey(parameter.Register));
        return _allocations[parameter.Register].HostRegister;
    }

    internal int MapFloat(Sh4ShilParameter parameter, int index = 0)
    {
        Sh4HostRuntime.Verify(IsFloatAllocated(parameter));
        Sh4HostRuntime.Verify(parameter.Count <= _maxVectorSize);
        Sh4Register register = parameter.Register + index;
        Sh4HostRuntime.Verify(_allocations.ContainsKey(register));
        return _allocations[register].HostRegister;
    }

    internal bool IsGeneralUsed(int hostRegister) =>
        _allocations.Any(pair => pair.Value.HostRegister == hostRegister && !IsFloat(pair.Key));

    internal bool IsFloatUsed(int hostRegister) =>
        _allocations.Any(pair => pair.Value.HostRegister == hostRegister && IsFloat(pair.Key));

    internal void Cleanup()
    {
        Sh4HostRuntime.Verify(_finalOperationEnded || _block.Opcodes.Count == 0);
        _finalOperationEnded = false;
        FlushAllRegisters(hard: true);
        Sh4HostRuntime.Verify(_allocations.Count == 0);
        Sh4HostRuntime.Verify(_pendingFlushes.Count == 0);
        _block = null!;
        _hostFloatRegisters.Clear();
        _hostGeneralRegisters.Clear();
    }

    protected abstract void Preload(Sh4Register guestRegister, int hostRegister);
    protected abstract void WriteBack(Sh4Register guestRegister, int hostRegister);
    protected abstract void PreloadFloat(Sh4Register guestRegister, int hostRegister);
    protected abstract void WriteBackFloat(Sh4Register guestRegister, int hostRegister);

    private sealed class Allocation(int hostRegister, ushort version, bool writeBack, bool dirty)
    {
        internal int HostRegister { get; } = hostRegister;
        internal ushort Version { get; set; } = version;
        internal bool WriteBack { get; set; } = writeBack;
        internal bool Dirty { get; set; } = dirty;
    }

    private static bool IsFloat(Sh4Register register) => register is >= reg_fr_0 and <= reg_xf_15;

    private LinkedList<int> AvailableRegisters(bool floating) => floating ? _hostFloatRegisters : _hostGeneralRegisters;

    private void WriteBackRegister(Sh4Register register, Allocation allocation)
    {
        if (!allocation.WriteBack)
            return;
        if (!_fastForwarding)
        {
            if (IsFloat(register))
            {
                Debug.WriteLine($"WB {Sh4Shil.RegisterName(register)}.{allocation.Version} <- xmm{allocation.HostRegister}");
                WriteBackFloat(register, allocation.HostRegister);
            }
            else
            {
                Debug.WriteLine($"WB {Sh4Shil.RegisterName(register)}.{allocation.Version} <- {(char)('a' + allocation.HostRegister)}x");
                WriteBack(register, allocation.HostRegister);
            }
        }
        allocation.WriteBack = false;
        allocation.Dirty = false;
    }

    private void FlushRegister(Sh4Register register, bool hard)
    {
        if (!_allocations.TryGetValue(register, out var allocation))
            return;
        WriteBackRegister(register, allocation);
        if (hard)
        {
            _allocations.Remove(register);
            AvailableRegisters(IsFloat(register)).AddFirst(allocation.HostRegister);
        }
    }

    private void FlushAllRegisters(bool hard)
    {
        if (hard)
        {
            while (_allocations.Count > 0)
                FlushRegister(_allocations.First().Key, hard: true);
        }
        else
        {
            foreach (var register in _allocations.Keys)
                FlushRegister(register, hard: false);
        }
    }

    private int TakeHostRegister(bool floating, bool source)
    {
        var available = AvailableRegisters(floating);
        if (available.Count == 0)
        {
            SpillRegister(floating, source);
            Sh4HostRuntime.Verify(available.Count != 0);
        }
        int hostRegister = available.Last!.Value;
        available.RemoveLast();
        return hostRegister;
    }

    private void AllocateSource(Sh4ShilParameter parameter)
    {
        if (!parameter.IsRegister || parameter.Count > _maxVectorSize)
            return;
        for (int i = 0; i < parameter.Count; i++)
        {
            var register = parameter.Register + i;
            if (_allocations.ContainsKey(register))
                continue;

            int hostRegister = TakeHostRegister(!parameter.IsInteger32, source: true);
            ushort version = parameter.GetVersion(i);
            _allocations[register] = new Allocation(hostRegister, version, writeBack: false, dirty: false);
            if (!_fastForwarding)
            {
                if (IsFloat(register))
                {
                    Debug.WriteLine($"PL {Sh4Shil.RegisterName(register)}.{version} -> xmm{hostRegister}");
                    PreloadFloat(register, hostRegister);
                }
                else
                {
                    Debug.WriteLine($"PL {Sh4Shil.RegisterName(register)}.{version} -> {(char)('a' + hostRegister)}x");
                    Preload(register, hostRegister);
                }
            }
        }
    }

    private bool NeedsWriteBack(Sh4Register register, ushort version)
    {
        for (int i = _operationIndex + 1; i < _block.Opcodes.Count; i++)
        {
            var opcode = _block.Opcodes[i];
            if (opcode.Operation == shop_ifb || (mmu.Enabled && opcode.Operation is shop_readm or shop_writem or shop_pref))
                return true;
            if (opcode.Operation == shop_sync_sr && (register == reg_sr_status
                    || register is >= reg_r0 and <= reg_r7 or >= reg_r0_Bank and <= reg_r7_Bank))
                return true;
            if (opcode.Operation == shop_sync_fpscr && (register is reg_fpscr or reg_old_fpscr
                    || register is >= reg_fr_0 and <= reg_xf_15))
                return true;
            if (opcode.Operation == shop_div1 && register == reg_sr_status)
                return true;
            if (UsesRegister(opcode, register, version, vector: true))
                return true;
            if (DefinesRegister(opcode, register, vector: true) || DefinesRegister(opcode, register, vector: false))
                return false;
        }
        return true;
    }

    private void AllocateDestination(Sh4ShilParameter parameter)
    {
        if (!parameter.IsRegister || parameter.Count > _maxVectorSize)
            return;
        for (int i = 0; i < parameter.Count; i++)
        {
            var register = parameter.Register + i;
            ushort version = parameter.GetVersion(i);
            if (!_allocations.TryGetValue(register, out var allocation))
            {
                int hostRegister = TakeHostRegister(!parameter.IsInteger32, source: false);
                bool writeBack = NeedsWriteBack(register, version);
                _allocations[register] = new Allocation(hostRegister, version, writeBack, dirty: true);
                if (parameter.IsInteger32)
                    Debug.WriteLine($"   {Sh4Shil.RegisterName(register)}.{version} -> {(char)('a' + hostRegister)}x {(writeBack ? "(wb)" : "")}");
                else
                    Debug.WriteLine($"   {Sh4Shil.RegisterName(register)}.{version} -> xmm{hostRegister} {(writeBack ? "(wb)" : "")}");
            }
            else
            {
                Sh4HostRuntime.Verify(!allocation.WriteBack);
                allocation.WriteBack = NeedsWriteBack(register, version);
                allocation.Dirty = true;
                allocation.Version = version;
            }
            Sh4HostRuntime.Verify(_allocations[register].Dirty);
        }
    }

    private void SpillRegister(bool floating, bool source)
    {
        var spilled = NoReg;
        int latestUse = -1;
        foreach (var (register, allocation) in _allocations)
        {
            if (IsFloat(register) != floating || _pendingFlushes.Contains(register)
                    || DefinesRegister(_block.Opcodes[_operationIndex], register, vector: false))
                continue;

            int firstUse = -1;
            for (int i = _operationIndex + (source ? 0 : 1); i < _block.Opcodes.Count; i++)
            {
                if (UsesRegister(_block.Opcodes[i], register, allocation.Version, vector: false))
                {
                    firstUse = i;
                    break;
                }
            }
            if (firstUse == -1)
            {
                latestUse = -1;
                spilled = register;
                break;
            }
            if (firstUse > latestUse && firstUse > _operationIndex)
            {
                latestUse = firstUse;
                spilled = register;
            }
        }

        if (latestUse != -1)
        {
            Debug.WriteLine($"RegAlloc: non optimal alloc? reg {Sh4Shil.RegisterName(spilled)} used in op {latestUse}");
            Spills++;
            if (_allocations[spilled].Dirty)
                _allocations[spilled].WriteBack = true;
        }
        Sh4HostRuntime.Verify(spilled != NoReg);

        if (source)
        {
            FlushRegister(spilled, hard: true);
        }
        else
        {
            var spill = _allocations[spilled];
            WriteBackRegister(spilled, spill);
            AvailableRegisters(IsFloat(spilled)).AddFirst(spill.HostRegister);
            _pendingFlushes.Add(spilled);
        }
    }

    internal bool IsVectorOperation(Sh4ShilOpcode opcode) =>
        opcode.Source1.Count > _maxVectorSize || opcode.Source2.Count > _maxVectorSize
        || opcode.Source3.Count > _maxVectorSize || opcode.Destination.Count > _maxVectorSize
        || opcode.Destination2.Count > _maxVectorSize;

    private bool UsesRegister(Sh4ShilOpcode opcode, Sh4Register register, ushort version, bool vector) =>
        UsesRegister(opcode.Source1, register, version, vector) || UsesRegister(opcode.Source2, register, version, vector)
        || UsesRegister(opcode.Source3, register, version, vector);

    private bool UsesRegister(Sh4ShilParameter parameter, Sh4Register register, ushort version, bool vector) =>
        CoversRegister(parameter, register, vector) && parameter.GetVersion(register - parameter.Register) == version;

    private bool DefinesRegister(Sh4ShilOpcode opcode, Sh4Register register, bool vector) =>
        CoversRegister(opcode.Destination, register, vector) || CoversRegister(opcode.Destination2, register, vector);

    private bool CoversRegister(Sh4ShilParameter parameter, Sh4Register register, bool vector) =>
        parameter.IsRegister && register >= parameter.Register && register < parameter.Register + parameter.Count
        && vector == (parameter.Count > _maxVectorSize);

#if false
    private bool DefinesRegister(int from, int to, Sh4Register register)
    {
        for (int i = from; i <= to; i++)
        {
            var opcode = _block.Opcodes[i];
            if (opcode.Destination.IsRegister && register >= opcode.Destination.Register
                && register < opcode.Destination.Register + opcode.Destination.Count)
                return true;
            if (opcode.Destination2.IsRegister && register >= opcode.Destination2.Register
                && register < opcode.Destination2.Register + opcode.Destination2.Count)
                return true;
        }
        return false;
    }

    private bool EarlyLoad(int currentIndex, int earlyIndex)
    {
        var opcode = _block.Opcodes[currentIndex];
        var next = _block.Opcodes[earlyIndex];
        if (next.Operation is shop_ifb or shop_sync_sr or shop_sync_fpscr)
            return false;
        if (next.Source1.IsRegister && next.Source1.Count == 1
            && !DefinesRegister(currentIndex, earlyIndex - 1, next.Source1.Register))
        {
            if (next.Source1.IsInteger32 && _hostGeneralRegisters.Count != 0
                || next.Source1.IsFloat32 && _hostFloatRegisters.Count != 0)
            {
                Debug.WriteLine($"Early loading (+{earlyIndex - currentIndex}) rs1: {Sh4Shil.RegisterName(next.Source1.Register)}");
                AllocateSource(next.Source1);
            }
        }
        if (next.Source2.IsRegister && next.Source2.Count == 1
            && !DefinesRegister(currentIndex, earlyIndex - 1, next.Source2.Register))
        {
            if (next.Source2.IsInteger32 && _hostGeneralRegisters.Count != 0
                || next.Source2.IsFloat32 && _hostFloatRegisters.Count != 0)
            {
                Debug.WriteLine($"Early loading (+{earlyIndex - currentIndex}) rs2: {Sh4Shil.RegisterName(next.Source1.Register)}");
                AllocateSource(next.Source2);
            }
        }
        if (next.Source3.IsRegister && next.Source3.Count == 1
            && !DefinesRegister(currentIndex, earlyIndex - 1, next.Source3.Register))
        {
            if (next.Source3.IsInteger32 && _hostGeneralRegisters.Count != 0
                || next.Source3.IsFloat32 && _hostFloatRegisters.Count != 0)
            {
                Debug.WriteLine($"Early loading (+{earlyIndex - currentIndex}) rs3: {Sh4Shil.RegisterName(next.Source1.Register)}");
                AllocateSource(next.Source3);
            }
        }
        return true;
    }
#endif
}
