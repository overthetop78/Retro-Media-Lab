using Phase = RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Enums.Cpu6502BusPhase;
using C = RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Constants.Cpu6502Constants;

namespace RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;

public partial class Cpu6502
{
    private int _inputClockPhase;
    private long _clockCycleStart;
    private bool _clockPresentOnly;
    private bool _clockCyclePrepared;
    private bool _clockResetCycle;
    private bool _clockPhaseAdvanced;
    private bool _clockOverflowSampled;
    private bool _externalClockDriven;
    private bool _externalClockInputHigh;
    protected Action<bool, bool>? AdvancedClockOutputsChanged { get; set; }
    protected Action<bool>? DirectClockOutputChanged { get; set; }
    protected bool ExternalClockInputHigh => _externalClockInputHigh;
    protected bool AdvancedPhase2High => _inputClockPhase >= C.AdvancedPhase2Start && _inputClockPhase < C.AdvancedClockDivider;
    protected bool AdvancedPhase4High => _inputClockPhase >= C.AdvancedPhase4Start && _inputClockPhase < C.AdvancedPhase4End;
    private bool HasClockReceiver => _clockDivider == C.AdvancedClockDivider
        ? AdvancedClockOutputsChanged is not null : DirectClockOutputChanged is not null;
    public long InputClocks => _clockCyclePrepared
        ? checked(_clockCycleStart + _inputClockPhase) : checked(_cycles * _clockDivider + _inputClockPhase);

    /// <summary>Un front de cycle CPU, sans exécuter les phases restantes de l'instruction.</summary>
    public void Clock()
    {
        RequireCycleBoundary();
        if (HasClockReceiver) AdvanceInputClocks(_clockDivider);
        else RunClockCycle();
    }

    private void RequireCycleBoundary()
    {
        if (_externalClockDriven || _inputClockPhase != 0 || _clockPresentOnly || _clockCyclePrepared)
            throw new InvalidOperationException(nameof(AdvanceInputClocks));
    }

    /// <summary>Conserve les périodes incomplètes du diviseur interne, même pendant RDY.</summary>
    public void AdvanceInputClocks(long clocks)
    {
        if (clocks < 0) throw new ArgumentOutOfRangeException(nameof(clocks));
        if (_externalClockDriven) throw new InvalidOperationException(nameof(AdvanceInputClocks));
        AdvanceInputClocksCore(clocks);
    }

    private void AdvanceInputClocksCore(long clocks)
    {
        while (clocks != 0)
        {
            // No receiver observes quarter-cycle outputs: execute the same complete cycle directly.
            if (_inputClockPhase == 0 && clocks >= _clockDivider && !HasClockReceiver)
            {
                RunClockCycle();
                clocks -= _clockDivider;
                continue;
            }
            if (_clockDivider == C.DirectClockDivider)
            {
                PrepareAdvancedClockCycle();
                if (_samplesOverflow) { SampleOverflow(); _clockOverflowSampled = true; }
                DirectClockOutputChanged?.Invoke(true);
                _inputClockPhase = C.DirectClockDivider;
                CompletePreparedClockCycle();
                DirectClockOutputChanged?.Invoke(false);
                clocks--;
                continue;
            }
            if (_inputClockPhase == 0) PrepareAdvancedClockCycle();
            _inputClockPhase++;
            clocks--;
            if (_inputClockPhase == C.AdvancedPhase2Start && _samplesOverflow)
            {
                SampleOverflow();
                _clockOverflowSampled = true;
            }
            if (_inputClockPhase == _clockDivider)
            {
                CompletePreparedClockCycle();
            }
            AdvancedClockOutputsChanged?.Invoke(AdvancedPhase2High, AdvancedPhase4High);
        }
    }

    /// <summary>Entrée PHI2 du GTE12, ou CLK de l'oscillateur du GTE102 ; aucun thread ni attente hôte.</summary>
    protected void SetCmosClockInput(bool high)
    {
        if (!_externalClockDriven)
        {
            RequireCycleBoundary();
            _externalClockDriven = true;
            if (_clockDivider == C.DirectClockDivider) PrepareAdvancedClockCycle();
        }
        if (_externalClockInputHigh == high) return;
        _externalClockInputHigh = high;
        if (_clockDivider == C.AdvancedClockDivider)
        {
            if (high) AdvanceInputClocksCore(C.DirectClockDivider);
            return;
        }
        if (high)
        {
            if (_samplesOverflow) { SampleOverflow(); _clockOverflowSampled = true; }
            DirectClockOutputChanged?.Invoke(true);
        }
        else
        {
            _inputClockPhase = C.DirectClockDivider;
            CompletePreparedClockCycle();
            DirectClockOutputChanged?.Invoke(false);
            PrepareAdvancedClockCycle();
        }
    }

    private void CompletePreparedClockCycle()
    {
        try
        {
            if (_clockResetCycle) { HoldCmosReset(); _clockPhaseAdvanced = false; Tick(); }
            else RunClockCycle();
        }
        finally { _inputClockPhase = 0; _clockCyclePrepared = false; }
    }

    private void RunClockCycle()
    {
        if (cmos) ClockCmos();
        else ClockNmos();
    }

    private void PrepareAdvancedClockCycle()
    {
        _clockCycleStart = checked(_cycles * _clockDivider);
        _clockResetCycle = _samplesReset && ResetInputActive;
        _clockPresentOnly = true;
        try
        {
            if (_clockResetCycle) HoldCmosReset();
            else ClockCmos();
        }
        finally { _clockPresentOnly = false; }
        _clockCyclePrepared = true;
    }

    private void ClockCmos()
    {
        if (_samplesReset && ResetInputActive) { HoldCmosReset(); _clockPhaseAdvanced = false; Tick(); return; }
        if (_cmosResetHeld) BeginCmosReset();
        if (_cmosPhase == Phase.Opcode)
        {
            _cmosFlagsBefore = _p;
            if (_nmiAccepted) BeginCmosInterrupt(C.NmiVector);
            else if (_irqPending) BeginCmosInterrupt(C.IrqVector);
            else _instructionAddress = _pc;
        }
        _clockPhaseAdvanced = ExecuteCmosPhase();
    }

    private void ClockNmos()
    {
        if (_samplesReset && ResetInputActive) { HoldNmosReset(); Tick(); return; }
        if (_nmosResetHeld) BeginNmosReset();
        if (_nmosPhase == Phase.Opcode)
        {
            _nmosFlagsBefore = _p;
            if (_nmiAccepted) BeginNmosInterrupt(C.NmiVector);
            else if (_irqPending) BeginNmosInterrupt(C.IrqVector);
            else _instructionAddress = _pc;
        }
        ExecuteNmosPhase();
    }
}
