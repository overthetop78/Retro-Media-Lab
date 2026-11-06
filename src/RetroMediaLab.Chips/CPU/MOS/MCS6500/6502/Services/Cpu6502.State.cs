using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Contracts;
using C = RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Constants.Cpu6502Constants;

namespace RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;

public partial class Cpu6502
{
    /// <summary>Capture aussi les instructions suspendues, fronts en attente et phases de l'oscillateur.</summary>
    public Cpu6502State CaptureState() => new()
    {
        FormatVersion = C.StateFormatVersion,
        Variant = Variant,
        ClockFrequency = ClockFrequency,
        SamplesDataEnable = _samplesDataEnable,
        PublishesSync = _publishesSync,
        InputClockPhase = _inputClockPhase,
        ClockCycleStart = _clockCycleStart,
        ClockPresentOnly = _clockPresentOnly,
        ClockCyclePrepared = _clockCyclePrepared,
        ClockResetCycle = _clockResetCycle,
        ClockPhaseAdvanced = _clockPhaseAdvanced,
        ClockOverflowSampled = _clockOverflowSampled,
        ExternalClockDriven = _externalClockDriven,
        ExternalClockInputHigh = _externalClockInputHigh,
        CmosSync = _cmosSync,
        CmosHeldSync = _cmosHeldSync,
        CmosOpcodeHeld = _cmosOpcodeHeld,
        CmosResetHeld = _cmosResetHeld,
        CmosPhase = _cmosPhase,
        CmosOperation = _cmosInstruction.Operation,
        CmosMode = _cmosInstruction.Mode,
        CmosRepeatedCycles = _cmosInstruction.RepeatedCycles,
        CmosFlagsBefore = _cmosFlagsBefore,
        CmosLow = _cmosLow,
        CmosPointer = _cmosPointer,
        CmosData = _cmosData,
        CmosNopRemaining = _cmosNopRemaining,
        CmosBase = _cmosBase,
        CmosAddress = _cmosAddress,
        CmosVector = _cmosVector,
        CmosHardwareInterrupt = _cmosHardwareInterrupt,
        SamplesReady = _samplesReady,
        SamplesReset = _samplesReset,
        SamplesOverflow = _samplesOverflow,
        IrqPending = _irqPending,
        IrqLevel = _irqLevel,
        IrqPrevious = _irqPrevious,
        BranchIrqSample = _branchIrqSample,
        BranchNmiSample = _branchNmiSample,
        A = _a,
        X = _x,
        Y = _y,
        S = _s,
        P = _p,
        Pc = _pc,
        InstructionAddress = _instructionAddress,
        Cycles = _cycles,
        NmiLevel = _nmiLevel,
        NmiPending = _nmiPending,
        NmiPrevious = _nmiPrevious,
        NmiAccepted = _nmiAccepted,
        OverflowLevel = _overflowLevel,
        IrqPinConnected = _irqPinConnected,
        NmiPinConnected = _nmiPinConnected,
        IrqInput = _irqInput,
        NmiInputEdge = _nmiInputEdge,
        NmosPhase = _nmosPhase,
        NmosOperation = _nmosInstruction.Operation,
        NmosMode = _nmosInstruction.Mode,
        NmosFlagsBefore = _nmosFlagsBefore,
        NmosLow = _nmosLow,
        NmosPointer = _nmosPointer,
        NmosData = _nmosData,
        NmosAddress = _nmosAddress,
        NmosBase = _nmosBase,
        NmosVector = _nmosVector,
        NmosHardwareInterrupt = _nmosHardwareInterrupt,
        NmosSync = _nmosSync,
        NmosOpcodeHeld = _nmosOpcodeHeld,
        NmosResetHeld = _nmosResetHeld,
        NmosHeldSync = _nmosHeldSync,
        BusPinsEnabled = _busPinsEnabled,
        MemoryLockAsserted = _memoryLockAsserted,
        DirectDataBusEnable = _directDataBusEnable,
        HasDirectDataBusEnable = _hasDirectDataBusEnable,
        BusCyclePresented = _busCyclePresented,
        BusCycleWriting = _busCycleWriting,
        BusCycleAddress = _busCycleAddress,
        BusCycleData = _busCycleData,
        ResetPinConnected = _resetPinConnected,
        ResetInput = _resetInput,
        ReadyPinConnected = _readyPinConnected,
        ReadyInput = _readyInput,
        OverflowPinConnected = _overflowPinConnected,
        OverflowInputEdge = _overflowInputEdge,
    };

    /// <summary>Restaure sans exécuter de cycle ni modifier les raccordements. Le bus doit être restauré au même instant.</summary>
    public void RestoreState(Cpu6502State state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.FormatVersion != C.StateFormatVersion || state.Variant != Variant || state.ClockFrequency != ClockFrequency
            || state.SamplesDataEnable != _samplesDataEnable || state.PublishesSync != _publishesSync)
            throw new ArgumentException(nameof(state));
        _inputClockPhase = state.InputClockPhase;
        _clockCycleStart = state.ClockCycleStart;
        _clockPresentOnly = state.ClockPresentOnly;
        _clockCyclePrepared = state.ClockCyclePrepared;
        _clockResetCycle = state.ClockResetCycle;
        _clockPhaseAdvanced = state.ClockPhaseAdvanced;
        _clockOverflowSampled = state.ClockOverflowSampled;
        _externalClockDriven = state.ExternalClockDriven;
        _externalClockInputHigh = state.ExternalClockInputHigh;
        _cmosSync = state.CmosSync;
        _cmosHeldSync = state.CmosHeldSync;
        _cmosOpcodeHeld = state.CmosOpcodeHeld;
        _cmosResetHeld = state.CmosResetHeld;
        _cmosPhase = state.CmosPhase;
        _cmosInstruction = new(state.CmosOperation, state.CmosMode, state.CmosRepeatedCycles);
        _cmosFlagsBefore = state.CmosFlagsBefore;
        _cmosLow = state.CmosLow;
        _cmosPointer = state.CmosPointer;
        _cmosData = state.CmosData;
        _cmosNopRemaining = state.CmosNopRemaining;
        _cmosBase = state.CmosBase;
        _cmosAddress = state.CmosAddress;
        _cmosVector = state.CmosVector;
        _cmosHardwareInterrupt = state.CmosHardwareInterrupt;
        _samplesReady = state.SamplesReady;
        _samplesReset = state.SamplesReset;
        _samplesOverflow = state.SamplesOverflow;
        _irqPending = state.IrqPending;
        _irqLevel = state.IrqLevel;
        _irqPrevious = state.IrqPrevious;
        _branchIrqSample = state.BranchIrqSample;
        _branchNmiSample = state.BranchNmiSample;
        _a = state.A;
        _x = state.X;
        _y = state.Y;
        _s = state.S;
        _p = state.P;
        _pc = state.Pc;
        _instructionAddress = state.InstructionAddress;
        _cycles = state.Cycles;
        _nmiLevel = state.NmiLevel;
        _nmiPending = state.NmiPending;
        _nmiPrevious = state.NmiPrevious;
        _nmiAccepted = state.NmiAccepted;
        _overflowLevel = state.OverflowLevel;
        _irqPinConnected = state.IrqPinConnected;
        _nmiPinConnected = state.NmiPinConnected;
        _irqInput = state.IrqInput;
        _nmiInputEdge = state.NmiInputEdge;
        _nmosPhase = state.NmosPhase;
        _nmosInstruction = new(state.NmosOperation, state.NmosMode);
        _nmosFlagsBefore = state.NmosFlagsBefore;
        _nmosLow = state.NmosLow;
        _nmosPointer = state.NmosPointer;
        _nmosData = state.NmosData;
        _nmosAddress = state.NmosAddress;
        _nmosBase = state.NmosBase;
        _nmosVector = state.NmosVector;
        _nmosHardwareInterrupt = state.NmosHardwareInterrupt;
        _nmosSync = state.NmosSync;
        _nmosOpcodeHeld = state.NmosOpcodeHeld;
        _nmosResetHeld = state.NmosResetHeld;
        _nmosHeldSync = state.NmosHeldSync;
        _busPinsEnabled = state.BusPinsEnabled;
        _memoryLockAsserted = state.MemoryLockAsserted;
        _directDataBusEnable = state.DirectDataBusEnable;
        _hasDirectDataBusEnable = state.HasDirectDataBusEnable;
        _busCyclePresented = state.BusCyclePresented;
        _busCycleWriting = state.BusCycleWriting;
        _busCycleAddress = state.BusCycleAddress;
        _busCycleData = state.BusCycleData;
        _resetPinConnected = state.ResetPinConnected;
        _resetInput = state.ResetInput;
        _readyPinConnected = state.ReadyPinConnected;
        _readyInput = state.ReadyInput;
        _overflowPinConnected = state.OverflowPinConnected;
        _overflowInputEdge = state.OverflowInputEdge;
    }
}
