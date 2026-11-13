using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Contracts;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Enums;

namespace RetroMediaLab.Chips.EmuChip.CPU.Zilog.Z80.Z80.Services;

public sealed partial class EmuZ80
{
    private Z80ControlState _control;
    public bool IsHalted => _control.Halted;
    public bool IsBusGranted => _control.BusGranted;
    public void SetInt(bool asserted) => _interrupts.SetInt(asserted);
    public void SetNmi(bool asserted) => _interrupts.SetNmi(asserted);
    public void SetReset(bool asserted) => _control.ResetRequested = asserted;
    public void SetBusRequest(bool asserted) => _control.BusRequested = asserted;

    private void SampleControlInputs()
    {
        _bus.ExchangeControlInputs();
        _control.SampledBusRequest = _control.BusRequested;
        _control.SampledInt = _interrupts.IntAsserted;
        _control.SampledNmi = _interrupts.NmiPending;
    }

    private bool CompleteReceivedBusCycle()
    {
        if (!CycleHasInternalExtension() && GrantBus(CpuZ80Continuation.BusCycleCompleted)) return false;
        BusCycleCompleted();
        return true;
    }

    private void CommitPendingHalt()
    {
        if (_control.PendingHalt is not { } halted) return;
        _bus.SetHalt(halted);
        _control.PendingHalt = null;
    }

    private bool GrantBus(CpuZ80Continuation continuation)
    {
        if (!_control.SampledBusRequest) return false;
        _control.Deferred = continuation;
        _control.BusGranted = true;
        _control.BusReleaseSampled = false;
        _bus.SetBusGranted(true);
        return true;
    }

    // Les états internes peuvent prolonger le cycle M courant : ne pas concéder
    // le bus à la fin des seuls 3/4 T de sa transaction mémoire.
    private bool CycleHasInternalExtension()
    {
        if (_flow is CpuZ80Flow.Fetch or CpuZ80Flow.PrefixIndex)
        {
            byte op = _bus.SampledData;
            return (op & 0xCF) is 0x03 or 0x0B or 0xC5
                || (op & 0xC7) is 0xC0 or 0xC7 || op is 0x10 or 0xF9;
        }
        if (_flow == CpuZ80Flow.PrefixEd)
        {
            byte op = _bus.SampledData;
            return op is 0x47 or 0x4F or 0x57 or 0x5F
                || ((op & 0xE4) == 0xA0 && (op & 3) >= 2);
        }
        return _flow switch
        {
            CpuZ80Flow.InterruptAcknowledge => _interrupts.Mode != Z80InterruptMode.Mode0
                || OpcodeExtendsFetch(_bus.SampledData),
            CpuZ80Flow.NmiFetch => true,
            CpuZ80Flow.ImmediateHigh => _operation == CpuZ80Operation.Call
                || (_operation == CpuZ80Operation.CallConditional && Condition((byte)((_opcode >> 3) & 7))),
            CpuZ80Flow.IndexImmediate or CpuZ80Flow.IndexCbOpcode or CpuZ80Flow.ExchangeHigh
                or CpuZ80Flow.ExchangeWriteLow or CpuZ80Flow.ReadBit => true,
            CpuZ80Flow.ReadModifyWrite => _operation is not
                (CpuZ80Operation.RotateDigitsLeft or CpuZ80Operation.RotateDigitsRight),
            CpuZ80Flow.BlockWriteMemory => _operation == CpuZ80Operation.BlockTransfer,
            _ => false
        };
    }

    private static bool OpcodeExtendsFetch(byte opcode) => (opcode & 0xCF) is 0x03 or 0x0B or 0xC5
        || (opcode & 0xC7) is 0xC0 or 0xC7 || opcode is 0x10 or 0xF9;

    private bool AtInternalBoundary => _internalClocksRemaining == 1
        || (_internalClocksRemaining == 4 && _flow == CpuZ80Flow.InternalOperation
            && _operation is CpuZ80Operation.AddPair or CpuZ80Operation.AddCarryPair or CpuZ80Operation.SubtractCarryPair);

    private void AdvanceInternalClock()
    {
        if (AtInternalBoundary && GrantBus(CpuZ80Continuation.InternalClock)) return;
        if (--_internalClocksRemaining == 0) CompleteInternal();
        else if (AtInternalBoundary) SampleControlInputs();
    }

    private void EnterNormalReset()
    {
        bool requested = _control.ResetRequested;
        _control = new Z80ControlState { ResetRequested = requested, ResetSampled = true, ResetHolding = true,
            BusRequested = _control.BusRequested };
        _bus.EnterReset();
        _registers.PC = 0; _registers.I = 0; _registers.R = 0;
        _interrupts.ApplyNormalResetEffects();
        _internalClocksRemaining = 0;
        _operation = CpuZ80Operation.None;
        _flow = CpuZ80Flow.Fetch;
        _index = CpuZ80Index.None;
        _prefixEd = _instructionFlagsModified = false;
        _started = true;
    }

    private void FallingControlEdge()
    {
        if (_control.ResetSampled)
        {
            if (_control.ResetAtOpcodeT2 && !_control.ResetHolding)
            {
                _control.SpecialResetPending = true;
                if (_control.Halted)
                {
                    // Relevé matériel Brewer : le fetch HALT devient exécutable,
                    // sans l'incrément PC d'un fetch ordinaire.
                    _control.Halted = false;
                    _flow = CpuZ80Flow.Fetch;
                    _bus.SetHalt(false);
                }
            }
            else EnterNormalReset();
        }
        else if (_control.ResetHolding)
        {
            _control.ResetHolding = false;
            BeginInternal(2, CpuZ80Flow.InternalResetRecovery);
        }
        CommitPendingHalt();
        if (_control.BusGranted && _control.BusReleaseSampled)
        {
            _bus.SetBusGranted(false);
            _control.BusGranted = false;
            _control.ResumePending = true;
            _control.SampledBusRequest = false;
        }
    }

    private void BeginIdleFetch(CpuZ80Flow flow)
    {
        _flow = flow;
        ushort refresh = (ushort)((_registers.I << 8) | _registers.R);
        _registers.IncrementRefresh();
        _bus.BeginCycle(Z80BusCycle.OpcodeFetch, _registers.PC, refresh);
    }

    private void ContinueAtInstructionBoundary()
    {
        if (_control.SpecialResetPending)
        {
            _control.SpecialResetPending = false;
            _control.Halted = false;
            _control.PendingHalt = false;
            BeginIdleFetch(CpuZ80Flow.SpecialResetFetch);
            _registers.PC = 0;
        }
        else if (TryBeginInterrupt()) { }
        else if (_control.Halted) BeginIdleFetch(CpuZ80Flow.HaltFetch);
        else BeginFetch();
    }
}
