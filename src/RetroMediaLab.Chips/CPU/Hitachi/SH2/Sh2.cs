using RetroMediaLab.Chips.CPU.Hitachi.SH2.Contracts;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH2;

/// <summary>SH-2 integer interpreter. Owns CPU state, borrows its bus; no host thread or machine dependencies.</summary>
public partial class Sh2<TBus> where TBus : ISh2Bus
{
    private readonly TBus _bus;
    public uint[] R { get; } = new uint[16];
    private uint PC, PPC, GBR, VBR, PR, ID, ET;
    private long MAC;
    private int _branch, _irqLevel, _irqVector;
    private Status SR;
    private bool _yield;
    private ulong _executionEnd;
    private readonly bool _deferClockUpdates;
    private int _pendingBusClocks;
    private ulong _busDeadline;
    public bool AccelerateIdleLoops { get; set; } = true;
    public uint PollingEpoch { get; private set; }
    public ulong Cycles { get; private set; }
    public uint ProgramCounter { get => unchecked(PC - 4); set { PC = unchecked(value + 4); _branch = 0; } }
    public uint StatusRegister { get => SR; set => SR = value; }
    public uint VectorBase { get => VBR; set => VBR = value; }
    public uint GlobalBase { get => GBR; set => GBR = value; }
    public uint ProcedureRegister { get => PR; set => PR = value; }
    public bool Sleeping { get; private set; }
    public bool InterruptReady => _irqLevel == 16 || _irqLevel > SR.I;
    /// <summary>Finish the current instruction, then return the remaining slice to the bus owner.</summary>
    public void YieldExecution() => _yield = true;
    public void Idle(int clocks) { if (clocks > 0) step(clocks); SynchronizeBusClock(); }
    public Sh2(TBus bus) { ArgumentNullException.ThrowIfNull(bus); _bus = bus; _deferClockUpdates = bus.DeferClockUpdates; _busDeadline = bus.NextEventCycle; }
    public void Reset(bool warm = false)
    {
        ClearCodeCache(); _pendingBusClocks = 0; _busDeadline = _bus.NextEventCycle; Array.Clear(R); SR = 0xf0; VBR = GBR = PR = ID = ET = PPC = 0; MAC = 0;
        _branch = _irqLevel = _irqVector = 0; Sleeping = _yield = false; Cycles = 0; PollingEpoch = 0;
        PC = unchecked(_bus.Read32(warm ? 8u : 0u) + 4); R[15] = _bus.Read32(warm ? 12u : 4u);
    }
    public void SetInterrupt(int level, int vector)
    {
        if ((uint)level > 16 || (uint)vector > 255) throw new ArgumentOutOfRangeException(nameof(level));
        _irqLevel = level; _irqVector = vector;
        if (level > SR.I) Sleeping = false;
    }
    public virtual int Execute(int budget)
    {
        if (budget <= 0) return 0;
        SynchronizeBusClock();
        ulong start = Cycles, end = start + (uint)budget;
        _executionEnd = end;
        try
        {
            if (UseCompiledBlocks && System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported) ExecuteNativeLoop();
            else ExecuteLoop();
        }
        finally { SynchronizeBusClock(); }
        return checked((int)(Cycles - start));
    }
    private void ExecuteLoop()
    {
        ulong end = _executionEnd;
        ReadOnlyMemory<byte> fetchMemory = default;
        uint fetchStart = 0, fetchVersion = 0;
        int mappedWait = 0;
        while (Cycles < end)
        {
            if (_irqLevel != 0 && (_irqLevel == 16 || _irqLevel > SR.I) && _branch == 0 && ID == 0)
            { interrupt((uint)_irqLevel, (uint)_irqVector); Sleeping = false; step(5); }
            if (Sleeping) { step((int)(end > Cycles ? end - Cycles : 0)); break; }
            ID = 0;
            if ((PC & 1) != 0) { addressErrorCPU(); step(5); continue; }
            if (UseCompiledBlocks && Cycles < end && System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported && TryExecuteCompiledBlock()) continue;
            uint instructionAddress = unchecked(PC - 4);
            ushort opcode;
            int fetchWait;
            uint offset = unchecked(instructionAddress - fetchStart);
            if (fetchMemory.Length >= 2 && offset <= (uint)(fetchMemory.Length - 2) && fetchVersion == _bus.FetchMapVersion)
            {
                opcode = System.Buffers.Binary.BinaryPrimitives.ReadUInt16BigEndian(fetchMemory.Span.Slice((int)offset, 2));
                fetchWait = mappedWait;
            }
            else if (_bus.TryMapFetch(instructionAddress, out fetchMemory, out fetchStart, out mappedWait))
            {
                fetchVersion = _bus.FetchMapVersion;
                opcode = System.Buffers.Binary.BinaryPrimitives.ReadUInt16BigEndian(fetchMemory.Span.Slice((int)(instructionAddress - fetchStart), 2));
                fetchWait = mappedWait;
            }
            else opcode = _bus.Fetch16(instructionAddress, out fetchWait);
            step(fetchWait + 1); ExecuteOpcode(opcode);
            if (!CompleteInstruction()) break;
        }
    }
    private uint SP { get => R[15]; set => R[15] = value; }
    private uint IdleIterations(uint maximum, int loopClocks)
    {
        if (!AccelerateIdleLoops || Cycles >= _executionEnd || InterruptReady) return 0;
        ulong untilEvent = _deferClockUpdates ? (_busDeadline > Cycles ? _busDeadline - Cycles : 0) : (ulong)Math.Max(0, _bus.CyclesUntilEvent);
        ulong clocks = Math.Min(_executionEnd - Cycles, untilEvent);
        return (uint)Math.Min(maximum, clocks / (uint)loopClocks);
    }
    private uint MACH { get => unchecked((uint)(MAC >> 32)); set => MAC = unchecked((long)((ulong)value << 32 | (uint)MAC)); }
    private uint MACL { get => unchecked((uint)MAC); set => MAC = unchecked((long)((ulong)MAC & 0xffffffff00000000UL | value)); }
    private void step(int clocks)
    {
        Cycles += (uint)clocks;
        if (!_deferClockUpdates) { _bus.AdvanceCycles(clocks); return; }
        _pendingBusClocks += clocks;
        if (Cycles >= _busDeadline) SynchronizeBusClock();
    }
    public void SynchronizeBusClock()
    {
        if (_pendingBusClocks != 0) { int elapsed = _pendingBusClocks; _pendingBusClocks = 0; _bus.AdvanceCycles(elapsed); }
        _busDeadline = _bus.NextEventCycle;
    }
    private uint readByte(long address) { uint a = unchecked((uint)address); step(_bus.WaitStates(a, false)); bool timed = _bus.IsTimedAccess(a); if (timed) SynchronizeBusClock(); uint value = _bus.Read8(a); if (timed) _busDeadline = _bus.NextEventCycle; return value; }
    private uint readWord(long address) { uint a = unchecked((uint)address); step(_bus.WaitStates(a, false)); bool timed = _bus.IsTimedAccess(a); if (timed) SynchronizeBusClock(); uint value = _bus.Read16(a); if (timed) _busDeadline = _bus.NextEventCycle; return value; }
    private uint readLong(long address) { uint a = unchecked((uint)address); step(_bus.WaitStates(a, false)); bool timed = _bus.IsTimedAccess(a); if (timed) SynchronizeBusClock(); uint value = _bus.Read32(a); if (timed) _busDeadline = _bus.NextEventCycle; return value; }
    private void writeByte(long address, long value) { uint a = unchecked((uint)address); step(_bus.WaitStates(a, true)); bool timed = _bus.IsTimedAccess(a); if (timed) SynchronizeBusClock(); _bus.Write8(a, unchecked((byte)value)); if (timed) _busDeadline = _bus.NextEventCycle; }
    private void writeWord(long address, long value) { uint a = unchecked((uint)address); step(_bus.WaitStates(a, true)); bool timed = _bus.IsTimedAccess(a); if (timed) SynchronizeBusClock(); _bus.Write16(a, unchecked((ushort)value)); if (timed) _busDeadline = _bus.NextEventCycle; }
    private void writeLong(long address, long value) { uint a = unchecked((uint)address); step(_bus.WaitStates(a, true)); bool timed = _bus.IsTimedAccess(a); if (timed) SynchronizeBusClock(); _bus.Write32(a, unchecked((uint)value)); if (timed) _busDeadline = _bus.NextEventCycle; }
    private bool inDelaySlot() => _branch == 2;
    private void branch(long address) { PPC = unchecked((uint)address); _branch = 2; step(1); }
    private void delaySlot(long address) { PPC = unchecked((uint)address); _branch = 1; step(1); }
    private void jump(uint address) { PC = address; _branch = 0; }
    private void push(uint data) { SP = unchecked(SP - 4); writeLong(SP, data); }
    private void interrupt(uint level, uint vector) { push(SR); push(unchecked(PC - 4)); jump(unchecked(readLong(VBR + vector * 4) + 4)); SR.I = Math.Min(level, 15); }
    private void addressErrorCPU() { SP &= 0xfffffffc; push(SR); push(unchecked(PC - 4)); jump(unchecked(readLong(VBR + 36) + 4)); }
    private void illegalInstruction() { if (inDelaySlot()) { illegalSlotInstruction(); return; } push(SR); push(unchecked(PC - 4)); PC = unchecked(readLong(VBR + 16) + 4); _branch = 2; PPC = PC; }
    private void illegalSlotInstruction() { push(SR); push(unchecked(PPC - 4)); PPC = unchecked(readLong(VBR + 24) + 4); _branch = 2; }
    private static uint B(bool value) => value ? 1u : 0u;
    private static uint B(uint value) => value;
    private static uint B(long value) => unchecked((uint)value);
    private struct Status
    {
        private uint _value;
        public uint T { readonly get => _value & 1; set => _value = (_value & ~1u) | (value & 1); }
        public uint S { readonly get => (_value >> 1) & 1; set => _value = (_value & ~2u) | ((value & 1) << 1); }
        public uint I { readonly get => (_value >> 4) & 15; set => _value = (_value & ~0xf0u) | ((value & 15) << 4); }
        public uint Q { readonly get => (_value >> 8) & 1; set => _value = (_value & ~0x100u) | ((value & 1) << 8); }
        public uint M { readonly get => (_value >> 9) & 1; set => _value = (_value & ~0x200u) | ((value & 1) << 9); }
        public static implicit operator uint(Status value) => value._value;
        public static implicit operator Status(uint value) => new() { _value = value & 0x3f3 };
    }
}
