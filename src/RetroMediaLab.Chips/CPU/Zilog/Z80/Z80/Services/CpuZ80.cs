using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Contracts;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Enums;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Interfaces;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Models;

namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Services;

/// <summary>Cœur Z80 : séquencement du décodeur et du contrôle par le bus matériel commun.</summary>
/// <remarks>État initial fourni explicitement. Aucun oscillateur, thread, mémoire de carte,
/// service de périphérique ni seconde voie d'exécution n'est créé. L'implémentation complète
/// est suivie dans la fiche de la puce ; les variantes restent à qualifier avant clôture.</remarks>
public sealed partial class CpuZ80 : ICpuZ80
{
    private readonly Z80BusSequencer _bus;
    private readonly Z80HardwareProfile _profile;
    private Z80InterruptControl _interrupts;
    private Z80Registers _registers;
    private CpuZ80Flow _flow;
    private CpuZ80Operation _operation;
    private CpuZ80Index _index;
    private bool _started, _faulted, _prefixEd, _insideClock, _publishingDeadline;
    private byte _opcode, _registerCode, _pairCode, _low;
    private ushort _address, _word, _instructionAddress;
    private long _instructionsCompleted;
    private int _internalClocksRemaining;
    private bool _instructionFlagsModified;
    private long? _publishedNext;
    private Action<long?>? _deadlineListener;
    private readonly Action<long?> _busDeadlineListener;

    public CpuZ80(Z80BusTiming timing, Z80Registers initialRegisters,
        Z80InterruptState initialInterrupts, byte initialInputData)
        : this(Z80HardwareProfile.ZilogNmos, timing, initialRegisters, initialInterrupts, initialInputData) { }

    public CpuZ80(Z80HardwareProfile profile, Z80BusTiming timing, Z80Registers initialRegisters,
        Z80InterruptState initialInterrupts, byte initialInputData)
    {
        ArgumentNullException.ThrowIfNull(profile);
        profile.Validate();
        _profile = profile;
        _interrupts = new Z80InterruptControl(initialInterrupts);
        _bus = new Z80BusSequencer(timing, initialInputData);
        _registers = initialRegisters;
        _busDeadlineListener = OnBusDeadlineChanged;
    }

    public Z80Registers Registers => _registers;
    public Z80SiliconVariant Silicon => _profile.Silicon;
    public Z80HardwareProfile HardwareProfile => _profile;
    public Z80InterruptState InterruptState => _interrupts.CaptureState();
    public Z80BusPins Pins => _bus.Pins;
    public long TimeNanoseconds => _bus.TimeNanoseconds;
    public long InputClockCount => _bus.RisingClocks;
    public long InstructionsCompleted => _instructionsCompleted;
    public ushort InstructionAddress => _instructionAddress;
    public bool IsFaulted => _faulted;
    public bool AtInstructionBoundary => !_faulted && (!_started ||
        (_flow == CpuZ80Flow.Fetch && _bus.AtCycleStart));
    public long? NextEventNanoseconds => _bus.NextEventNanoseconds;
    public Action<Z80BusPins>? PinsChanged { get => _bus.PinsChanged; set => _bus.PinsChanged = value; }
    public Action<long?>? NextEventNanosecondsChanged
    {
        get => _deadlineListener;
        set
        {
            if (_deadlineListener is null && value is not null)
                _publishedNext = _bus.PublishedNextEventNanoseconds;
            _deadlineListener = value;
            _bus.NextEventNanosecondsChanged = value is null ? null : _busDeadlineListener;
        }
    }
    public void SetData(byte data) => _bus.SetData(data);
    public void SetWait(bool asserted) => _bus.SetWait(asserted);
    public void AdvanceTo(long nanoseconds)
    {
        if (_publishingDeadline && nanoseconds != TimeNanoseconds)
            throw new InvalidOperationException("Cannot advance time from a CPU deadline callback.");
        _bus.AdvanceTo(nanoseconds);
    }

    public void SetClock(bool high)
    {
        if (_faulted) throw new InvalidOperationException("The incomplete Z80 decoder is stopped on an unsupported operation.");
        if (_bus.ClockHigh == high) return;
        if (_insideClock || _publishingDeadline) throw new InvalidOperationException("Cannot recursively drive the CPU clock.");
        _hasClockSource = false;
        _insideClock = true;
        try
        {
            _bus.ValidateClockEdge(_bus.TimeNanoseconds, high);
            ApplyClockEdge(high);
        }
        finally { _insideClock = false; if (_deadlineListener is not null) PublishDeadline(); }
    }

    // Parcours canonique commun aux niveaux isolés et aux trains reçus.
    private void ApplyClockEdge(bool high)
    {
        bool wasActive = _bus.IsActive;
        _bus.AdvanceClockEdge(high);
        if (!high) { FallingControlEdge(); return; }
        _control.ResetSampled = _control.ResetRequested;
        _control.ResetAtOpcodeT2 = _bus.AtOpcodeT2;
        if (_control.ResetHolding || (!_started && _control.ResetRequested)) return;
        if (_control.BusGranted)
        {
            _control.BusReleaseSampled = !_control.BusRequested;
            return;
        }
        if (_control.ResumePending)
        {
            _control.ResumePending = false;
            var deferred = _control.Deferred;
            _control.Deferred = CpuZ80Continuation.None;
            if (deferred == CpuZ80Continuation.BusCycleCompleted) BusCycleCompleted();
            else if (deferred == CpuZ80Continuation.InternalClock) AdvanceInternalClock();
            return;
        }
        if (!_started) { _started = true; BeginFetch(); }
        else if (_internalClocksRemaining != 0) AdvanceInternalClock();
        else if (wasActive && !_bus.IsActive)
        {
            if (!CycleHasInternalExtension() && GrantBus(CpuZ80Continuation.BusCycleCompleted)) return;
            BusCycleCompleted();
        }
        if (_bus.AtLastClockStart)
        {
            SampleControlInputs();
            if (_flow is CpuZ80Flow.Fetch or CpuZ80Flow.PrefixIndex && _bus.SampledData == 0x76)
                _control.PendingHalt = true;
            if (_control.Halted && (_control.SampledNmi ||
                (_control.SampledInt && _interrupts.Iff1 && _interrupts.CaptureState().EiInhibitRemaining == 0)))
                _control.PendingHalt = false;
        }
    }

    private void OnBusDeadlineChanged(long? next)
    {
        if (!_insideClock) PublishDeadline();
    }

    private void PublishDeadline()
    {
        if (_deadlineListener is not { } listener) return;
        long? next = _bus.NextEventNanoseconds;
        if (_publishedNext == next) return;
        _publishedNext = next;
        _publishingDeadline = true;
        try { listener(next); }
        finally { _publishingDeadline = false; }
    }

    private void RetireInstruction()
    {
        _registers.Q = _instructionFlagsModified ? _registers.F : (byte)0;
        _instructionsCompleted = checked(_instructionsCompleted + 1);
        _interrupts.InstructionCompleted();
        ContinueAtInstructionBoundary();
    }

    private void Unsupported(byte opcode)
    {
        _opcode = opcode;
        _faulted = true;
        string prefix = _prefixEd ? "ED " : _flow == CpuZ80Flow.PrefixCb ? "CB "
            : _index == CpuZ80Index.IX ? "DD " : _index == CpuZ80Index.IY ? "FD " : "";
        throw new NotSupportedException($"Z80 instruction not yet implemented at {_instructionAddress:X4}: {prefix}{opcode:X2}.");
    }
}
