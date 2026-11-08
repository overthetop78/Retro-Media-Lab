using RetroMediaLab.Chips.CPU.Common.Interfaces;
using System.Globalization;
using C = RetroMediaLab.Chips.CPU.Motorola.M6800.Chip6809.Constants.Cpu6809Constants;

namespace RetroMediaLab.Chips.CPU.Motorola.M6800.Chip6809.Services;

/// <summary>MC6809 : registres, bus 16 bits big-endian et horloge E.</summary>
/// <remarks>La carte possède le bus et les lignes d’interruption. Chaque accès
/// avance les composants ; les cycles internes complètent le coût de l’instruction.</remarks>
public sealed partial class Cpu6809(ICpuBus bus)
{
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
    /// <summary>Retained logical-active IRQ level; sampled by the processor's interrupt phase.</summary>
    public void SetIrq(bool active) => _irqInput = active;
    public void SetFirq(bool active) => _firqInput = active;
    public void SetNmi(bool active) => _nmiInput = active;
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
        CC |= C.IrqMask | C.FirqMask;
        DP = 0;
        _nmiPending = _stackInitialized = _sync = _cwai = _freeRun = false;
        _nmiLevel = _nmiInput;
        _pendingPage = 0;
        _immediateStoreFlags = C.Negative;
        var start = Cycles;
        Idle(C.ResetInternalCycles);
        PC = ReadWord(C.ResetVector);
        Pad(start, 7);
    }

    public void Step()
    {
        var start = Cycles;
        if (_freeRun) { Fetch(); return; }
        if (_pendingPage == 0 && ServiceInterrupt()) return;
        if (_sync || _cwai) { Tick(); return; }
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
        bus.Tick();
        Cycles++;
        var nmi = _nmiInput;
        if (nmi && !_nmiLevel) _nmiPending = true;
        _nmiLevel = nmi;
    }
    private byte Read(ushort address) { Tick(); return bus.Read(address); }
    private void Write(ushort address, byte value) { Tick(); bus.Write(address, value); }
    private byte Fetch() => Read(PC++);
    private ushort FetchWord() { var high = Fetch(); return (ushort)(high << C.ByteBits | Fetch()); }
    private ushort ReadWord(ushort address) { var high = Read(address); return (ushort)(high << C.ByteBits | Read((ushort)(address + 1))); }
    private void WriteWord(ushort address, ushort value) { Write(address, (byte)(value >> C.ByteBits)); Write((ushort)(address + 1), (byte)value); }
    private void Pad(long start, int cycles) { while (Cycles - start < cycles) Tick(); }
    private void Idle(int cycles = 1) { for (var cycle = 0; cycle < cycles; cycle++) Tick(); }
    private bool IsSet(byte flag) => (CC & flag) != 0;
    private void Flag(byte flag, bool set) => CC = set ? (byte)(CC | flag) : (byte)(CC & ~flag);
    private byte Nz(byte value) { Flag(C.Negative, (value & C.ByteSign) != 0); Flag(C.Zero, value == 0); return value; }
    private ushort Nz(ushort value) { Flag(C.Negative, (value & C.WordSign) != 0); Flag(C.Zero, value == 0); return value; }
    private byte Load(byte value) { Flag(C.Overflow, false); return Nz(value); }
    private ushort Load(ushort value) { Flag(C.Overflow, false); return Nz(value); }
    private static Exception Illegal(byte opcode) => new NotSupportedException(opcode.ToString(C.OpcodeFormat, CultureInfo.InvariantCulture));
}
