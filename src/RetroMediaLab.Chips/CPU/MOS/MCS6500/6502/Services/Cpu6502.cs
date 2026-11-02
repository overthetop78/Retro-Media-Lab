using RetroMediaLab.Chips.CPU.Common.Interfaces;
using RetroMediaLab.Chips.CPU.Common.Enums;
using C = RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Constants.Cpu6502Constants;

namespace RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;

/// <summary>6502 NMOS et 65C12/65C102 CMOS : registres, séquences d'instructions et sondages d'interruption.</summary>
/// <remarks>Les accès transitoires avancent le bus comme les accès utiles. Les cycles internes CMOS font avancer
/// l'horloge sans inventer un accès d'E/S ; les opcodes instables NMOS utilisent le modèle déterministe documenté.</remarks>
public sealed partial class Cpu6502(ICpuBus bus, bool cmos = false)
{
    private readonly bool _samplesReady = (bus.ControlSignals & CpuBusControlSignals.Ready) != 0;
    private readonly bool _samplesReset = (bus.ControlSignals & CpuBusControlSignals.Reset) != 0;
    private readonly bool _samplesOverflow = (bus.ControlSignals & CpuBusControlSignals.SetOverflow) != 0;
    private readonly bool _samplesDataEnable = (bus.ControlSignals & CpuBusControlSignals.DataBusEnable) != 0;
    private readonly bool _publishesSync = (bus.ControlSignals & CpuBusControlSignals.Sync) != 0;
    private const byte Carry = 1;
    private const byte Zero = 2;
    private const byte InterruptDisable = 4;
    private const byte Decimal = 8;
    private const byte Break = 16;
    private const byte Unused = 32;
    private const byte Overflow = 64;
    private const byte Negative = 128;
    private bool _irqPending;
    private bool _irqLevel;
    private bool _irqPrevious;
    private bool? _branchIrqSample;
    private bool? _branchNmiSample;

    /// <summary>Registres du CPU ; la pile matérielle occupe la page 01 du bus.</summary>
    private byte _a;
    public byte A { get => _a; private set => _a = value; }
    private byte _x;
    public byte X { get => _x; private set => _x = value; }
    private byte _y;
    public byte Y { get => _y; private set => _y = value; }
    private byte _s;
    public byte S { get => _s; private set => _s = value; }
    private byte _p = InterruptDisable | Unused;
    public byte P { get => _p; private set => _p = value; }
    private ushort _pc;
    public ushort PC { get => _pc; private set => _pc = value; }
    private ushort _instructionAddress;
    public ushort InstructionAddress { get => _instructionAddress; private set => _instructionAddress = value; }
    private long _cycles;
    public long Cycles { get => _cycles; private set => _cycles = value; }
    /// <summary>Termine un appel MOS traité explicitement par l'assemblage de la machine, comme un RTS matériel.</summary>
    /// <remarks>Le CPU ne décide pas quel appel intercepter. Le modèle choisit le contrat MOS et les données à charger.</remarks>
    public void CompleteHostCall(byte result)
    {
        _a = result; _p &= unchecked((byte)~Carry);
        var start = _cycles;
        Read(_pc);
        PreparePull();
        var low = Pop();
        var last = (ushort)(low | Pop() << C.HighByteShift);
        Read(last);
        _pc = (ushort)(last + 1);
        Pad(start, 6);
    }

    /// <summary>Applique RESET et lit le vecteur FFFC/FFFD. Le bus et les périphériques sont réinitialisés par leur propriétaire.</summary>
    public void Reset()
    {
        if (!cmos) { BeginNmosReset(); StepNmos(); return; }
        ResetNmosSequence();
        _overflowLevel = _samplesOverflow && bus.SetOverflow;
        _nmiPending = false; _nmiLevel = bus.Nmi;
        _nmiAccepted = _nmiPrevious = false;
        _irqPending = _irqPrevious = _irqLevel = false;
        _p = (byte)(_p | InterruptDisable | Unused);
        if (cmos) _p &= unchecked((byte)~Decimal);
        Read(_pc);
        Read(_pc);
        for (var i = 0; i < 3; i++) Read((ushort)(C.StackBase | _s--));
        _pc = ReadWord(C.ResetVector);
        _irqPending = false;
    }

    /// <summary>Exécute jusqu'à la fin de l'instruction/interruption ou une lecture NMOS retenue par RDY, puis rend la main.</summary>
    public void Step()
    {
        if (!cmos) { StepNmos(); return; }
        var start = _cycles;
        if (_nmiAccepted)
        {
            _nmiPending = _nmiAccepted = false;
            EnterInterrupt(start, C.NmiVector);
            return;
        }
        if (_irqPending)
        {
            EnterInterrupt(start, C.IrqVector);
            return;
        }
        _instructionAddress = _pc;
        _branchIrqSample = null;
        _branchNmiSample = null;
        var previousFlags = _p;
        var opcode = FetchOpcode();
        var expected = ExecuteCmos(opcode);
        Pad(start, expected);
        _p |= Unused;
        var interruptFlags = opcode is (0x58 or 0x78 or 0x28) ? previousFlags : _p;
        _irqPending = _branchIrqSample ?? (_irqPrevious && (interruptFlags & InterruptDisable) == 0);
        _nmiAccepted = _branchNmiSample ?? _nmiPrevious;
    }

    private void Tick()
    {
        bus.Tick();
        _cycles++;
        _irqPrevious = _irqLevel;
        _irqLevel = bus.Irq;
        _nmiPrevious = _nmiPending;
        SampleNmi();
        if (_samplesOverflow) SampleOverflow();
    }
    private byte Read(ushort address) { Tick(); return bus.Read(address); }
    private void Write(ushort address, byte value) { Tick(); bus.Write(address, value); }
    private byte Fetch() => Read(_pc++);
    private byte FetchOpcode()
    {
        if (_publishesSync) bus.SetSync(true);
        try { return Fetch(); }
        finally { if (_publishesSync) bus.SetSync(false); }
    }
    private ushort FetchWord() { var low = Fetch(); return (ushort)(low | Fetch() << 8); }
    private ushort ReadWord(ushort address) { var low = Read(address); return (ushort)(low | Read((ushort)(address + 1)) << 8); }
    private ushort ZeroPageWord(byte address) { var low = Read(address); return (ushort)(low | Read((byte)(address + 1)) << 8); }
    private void Push(byte value) => Write((ushort)(C.StackBase | _s--), value);
    private byte Pop() => Read((ushort)(C.StackBase | ++_s));
    private void Pad(long start, int length) { while (_cycles - start < length) { if (cmos) Tick(); else Read(_pc); } }
    private void Flag(byte flag, bool set) => _p = set ? (byte)(_p | flag) : (byte)(_p & ~flag);
    private byte Nz(byte value)
    {
        _p = (byte)((_p & ~(Zero | Negative)) | (value == 0 ? Zero : 0) | (value & Negative));
        return value;
    }
    private void Compare(byte left, byte right) { Flag(Carry, left >= right); Nz((byte)(left - right)); }

    /// <summary>ADC NMOS : les indicateurs ne suivent pas tous le résultat décimal corrigé.</summary>
    private void Add(byte value)
    {
        var c = _p & Carry;
        var binary = _a + value + c;
        if ((_p & Decimal) != 0)
        {
            var low = (_a & 15) + (value & 15) + c;
            if (low > 9) low += 6;
            var sum = (_a & 0xf0) + (value & 0xf0) + (low & 15) + (low > 15 ? 16 : 0);
            Flag(Zero, (byte)binary == 0);
            Flag(Negative, (sum & 0x80) != 0);
            Flag(Overflow, (~(_a ^ value) & (_a ^ sum) & 0x80) != 0);
            if (sum >= 0xa0) sum += 0x60;
            Flag(Carry, sum > 255);
            _a = (byte)sum;
            if (cmos) { Nz(_a); Tick(); }
        }
        else
        {
            Flag(Overflow, (~(_a ^ value) & (_a ^ binary) & 0x80) != 0);
            Flag(Carry, binary > 255);
            _a = Nz((byte)binary);
        }
    }

    /// <summary>SBC conserve les indicateurs de la soustraction binaire en mode décimal NMOS.</summary>
    private void Subtract(byte value)
    {
        var borrow = (_p & Carry) == 0 ? 1 : 0;
        var binary = _a - value - borrow;
        Flag(Overflow, ((_a ^ value) & (_a ^ binary) & 0x80) != 0);
        Flag(Carry, binary >= 0);
        Nz((byte)binary);
        if ((_p & Decimal) != 0)
        {
            if (cmos)
            {
                var corrected = binary;
                if (binary < 0) corrected -= 0x60;
                if ((_a & 15) - (value & 15) - borrow < 0) corrected -= 6;
                _a = Nz((byte)corrected);
                Tick();
                return;
            }
            var low = (_a & 15) - (value & 15) - borrow;
            var high = (_a >> 4) - (value >> 4);
            if (low < 0) { low -= 6; high--; }
            if (high < 0) high -= 6;
            _a = (byte)((high << 4) | (low & 15));
        }
        else _a = (byte)binary;
    }
}
