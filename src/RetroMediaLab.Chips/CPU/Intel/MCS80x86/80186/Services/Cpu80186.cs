using static RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Constants.Cpu80186Constants;
using T = RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Constants.Cpu80186Timings;
using I = RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Constants.Interrupt80186Constants;

namespace RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Services;

/// <summary>Intel 80186 en mode réel : registres, bus 20 bits, segments et pile. DMA intégré ; les bus externes sont fournis par la carte.</summary>
public sealed partial class Cpu80186(Func<int, byte> read, Action<int, byte> write, Func<ushort, byte> input, Action<ushort, byte> output)
{
    private readonly ushort[] _registers = new ushort[8];
    private readonly ushort[] _segments = new ushort[4];
    private ushort _flags = Reserved;
    private ushort _start;
    private int _segmentOverride = -1;
    private byte _repeat;
    private bool _halted;
    private int _interruptDelay;
    private bool _suppressSingleStep;
    private bool _enteredInterrupt;
    private int _busPenalty;
    private bool _repeatContinuation;
    private bool _continuingRepeat;
    private ushort _repeatStart;
    private ushort _repeatSegment;
    private byte _suspendedOpcode;
    private ushort _suspendedEnd;
    private int _suspendedSegmentOverride;
    private byte _suspendedRepeat;
    private bool _waiting;
    private bool _nmiLevel;
    private bool _nmiPending;
    private bool _prefixPending;
    private bool _prefixTrace;
    public ushort IP { get; private set; }
    public long Cycles { get; private set; }
    public bool InterruptPending { get; set; }
    public byte InterruptVector { get; set; }
    public bool CoprocessorBusy { get; set; }
    public bool Nmi
    {
        get => _nmiLevel;
        set
        {
            if (value && !_nmiLevel)
            {
                _nmiPending = true;
                // DHLT arrête le prochain DMA ; le transfert qui reçoit ce front se termine.
                if (!SlaveInterruptMode) _interruptStatus |= I.DmaHalt;
            }
            _nmiLevel = value;
        }
    }
    public ushort Register(int index) => _registers[index];
    public ushort Segment(int index) => _segments[index];
    public ushort Flags => _flags;
    public void Reset()
    {
        Array.Clear(_registers); Array.Clear(_segments);
        _segments[Cs] = 0xffff; IP = 0; _flags = Reserved; _halted = false; _interruptDelay = 0;
        _repeatContinuation = _continuingRepeat = _waiting = false;
        _nmiPending = _prefixPending = _prefixTrace = false;
        _enteredInterrupt = _suppressSingleStep = false;
        _lockPrefix = _repeatBusLock = false;
        SetHoldAcknowledged(false);
        ResetDma();
        _peripheralRelocation = Constants.Peripheral80186Constants.ResetRelocation;
        ResetInterruptController();
        ResetTimers();
        ResetChipSelects();
        ResetPrefetch();
    }
    private int Physical(int segment, ushort offset) => ((_segments[segment] << 4) + offset) & AddressMask;
    private byte ReadByte(int segment, ushort offset) => ReadMemoryByte(Physical(segment, offset));
    private ushort ReadWord(int segment, ushort offset, int displacement = 0) =>
        ReadMemoryWord((Physical(segment, offset) + displacement) & AddressMask);
    private void WriteByte(int segment, ushort offset, byte value) => WriteMemoryByte(Physical(segment, offset), value);
    private void WriteWord(int segment, ushort offset, ushort value) => WriteMemoryWord(Physical(segment, offset), value);
    private byte Fetch() => FetchPrefetched();
    private ushort FetchWord() { var low = Fetch(); return (ushort)(low | Fetch() << 8); }
    private void Push(ushort value) { _registers[Sp] -= StackWordBytes; WriteWord(Ss, _registers[Sp], value); }
    private void PushRegister(int register) =>
        Push(register == Sp ? (ushort)(_registers[Sp] - StackWordBytes) : _registers[register]);
    private ushort Pop() { var value = ReadWord(Ss, _registers[Sp]); _registers[Sp] += 2; return value; }
    private int ByteRegister(int index) => index < 4 ? _registers[index] & 255 : _registers[index - 4] >> 8;
    private void SetByteRegister(int index, int value)
    {
        if (index < 4) _registers[index] = (ushort)((_registers[index] & 0xff00) | (value & 255));
        else _registers[index - 4] = (ushort)((_registers[index - 4] & 255) | ((value & 255) << 8));
    }
    private readonly record struct Operand(bool Register, int Index, int Segment, ushort Offset);
    private Operand Decode(byte modrm)
    {
        var mod = modrm >> 6; var rm = modrm & 7;
        if (mod == 3) return new(true, rm, 0, 0);
        var bp = rm is 2 or 3 || rm == 6 && mod != 0;
        var offset = rm switch
        {
            0 => _registers[Bx] + _registers[Si], 1 => _registers[Bx] + _registers[Di],
            2 => _registers[Bp] + _registers[Si], 3 => _registers[Bp] + _registers[Di],
            4 => _registers[Si], 5 => _registers[Di], 6 => mod == 0 ? FetchWord() : _registers[Bp], _ => _registers[Bx]
        };
        if (mod == 1) offset += (sbyte)Fetch();
        else if (mod == 2) offset += FetchWord();
        return new(false, 0, _segmentOverride >= 0 ? _segmentOverride : bp ? Ss : Ds, (ushort)offset);
    }
    private int Load(Operand operand, bool word) => operand.Register ? word ? _registers[operand.Index] : ByteRegister(operand.Index)
        : word ? ReadWord(operand.Segment, operand.Offset) : ReadByte(operand.Segment, operand.Offset);
    private void Store(Operand operand, bool word, int value)
    {
        if (operand.Register) { if (word) _registers[operand.Index] = (ushort)value; else SetByteRegister(operand.Index, value); }
        else if (word) WriteWord(operand.Segment, operand.Offset, (ushort)value); else WriteByte(operand.Segment, operand.Offset, (byte)value);
    }
    private void Flag(ushort flag, bool value) => _flags = value ? (ushort)(_flags | flag) : (ushort)(_flags & ~flag);
    private bool Has(ushort flag) => (_flags & flag) != 0;
    private void Interrupt(byte vector, bool fault = false)
    {
        _enteredInterrupt = true;
        _repeatContinuation = _repeatBusLock = _lockPrefix = _waiting = false;
        Cycles += InterruptCycles;
        _halted = false; Push((ushort)(_flags | ReservedHigh)); Push(_segments[Cs]); Push(fault ? _start : IP);
        _flags &= unchecked((ushort)~(InterruptEnable | Trap));
        IP = ReadMemoryWord(vector * 4);
        _segments[Cs] = ReadMemoryWord(vector * 4 + 2);
        FlushPrefetch();
    }
    public void Step()
    {
        var previousCycles = Cycles;
        ExecuteStep();
        Cycles += _prefetchStalls;
        var elapsed = Cycles - previousCycles;
        AdvanceTimers(elapsed);
        BusClocksAdvanced?.Invoke(elapsed);
    }

    private void ExecuteStep()
    {
        _busPenalty = 0;
        _prefetchStalls = _prefetchAdvancedClocks = _executionBusClocks = _executionWaitClocks = 0;
        if (GrantExternalBus()) return;
        if (!_prefixPending && !ExternalBusLocked && TransferDma()) return;
        if (!_prefixPending)
        {
            if (_interruptDelay == 0)
            {
                if (_nmiPending)
                {
                    _nmiPending = false;
                    Interrupt(NmiVector);
                    PrefetchDuringExecution(InterruptCycles);
                    Cycles += _busPenalty;
                    return;
                }
                if (Has(InterruptEnable) && TryAcknowledgeMaskableInterrupt(out var vector))
                {
                    Interrupt(vector);
                    PrefetchDuringExecution(InterruptCycles);
                    Cycles += _busPenalty;
                    return;
                }
            }
            if (_interruptDelay > 0) _interruptDelay--;
            if (_halted) { Cycles++; return; }
            _continuingRepeat = _repeatContinuation && _repeatSegment == _segments[Cs] && _repeatStart == IP;
            _repeatContinuation = false;
            _lockPrefix = _continuingRepeat && _repeatBusLock;
            _start = IP;
            _segmentOverride = _continuingRepeat || _waiting ? _suspendedSegmentOverride : -1;
            _repeat = _continuingRepeat || _waiting ? _suspendedRepeat : (byte)0;
            _prefixTrace = Has(Trap);
        }
        var prefixCycles = 0;
        _enteredInterrupt = _suppressSingleStep = false;
        var prefixCount = 0;
        byte opcode;
        if (_continuingRepeat || _waiting)
        {
            opcode = _suspendedOpcode;
            IP = _suspendedEnd;
            _waiting = false;
        }
        else while (true)
        {
            opcode = Fetch();
            if (opcode is 0x26 or 0x2e or 0x36 or 0x3e) { _segmentOverride = (opcode >> 3) & 3; prefixCycles += T.Prefix; }
            else if (opcode is 0xf2 or 0xf3) { _repeat = opcode; prefixCycles += T.Prefix; }
            else if (opcode == 0xf0) { _lockPrefix = true; prefixCycles += T.Prefix; }
            else break;
            PrefetchDuringPrefix(T.Prefix);
            if (++prefixCount == PrefixWorkLimit)
            {
                _prefixPending = true;
                Cycles += prefixCycles;
                return;
            }
        }
        _prefixPending = false;
        if (_repeat != 0 && IsStringInstruction(opcode)) prefixCycles -= T.Prefix;
        _suspendedOpcode = opcode;
        _suspendedEnd = IP;
        _suspendedSegmentOverride = _segmentOverride;
        _suspendedRepeat = _repeat;
        var executionStart = Cycles;
        var instructionCycles = Execute(opcode);
        Cycles += instructionCycles + (_continuingRepeat ? 0 : prefixCycles);
        _flags |= Reserved;
        if (_prefixTrace && !_suppressSingleStep && !_enteredInterrupt) Interrupt(SingleStepVector);
        PrefetchDuringExecution((int)(Cycles - executionStart));
        Cycles += _busPenalty;
    }

    private static bool IsStringInstruction(byte opcode) =>
        opcode is >= 0x6c and <= 0x6f or >= 0xa4 and <= 0xa7 or >= 0xaa and <= 0xaf;
}
