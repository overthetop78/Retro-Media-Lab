using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Contracts;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Enums;
using C = RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Constants.CpuZ80Constants;

namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Services;

public sealed partial class CpuZ80
{
    public CpuZ80State CaptureState() => new(C.StateVersion, _registers, _bus.CaptureState(),
        _interrupts.CaptureState(), _flow, _operation, _started, _faulted, _prefixEd,
        _opcode, _registerCode, _pairCode, _low, _address, _word, _instructionAddress,
        _instructionsCompleted, _internalClocksRemaining, _index, _profile, _instructionFlagsModified, _control);

    /// <summary>Restaure le CPU sans publier de faux fronts. Le propriétaire restaure la mémoire
    /// et les périphériques avant de poursuivre le temps ; aucune mémoire extérieure n'est copiée ici.</summary>
    public void RestoreState(CpuZ80State state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (_insideClock || _publishingDeadline || _bus.IsPublishing)
            throw new InvalidOperationException("Cannot restore the CPU from a signal callback.");
        if (state.FormatVersion != C.StateVersion || !Enum.IsDefined(state.Flow)
            || state.HardwareProfile != _profile || !Enum.IsDefined(state.Control.Deferred)
            || (state.Control.BusGranted && state.Bus.Active)
            || !Enum.IsDefined(state.Operation) || !Enum.IsDefined(state.Index)
            || state.RegisterCode > 7 || state.PairCode > 3
            || state.InstructionsCompleted < 0 || state.Bus is null
            || state.InternalClocksRemaining is < 0 or > 7
            || (state.InternalClocksRemaining != 0 && (state.Bus.Active || !state.Started))
            || ((state.InternalClocksRemaining != 0) != (state.Flow is
                CpuZ80Flow.InternalResetRecovery or CpuZ80Flow.InternalOperation or CpuZ80Flow.InternalPush or CpuZ80Flow.InternalRelativeRead
                or CpuZ80Flow.InternalRelativeJump or CpuZ80Flow.InternalConditionalReturn or CpuZ80Flow.InternalModifyWrite
                or CpuZ80Flow.InternalIndexedAccess or CpuZ80Flow.InternalIndexCbRead
                or CpuZ80Flow.InternalExchangeWrite or CpuZ80Flow.InternalExchangeFinish or CpuZ80Flow.InternalBitFinish
                or CpuZ80Flow.InternalBlockStart or CpuZ80Flow.InternalBlockFinish or CpuZ80Flow.InternalBlockRepeat))
            || (!state.Started && (state.Bus.Active || state.InstructionsCompleted != 0)))
            throw new ArgumentException("Invalid Z80 CPU state.", nameof(state));
        // Toutes les opérations susceptibles de refuser l'état précèdent le remplacement des champs.
        var interrupts = new Z80InterruptControl(state.Interrupts);
        _bus.RestoreState(state.Bus);
        _interrupts = interrupts;
        _registers = state.Registers; _flow = state.Flow; _operation = state.Operation;
        _started = state.Started; _faulted = state.Faulted; _prefixEd = state.PrefixEd;
        _opcode = state.Opcode; _registerCode = state.RegisterCode; _pairCode = state.PairCode;
        _low = state.Low; _address = state.Address; _word = state.Word;
        _instructionAddress = state.InstructionAddress;
        _instructionsCompleted = state.InstructionsCompleted;
        _internalClocksRemaining = state.InternalClocksRemaining;
        _control = state.Control;
        _index = state.Index;
        _instructionFlagsModified = state.InstructionFlagsModified;
        _publishedNext = _bus.NextEventNanoseconds;
    }
}
