using RetroMediaLab.Chips.CPU.Common.Interfaces;
using System.Globalization;
using C = RetroMediaLab.Chips.CPU.Motorola.M6800.Chip6809.Constants.Cpu6809Constants;

namespace RetroMediaLab.Chips.CPU.Motorola.M6800.Chip6809.Services;

/// <summary>MC6809 : registres, bus 16 bits big-endian et horloge E.</summary>
/// <remarks>La carte fournit le bus et la référence temporelle ; les phases E/Q,
/// entrées retenues et séquences du processeur restent dans la puce.</remarks>
public sealed partial class Cpu6809
{
    private readonly ICpuBus _bus;
    private bool _resetSequence;

    public Cpu6809(ICpuBus bus)
    {
        ArgumentNullException.ThrowIfNull(bus);
        _bus = bus;
        _clockBus = bus as Interfaces.ICpu6809ClockBus;
        _cyclePins = bus as ICpuBusCyclePins;
    }

    public byte A { get; private set; }
    public byte B { get; private set; }
    public byte DP { get; private set; }
    public byte CC { get; private set; }
    public ushort X { get; private set; }
    public ushort Y { get; private set; }
    public ushort U { get; private set; }
    public ushort S { get; private set; }
    public ushort PC { get; private set; }
    public long Cycles { get; private set; }
    private bool _irqInput;
    private bool _firqInput;
    private bool _nmiInput;
    private bool _irqClocked;
    private bool _firqClocked;
    private bool _irqSampled;
    private bool _firqSampled;
    private bool _nmiClocked;
    private int _interruptClockStages;
    private bool _interruptsInhibited;
    /// <summary>Retained logical-active IRQ level; sampled by the processor's interrupt phase.</summary>
    public void SetIrq(bool active)
    {
        if (_irqInput == active) return;
        _irqInput = active;
        _interruptClockStages = C.InterruptClockStages;
    }
    public void SetFirq(bool active)
    {
        if (_firqInput == active) return;
        _firqInput = active;
        _interruptClockStages = C.InterruptClockStages;
    }
    public void SetNmi(bool active)
    {
        if (_nmiInput == active) return;
        _nmiInput = active;
        _interruptClockStages = C.InterruptClockStages;
    }
    private bool _nmiLevel;
    private bool _nmiPending;
    private bool _stackInitialized;
    private bool _sync;
    private bool _cwai;
    private bool _freeRun;
    private int _indexedCycles;
    private int _pendingPage;
    private ushort _wordResult;
    private byte _immediateStoreFlags = C.Negative;
    private ushort D { get => (ushort)(A << C.ByteBits | B); set { A = (byte)(value >> C.ByteBits); B = (byte)value; } }

    public void Reset()
    {
        if (_resetInput || _haltInput) { _resetLatched = true; return; }
        _resetSequence = true;
        try { ResetSequence(); }
        catch (ResetAcceptedException) { }
        finally { _resetSequence = false; }
    }

    private void ResetSequence()
    {
        // A retained physical RESET has already spent its assertion cycle.
        // Figure 6, note 3: three FFFE cycles remain after its release.
        var internalCycles = _resetLatched ? C.ResetReleaseInternalCycles : C.ResetInternalCycles;
        _resetLatched = _haltAcknowledged = false;
        _interruptsInhibited = true;
        SetBusStatus(false, false);
        CC |= C.IrqMask | C.FirqMask;
        DP = 0;
        _nmiPending = _stackInitialized = _sync = _cwai = _freeRun = false;
        _nmiLevel = _nmiInput;
        _nmiClocked = _nmiInput;
        _irqClocked = _irqSampled = false;
        _firqClocked = _firqSampled = false;
        _interruptClockStages = 0;
        _pendingPage = 0;
        _immediateStoreFlags = C.Negative;
        var start = _processorCycles;
        for (var cycle = 0; cycle < internalCycles; cycle++)
            ProcessorCycle(address: C.ResetVector);
        PC = ReadVector(C.ResetVector);
        Pad(start, internalCycles + C.ResetVectorAndRecoveryCycles);
    }

    public void Step()
    {
        try { StepInstruction(); }
        catch (ResetAcceptedException) { }
    }

    private void StepInstruction()
    {
        if (HoldResetAtBoundary()) return;
        if (HoldAtInstructionBoundary()) return;
        var start = _processorCycles;
        if (_freeRun) { Fetch(); return; }
        if (_pendingPage == 0 && ServiceInterrupt()) return;
        if (_sync || _cwai) { GrantDma(); ClockBusCycle(); return; }
        _indexedCycles = 0;
        var opcode = Fetch();
        var page = _pendingPage;
        var prefixes = 0;
        while (opcode is C.FirstPrefix or C.SecondPrefix)
        {
            if (page == 0) page = opcode;
            if (++prefixes == C.AddressSpaceSize) { _pendingPage = page; return; }
            opcode = Fetch();
        }
        _pendingPage = 0;
        var cycles = Execute(opcode, page);
        RememberStoreFlags(opcode, page);
        Pad(start, cycles + _indexedCycles + prefixes - (page == 0 ? 0 : 1));
    }

    private void Tick()
    {
        ProcessorCycle();
    }
    private byte Read(ushort address) { ProcessorCycle(true, address); return _bus.Read(address); }
    private void Write(ushort address, byte value) { ProcessorCycle(true, address, false, value); _bus.Write(address, value); }
    private byte Fetch() => Read(PC++);
    private ushort FetchWord() { var high = Fetch(); return (ushort)(high << C.ByteBits | Fetch()); }
    private ushort ReadWord(ushort address) { var high = Read(address); return (ushort)(high << C.ByteBits | Read((ushort)(address + 1))); }
    private ushort ReadVector(ushort address)
    {
        if (_resetSequence && address == C.ResetVector)
        {
            // Figure 9: no interrupt is latched before RESET acknowledge (BS high).
            // A pre-existing NMI level is not a fresh edge after this acknowledge.
            _nmiClocked = _nmiLevel = _nmiInput;
            _interruptClockStages = C.InterruptClockStages;
            _interruptsInhibited = false;
        }
        SetBusStatus(false, true);
        var value = ReadWord(address);
        SetBusStatus(false, false);
        return value;
    }
    private void WriteWord(ushort address, ushort value) { Write(address, (byte)(value >> C.ByteBits)); Write((ushort)(address + 1), (byte)value); }
    private void Pad(long start, int cycles) { while (_processorCycles - start < cycles) Tick(); }
    private void Idle(int cycles = 1) { for (var cycle = 0; cycle < cycles; cycle++) Tick(); }
    private bool IsSet(byte flag) => (CC & flag) != 0;
    private void Flag(byte flag, bool set) => CC = set ? (byte)(CC | flag) : (byte)(CC & ~flag);
    private byte Nz(byte value) { Flag(C.Negative, (value & C.ByteSign) != 0); Flag(C.Zero, value == 0); return value; }
    private ushort Nz(ushort value) { Flag(C.Negative, (value & C.WordSign) != 0); Flag(C.Zero, value == 0); return value; }
    private byte Load(byte value) { Flag(C.Overflow, false); return Nz(value); }
    private ushort Load(ushort value) { Flag(C.Overflow, false); return Nz(value); }
    private static Exception Illegal(byte opcode) => new NotSupportedException(opcode.ToString(C.OpcodeFormat, CultureInfo.InvariantCulture));
}
