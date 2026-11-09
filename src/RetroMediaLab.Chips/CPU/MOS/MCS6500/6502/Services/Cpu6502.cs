using RetroMediaLab.Chips.CPU.Common.Interfaces;
using RetroMediaLab.Chips.CPU.Common.Enums;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Enums;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Interfaces;
using C = RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Constants.Cpu6502Constants;

namespace RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;

/// <summary>6502 NMOS et 65C12/65C102 CMOS : registres, séquences d'instructions et sondages d'interruption.</summary>
/// <remarks>Les accès transitoires avancent le bus comme les accès utiles. Les cycles internes CMOS font avancer
/// l'horloge sans inventer un accès d'E/S ; les opcodes instables NMOS utilisent le modèle déterministe documenté.</remarks>
public partial class Cpu6502 : ICpu6502
{
    private readonly ICpuBus bus;
    private readonly bool cmos;
    private readonly ICpuBusCyclePins? _cyclePins;
    private bool _samplesReady;
    private bool _samplesReset;
    private bool _samplesOverflow;
    private readonly bool _samplesDataEnable;
    private readonly bool _publishesSync;
    private readonly int _clockDivider;

    public Cpu6502(ICpuBus bus, bool cmos = false)
        : this(bus, cmos ? Cpu6502Variant.Gte65Sc12 : Cpu6502Variant.Nmos6502, 0) { }

    public Cpu6502(ICpuBus bus, long clockFrequency)
        : this(bus, Cpu6502Variant.Nmos6502, clockFrequency)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(clockFrequency);
    }

    protected Cpu6502(ICpuBus bus, Cpu6502Variant variant, long clockFrequency)
    {
        ArgumentNullException.ThrowIfNull(bus);
        if (!Enum.IsDefined(variant)) throw new ArgumentOutOfRangeException(nameof(variant));
        if (clockFrequency < 0 || clockFrequency > long.MaxValue / C.AdvancedClockDivider)
            throw new ArgumentOutOfRangeException(nameof(clockFrequency));
        this.bus = bus;
        Variant = variant;
        cmos = variant != Cpu6502Variant.Nmos6502;
        ClockFrequency = clockFrequency;
        _clockDivider = variant == Cpu6502Variant.Gte65Sc102 ? C.AdvancedClockDivider : C.DirectClockDivider;
        InputClockFrequency = clockFrequency * _clockDivider;
        _cyclePins = bus as ICpuBusCyclePins;
        var signals = bus.ControlSignals;
        _samplesReady = variant != Cpu6502Variant.Gte65Sc12 && (signals & CpuBusControlSignals.Ready) != 0;
        _samplesReset = (signals & CpuBusControlSignals.Reset) != 0;
        _samplesOverflow = (signals & CpuBusControlSignals.SetOverflow) != 0;
        _samplesDataEnable = variant == Cpu6502Variant.Gte65Sc12 && (signals & CpuBusControlSignals.DataBusEnable) != 0;
        _publishesSync = (signals & CpuBusControlSignals.Sync) != 0;
    }

    public Cpu6502Variant Variant { get; }
    public long ClockFrequency { get; }
    public long InputClockFrequency { get; }
    public int ClockDivider => _clockDivider;
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
        RequireCycleBoundary();
        if (!cmos)
        {
            BeginNmosReset();
            if (HasClockReceiver) Step(); else StepNmos();
            return;
        }
        BeginCmosReset();
        Step();
    }

    /// <summary>Exécute jusqu'à la fin de l'instruction/interruption ou un cycle retenu par RDY, puis rend la main.</summary>
    public void Step()
    {
        RequireCycleBoundary();
        if (!cmos && !HasClockReceiver) { StepNmos(); return; }
        if (HasClockReceiver)
        {
            do { Clock(); }
            while (_clockPhaseAdvanced && !AtInstructionBoundary && (cmos || _nmosPhase != Cpu6502BusPhase.Jammed));
            return;
        }
        StepCmos();
    }

    private void Tick()
    {
        // A notified SO edge is captured at rising PHI2, before the bus advances to its trailing edge.
        if (_overflowPinConnected && !_clockOverflowSampled)
        {
            SampleOverflow();
            _clockOverflowSampled = true;
        }
        bus.Tick();
        _cycles++;
        _irqPrevious = _irqLevel;
        _irqLevel = _irqPinConnected ? _irqInput : bus.Irq;
        _nmiPrevious = _nmiPending;
        if (_nmiPinConnected)
        {
            _nmiPending |= _nmiInputEdge;
            _nmiInputEdge = false;
        }
        else SampleNmi();
        if (_samplesOverflow && !_clockOverflowSampled) SampleOverflow();
        _clockOverflowSampled = false;
    }
    private byte Read(ushort address)
    {
        _cyclePins?.PrepareRead(address);
        Tick();
        return bus.Read(address);
    }
    private byte Pop() => Read((ushort)(C.StackBase | ++_s));
    private void Pad(long start, int length) { while (_cycles - start < length) { if (cmos) Tick(); else Read(_pc); } }
    private void Flag(byte flag, bool set) => _p = set ? (byte)(_p | flag) : (byte)(_p & ~flag);
    private byte Nz(byte value)
    {
        _p = (byte)((_p & ~(Zero | Negative)) | (value == 0 ? Zero : 0) | (value & Negative));
        return value;
    }
    private void Compare(byte left, byte right) { Flag(Carry, left >= right); Nz((byte)(left - right)); }
    private void Bit(byte value) { Flag(Zero, (_a & value) == 0); _p = (byte)((_p & 0x3f) | (value & 0xc0)); }

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
            var low = (_a & 15) - (value & 15) - borrow;
            var high = (_a >> 4) - (value >> 4);
            if (low < 0) { low -= 6; high--; }
            if (high < 0) high -= 6;
            _a = (byte)((high << 4) | (low & 15));
        }
        else _a = (byte)binary;
    }
}
