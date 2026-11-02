using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Common.Interfaces;
using System.Globalization;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Interfaces;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Mc6809.Constants.Cpu6809Constants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Mc6809.Services;

/// <summary>MC6809 : registres, bus 16 bits big-endian et horloge E.</summary>
/// <remarks>La carte possède le bus et les lignes d’interruption. Chaque accès
/// avance les composants ; les cycles internes complètent le coût de l’instruction.</remarks>
internal sealed partial class Cpu6809(ICpuBus bus)
{
    internal byte A { get; private set; }
    internal byte B { get; private set; }
    internal byte DP { get; private set; }
    internal byte CC { get; private set; }
    internal ushort X { get; private set; }
    internal ushort Y { get; private set; }
    internal ushort U { get; private set; }
    internal ushort S { get; private set; }
    internal ushort PC { get; private set; }
    internal long Cycles { get; private set; }
    internal Func<bool>? Firq { get; set; }
    private bool _nmiLevel;
    private bool _nmiPending;
    private bool _stackInitialized;
    private bool _sync;
    private bool _cwai;
    private int _indexedCycles;
    private ushort D { get => (ushort)(A << C.ByteBits | B); set { A = (byte)(value >> C.ByteBits); B = (byte)value; } }

    internal void Reset()
    {
        CC = C.IrqMask | C.FirqMask;
        DP = 0;
        _nmiPending = _stackInitialized = _sync = _cwai = false;
        _nmiLevel = bus.Nmi;
        var start = Cycles;
        PC = ReadWord(C.ResetVector);
        Pad(start, 7);
    }

    internal void Step()
    {
        var start = Cycles;
        if (ServiceInterrupt()) return;
        if (_sync || _cwai) { Tick(); return; }
        _indexedCycles = 0;
        var opcode = Fetch();
        var page = opcode is 0x10 or 0x11 ? opcode : 0;
        if (page != 0) opcode = Fetch();
        var cycles = Execute(opcode, page);
        Pad(start, cycles + _indexedCycles);
    }

    private void Tick()
    {
        bus.Tick();
        Cycles++;
        var nmi = bus.Nmi;
        if (_stackInitialized && nmi && !_nmiLevel) _nmiPending = true;
        _nmiLevel = nmi;
    }
    private byte Read(ushort address) { Tick(); return bus.Read(address); }
    private void Write(ushort address, byte value) { Tick(); bus.Write(address, value); }
    private byte Fetch() => Read(PC++);
    private ushort FetchWord() { var high = Fetch(); return (ushort)(high << C.ByteBits | Fetch()); }
    private ushort ReadWord(ushort address) { var high = Read(address); return (ushort)(high << C.ByteBits | Read((ushort)(address + 1))); }
    private void WriteWord(ushort address, ushort value) { Write(address, (byte)(value >> C.ByteBits)); Write((ushort)(address + 1), (byte)value); }
    private void Pad(long start, int cycles) { while (Cycles - start < cycles) Tick(); }
    private bool IsSet(byte flag) => (CC & flag) != 0;
    private void Flag(byte flag, bool set) => CC = set ? (byte)(CC | flag) : (byte)(CC & ~flag);
    private byte Nz(byte value) { Flag(C.Negative, (value & C.ByteSign) != 0); Flag(C.Zero, value == 0); return value; }
    private ushort Nz(ushort value) { Flag(C.Negative, (value & C.WordSign) != 0); Flag(C.Zero, value == 0); return value; }
    private byte Load(byte value) { Flag(C.Overflow, false); return Nz(value); }
    private ushort Load(ushort value) { Flag(C.Overflow, false); return Nz(value); }
    private static Exception Illegal(byte opcode) => new NotSupportedException(opcode.ToString(C.OpcodeFormat, CultureInfo.InvariantCulture));
}
